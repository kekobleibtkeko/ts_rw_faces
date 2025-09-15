using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

public class FaceLayoutPart
{
	public SlotDef slot = default!;
	public FaceSide side;
	public Vector2 pos;
	public float rotation;

	public static FaceLayoutPart Mirrored(FaceLayoutPart part)
	{
		return new FaceLayoutPart
		{
			slot = part.slot,
			side = part.side.Mirror(),
			pos = part.pos * new Vector2(-1, 1),
			rotation = 360 - part.rotation
		};
	}

	public void ResolveReferences()
	{
		slot ??= SlotDefOf.Eye;
	}
}

public class FaceLayout
{
	public List<FaceLayoutPart> east = [];
	public List<FaceLayoutPart> south = [];
	public List<FaceLayoutPart>? north;
	public List<FaceLayoutPart>? west;

	public List<FaceLayoutPart> MirrorSide(IEnumerable<FaceLayoutPart> parts)
	{
		return [.. parts.Select(FaceLayoutPart.Mirrored)];
	}

	public IEnumerable<FaceLayoutPart> ForRot(Rot4 rot) => rot.AsInt switch
	{
		0 => north ?? Enumerable.Empty<FaceLayoutPart>(),
		1 => east,
		2 => south,
		3 => west ??= MirrorSide(east),
		_ => south,
	};

	public void ResolveReferences()
	{
		Rot4.AllRotations.Do(rot => ForRot(rot).Do(layout => layout.ResolveReferences()));
	}
}
