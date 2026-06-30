using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    // ---- Old-age mortality & morbidity ----------------------------------------------------------------
    // Vanilla decides when a pawn falls apart from old age by comparing its biological age against its
    // race's life expectancy. Feeding these checks the effective (gene-adjusted) age is what finally makes
    // the lifespan genes actually shorten or lengthen a life, instead of only tweaking stats and fertility.

    // Lethal, ongoing age diseases (heart attack, carcinoma growth, ...). They fire on a mean-time-between
    // basis from HediffGiver_RandomAgeCurved, which reads the pawn's biological age internally, so we just
    // gate the read: a long-lived pawn rolls these far later, a short-lived one far sooner.
    [HarmonyPatch(typeof(HediffGiver_RandomAgeCurved), nameof(HediffGiver_RandomAgeCurved.OnIntervalPassed))]
    public static class Patch_HediffGiver_RandomAgeCurved_OnIntervalPassed
    {
        static void Prefix() => AgeSpoofer.Push();
        static void Finalizer() => AgeSpoofer.Pop();
    }

    // Chronic age conditions gained on birthdays (cataracts, bad back, frailty, dementia, hearing loss).
    // Their chance curve is keyed on the age passed in, so we hand it the effective age. The same method is
    // used both for ongoing birthdays and when a pawn is generated with a past, so a freshly spawned
    // long-lived pawn is no longer riddled with old-age injuries it should not have yet.
    [HarmonyPatch(typeof(AgeInjuryUtility), nameof(AgeInjuryUtility.RandomHediffsToGainOnBirthday), new[] { typeof(Pawn), typeof(float) })]
    public static class Patch_AgeInjuryUtility_RandomHediffsToGainOnBirthday
    {
        static void Prefix(Pawn pawn, ref float age)
        {
            float factor = LifespanUtility.GetFactor(pawn);
            if (LifespanUtility.HasEffect(factor))
                age = LifespanUtility.GetEffectiveAge(age, factor, LifespanUtility.GetLifeExpectancy(pawn));
        }
    }

    // Generation-time severity simulation for the birthday hediffs above. It reads the pawn's current age
    // (gated) and also takes the age the hediff was "gained at" (scaled), so the simulated progression
    // spans the effective, not the real, number of years and the two stay consistent with each other.
    [HarmonyPatch(typeof(HediffGiver_Birthday), nameof(HediffGiver_Birthday.TryApplyAndSimulateSeverityChange))]
    public static class Patch_HediffGiver_Birthday_TryApplyAndSimulateSeverityChange
    {
        static void Prefix(Pawn pawn, ref float gotAtAge)
        {
            AgeSpoofer.Push();
            float factor = LifespanUtility.GetFactor(pawn);
            if (LifespanUtility.HasEffect(factor))
                gotAtAge = LifespanUtility.GetEffectiveAge(gotAtAge, factor, LifespanUtility.GetLifeExpectancy(pawn));
        }

        static void Finalizer() => AgeSpoofer.Pop();
    }
}
