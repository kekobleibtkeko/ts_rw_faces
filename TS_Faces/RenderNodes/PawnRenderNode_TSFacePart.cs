using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Faces.Comps;
using TS_Faces.Data;
using UnityEngine;
using Verse;

namespace TS_Faces.RenderNodes;

public class PawnRenderNode_TSFacePart(
    Pawn pawn,
    Comp_TSFace face,
    FaceLayoutPart slot,
    PawnRenderTree tree
)
    : PawnRenderNode(
        pawn,
        new PawnRenderNodeProperties
        {
            workerClass = typeof(PawnRenderNodeWorker_TSFacePart),
            parentTagDef = PawnRenderNodeTagDefOf.Head,
            baseLayer = Comp_TSFace.TSHeadBaseLayer,
        },
        tree
    )
{
    public Pawn Pawn = pawn;
    public FaceLayoutPart Slot = slot;
    public Comp_TSFace Face = face;

    public override Graphic GraphicFor(Pawn pawn)
    {
        var part = Face.GetPartForSlot(Slot.slot);
        var shader = part.PartDef.shader.NullOrEmpty()
            ? ShaderDatabase.CutoutSkin
            : ShaderDatabase.LoadShader(part.PartDef.shader)
        ;
        var side = Slot.slot.ToSide();
        Color color = part.PartDef.color switch
        {
            FacePartDef.Color.Eye => Face.GetEyeColor(side),
            FacePartDef.Color.Skin => pawn.story.SkinColor,
            FacePartDef.Color.Hair => pawn.story.HairColor,
            FacePartDef.Color.Sclera => Face.GetScleraColor(side),
            FacePartDef.Color.None or _ => Color.white,
        };
        Log.Message($"getting graphic for pawn {Pawn}, node slot {Slot.slot}");
        string? path = null;
        switch (Face.GetPawnState())
        {
            case PawnState.Normal:
                path = part.PartDef.graphicPath;
                break;
            case PawnState.Sleeping:
                if (!part.PartDef.hideSleep)
                    path = part.PartDef.graphicPathSleep ?? part.PartDef.graphicPath;
                break;
            case PawnState.Dead:
                if (!part.PartDef.hideDead)
                    path = part.PartDef.graphicPathDead ?? part.PartDef.graphicPath;
                break;
        }
        if (path.NullOrEmpty())
            return base.GraphicFor(pawn);
        return GraphicDatabase.Get<Graphic_Multi>(path, shader, part.PartDef.drawSize, color);
    }
}

public class PawnRenderNodeWorker_TSFacePart : PawnRenderNodeWorker
{
    public bool GetNodeData(PawnRenderNode node, out PawnRenderNode_TSFacePart part_node, out TRFacePart part)
    {
        part_node = default!;
        if (node is PawnRenderNode_TSFacePart p_node)
            part_node = p_node;

        part = part_node.Face.GetPartForSlot(part_node.Slot.slot);
        return part_node is not null && part is not null;
    }

    public override Vector3 OffsetFor(PawnRenderNode node, PawnDrawParms parms, out Vector3 pivot)
    {
        var base_val = base.OffsetFor(node, parms, out pivot);
        if (!GetNodeData(node, out var part_node, out var part))
            return base_val;
        return base_val
            + part_node.Slot.pos
            + part.Transform.ForRot(part_node.Pawn.Rotation).Offset
            + new Vector3(0, part_node.Slot.slot.ToLayerOffset(), 0)
        ;
    }

    public override Vector3 ScaleFor(PawnRenderNode node, PawnDrawParms parms)
    {
        return base.ScaleFor(node, parms);
    }

    public override Quaternion RotationFor(PawnRenderNode node, PawnDrawParms parms)
    {
        return base.RotationFor(node, parms);
    }

}