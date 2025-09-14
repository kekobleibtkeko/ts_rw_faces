using System;
using System.Linq;
using RimWorld;
using TS_Lib.Util;
using Verse;

namespace TS_Faces.Data;

public interface IFaceSlotPartWorker
{
	public enum PartHealth
	{
		Ok,
		Damaged,
		Missing,
	}
	PartHealth GetPartHealth(Pawn pawn, FaceSide side);
}

public abstract class FaceSlotPartWorkerBase(FaceSlotWorkerPropsBase props) : IFaceSlotPartWorker
{
	public FaceSlotWorkerPropsBase Props = props;

	protected IFaceSlotPartWorker.PartHealth HandleRecord(Pawn pawn, BodyPartRecord? record)
	{
		if (record is null)
			return IFaceSlotPartWorker.PartHealth.Ok;
		var health = pawn.health.hediffSet.GetPartHealth(record);
		if (health == 0)
			return IFaceSlotPartWorker.PartHealth.Missing;
		if (health >= record.def.GetMaxHealth(pawn))
			return IFaceSlotPartWorker.PartHealth.Ok;
		return IFaceSlotPartWorker.PartHealth.Damaged;
	}

	public abstract IFaceSlotPartWorker.PartHealth GetPartHealth(Pawn pawn, FaceSide side);
}

public abstract class FaceSlotPartWorker<TProps>(TProps props) : FaceSlotPartWorkerBase(props)
	where
		TProps : FaceSlotWorkerPropsBase
{
	public new TProps Props => base.Props as TProps ?? throw new Exception($"Invalid props for FaceSlotPartWorker '{GetType()}'");
}

public abstract class FaceSlotWorkerPropsBase
{
	public abstract Type WorkerType { get; }
}


public abstract class FaceSlotWorkerProps<TWorker> : FaceSlotWorkerPropsBase
	where
		TWorker : FaceSlotPartWorkerBase
{
	public override Type WorkerType => typeof(TWorker);
}

public class FaceSlotWorkerNoneProps : FaceSlotWorkerProps<FaceSlotWorkerNoneProps.Worker>
{
	public class Worker(FaceSlotWorkerNoneProps props) : FaceSlotPartWorker<FaceSlotWorkerNoneProps>(props)
	{
		public override IFaceSlotPartWorker.PartHealth GetPartHealth(Pawn pawn, FaceSide side) => IFaceSlotPartWorker.PartHealth.Ok;
	}
}

public abstract class PartSlotRecordWorker<TProps>(TProps props) : FaceSlotPartWorker<TProps>(props)
	where
		TProps : FaceSlotWorkerPropsBase
{
	protected abstract BodyPartRecord? GetBodyPartRecord(Pawn pawn, FaceSide side);
	public override IFaceSlotPartWorker.PartHealth GetPartHealth(Pawn pawn, FaceSide side)
	{
		var record = GetBodyPartRecord(pawn, side);
		return HandleRecord(pawn, record);
	}
}

public class PartTagSlotWorkerProps : FaceSlotWorkerProps<PartTagSlotWorkerProps.Worker>
{
	public BodyPartTagDef tag = BodyPartTagDefOf.SightSource;

	public class Worker(PartTagSlotWorkerProps props) : PartSlotRecordWorker<PartTagSlotWorkerProps>(props)
	{
		protected override BodyPartRecord? GetBodyPartRecord(Pawn pawn, FaceSide side)
		{
			var parts = pawn.RaceProps.body.GetPartsWithTag(Props.tag).AsEnumerable();
			if (side != FaceSide.None)
			{
				parts = parts.Where(x => x.Label.ContainsLowerInvariant(side));
			}
			return parts.FirstOrDefault();
		}
	}
}

public class PartNameSlotWorkerProps : FaceSlotWorkerProps<PartNameSlotWorkerProps.Worker>
{
	public string name = string.Empty;
	public class Worker(PartNameSlotWorkerProps props) : PartSlotRecordWorker<PartNameSlotWorkerProps>(props)
	{
		protected override BodyPartRecord? GetBodyPartRecord(Pawn pawn, FaceSide side)
		{
			var parts = pawn.RaceProps.body.AllParts.Where(x => x.def.label.ContainsLowerInvariant(Props.name));
			if (side != FaceSide.None)
			{
				parts = parts.Where(x => x.Label.ContainsLowerInvariant(side));
			}
			return parts.FirstOrDefault();
		}
	}
}