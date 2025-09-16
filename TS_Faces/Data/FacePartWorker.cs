using System;
using System.Collections.Generic;
using TS_Faces.Comps;
using TS_Faces.Util;
using Verse;

namespace TS_Faces.Data;

public interface IFacePartStateWorker
{
	bool IsActive(Comp_TSFace face, FacePartDef def, FaceSide side);
	FacePartWorkerPropsBase Properties { get; }
}

public abstract class FacePartWorkerBase(FacePartWorkerPropsBase props) : IFacePartStateWorker
{
	public FacePartWorkerPropsBase Props = props;
	public FacePartWorkerPropsBase Properties => Props;

	public abstract bool IsActive(Comp_TSFace face, FacePartDef def, FaceSide side);
}

public abstract class FacePartWorkerPropsBase
{
	public string path = string.Empty;
	public bool hide;
	public float priority;
	public abstract Type WorkerType { get; }
}

public abstract class FacePartWorker<TProps>(TProps props) : FacePartWorkerBase(props)
	where
		TProps : FacePartWorkerPropsBase
{
	public new TProps Props = props;
}

public abstract class FacePartWorkerProps<TWorker> : FacePartWorkerPropsBase
	where
		TWorker : FacePartWorkerBase
{
	public override Type WorkerType => typeof(TWorker);
}


public class FacePartStateProps : FacePartWorkerProps<FacePartStateProps.Worker>
{
	public PawnState state;

	public class Worker(FacePartStateProps props) : FacePartWorker<FacePartStateProps>(props)
	{
		public override bool IsActive(Comp_TSFace face, FacePartDef def, FaceSide side) => face.GetPawnState() == Props.state;
	}
}

public class FacePartHealthProps : FacePartWorkerProps<FacePartHealthProps.Worker>
{
	public IFaceSlotPartWorker.PartHealth health;
	public class Worker(FacePartHealthProps props) : FacePartWorker<FacePartHealthProps>(props)
	{
		public override bool IsActive(Comp_TSFace face, FacePartDef def, FaceSide side) => Props.health == def.slot.Worker.GetPartHealth(face.Pawn, side);
	}
}