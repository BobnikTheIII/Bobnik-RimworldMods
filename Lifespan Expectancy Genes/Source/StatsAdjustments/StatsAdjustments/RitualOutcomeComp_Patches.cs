using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    public static class AgeSpoofer
    {
        // Flag used during the actual childbirth moment (in the background)
        public static bool isRitualActive = false;

        // Ultra-fast scanner: checks if the player is currently looking at the Ritual window.
        // Runs in a fraction of a nanosecond, ensuring 0% FPS impact!
        public static bool IsRitualWindowOpen()
        {
            if (Current.ProgramState != ProgramState.Playing) return false;

            WindowStack stack = Find.WindowStack;
            if (stack == null) return false;

            IList<Window> windows = stack.Windows;
            for (int i = 0; i < windows.Count; i++)
            {
                // Returns true if the "Gather for birth" window is on the screen
                if (windows[i] is Dialog_BeginRitual)
                {
                    return true;
                }
            }
            return false;
        }
    }

    // 1. OPTIMIZED AGE INTERCEPTOR (Exact age in floats)
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYearsFloat), MethodType.Getter)]
    public static class Patch_Pawn_AgeTracker_AgeBiologicalYearsFloat
    {
        static void Postfix(Pawn_AgeTracker __instance, ref float __result, Pawn ___pawn)
        {
            if (___pawn == null) return;

            // INSTANT ABORT: If there is no active ritual and the window is not open, 
            // the script exits immediately to prevent lag!
            if (!AgeSpoofer.isRitualActive && !AgeSpoofer.IsRitualWindowOpen()) return;

            float lifespanFactor = 1f;
            if (___pawn.genes?.GenesListForReading != null)
            {
                foreach (var gene in ___pawn.genes.GenesListForReading)
                {
                    var ext = gene.def?.GetModExtension<GeneExtension_Lifespan>();
                    if (ext != null) lifespanFactor *= ext.lifespanFactor;
                }
            }

            if (lifespanFactor <= 1f) return;
            __result = AgeCalculator.GetEffectiveAge(__result, lifespanFactor);
        }
    }

    // 2. OPTIMIZED AGE INTERCEPTOR
    [HarmonyPatch(typeof(Pawn_AgeTracker), nameof(Pawn_AgeTracker.AgeBiologicalYears), MethodType.Getter)]
    public static class Patch_Pawn_AgeTracker_AgeBiologicalYears
    {
        static void Postfix(Pawn_AgeTracker __instance, ref int __result, Pawn ___pawn)
        {
            if (___pawn == null) return;

            // Instant abort for maximum performance
            if (!AgeSpoofer.isRitualActive && !AgeSpoofer.IsRitualWindowOpen()) return;

            float lifespanFactor = 1f;
            if (___pawn.genes?.GenesListForReading != null)
            {
                foreach (var gene in ___pawn.genes.GenesListForReading)
                {
                    var ext = gene.def?.GetModExtension<GeneExtension_Lifespan>();
                    if (ext != null) lifespanFactor *= ext.lifespanFactor;
                }
            }

            if (lifespanFactor <= 1f) return;
            __result = (int)AgeCalculator.GetEffectiveAge(__result, lifespanFactor);
        }
    }

    // 3. MECHANICS TOGGLE (Secures the childbirth moment when the window is already closed)
    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), "Count")]
    public static class Patch_RitualOutcomeComp_PawnAge_Count
    {
        static void Prefix() => AgeSpoofer.isRitualActive = true;
        static void Postfix() => AgeSpoofer.isRitualActive = false;
    }

    // 4. LETTER TOGGLE (Secures the content of the message sent to the player after birth)
    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), "GetDesc")]
    public static class Patch_RitualOutcomeComp_PawnAge_GetDesc
    {
        static void Prefix() => AgeSpoofer.isRitualActive = true;
        static void Postfix() => AgeSpoofer.isRitualActive = false;
    }
}