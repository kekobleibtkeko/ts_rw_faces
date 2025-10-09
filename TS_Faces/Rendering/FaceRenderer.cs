using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Faces.Mod;
using TS_Faces.Util;
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
	public static Shader MaskShader;
	public static Color TattooColor;
	static FaceRenderer()
	{
		MainRT.Create();
		SkinShader = ShaderDatabase.LoadShader("TSSkin");
		EyeShader = ShaderDatabase.LoadShader("TSEye");
		TransparentShader = ShaderDatabase.LoadShader("TSTransparent");
		ColorOverrideShader = ShaderDatabase.LoadShader("TSColorOverride");
		MaskShader = ShaderDatabase.LoadShader("TSOutlineMask");
		TattooColor = Color.white;
		TattooColor.a *= 0.8f;
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

		TSFacesMod.Logger.Verbose($"regenerating face for {face.Pawn}, state: {pawn_state}");

		var pawn = face.Pawn;

		var head_def = face.GetActiveHeadDef();
		var head_shader = RendererExtensions.GetShader(head_def.shader);
		var head_color = RendererExtensions.GetColorFor(head_def.color, face);
		var head_graphic = GraphicDatabase.Get<Graphic_Multi>(head_def.graphicPath, MaskShader, Vector2.one, head_color);
		var new_graphic = new Graphic_Multi();

		var head_mat = new Material(head_shader)
		{
			color = head_color,
		};

		var old_graphic = face.CachedGraphic;

		var render_target = new FaceRTTarget(MainRT);
		foreach (var rot in Rot4.AllRotations)
		{
			MainRT.Clear();
			SecRT.Clear();
			var head_side_mat = head_graphic.MatAt(rot);
			Graphics.Blit(head_side_mat.mainTexture, MainRT, head_mat);

			var renderables = Enumerable.Empty<FacePartRenderable>()
				.Concat(head_def.faceLayout.CollectNeededAndExtraRenderables(face, rot, def => !def.IsFloating))
				.Concat(face.CollectOnFaceRenderables(rot))
			;
			// ugly hack because the transforms that are passed are reversed in the west side
			//   for some reason
			// TODO: find out why the FUCK this is necessary
			render_target.ReverseX = rot == Rot4.West;
			render_target.ApplyAll(renderables);

			// remove things outside of mask area
			Graphics.Blit(MainRT, SecRT, head_side_mat);

			new_graphic.mats[rot.AsInt] = new Material(ShaderDatabase.Cutout)
			{
				mainTexture = SecRT.CreateTexture2D(),
			};
		}

		if (old_graphic is not null)
		{
			try
			{
				Rot4.AllRotations.Do(x => UnityEngine.Object.DestroyImmediate(old_graphic.mats[x.AsInt].mainTexture));
			}
			catch (Exception e)
			{
				TSFacesMod.Logger.Warning($"unable to destroy old face texture: '{e}'");
			}
		}

		face.CachedGraphic = new_graphic;
		face.RenderState = Comp_TSFace.ReRenderState.UpToDate;
	}
}
