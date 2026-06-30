using HarmonyLib;
using RimWorld;

namespace StatsAdjustments
{
    // All of vanilla's age-based stat scaling (immunity gain speed, global work speed, hearing, etc.)
    // funnels through StatPart_Age.AgeMultiplier, which reads the pawn's biological age and evaluates the
    // stat's age curve. We let vanilla do its normal work but feed it the effective (gene-adjusted) age:
    // a long-lived pawn keeps young-adult stats far longer, a short-lived pawn ages out of them faster.
    //
    // Both TransformValue (the real stat) and ExplanationPart (the tooltip breakdown) call AgeMultiplier,
    // so gating this single method fixes value and tooltip together. ExplanationPart reads the *displayed*
    // age separately, outside AgeMultiplier, so the tooltip still shows the pawn's real age next to the
    // effective-age multiplier - exactly what the old hand-written explanation tried to do, but for free.
    [HarmonyPatch(typeof(StatPart_Age), "AgeMultiplier")]
    public static class Patch_StatPart_Age_AgeMultiplier
    {
        static void Prefix() => AgeSpoofer.Push();
        static void Finalizer() => AgeSpoofer.Pop();
    }
}
