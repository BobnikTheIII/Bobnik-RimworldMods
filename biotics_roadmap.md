# ME(b) - Biotics — Path Roadmap & Balance Sheet

Concrete per-level progression, stats, effects, and costs for every biotic power. Companion to
`biotics_content_plan.md` (design/scope). **All numbers are starting values for playtest tuning**,
deliberately anchored to real Vanilla Psycasts Expanded (VPE) abilities so the cost/reward curve
reads as vanilla.

## Conventions

- **Levels 1–5.** Each path unlocks one power per level; **level 5 is the capstone**. `order` is 1
  unless noted.
- **Cost = `psyfocusCost` (0–1 pool) + `entropyGain` (neural heat).** In combat, **neural heat is
  the binding limit** — chained casts overheat first, so heat is the primary balance lever.
- **Cast/duration in ticks** (60 ticks = 1 s). Biotic powers use short casts (15–60) so they're
  usable mid-fight, unlike ritual psycasts (120–600).
- **Range** in cells; `~24.9` is the standard psycast targeting range.
- **`psychic: true`** on all; magnitude/duration scale with **Psychic Sensitivity**
  (`durationMultiplier`). The **Biotic Amp implant grants psylink 1** (the entry ticket) **and buffs
  Psychic Sensitivity** — so gene + amp = immediately castable, and the amp makes powers stronger.
- **Damage type:** kinetic biotics deal **blunt** (mass-effect crush); Warp/Reave/Dark Channel use
  a **biotic damage-over-time hediff** (direct HP loss, ignores armor).
- **Combo tags** (optional v1.1): **[P]** = primer (applies "biotically-charged" marker),
  **[D]** = detonator (consumes marker for a bonus AoE blunt burst, radius ~2.4).

### Vanilla calibration reference (anchors used)
| Vanilla ability | Lvl | psyfocus | heat | cast | note |
|---|---|---|---|---|---|
| VPE_SpeedBoost (self-buff) | 1 | 0.12 | 12 | 15 | L1 self-buff anchor |
| VPE_Flameball (dmg grenade) | 1 | 0.02 | 12 | 60 | L1 damage anchor |
| VPE_Skip (teleport) | 2 | 0.02 | 25 | 30 | dash anchor |
| VPE_StealVitality (lifesteal) | 2 | 0.06 | 20 | 15 | Reave anchor |
| VPE_Liferot (DoT) | 2 | 0.06 | 24 | 30 | Warp/DoT anchor |
| VPE_Explosion (AoE r7) | 3 | 0.06 | 42 | 30 | AoE damage anchor |
| VPE_Overshield (Pawn shield) | 3 | 0.08 | 50 | 60 | Ward anchor |
| VPE_EyeBlast (AoE 14 dmg r3) | 4 | 0.08 | 35 | 15 | burst-dmg anchor |
| VPE_RaidPause (AoE hard-CC) | 4 | 0.40 | 60 | 600 | hard-CC anchor |
| VPE_Timequake / FireBeam (capstone) | 5 | 0.6 / 0.5 | — / 90 | 1200 / 300 | capstone anchor |

---

## Path A — Adept (Biotic Control)
*Identity:* keep enemies helpless — the generalist controller. Rich in **primers**.

