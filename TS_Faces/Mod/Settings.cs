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
	private static Lazy<FacesSettings> _Instance = new(TSFacesMod.Instance.GetSettings<FacesSettings>);
	public static FacesSettings Instance => _Instance.Value;


	// Saved variables
	public bool StrictGender = false;
	public bool StrictBeauty = false;
	public HashSet<SlotDef> ForcedFloatingSlots = [];

	public override void ExposeData()
	{
		Scribe_Values.Look(ref StrictGender, "strictgender");
		Scribe_Values.Look(ref StrictBeauty, "strictbeauty");
		Scribe_Collections.Look(ref ForcedFloatingSlots, "forcedfloat");
	}

	public void DrawSettings(Rect rect)
	{
		using var list = new TSUtil.Listing_D(rect);
		list.Listing.GapLine();

		list.Listing.CheckboxLabeled("strict gender".ModTranslate(), ref StrictGender);
		list.Listing.CheckboxLabeled("strict beauty".ModTranslate(), ref StrictBeauty);

		list.Listing.GapLine();

		{ // handle eyebrow floating
			var floating = ForcedFloatingSlots.Contains(SlotDefOf.Brow);
			list.Listing.CheckboxLabeled("float brows".ModTranslate(), ref floating);
			if (floating)
				ForcedFloatingSlots.Add(SlotDefOf.Brow);
			else
				ForcedFloatingSlots.Remove(SlotDefOf.Brow);
		}
	}
}