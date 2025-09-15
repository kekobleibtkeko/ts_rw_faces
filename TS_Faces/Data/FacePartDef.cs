using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Faces.Comps;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

[DefOf]
public static class FacePartDefOf
{
	public static FacePartDef Empty = default!;

	public static FacePartDef DebugEye = default!;
	public static FacePartDef DebugIris = default!;
	public static FacePartDef DebugMouth = default!;
	public static FacePartDef DebugNose = default!;

	static FacePartDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(FacePartDefOf));
	}
}

public class FacePartDef : Def, IPawnFilterable, IComparable<FacePartDef>
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

	public int commonality = 10;
	public List<PawnFilterEntry> filters = [];

	IEnumerable<PawnFilterEntry> IPawnFilterable.FilterEntries => filters;
	float IPawnFilterable.Commonality => commonality;

	private Lazy<List<IFacePartStateWorker>> _Workers = default!;
	public List<IFacePartStateWorker> Workers => _Workers.Value;
	

	public override void ResolveReferences()
	{
		slot ??= SlotDefOf.Eye;
		_Workers = new(() => [..stateProps
			.Select(prop => Activator.CreateInstance(prop.WorkerType, prop))
			.Cast<IFacePartStateWorker>()
		]);
		base.ResolveReferences();
	}

	public override IEnumerable<string> ConfigErrors()
	{
		foreach (var er in base.ConfigErrors())
			yield return er;

		foreach (var er in filters.SelectMany(x => x.GetConfigErrors()))
			yield return er;
	}

	public string? GetGraphicPath(Comp_TSFace face, FaceSide side)
	{
		var worker = Workers
			.Where(x => x.IsActive(face, this, side))
			.OrderByDescending(x => x.Properties.priority)
			.FirstOrDefault()
		;

		if (worker?.Properties.hide == true)
			return null;

		return worker?.Properties.path ?? graphicPath;
	}

	public int CompareTo(FacePartDef other) => string.Compare(defName, other.defName);
}