| Lvl | Power | Cast | Range | Target | Dur | psyfocus | heat | Effect (starting numbers) |
|---|---|---|---|---|---|---|---|---|
| 1 | **Throw** [D] | 20 | 24.9 | Pawn/Loc | — | 0.05 | 12 | 8 blunt + knockback ~4 cells + stagger (stun 30t) |
| 1 | **Biotic Focus** | 15 | — | Self | 900 | 0.06 | 10 | Self buff: +0.15 PsychicSensitivity (stronger/longer casts) |
| 2 | **Pull** [P] | 20 | 24.9 | Pawn | — | 0.05 | 16 | Drag ~6 cells toward caster; interrupt + stagger (stun 45t); no damage |
| 2 | **Lash** | 20 | 24.9 | Pawn | — | 0.05 | 18 | 10 blunt + yank/stagger (stun 30t) |
| 3 | **Heavy Throw** [D] | 22 | 24.9 | Pawn/Loc | — | 0.08 | 26 | 16 blunt AoE r~2.4 + strong knockback + stagger |
| 3 | **Lift** [P] | 25 | 24.9 | Pawn | 300 | 0.10 | 30 | Suspend: Moving/Manipulation → 0; +25% incoming damage while lifted |
| 4 | **Stasis** | 25 | 24.9 | Pawn | 360 | 0.12 | 35 | Full incapacitate **+ invulnerable & untargetable** (deals no damage) |
| 4 | **Lift Grenade** [P] | 30 | 24.9 | Location | 300 | 0.14 | 42 | Field r~3.4; Lift all enemies inside |
| 5 | **Annihilation Field** | 25 | — | Self | 900 | 0.25 | 50 | Mobile DoT aura r~3.9 around the caster |
| 5 | **Singularity** [P] | 60 | 24.9 | Location | 600 | 0.40 | 70 | Field r~3.9; enemies inside get Lift, released on exit/expiry |

**Notes.** Throw is the cheap spammable opener/detonator (Flameball-priced). Stasis costs more than
Lift and is invuln-while-frozen *by design* — it's lockdown, not a damage setup, so it can't be
abused to freeze-and-burst. **Stasis is deliberately not a combo primer:** an invulnerable target
can't be detonated (matches ME). Singularity is the capstone hard-CC, priced between RaidPause
(0.40/60) and Timequake — expensive heat, long cast.

---

## Path B — Vanguard (Biotic Assault)
*Identity:* dive in, burst, survive on Barrier. The **Charge → Barrier → Nova** loop is the payoff.

| Lvl | Power | Cast | Range | Target | Dur | psyfocus | heat | Effect (starting numbers) |
|---|---|---|---|---|---|---|---|---|
| 1 | **Barrier** | 15 | — | Self | 1800 | 0.10 | 14 | Hediff (`AbilityExtension_Hediff`): **+0.40 Sharp/Blunt/Heat armor**. The canonical "Barrier" — Ward, Sphere and Nova's consume all reference it. |
| 1 | **Biotic Flux** | 15 | — | Self | 1200 | 0.10 | 12 | Self buff: burst of move speed to reposition/close in |
| 2 | **Biotic Charge** [D] | 20 | 28 | Pawn | — | 0.06 | 28 | Teleport adjacent; 10 blunt AoE r~2.4 + stagger; **refreshes Barrier** |
| 2 | **Biotic Leap** | 10 | 14.9 | Location | — | 0.04 | 16 | Leap to a cell (gap-closer, no attack; reuse `Ability_PowerLeap`) |
| 3 | **Slam** [D] | 25 | 24.9 | Pawn | — | 0.08 | 32 | 20 blunt single-target + knockdown (downed 60t) |
| 3 | **Reinforced Barrier** | 15 | — | Self | 2400 | 0.14 | 30 | Stronger Barrier: **+0.60 armor**, longer duration |
| 4 | **Shockwave** [D] | 30 | 24.9 | Location | — | 0.10 | 40 | Radial blast r~3.4: each enemy hit 12 blunt + knock-up + stagger |
| 4 | **Heavy Charge** [D] | 20 | 40 | Pawn | — | 0.10 | 40 | Longer dash; 16 blunt AoE r~3.4 on arrival; **refreshes Barrier** |
| 5 | **Nova** [D] | 20 | — | Self | — | 0.20 | 40 | Self AoE r~3.4, 14 blunt; **if Barrier active, consume it ×1.5** then remove |
| 5 | **Flare** | 30 | 24.9 | Location | — | 0.35 | 60 | Massive AoE r~4.9, 26 blunt — the biggest single detonation |

