using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using Verse.Noise;
using static Verse.PawnRenderNodeProperties;

namespace TS_Faces.Rendering;

[StaticConstructorOnStartup]
public static class FaceRenderer
{
    public const int RT_SIZE = 256;
    public static RenderTexture MainRT = new(RT_SIZE, RT_SIZE, 1);
    public static RenderTexture SecRT = new(RT_SIZE, RT_SIZE, 1);

    public static Shader EyeShader;

    static FaceRenderer()
    {
        MainRT.Create();
        SecRT.Create();
        EyeShader = ShaderDatabase.LoadShader("TSEye");
    }

    public static void RegenerateFaces(Comp_TSFace face)
    {
        face.RenderState = Comp_TSFace.ReRenderState.InProgress;
        var pawn = face.Pawn;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Shader get_shader(string? shader_path)
        {
            if (shader_path.NullOrEmpty() || ShaderDatabase.LoadShader(shader_path) is not Shader res)
                return ShaderDatabase.CutoutSkinOverlay;
            return res;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Color get_color(PartColor part_color, FaceSide side = FaceSide.None, Color? custom_color = null)
        {
            return custom_color ?? part_color switch
            {
                PartColor.Eye => face.GetEyeColor(side),
                PartColor.Skin => pawn.story.SkinColor,
                PartColor.Hair => pawn.story.HairColor,
                PartColor.Sclera => face.GetScleraColor(side),
                PartColor.None or _ => Color.white,
            };
        }

        var head_def = face.GetActiveHeadDef();
        var head_shader = get_shader(head_def.shader);
        var head_color = get_color(head_def.color);
        var head_graphic = GraphicDatabase.Get<Graphic_Multi>(head_def.graphicPath, head_shader, Vector2.one, head_color);
        var new_graphic = new Graphic_Multi();

        foreach (var rot in Rot4.AllRotations)
        {
            MainRT.Clear();
            var head_side_mat = head_graphic.MatAt(rot);
            Graphics.Blit(head_side_mat.mainTexture, MainRT, head_side_mat);

            var face_layout = head_def.faceLayout.ForRot(rot);
            var all_parts = face_layout
                .OrderBy(x => x.pos.y + x.slot.ToLayerOffset())
                .ToList()
            ;
            foreach (var part in all_parts)
            {
                var comp_part = face.GetPartForSlot(part.slot);
                if (!comp_part.PartDef.isOnHead)
                    continue;

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

                var color = get_color(def.color, side, def.customColor);
                var shader = get_shader(def.shader);
                var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
                if (graphic is null)
                {
                    Log.Warning($"Unable to get graphic to draw slot {part.slot} for {pawn}");
                    continue;
                }

                bool flip = !comp_part.PartDef.noMirror && rot.AsInt switch
                {
                    //north
                    0 => side == FaceSide.Left,
                    //south
                    2 => side == FaceSide.Right,
                    _ => false,
                };

                //Log.Message($"Slot {part.slot}({side}) flipped on side {parms.facing.ToStringHuman()}: {flip}");
                Material part_mat = graphic.MatAt(rot);
                if (part_mat is null)
                {
                    Log.Warning($"Unable to get material to draw slot {part.slot} for {pawn}");
                    continue;
                }

                var transform = comp_part.Transform.ForRot(rot);
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

                float tex_x_mul = flip ? -1 : 1;

                if (part.slot.OnSide(FaceSide.Left) == FaceSlot.EyeL)
                {
                    Log.Message("rendering an eye :3");
                    part_mat.SetTexture("_EyeTex", part_mat.mainTexture);
                }

                SecRT.Clear();
                Graphics.Blit(part_mat.mainTexture, SecRT, part_mat);
                var temp_tex = SecRT.CreateTexture2D();
                using (new TSUtil.ActiveRT_D(SecRT))
                {
                    GL.Clear(true, true, Color.clear);
                    GL.PushMatrix();
                    GL.LoadOrtho();
                    Matrix4x4 matrix =
                        Matrix4x4.TRS(
                            new Vector3(
                                0.5f + (pos.x),
                                0.5f + (pos.z),
                                0.5f
                            ),
                            Quaternion.Euler(0f, 0f, rotation),
                            new Vector3(
                                draw_scale.x,
                                draw_scale.z,
                                1f
                            )
                        )
                        * Matrix4x4.Translate(new Vector3(-0.5f, -0.5f, 0f))
                    ;
                    GL.MultMatrix(matrix);
                    Graphics.DrawTexture(new(0, 1, 1, -1), temp_tex);
                    GL.PopMatrix();
                }
                //Graphics.Blit(SecRT, SecRT, head_side_mat);

                if (part.slot.OnSide(FaceSide.Left) == FaceSlot.EyeL)
                {
                    //var eyemat = new Material(EyeShader);
                    //TSUtil.BlitUtils.BlitWithTransform(
                    //    MainRT,
                    //    part_mat,
                    //    source: part_mat.mainTexture,
                    //    scale: def.drawSize * transform.Scale * new Vector2(tex_x_mul, 1),
                    //    offset: part.pos.FromUpFacingVec3() + transform.Offset.FromUpFacingVec3(),
                    //    rotation: rotation
                    //);
                }
                //else
                //{
                //    TSUtil.BlitUtils.BlitWithTransform(
                //        MainRT,
                //        part_mat,
                //        source: part_mat.mainTexture,
                //        scale: def.drawSize * transform.Scale * new Vector2(tex_x_mul, 1),
                //        offset: part.pos.FromUpFacingVec3() + transform.Offset.FromUpFacingVec3(),
                //        rotation: rotation
                //    );
                //}

                Graphics.Blit(SecRT, MainRT, new(ShaderDatabase.CutoutComplexBlend));
                //Graphics.Blit(SecRT, MainRT, draw_scale.FromUpFacingVec3(), pos.FromUpFacingVec3());
                //using (new TSUtil.ActiveRT_D(MainRT))
                //{

                //    //GL.PushMatrix();
                //    //GL.LoadOrtho();
                //    //GL.MultMatrix(loc_matrix);
                //    //////Graphics.Blit(tex, MainRT, material);
                //    //Graphics.DrawTexture(new(0, 1, 1, -1), part_mat.mainTexture);

                //    //GL.PopMatrix();
                //    //TSUtil.ScreenQuadDrawer.DrawScreenQuad(head_side_mat);
                //}
            }

            new_graphic.mats[rot.AsInt] = new Material(ShaderDatabase.Cutout)
            {
                mainTexture = MainRT.CreateTexture2D(),
            };
        }

        face.CachedGraphic = new_graphic;
        face.RenderState = Comp_TSFace.ReRenderState.UpToDate;
    }
}
