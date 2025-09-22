using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Faces.Comps;
using TS_Faces.Mod;
using TS_Faces.Util;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

[DefOf]
public static class FacePartDefOf
{
	public static FacePartDef Empty = default!;

	// public static FacePartDef Eye1 = default!;
	// public static FacePartDef Iris1 = default!;
	// public static FacePartDef Mouth1 = default!;
	// public static FacePartDef NoseShiny = default!;

	static FacePartDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(FacePartDefOf));
	}
}

public class FacePartDef : Def,
	IPawnFilterable,
	IComparable<FacePartDef>,
	ICreateCopy<FacePartDef> // needed to clone sideables, doesn't actually create a copy
{
	public SlotDef slot = default!;

	public string? shader;
	public PartColor color = PartColor.None;
	public Color? customColor;
	public bool floating = false;
	// base graphic path, changable by workers below
	public string graphicPath = "";
	public List<FacePartWorkerPropsBase> stateProps = [];
	
	public bool noMirror = false;
	public Vector2 drawSize = Vector2.one;
	public Vector2 offset = Vector2.zero;
	public float rotation;
	public float layerOffset = 0;
	public bool undoSlotOffset = false;

	// only for eye + iris
	public Vector2 highlightOffset = Vector2.zero;

	public int commonality = 10;
	public List<PawnFilterEntry> filters = [];
	public List<PawnFilterDef> filterDefs = [];

	IEnumerable<IPawnFilterEntry> IPawnFilterable.FilterEntries => Enumerable.Empty<IPawnFilterEntry>()
		.Concat(filters)
		.Concat(filterDefs)
	;
	int IPawnFilterable.Commonality => commonality;

	private Lazy<List<IFacePartStateWorker>> _Workers = default!;
	public List<IFacePartStateWorker> Workers => _Workers.Value;

	public bool IsFloating => floating || FacesSettings.Instance.ForcedFloatingSlots.Contains(slot);

	public override void ResolveReferences()
	{
		base.ResolveReferences();

		slot ??= SlotDefOf.Eye;
		_Workers = new(() => [..stateProps
			.Select(prop => Activator.CreateInstance(prop.WorkerType, prop))
			.Cast<IFacePartStateWorker>()
		]);

		if (undoSlotOffset)
		{
			layerOffset -= slot.layerOffset;
		}
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (var er in base.ConfigErrors())
			yield return er;

		foreach (var er in filters.SelectMany(x => x.ConfigErrors()))
			yield return er;
	}

	public string? GetGraphicPath(Comp_TSFace face, FaceSide side)
	{
		var worker = Workers
			.Where(x => x.IsActive(face, this, side))
			.OrderByDescending(x => x.Properties.priority)
			.FirstOrDefault()
		;

		// if (worker is not null)
		// {
		// 	Log.Message($"worker {worker}({worker.GetType()}) active for {this} for pawn {face.Pawn}, path={worker.Properties.path}, hide={worker.Properties.hide}");
		// }

		if (worker?.Properties.hide == true)
			return null;

		return worker?.Properties.path ?? graphicPath;
	}

	public int CompareTo(FacePartDef other) => string.Compare(defName, other.defName);

	public FacePartDef CreateCopy() => this;
}