**Notes.** Barrier is a milder reduction than VPE_Overshield (which fully deflects at L3/heat 50), so
it earns an L1 slot at heat 14. Charge is a Skip (0.02/25) plus impact + shield-refresh → heat 28.
Nova is tuned as a **repeatable combo finisher**, not a one-off capstone: at heat 40 and low
psyfocus (0.20), the Charge (28) + Nova (40) loop lands at **~68 heat** — castable in one burst by a
developed biotic, then a short cool-down. The barrier-consume ×1.5 is the reward for setting it up.

---

## Path C — Sentinel (Warp & Wards)
*Identity:* attrition + protection. Feeds detonators and keeps the squad alive.

| Lvl | Power | Cast | Range | Target | Dur | psyfocus | heat | Effect (starting numbers) |
|---|---|---|---|---|---|---|---|---|
| 1 | **Warp** [P] | 20 | 24.9 | Pawn | 300 | 0.05 | 14 | Biotic DoT ~10 HP over 5 s; **ignores armor** (anti-armor/anti-mech) |
| 1 | **Biotic Mending** | 30 | 24.9 | Pawn (ally) | 1800 | 0.10 | 12 | Heal-over-time on an ally (or self), ~15 HP over duration |
| 2 | **Reave** [P] | 20 | 12.9 | Pawn | 240 | 0.06 | 22 | 10 DoT to target **and** caster gains heal-over-time (~12 HP) |
| 2 | **Lance** | 20 | 28 | Pawn | — | 0.06 | 22 | 14 single-target biotic burst damage |
| 3 | **Dark Channel** [P] | 25 | 24.9 | Pawn | 420 | 0.08 | 30 | Strong DoT ~18; **on victim death, jumps to nearest enemy ~8** (custom comp) |
| 3 | **Backlash** | 15 | — | Self | 600 | 0.10 | 28 | Self buff: **+0.75 armor** (frontal deflect), short duration |
| 4 | **Warp Field** [P] | 25 | 24.9 | Location | 300 | 0.12 | 40 | AoE r~3.4 Warp DoT / armor-shred over the zone |
| 4 | **Biotic Ward** | 30 | 24.9 | Pawn (ally) | 1800 | 0.10 | 40 | Applies the **Barrier** hediff to a chosen ally |
| 5 | **Cluster Ward** | 30 | 24.9 | Location | 1800 | 0.30 | 55 | Applies **Barrier** to all allies in r~3.4 |
| 5 | **Barrier Sphere** | 60 | 24.9 | Location | 900 | 0.30 | 60 | Field r~3.9; allies inside gain/refresh Barrier while standing in it |

