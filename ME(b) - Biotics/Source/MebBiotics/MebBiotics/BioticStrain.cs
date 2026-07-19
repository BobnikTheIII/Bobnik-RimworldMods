using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MebBiotics
{
    // ============================================================================================
    // Ampless biotic strain.
    //
    // Design: the Eezo gene unlocks the biotic paths, the Biotic Amp makes casting *safe*. A
    // biotic who casts without an installed amp channels unfocused mass effect fields through
    // bare nerve tissue, accumulating "biotic strain" (pain, then dulled consciousness). The
    // strain decays on its own over a few in-game hours.
    //
    // Implementation: a single Harmony postfix on VEF's Ability.Cast - the one choke point every
    // VEF ability (Blank, Explode, Spawn, our custom classes, VPE's Killskip...) runs through,
    // because subclasses all call base.Cast. The postfix is strictly gated to abilities whose
    // AbilityExtension_Psycast points at one of OUR three paths, so ordinary psycasts (vanilla or
    // VPE) are never affected - exactly the constraint from the design notes.
    // ============================================================================================
    [StaticConstructorOnStartup]
    public static class MebBiotics_Startup
    {
        private static bool Verbose
        {
            get { return Prefs.DevMode; }
        }

        static MebBiotics_Startup()
        {
            try
            {
                Harmony harmony = new Harmony("com.bobniktheiii.mebbiotics");
                System.Reflection.MethodInfo target = AccessTools.Method(typeof(VEF.Abilities.Ability), "Cast");
                System.Reflection.MethodInfo postfix = AccessTools.Method(typeof(BioticStrainPatch), "Postfix");
                if (target == null || postfix == null)
                {
                    Log.Warning("[MebBiotics] Could not resolve Ability.Cast - ampless strain disabled.");
                    return;
                }
                harmony.Patch(target, null, new HarmonyMethod(postfix));
                if (Verbose)
                {
                    Log.Message("[MebBiotics] Ampless-strain patch applied.");
                }
            }
            catch (Exception e)
            {
                Log.Error("[MebBiotics] Startup patching failed: " + e);
            }
        }
    }

    public static class BioticStrainPatch
    {
        private const float SeverityPerCast = 0.34f;

        public static void Postfix(VEF.Abilities.Ability __instance)
        {
            try
            {
                if (__instance == null || __instance.pawn == null || __instance.def == null)
                {
                    return;
                }
                VanillaPsycastsExpanded.AbilityExtension_Psycast ext =
                    __instance.def.GetModExtension<VanillaPsycastsExpanded.AbilityExtension_Psycast>();
                if (ext == null || ext.path == null)
                {
                    return;
                }
                string path = ext.path.defName;
                if (path != "VPE_MebAdept" && path != "VPE_MebVanguard" && path != "VPE_MebSentinel")
                {
                    return;
                }
                Pawn caster = __instance.pawn;
                if (caster.Dead || caster.health == null)
                {
                    return;
                }
                HediffDef ampDef = DefDatabase<HediffDef>.GetNamed("MebBiotic_AmpImplant", false);
                if (ampDef != null && caster.health.hediffSet.GetFirstHediffOfDef(ampDef) != null)
                {
                    return;
                }
                HediffDef strainDef = DefDatabase<HediffDef>.GetNamed("MebBiotic_Strain", false);
                if (strainDef == null)
                {
                    return;
                }
                Hediff existing = caster.health.hediffSet.GetFirstHediffOfDef(strainDef);
                if (existing != null)
                {
                    existing.Severity += SeverityPerCast;
                }
                else
                {
                    Hediff strain = HediffMaker.MakeHediff(strainDef, caster);
                    strain.Severity = SeverityPerCast;
                    caster.health.AddHediff(strain);
                }
            }
            catch (Exception e)
            {
                if (Prefs.DevMode)
                {
                    Log.Warning("[MebBiotics] Strain postfix error: " + e);
                }
            }
        }
    }
}
