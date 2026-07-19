# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Overview

This is a **monorepo of 10 RimWorld mods** by author **BobnikTheIII** — a Mass Effect–themed
suite (folders prefixed `ME(b)` = *Mass Effect, Bobnik*) plus two standalone Biotech gene mods.
Each top-level folder is an independent, shippable RimWorld mod; the repo root is meant to be
dropped into RimWorld's `Mods/` folder.

- All mods target RimWorld **1.5 and 1.6** (the *Asari Parthenogenesis Addon* is 1.6-only).
- All mods depend on **Biotech** (`ludeon.rimworld.biotech`).
- All C#-bearing mods depend on **Harmony** (`brrainz.harmony`); `0Harmony.dll` is committed
  into each such mod's `Assemblies/` folder.

## The mods

| Folder | packageId | C# | Purpose |
|---|---|---|---|
| Gender Genes | `BobnikTheIII.GenderGenes` | yes | Genes that make a xenotype mono-gendered |
| Lifespan Expectancy Genes | `BobnikTheIII.LifespanExpectancyGenes` | yes | Genes that alter lifespan / aging rate |
| ME(b) - Utilities | `BobnikTheIII.MebUtilities` | yes | Shared base dependency for all ME races |
| ME(b) - Asari | `BobnikTheIII.MebAsari` | yes | Asari xenotype race |
| ME(b) - Asari Parthenogenesis Addon | `BobnikTheIII.MebAsariReproductionAddon` | yes | 1.6-only addon: same-sex Asari reproduction |
| ME(b) - Krogan | `BobnikTheIII.MebKrogan` | no | Krogan xenotype race (XML only) |
| ME(b) - Quarians | `BobnikTheIII.MebQuarians` | no | Quarian xenotype race (XML only) |
| ME(b) - Salarians | `BobnikTheIII.MebSalarians` | no | Salarian xenotype race (XML only) |
| ME(b) - Turians | `BobnikTheIII.MebTurians` | no | Turian xenotype race (XML only) |
| ME(b) - Factions | `BobnikTheIII.MebFactions` | no | Factions + PawnKinds using the ME races |

**Inter-mod dependencies** — the ME race mods depend on and `loadAfter` **ME(b) - Utilities**.
*Asari* additionally depends on *Lifespan Expectancy Genes* and *Gender Genes*. *Factions*
cross-references the race packageIds via `MayRequire` guards, so load order matters.

## Per-mod folder layout

Standard RimWorld conventions are used. Not every folder is present in every mod:

- `About/About.xml` — every mod. Metadata, `modDependencies`, `supportedVersions`, `loadAfter`.
- `Defs/` — every mod. `GeneDef`, `XenotypeDef`, `FactionDef`, `PawnKindDef`, name makers, etc.
- `Textures/` — every mod. Gene icons and per-race pawn/head/tattoo art.
- `Languages/English/Strings/Names/` — the ME races + Factions. Name lists.
- `Patches/` — **only** `ME(b) - Factions/Patches/Royalty.xml`.
- `Assemblies/` — **only C# mods.** Compiled `.dll` + committed `0Harmony.dll`.
- `Source/` — **only C# mods.** The Visual Studio solution + project.

## Building the C# mods

There is **no build script**; each C# mod is built in Visual Studio / MSBuild per project.

- 6 projects, all **old-style (non-SDK) `.csproj`**, `TargetFrameworkVersion v4.7.2`,
  `OutputType Library`.
