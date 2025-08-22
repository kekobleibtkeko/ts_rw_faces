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

public class PawnRenderNode_TSFace(
    Pawn pawn,
    Comp_TSFace face,
    PawnRenderTree tree
)
    : PawnRenderNode(
        pawn,
        new PawnRenderNodeProperties
        {
            workerClass = typeof(PawnRenderNodeWorker_TSFace),
            parentTagDef = PawnRenderNodeTagDefOf.Head,
            baseLayer = Comp_TSFace.TSHeadBaseLayer,
        },
        tree
    )
{
    public Pawn Pawn = pawn;

    public override Graphic GraphicFor(Pawn pawn)
    {
        return GraphicDatabase.Get<Graphic_Multi>(face.PersistentData.Heads.First().graphicPath, ShaderDatabase.CutoutSkin, Vector2.one, pawn.story.SkinColor);
    }

    public IEnumerable<PawnRenderNode> GetSubNodes()
    {
        Rot4 direction = Pawn.Rotation;
        var face_layout = face.GetActiveFaceLayout().ForRot(direction);
        foreach (var slot in face_layout)
        {
            if (slot.slot.OnSide(Side.Left) == FaceSlot.EyeL)
            {
                var orig_side = slot.slot.ToSide();
                yield return new PawnRenderNode_TSFacePart(Pawn, face, slot.WithSlot(Data.FaceSlot.ScleraL.OnSide(orig_side)), tree);
                yield return new PawnRenderNode_TSFacePart(Pawn, face, slot.WithSlot(Data.FaceSlot.IrisL.OnSide(orig_side)), tree);
                yield return new PawnRenderNode_TSFacePart(Pawn, face, slot.WithSlot(Data.FaceSlot.HighlightL.OnSide(orig_side)), tree);
            }
            yield return new PawnRenderNode_TSFacePart(Pawn, face, slot, tree);
        }
        yield break;
    }
}

public class PawnRenderNodeWorker_TSFace : PawnRenderNodeWorker
{
}