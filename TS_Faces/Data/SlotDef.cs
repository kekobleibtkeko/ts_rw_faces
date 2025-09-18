using System;
using System.Linq;
using System.Text;
using RimWorld;
using TS_Faces.Mod;
using TS_Faces.Util;
using TS_Lib.Util;
using Verse;
using static TS_Lib.Util.TSUtil;

namespace TS_Faces.Data;

[DefOf]
public static class SlotDefOf
{
	public static SlotDef None = default!;

	public static SlotDef SkinDecor = default!;

	public static SlotDef Eye = default!;
	public static SlotDef Iris = default!;
	public static SlotDef Highlight = default!;

	public static SlotDef Nose = default!;
	public static SlotDef Mouth = default!;
	public static SlotDef Brow = default!;
	public static SlotDef Ear = default!;

	static SlotDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(SlotDefOf));
	}
}

public struct RandomFacePart : IWeightedRandom<FacePartDef>
{
	public readonly int Weight { get; }
	public readonly FacePartDef Value { get; }

	private RandomFacePart(int weight, FacePartDef def) : this()
	{
		Weight = weight;
		Value = def;
	}
	public static RandomFacePart From(FacePartDef def, Pawn pawn)
	{
		var weight = def.commonality + def.filters.Sum(x => x.CommonalityFor(pawn));
		return new(weight, def);
	}
}

public class SlotDef : Def
{
	public FaceSlotWorkerPropsBase props = new FaceSlotWorkerNoneProps();
	public float layerOffset = 0;
	public float order = 0;

	private Lazy<IFaceSlotPartWorker> _Worker = default!;
	public IFaceSlotPartWorker Worker => _Worker.Value;


	public override void ResolveReferences()
	{
		base.ResolveReferences();
		_Worker = new(() =>
		{
			var worker = Activator.CreateInstance(
				props.WorkerType,
				props
			);
			return worker as IFaceSlotPartWorker ?? throw new Exception($"unable to create instance of worker for '{this}'");
		});
	}

	public FacePartDef? GetRandomPartFor(Pawn pawn, StringBuilder? reasons = null)
	{
		return FacesUtil.RandomParts.Ensure(this).GetRandomFor(pawn, reasons);
	}
}