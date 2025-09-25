using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MEbAsariReproductionAddon
{
    // This class explicitly patches CanEverProduceChild at startup and logs results.
    [StaticConstructorOnStartup]
    public static class PregnancyCanEverProducePatcher_ForcePatch
    {
        private static bool Verbose => Prefs.DevMode;

        static PregnancyCanEverProducePatcher_ForcePatch()
        {
            try
            {
                var harmony = new Harmony("com.yourname.mebasari.pregpatch");
                MethodInfo target = AccessTools.Method(typeof(PregnancyUtility), "CanEverProduceChild", new Type[] { typeof(Pawn), typeof(Pawn) });

                if (target == null)
                {
                    // Fallback: search for any method on PregnancyUtility with signature (Pawn, Pawn) returning AcceptanceReport
                    var pregType = AccessTools.TypeByName("RimWorld.PregnancyUtility") ?? AccessTools.TypeByName("PregnancyUtility") ?? typeof(PregnancyUtility);
                    target = pregType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                                    .FirstOrDefault(m => m.ReturnType == typeof(AcceptanceReport)
                                                         && m.GetParameters().Length == 2
                                                         && m.GetParameters()[0].ParameterType == typeof(Pawn)
                                                         && m.GetParameters()[1].ParameterType == typeof(Pawn));
                }

                if (target == null)
                {
                    Log.Error("[MEbAsariReproductionAddon] Could not find PregnancyUtility.CanEverProduceChild to patch.");
                    return;
                }

                if (Verbose) Log.Message($"[MEbAsariReproductionAddon] Patching {target.DeclaringType.FullName}.{target.Name}");

                // Create a HarmonyMethod pointing to our prefix
                var prefixMethod = new HarmonyMethod(typeof(PregnancyCanEverProducePatcher_ForcePatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));
                // Mark priority last by applying attribute on method (see Prefix below) — Harmony respects that.
                harmony.Patch(target, prefix: prefixMethod);

                if (Verbose) Log.Message("[MEbAsariReproductionAddon] Patched PregnancyUtility.CanEverProduceChild");
            }
            catch (Exception ex)
            {
                Log.Error("[MEbAsariReproductionAddon] Exception while applying CanEverProduceChild patch: " + ex);
            }
        }

        // Prefix will execute for the method. Priority set to Last so we can override vanilla for our narrow cases.
        [HarmonyPriority(Priority.Last)]
        private static bool Prefix(Pawn first, Pawn second, ref AcceptanceReport __result)
        {
            try
            {
                if (first == null || second == null) return true;

                // Quick dev log when prefix runs
                if (Verbose) Log.Message($"[MEbAsariReproductionAddon] CanEverProduceChild prefix invoked for {first.LabelShort} / {second.LabelShort}.");

                // Only override female-female pairs where at least one has the gene
                if (!(first.gender == Gender.Female && second.gender == Gender.Female))
                    return true;

                bool firstHas = first.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                bool secondHas = second.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                if (!firstHas && !secondHas) return true;

                Pawn mother = firstHas ? first : second;
                Pawn donor = mother == first ? second : first;

                // Mirror vanilla checks (dead, fertility, age, sterile) — same strings to keep UI messages consistent.
                if (mother.Dead) { __result = (AcceptanceReport)"PawnIsDead".Translate(mother.Named("PAWN")); return false; }
                if (donor.Dead) { __result = (AcceptanceReport)"PawnIsDead".Translate(donor.Named("PAWN")); return false; }

                bool donorInfertile = donor.GetStatValue(StatDefOf.Fertility) <= 0f;
                bool motherInfertile = mother.GetStatValue(StatDefOf.Fertility) <= 0f;
                if (donorInfertile & motherInfertile) { __result = (AcceptanceReport)"PawnsAreInfertile".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve(); return false; }
                if (donorInfertile != motherInfertile) { __result = (AcceptanceReport)"PawnIsInfertile".Translate((donorInfertile ? (object)donor : (object)mother).Named("PAWN")).Resolve(); return false; }

                bool donorTooYoung = !donor.ageTracker.CurLifeStage.reproductive;
                bool motherTooYoung = !mother.ageTracker.CurLifeStage.reproductive;
                if (donorTooYoung & motherTooYoung) { __result = (AcceptanceReport)"PawnsAreTooYoung".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve(); return false; }
                if (donorTooYoung != motherTooYoung) { __result = (AcceptanceReport)"PawnIsTooYoung".Translate((donorTooYoung ? (object)donor : (object)mother).Named("PAWN")).Resolve(); return false; }

                bool motherSterile = mother.Sterile() && PregnancyUtility.GetPregnancyHediff(mother) == null;
                bool donorSterile = donor.Sterile();
                if (donorSterile & motherSterile) { __result = (AcceptanceReport)"PawnsAreSterile".Translate(donor.Named("PAWN1"), mother.Named("PAWN2")).Resolve(); return false; }
                if (donorSterile != motherSterile) { __result = (AcceptanceReport)"PawnIsSterile".Translate((donorSterile ? (object)donor : (object)mother).Named("PAWN")).Resolve(); return false; }

                // All checks passed -> accept
                __result = AcceptanceReport.WasAccepted;
                if (Verbose) Log.Message($"[MEbAsariReproductionAddon] CanEverProduceChild: accepted for {mother.LabelShort} + {donor.LabelShort}");
                return false; // skip original
            }
            catch (Exception ex)
            {
                Log.Error("[MEbAsariReproductionAddon] Exception in CanEverProduceChild prefix: " + ex);
                return true; // fallback to vanilla on error
            }
        }
    }
}
