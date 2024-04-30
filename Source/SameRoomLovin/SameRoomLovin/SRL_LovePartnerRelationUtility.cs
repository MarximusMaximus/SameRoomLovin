using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace SameRoomLovin
{
    public static class SRL_LovePartnerRelationUtility
    {
        public static Dictionary<Pawn, Building_Bed> GetPartnersInMyRoom(Pawn pawn)
        {
            
            Building_Bed building_Bed = pawn.CurrentBed();
            if (building_Bed == null)
            {
                
                return null;
            }
            if (!LovePartnerRelationUtility.HasAnyLovePartner(pawn))
            {
                
                return null;
            }
            Room room = pawn.CurrentBed().GetRoom();
            if (room == null)
            {
                
                return null;
            }
            IEnumerable<Building_Bed> RoomBeds = room.ContainedBeds;
            Dictionary<Pawn, Building_Bed> curOccupants = new Dictionary<Pawn, Building_Bed>();
            foreach (Building_Bed bed in RoomBeds)
            {
                
                foreach (Pawn curOccupant in bed.CurOccupants)
                {
                    
                    if (curOccupant != pawn && LovePartnerRelationUtility.LovePartnerRelationExists(pawn, curOccupant))
                    {
                        
                        curOccupants.Add(curOccupant, bed);
                    }
                }
            }
            if(curOccupants.Count == 0)
            {
                
                return null;
            }
            
            return curOccupants;
        }
    }
}
