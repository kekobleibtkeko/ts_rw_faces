using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
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
    public const int RT_SIZE = 512;
    public static RenderTexture MainRT = new(RT_SIZE, RT_SIZE, 1);
    public static RenderTexture SecRT = new(RT_SIZE, RT_SIZE, 1);

    public static Shader SkinShader;
    public static Shader EyeShader;
    public static Shader TransparentShader;
    public static Shader ColorOverrideShader;
    static FaceRenderer()
    {
        MainRT.Create();
        SecRT.Create();
        SkinShader = ShaderDatabase.LoadShader("TSSkin");
        EyeShader = ShaderDatabase.LoadShader("TSEye");
        TransparentShader = ShaderDatabase.LoadShader("TSTransparent");
        ColorOverrideShader = ShaderDatabase.LoadShader("TSColorOverride");
    }

    public static void MakeStatueColored(Comp_TSFace face, Color statue_color)
    {
        //Log.Message("making face statue colored");
        if (face.IsRegenerationNeeded())
            RegenerateFaces(face);

        if (face.OverriddenColor == statue_color)
            return;

        face.OverriddenColor = statue_color;
        //Log.Message("making face statue colored!!!!!!!!!! frfr");

        var override_mat = new Material(ColorOverrideShader)
        {
            //color = statue_color,
        };

        var graphic = face.CachedGraphic!;
        
        for (int i = 0; i < 4; i++)
        {
            MainRT.Clear();
            var side_mat = graphic.mats[i];

            Graphics.Blit(side_mat.mainTexture, MainRT, override_mat);
            side_mat.mainTexture = MainRT.CreateTexture2D();
        }
    }

    public static void RegenerateFaces(Comp_TSFace face)
    {
        face.RenderState = Comp_TSFace.ReRenderState.InProgress;
        var pawn_state = face.GetPawnState();

        if (pawn_state == PawnState.Dessicated)
        {
            face.CachedGraphic = null;
            face.RenderState = Comp_TSFace.ReRenderState.UpToDate;
            return;
        }

        var pawn = face.Pawn;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Shader get_shader(string? shader_path)
        {
            if (shader_path.NullOrEmpty() || ShaderDatabase.LoadShader(shader_path) is not Shader res)
                return SkinShader;
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
                if (comp_part.PartDef.floating)
                    continue;

                var side = part.slot.ToSide();
                var def = comp_part.PartDef;
                //Log.Message($"getting graphic for pawn {pawn}, node slot {slot.slot}");
                string? path = comp_part.GetGraphicPath(face, side);

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

                Material part_mat = graphic.MatAt(rot);
                if (part_mat is null)
                {
                    Log.Warning($"Unable to get material to draw slot {part.slot} for {pawn}");
                    continue;
                }

                var transform = comp_part.Transform.ForRot(rot);
                var pos = part.pos
                    + transform.Offset
                    + def.offset.ToUpFacingVec3()
                    + TSUtil.GetUpVector(part.slot.ToLayerOffset())
                ;
                var draw_scale = def.drawSize.ToUpFacingVec3(1)
                    .MultipliedBy(transform.Scale.ToUpFacingVec3(1))
                ;
                var rotation = transform.RotationOffset;

                // handle part specific adjustments
                switch (part.slot)
                {
                    case FaceSlot.EyeL:
                    case FaceSlot.EyeR:
                        //Log.Message($"rendering an eye :3  texture={part_mat.mainTexture}");
                        var iris = face.GetPartForSlot(FaceSlot.IrisL.OnSide(side));
                        var iris_graphic = GraphicDatabase.Get<Graphic_Multi>(iris.PartDef.graphicPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
                        var iris_side_mat = iris_graphic.MatAt(rot);

                        part_mat.SetTexture("_EyeTex", part_mat.mainTexture);
                        part_mat.SetTexture("_IrisTex", iris_side_mat.mainTexture);

                        part_mat.SetColor("_SkinColor", pawn.story.SkinColor);
                        part_mat.SetColor("_LashColor", pawn.story.hairColor);
                        part_mat.SetColor("_ScleraColor", face.GetScleraColor(side));
                        part_mat.SetColor("_IrisColor", face.GetEyeColor(side));

                        part_mat.SetFloat("_Flipped", flip ? 1 : 0);
                        flip = false; // the shader handles the flipping here
                        break;
                }

                TSUtil.BlitUtils.BlitWithTransform(
                    MainRT,
                    part_mat,
                    source: part_mat.mainTexture,
                    scale: draw_scale.FromUpFacingVec3(),
                    offset: pos.FromUpFacingVec3(),
                    rotation: rotation,
                    flip_x: flip
                );
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
