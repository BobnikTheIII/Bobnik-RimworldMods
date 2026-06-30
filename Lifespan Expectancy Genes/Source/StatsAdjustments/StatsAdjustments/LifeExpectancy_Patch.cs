using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace StatsAdjustments
{
    // Cosmetic: the pawn info card shows a flat "Life expectancy" taken straight from the race's
    // lifeExpectancy, ignoring genes (vanilla does not even fold its own LifespanFactor stat into it).
    // We post-process RaceProperties.SpecialDisplayStats and, only for a pawn carrying a lifespan gene,
    // rewrite that single entry's value to factor * lifeExpectancy - the same lifespan the mod targets.
    // Every other entry is passed through untouched, and non-gene pawns, animals and def previews are
    // left completely alone, so this stays a display-only change with no reach into other systems.
    [HarmonyPatch(typeof(RaceProperties), nameof(RaceProperties.SpecialDisplayStats))]
    public static class Patch_RaceProperties_SpecialDisplayStats
    {
        // Backing field behind StatDrawEntry.ValueString (the property is get-only). Overwriting it keeps
        // the entry's label, description, category and priority intact - only the shown value changes.
        private static readonly AccessTools.FieldRef<StatDrawEntry, string> ValueStringRef =
            AccessTools.FieldRefAccess<StatDrawEntry, string>("valueStringInt");

        static void Postfix(ThingDef parentDef, StatRequest req, ref IEnumerable<StatDrawEntry> __result)
        {
            if (__result == null || !(req.Thing is Pawn pawn))
                return;

            float factor = LifespanUtility.GetFactor(pawn);
            if (!LifespanUtility.HasEffect(factor))
                return;

            float baseLife = parentDef?.race?.lifeExpectancy ?? 0f;
            if (baseLife <= 0f)
                return;

            __result = Adjust(__result, baseLife * factor);
        }

        private static IEnumerable<StatDrawEntry> Adjust(IEnumerable<StatDrawEntry> entries, float newLife)
        {
            string targetLabel = "StatsReport_LifeExpectancy".Translate().CapitalizeFirst();
            string newValue = FormatYears(newLife);

            foreach (StatDrawEntry entry in entries)
            {
                if (entry != null && entry.LabelCap == targetLabel)
                    ValueStringRef(entry) = newValue;

                yield return entry;
            }
        }

        // Formats the value as a plain whole number of years with no unit (e.g. "80", "120"), matching how
        // RimWorld shows life expectancy on the pawn card. Our human-based factors all land on integers
        // (40/60/100/120/800), so there is nothing to truncate.
        private static string FormatYears(float years) => years.ToStringByStyle(ToStringStyle.Integer);
    }
}
