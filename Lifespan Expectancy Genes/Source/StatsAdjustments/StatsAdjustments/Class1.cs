using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    // Optional XML extension to tag genes with a lifespan multiplier.
    // factor > 1 = longer life (slower aging), factor < 1 = shorter life (faster aging).
    public class GeneExtension_Lifespan : DefModExtension
    {
        public float lifespanFactor = 1f;
    }

    // Single source of truth for the whole mod: how strong a pawn's lifespan genes are, and how that
    // turns a real biological age into an "effective" human-equivalent age. Stats, fertility and
    // mortality all read from here so they stay consistent with one another.
    public static class LifespanUtility
    {
        // Below this biological age aging is left untouched, so childhood/adolescence stays standard
        // regardless of the gene. Only adult aging is stretched (long-lived) or compressed (short-lived).
        // Set to 18 (RimWorld's adulthood / full-work-speed threshold): keeping it here guarantees a
        // gene-bearing adult never maps to an effective age below 18, which would otherwise drop them onto
        // vanilla's child-labor stat curves (e.g. the global work speed ramp that only reaches 100% at 18).
        public const float ChildhoodCutoff = 18f;

        // Life-expectancy fallback (human) for races that do not declare a usable one.
        public const float FallbackLifeExpectancy = 80f;

        private const float Epsilon = 0.0001f;

        // Combined lifespan multiplier from every lifespan gene the pawn carries (1 = no effect).
        public static float GetFactor(Pawn pawn)
        {
            List<Gene> genes = pawn?.genes?.GenesListForReading;
            if (genes == null) return 1f;

            float factor = 1f;
            for (int i = 0; i < genes.Count; i++)
            {
                GeneExtension_Lifespan ext = genes[i].def?.GetModExtension<GeneExtension_Lifespan>();
                if (ext != null) factor *= ext.lifespanFactor;
            }
            return factor;
        }

        // True when the factor actually changes anything (not ~1 and not a degenerate value).
        public static bool HasEffect(float factor) => factor > 0f && Math.Abs(factor - 1f) > Epsilon;

        // A pawn's natural life expectancy (its race's), which the multiplier scales.
        public static float GetLifeExpectancy(Pawn pawn)
        {
            float life = pawn?.RaceProps?.lifeExpectancy ?? FallbackLifeExpectancy;
            return life > ChildhoodCutoff ? life : FallbackLifeExpectancy;
        }

        // Linear remap that makes the multiplier read directly as a lifespan multiplier: a pawn reaches its
        // natural mortality age (its life expectancy) at factor * lifeExpectancy real years, while childhood
        // (<= cutoff) is left standard. With the human 80-year baseline that means x0.5 -> dies ~40, x0.75 ->
        // ~60, x1.25 -> ~100, x1.5 -> ~120, x10 -> ~800. The slope is solved from those two anchor points
        // (cutoff and target death age); because the unscaled childhood years are "free", the real aging rate
        // ends up slightly steeper than the raw factor, which is exactly what lands the death age on the mark.
        public static float GetEffectiveAge(float realAge, float factor, float lifeExpectancy)
        {
            if (!HasEffect(factor)) return realAge;
            if (realAge <= ChildhoodCutoff) return realAge;

            if (lifeExpectancy <= ChildhoodCutoff) lifeExpectancy = FallbackLifeExpectancy;
            float targetDeathAge = factor * lifeExpectancy;
            if (targetDeathAge <= ChildhoodCutoff + 1f) targetDeathAge = ChildhoodCutoff + 1f; // guard tiny factors

            float slope = (lifeExpectancy - ChildhoodCutoff) / (targetDeathAge - ChildhoodCutoff);
            return ChildhoodCutoff + (realAge - ChildhoodCutoff) * slope;
        }

        public static float GetEffectiveAge(Pawn pawn, float realAge)
            => GetEffectiveAge(realAge, GetFactor(pawn), GetLifeExpectancy(pawn));
    }

    [StaticConstructorOnStartup]
    public static class StatsAdjustments_Mod
    {
        public static bool debugLogging = false;

        static StatsAdjustments_Mod()
        {
            var harmony = new Harmony("com.bobniktheiii.rimworld.statsadjustments");
            int applied = 0, skipped = 0;

            // Patch each annotated class on its own instead of PatchAll(), so that if a single target
            // method differs on another supported RimWorld version (1.5 vs 1.6) only that one patch is
            // skipped rather than the whole mod failing to load.
            foreach (var type in typeof(StatsAdjustments_Mod).Assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
                    continue;

                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    applied++;
                }
                catch (Exception ex)
                {
                    skipped++;
                    Log.Warning($"[LifespanGenes] Skipped patch {type.Name} (incompatible with this RimWorld version?): {ex.Message}");
                }
            }

            if (debugLogging || skipped > 0)
                Log.Message($"[LifespanGenes] Harmony patches applied: {applied}, skipped: {skipped}.");
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
                Log.Message($"[LifespanGenes] AgeFactor patch for {pawn.LabelShort} (ID {pawn.thingIDNumber})");

            // Calculate total lifespan factor from all genes
            float lifespanFactor = LifespanUtility.GetFactor(pawn);
            if (!LifespanUtility.HasEffect(lifespanFactor))
                return true;

            // Retrieve correct curve
            SimpleCurve curve = null;
            try
            {
                var field = pawn.gender == Gender.Male
                    ? AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "maleFertilityAgeFactor")
                    : AccessTools.Field(typeof(StatPart_FertilityByGenderAge), "femaleFertilityAgeFactor");

                if (field == null)
                {
                    if (doLog) Log.Warning("[LifespanGenes] Curve field not found.");
                    return true;
                }

                curve = field.GetValue(__instance) as SimpleCurve;
                if (curve == null)
                {
                    if (doLog) Log.Warning("[LifespanGenes] Curve is null.");
                    return true;
                }

                if (doLog) Log.Message($"[LifespanGenes] Retrieved curve with {curve.Points.Count} points.");
            }
            catch (Exception ex)
            {
                if (doLog) Log.Error("[LifespanGenes] Reflection error: " + ex);
                return true;
            }

            float declineFactor = lifespanFactor > 2f
                ? lifespanFactor * 2f
                : lifespanFactor > 0f ? lifespanFactor * lifespanFactor : 0f;

            if (doLog)
                Log.Message($"[LifespanGenes] Effective declineFactor = {declineFactor}");

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
                        Log.Message($"[LifespanGenes] Keep point (x={pt.x}, y={pt.y})");
                }
                else
                {
                    float newX = pt.x * declineFactor;
                    adjusted.Add(new CurvePoint(newX, pt.y));
                    if (doLog)
                        Log.Message($"[LifespanGenes] Adjust point: {pt.x} → {newX} (y={pt.y})");
                }
            }

            try
            {
                float age = pawn.ageTracker.AgeBiologicalYearsFloat;
                float factor = adjusted.Evaluate(age);
                __result = factor;

                if (doLog)
                    Log.Message($"[LifespanGenes] Age = {age}, Fertility factor = {factor}");

                return false;
            }
            catch (Exception ex)
            {
                Log.Warning("[LifespanGenes] Error evaluating curve: " + ex);
                return true;
            }
        }
    }
}
