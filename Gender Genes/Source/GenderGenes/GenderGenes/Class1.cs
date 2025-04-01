using HarmonyLib;
using RimWorld;
using System;
using System.Reflection;
using Verse;

namespace GenderGenes
{
    [StaticConstructorOnStartup]
    public static class GenderSpecificGenePatch
    {
        static GenderSpecificGenePatch()
        {
            new Harmony("com.bobniktheiii.rimworldmod.gendergenes").Patch((MethodBase)AccessTools.Method(typeof(PawnGenerator), "GeneratePawn", new Type[1]
            {
        typeof(PawnGenerationRequest)
            }), postfix: new HarmonyMethod(typeof(GenderSpecificGenePatch), "Postfix"));
        }

        public static void Postfix(ref Pawn __result)
        {
            if (__result?.genes == null || __result?.story == null || __result?.ageTracker == null)
                return;

            bool flag = false;

            // Check for gender-specific genes
            if (GenCollection.Any<Gene>(__result.genes.GenesListForReading, g => ((Def)g.def).defName == "FemaleOnly"))
            {
                if (__result.gender != Gender.Female)
                {
                    __result.gender = Gender.Female;
                    flag = true;
                }
            }
            else if (GenCollection.Any<Gene>(__result.genes.GenesListForReading, g => ((Def)g.def).defName == "MaleOnly"))
            {
                if (__result.gender != Gender.Male)
                {
                    __result.gender = Gender.Male;
                    flag = true;
                }
            }

            if (flag)
            {
                // Apply appropriate body type, skipping non-adult pawns
                if (IsAdult(__result))
                {
                    __result.story.bodyType = GetBodyTypeForGender(__result.gender);
                }

                // Regenerate name after gender change
                __result.Name = PawnBioAndNameGenerator.GeneratePawnName(__result, NameStyle.Full, null, false, null);
            }
        }

        private static BodyTypeDef GetBodyTypeForGender(Gender gender)
        {
            if (gender == Gender.Male)
                return BodyTypeDefOf.Male;
            else if (gender == Gender.Female)
                return BodyTypeDefOf.Female;
            else
                return BodyTypeDefOf.Thin;
        }

        private static bool IsAdult(Pawn pawn)
        {
            // Check if the current life stage is adult
            return pawn.ageTracker.CurLifeStage.reproductive; // 'reproductive' indicates adulthood in RimWorld
        }
    }
}
