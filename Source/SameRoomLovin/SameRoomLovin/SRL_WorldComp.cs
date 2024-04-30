using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Collections.Generic;

namespace SameRoomLovin
{
    public class SRL_WorldComp : WorldComponent
    {
        public Dictionary<Pawn, Dictionary<Pawn, Building_Bed>> SRL_group_list = new Dictionary<Pawn, Dictionary<Pawn, Building_Bed>>();

        public SRL_WorldComp(World world) : base(world)
        {
        }

        public void Register(Pawn p, Dictionary<Pawn, Building_Bed> dict)
        {
            if (SRL_group_list.ContainsKey(p))
            {
                Deregister(p);
            }
            SRL_group_list.Add(p, dict);
        }

        public void Deregister(Pawn p)
        {
            SRL_group_list.Remove(p);
        }
    }
}