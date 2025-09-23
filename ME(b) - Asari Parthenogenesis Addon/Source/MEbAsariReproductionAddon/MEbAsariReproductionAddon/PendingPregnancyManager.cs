using System;
using System.Collections.Generic;
using Verse;
using RimWorld;

namespace MEbAsariReproductionAddon
{
    // Small data holder
    public class PendingPregnancy
    {
        public Pawn mother;
        public Pawn father;
        public int queuedTick;

        public PendingPregnancy(Pawn mother, Pawn father)
        {
            this.mother = mother;
            this.father = father;
            this.queuedTick = Find.TickManager.TicksGame;
        }
    }

    // GameComponent that applies queued pregnancies on ticks (safe context)
    public class PendingPregnancyManager : GameComponent
    {
        private static List<PendingPregnancy> pending = new List<PendingPregnancy>();

        // Track last attempt tick per pawn (thingID -> tick)
        private static Dictionary<int, int> lastAttemptTickByPawn = new Dictionary<int, int>();

        // Cooldown in ticks during which additional attempts for same pawn are ignored.
        private const int AttemptCooldownTicks = 2500;

        // Verbose logging only when Dev Mode is enabled
        private static bool Verbose => Prefs.DevMode;

        public PendingPregnancyManager(Game game) : base()
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();

            if (pending.Count == 0) return;

            List<PendingPregnancy> copy;
            lock (pending)
            {
                copy = new List<PendingPregnancy>(pending);
                pending.Clear();
            }

            foreach (var p in copy)
            {
                try
                {
                    if (p?.mother == null) continue;
                    if (p.mother.Dead) continue;
                    if (PregnancyUtility.GetPregnancyHediff(p.mother) != null) continue;

                    HediffDef pregDef = HediffDefOf.PregnantHuman ?? DefDatabase<HediffDef>.GetNamedSilentFail("Pregnant");
                    if (pregDef == null)
                    {
                        Log.Error("[MEbAsariReproductionAddon] PendingPregnancyManager: No Pregnant hediff found.");
                        continue;
                    }

                    var hediff = HediffMaker.MakeHediff(pregDef, p.mother, null);
                    p.mother.health.AddHediff(hediff);

                    // Player-facing notification: keep this so players see births triggered
                    //Messages.Message($"{p.mother.LabelShort} became pregnant via {p.father?.LabelShort ?? "unknown"}.", MessageTypeDefOf.PositiveEvent);

                    // Internal log only in Dev Mode to avoid console spam
                    if (Verbose)
                        Log.Message($"[MEbAsariReproductionAddon] Applied queued pregnancy to {p.mother.LabelShort} (father: {p.father?.LabelShort}).");
                }
                catch (Exception ex)
                {
                    Log.Error("[MEbAsariReproductionAddon] Exception applying queued pregnancy: " + ex);
                }
            }
        }

        // API to queue pregnancy
        public static void QueuePregnancy(Pawn mother, Pawn father)
        {
            if (mother == null) return;

            int id = mother.thingIDNumber; // stable per-Thing id
            int now = Find.TickManager.TicksGame;

            lock (pending)
            {
                // cooldown check: if last attempt was recent, skip
                if (lastAttemptTickByPawn.TryGetValue(id, out int lastTick))
                {
                    if (now - lastTick < AttemptCooldownTicks)
                    {
                        if (Verbose)
                            Log.Message($"[MEbAsariReproductionAddon] Skipping queue for {mother.LabelShort}: cooldown active ({now - lastTick} ticks since last attempt).");
                        return;
                    }
                }

                // Skip if already queued
                if (pending.Exists(x => x.mother == mother))
                {
                    if (Verbose)
                        Log.Message($"[MEbAsariReproductionAddon] {mother.LabelShort} already queued for pregnancy; skipping duplicate queue.");
                    // Update last attempt tick so further attempts within cooldown will be suppressed
                    lastAttemptTickByPawn[id] = now;
                    return;
                }

                pending.Add(new PendingPregnancy(mother, father));
                lastAttemptTickByPawn[id] = now;
                if (Verbose)
                    Log.Message($"[MEbAsariReproductionAddon] Queued pregnancy for {mother.LabelShort} (father: {father?.LabelShort}).");
            }
        }

        public static bool IsQueued(Pawn mother)
        {
            if (mother == null) return false;
            lock (pending)
            {
                return pending.Exists(x => x.mother == mother);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
