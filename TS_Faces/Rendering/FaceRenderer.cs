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

	public static Shader SkinShader;
	public static Shader EyeShader;
	public static Shader TransparentShader;
	public static Shader ColorOverrideShader;
	static FaceRenderer()
	{
		MainRT.Create();
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

		var head_def = face.GetActiveHeadDef();
		var head_shader = RendererExtensions.GetShader(head_def.shader);
		var head_color = RendererExtensions.GetColorFor(head_def.color, face);
		var head_graphic = GraphicDatabase.Get<Graphic_Multi>(head_def.graphicPath, head_shader, Vector2.one, head_color);
		var new_graphic = new Graphic_Multi();

		var render_target = new FaceRTTarget(MainRT);
		foreach (var rot in Rot4.AllRotations)
		{
			MainRT.Clear();
			var head_side_mat = head_graphic.MatAt(rot);
			Graphics.Blit(head_side_mat.mainTexture, MainRT, head_side_mat);

			var face_layout = head_def.faceLayout.ForRot(rot);
			var all_parts = face_layout
				.OrderBy(x => x.pos.y + x.slot.layerOffset)
				.ToList()
			;

			var renderables = head_def.faceLayout.CollectRenderables(face, rot, def => !def.floating);
			render_target.ApplyAll(renderables);

			new_graphic.mats[rot.AsInt] = new Material(ShaderDatabase.Cutout)
			{
				mainTexture = MainRT.CreateTexture2D(),
			};
		}

		face.CachedGraphic = new_graphic;
		face.RenderState = Comp_TSFace.ReRenderState.UpToDate;
	}
}
