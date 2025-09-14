using System;
using RimWorld;
using Verse;

namespace TS_Faces.Data;

[DefOf]
public static class SlotDefOf
{
    public static SlotDef Eye = default!;

    static SlotDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(SlotDefOf));
    }
}

public class SlotDef : Def
{
	public FaceSlotWorkerPropsBase props = new FaceSlotWorkerNoneProps();
	public float layerOffset = 0;
	public float order = 0;

	private readonly Lazy<IFaceSlotPartWorker> _Worker;
	public IFaceSlotPartWorker Worker => _Worker.Value;

	public SlotDef()
	{
		_Worker = new(() =>
		{
			var worker = Activator.CreateInstance(
				props.WorkerType,
				props
			);
			return worker as IFaceSlotPartWorker ?? throw new Exception($"unable to create instance of worker for '{this}'");
		});
	}
}