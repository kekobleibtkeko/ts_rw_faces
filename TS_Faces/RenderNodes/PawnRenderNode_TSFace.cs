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

namespace TS_Faces.RenderNodes;

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
        meshSet = MeshSetFor(pawn);
    }

    public override GraphicMeshSet MeshSetFor(Pawn pawn)
    {
        return HumanlikeMeshPoolUtility.GetHumanlikeHeadSetForPawn(pawn);
    }

    //public override Graphic GraphicFor(Pawn pawn)
    //{
    //    //return GraphicDatabase.Get<Graphic_Multi>(
    //    //    FacePartDefOf.Empty.graphicPath,
    //    //    ShaderDatabase.Transparent,
    //    //    Vector2.zero,
    //    //    Color.clear
    //    //);

    //    var head_def = Face.PersistentData.Heads.GetFilteredDef(pawn);
    //    return GraphicDatabase.Get<Graphic_Multi>(
    //        head_def.graphicPath,
    //        ShaderDatabase.CutoutSkin,
    //        Vector2.one,
    //        pawn.story.SkinColor
    //    );
    //}

    public override string TexPathFor(Pawn pawn) => FacePartDefOf.Empty.graphicPath;
}

public class PawnRenderNodeWorker_TSFace : PawnRenderNodeWorker
{
    [TweakValue("TS", -50f, 50f)]
    public static float ts_face_tweak;
    public static Shader DefaultShader => ShaderDatabase.CutoutSkin;
    public override void PostDraw(PawnRenderNode node, PawnDrawParms parms, Mesh mesh, Matrix4x4 matrix)
    {
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
                    : (ShaderDatabase.LoadShader(shader_path) ?? DefaultShader)
            ;

            color = parms.statueColor ?? custom_color ?? part_color switch
            {
                PartColor.Eye => face.GetEyeColor(side),
                PartColor.Skin => pawn.story.SkinColor,
                PartColor.Hair => pawn.story.HairColor,
                PartColor.Sclera => face.GetScleraColor(side),
                PartColor.None or _ => Color.white,
            };
        }

        //render head
        {
            var head_def = face.PersistentData.Heads.GetFilteredDef(pawn);
            get_shader_and_color(head_def.shader, head_def.color, FaceSide.None, null, out var shader, out var color);
            var head_graphic = GraphicDatabase.Get<Graphic_Multi>(
                head_def.graphicPath,
                shader,
                Vector2.one,
                color
            );
            Material head_material = head_graphic.NodeGetMat(parms);
            GenDraw.DrawMeshNowOrLater(
                mesh,
                matrix,
                head_material,
                parms.DrawNow
            );
        }

        var face_layout = face.GetActiveFaceLayout().ForRot(parms.facing);
        var extra_eye_parts = face_layout
            .Where(x => x.slot.OnSide(FaceSide.Left) == FaceSlot.EyeL)
            .SelectMany(x =>
            {
                var orig_side = x.slot.ToSide();
                return Enumerable.Empty<FaceLayoutPart>()
                    .Append(x.WithSlot(Data.FaceSlot.ScleraL.OnSide(orig_side)))
                    .Append(x.WithSlot(Data.FaceSlot.IrisL.OnSide(orig_side)))
                    .Append(x.WithSlot(Data.FaceSlot.HighlightL.OnSide(orig_side)))
                ;
            })
        ;
        var all_parts = face_layout.Concat(extra_eye_parts).ToList();
        //Log.Message(string.Join(
        //    ",\n",
        //    all_parts.Select(x => $"{x.slot} on side {x.slot.ToSide()}")
        //));
        foreach (var part in all_parts)
        {
            var actual_mesh = mesh;
            var comp_part = face.GetPartForSlot(part.slot);
            var side = part.slot.ToSide();
            var def = comp_part.PartDef;
            //Log.Message($"getting graphic for pawn {pawn}, node slot {slot.slot}");
            string? path = null;
            switch (face.GetPawnState())
            {
                case PawnState.Normal:
                    path = comp_part.PartDef.graphicPath;
                    break;
                case PawnState.Sleeping:
                    if (!comp_part.PartDef.hideSleep)
                        path = comp_part.PartDef.graphicPathSleep ?? comp_part.PartDef.graphicPath;
                    break;
                case PawnState.Dead:
                    if (!comp_part.PartDef.hideDead)
                        path = comp_part.PartDef.graphicPathDead ?? comp_part.PartDef.graphicPath;
                    break;
            }
            
            if (path.NullOrEmpty())
                continue; // this is fine, part may not have a graphic for this state

            get_shader_and_color(def.shader, def.color, side, def.customColor, out var shader, out var color);
            var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
            if (graphic is null)
            {
                Log.Warning($"Unable to get graphic to draw slot {part.slot} for {pawn}");
                continue;
            }

            if (node.Props.flipGraphic && parms.facing.IsHorizontal)
            {
                parms.facing = parms.facing.Opposite;
            }
            bool flip = !comp_part.PartDef.noMirror && parms.facing.AsInt switch
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
                Log.Warning($"Unable to get material to draw slot {part.slot} for {pawn}");
                continue;
            }

            var transform = comp_part.Transform.ForRot(parms.facing);
            var pos = part.pos
                + transform.Offset
                + TSUtil.GetUpVector(part.slot.ToLayerOffset())
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