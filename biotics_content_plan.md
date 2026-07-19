# ME(b) - Biotics — Content Design Plan

Content design for a RimWorld mod adding **Mass Effect biotic powers**, built on
**Vanilla Psycasts Expanded (VPE)**. This document defines the powers and their in-game effects
so scope and balance are agreed **before** any defs or code are written. It follows the initial
note in `biotics_plan.txt`.

**Status:** roster proposal for sign-off. No defs/code yet.

## Design goals

- Recreate the *feel* of Mass Effect biotics within RimWorld's limits — imitate each power's
  effect "to the extent of the game's abilities."
- **Integrate with VPE** rather than reinvent it: powers are VPE psycasts using its paths,
  psyfocus, neural heat, and psylink leveling.
- **Trilogy-first (ME1–3),** with a few iconic Andromeda powers (Lash, Lance, Annihilation) used
  only to fill out tree density. No multiplayer-exclusive powers.
- **Depth without bloat:** 10 powers per path (30 total, ~2 per level) to match VPE's tree density —
  a trilogy core plus upgrade tiers, utility casts, and a few Andromeda fills, each carefully mapped.
- Keep the game balanced — costs scale with power; hard crowd-control and AoE are expensive.

## Scope decisions (locked)

| Decision | Choice |
|---|---|
| Source material | Trilogy (ME1–3) core + a few Andromeda fills (Lash, Lance, Annihilation) |
| Organization | Class-themed VPE paths: **Adept**, **Vanguard**, **Sentinel** |
| Breadth | 10 per path / 30 total (trilogy core + upgrade tiers + utility + a few Andromeda) |
| Access / gating | **Gene + amp** (Option A): Eezo-nodules gene = potential, biotic-amp implant = usable casting. See [Access & gating](#access--gating). |

---

## How biotics map onto VPE (integration model)

Confirmed against the VPE repository schema:

- **Paths** = `VanillaPsycastsExpanded.PsycasterPathDef` — fields `defName`, `label`,
  `description`, `background` / `altBackground`, `tab` (`Psycasts`), `tooltip`, plus optional
  requirement fields. Three new paths (repo prefix convention):
  `VPE_MebAdept`, `VPE_MebVanguard`, `VPE_MebSentinel`.
- **Powers** = `VEF.Abilities.AbilityDef ParentName="VPE_PsycastBase"` — with `targetMode`,
  `castTime`, `durationTime`, `abilityClass`, `castSound`, `iconPath`, and `modExtensions`:
  - `VanillaPsycastsExpanded.AbilityExtension_Psycast` → `path`, `level` (1–5 tier within the
    path), `order`, `psyfocusCost`, `entropyGain` (neural heat), `psychic:true`.
  - one or more **effect** extensions — e.g. `VEF.Abilities.AbilityExtension_Hediff` (apply a
    buff / debuff / DoT / stun hediff, with `durationMultiplier` scaling on PsychicSensitivity),
    plus VEF ability classes for damage / AoE / teleport (`Ability_Explode`, `Ability_ShootProjectile`,
    `Ability_Barrier`, `Ability_Spawn`, `AbilityExtension_Stun`). Confirmed class list + verified DLL
    paths are in `biotics_roadmap.md`.
- **Custom `HediffDef`s** carry most bespoke effects (Barrier absorb, Lift suspend, Stasis, Warp
  DoT, Reave lifesteal, Dark Channel DoT, primer marker).
- **Balance levers:** `psyfocusCost` + `entropyGain` scaled by tier; magnitude/duration scale
  with psychic sensitivity, exactly like vanilla psycasts.

**Reference example** (VPE's own `VPE_SpeedBoost`, for schema shape):

```xml
<VEF.Abilities.AbilityDef ParentName="VPE_PsycastBase">
  <defName>VPE_SpeedBoost</defName>
  ...
  <modExtensions>
    <li Class="VanillaPsycastsExpanded.AbilityExtension_Psycast">
      <path>VPE_Warlord</path>
      <level>1</level>
      <order>1</order>
      <psyfocusCost>0.12</psyfocusCost>
      <entropyGain>12</entropyGain>
      <psychic>true</psychic>
    </li>
    <li Class="VEF.Abilities.AbilityExtension_Hediff">
      <hediff>VPE_SpeedBoost</hediff>
      <durationMultiplier>PsychicSensitivity</durationMultiplier>
    </li>
  </modExtensions>
</VEF.Abilities.AbilityDef>
```

Cost tiers used below: **Low** ≈ psyfocus 0.10–0.15 / heat ~10–15 · **Med** ≈ 0.20–0.30 / ~20–30 ·
**High** ≈ 0.35–0.50 / ~35–50. Exact numbers tuned during implementation.

---

## Path A — Adept (Biotic Control)

Crowd-control, suspension, area denial. The generalist biotic; keeps enemies helpless.

| Lvl | Power | Src | ME effect → RimWorld effect | Target | Cost |
|---|---|---|---|---|---|
| 1 | **Throw** | trilogy | Kinetic burst that hurls a target → knockback + light blunt damage + brief stagger | Single | Low |
| 1 | **Biotic Focus** | utility | Focuses the caster's fields → self buff briefly raising psychic sensitivity (stronger/longer casts) | Self | Low |
| 2 | **Pull** | trilogy | Yanks a target off their feet → drag/stagger toward a point; interrupts their action | Single | Low |
| 2 | **Lash** | Andromeda | A biotic tendril snaps a target → single-target blunt + yank/stagger | Single | Low |
| 3 | **Heavy Throw** | upgrade | Stronger Throw → larger blunt burst + strong knockback over a small area | Single/AoE | Med |
| 3 | **Lift** | trilogy | Suspends a target helplessly aloft → incapacitates (no move/act) for a few seconds | Single | Med |
| 4 | **Stasis** | trilogy | Seals a target in a stasis bubble → incapacitated **and** invulnerable/untargetable (lockdown, no damage) | Single | Med |
| 4 | **Lift Grenade** | trilogy | Area lift → suspends **all** enemies in a target area | AoE zone | Med |
| 5 | **Annihilation Field** | Andromeda | A mobile field of dark energy → damage-over-time aura around the caster as they move | Self aura | High |
| 5 | **Singularity** | trilogy | Gravity well → AoE field that suspends all enemies inside for its duration | AoE zone | High |

*Signature:* **Singularity** — the Adept capstone and a classic combo primer.

## Path B — Vanguard (Biotic Assault)

Mobility and burst. Close the distance, hit hard, survive on Barrier.

| Lvl | Power | Src | ME effect → RimWorld effect | Target | Cost |
|---|---|---|---|---|---|
| 1 | **Barrier** | trilogy | Biotic shield around the caster → self hediff granting temporary armour for a duration | Self | Low |
| 1 | **Biotic Flux** | utility | Biotically augmented stride → self burst of move speed to reposition/close in | Self | Low |
| 2 | **Biotic Charge** | trilogy | Hurls self into a target → teleport-dash, impact blunt AoE on arrival, refreshes Barrier | Single (dash) | Med |
| 2 | **Biotic Leap** | utility | Leap through space → jump to a target location (gap-closer, no attack; reuses PowerLeap) | Location | Low |
| 3 | **Slam** | trilogy | Lifts and smashes one enemy → high blunt damage + knockdown | Single | Med |
| 3 | **Reinforced Barrier** | upgrade | Stronger, longer Barrier → higher armour offset, longer duration | Self | Med |
| 4 | **Shockwave** | trilogy | Radial biotic detonation → AoE knock-up + stagger + light damage | AoE | Med |
| 4 | **Heavy Charge** | upgrade | Upgraded Charge → longer range, bigger impact AoE, refreshes Barrier | Single (dash) | Med |
| 5 | **Nova** | trilogy | Discharges the barrier in an explosion → self AoE burst; consumes Barrier for bonus damage | Self AoE | High |
| 5 | **Flare** | ME3/upgrade | A massive biotic detonation → large, high-damage AoE explosion | AoE | High |

*Signature combo:* **Charge → Barrier refill → Nova** — the iconic Vanguard loop.

## Path C — Sentinel (Warp & Wards)

Damage-over-time and protection/support. Attrition and keeping allies alive.

| Lvl | Power | Src | ME effect → RimWorld effect | Target | Cost |
|---|---|---|---|---|---|
| 1 | **Warp** | trilogy | Biotic DoT that tears flesh and armour → single-target DoT; extra effective vs armoured / mechanoids | Single | Low |
| 1 | **Biotic Mending** | utility | Channels fields to knit wounds → heal-over-time on a chosen ally (or self) | Single (ally) | Low |
| 2 | **Reave** | trilogy | Drains life from a target → damage to the enemy + self heal-over-time | Single | Med |
| 2 | **Lance** | Andromeda | A focused lance of dark energy → single-target biotic burst damage | Single | Med |
| 3 | **Dark Channel** | trilogy | Persistent DoT that leaps to a new enemy on the victim's death → DoT with on-death "jump to nearest" | Single → spreads | Med |
| 3 | **Backlash** | Andromeda | A frontal biotic guard → brief self buff granting very high armour / deflection | Self | Med |
| 4 | **Warp Field** | upgrade | Area Warp → AoE damage-over-time / armour-shred over a target zone | AoE | Med |
| 4 | **Biotic Ward** | trilogy | Projects a barrier onto an ally → applies the Barrier hediff to a chosen ally | Single (ally) | Med |
| 5 | **Cluster Ward** | upgrade | Mass ward → applies the Barrier hediff to **all** allies in a target area | AoE (ally) | High |
| 5 | **Barrier Sphere** | trilogy | Stationary protective field → AoE zone granting Barrier to allies standing inside | AoE zone (ally) | High |

*Signature:* **Warp/Reave/Dark Channel** as primers feeding another biotic's detonators (see below).

> **Tier note:** VPE paths run levels **1–5** (capstone at 5). Concrete per-level stats, costs, and
> effects are specified in the companion **`biotics_roadmap.md`**.

---

## Optional design pillar — Biotic Combos (detonations)

A signature Mass Effect mechanic worth capturing for identity and skill depth. **Flagged as an
optional v1.1 system** — it needs custom C# and is *not* required for the first release.

- **Primers** apply a short "biotically-charged" marker hediff to the target:
  Warp, Lift, Singularity, Pull, Reave, Dark Channel. *(Stasis is **not** a primer — an invulnerable
  target can't be detonated, matching ME.)*
- **Detonators** consume that marker for a bonus **biotic explosion** (extra AoE damage):
  Throw, Biotic Charge, Nova, Shockwave, Slam.

Note: because Vanguard powers are all detonators and Sentinel powers all primers, **combos are a
cross-path / multi-pawn mechanic** (only Adept can self-combo). This is an intended design choice —
see the "Open decisions" in `biotics_roadmap.md`.

This rewards combining powers (often across paths / between pawns) instead of spamming a single
ability, and gives biotics a genuine skill ceiling. If cut from v1, the powers above all still
function standalone.

---

## Cross-cutting requirements

- **New `HediffDef`s:** Barrier (damage absorb), Lift/Suspend (stun-like incapacitation),
  Stasis (invulnerable + incapacitated), Warp DoT, Reave lifesteal, Dark Channel DoT,
  "biotically-charged" primer marker, and the **Biotic Amp implant** (grants/buffs psycasting).
- **New `GeneDef`:** `MebBioticGene` (Eezo Nodules), set as `<requiredGene>` on the three paths.
- **New surgery + item:** the Biotic Amp implant (recipe/`ThingDef` + install `RecipeDef`).
- **Xenotype patch:** inject `MebBioticGene` into the Asari `XenotypeDef` via
  `PatchOperationFindMod` + `MayRequire="BobnikTheIII.MebAsari"`.
- **Mostly XML, a little C#.** VEF ships reusable ability classes (`Ability_Barrier`,
  `Ability_Explode`, `Ability_ShootProjectile`, `Ability_Spawn`, `AbilityExtension_Stun/Hediff`) and
  VPE adds teleport-strike (`Ability_Killskip`), so most powers are XML by reusing a class. The only
  custom code is ~3 small classes (a damage-over-time HediffComp, a Pull mover, a field comp for
  Singularity/Barrier Sphere). See the verified **Buildability** table in `biotics_roadmap.md`.
- **Assets:** per-power ability icons, three path backgrounds, and cast sounds — placeholder art
  first, polished later.
- **Dependencies:** Vanilla Psycasts Expanded (+ Vanilla Expanded Framework), Biotech, Harmony. New
  mod folder `ME(b) - Biotics`, standard repo layout, `packageId` `BobnikTheIII.MebBiotics`.
  **Supported version: 1.6 only for v1** — VEF renamed `VFECore.dll` → `VEF.dll` between 1.5/1.6, so
  1.5 is a separate later effort. Verified build/DLL paths are in `biotics_roadmap.md` → Build setup.
- **Balance summary:** cost scales with power; hard CC (Stasis, Singularity) and AoE (Nova,
  Shockwave, Barrier Sphere) are the most expensive; effect magnitude/duration scale with psychic
  sensitivity like all VPE psycasts.

## Access & gating

Two independent requirements, mirroring Mass Effect biology (Option A — uses vanilla + VPE
mechanics, no patching of VPE's path system):

1. **Eezo Nodules — gene (`MebBioticGene`) = biotic *potential*.**
   Set as `<requiredGene>` on all three biotic `PsycasterPathDef`s (Adept / Vanguard / Sentinel),
   so a path simply doesn't unlock unless the pawn has the gene. This is a first-class VPE field
   (`PsycasterPathDef.requiredGene`, checked in `CanPawnUnlock`) — the same mechanism VPE's own
   Biotech Integration addon uses. Delivery:
   - Standalone gene, usable via the gene assembler and gene-ripping — any pawn can *acquire* it.
   - **Asari are the only xenotype born biotic.** The gene is injected into the Asari
     `XenotypeDef` via a `PatchOperationFindMod` + `MayRequire="BobnikTheIII.MebAsari"` patch (the
     repo's existing pattern, cf. `ME(b) - Factions/Patches/Royalty.xml`), so it only applies when
     the Asari mod is loaded and keeps the Biotics mod standalone and non-breaking.
   - No other xenotype ships the gene; non-asari become biotic only by acquiring it.

2. **Biotic Amp — implant (Hediff installed by surgery) = the ability to *cast*.**
   A latent biotic (gene only) can't yet use powers; installing the amp enables casting. The amp
   **grants psylink level 1** (entry into the VPE psycaster system) **and buffs** psychic
   sensitivity / cast quality. Higher psylink levels still come from normal VPE progression.

**Net:** the gene decides whether the biotic paths *appear*; the amp decides whether the pawn can
actually *cast*. Both are needed to be a functional biotic.

### Sub-decisions

**Decided:**
- **Psylink source:** the amp **grants psylink level 1** (the entry ticket) **and buffs** higher
  progression (psychic sensitivity / cast quality). So gene + amp = immediately castable; levels
  beyond 1 still come from normal VPE progression. *(Revised per sanity check — supersedes the
  earlier "amp only buffs, never grants" call, which left an amped pawn unable to cast.)*
- **Amp tiers:** **one** amp implant — no L2→L5n upgrade line.
- **One gene:** a single shared `MebBioticGene` gates all three paths (no per-path genes).

**Gene & amp stats (specified):** `MebBioticGene` = an **endogene**, Complexity **2**, Metabolism
**0** (neutral — pure gate, no metabolic cost), no granted stats. The **Biotic Amp**
grants **psylink 1** + **PsychicSensitivity +0.25**. Full spec + rationale in the "Gene & Amp
implementation spec" of `biotics_roadmap.md`.

## Power roster at a glance

| Path | Powers (10 each) |
|---|---|
| Adept (Control) | Throw · Biotic Focus · Pull · Lash · Heavy Throw · Lift · Stasis · Lift Grenade · Annihilation Field · Singularity |
| Vanguard (Assault) | Barrier · Biotic Flux · Biotic Charge · Biotic Leap · Slam · Reinforced Barrier · Shockwave · Heavy Charge · Nova · Flare |
| Sentinel (Warp & Wards) | Warp · Biotic Mending · Reave · Lance · Dark Channel · Backlash · Warp Field · Biotic Ward · Cluster Ward · Barrier Sphere |

*30 powers across 3 paths (10 per path, ~2 per level) — trilogy core, filled out with upgrade tiers,
utility casts, and a few Andromeda powers, plus an optional combo/detonation layer.*
