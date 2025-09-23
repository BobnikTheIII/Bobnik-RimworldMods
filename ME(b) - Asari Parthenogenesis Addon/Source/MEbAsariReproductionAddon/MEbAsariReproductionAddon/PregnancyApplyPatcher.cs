using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace MEbAsariReproductionAddon
{
    public static class PregnancyApplyPatcher
    {
        // Helper to only emit verbose messages when Dev Mode is enabled
        private static bool Verbose => Prefs.DevMode;

        // Call from startup with your Harmony instance
        public static void PatchAllApplyMethods(Harmony harmony)
        {
            try
            {
                var thisAsm = Assembly.GetExecutingAssembly();

                // Search candidate methods:
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var candidates = assemblies
                    .SelectMany(a =>
                    {
                        try { return a.GetTypes(); } catch { return new Type[0]; }
                    })
                    .Where(t => (t.Namespace != null && t.Namespace.StartsWith("RimWorld")) // prefer RimWorld types
                                || (t.Assembly.GetName().Name.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0))
                    .SelectMany(t => t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                        .Where(m => m.ReturnType == typeof(bool)
                                    && m.GetParameters().Length >= 2
                                    && m.GetParameters()[0].ParameterType == typeof(Pawn)
                                    && m.GetParameters()[1].ParameterType == typeof(Pawn)
                                    && (m.Name.IndexOf("Pregn", StringComparison.OrdinalIgnoreCase) >= 0
                                        || m.Name.IndexOf("TryToAddPregnancy", StringComparison.OrdinalIgnoreCase) >= 0
                                        || m.Name.IndexOf("TryAddPregnancy", StringComparison.OrdinalIgnoreCase) >= 0
                                        || m.Name.IndexOf("TryGivePregnancy", StringComparison.OrdinalIgnoreCase) >= 0)))
                    .ToList();

                // If nothing found, as a fallback try LovePartnerRelationUtility specifically
                if (!candidates.Any())
                {
                    var loveType = AccessTools.TypeByName("RimWorld.LovePartnerRelationUtility") ?? AccessTools.TypeByName("LovePartnerRelationUtility");
                    if (loveType != null)
                    {
                        var methods = loveType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                            .Where(m => m.ReturnType == typeof(bool)
                                        && m.GetParameters().Length >= 2
                                        && m.GetParameters()[0].ParameterType == typeof(Pawn)
                                        && m.GetParameters()[1].ParameterType == typeof(Pawn))
                            .ToList();
                        candidates.AddRange(methods);
                    }
                }

                // Remove methods declared in our own assembly
                candidates = candidates.Where(m => m.DeclaringType.Assembly != thisAsm).Distinct().ToList();

                if (!candidates.Any())
                {
                    if (Verbose)
                        Log.Message("[MEbAsariReproductionAddon] No pregnancy-apply candidate methods found to patch.");
                    return;
                }

                foreach (var method in candidates)
                {
                    try
                    {
                        if (Verbose)
                            Log.Message($"[MEbAsariReproductionAddon] Found candidate to patch: {method.DeclaringType.FullName}.{method.Name} (params: {string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))})");

                        var prefix = new HarmonyMethod(typeof(PregnancyApplyPatcher).GetMethod(nameof(PregnancyApplyPatcher.Prefix_ApplyPregnancy), BindingFlags.Static | BindingFlags.Public));
                        harmony.Patch(method, prefix);

                        if (Verbose)
                            Log.Message($"[MEbAsariReproductionAddon] Patched: {method.DeclaringType.FullName}.{method.Name}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[MEbAsariReproductionAddon] Failed to patch {method.DeclaringType.FullName}.{method.Name}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("[MEbAsariReproductionAddon] Exception in PatchAllApplyMethods: " + ex);
            }
        }

        // Generic prefix for methods that attempt to apply pregnancy.
        // Return false to skip the original and set __result to the outcome we determined.
        public static bool Prefix_ApplyPregnancy(object[] __args, ref bool __result)
        {
            try
            {
                if (__args == null || __args.Length < 2)
                    return true;

                Pawn woman = __args[0] as Pawn;
                Pawn man = __args[1] as Pawn;
                if (woman == null || man == null)
                    return true;

                // Guard: only in lovin' job context and same map
                bool womanDoingLovin = woman.jobs?.curJob?.def == JobDefOf.Lovin;
                bool manDoingLovin = man.jobs?.curJob?.def == JobDefOf.Lovin;
                bool sameMap = woman.Map != null && man.Map != null && woman.Map == man.Map;
                if (!(sameMap && (womanDoingLovin || manDoingLovin)))
                    return true;

                // Gene/gender checks
                bool womanHasGene = woman.genes?.HasActiveGene(DefDatabase<GeneDef>.GetNamed("MEbParthenogenesisGene")) == true;
                bool bothFemale = woman.gender == Gender.Female && man.gender == Gender.Female;
                if (!bothFemale || !womanHasGene)
                    return true;

                // If already pregnant, skip
                if (PregnancyUtility.GetPregnancyHediff(woman) != null)
                {
                    __result = false;
                    return false;
                }

                // If already queued, the QueuePregnancy call will handle duplicate logging/cooldown — but check quickly:
                if (PendingPregnancyManager.IsQueued(woman))
                {
                    __result = false;
                    return false;
                }

                // Life-stage/fertility checks
                if (!woman.ageTracker.CurLifeStage.reproductive)
                {
                    __result = false;
                    if (Verbose) Log.Message($"[MEbAsariReproductionAddon] {woman.LabelShort} cannot reproduce due to life stage.");
                    return false;
                }

                float fertility = Math.Max(0f, woman.GetStatValue(StatDefOf.Fertility, true));
                float chance = 0.25f * fertility;
                if (chance < 0.02f) chance = 0.02f;

                if (Verbose) Log.Message($"[MEbAsariReproductionAddon] Attempting parthenogenesis (queued) for {woman.LabelShort} with {man.LabelShort}. chance={chance:0.000}");

                if (Rand.Value <= chance)
                {
                    PendingPregnancyManager.QueuePregnancy(woman, man);
                    __result = true;
                }
                else
                {
                    __result = false;
                }

                // We decided the outcome; skip original method
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("[MEbAsariReproductionAddon] Exception in Prefix_ApplyPregnancy (queued + cooldown): " + ex);
                return true;
            }
        }
    }
}
