using System;
using HarmonyLib;
using Verse;

namespace StatsAdjustments
{
    // Shared "report the effective age" switch. While the depth counter is above zero, the biological-age
    // getters below return a pawn's effective (gene-adjusted) age instead of its real one. Any check that
    // should run on effective age - a stat curve, a childbirth ritual, an aging-disease roll - wraps the
    // vanilla method with Push()/Pop(), always restoring the depth from a Harmony Finalizer so it is safe
    // even if the wrapped method throws. The counter (rather than a bool) keeps nested wraps correct, and
    // it is thread-static so background pawn generation can never leak the flag onto the main thread.
    public static class AgeSpoofer
    {
        [ThreadStatic] private static int depth;

        public static bool Active => depth > 0;
        public static void Push() => depth++;
        public static void Pop() { if (depth > 0) depth--; }
    }

    // Exact biological age in float form. Read by stat curves, fertility-free age checks and the ritual.
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYearsFloat), MethodType.Getter)]
    public static class Patch_Pawn_AgeTracker_AgeBiologicalYearsFloat
    {
        static void Postfix(ref float __result, Pawn ___pawn)
        {
            if (!AgeSpoofer.Active || ___pawn == null) return;
            float factor = LifespanUtility.GetFactor(___pawn);
            if (!LifespanUtility.HasEffect(factor)) return;
            __result = LifespanUtility.GetEffectiveAge(__result, factor, LifespanUtility.GetLifeExpectancy(___pawn));
        }
    }

    // Whole-year biological age. Read by StatPart_Age.AgeMultiplier and by the ritual quality display.
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYears), MethodType.Getter)]
    public static class Patch_Pawn_AgeTracker_AgeBiologicalYears
    {
        static void Postfix(ref int __result, Pawn ___pawn)
        {
            if (!AgeSpoofer.Active || ___pawn == null) return;
            float factor = LifespanUtility.GetFactor(___pawn);
            if (!LifespanUtility.HasEffect(factor)) return;
            __result = (int)LifespanUtility.GetEffectiveAge(__result, factor, LifespanUtility.GetLifeExpectancy(___pawn));
        }
    }
}
