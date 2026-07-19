# ME(b) - Biotics — Build Manifest (Slice 1)

Unambiguous starting brief for an implementing agent. **Slice 1 goal:** prove the
**gene → path → cast** chain end-to-end using **XML only** (no compiled assembly). Companion specs:
`biotics_content_plan.md` (design) and `biotics_roadmap.md` (per-power stats + verified build paths).

## Target & dependencies

- **RimWorld 1.6 only** (VEF namespace differs on 1.5 — see roadmap → Build setup).
- New mod folder `ME(b) - Biotics/` at repo root, following the layout conventions in `CLAUDE.md`.
- `packageId` **`BobnikTheIII.MebBiotics`**, name "ME(b) - Biotics", author `BobnikTheIII`.
- `About.xml` `modDependencies` + `loadAfter` (exact packageIds, confirmed from installed mods):
  | Dependency | packageId |
  |---|---|
  | Biotech (genes) | `Ludeon.RimWorld.Biotech` |
  | Royalty (psycasts/psylink) | `Ludeon.RimWorld.Royalty` |
  | Harmony | `brrainz.harmony` |
  | Vanilla Expanded Framework | `OskarPotocki.VanillaFactionsExpanded.Core` |
  | Vanilla Psycasts Expanded | `VanillaExpanded.VPsycastsE` |

## Slice-1 scope (all-XML powers only)

Build the **plumbing + the powers that need zero C#**:

- 3 paths, the gene + gene category, the Asari patch, the amp item shell.
- **Powers (definite XML):** Adept — **Throw, Lift, Stasis** (simplified: stun + incapacitate, true
  invuln deferred); Vanguard — **Barrier, Slam**; Sentinel — **Biotic Ward**.
- **Powers to add if the reused class verifies in-game:** Biotic Charge (`Ability_Killskip`),
  Shockwave & Nova-without-consume (`Ability_Explode`).

