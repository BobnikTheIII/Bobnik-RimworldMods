using System.Linq;
using RimWorld;
using Verse;

namespace MebBiotics
{
    // ============================================================================================
    // Biotic damage-over-time.
    //
    // Vanilla has no generic "ticking damage" HediffComp, and VEF's ability framework only applies
    // hediffs - it never deals periodic damage. This comp is the single DoT engine shared by Warp,
    // Reave and Dark Channel. Damage is applied with near-infinite armor penetration because biotic
    // fields tear matter directly; armor is irrelevant (this is Warp's anti-armor identity).
    //
    // jumpOnDeath implements Dark Channel: when the victim dies, the hediff re-applies itself to
    // the nearest living pawn of the *same faction* within jumpRadius (enemies arrive in
    // same-faction groups, so "victim's faction" is the cheapest correct notion of "next enemy"
    // that needs no caster back-reference). Remaining duration is carried over.
    // ============================================================================================
    public class HediffCompProperties_BioticDoT : HediffCompProperties
    {
        public float damagePerInterval = 2f;
        public int tickInterval = 60;
        public bool jumpOnDeath;
        public float jumpRadius = 8.9f;

        public HediffCompProperties_BioticDoT()
        {
            compClass = typeof(HediffComp_BioticDoT);
        }
    }

    public class HediffComp_BioticDoT : HediffComp
    {
        private int ticks;

        public HediffCompProperties_BioticDoT Props
        {
            get { return (HediffCompProperties_BioticDoT)props; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticks++;
            if (ticks < Props.tickInterval)
            {
                return;
            }
            ticks = 0;
            if (Pawn.Dead || !Pawn.Spawned)
            {
                return;
            }
            // High armorPenetration so armor never mitigates the biotic tearing.
            DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Props.damagePerInterval, 999f);
            Pawn.TakeDamage(dinfo);
        }

