using RimWorld;
using Verse;
using HarmonyLib;

namespace AsariAgeGenerator
{
    [StaticConstructorOnStartup]
    public static class AsariAgeGenerator
    {
        static AsariAgeGenerator()
        {
            var harmony = new Harmony("com.bobniktheiii.asari.agegenerator");
            harmony.Patch(
                original: AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new[] { typeof(PawnGenerationRequest) }),
                postfix: new HarmonyMethod(typeof(GeneratePawnPatch), nameof(GeneratePawnPatch.Postfix))
            );
        }

        public static class GeneratePawnPatch
        {
            public static void Postfix(Pawn __result, PawnGenerationRequest request)
            {
                if (__result == null || __result.genes == null)
                    return;

                XenotypeDef asariXenotype = DefDatabase<XenotypeDef>.GetNamed("Asari", errorOnFail: false);
                if (asariXenotype == null)
                {
                    //Log.Warning("Asari xenotype not found in the database.");
                    return;
                }

                if (__result.RaceProps.Humanlike && __result.genes.Xenotype == asariXenotype)
                {
                    //Log.Message($"Asari pawn detected: {__result.Name?.ToStringShort ?? "Unnamed"}");

                    long ageTicks;

                    if (request.AllowedDevelopmentalStages.HasFlag(DevelopmentalStage.Baby))
                    {
                        ageTicks = (long)(Rand.RangeInclusive(0, 2) * 3600000f);
                    }
                    else if (request.AllowedDevelopmentalStages.HasFlag(DevelopmentalStage.Child))
                    {
                        ageTicks = (long)(Rand.RangeInclusive(3, 12) * 3600000f);
                    }
                    else
                    {
                        ageTicks = (long)(Rand.RangeInclusive(18, 1000) * 3600000f);
                    }
                    // Set biological age
                    long biologicalAgeTicks = ageTicks;
                    __result.ageTracker.AgeBiologicalTicks = biologicalAgeTicks;

                    // Set chronological age to match biological age
                    __result.ageTracker.AgeChronologicalTicks = biologicalAgeTicks + (long)(Rand.RangeInclusive(0, 1000) * 3600000f);
                }
            }

        }
    }
}
