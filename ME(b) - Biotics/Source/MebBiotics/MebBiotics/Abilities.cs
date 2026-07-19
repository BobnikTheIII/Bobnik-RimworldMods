using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace MebBiotics
{
    // ============================================================================================
    // Generic "apply a hediff to everyone in an area" psycast. VEF's AbilityExtension_Hediff only
    // touches the direct target, so AoE powers (Lift Grenade, Warp Field, Cluster Ward) need this.
    // Radius and duration go through the VEF stat pipeline (GetRadiusForPawn / GetDurationForPawn),
    // so radiusStatFactors / durationTimeStatFactors in the XML scale with psychic sensitivity the
    // same way vanilla VPE psycasts do. Duration lands on the hediff's Disappears comp via the
    // base ApplyHediff helper.
    // ============================================================================================
    public class AbilityExtension_AoEHediff : DefModExtension
    {
        public HediffDef hediff;
        public float severity = -1f;
        public bool affectEnemies = true;
        public bool affectAllies;
    }

    public class Ability_AoEHediff : Ability
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            AbilityExtension_AoEHediff ext = def.GetModExtension<AbilityExtension_AoEHediff>();
            if (ext == null || ext.hediff == null)
            {
                return;
            }
            int duration = (int)GetDurationForPawn();
            float radius = GetRadiusForPawn();
            foreach (GlobalTargetInfo target in targets)
            {
                Map map = target.Map ?? pawn.Map;
                if (map == null)
                {
                    continue;
                }
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(target.Cell, map, radius, true)
                             .OfType<Pawn>().ToList())
                {
                    if (p.Dead)
                    {
                        continue;
                    }
                    bool hostile = p.HostileTo(pawn);
                    if (hostile && !ext.affectEnemies)
                    {
                        continue;
                    }
                    if (!hostile && !ext.affectAllies)
                    {
                        continue;
                    }
                    ApplyHediff(p, ext.hediff, null, duration, ext.severity);
                }
            }
        }
    }

    // ============================================================================================
    // Pull: yank the target a few cells toward the caster and stagger them. There is no vanilla or
    // VEF "forced move" ability, so this teleports the victim along the line of sight toward the
    // caster (last standable cell, max pullCells) - cheap, save-safe, and reads clearly in game.
    // Notify_Teleported cleans up pathing/jobs so the victim doesn't glide back.
    // ============================================================================================
    public class Ability_Pull : Ability
    {
        private const int PullCells = 6;
        private const int StunTicks = 45;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            foreach (GlobalTargetInfo target in targets)
            {
                Pawn victim = target.Thing as Pawn;
                if (victim == null || victim.Dead || !victim.Spawned)
                {
                    continue;
                }
                Map map = victim.Map;
                IntVec3 dest = victim.Position;
                int steps = 0;
                foreach (IntVec3 cell in GenSight.PointsOnLineOfSight(victim.Position, pawn.Position))
                {
                    if (steps >= PullCells || cell == pawn.Position)
                    {
                        break;
                    }
                    if (cell == victim.Position)
                    {
                        continue;
                    }
                    if (!cell.Standable(map))
                    {
                        break;
                    }
                    dest = cell;
                    steps++;
                }
                if (dest != victim.Position)
                {
                    victim.Position = dest;
                    victim.Notify_Teleported(true, true);
                    FleckMaker.ThrowDustPuffThick(dest.ToVector3Shifted(), map, 2f, Color.white);
                }
                if (victim.stances != null && victim.stances.stunner != null)
                {
                    victim.stances.stunner.StunFor(StunTicks, pawn, false);
                }
            }
        }
    }

    // ============================================================================================
    // Biotic Charge / Heavy Charge: VPE's Killskip already provides the teleport-strike (with the
    // on-kill chain, which suits the ME vanguard perfectly); this subclass adds the signature
    // "charging refreshes your barrier" - apply or top up MebBiotic_Barrier on the caster.
    // ============================================================================================
    public class Ability_BioticCharge : VanillaPsycastsExpanded.Ability_Killskip
    {
        private const int BarrierDuration = 1800;

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            HediffDef barrierDef = DefDatabase<HediffDef>.GetNamed("MebBiotic_Barrier", false);
            if (barrierDef == null)
            {
                return;
            }
            int duration = (int)(BarrierDuration * pawn.GetStatValue(StatDefOf.PsychicSensitivity));
            HediffWithComps existing = pawn.health.hediffSet.GetFirstHediffOfDef(barrierDef) as HediffWithComps;
            if (existing != null)
            {
                HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                if (disappears != null && disappears.ticksToDisappear < duration)
                {
                    disappears.ticksToDisappear = duration;
                }
            }
            else
            {
                ApplyHediff(pawn, barrierDef, null, duration, -1f);
            }
        }
    }

    // ============================================================================================
    // Persistent biotic field (Singularity, Barrier Sphere). The ability side is just VEF's
    // Ability_Spawn dropping an ethereal Thing; this comp is the field behavior: every interval it
    // applies/refreshes a hediff on every pawn in radius. The hediff duration is kept short so the
    // effect fades quickly after leaving the field ("released on exit"), while the field keeps
    // re-topping it for anyone inside. Field lifetime itself is vanilla CompLifespan.
    // Deliberately faction-blind: a singularity lifts anyone, a barrier sphere shields anyone -
    // stay out of your own singularity.
    // ============================================================================================
    public class CompProperties_BioticField : CompProperties
    {
        public float radius = 3.9f;
        public HediffDef hediff;
        public int tickInterval = 30;
        public int hediffDuration = 120;
        public float severity = -1f;

        public CompProperties_BioticField()
        {
            compClass = typeof(CompBioticField);
        }
    }

    public class CompBioticField : ThingComp
    {
        private int ticks;

        public CompProperties_BioticField Props
        {
            get { return (CompProperties_BioticField)props; }
        }

        public override void CompTick()
        {
            base.CompTick();
            ticks++;
            if (ticks < Props.tickInterval)
            {
                return;
            }
            ticks = 0;
            if (!parent.Spawned || Props.hediff == null)
            {
                return;
            }
            Map map = parent.Map;
            foreach (Pawn p in GenRadial.RadialDistinctThingsAround(parent.Position, map, Props.radius, true)
                         .OfType<Pawn>().ToList())
            {
                if (p.Dead)
                {
                    continue;
                }
                Hediff existing = p.health.hediffSet.GetFirstHediffOfDef(Props.hediff);
                if (existing == null)
                {
                    existing = HediffMaker.MakeHediff(Props.hediff, p);
                    if (Props.severity > float.Epsilon)
                    {
                        existing.Severity = Props.severity;
                    }
                    p.health.AddHediff(existing);
                }
                HediffWithComps withComps = existing as HediffWithComps;
                HediffComp_Disappears disappears = withComps != null ? withComps.TryGetComp<HediffComp_Disappears>() : null;
                if (disappears != null && disappears.ticksToDisappear < Props.hediffDuration)
                {
                    disappears.ticksToDisappear = Props.hediffDuration;
                }
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticks, "MebBiotics_fieldTicks", 0);
        }
    }
}
