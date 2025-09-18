using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using TS_Faces.Util;
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
	public static Dictionary<(string, int), (int, IFaceSlotPartWorker.PartHealth)> HealthPartCache = [];
	public FaceSlotWorkerPropsBase Props = props;

	protected virtual IFaceSlotPartWorker.PartHealth HandleRecord(Pawn pawn, BodyPartRecord? record)
	{
		IFaceSlotPartWorker.PartHealth part_health;
		if (record is null)
			return IFaceSlotPartWorker.PartHealth.Ok;

		var id = (pawn.ThingID, record.Index);
		if (TickCache<(string, int), IFaceSlotPartWorker.PartHealth>.TryGetCached(id, out var cache_health))
		{
			return cache_health;
		}

		var health = pawn.health.hediffSet.GetPartHealth(record);
		if (health == 0)
			part_health = IFaceSlotPartWorker.PartHealth.Missing;
		else if (health >= record.def.GetMaxHealth(pawn))
			part_health = IFaceSlotPartWorker.PartHealth.Ok;
		else
			part_health = IFaceSlotPartWorker.PartHealth.Damaged;

		TickCache<(string, int), IFaceSlotPartWorker.PartHealth>.Cache(id, part_health);
		return part_health;
	}

	public abstract IFaceSlotPartWorker.PartHealth GetPartHealth(Pawn pawn, FaceSide side);
}

public abstract class FaceSlotPartWorker<TProps>(TProps props) : FaceSlotPartWorkerBase(props)
	where
		TProps: FaceSlotWorkerPropsBase
{
	public new TProps Props => base.Props as TProps ?? throw new Exception($"Invalid props for FaceSlotPartWorker '{GetType()}'");
}

public abstract class FaceSlotWorkerPropsBase
{
	public abstract Type WorkerType { get; }
}


public abstract class FaceSlotWorkerProps<TWorker> : FaceSlotWorkerPropsBase
	where
		TWorker: FaceSlotPartWorkerBase
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

public abstract class FaceSlotRecordWorkerProps<TProps, TWorker> : FaceSlotWorkerProps<TWorker>
	where
		TProps: FaceSlotWorkerProps<TWorker>
	where
		TWorker: PartSlotRecordWorker<TProps>
{
	public IFaceSlotPartWorker.PartHealth okOverride = IFaceSlotPartWorker.PartHealth.Ok;
	public IFaceSlotPartWorker.PartHealth damagedOverride = IFaceSlotPartWorker.PartHealth.Damaged;
	public IFaceSlotPartWorker.PartHealth missingOverride = IFaceSlotPartWorker.PartHealth.Missing;
}

public abstract class PartSlotRecordOverrideWorker<TProps, TWorker>(TProps props) : PartSlotRecordWorker<TProps>(props)
	where
		TProps : FaceSlotRecordWorkerProps<TProps, TWorker>
	where
		TWorker : PartSlotRecordWorker<TProps>
{
	protected override IFaceSlotPartWorker.PartHealth HandleRecord(Pawn pawn, BodyPartRecord? record)
	{
		return base.HandleRecord(pawn, record) switch
		{
			IFaceSlotPartWorker.PartHealth.Damaged => Props.damagedOverride,
			IFaceSlotPartWorker.PartHealth.Missing => Props.missingOverride,
			IFaceSlotPartWorker.PartHealth.Ok or _ => Props.okOverride,
		};
	}
}

public class PartTagSlotWorkerProps : FaceSlotRecordWorkerProps<PartTagSlotWorkerProps, PartTagSlotWorkerProps.Worker>
{
	public BodyPartTagDef tag = default!;

	public class Worker(PartTagSlotWorkerProps props) : PartSlotRecordOverrideWorker<PartTagSlotWorkerProps, Worker>(props)
	{
		protected override BodyPartRecord? GetBodyPartRecord(Pawn pawn, FaceSide side)
		{
			Props.tag ??= BodyPartTagDefOf.SightSource;
			var parts = pawn.RaceProps.body.GetPartsWithTag(Props.tag).AsEnumerable();
			if (side != FaceSide.None)
			{
				parts = parts.Where(x => x.Label.ContainsLowerInvariant(side));
			}
			return parts.FirstOrDefault();
		}
	}
}

public class PartNameSlotWorkerProps : FaceSlotRecordWorkerProps<PartNameSlotWorkerProps, PartNameSlotWorkerProps.Worker>
{
	public string name = string.Empty;

	public class Worker(PartNameSlotWorkerProps props) : PartSlotRecordOverrideWorker<PartNameSlotWorkerProps, Worker>(props)
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