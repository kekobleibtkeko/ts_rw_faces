using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;

namespace TS_Faces.Data;

public class ExtraPartDef : Def, IPawnFilterable, IComparable<ExtraPartDef>
{
	public enum PartDetail
	{
		Primary,
		Secondary,
		Tertiary,
	}

	public FacePartDef def = default!;
	public PartDetail detail = PartDetail.Primary;
	public TransformModifier transforms = new();
	public SlotDef anchor = default!;
	public FaceSide side = FaceSide.None;
	public bool mirror = false;
	public bool force = false;
	public bool skip = false;

	public int commonality = 10;
	public List<PawnFilterEntry> filters = [];
	public List<PawnFilterDef> filterDefs = [];

	IEnumerable<IPawnFilterEntry> IPawnFilterable.FilterEntries => Enumerable.Empty<IPawnFilterEntry>()
		.Concat(filters)
		.Concat(filterDefs)
	;

	int IPawnFilterable.Commonality => commonality;

	public override void ResolveReferences()
	{
		base.ResolveReferences();
		def ??= FacePartDefOf.Empty;
		anchor ??= SlotDefOf.None;
	}

	public int CompareTo(ExtraPartDef other) => defName.CompareTo(other.defName);
}