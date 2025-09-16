using System;
using System.Collections.Generic;
using TS_Faces.Data;
using TS_Faces.Util;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Mod;

[StaticConstructorOnStartup]
public static class TSFacesStartup
{
	static TSFacesStartup()
	{
		Harmony.Patcher.Patch();
		TSFacesMod.Logger.Info($"[TS] Faces loaded, settings hash: {FacesSettings.Instance.GetHashCode()}"); // getting hash to init settings
	}
}

public class TSFacesMod : Verse.Mod
{
	public const string ID = "[TS] Faces";
	public const string ModID = "tsuyao.ts_rw_faces";
	public static TSFacesMod Instance = default!;
	public static TSLogger Logger = new("Faces", TSLogger.Level.Verbose);
	public TSFacesMod(ModContentPack content) : base(content)
	{
		Instance = this;
	}

	public override string SettingsCategory() => ID;
	public override void DoSettingsWindowContents(Rect inRect) => FacesSettings.Instance.DrawSettings(inRect);
}

public class FacesSettings : ModSettings
{
	private static Lazy<FacesSettings> _Instance = new(TSFacesMod.Instance.GetSettings<FacesSettings>);
	public static FacesSettings Instance => _Instance.Value;


	// Saved variables
	public bool StrictGender = false;
	public bool StrictBeauty = false;
	public List<SlotDef> ForcedFloatingSlots = [];

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
			var floating = ForcedFloatingSlots.Contains(SlotDefOf.B)
		}
	}
}