**Notes.** Warp is the L1 anti-armor primer (Liferot-priced, cheaper as a foundation). Reave = the
StealVitality lifesteal pattern (0.06/20) → self-sustain. Dark Channel's death-jump is the one power
here needing the **most bespoke C#** (a HediffComp with an on-death jump). Ward and Sphere reuse the
Vanguard Barrier hediff, priced against Overshield (0.08/50) / GuardianSkipbarrier — a reduction, not
full deflect, so Ward sits at heat 40 and the AoE Sphere at the capstone (0.30/60). See
[Buildability](#buildability--xml-vs-custom-c-verified-against-vefvpe-source) for what each power
actually requires to build.

---

## Cross-path balance summary

- **Heat budget shape:** L1 ≈ 12–16 · L2 ≈ 16–28 · L3 ≈ 30–35 · L4 ≈ 35–40 · L5 ≈ 40–70. This
  mirrors the vanilla ramp (cheap spam early, capstones you cast once per engagement). **Nova is the
  deliberate exception at 40** — a chainable combo finisher, not a one-off capstone.
- **CC costs more than damage** at equal tier (Stasis 35 vs Slam 32; Singularity 70 vs Nova 40) —
  denial is worth more than raw damage, per vanilla (RaidPause/Timequake are the priciest).
- **The amp scales rewards, not costs:** higher Psychic Sensitivity lengthens durations and DoT/CC
  effectiveness but does **not** reduce heat — so amped biotics hit harder yet still overheat,
  keeping them in check.
- **Combo layer — ✅ implemented:** primers [P] mark a target ("biotically charged", ~15 s decay);
  detonators [D] consume the mark for a 14-blunt AoE burst (r 2.4, caster-immune) centered on each
  charged victim. Priced into the *detonator*, not the primer, so combos reward setup without
  making single casts free. (`Combos.cs`, gated by `AbilityExtension_Combo` in XML.)

## Buildability — XML vs custom C# (verified against VEF/VPE source)

**Revised — much more is XML-able than the sanity check feared.** VEF ships a real reusable
`abilityClass` + extension toolbox (`Source/VEF/Abilities`): `Ability_Barrier`, `Ability_Explode`,
`Ability_ShootProjectile`, `Ability_Spawn`, plus `AbilityExtension_Stun`, `AbilityExtension_Hediff`,
`AbilityExtension_ExtraHediffs`. VPE adds `Ability_Killskip` / `Ability_PowerLeap` (teleport-strike).
So most powers are **XML by reusing an existing class**; only DoT, pull, and the field-tick need code.

| Power | Build approach | Reused class(es) |
|---|---|---|
| **Barrier**, **Biotic Ward** | **XML** — barrier/shield ability | `VEF.Abilities.Ability_Barrier` (+ target self / ally) |
| **Lift** | **XML** — hediff (cap mods) + stun | `AbilityExtension_Hediff` + `AbilityExtension_Stun` |
| **Stasis** | **XML** if simplified (long stun + incapacitate hediff); **small C#** only for true invuln/untargetable | `AbilityExtension_Stun` + `AbilityExtension_Hediff` |
| **Throw** | **XML** — biotic projectile + stagger | `Ability_ShootProjectile` + `AbilityExtension_Stun` |
| **Shockwave** | **XML** — AoE blast (approx. line as radial) + stagger | `Ability_Explode` + `AbilityExtension_Stun` |
| **Nova** | **XML** for the blast; **small C#** for the "consume Barrier ×1.5" bonus (v1.1) | `Ability_Explode` |
| **Slam** | **XML** — single-target hit + knockdown stun | `AbilityExtension_Hediff` + `AbilityExtension_Stun` |
| **Biotic Charge** | **Reuse VPE class** — teleport-strike + refresh barrier | `Ability_Killskip` / `Ability_PowerLeap` |
| **Singularity**, **Barrier Sphere** | **XML spawn** the field + **custom comp** that ticks a hediff onto pawns in radius | `Ability_Spawn` + custom `CompProperties` |
| **Warp**, **Reave** | **Custom C#** — one `HediffComp_DamageOverTime` (Reave adds self-heal) | applied via `AbilityExtension_Hediff` |
| **Dark Channel** | **Custom C#** — the DoT comp + on-death jump-to-nearest | applied via `AbilityExtension_Hediff` |
| **Pull** | **Custom C#** — pull-toward movement (interrupt/stun is XML) | `AbilityExtension_Stun` for the stun half |

**Net custom-C# footprint (small):** one shared `HediffComp_DamageOverTime` (Warp/Reave/Dark
Channel, with self-heal + jump variants), one `Pull` mover, one field `Comp` for
Singularity/Barrier Sphere, and two *optional* v1.1 bits (Stasis true-invuln, Nova barrier-consume).
Everything else is XML reusing the classes above. **This corrects the earlier "≈8 powers need code"
estimate — it's really ~3 small classes.**