- **Debug `OutputPath` = the mod's `..\..\..\Assemblies\`**, so a Debug build drops the DLL
  exactly where RimWorld loads it (Release goes to `bin\Release\`). Debug uses `DebugType none`,
  no symbols.

### Gotchas

- **Game-DLL reference paths are inconsistent across projects** — this is the main reason a
  clean checkout may not build elsewhere without fixing `HintPath`s:
  - *StatsAdjustments* (Lifespan) and *HideCrestUnderHeadwear* (Utilities) reference the
    absolute path `E:\Programy\Managed\` (an extracted RimWorld `Managed` folder; this is the
    additional working directory).
  - The other projects reference Steam-relative paths, e.g.
    `..\..\..\..\..\RimWorldWin64_Data\Managed\Assembly-CSharp.dll` (assumes the repo lives
    under `steamapps\common\RimWorld\Mods\...`).
- **Files are listed explicitly with `<Compile Include=...>` (no globbing)** — a new `.cs` file
  must be added to the `.csproj` or it won't compile.
- `obj/` build artifacts are checked in; `.gitignore` only excludes `.claude/settings.local.json`.
- Primary source files are frequently still named `Class1.cs` (Visual Studio default), even
  when they hold the main class.

## C# conventions (house style)

*Lifespan Expectancy Genes* (`StatsAdjustments`) is the reference for quality.

- **Harmony bootstrap:** a `[StaticConstructorOnStartup]` static class whose static constructor
  does `new Harmony("com.bobniktheiii.<mod>")` and applies patches. Both patching styles coexist:
  annotation (`[HarmonyPatch(typeof(X), nameof(X.M))]`) and manual
  (`AccessTools.Method(...)` + `harmony.Patch(...)`).
- **Cross-version (1.5/1.6) tolerance:** prefer patching annotated types individually via
  `CreateClassProcessor(type).Patch()` inside a per-type try/catch, rather than `PatchAll()`, so
  one version-incompatible patch is skipped instead of failing the whole mod. Reflection lookups
  should null-guard and skip gracefully when a target method/field isn't found.
- **Defensive reflection** (`AccessTools.Method/Field/TypeByName`, `FieldRefAccess`) and
  null-guards everywhere. Verbose logging is gated behind `Prefs.DevMode` (often aliased as a
  `Verbose` property) so players don't get console spam.
- **Naming:** one namespace per mod, matching the assembly/root namespace. Patch classes are
  `Patch_<Type>_<Member>` or `<Purpose>Patch` / `<Purpose>Patcher`; Harmony hooks are
  `Prefix`/`Postfix`/`Finalizer` or descriptive names. Harmony IDs are `com.bobniktheiii.<mod>`.
- **Comments are English, rationale-first** — multi-line block headers above a patch that
  explain the vanilla behavior being altered and *why*, not just what the code does.

## Key architecture: the Lifespan effective-age model

`LifespanUtility` in `Lifespan Expectancy Genes/Source/StatsAdjustments/StatsAdjustments/Class1.cs`
is the **single source of truth** for how altered lifespan affects a pawn. Understand this before
touching anything age-related.

- Genes carry a `DefModExtension` `GeneExtension_Lifespan { float lifespanFactor }`
  (`>1` = longer life / slower aging, `<1` = shorter). `GetFactor(pawn)` multiplies the factor
  across all of the pawn's genes.
- `GetEffectiveAge` is a **linear remap**: below `ChildhoodCutoff = 18` age is untouched (kids
  stay on vanilla child curves); above 18 it rescales so the pawn hits natural mortality at
  `factor * lifeExpectancy` real years.
- The **`AgeSpoofer`** (`AgeSpoof.cs`) is a `[ThreadStatic]` depth counter. While active, Harmony
  postfixes on `Pawn_AgeTracker`'s biological-age getters return the *effective* age. Any system
  that should judge on effective age wraps the vanilla call with `Push()` (Prefix) / `Pop()`
  (Finalizer). The counter (not a bool) allows nesting; the Finalizer guarantees restore on
  exception; thread-static prevents background pawn generation from leaking the flag.
- This one model drives **stats** (`StatPart_Age_Patches.cs`), **fertility**
  (`Patch_StatPart_FertilityByGenderAge_AgeFactor`), **mortality / birthday morbidity**
  (`Mortality_Patches.cs`), the **childbirth ritual** (`RitualOutcomeComp_Patches.cs`), and the
  displayed life expectancy (`LifeExpectancy_Patch.cs`).

**Rule:** to make a new behavior respect altered lifespan, read effective age through
`LifespanUtility` and use the `AgeSpoofer` Push/Pop pattern — do not compute age math ad hoc.

## XML Def patterns

- **Genes:** an abstract `GeneDef Name="..."` base with `exclusionTags` (for mutually-exclusive
  spectra), concrete genes inherit via `ParentName`, and code-driven genes attach a
  `<modExtensions>` `<li Class="...">`.
- **Conditional / cross-mod patches:** `PatchOperationFindMod` wrapping the operation, plus
  `li ... MayRequire="BobnikTheIII.Meb..."` guards so entries only load when the sibling mod is
  present. Canonical example: `ME(b) - Factions/Patches/Royalty.xml`.
- **packageId convention:** `BobnikTheIII.<Mod>`.

## Housekeeping

- **Chat with the user is in Polish, but all shipped mod text and code comments are English.**
  Keep it that way.
- Local settings deny `git push` and destructive deletes (`rm`, `Remove-Item`, etc.) — do not
  attempt them.
- `biotics_plan.txt` at the repo root is a **design note for a planned, not-yet-built** mod
  (Mass Effect biotic powers built on Vanilla Psycasts Expanded). It is not implemented in the tree.
