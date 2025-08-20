using HarmonyLib;
using RimWorld;
using System;
using System.Linq;
using Verse;

// Thanks to Hysteresis for great help with the development of this mod!


// Hello there fellow sourcecode dweller, these notes will help you understand what's what if you wanna know. Have a wonderful [specify time of day...]

namespace GenderGenes
{
    [StaticConstructorOnStartup]
    public static class GenderSpecificGenePatch
    {
        static GenderSpecificGenePatch()
        {
            var harmony = new Harmony("com.bobniktheiii.rimworldmod.gendergenes");

            // Patch PawnGenerator.GeneratePawn so newly generated pawns get correct gender/visuals
            var genPawn = AccessTools.Method(
                typeof(PawnGenerator),
                "GeneratePawn",
                new Type[] { typeof(PawnGenerationRequest) }
            );
            if (genPawn != null)
                harmony.Patch(genPawn,
                    postfix: new HarmonyMethod(typeof(GenderSpecificGenePatch), nameof(GeneratePawn_Postfix))
                );
            else
                Log.Error("[GenderGenes] Could not find PawnGenerator.GeneratePawn!");
        }

        /// <summary>
        /// Apply gender and visuals according to the custom genes.
        /// - Sets gender from gene
        /// - Preserves gender-neutral bodytypes (Thin/Fat/Hulk)
        /// - Converts Male/Female body types to match gender
        /// - Picks a gender-appropriate headType
        /// - Clears beard via pawn.style
        /// - Resolves graphics via the internal PawnGraphicSet ResolveAllGraphics()
        ///
        /// Call this after changing a pawn's genes at runtime.
        /// </summary>
        public static void ApplyGenderAndVisuals(Pawn pawn)
        {
            if (pawn == null) return;
            if (pawn.genes == null || pawn.story == null) return;

            var defs = pawn.genes.GenesListForReading.Select(g => g.def.defName);
            bool hasFemale = defs.Contains("FemaleOnly");
            bool hasMale = defs.Contains("MaleOnly");
            if (!hasFemale && !hasMale) return;

            // Remember old gender to avoid unnecessary name regeneration and logging
            var oldGender = pawn.gender;

            // --- Gender ---
            if (hasFemale) pawn.gender = Gender.Female;
            else if (hasMale) pawn.gender = Gender.Male;

            // --- BodyType for adults ---
            // Keep Thin/Fat/Hulk as-is; only convert Male<->Female bodytype if present
            try
            {
                if (pawn.ageTracker?.CurLifeStage?.reproductive ?? false)
                {
                    var current = pawn.story.bodyType;
                    if (current == BodyTypeDefOf.Male && pawn.gender == Gender.Female)
                    {
                        pawn.story.bodyType = BodyTypeDefOf.Female;
                    }
                    else if (current == BodyTypeDefOf.Female && pawn.gender == Gender.Male)
                    {
                        pawn.story.bodyType = BodyTypeDefOf.Male;
                    }
                    // otherwise leave Thin/Fat/Hulk or any custom shared bodytypes alone
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[GenderGenes] Error setting bodyType for {pawn}: {ex}");
            }

            // --- Regenerate name only if gender actually changed ---
            try
            {
                if (pawn.Name != null && oldGender != pawn.gender)
                {
                    pawn.Name = PawnBioAndNameGenerator.GeneratePawnName(pawn, NameStyle.Full, null, false, null);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[GenderGenes] Error regenerating name for {pawn}: {ex}");
            }

            // --- Head type (head & eyes) ---
            // Credits to Hysteresis for scripting this part
            try
            {
                string currentDef = pawn.story.headType.defName;
                string currentDefLower = currentDef.ToLowerInvariant();

                // Male
                if (pawn.gender == Gender.Male && currentDefLower.Contains("female"))
                {
                    string targetDef = ReplaceFirstIgnoreCase(currentDef, "female", "male");
                    var heads = DefDatabase<HeadTypeDef>.AllDefsListForReading
                        .Where(h => h.defName.Equals(targetDef, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    pawn.story.headType = heads.FirstOrFallback(pawn.story.headType);
                }
                // Female, requires extra check to prevent "female" from being caught as "male"
                else if (pawn.gender == Gender.Female && !currentDefLower.Contains("female") && currentDefLower.Contains("male"))
                {
                    string targetDef = ReplaceFirstIgnoreCase(currentDef, "male", "female");
                    var heads = DefDatabase<HeadTypeDef>.AllDefsListForReading
                        .Where(h => h.defName.Equals(targetDef, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    pawn.story.headType = heads.FirstOrFallback(pawn.story.headType);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[GenderGenes] Error picking headType for {pawn}: {ex}");
            }

            // --- Clear beard properly (RimWorld 1.6 uses pawn.style) ---
            try
            {
                if (pawn.style != null)
                {
                    pawn.style.beardDef = BeardDefOf.NoBeard;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[GenderGenes] Error clearing beard for {pawn}: {ex}");
            }

            // --- Refresh graphics (use private PawnGraphicSet.resolve via AccessTools) ---
            try
            {
                var renderer = pawn.Drawer?.renderer;
                if (renderer != null)
                {
                    var graphicsField = AccessTools.Field(renderer.GetType(), "graphics");
                    var graphics = graphicsField?.GetValue(renderer);
                    if (graphics != null)
                    {
                        var resolveMethod = AccessTools.Method(graphics.GetType(), "ResolveAllGraphics");
                        resolveMethod?.Invoke(graphics, Array.Empty<object>());
                    }
                    else
                    {
                        // Fallback: some RimWorld builds expose ResolveAllGraphics on the renderer itself
                        var fallback = AccessTools.Method(renderer.GetType(), "ResolveAllGraphics");
                        fallback?.Invoke(renderer, Array.Empty<object>());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[GenderGenes] Error resolving graphics for {pawn}: {ex}");
            }
        }

        // Postfix for generated pawns
        public static void GeneratePawn_Postfix(Pawn __result)
        {
            var pawn = __result;
            try
            {
                ApplyGenderAndVisuals(pawn);
            }
            catch (Exception ex)
            {
                Log.Error($"[GenderGenes] Exception in GeneratePawn_Postfix: {ex}");
            }
        }
        private static string ReplaceFirstIgnoreCase(string text, string search, string replacement)
        {
            int pos = text.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            if (pos < 0) return text; // no match
            return text.Substring(0, pos) + replacement + text.Substring(pos + search.Length);
        }
    }
}
