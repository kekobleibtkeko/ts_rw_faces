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
            baseLayer = Comp_TSFace.TSHeadBaseLayer + PawnRenderNodeWorker_TSFace.ts_face_tweak,
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void get_shader_and_color(
            string? shader_path,
            PartColor part_color,
            FaceSide side,
            Color? custom_color,
            out Shader shader, out Color color
        ) {
            shader = parms.Statue
                ? ShaderDatabase.Cutout
                : shader_path.NullOrEmpty()
                    ? DefaultShader
                    : ShaderDatabase.LoadShader(shader_path) ?? DefaultShader
            ;
			if (shader == ShaderDatabase.CutoutSkin)
			{
				shader = ShaderUtility.GetSkinShader(pawn);
			}

            color = parms.statueColor ?? custom_color ?? part_color switch
            {
                PartColor.Eye => face.GetEyeColor(side),
                PartColor.Skin => pawn.story.SkinColor,
                PartColor.Hair => pawn.story.HairColor,
                PartColor.Sclera => face.GetScleraColor(side),
                PartColor.None or _ => Color.white,
            };
        }

        var face_layout = face.GetActiveFaceLayout().ForRot(parms.facing);
        var all_parts = face_layout
            //.Where(x => x.)
            .ToList();
		//Log.Message(string.Join(
		//    ",\n",
		//    all_parts.Select(x => $"{x.slot} on side {x.slot.ToSide()}")
		//));

		foreach (var extra in face.PersistentData.ExtraParts)
		{
			
		}
        
        foreach (var face_layout_part in all_parts)
		{
			var actual_mesh = mesh;
			var side = face_layout_part.side;
			if (!face.TryGetPartForSlot(face_layout_part.slot, side, out var comp_part))
			{
				TSFacesMod.Logger.Warning($"RenderNode unable to get part for slot '{face_layout_part.slot.defName}' for pawn {pawn}");
				continue;
			}

			var def = comp_part.Def;
			if (!def.floating)
				continue;
			//Log.Message($"getting graphic for pawn {pawn}, node slot {slot.slot}");
			string? path = comp_part.Def.GetGraphicPath(face, side);

			if (path.NullOrEmpty())
				continue; // this is fine, part may not have a graphic for this state

			get_shader_and_color(def.shader, def.color, side, def.customColor, out var shader, out var color);
			var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
			if (graphic is null)
			{
				Log.Warning($"Unable to get graphic to draw slot {face_layout_part.slot} for {pawn}");
				continue;
			}

			if (node.Props.flipGraphic && parms.facing.IsHorizontal)
			{
				parms.facing = parms.facing.Opposite;
			}
			bool flip = !comp_part.Def.noMirror && parms.facing.AsInt switch
			{
				//north
				0 => side == FaceSide.Left,
				//south
				2 => side == FaceSide.Right,
				_ => false,
			};
			if (flip)
			{
				actual_mesh = actual_mesh.GetFlippedMesh();
			}
			//Log.Message($"Slot {part.slot}({side}) flipped on side {parms.facing.ToStringHuman()}: {flip}");
			Material material = graphic.NodeGetMat(parms);
			if (material is null)
			{
				Log.Warning($"Unable to get material to draw slot {face_layout_part.slot} for {pawn}");
				continue;
			}

			var transform = comp_part.Transform.ForRot(parms.facing);
			var pos = face_layout_part.pos.ToUpFacingVec3()
				+ transform.Offset
				+ TSUtil.GetUpVector(face_layout_part.slot.layerOffset)
				+ def.offset.ToUpFacingVec3()
			//+ TSUtil.GetUpVector(ts_face_tweak)
			;
			var draw_scale = def.drawSize.ToUpFacingVec3(1)
				.MultipliedBy(transform.Scale.ToUpFacingVec3(1))
			;
			var rotation = transform.RotationOffset;

			var loc_matrix = Matrix4x4.TRS(
				pos,
				Quaternion.AngleAxis(rotation, Vector3.up),
				draw_scale
			);
			//Log.Message($"drawing slot {part.slot} for {pawn}");
			GenDraw.DrawMeshNowOrLater(actual_mesh, matrix * loc_matrix, material, parms.DrawNow);
		}

        base.PostDraw(node, parms, mesh, matrix);
    }
}