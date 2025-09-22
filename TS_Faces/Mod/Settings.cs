using System;
using System.Collections.Generic;
using TS_Faces.Data;
using TS_Faces.Util;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Mod;

public class FacesSettings : ModSettings
{
	public const int COMP_UPDATE_INTERVAL = 30;

	private static Lazy<FacesSettings> _Instance = new(TSFacesMod.Instance.GetSettings<FacesSettings>);
	public static FacesSettings Instance => _Instance.Value;


	// Saved variables
	public float CompUpdateInterval = COMP_UPDATE_INTERVAL;
	public bool StrictGender = false;
	public bool StrictBeauty = false;
	public HashSet<SlotDef> ForcedFloatingSlots = [];

	// Non-Saved variables
	public Dictionary<string, string> EditBuffers = [];

	public override void ExposeData()
	{
		Scribe_Values.Look(ref CompUpdateInterval, "compinterval", COMP_UPDATE_INTERVAL);
		Scribe_Values.Look(ref StrictGender, "strictgender", false);
		Scribe_Values.Look(ref StrictBeauty, "strictbeauty", false);
		Scribe_Collections.Look(ref ForcedFloatingSlots, "forcedfloat");
	}

	public void DrawSettings(Rect rect)
	{
		using var list = new TSUtil.Listing_D(rect);
		list.Listing.GapLine();

		var (update_lab, update_desc) = "compupdate interval".ModLabelDesc();
		list.Listing.SliderLabeledWithValue(
			ref CompUpdateInterval,
			update_lab,
			1, 300,
			EditBuffers,
			tt: update_desc,
			resetval: COMP_UPDATE_INTERVAL
		);

		var (gender_lab, gender_desc) = "strict gender".ModLabelDesc();
		list.Listing.CheckboxLabeled(gender_lab, ref StrictGender, gender_desc);
		var (beauty_lab, beauty_desc) = "strict beauty".ModLabelDesc();
		list.Listing.CheckboxLabeled(beauty_lab, ref StrictBeauty, beauty_desc);

		list.Listing.GapLine();

		{ // handle eyebrow floating (yes this could be automated for all slots)
			var floating = ForcedFloatingSlots.Contains(SlotDefOf.Brow);
			list.Listing.CheckboxLabeled("float brows".ModTranslate(), ref floating);
			if (floating)
				ForcedFloatingSlots.Add(SlotDefOf.Brow);
			else
				ForcedFloatingSlots.Remove(SlotDefOf.Brow);
		}

		{ // handle eye floating
			var floating = ForcedFloatingSlots.Contains(SlotDefOf.Eye);
			list.Listing.CheckboxLabeled("float eye".ModTranslate(), ref floating);
			if (floating)
				ForcedFloatingSlots.Add(SlotDefOf.Eye);
			else
				ForcedFloatingSlots.Remove(SlotDefOf.Eye);
		}
	}
}