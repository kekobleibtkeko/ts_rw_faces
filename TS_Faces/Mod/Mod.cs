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