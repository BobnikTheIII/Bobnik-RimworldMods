using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MEbAsariReproductionAddon
{
    [StaticConstructorOnStartup]
    public static class AsariReproductionMod
    {
        // Helper to only emit verbose messages when Dev Mode is enabled
        private static bool Verbose => Prefs.DevMode;

        static AsariReproductionMod()
        {
            try
            {
                var harmony = new Harmony("com.bobniktheiii.mebasarireproduction");
                if (Verbose) Log.Message("[MEbAsariReproductionAddon] Starting Harmony patching.");

                // 1) Try to patch PregnancyUtility.PregnancyChanceForPartnersWithoutPregnancyApproach (postfix)
                MethodInfo chanceMethod = null;
                try
                {
                    // Best-effort lookups
                    var pregUtilType = AccessTools.TypeByName("RimWorld.PregnancyUtility") ?? AccessTools.TypeByName("PregnancyUtility");
                    if (pregUtilType != null)
                    {
                        var allStatic = pregUtilType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                        chanceMethod = allStatic.FirstOrDefault(mi =>
                            mi.ReturnType == typeof(float)
                            && mi.GetParameters().Length >= 2
                            && mi.GetParameters()[0].ParameterType == typeof(Pawn)
                            && mi.GetParameters()[1].ParameterType == typeof(Pawn)
                            && mi.Name.IndexOf("PregnancyChanceForPartnersWithoutPregnancyApproach", StringComparison.OrdinalIgnoreCase) >= 0
                        );

                        // fallback: any static float (Pawn,Pawn) method in PregnancyUtility
                        if (chanceMethod == null)
                        {
                            chanceMethod = allStatic.FirstOrDefault(mi =>
                                mi.ReturnType == typeof(float)
                                && mi.GetParameters().Length >= 2
                                && mi.GetParameters()[0].ParameterType == typeof(Pawn)
                                && mi.GetParameters()[1].ParameterType == typeof(Pawn));
                        }
                    }
                }
                catch (Exception ex)
                {
                    // This is an internal reflection failure — keep as Error so it shows up for authors.
                    Log.Error("[MEbAsariReproductionAddon] Exception while finding pregnancy chance method: " + ex);
                }

                if (chanceMethod != null)
                {
                    if (Verbose) Log.Message($"[MEbAsariReproductionAddon] Patching pregnancy chance method: {chanceMethod.DeclaringType.FullName}.{chanceMethod.Name}");
                    var postfix = new HarmonyMethod(typeof(AsariReproductionMod).GetMethod(nameof(PregnancyChancePostfix), BindingFlags.Static | BindingFlags.NonPublic));
                    harmony.Patch(chanceMethod, postfix: postfix);
                }
                else
                {
                    if (Verbose) Log.Message("[MEbAsariReproductionAddon] Could NOT find pregnancy chance method to patch. Will attempt to patch apply methods instead.");
                }

                // 2) Ask the apply patcher to search & patch likely "apply pregnancy" methods
                PregnancyApplyPatcher.PatchAllApplyMethods(harmony);

                if (Verbose) Log.Message("[MEbAsariReproductionAddon] Harmony patching complete.");
            }
            catch (Exception ex)
            {
                // Keep this as an error — static ctor failing is critical.
                Log.Error("[MEbAsariReproductionAddon] Exception in static constructor: " + ex);
            }
        }

        // Postfix modifies computed pregnancy chance when woman has the gene and partner is female.
        // Keep logs small — only output adjustments in Dev Mode to avoid console spam.
        private static void PregnancyChancePostfix(Pawn woman, Pawn man, ref float __result)
        {
            try
            {
                if (woman == null || man == null) return;

                bool womanHasGene = woman.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                bool bothFemale = woman.gender == Gender.Female && man.gender == Gender.Female;
                if (!bothFemale || !womanHasGene) return;

                // Adjust the chance conservatively — tweak these constants as you like
                const float baseGeneFactor = 1.5f;
                float womanFertility = Math.Max(0.0f, woman.GetStatValue(StatDefOf.Fertility, true));
                float fertilityMultiplier = 0.5f + womanFertility; // if fertility ~1 => 1.5
                float newChance = __result * baseGeneFactor * fertilityMultiplier;
                if (newChance > 0.95f) newChance = 0.95f;
                if (__result < 0.01f)
                    newChance = Math.Max(newChance, 0.02f * womanFertility);

                // Dev-mode only: log the adjustment so regular players don't see it
                if (Verbose)
                    Log.Message($"[MEbAsariReproductionAddon] Adjusted pregnancy chance {woman.LabelShort}: {__result:0.000} -> {newChance:0.000} (partner {man.LabelShort}).");

                __result = newChance;
            }
            catch (Exception ex)
            {
                // This is an internal failure that indicates something deeper; keep as Error so it surfaces.
                Log.Error("[MEbAsariReproductionAddon] Exception in PregnancyChancePostfix: " + ex);
            }
        }
    }
}
