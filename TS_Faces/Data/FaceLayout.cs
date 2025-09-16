using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using TS_Faces.Util;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

public class FaceLayoutPart : IMirrorable<FaceLayoutPart>
{
	public SlotDef slot = default!;
	public FaceSide side;
	public Vector2 pos;
	public float rotation;

	public static FaceLayoutPart Mirrored(FaceLayoutPart part) => part.Mirror();

	public FaceLayoutPart Mirror() => new()
	{
		slot = slot,
		side = side.Mirror(),
		pos = pos * new Vector2(-1, 1),
		rotation = 360 - rotation
	};

	public void ResolveReferences()
	{
		slot ??= SlotDefOf.Eye;
	}
}

public struct FaceLayoutSide(IEnumerable<FaceLayoutPart> parts) : IMirrorable<FaceLayoutSide>
{
	public IEnumerable<FaceLayoutPart> Parts = parts;

	public FaceLayoutSide() : this([]) { }

	public readonly FaceLayoutSide Mirror() => new(Parts.Select(FaceLayoutPart.Mirrored));
}

public class FaceLayout
{
	public List<FaceLayoutPart> east = [];
	public List<FaceLayoutPart> south = [];
	public List<FaceLayoutPart>? north;
	public List<FaceLayoutPart>? west;

	private FaceLayoutSide EastLayout;
	private FaceLayoutSide SouthLayout;
	private FaceLayoutSide NorthLayout;
	private FaceLayoutSide WestLayout;

	public FaceLayoutSide ForRot(Rot4 rot) => rot.AsInt switch
	{
		0 => NorthLayout,
		1 => EastLayout,
		2 => SouthLayout,
		3 => WestLayout,
		_ => default,
	};

	public void ResolveReferences()
	{
		EastLayout = new(east);
		SouthLayout = new(south);
		NorthLayout = new(north ?? []);
		WestLayout = new(west ?? EastLayout.Mirror().Parts);

		Rot4.AllRotations.Do(rot => ForRot(rot).Parts.Do(layout => layout.ResolveReferences()));
	}
}
