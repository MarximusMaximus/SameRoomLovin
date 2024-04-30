using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace SameRoomLovin
{
    public class JobDriver_Lovin_Standard : JobDriver
    {
        private int ticksLeft;

        private TargetIndex PartnerInd = TargetIndex.A;

        private TargetIndex BedInd = TargetIndex.B;

        private const int TicksBetweenHeartMotes = 100;

        private static float PregnancyChance = 0.05f;

        private static readonly SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve
        {
            new CurvePoint(16f, 1.5f),
            new CurvePoint(22f, 1.5f),
            new CurvePoint(30f, 4f),
            new CurvePoint(50f, 12f),
            new CurvePoint(75f, 36f)
        };

        private Pawn Partner => (Pawn)(Thing)job.GetTarget(PartnerInd);

        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            
            if (pawn.Reserve(Partner, job, 1, -1, null, errorOnFailed))
            {
                return pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed);
            }
            return false;
        }

        public override bool CanBeginNowWhileLyingDown()
        {
            return JobInBedUtility.InBedOrRestSpotNow(pawn, job.GetTarget(BedInd));
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOnDespawnedOrNull(PartnerInd);
            this.FailOn(() => !Partner.health.capacities.CanBeAwake);
            this.KeepLyingDown(BedInd);
            yield return Toils_Bed.ClaimBedIfNonMedical(BedInd);
            yield return Toils_Bed.GotoBed(BedInd);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                if (Partner.CurJob == null || Partner.CurJob.def != SRL_JobDefOf.SRL_Lovin_Standard)
                {
                    Job newJob = JobMaker.MakeJob(SRL_JobDefOf.SRL_Lovin_Standard, pawn, Partner.CurrentBed());
                    Partner.jobs.StartJob(newJob, JobCondition.InterruptForced);
                    ticksLeft = (int)(2500f * Mathf.Clamp(Rand.Range(0.1f, 1.1f), 0.1f, 2f));
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(pawn, Partner) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(pawn, Partner) && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(Partner)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(pawn.Named("BONDPAWN"), Partner.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn, Partner));
                    }
                }
                else
                {
                    ticksLeft = 9999999;
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            Toil toil2 = Toils_LayDown.LayDown(BedInd, hasBed: true, lookForOtherJobs: false, canSleep: false, gainRestAndHealth: false);
            toil2.FailOn(() => Partner.CurJob == null || Partner.CurJob.def != SRL_JobDefOf.SRL_Lovin_Standard);
            toil2.AddPreTickAction(delegate
            {
                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            });
            toil2.AddFinishAction(delegate
            {
                Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                if ((base.pawn.health != null && base.pawn.health.hediffSet != null && base.pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (Partner.health != null && Partner.health.hediffSet != null && Partner.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                {
                    thought_Memory.moodPowerFactor = 1.5f;
                }
                if (base.pawn.needs.mood != null)
                {
                    base.pawn.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, Partner);
                }
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, base.pawn.Named(HistoryEventArgsNames.Doer)));
                HistoryEventDef def = (base.pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, Partner) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, base.pawn.Named(HistoryEventArgsNames.Doer)));
                base.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextLovin(base.pawn);
                if (ModsConfig.BiotechActive)
                {
                    Pawn pawn = ((base.pawn.gender == Gender.Male) ? base.pawn : ((Partner.gender == Gender.Male) ? Partner : null));
                    Pawn pawn2 = ((base.pawn.gender == Gender.Female) ? base.pawn : ((Partner.gender == Gender.Female) ? Partner : null));
                    if (pawn != null && pawn2 != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                    {
                        Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2);
                        hediff_Pregnant.SetParents(null, pawn, PregnancyUtility.GetInheritedGeneSet(pawn, pawn2));
                        pawn2.health.AddHediff(hediff_Pregnant);
                    }
                }
            });
            toil2.socialMode = RandomSocialMode.Off;
            yield return toil2;
        }

        private int GenerateRandomMinTicksToNextLovin(Pawn pawn)
        {
            if (DebugSettings.alwaysDoLovin)
            {
                return 100;
            }
            float num = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            return (int)(num * 2500f);
        }
    }


    public class JobDriver_Lovin_Group_Primary : JobDriver
    {
        private int ticksLeft;

        private TargetIndex BedInd = TargetIndex.A;

        private const int TicksBetweenHeartMotes = 100;

        private static float PregnancyChance = 0.05f;

        private static readonly SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve
        {
            new CurvePoint(16f, 1.5f),
            new CurvePoint(22f, 1.5f),
            new CurvePoint(30f, 4f),
            new CurvePoint(50f, 12f),
            new CurvePoint(75f, 36f)
        };
        
        private Dictionary<Pawn, Building_Bed> partners = new Dictionary<Pawn, Building_Bed>();
        private Dictionary<Pawn, Building_Bed> confirmedParticipants = new Dictionary<Pawn, Building_Bed>();

        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {

            
            if (pawn.Reserve(Bed, job, Bed.SleepingSlotsCount, 0, null, errorOnFailed))
            {
                partners = new Dictionary<Pawn, Building_Bed>();
                partners = Find.World.GetComponent<SRL_WorldComp>().SRL_group_list[pawn];

                confirmedParticipants = new Dictionary<Pawn, Building_Bed>();
                confirmedParticipants.Add(pawn, Bed);
                foreach (KeyValuePair<Pawn, Building_Bed> partner in partners)
                {
                    if(partner.Key == pawn)
                    {
                        continue;
                    }
                    if(pawn.Reserve(partner.Key, job, 1, -1, null, errorOnFailed))
                    {
                        
                        confirmedParticipants.Add(partner.Key, partner.Value);
                    }
                }
            }
            if(confirmedParticipants.Count > 1)
            {
                Find.World.GetComponent<SRL_WorldComp>().Deregister(pawn);
                Find.World.GetComponent<SRL_WorldComp>().Register(pawn, confirmedParticipants);
                return true;
            }
            return false;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            List<Pawn> confirmedParticipantsList = new List<Pawn>();
            foreach(KeyValuePair<Pawn, Building_Bed> p in confirmedParticipants)
            {
                confirmedParticipantsList.Add(p.Key);
            }
            this.FailOnDespawnedOrNull(BedInd);
            this.FailOn(() => confirmedParticipantsList.Any((Pawn p) => !p.health.capacities.CanBeAwake));
            this.KeepLyingDown(BedInd);
            yield return Toils_Bed.ClaimBedIfNonMedical(BedInd);
            yield return Toils_Bed.GotoBed(BedInd);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                foreach (KeyValuePair<Pawn, Building_Bed> partner in confirmedParticipants)
                {
                    if (partner.Key == pawn)
                    {
                        continue;
                    }
                    if (partner.Key.CurJob == null || partner.Key.CurJob.def != SRL_JobDefOf.SRL_Lovin_Group_Secondary)
                    {
                        Job newJob = JobMaker.MakeJob(SRL_JobDefOf.SRL_Lovin_Group_Secondary, pawn, partner.Value);
                        partner.Key.jobs.StartJob(newJob, JobCondition.InterruptForced);
                        ticksLeft = (int)(2500f * Mathf.Clamp(Rand.Range(0.1f, 1.1f), 0.1f, 2f));
                        Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));
                        if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(pawn, partner.Key) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(pawn, partner.Key) && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(partner.Key)))
                        {
                            Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(pawn.Named("BONDPAWN"), partner.Key.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn, partner.Key));
                        }
                    }
                    else
                    {
                        ticksLeft = 9999999;
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            foreach (KeyValuePair<Pawn, Building_Bed> partner in confirmedParticipants)
            {
                if(partner.Key == pawn)
                {
                    continue;
                }
                
                Toil toil2 = Toils_LayDown.LayDown(BedInd, hasBed: true, lookForOtherJobs: false, canSleep: false, gainRestAndHealth: false);
                
                toil2.FailOn(() => partner.Key.CurJob == null || partner.Key.CurJob.def != SRL_JobDefOf.SRL_Lovin_Group_Secondary);
                
                toil2.AddPreTickAction(delegate
                {
                    
                    
                    ticksLeft--;
                    if (ticksLeft <= 0)
                    {
                        ReadyForNextToil();
                    }
                    else if (pawn.IsHashIntervalTick(100))
                    {
                        FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                    }
                });
                
                toil2.AddFinishAction(delegate
                {
                    Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                    if ((base.pawn.health != null && base.pawn.health.hediffSet != null && base.pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (partner.Key.health != null && partner.Key.health.hediffSet != null && partner.Key.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        thought_Memory.moodPowerFactor = 1.5f;
                    }
                    if (base.pawn.needs.mood != null)
                    {
                        base.pawn.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, partner.Key);
                    }
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, base.pawn.Named(HistoryEventArgsNames.Doer)));
                    HistoryEventDef def = (base.pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner.Key) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, base.pawn.Named(HistoryEventArgsNames.Doer)));
                    base.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextLovin(base.pawn);
                    if (ModsConfig.BiotechActive)
                    {
                        Pawn pawn = ((base.pawn.gender == Gender.Male) ? base.pawn : ((partner.Key.gender == Gender.Male) ? partner.Key : null));
                        Pawn pawn2 = ((base.pawn.gender == Gender.Female) ? base.pawn : ((partner.Key.gender == Gender.Female) ? partner.Key : null));
                        if (pawn != null && pawn2 != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                        {
                            Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2);
                            hediff_Pregnant.SetParents(null, pawn, PregnancyUtility.GetInheritedGeneSet(pawn, pawn2));
                            pawn2.health.AddHediff(hediff_Pregnant);
                        }
                    }
                });
                toil2.socialMode = RandomSocialMode.Off;
                
                yield return toil2;
            }
        }

        private int GenerateRandomMinTicksToNextLovin(Pawn pawn)
        {
            if (DebugSettings.alwaysDoLovin)
            {
                return 100;
            }
            float num = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            return (int)(num * 2500f);
        }
    }

    public class JobDriver_Lovin_Group_Secondary : JobDriver
    {
        private int ticksLeft;

        private TargetIndex InitiatorInd = TargetIndex.A;

        private TargetIndex BedInd = TargetIndex.B;

        private const int TicksBetweenHeartMotes = 100;

        private static float PregnancyChance = 0.05f;

        private static readonly SimpleCurve LovinIntervalHoursFromAgeCurve = new SimpleCurve
        {
            new CurvePoint(16f, 1.5f),
            new CurvePoint(22f, 1.5f),
            new CurvePoint(30f, 4f),
            new CurvePoint(50f, 12f),
            new CurvePoint(75f, 36f)
        };
        private Pawn initiator => (Pawn)(Thing)job.GetTarget(InitiatorInd);

        private Dictionary<Pawn, Building_Bed> partners = new Dictionary<Pawn, Building_Bed>();

        private Building_Bed Bed => (Building_Bed)(Thing)job.GetTarget(BedInd);

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksLeft, "ticksLeft", 0);
        }

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            partners = new Dictionary<Pawn, Building_Bed>();
            partners = Find.World.GetComponent<SRL_WorldComp>().SRL_group_list[initiator];
            this.FailOnDespawnedOrNull(BedInd);
            this.KeepLyingDown(BedInd);
            yield return Toils_Bed.ClaimBedIfNonMedical(BedInd);
            yield return Toils_Bed.GotoBed(BedInd);
            Toil toil = ToilMaker.MakeToil("MakeNewToils");
            toil.initAction = delegate
            {
                if(initiator.CurJob.def != null && initiator.CurJob.def == SRL_JobDefOf.SRL_Lovin_Group_Primary)
                {
                    ticksLeft = 9999999;
                }
                else
                {
                    Job newJob = JobMaker.MakeJob(SRL_JobDefOf.SRL_Lovin_Group_Primary, partners[initiator]);
                    initiator.jobs.StartJob(newJob, JobCondition.InterruptForced);
                    ticksLeft = (int)(2500f * Mathf.Clamp(Rand.Range(0.1f, 1.1f), 0.1f, 2f));
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.InitiatedLovin, pawn.Named(HistoryEventArgsNames.Doer)));

                }
                foreach (KeyValuePair<Pawn, Building_Bed> partner in partners)
                {
                    if (partner.Key == base.pawn)
                    {
                        continue;
                    }
                    if (InteractionWorker_RomanceAttempt.CanCreatePsychicBondBetween(pawn, partner.Key) && InteractionWorker_RomanceAttempt.TryCreatePsychicBondBetween(pawn, partner.Key) && (PawnUtility.ShouldSendNotificationAbout(pawn) || PawnUtility.ShouldSendNotificationAbout(partner.Key)))
                    {
                        Find.LetterStack.ReceiveLetter("LetterPsychicBondCreatedLovinLabel".Translate(), "LetterPsychicBondCreatedLovinText".Translate(pawn.Named("BONDPAWN"), partner.Key.Named("OTHERPAWN")), LetterDefOf.PositiveEvent, new LookTargets(pawn, partner.Key));
                    }
                }
            };
            toil.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return toil;
            

            Toil toil2 = Toils_LayDown.LayDown(BedInd, hasBed: true, lookForOtherJobs: false, canSleep: false, gainRestAndHealth: false);
            toil2.FailOn(() => initiator.CurJob == null || (initiator.CurJob.def != SRL_JobDefOf.SRL_Lovin_Group_Secondary && initiator.CurJob.def != SRL_JobDefOf.SRL_Lovin_Group_Primary));
            toil2.AddPreTickAction(delegate
            {
                ticksLeft--;
                if (ticksLeft <= 0)
                {
                    ReadyForNextToil();
                }
                else if (pawn.IsHashIntervalTick(100))
                {
                    FleckMaker.ThrowMetaIcon(pawn.Position, pawn.Map, FleckDefOf.Heart);
                }
            });
            toil2.AddFinishAction(delegate
            {
                foreach (KeyValuePair<Pawn, Building_Bed> partner in partners)
                {
                    if(partner.Key == base.pawn)
                    {
                        continue;
                    }
                    Thought_Memory thought_Memory = (Thought_Memory)ThoughtMaker.MakeThought(ThoughtDefOf.GotSomeLovin);
                    if ((base.pawn.health != null && base.pawn.health.hediffSet != null && base.pawn.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)) || (partner.Key.health != null && partner.Key.health.hediffSet != null && partner.Key.health.hediffSet.hediffs.Any((Hediff h) => h.def == HediffDefOf.LoveEnhancer)))
                    {
                        thought_Memory.moodPowerFactor = 1.5f;
                    }
                    if (base.pawn.needs.mood != null)
                    {
                        base.pawn.needs.mood.thoughts.memories.TryGainMemory(thought_Memory, partner.Key);
                    }
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(HistoryEventDefOf.GotLovin, base.pawn.Named(HistoryEventArgsNames.Doer)));
                    HistoryEventDef def = (base.pawn.relations.DirectRelationExists(PawnRelationDefOf.Spouse, partner.Key) ? HistoryEventDefOf.GotLovin_Spouse : HistoryEventDefOf.GotLovin_NonSpouse);
                    Find.HistoryEventsManager.RecordEvent(new HistoryEvent(def, base.pawn.Named(HistoryEventArgsNames.Doer)));
                    base.pawn.mindState.canLovinTick = Find.TickManager.TicksGame + GenerateRandomMinTicksToNextLovin(base.pawn);
                    if (ModsConfig.BiotechActive)
                    {
                        Pawn pawn = ((base.pawn.gender == Gender.Male) ? base.pawn : ((partner.Key.gender == Gender.Male) ? partner.Key : null));
                        Pawn pawn2 = ((base.pawn.gender == Gender.Female) ? base.pawn : ((partner.Key.gender == Gender.Female) ? partner.Key : null));
                        if (pawn != null && pawn2 != null && Rand.Chance(PregnancyChance * PregnancyUtility.PregnancyChanceForPartners(pawn2, pawn)))
                        {
                            Hediff_Pregnant hediff_Pregnant = (Hediff_Pregnant)HediffMaker.MakeHediff(HediffDefOf.PregnantHuman, pawn2);
                            hediff_Pregnant.SetParents(null, pawn, PregnancyUtility.GetInheritedGeneSet(pawn, pawn2));
                            pawn2.health.AddHediff(hediff_Pregnant);
                        }
                    }
                }
            });
            toil2.socialMode = RandomSocialMode.Off;
            yield return toil2;
        }

        private int GenerateRandomMinTicksToNextLovin(Pawn pawn)
        {
            if (DebugSettings.alwaysDoLovin)
            {
                return 100;
            }
            float num = LovinIntervalHoursFromAgeCurve.Evaluate(pawn.ageTracker.AgeBiologicalYearsFloat);
            if (ModsConfig.BiotechActive && pawn.genes != null)
            {
                foreach (Gene item in pawn.genes.GenesListForReading)
                {
                    num *= item.def.lovinMTBFactor;
                }
            }
            num = Rand.Gaussian(num, 0.3f);
            if (num < 0.5f)
            {
                num = 0.5f;
            }
            return (int)(num * 2500f);
        }
    }
}