**Expansion powers (to reach 10/path) buildability:**
- **XML now** (reuse existing classes): Biotic Focus, Biotic Flux, Reinforced Barrier (hediff);
  Lash, Lance (projectile); Heavy Throw, Flare (explosion); Biotic Leap (reuse `Ability_PowerLeap`);
  Heavy Charge (reuse `Ability_Killskip`); Backlash (self-armor hediff — true frontal-only deflect
  would be C#, simplified to a strong armour buff for now).
- **Custom C# (Slice 2):** Lift Grenade, Warp Field, Cluster Ward, Annihilation Field all reduce to
  one generalised **"AoE/aura that applies a hediff to pawns in radius"** comp; Biotic Mending shares
  the DoT/HoT HediffComp. So the expansion adds **no new *kinds* of custom class** — same ~3.

**First-slice recommendation:** ship the all-XML powers first (Barrier, Ward, Lift, Slam, Throw,
Stasis-simplified) to validate the path + gene + amp plumbing end-to-end, then add the ~3 C# classes.

> **Version — confirmed & decided:** the installed VEF assembly is **`VFECore.dll` in 1.5** but
> **`VEF.dll` in 1.6** — the namespace really did rename (`VFECore.Abilities.*` → `VEF.Abilities.*`).
> That split would force version-specific defs + assemblies, so **v1 targets RimWorld 1.6 only**
> (matching the 1.6-only Asari Parthenogenesis addon). 1.5 is a later, separate effort.

### Confirmed class names (for XML authoring)
- `VanillaPsycastsExpanded.AbilityExtension_Psycast` — **required** on every psycast (path/level/cost).
- `VEF.Abilities.Ability_Blank` — base ability class (effects come from extensions).
- `VEF.Abilities.Ability_Barrier`, `Ability_Explode`, `Ability_ShootProjectile`, `Ability_Spawn`.
- `VEF.Abilities.AbilityExtension_Hediff`, `AbilityExtension_ExtraHediffs`, `AbilityExtension_Stun`.
- `VanillaPsycastsExpanded.Ability_Killskip`, `Ability_PowerLeap`, `AbilityExtension_ForceJobOnTarget`.
- Reusable comps: `HediffComp_DisappearsOnDowned`, `HediffComp_SpawnMote` (VPE).

## Build setup (verified on this machine)

RimWorld Steam library is at `E:\Gry\Steam`. DLL reference paths (all confirmed present):

| Reference | Path |
|---|---|
| `Assembly-CSharp.dll` | `E:\Gry\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll` (or the `E:\Programy\Managed` copy this repo already uses) |
| `UnityEngine.CoreModule.dll` | same `Managed` folder (or `E:\Programy\Managed`) |
| `VEF.dll` (1.6) | `E:\Gry\Steam\steamapps\workshop\content\294100\2023507013\1.6\Assemblies\VEF.dll` |
| `VanillaPsycastsExpanded.dll` (1.6) | `E:\Gry\Steam\steamapps\workshop\content\294100\2842502659\1.6\Assemblies\VanillaPsycastsExpanded.dll` |
| `0Harmony.dll` | reuse the copy this repo already commits under each mod's `Assemblies/` |

- **Target: RimWorld 1.6 only** for v1 (see version note above).
- `.csproj`: old-style non-SDK, `TargetFrameworkVersion v4.7.2`, `OutputType Library`, output into the
  mod's `Assemblies\` — follow the repo pattern documented in `CLAUDE.md`. Mark the VEF/VPE/game
  references `<Private>False</Private>` (do not copy them into our `Assemblies/`).
- Only the ~3 custom classes need this project; the all-XML first slice compiles nothing.

## Decisions resolved (from sanity check)

All three sanity-check calls are **decided and applied** to the tables/text above:

1. **Nova & Slam retuned — ✅ applied.** Nova: radius r4.9 → **3.4**, heat 55 → **40**, base damage
   18 → **14**; Slam: 24 → **20**. Nova is now a repeatable combo finisher (Charge+Nova ≈ 68 heat)
   rather than an always-overloading one-off, with the barrier-consume ×1.5 as the payoff.
2. **The amp grants psylink 1 — ✅ applied.** In addition to buffing higher progression, the Biotic
   Amp grants psylink level 1, so a gene-carrying + amped pawn is immediately castable (no separate
   psylink hunt). This **supersedes** the earlier "amp only buffs, never grants" note in
   `biotics_content_plan.md`. Lore-clean: the amp is what lets a biotic channel power.
3. **Combos are cross-path only — ✅ accepted as intended.** Vanguard = all detonators, Sentinel =
   all primers, Adept = both; combos reward squad composition, and Vanguard's self-loop remains
   Charge→Barrier→Nova. No per-path primer added.

### Previously-open items — now specified
- **Gene stats** → see [Gene & Amp spec](#gene--amp-implementation-spec).
- **Amp buff magnitude** → see [Gene & Amp spec](#gene--amp-implementation-spec).
- **Effect-extension / ability class names** → confirmed above in
  [Buildability](#buildability--xml-vs-custom-c-verified-against-vefvpe-source) (with a version
  caveat to check against the installed VEF/VPE DLLs).

---

## Gene & Amp implementation spec

### `MebBioticGene` — "Eezo Nodules"
| Field | Value | Rationale |
|---|---|---|
| Kind | **Endogene** (germline, inheritable) | Natural biology; sits in the Asari xenotype and passes to children. Becomes a *xenogene* when extracted/implanted into a non-asari. |
| `biostatCpx` (Complexity) | **2** | Gates three whole psycast paths — a meaningful assembler cost. |
| `biostatMet` (Metabolism) | **0** | Neutral — the gene is a pure gate with no metabolic cost (playtest decision; the earlier "biotics eat more" −2 was dropped). |
| `biostatArc` (Archite) | 0 | Not an archite gene. |
| `geneClass` | default `Gene` | No behavior needed — it's a **gate**, enforced by the paths' `<requiredGene>`, not by the gene itself. |
| Grants stats? | **No** (psychic sensitivity comes from the amp) | Keeps gene = *potential*, amp = *power*; avoids double-dipping. *(Optional: a tiny +0.05 PsychicSensitivity for flavor.)* |
| `displayCategory` | new `GeneCategoryDef` "Biotics" | Mirrors the repo's existing `LifespanCategory.xml` pattern. |
| `exclusionTags` | none | Standalone gate, not a spectrum. |
| Icon | blue eezo-glow placeholder | Polish later. |

### `MebBioticAmp` — "Biotic Amp" implant
| Piece | Value | Notes |
|---|---|---|
| Item | `ThingDef` (Spacer implant), research-gated craft at fabrication bench (or trader-bought) | One tier only (per decision). |
| Install | `RecipeDef` on **Brain**; consumes the amp item + medicine + surgery skill | Standard implant surgery. |
| Hediff | `MebBioticAmp` on Brain | The buff carrier. |
| **Grants psylink 1** | on install, via a small `HediffComp` on the amp | On post-add, if the pawn has no psylink, add one vanilla psylink level (`Hediff_Psylink`, def `PsychicAmplifier`); VPE's `Hediff_PsycastAbilities` then treats them as a psycaster. *(To test the XML slice before this comp exists, grant psylink via dev mode.)* |
| `statOffsets` → **PsychicSensitivity +0.25** | main buff | Also raises max neural heat ~+25% (max heat = 30 × sensitivity) and lengthens every duration/CC/DoT (they scale on sensitivity) — lands the roadmap numbers where intended **without cutting cast costs**. |
| Downside | none in v1 | *(Optional flavor: a minor "biotic headache" mood/consciousness debuff, matching ME amps — deferred.)* |

**Interaction with the model:** gene = appears in the path list (`requiredGene`) + eats more;
amp = can actually cast (psylink 1) + hits ~25% harder/longer. Base Psychic Sensitivity 1.0 → 1.25
with the amp is the reference point all the cost/effect numbers above assume.
