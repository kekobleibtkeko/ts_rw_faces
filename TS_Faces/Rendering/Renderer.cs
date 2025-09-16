using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Rendering;

public interface IFaceRenderTarget<TRenderable>
	where
		TRenderable : IFaceRenderable
{
	bool Apply(TRenderable renderable);
}

public interface IFaceRenderable
{
	float Order { get; }
}

public class FaceRTTarget(RenderTexture rt) : IFaceRenderTarget<FacePartRenderable>
{
	public RenderTexture RT = rt;

	public bool Apply(FacePartRenderable renderable)
	{
		renderable = renderable.ModifyFunction?.Invoke(renderable) ?? renderable;
		TSUtil.BlitUtils.BlitWithTransform(
			RT,
			renderable.Material,
			source: renderable.TextureOverride ?? renderable.Material.mainTexture,
			scale: renderable.Scale.FromUpFacingVec3(),
			offset: renderable.Offset.FromUpFacingVec3(),
			rotation: renderable.Rotation,
			flip_x: renderable.FlipX
		);
		return true;
	}
}

public class FaceMeshTarget(Mesh mesh, Matrix4x4 mat, PawnDrawParms parms) : IFaceRenderTarget<FacePartRenderable>
{
	public bool Apply(FacePartRenderable renderable)
	{
		renderable = renderable.ModifyFunction?.Invoke(renderable) ?? renderable;
		if (renderable.FlipX)
		{
			mesh = mesh.GetFlippedMesh();
		}
		GenDraw.DrawMeshNowOrLater(
			mesh,
			mat * Matrix4x4.TRS(
				renderable.Offset,
				Quaternion.AngleAxis(renderable.Rotation, Vector3.up),
				renderable.Scale
			),
			renderable.Material,
			parms.DrawNow
		);
		return true;
	}
}

public struct FacePartRenderable : IFaceRenderable
{
	public Material Material { get; }
	public Color Color { get; }
	public Vector3 Scale { get; }
	public Vector3 Offset { get; }
	public float Rotation { get; }
	public float Order { get; }

	public bool FlipX;
	public SlotDef? Slot;
	public FacePartDef? Part;
	public Texture2D? TextureOverride;
	public Func<FacePartRenderable, FacePartRenderable>? ModifyFunction;

	public FacePartRenderable(Material mat, Color col, Vector3 offset, Vector3 scale, float rotation, bool flip_x, float? order = null) : this()
	{
		Offset = offset;
		Scale = scale;
		Rotation = rotation;

		Material = mat;
		Color = col;
		FlipX = flip_x;
		Order = order ?? offset.y;
	}
}

public static class RendererExtensions
{
	public static Shader DefaultShader => FaceRenderer.SkinShader;

