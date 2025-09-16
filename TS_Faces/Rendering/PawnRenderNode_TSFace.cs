using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using static Verse.PawnRenderNodeProperties;

namespace TS_Faces.Rendering;

public class PawnRenderNode_TSFace : PawnRenderNode
{
    public Pawn Pawn;
    public Comp_TSFace Face;

    public PawnRenderNode_TSFace(
        Pawn pawn,
        Comp_TSFace face,
        PawnRenderTree tree
    ) : base(
        pawn,
        new PawnRenderNodeProperties
        {
            workerClass = typeof(PawnRenderNodeWorker_TSFace),
            parentTagDef = PawnRenderNodeTagDefOf.Head,
            // baseLayer = Comp_TSFace.TSHeadBaseLayer + PawnRenderNodeWorker_TSFace.ts_face_tweak,
            //overlayLayer = PawnOverlayDrawer.OverlayLayer.Head,
            //colorType = AttachmentColorType.Skin,
        },
        tree
    ) {
        Pawn = pawn;
        Face = face;
    }

    public override string TexPathFor(Pawn pawn) => FacePartDefOf.Empty.graphicPath;
}

public class PawnRenderNodeWorker_TSFace : PawnRenderNodeWorker
{
    [TweakValue("TS", -50f, 50f)]
    public static float ts_face_tweak;
    public static Shader DefaultShader => ShaderDatabase.Cutout;
    public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
    {
        if (parms.rotDrawMode == RotDrawMode.Dessicated)
            return;

        if (node is not PawnRenderNode_TSFace face_node)
            return;

        var face = face_node.Face;
        var pawn = face.Pawn;

		var render_target = new FaceMeshTarget(mesh, matrix, parms);
		var renderables = face
			.GetActiveFaceLayout()
			.CollectAllRenderables(face, parms.facing, def => def.IsFloating)
		;
		render_target.ApplyAll(renderables);

        base.PostDraw(node, parms, mesh, matrix);
    }
}