using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;
using RimWorld;

namespace HideCrestUnderHeadwear
{
    [StaticConstructorOnStartup]
    public static class HideCrestUnderHeadwearPatch
    {
        // ===== CONFIGURE GENE NAMES HERE =====
        // Exact defName strings of your crest genes
        private static readonly string[] GeneNames = new[]
        {
            "Asari_Crest",
            "Salarian_Crest",
            "Krogan_Crest_White",
            "Krogan_Crest_Red"
        };
        // ======================================

        // Cached FieldInfo for PawnRenderNode.tree
        private static readonly FieldInfo PawnRenderNode_treeField;
        // Cached FieldInfo for PawnRenderTree.pawn
        private static readonly FieldInfo PawnRenderTree_pawnField;

        static HideCrestUnderHeadwearPatch()
        {
            var harmony = new Harmony("com.bobniktheiii.hidecrestunderheadwear");
            // Cache reflection fields
            PawnRenderNode_treeField = AccessTools.Field(typeof(PawnRenderNode), "tree");
            var pawnRenderTreeType = AccessTools.TypeByName("PawnRenderTree");
            if (pawnRenderTreeType != null)
                PawnRenderTree_pawnField = AccessTools.Field(pawnRenderTreeType, "pawn");

            // Patch PawnRenderNodeWorker.CanDrawNow
            var baseWorkerType = AccessTools.TypeByName("PawnRenderNodeWorker");
            var pawnRenderNodeType = AccessTools.TypeByName("PawnRenderNode");
            if (baseWorkerType == null || pawnRenderNodeType == null)
            {
                //Log.Warning("[HideCrestUnderHeadwear] Could not find types to patch; patch not applied.");
                return;
            }
            var canDrawNowMethod = AccessTools.Method(baseWorkerType, "CanDrawNow",
                new[] { pawnRenderNodeType, typeof(PawnDrawParms) });
            if (canDrawNowMethod == null)
            {
                //Log.Warning("[HideCrestUnderHeadwear] Could not find CanDrawNow on PawnRenderNodeWorker; patch not applied.");
                return;
            }
            harmony.Patch(canDrawNowMethod,
                prefix: new HarmonyMethod(typeof(HideCrestUnderHeadwearPatch), nameof(Prefix_CanDrawNow)));
            //Log.Message("[HideCrestUnderHeadwear] Patched PawnRenderNodeWorker.CanDrawNow");
        }

        // Prefix for PawnRenderNodeWorker.CanDrawNow
        public static bool Prefix_CanDrawNow(PawnRenderNodeWorker __instance, PawnRenderNode node, PawnDrawParms parms, ref bool __result)
        {
            if (node == null)
                return true;

            // Only care about head-attachment nodes
            var nodeType = node.GetType();
            if (nodeType.Name != "PawnRenderNode_AttachmentHead")
            {
                // Alternatively, if in future node class differs, one could check parentTagDef here.
                return true;
            }

            // Retrieve Pawn via cached reflection: node.tree.pawn
            Pawn pawn = null;
            if (PawnRenderNode_treeField != null && PawnRenderTree_pawnField != null)
            {
                var treeObj = PawnRenderNode_treeField.GetValue(node);
                if (treeObj != null)
                    pawn = PawnRenderTree_pawnField.GetValue(treeObj) as Pawn;
            }
            if (pawn == null)
            {
                // Cannot retrieve pawn: skip hide logic
                return true;
            }

            // Check if pawn has any of the target genes
            var genesComp = pawn.genes;
            if (genesComp == null)
                return true;
            bool hasCrestGene = false;
            foreach (var defName in GeneNames)
            {
                if (string.IsNullOrWhiteSpace(defName)) continue;
                var gdef = DefDatabase<GeneDef>.GetNamedSilentFail(defName);
                if (gdef != null && genesComp.HasActiveGene(gdef))
                {
                    hasCrestGene = true;
                    break;
                }
            }
            if (!hasCrestGene)
                return true;

            // If wearing any overhead apparel (hat/helmet), hide the crest
            var worn = pawn.apparel?.WornApparel;
            if (worn != null && worn.Any(app =>
                    app.def.apparel.layers != null &&
                    app.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead)))
            {
                __result = false;
                return false;
            }

            // Otherwise draw normally
            return true;
        }
    }
}
