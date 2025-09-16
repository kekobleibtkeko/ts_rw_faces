using System;
using System.Linq;
using System.Text;
using RimWorld;
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

	public FacePartDef? GetRandomPartFor(Pawn pawn)
	{
		var parts = FacesUtil.RandomParts.Ensure(this);
		var fitting_parts = parts
			.Select(part =>
			{
				var reasons = new StringBuilder();
				if (part.FilterFits(pawn, out var _, reasons))
					return (FacePartDef?)part;

				if (reasons is not null)
				{
					TSFacesMod.Logger.Verbose($"Random: part {part} doesn't fit, Reasons:");
					TSFacesMod.Logger.Verbose(reasons.ToString());
				}
				return null;
			})
			.Where(part => part is not null)
			.Select(part => RandomFacePart.From(part!, pawn))
		;

		return fitting_parts.GetRandom<RandomFacePart, FacePartDef>();
	}
}