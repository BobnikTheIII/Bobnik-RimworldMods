using HarmonyLib;
using RimWorld;

namespace StatsAdjustments
{
    // The childbirth ritual scores its outcome partly on the mother's age, and we want that judged on her
    // effective (gene-adjusted) age. Wrapping the comp's age-reading methods with the spoof switch makes
    // each read return the effective age. Count drives the actual outcome mechanics, GetDesc drives the
    // post-birth letter text, and GetQualityFactor fills the value shown in the ritual dialog - that last
    // one was previously left out, which is why the mechanics used the adjusted age but the window kept
    // showing the unadjusted, standard one. All three are wrapped now so the display matches the mechanics.
    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), "Count")]
    public static class Patch_RitualOutcomeComp_PawnAge_Count
    {
        static void Prefix() => AgeSpoofer.Push();
        static void Finalizer() => AgeSpoofer.Pop();
    }

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), "GetQualityFactor")]
    public static class Patch_RitualOutcomeComp_PawnAge_GetQualityFactor
    {
        static void Prefix() => AgeSpoofer.Push();
        static void Finalizer() => AgeSpoofer.Pop();
    }

    [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), "GetDesc")]
    public static class Patch_RitualOutcomeComp_PawnAge_GetDesc
    {
        static void Prefix() => AgeSpoofer.Push();
        static void Finalizer() => AgeSpoofer.Pop();
    }
}
