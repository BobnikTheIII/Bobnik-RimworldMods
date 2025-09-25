using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MEbAsariReproductionAddon
{
    // Patch PregnancyUtility.CanEverProduceChild to allow female-female pregnancies
    [HarmonyPatch(typeof(PregnancyUtility))]
    [HarmonyPatch(nameof(PregnancyUtility.CanEverProduceChild))]
    public static class PregnancyCanEverProducePatcher
    {
        // Run as a Prefix with Last priority so we can short-circuit the vanilla check for our narrow case.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Pawn first, Pawn second, ref AcceptanceReport __result)
        {
            try
            {
                // If either null, let vanilla handle it
                if (first == null || second == null) return true;

                // If vanilla case (different genders) — we don't interfere, let vanilla run
                if (first.gender != Gender.Female || second.gender != Gender.Female)
                    return true;

                // If neither pawn has our gene, leave vanilla behaviour (which will probably reject same-gender).
                bool firstHas = first.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                bool secondHas = second.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                if (!firstHas && !secondHas) return true;

                // Decide roles: carrier becomes 'mother', other is 'donor'
                Pawn mother = firstHas ? first : second;
                Pawn donor = mother == first ? second : first;

                // FOLLOW vanilla checks as closely as possible, returning the same AcceptanceReport reasons.

                // Dead checks
                if (mother.Dead)
                {
                    __result = (AcceptanceReport)"PawnIsDead".Translate(mother.Named("PAWN"));
                    return false;
                }
                if (donor.Dead)
                {
                    __result = (AcceptanceReport)"PawnIsDead".Translate(donor.Named("PAWN"));
                    return false;
                }

                // Fertility checks (uses StatDefOf.Fertility)
                bool donorInfertile = donor.GetStatValue(StatDefOf.Fertility) <= 0f;
                bool motherInfertile = mother.GetStatValue(StatDefOf.Fertility) <= 0f;
                if (donorInfertile & motherInfertile)
                {
                    __result = (AcceptanceReport)"PawnsAreInfertile".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
                    return false;
                }
                if (donorInfertile != motherInfertile)
                {
                    __result = (AcceptanceReport)"PawnIsInfertile".Translate((donorInfertile ? (object)donor : (object)mother).Named("PAWN")).Resolve();
                    return false;
                }

                // Age / life stage checks
                bool donorTooYoung = !donor.ageTracker.CurLifeStage.reproductive;
                bool motherTooYoung = !mother.ageTracker.CurLifeStage.reproductive;
                if (donorTooYoung & motherTooYoung)
                {
                    __result = (AcceptanceReport)"PawnsAreTooYoung".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
                    return false;
                }
                if (donorTooYoung != motherTooYoung)
                {
                    __result = (AcceptanceReport)"PawnIsTooYoung".Translate((donorTooYoung ? (object)donor : (object)mother).Named("PAWN")).Resolve();
                    return false;
                }

                // Sterility checks (matching vanilla: mother sterile AND not already pregnant is special-cased)
                bool motherSterile = mother.Sterile() && PregnancyUtility.GetPregnancyHediff(mother) == null;
                bool donorSterile = donor.Sterile();
                if (donorSterile & motherSterile)
                {
                    __result = (AcceptanceReport)"PawnsAreSterile".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve();
                    return false;
                }
                if (donorSterile != motherSterile)
                {
                    __result = (AcceptanceReport)"PawnIsSterile".Translate((donorSterile ? (object)donor : (object)mother).Named("PAWN")).Resolve();
                    return false;
                }

                // Passed all checks — accept
                __result = AcceptanceReport.WasAccepted;
                return false; // skip original: we've produced __result
            }
            catch (Exception ex)
            {
                Log.Error("[MEbAsariReproductionAddon] Exception in PregnancyCanEverProducePatcher: " + ex);
                // On unexpected error, fall back to vanilla logic
                return true;
            }
        }
    }
}