        public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
        {
            base.Notify_PawnDied(dinfo, culprit);
            if (!Props.jumpOnDeath)
            {
                return;
            }
            Map map = Pawn.MapHeld;
            if (map == null)
            {
                return;
            }
            IntVec3 pos = Pawn.PositionHeld;
            Pawn best = null;
            float bestDist = float.MaxValue;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p == Pawn || p.Dead || p.Faction != Pawn.Faction)
                {
                    continue;
                }
                // Don't double-stack on someone already channeled.
                if (p.health.hediffSet.GetFirstHediffOfDef(parent.def) != null)
                {
                    continue;
                }
                float dist = p.Position.DistanceTo(pos);
                if (dist <= Props.jumpRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = p;
                }
            }
            if (best == null)
            {
                return;
            }
            Hediff jumped = HediffMaker.MakeHediff(parent.def, best);
            best.health.AddHediff(jumped);
            // Carry the remaining duration over instead of restarting the clock.
            HediffComp_Disappears mine = parent.TryGetComp<HediffComp_Disappears>();
            HediffWithComps jumpedWithComps = jumped as HediffWithComps;
            HediffComp_Disappears theirs = jumpedWithComps != null ? jumpedWithComps.TryGetComp<HediffComp_Disappears>() : null;
            if (mine != null && theirs != null)
            {
                theirs.ticksToDisappear = mine.ticksToDisappear;
            }
            FleckMaker.Static(best.Position, map, FleckDefOf.PsycastAreaEffect, 1f);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticks, "MebBiotics_dotTicks", 0);
        }
    }

    // ============================================================================================
    // Biotic heal-over-time. Used by Biotic Mending (targeted) and Reave's self-sustain (applied
    // to the caster via VEF's AbilityExtension_Hediff applyToCaster). Heals existing injuries a
    // little every interval; does nothing for diseases or missing parts on purpose - biotics knit
    // tissue, they don't cure plague or regrow limbs.
    // ============================================================================================
    public class HediffCompProperties_BioticHoT : HediffCompProperties
    {
        public float healPerInterval = 0.5f;
        public int tickInterval = 60;

        public HediffCompProperties_BioticHoT()
        {
            compClass = typeof(HediffComp_BioticHoT);
        }
    }

    public class HediffComp_BioticHoT : HediffComp
    {
        private int ticks;

        public HediffCompProperties_BioticHoT Props
        {
            get { return (HediffCompProperties_BioticHoT)props; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticks++;
            if (ticks < Props.tickInterval)
            {
                return;
            }
            ticks = 0;
            if (Pawn.Dead)
            {
                return;
            }
            Hediff_Injury injury = Pawn.health.hediffSet.hediffs
                .OfType<Hediff_Injury>()
                .Where(i => i.Severity > 0f && !i.IsPermanent())
                .FirstOrDefault();
            if (injury != null)
            {
                injury.Heal(Props.healPerInterval);
            }
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticks, "MebBiotics_hotTicks", 0);
        }
    }

    // ============================================================================================
    // Annihilation Field: a moving damage aura carried by the caster (a hediff, so it follows the
    // pawn - a spawned field Thing would stay put). Every interval it deals armor-ignoring biotic
    // damage to every pawn hostile to the carrier within radius. Duration is governed by the
    // hediff's Disappears comp, set by the casting ability like any other VEF duration.
    // ============================================================================================
    public class HediffCompProperties_BioticAura : HediffCompProperties
    {
        public float radius = 3.9f;
        public float damagePerInterval = 1.5f;
        public int tickInterval = 60;

        public HediffCompProperties_BioticAura()
        {
            compClass = typeof(HediffComp_BioticAura);
        }
    }

    public class HediffComp_BioticAura : HediffComp
    {
        private int ticks;

        public HediffCompProperties_BioticAura Props
        {
            get { return (HediffCompProperties_BioticAura)props; }
        }

        public override void CompPostTick(ref float severityAdjustment)
        {
            ticks++;
            if (ticks < Props.tickInterval)
            {
                return;
            }
            ticks = 0;
            if (Pawn.Dead || !Pawn.Spawned)
            {
                return;
            }
            Map map = Pawn.Map;
            foreach (Pawn p in GenRadial.RadialDistinctThingsAround(Pawn.Position, map, Props.radius, true)
                         .OfType<Pawn>().ToList())
            {
                if (p == Pawn || p.Dead || !p.HostileTo(Pawn))
                {
                    continue;
                }
                DamageInfo dinfo = new DamageInfo(DamageDefOf.Blunt, Props.damagePerInterval, 999f, -1f, Pawn);
                p.TakeDamage(dinfo);
            }
            FleckMaker.Static(Pawn.Position, map, FleckDefOf.PsycastAreaEffect, 0.6f);
        }

        public override void CompExposeData()
        {
            base.CompExposeData();
            Scribe_Values.Look(ref ticks, "MebBiotics_auraTicks", 0);
        }
    }

    // ============================================================================================
    // Biotic Amp: grants psylink level 1 on install. Design: the Eezo gene decides whether the
    // biotic paths *appear*; the amp decides whether the pawn can actually *cast*. Without this a
    // gene-carrying, amp-installed pawn would still have to hunt a psylink separately. Only grants
    // when the pawn has no psylink at all - higher levels stay on normal VPE progression.
    // ============================================================================================
    public class HediffCompProperties_GrantPsylink : HediffCompProperties
    {
        public HediffCompProperties_GrantPsylink()
        {
            compClass = typeof(HediffComp_GrantPsylink);
        }
    }

    public class HediffComp_GrantPsylink : HediffComp
    {
        public override void CompPostPostAdd(DamageInfo? dinfo)
        {
            base.CompPostPostAdd(dinfo);
            if (Pawn.GetPsylinkLevel() < 1)
            {
                Pawn.ChangePsylinkLevel(1, false);
            }
        }
    }
}
