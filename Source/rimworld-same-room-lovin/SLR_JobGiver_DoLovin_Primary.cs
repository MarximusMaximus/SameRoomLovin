using RimWorld;
using Verse;
using Verse.AI;
using System.Collections.Generic;
using System.Linq;

namespace SameRoomLovin
{
    class SRL_JobGiver_DoLovin_Primary : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            int groupChance = SRL_Settings.groupChance;


            if (Find.TickManager.TicksGame < pawn.mindState.canLovinTick)
            {
                
                return null;
            }
            if (pawn.CurrentBed() == null || pawn.CurrentBed().Medical || !pawn.health.capacities.CanBeAwake)
            {
                
                return null;
            }
            Dictionary<Pawn, Building_Bed> partnersInMyRoom = SRL_LovePartnerRelationUtility.GetPartnersInMyRoom(pawn);

            if (Rand.RangeInclusive(1, 100) > groupChance)
            {
                Pawn partnerInMyRoom = partnersInMyRoom.RandomElement().Key;
                Log.Message("SRL single: " + pawn.Name + " + " + partnerInMyRoom.Name);
                Log.Message("SRL single 2: " + pawn.Name + " + " + partnerInMyRoom.Name);
                if (partnerInMyRoom == null || !partnerInMyRoom.health.capacities.CanBeAwake || Find.TickManager.TicksGame < partnerInMyRoom.mindState.canLovinTick)
                {
                    Log.Message("SRL single 3: " + pawn.Name + " + " + partnerInMyRoom.Name);
                    return null;
                }
                Log.Message("SRL single 4: " + pawn.Name + " + " + partnerInMyRoom.Name);
                if (!pawn.CanReserve(partnerInMyRoom) || !partnerInMyRoom.CanReserve(pawn))
                {
                    Log.Message("SRL single 5: " + pawn.Name + " + " + partnerInMyRoom.Name);
                    return null;
                }
                Log.Message("SRL single 6: " + pawn.Name + " + " + partnerInMyRoom.Name);
                return JobMaker.MakeJob(SRL_JobDefOf.SRL_Lovin_Standard, partnerInMyRoom, pawn.CurrentBed());
            }
            for (int i = partnersInMyRoom.Count - 1; i >= 0; i--)
            {
                Pawn partner = partnersInMyRoom.Keys.ElementAt(i);
                if (partner == null || !partner.health.capacities.CanBeAwake || Find.TickManager.TicksGame < partner.mindState.canLovinTick || !pawn.CanReserve(partner))
                {
                    partnersInMyRoom.Remove(partner);
                }
            }
            if (partnersInMyRoom.Count > 0)
            {
                partnersInMyRoom.Add(pawn, pawn.CurrentBed());
                Find.World.GetComponent<SRL_WorldComp>().Register(pawn, partnersInMyRoom);
                return JobMaker.MakeJob(SRL_JobDefOf.SRL_Lovin_Group_Primary, pawn.CurrentBed());
            }
            return null;
        }
    }
}
