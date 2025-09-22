using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using TS_Faces.Comps;
using TS_Lib.Transforms;
using TS_Lib.Util;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace TS_Faces.Data;

public class FacePartModifierDef : Def, IPawnFilterable, IComparable<FacePartModifierDef>
{
	public SlotDef slot = default!;
	public int commonality = 10;
	public TransformModifier transforms = new();

	public List<PawnFilterEntry> filters = [];
	public List<PawnFilterDef> filterDefs = [];

	IEnumerable<IPawnFilterEntry> IPawnFilterable.FilterEntries => Enumerable.Empty<IPawnFilterEntry>()
		.Concat(filters)
		.Concat(filterDefs)
	;
	int IPawnFilterable.Commonality => commonality;

	public void ApplyTo(Comp_TSFace face, int? seed)
	{
		if (!face.TryGetSidedPartForSlot(slot, out var sided))
			return;

		ApplyTo(sided, seed);
	}

	public void ApplyTo(TSTransform4 tr, int? seed)
	{
		transforms.Collapse(seed).ApplyTo(tr);
	}

	public void ApplyTo(SidedTRFaceParts sided, int? seed)
	{
		sided.Transform
			.ApplyTransform(
				transforms.Collapse(seed).ToTransform(),
				transforms.mirror == TransformModifier.MirrorType.Mirrored
			)
		;
	}

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		slot ??= SlotDefOf.Eye;
	}
	public int CompareTo(FacePartModifierDef other) => defName.CompareTo(other.defName);
}