	public static bool ApplyAll<TRenderable>(this IFaceRenderTarget<TRenderable> render_target, IEnumerable<TRenderable> renderables)
		where
			TRenderable : IFaceRenderable
	{
		return renderables
			.OrderBy(x => x.Order)
			.All(render_target.Apply)
		;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Shader GetShader(string? shader_path, PawnDrawParms? parms = null)
		=> (parms?.Statue == true)
			? ShaderDatabase.Cutout
			: shader_path.NullOrEmpty()
				? DefaultShader
				: ShaderDatabase.LoadShader(shader_path) ?? DefaultShader
		;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Color GetColorFor(this PartColor color, Comp_TSFace face, FaceSide side = FaceSide.None, Color? custom_color = null, PawnDrawParms? parms = null)
		=> parms?.statueColor ?? custom_color ?? color switch
		{
			PartColor.Eye => face.GetEyeColor(side),
			PartColor.Skin => face.Pawn.story.SkinColor,
			PartColor.Hair => face.Pawn.story.HairColor,
			PartColor.Sclera => face.GetScleraColor(side),
			PartColor.None or _ => Color.white,
		};

	public static IEnumerable<FacePartRenderable> CollectAdditional(
		this Comp_TSFace face,
		Rot4 rot,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		foreach (var part in face.PersistentData.ExtraParts)
		{

		}
	}

	public static bool TryCreateFaceRenderable(
		Comp_TSFace face,
		FacePartWithTransform comp_part,
		SlotDef slot,
		FaceSide side,
		Rot4 rot,
		[NotNullWhen(true)] out FacePartRenderable? res,
		Vector3 extra_offset = default,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		res = default;
		var pawn = face.Pawn;
		var def = comp_part.Def;
		if (part_predicate?.Invoke(def) == false)
			return false;

		string? path = comp_part.Def.GetGraphicPath(face, side);

		if (path.NullOrEmpty())
			return false; // this is fine, part may not have a graphic for this state

		var color = def.color.GetColorFor(face, side, def.customColor);
		var shader = GetShader(def.shader);
		var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
		if (graphic is null)
		{
			TSFacesMod.Logger.Warning($"Unable to get graphic to draw slot {slot} for {pawn}", (comp_part, pawn, path).GetHashCode());
			return false;
		}

		bool flip = !def.noMirror && rot.AsInt switch
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
			TSFacesMod.Logger.Warning($"Unable to get material to draw slot {slot} for {pawn}", (graphic, pawn).GetHashCode());
			return false;
		}

		var transform = comp_part.Transform.ForRot(rot);
		var pos = extra_offset
			+ transform.Offset
			+ def.offset.ToUpFacingVec3(slot.layerOffset)
		;
		var draw_scale = def.drawSize.ToUpFacingVec3(1)
			.MultipliedBy(transform.Scale.ToUpFacingVec3(1))
		;
		var rotation = transform.RotationOffset;

		res = new(part_mat, color, pos, draw_scale, rotation, flip, slot.order)
		{
			Slot = slot,
			Part = def,
			ModifyFunction = args_in =>
			{
				if (args_in.Slot is null || args_in.Slot != SlotDefOf.Eye)
					return args_in;

				var edit_mat = args_in.Material;
				// TSFacesMod.Logger.Info($"Drawing an eye for '{pawn}',   side: '{rot.ToStringHuman()}',  part: '{args_in.Part}',   tex: '{edit_mat.mainTexture}'");

				if (face.TryGetPartForSlot(SlotDefOf.Iris, side, out var iris))
				{
					var iris_graphic = GraphicDatabase.Get<Graphic_Multi>(iris.Def.graphicPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
					var iris_side_mat = iris_graphic.MatAt(rot);
					edit_mat.SetTexture("_IrisTex", iris_side_mat.mainTexture);
				}

				edit_mat.SetTexture("_EyeTex", edit_mat.mainTexture);

				edit_mat.SetColor("_SkinColor", pawn.story.SkinColor);
				edit_mat.SetColor("_LashColor", pawn.story.hairColor);
				edit_mat.SetColor("_ScleraColor", face.GetScleraColor(side));
				edit_mat.SetColor("_IrisColor", face.GetEyeColor(side));

				edit_mat.SetFloat("_Flipped", flip ? 1 : 0);
				return args_in with
				{
					FlipX = false, // the shader handles the flipping here
				};
			},
		};
		return res is not null;
	}

	public static IEnumerable<FacePartRenderable> CollectRenderables(
		this FaceLayout layout,
		Comp_TSFace face,
		Rot4 rot,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		var pawn = face.Pawn;
		var side_layout = layout.ForRot(rot);

		foreach (var layout_part in side_layout)
		{
			var side = layout_part.side;
			if (!face.TryGetPartForSlot(layout_part.slot, layout_part.side, out var comp_part))
			{
				TSFacesMod.Logger.Warning($"FaceRenderer unable to get part for slot '{layout_part.slot.defName}' for pawn {pawn}", (pawn, layout_part, layout_part.side).GetHashCode());
				continue;
			}

			var def = comp_part.Def;
			if (part_predicate?.Invoke(def) == false)
				continue;

			string? path = comp_part.Def.GetGraphicPath(face, side);

			if (path.NullOrEmpty())
				continue; // this is fine, part may not have a graphic for this state

			var color = def.color.GetColorFor(face, side, def.customColor);
			var shader = GetShader(def.shader);
			var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
			if (graphic is null)
			{
				TSFacesMod.Logger.Warning($"Unable to get graphic to draw slot {layout_part.slot} for {pawn}", (layout_part, pawn, path).GetHashCode());
				continue;
			}

			bool flip = !def.noMirror && rot.AsInt switch
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
				TSFacesMod.Logger.Warning($"Unable to get material to draw slot {layout_part.slot} for {pawn}", (graphic, pawn).GetHashCode());
				continue;
			}

			var transform = comp_part.Transform.ForRot(rot);
			var pos = layout_part.pos.ToUpFacingVec3()
				+ transform.Offset
				+ def.offset.ToUpFacingVec3(layout_part.slot.layerOffset)
			;
			var draw_scale = def.drawSize.ToUpFacingVec3(1)
				.MultipliedBy(transform.Scale.ToUpFacingVec3(1))
			;
			var rotation = transform.RotationOffset;

			yield return new(part_mat, color, pos, draw_scale, rotation, flip, layout_part.slot.order)
			{
				Slot = layout_part.slot,
				Part = def,
				ModifyFunction = args_in =>
				{
					if (args_in.Slot is null || args_in.Slot != SlotDefOf.Eye)
						return args_in;

					var edit_mat = args_in.Material;
					// TSFacesMod.Logger.Info($"Drawing an eye for '{pawn}',   side: '{rot.ToStringHuman()}',  part: '{args_in.Part}',   tex: '{edit_mat.mainTexture}'");

					if (face.TryGetPartForSlot(SlotDefOf.Iris, side, out var iris))
					{
						var iris_graphic = GraphicDatabase.Get<Graphic_Multi>(iris.Def.graphicPath, ShaderDatabase.Cutout, Vector2.one, Color.white);
						var iris_side_mat = iris_graphic.MatAt(rot);
						edit_mat.SetTexture("_IrisTex", iris_side_mat.mainTexture);
					}

					edit_mat.SetTexture("_EyeTex", edit_mat.mainTexture);

					edit_mat.SetColor("_SkinColor", pawn.story.SkinColor);
					edit_mat.SetColor("_LashColor", pawn.story.hairColor);
					edit_mat.SetColor("_ScleraColor", face.GetScleraColor(side));
					edit_mat.SetColor("_IrisColor", face.GetEyeColor(side));

					edit_mat.SetFloat("_Flipped", flip ? 1 : 0);
					return args_in with
					{
						FlipX = false, // the shader handles the flipping here
					};
				},
			};
		}
	}
}
