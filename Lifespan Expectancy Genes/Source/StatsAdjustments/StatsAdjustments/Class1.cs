using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    // Optional XML extension to tag genes with a lifespan multiplier
    public class GeneExtension_Lifespan : DefModExtension
    {
        public float lifespanFactor = 1f;
    }

    // Optional helper for reading lifespanFactor from gene instance
    public class Gene_LifespanModifier : Gene
    {
        public float LifespanFactor
        {
            get
            {
                var ext = def?.GetModExtension<GeneExtension_Lifespan>();
                return ext?.lifespanFactor ?? 1f;
            }
        }
    }

    [StaticConstructorOnStartup]
    public static class StatsAdjustments_Mod
    {
        public static bool debugLogging = false;

        static StatsAdjustments_Mod()
        {
            try
            {
                var harmony = new Harmony("com.bobniktheiii.rimworld.statsadjustments");
                harmony.PatchAll();
                if (debugLogging)
                    Log.Message("[StatsAdjustments] Harmony patches applied.");
            }
            catch (Exception ex)
            {
                Log.Error("[StatsAdjustments] Failed to apply Harmony patches:\n" + ex);
            }
        }
    }

    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    public static class Patch_StatPart_FertilityByGenderAge_AgeFactor
    {
        static readonly HashSet<int> loggedPawnIDs = new HashSet<int>();

        static bool Prefix(StatPart_FertilityByGenderAge __instance, Pawn pawn, ref float __result)
        {
            if (pawn == null || pawn.ageTracker == null)
                return true;

            bool doLog = StatsAdjustments_Mod.debugLogging && loggedPawnIDs.Add(pawn.thingIDNumber);

            if (doLog)
                Log.Message($"[StatsAdjustments] AgeFactor patch for {pawn.LabelShort} (ID {pawn.thingIDNumber})");

            // Retrieve correct curve
            SimpleCurve curve = null;
            try
            {
                var field = pawn.gender == Gender.Male
                    ? AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "maleFertilityAgeFactor")
                    : AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "femaleFertilityAgeFactor");

                if (field == null)
                {
                    if (doLog) Log.Warning("[StatsAdjustments] Curve field not found.");
                    return true;
                }

                curve = field.GetValue(__instance) as SimpleCurve;
                if (curve == null)
                {
                    if (doLog) Log.Warning("[StatsAdjustments] Curve is null.");
                    return true;
                }

                if (doLog) Log.Message($"[StatsAdjustments] Retrieved curve with {curve.Points.Count} points.");
            }
            catch (Exception ex)
            {
                if (doLog) Log.Error("[StatsAdjustments] Reflection error: " + ex);
                return true;
            }

            // Calculate total lifespan factor from all genes
            float lifespanFactor = 1f;
            if (pawn.genes?.GenesListForReading != null)
            {
                foreach (var gene in pawn.genes.GenesListForReading)
                {
                    var ext = gene.def?.GetModExtension<GeneExtension_Lifespan>();
                    if (ext != null)
                    {
                        lifespanFactor *= ext.lifespanFactor;
                        if (doLog)
                            Log.Message($"[StatsAdjustments] Gene {gene.def.defName} → lifespanFactor *= {ext.lifespanFactor} → {lifespanFactor}");
                    }
                }
            }

            if (lifespanFactor == 1f)
                return true;

            float declineFactor = lifespanFactor > 2f
                ? lifespanFactor * 2f
                : lifespanFactor > 0f ? lifespanFactor * lifespanFactor : 0f;

            if (doLog)
                Log.Message($"[StatsAdjustments] Effective declineFactor = {declineFactor}");

            var sorted = curve.Points.OrderBy(p => p.x).ToList();
            var adjusted = new SimpleCurve();

            int firstFullIndex = sorted.FindIndex(p => p.y >= 1f);
            if (firstFullIndex < 0)
                return true;

            float pubertyStart = sorted[firstFullIndex].x;

            foreach (var pt in sorted)
            {
                if (pt.x <= pubertyStart)
                {
                    adjusted.Add(new CurvePoint(pt.x, pt.y));
                    if (doLog)
                        Log.Message($"[StatsAdjustments] Keep point (x={pt.x}, y={pt.y})");
                }
                else
                {
                    float newX = pt.x * declineFactor;
                    adjusted.Add(new CurvePoint(newX, pt.y));
                    if (doLog)
                        Log.Message($"[StatsAdjustments] Adjust point: {pt.x} → {newX} (y={pt.y})");
                }
            }

            try
            {
                float age = pawn.ageTracker.AgeBiologicalYearsFloat;
                float factor = adjusted.Evaluate(age);
                __result = factor;

                if (doLog)
                    Log.Message($"[StatsAdjustments] Age = {age}, Fertility factor = {factor}");

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[StatsAdjustments] Error evaluating curve: " + ex);
                return true;
            }
        }
    }
}
