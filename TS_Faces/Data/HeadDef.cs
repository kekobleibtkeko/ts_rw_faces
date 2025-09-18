using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using TS_Faces.Util;
using Verse;

namespace TS_Faces.Data;

[DefOf]
public static class HeadDefOf
{
	public static HeadDef AverageLong = default!;
	
	static HeadDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(HeadDefOf));
	}
}

public class HeadDef : Def, IPawnFilterable, IComparable<HeadDef>
{
	[NoTranslate]
	public string graphicPath = "";

	public FaceLayoutDef faceLayout = default!;
	public string? shader;
	public PartColor color = PartColor.Skin;

	public int commonality = 10;
	public List<PawnFilterEntry> filters = [];

	IEnumerable<PawnFilterEntry> IPawnFilterable.FilterEntries => filters;
	int IPawnFilterable.Commonality => commonality;

	public int CompareTo(HeadDef other) => string.Compare(defName, other.defName);

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		faceLayout ??= FaceLayoutDefOf.Long;

		// Log.Message($"filters for head {this}:");
		// Log.Message(string.Join("\n", filters)); 

		// Log.Message($"slots for {this}:");
		// foreach (var rot in Rot4.AllRotations)
		// {
		// 	Log.Message($"   {rot.ToStringHuman()}:");
		// 	faceLayout.ForRot(rot).Parts.Do(part =>
		// 	{
		// 		Log.Message($"      slot: {part.slot},  side: {part.side},  pos: {part.pos},  rotation: {part.rotation}");
		// 	});
		// }
	}
}
