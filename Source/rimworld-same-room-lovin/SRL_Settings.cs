using System;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace SameRoomLovin
{

    public class SRL_Settings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public static int groupChance = 5;

        public void DoWindowContents(Rect canvas)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(canvas);
            listingStandard.Label("Group Lovin' Chance: " + groupChance + "%");
            groupChance = (int)listingStandard.Slider((float)groupChance, 0f, 100f);
            listingStandard.End();
        }

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref groupChance, "SRL_group_chance");
            base.ExposeData();
        }
    }

    public class SRL_Mod : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        public SRL_Settings settings;

        public static int groupChance = 5;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public SRL_Mod(ModContentPack content) : base(content)
        {
            this.settings = GetSettings<SRL_Settings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect canvas)
        {
            settings.DoWindowContents(canvas);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "SameRoomLovin";//.Translate();
        }
    }
}