# Bobnik's RimWorld Mods

A monorepo of **10 RimWorld mods** by **BobnikTheIII** — a *Mass Effect*–themed
xenotype suite (folders prefixed `ME(b)` = *Mass Effect, Bobnik*) plus two
standalone Biotech gene mods. Each top-level folder is an independent, shippable
mod; the repository root is meant to be dropped into RimWorld's `Mods/` folder.

- Target **RimWorld 1.5 and 1.6** (the *Asari Parthenogenesis Addon* is 1.6-only).
- All mods require the **Biotech** DLC (`ludeon.rimworld.biotech`).
- C#-bearing mods require **[Harmony](https://github.com/pardeike/HarmonyRimWorld)**
  (`brrainz.harmony`).

## The mods

| Folder | packageId | Purpose |
|---|---|---|
| **Gender Genes** | `BobnikTheIII.GenderGenes` | Genes that make a xenotype mono-gendered |
| **Lifespan Expectancy Genes** | `BobnikTheIII.LifespanExpectancyGenes` | Genes that alter lifespan / aging rate |
| **ME(b) - Utilities** | `BobnikTheIII.MebUtilities` | Shared base dependency for all ME races |
| **ME(b) - Asari** | `BobnikTheIII.MebAsari` | Asari xenotype race |
| **ME(b) - Asari Parthenogenesis Addon** | `BobnikTheIII.MebAsariReproductionAddon` | 1.6-only addon: same-sex Asari reproduction |
| **ME(b) - Krogan** | `BobnikTheIII.MebKrogan` | Krogan xenotype race |
| **ME(b) - Quarians** | `BobnikTheIII.MebQuarians` | Quarian xenotype race |
| **ME(b) - Salarians** | `BobnikTheIII.MebSalarians` | Salarian xenotype race |
| **ME(b) - Turians** | `BobnikTheIII.MebTurians` | Turian xenotype race |
| **ME(b) - Factions** | `BobnikTheIII.MebFactions` | Factions + pawn kinds using the ME races |

The races are **vanilla-oriented** xenotypes based on BioWare's *Mass Effect*
series.

## Dependencies & load order

- Every ME race mod depends on and **loads after `ME(b) - Utilities`**.
- **ME(b) - Asari** additionally depends on *Lifespan Expectancy Genes* and
  *Gender Genes*.
- **ME(b) - Factions** cross-references the race `packageId`s via `MayRequire`
  guards, so it should load after the races it uses.

## Installation (players)

Most of these mods are published on the Steam Workshop (see each mod's
`About/About.xml` for its Workshop link). To install manually instead:

1. Ensure the **Biotech** DLC and, for the race/gene mods, **Harmony** are installed.
2. Copy the individual mod folder(s) into
   `…/RimWorld/Mods/`.
3. Enable them in-game and set the load order so **Harmony** and
   **ME(b) - Utilities** load before the races, and **ME(b) - Factions** loads last.

## Building the C# mods (developers)

There is no build script — each C# mod is a separate old-style (non-SDK)
`.csproj` targeting **.NET Framework 4.7.2**, built in Visual Studio / MSBuild.
A **Debug** build outputs the DLL straight into the mod's `Assemblies/` folder
(where RimWorld loads it).

> ⚠️ Game-DLL reference paths (`HintPath`) differ across projects — a clean
> checkout may need those paths pointed at your local RimWorld `Managed/` folder
> before it compiles. See [`CLAUDE.md`](./CLAUDE.md) for the full architecture,
> per-mod layout, C# house style, and the shared *effective-age* lifespan model.

## Credits

*Mass Effect* and its races/factions are © BioWare / EA. These are non-commercial,
fan-made mods.
