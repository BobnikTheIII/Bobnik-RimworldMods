using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace MebBiotics
{
    // ============================================================================================
    // Biotic Combos - the ME primer/detonator system.
    //
    // Design (see biotics_roadmap.md): PRIMERS (Pull, Lift, Lift Grenade, Singularity, Warp,
    // Reave, Dark Channel, Warp Field) leave a decaying "biotically charged" marker hediff on
    // their victims. DETONATORS (Throw, Heavy Throw, Charge, Heavy Charge, Slam, Shockwave, Nova,
    // Flare) consume the marker for a bonus biotic explosion centered on each charged victim.
    // The bonus is priced into the detonator, so combos reward setup without making single casts
    // free. Stasis is deliberately NOT a primer - an invulnerable target can't be detonated.
    //
    // Implementation: one XML extension marks each ability's combo role; the logic runs from the
    // same Ability.Cast Harmony postfix as biotic strain (one choke point for every VEF ability,
    // and extension presence is the gate - vanilla/VPE psycasts are untouched). Both single-target
    // and area abilities work: a pawn target primes/detonates that pawn, a cell target sweeps the
    // ability's radius.
    // ============================================================================================
    public class AbilityExtension_Combo : DefModExtension
    {
        public bool primer;
        public bool detonator;
        public int detonationDamage = 14;
        public float detonationRadius = 2.4f;
    }

    public static class BioticCombos
    {
        private const string MarkDefName = "MebBiotic_Charged";
        private const int MarkDurationTicks = 900;

        public static void HandleCast(VEF.Abilities.Ability ability, GlobalTargetInfo[] targets)
        {
            if (ability == null || ability.pawn == null || ability.def == null || targets == null)
            {
                return;
            }
            AbilityExtension_Combo ext = ability.def.GetModExtension<AbilityExtension_Combo>();
            if (ext == null)
            {
                return;
            }
            if (ext.detonator)
            {
                Detonate(ability, targets, ext);
            }
            if (ext.primer)
            {
                ApplyPrimer(ability, targets);
            }
        }

        // Marks every pawn the cast touched: the direct pawn target, or - for cell-targeted area
        // powers - everyone inside the ability's radius. Re-priming refreshes the mark.
        private static void ApplyPrimer(VEF.Abilities.Ability ability, GlobalTargetInfo[] targets)
        {
            HediffDef markDef = DefDatabase<HediffDef>.GetNamed(MarkDefName, false);
            if (markDef == null)
            {
                return;
            }
            foreach (GlobalTargetInfo target in targets)
            {
                Map map = target.Map ?? ability.pawn.Map;
                if (map == null)
                {
                    continue;
                }
                Pawn direct = target.Thing as Pawn;
                if (direct != null)
                {
                    Mark(direct, markDef, ability.pawn);
                    continue;
                }
                float radius = ability.GetRadiusForPawn();
                if (radius <= 0f)
                {
                    continue;
                }
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(target.Cell, map, radius, true)
                             .OfType<Pawn>().ToList())
                {
                    if (!p.Dead && p != ability.pawn)
                    {
                        Mark(p, markDef, ability.pawn);
                    }
                }
            }
        }

        private static void Mark(Pawn victim, HediffDef markDef, Pawn caster)
        {
            if (victim.Dead || victim.health == null)
            {
                return;
            }
            HediffWithComps existing = victim.health.hediffSet.GetFirstHediffOfDef(markDef) as HediffWithComps;
            if (existing != null)
            {
                HediffComp_Disappears disappears = existing.TryGetComp<HediffComp_Disappears>();
                if (disappears != null)
                {
                    disappears.ticksToDisappear = MarkDurationTicks;
                }
                return;
            }
            victim.health.AddHediff(HediffMaker.MakeHediff(markDef, victim));
        }

        // Consumes the mark on every charged pawn in the detonator's reach: the direct pawn
        // target, or everyone within the ability's radius (falling back to detonationRadius for
        // radius-less strikes like Charge). Each consumed mark triggers a biotic explosion
        // centered on that victim; the caster is immune to their own detonations.
        private static void Detonate(VEF.Abilities.Ability ability, GlobalTargetInfo[] targets, AbilityExtension_Combo ext)
        {
            HediffDef markDef = DefDatabase<HediffDef>.GetNamed(MarkDefName, false);
            if (markDef == null)
            {
                return;
            }
            foreach (GlobalTargetInfo target in targets)
            {
                Map map = target.Map ?? ability.pawn.Map;
                if (map == null)
                {
                    continue;
                }
                Pawn direct = target.Thing as Pawn;
                if (direct != null)
                {
                    TryDetonateOn(direct, markDef, ability.pawn, map, ext);
                    continue;
                }
                float radius = ability.GetRadiusForPawn();
                if (radius <= 0f)
                {
                    radius = ext.detonationRadius;
                }
                foreach (Pawn p in GenRadial.RadialDistinctThingsAround(target.Cell, map, radius, true)
                             .OfType<Pawn>().ToList())
                {
                    TryDetonateOn(p, markDef, ability.pawn, map, ext);
                }
            }
        }

        private static void TryDetonateOn(Pawn victim, HediffDef markDef, Pawn caster, Map map, AbilityExtension_Combo ext)
        {
            if (victim.Dead || victim.health == null || !victim.Spawned)
            {
                return;
            }
            Hediff mark = victim.health.hediffSet.GetFirstHediffOfDef(markDef);
            if (mark == null)
            {
                return;
            }
            victim.health.RemoveHediff(mark);
            GenExplosion.DoExplosion(
                victim.Position,
                map,
                ext.detonationRadius,
                DamageDefOf.Blunt,
                caster,
                damAmount: ext.detonationDamage,
                armorPenetration: 0.5f,
                explosionSound: SoundDefOf.Psycast_Skip_Exit,
                ignoredThings: new System.Collections.Generic.List<Thing> { caster });
            FleckMaker.Static(victim.Position, map, FleckDefOf.PsycastAreaEffect, 1.4f);
        }
    }
}