**Deferred to Slice 2 (need the C# assembly):** Pull, Warp, Reave, Dark Channel, Singularity,
Barrier Sphere, Nova's barrier-consume bonus, Stasis true-invuln, and the amp's psylink-grant comp.
See "Deferred" at the bottom.

## File layout to create

```
ME(b) - Biotics/
├── About/About.xml                         (+ optional Preview.png / ModIcon.png placeholders)
├── Defs/
│   ├── PsycasterPathDefs/Paths.xml         3× PsycasterPathDef (VPE_MebAdept/Vanguard/Sentinel)
│   ├── AbilityDefs/Adept.xml               Throw, Lift, Stasis
│   ├── AbilityDefs/Vanguard.xml            Barrier, Slam
│   ├── AbilityDefs/Sentinel.xml            Biotic Ward
│   ├── HediffDefs/Hediffs.xml              MebBarrier, MebLiftSuspend, MebStasis
│   ├── GeneDefs/GeneCategory.xml           GeneCategoryDef "Biotics"
│   ├── GeneDefs/Genes.xml                  MebBioticGene
│   └── ThingDefs/BioticAmp.xml             amp ThingDef + install RecipeDef + ResearchProjectDef
├── Patches/Asari_Xenotype.xml              inject MebBioticGene into Asari (MayRequire guard)
└── Textures/
    ├── UI/Backgrounds/Meb{Adept,Vanguard,Sentinel}.png   (placeholder path backgrounds — required)
    └── Abilities/Meb/*.png                                (placeholder ability icons)
```
Single-version (1.6) mod → no `loadFolders.xml` needed; Defs at root load for 1.6.

## Per-file specs

- **Paths.xml** — 3× `VanillaPsycastsExpanded.PsycasterPathDef`: `defName` (VPE_MebAdept/Vanguard/
  Sentinel), `label`, `description`, `background` + `altBackground` (placeholder paths), `tab`
  `Psycasts`, `tooltip`, and **`<requiredGene>MebBioticGene</requiredGene>`** on all three.
- **AbilityDefs** — each power = `VEF.Abilities.AbilityDef ParentName="VPE_PsycastBase"` with
  `defName` `MebBiotic_<Power>`, `label`, `description`, `iconPath`, `targetMode`, `castTime`,
  `durationTime`, `castSound`, and `modExtensions`:
  - `VanillaPsycastsExpanded.AbilityExtension_Psycast` → `path`, `level`, `order`, `psyfocusCost`,
    `entropyGain`, `psychic:true` — **use the numbers from the roadmap tables**.
  - effect extension(s) per the roadmap **Buildability** table (`AbilityExtension_Hediff`,
    `AbilityExtension_Stun`, `Ability_ShootProjectile` / `Ability_Explode` as `abilityClass`).
- **Hediffs.xml** — `MebBarrier` (statOffsets `ArmorRating_Sharp/Blunt/Heat` +0.40; auto-remove after
  duration via a disappears comp), `MebLiftSuspend` (capMods Moving/Manipulation → 0, short),
  `MebStasis` (incapacitate). Reuse VPE `HediffComp_DisappearsOnDowned` / `HediffComp_SpawnMote`
  for cleanup/visuals.
- **Genes.xml / GeneCategory.xml** — `MebBioticGene` per the roadmap Gene spec: endogene,
  `biostatCpx` 2, `biostatMet` 0, `displayCategory` the new "Biotics" `GeneCategoryDef`, no stat
  grants, placeholder icon. (Mirror the repo's `Lifespan Expectancy Genes` gene/category defs.)
- **BioticAmp.xml** — implant `ThingDef` + install `RecipeDef` on Brain + gating `ResearchProjectDef`.
  Slice 1: the amp applies only the **PsychicSensitivity +0.25** hediff (`MebBioticAmp`). The
  **psylink-grant is a Slice-2 C# comp** — for Slice-1 testing, grant psylink via dev mode.
- **Asari_Xenotype.xml** — add `MebBioticGene` to the Asari `XenotypeDef`, guarded so it only applies
  when `BobnikTheIII.MebAsari` is loaded. **Copy the exact guard pattern from**
  `ME(b) - Factions/Patches/Royalty.xml` (`PatchOperationFindMod` / `MayRequire`).

## Verification (end-to-end, in-game, dev mode)

1. **Loads clean:** enable Harmony → Biotech/Royalty → VEF → VPE → (Asari) → ME(b) - Biotics; launch
   RimWorld 1.6; confirm **no red errors** in the log and all defs load.
2. **Gene gates the path:** dev-spawn a colonist, add `MebBioticGene` + a psylink (dev "add psylink").
   Confirm the **3 biotic paths appear** in the VPE psycast tab and vanilla paths still work.
3. **Powers cast:** unlock and cast each Slice-1 ability — Barrier applies the armor hediff; Lift
   incapacitates; Stasis stuns; Throw fires a projectile + stagger; Slam damages + knocks down; Ward
   puts Barrier on an ally.
4. **Negative test:** a pawn **without** the gene must **not** see the biotic paths.
5. **Asari patch:** with ME(b) - Asari loaded, a freshly generated Asari has `MebBioticGene`; with it
   unloaded, no errors and no orphaned patch.
6. **Amp:** install the Biotic Amp; confirm the sensitivity hediff applies (psylink-grant is Slice 2).

## Deferred to Slice 2 (C# assembly) — ✅ DONE (implemented & compiled; targets net4.8, since VPE 1.6 is net48)

Set up `Source/` (non-SDK csproj, .NET 4.7.2, output to `Assemblies/`) with the **verified HintPaths
in `biotics_roadmap.md` → Build setup**; mark VEF/VPE/game refs `<Private>False</Private>`. Then:

- **3 custom classes:** `HediffComp_DamageOverTime` (+ Reave self-heal, Dark Channel on-death jump),
  a `Pull` mover, a field `Comp` for Singularity/Barrier Sphere.
- **Amp psylink-grant** `HediffComp` (grants vanilla psylink on install).
- **Remaining powers:** Pull, Warp, Reave, Dark Channel, Singularity, Barrier Sphere; Nova's
  barrier-consume ×1.5; Stasis true-invuln/untargetable.
- **Optional v1.1:** Biotic Combos (primer marker + detonator explosion).
