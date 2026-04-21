using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    // Central age calculator for the entire mod
    public static class AgeCalculator
    {
        public static float GetEffectiveAge(float realAge, float lifespanFactor)
        {
            if (lifespanFactor <= 1f) return realAge;
            if (realAge <= 20f) return realAge; // Normal childhood phase

            // Asari life stage turning points (for factor=10 it's 350 and 700 years)
            float matronStart = 35f * lifespanFactor;
            float matriarchStart = 70f * lifespanFactor;

            // MAIDEN STAGE (from 20 to ~350): Prime vitality (equivalent to 20-30 years old)
            if (realAge <= matronStart)
            {
                float stageLength = matronStart - 20f;
                return 20f + ((realAge - 20f) / stageLength) * 10f;
            }

            // MATRON STAGE (from ~350 to ~700): Settling down (equivalent to 30-40 years old)
            if (realAge <= matriarchStart)
            {
                float stageLength = matriarchStart - matronStart;
                return 30f + ((realAge - matronStart) / stageLength) * 10f;
            }

            // MATRIARCH STAGE (above ~700): Supreme wisdom (equivalent to 40-50 years old)
            float finalStageLength = (80f * lifespanFactor) - matriarchStart;
            if (finalStageLength <= 0) finalStageLength = 10f * lifespanFactor;

            float calcAge = 40f + ((realAge - matriarchStart) / finalStageLength) * 10f;

            // Hard cap at 50 human years - keeps Immunity and Global Work Speed perfectly safe
            return Math.Min(calcAge, 50f);
        }
    }

    [HarmonyPatch(typeof(StatPart_Age), nameof(StatPart_Age.TransformValue))]
    public static class Patch_StatPart_Age_TransformValue
    {
        static bool Prefix(StatPart_Age __instance, StatRequest req, ref float val)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.ageTracker == null)
                return true;

            float lifespanFactor = 1f;
            if (pawn.genes?.GenesListForReading != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var ext = gene.def?.GetModExtension<GeneExtension_Lifespan>();
                    if (ext != null) lifespanFactor *= ext.lifespanFactor;
                }
            }

            if (lifespanFactor <= 1f) return true;

            // Base effective human age
            float effectiveAge = AgeCalculator.GetEffectiveAge(pawn.ageTracker.AgeBiologicalYearsFloat, lifespanFactor);

            FieldInfo curveField = AccessTools.Field(typeof(StatPart_Age), "curve")
                                ?? AccessTools.GetDeclaredFields(typeof(StatPart_Age)).FirstOrDefault(f => f.FieldType == typeof(SimpleCurve));

            SimpleCurve curve = curveField?.GetValue(__instance) as SimpleCurve;

            if (curve != null)
            {
                float evalAge = effectiveAge;

                if (curve.Points.Count > 0)
                {
                    float maxX = curve.Points[curve.Points.Count - 1].x;

                    // SMART CURVE DETECTION:
                    // If the curve ends at a small number (e.g., 1.0 or 2.0), it expects normalized life expectancy fraction.
                    // If it ends higher (e.g., 13 for Work Speed), it expects absolute biological years.
                    if (maxX <= 5f && pawn.def?.race != null && pawn.def.race.lifeExpectancy > 0)
                    {
                        evalAge /= pawn.def.race.lifeExpectancy;
                    }
                }

                val *= curve.Evaluate(evalAge);
                return false;
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(StatPart_Age), nameof(StatPart_Age.ExplanationPart))]
    public static class Patch_StatPart_Age_ExplanationPart
    {
        static bool Prefix(StatPart_Age __instance, StatRequest req, ref string __result)
        {
            if (!req.HasThing || !(req.Thing is Pawn pawn) || pawn.ageTracker == null)
                return true;

            float lifespanFactor = 1f;
            if (pawn.genes?.GenesListForReading != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var ext = gene.def?.GetModExtension<GeneExtension_Lifespan>();
                    if (ext != null) lifespanFactor *= ext.lifespanFactor;
                }
            }

            if (lifespanFactor <= 1f) return true;

            float effectiveAge = AgeCalculator.GetEffectiveAge(pawn.ageTracker.AgeBiologicalYearsFloat, lifespanFactor);

            FieldInfo curveField = AccessTools.Field(typeof(StatPart_Age), "curve")
                                ?? AccessTools.GetDeclaredFields(typeof(StatPart_Age)).FirstOrDefault(f => f.FieldType == typeof(SimpleCurve));

            SimpleCurve curve = curveField?.GetValue(__instance) as SimpleCurve;

            if (curve != null)
            {
                float evalAge = effectiveAge;

                if (curve.Points.Count > 0)
                {
                    float maxX = curve.Points[curve.Points.Count - 1].x;

                    if (maxX <= 5f && pawn.def?.race != null && pawn.def.race.lifeExpectancy > 0)
                    {
                        evalAge /= pawn.def.race.lifeExpectancy;
                    }
                }

                float multiplier = curve.Evaluate(evalAge);

                string statName = __instance.parentStat?.defName ?? "UnknownStat";

                if (StatsAdjustments_Mod.debugLogging)
                {
                    Log.Message($"[LifespanGenes] {statName} | {pawn.LabelShort}, Evaluated Age: {evalAge:F2}, Multiplier: {multiplier}");
                }

                if (Math.Abs(multiplier - 1f) < 0.0001f)
                {
                    __result = null;
                }
                else
                {
                    __result = "StatsReport_AgeMultiplier".Translate(pawn.ageTracker.AgeBiologicalYearsFloat.ToString("F0")) + ": x" + multiplier.ToStringPercent();
                }
                return false;
            }

            return true;
        }
    }
}