using RimWorld;
using Verse;
using Verse.AI;
using System.Linq;
using System.Collections.Generic;

namespace SameRoomLovin
{
    public class SRL_ThinkNode_ChancePerHour : ThinkNode_ChancePerHour
    {
        protected override float MtbHours(Pawn pawn)
        {
            
            if (pawn.CurrentBed() == null)
            {
                
                return -1f;
            }
            Dictionary<Pawn, Building_Bed> partnersInMyRoom = SRL_LovePartnerRelationUtility.GetPartnersInMyRoom(pawn);
            if (partnersInMyRoom == null)
            {
                
                return -1f;
            }
            Pawn firstPartner = partnersInMyRoom.First().Key;
            return LovePartnerRelationUtility.GetLovinMtbHours(pawn, firstPartner);
        }
    }
}
