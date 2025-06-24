using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MatriarchalGene
{
    // 1. DefOf to access MatriarchGene
    [DefOf]
    public static class InternalDefOf
    {
        public static GeneDef MatriarchGene;
        static InternalDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(InternalDefOf));
    }

    // 2. Harmony init: patch CreateInitialComponents to attach our comp
    [StaticConstructorOnStartup]
    public static class HarmonyInit
    {
        static HarmonyInit()
        {
            //Log.Message("[MatriarchalGene] Patching CreateInitialComponents");
            var harmony = new Harmony("com.bobniktheiii.matriarchinheritance");
            harmony.Patch(
                AccessTools.Method(typeof(PawnComponentsUtility), nameof(PawnComponentsUtility.CreateInitialComponents)),
                postfix: new HarmonyMethod(typeof(GeneInheritancePatch), nameof(GeneInheritancePatch.Postfix_CreateInitialComponents))
            );
            //Log.Message("[MatriarchalGene] Patch applied");
        }
    }

    // 3. Postfix: attach InheritanceThingComp to newborn human pawns
    public static class GeneInheritancePatch
    {
        public static void Postfix_CreateInitialComponents(Pawn pawn)
        {
            if (pawn == null || !pawn.RaceProps.Humanlike || pawn.genes == null || pawn.ageTracker == null)
                return;

            float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
            // Treat < 0.1 years as newborn/infant, then find mother for newborns
            if (ageYears >= 0.1f)
                return;

            // Attach our ThingComp if not already present
            if (pawn.AllComps.OfType<InheritanceThingComp>().FirstOrDefault() == null)
            {
                var comp = new InheritanceThingComp();
                comp.parent = pawn;
                pawn.AllComps.Add(comp);
                //Log.Message($"[MatriarchalGene] Attached InheritanceThingComp to newborn {pawn.LabelShort}, age {ageYears:F3}");
            }
        }
    }

    // 4. ThingComp that waits until mother relation exists, then overrides genes once
    public class InheritanceThingComp : ThingComp
    {
        private bool applied = false;

        public override void CompTick()
        {
            if (applied)
                return;

            var pawn = parent as Pawn;
            if (pawn == null || !pawn.RaceProps.Humanlike || pawn.genes == null || pawn.ageTracker == null)
            {
                applied = true;
                return;
            }

            float ageYears = pawn.ageTracker.AgeBiologicalYearsFloat;
            // Only attempt while infant (<1 year). After that, give up if searching for mother.
            if (ageYears >= 1f)
            {
                applied = true;
                return;
            }

            // Try find mother relation: DirectRelations with Parent def and female
            var rel = pawn.relations?.DirectRelations
                .FirstOrDefault(r => r.def == PawnRelationDefOf.Parent && r.otherPawn != null && r.otherPawn.gender == Gender.Female);
            if (rel == null)
                return; // still no mother

            Pawn motherPawn = rel.otherPawn;
            if (motherPawn == null)
            {
                applied = true;
                return;
            }

            // Now we have a mother. Check she has genes component and MatriarchGene
            if (motherPawn.genes == null)
            {
                //Log.Message($"[MatriarchalGene] Mother {motherPawn.LabelShort} has no genes; skipping override.");
                applied = true;
                return;
            }
            bool hasGene = motherPawn.genes.HasActiveGene(InternalDefOf.MatriarchGene);
            //Log.Message($"[MatriarchalGene] Found mother {motherPawn.LabelShort}. Has MatriarchGene? {hasGene}");
            if (!hasGene)
            {
                applied = true;
                return;
            }

            // Perform override: clear child genes, copy mother's
            //Log.Message($"[MatriarchalGene] Overriding genes for child {pawn.LabelShort} from mother {motherPawn.LabelShort}");

            // Remove all existing child genes
            var existing = pawn.genes.GenesListForReading.ToList();
            //Log.Message($"[MatriarchalGene] Removing {existing.Count} genes from child.");
            foreach (var g in existing)
            {
                pawn.genes.RemoveGene(g);
            }

            // Copy all mother's genes (use false for xenogene flag)
            var moms = motherPawn.genes.GenesListForReading;
            //Log.Message($"[MatriarchalGene] Copying {moms.Count} genes from mother.");
            foreach (var g in moms)
            {
                pawn.genes.AddGene(g.def, false);
            }

            // ... after copying mother’s genes:
            var geneTracker = pawn.genes;
            var motherTracker = motherPawn.genes;

            // Retrieve mother xenotype
            XenotypeDef motherXeno = null;
            try { motherXeno = motherTracker.Xenotype; }
            catch { }
            if (motherXeno == null)
            {
                var prop = AccessTools.Property(typeof(Pawn_GeneTracker), "Xenotype");
                if (prop != null) motherXeno = prop.GetValue(motherTracker) as XenotypeDef;
            }
            if (motherXeno != null)
            {
                // Set child xenotype
                var setMeth = AccessTools.Method(typeof(Pawn_GeneTracker), "SetXenotype", new[] { typeof(XenotypeDef) });
                if (setMeth != null)
                {
                    setMeth.Invoke(geneTracker, new object[] { motherXeno });
                }
                else
                {
                    var field = AccessTools.Field(typeof(Pawn_GeneTracker), "xenotype");
                    if (field != null)
                        field.SetValue(geneTracker, motherXeno);
                }
                //if (Prefs.DevMode)
                    //Log.Message($"[MatriarchalGene] Set xenotype of {pawn.LabelShort} to {motherXeno.defName}");
            }
            else if (Prefs.DevMode)
            {
                //Log.Warning("[MatriarchalGene] Failed to get mother’s XenotypeDef for assigning to child.");
            }


            //Log.Message("[MatriarchalGene] Inheritance override complete.");
            applied = true;
        }

        // Optional: run less frequently
        public override void CompTickRare()
        {
            CompTick();
        }
    }
}
