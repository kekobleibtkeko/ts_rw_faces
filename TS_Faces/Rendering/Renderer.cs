using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using RimWorld;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Faces.Mod;
using TS_Faces.Util;
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
	public bool ReverseX = false;

	public bool Apply(FacePartRenderable renderable)
	{
		renderable = renderable.ModifyFunction?.Invoke(renderable) ?? renderable;
		if (ReverseX)
		{
			// TODO: figure out why this hack is needed
			renderable = renderable with
			{
				Offset = renderable.Offset.ScaledBy(new(-1, 1, 1)),
				Rotation = 360 - renderable.Rotation,
			};
		}
		// TSFacesMod.Logger.Verbose($"Drawing:: Rotation {renderable.Rot.ToStringHuman()}: {(renderable.Side == FaceSide.None ? "" : renderable.Side.ToString())} {renderable.Slot}, Part '{renderable.Part}' | Offset: {renderable.Offset},  Scale: {renderable.Scale},  Rotation: {renderable.Rotation},  Flipped: {renderable.FlipX}");
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
	public Mesh Mesh { get; } = mesh;
	public Matrix4x4 Mat { get; } = mat;
	public PawnDrawParms Parms { get; } = parms;

	public bool Apply(FacePartRenderable renderable)
	{
		renderable = renderable.ModifyFunction?.Invoke(renderable) ?? renderable;
		// TSFacesMod.Logger.Verbose(
		// 	$"Drawing {renderable.Face.Pawn}: Rotation {renderable.Rot.ToStringHuman()}: {(renderable.Side == FaceSide.None ? "" : renderable.Side.ToString())} {renderable.Slot}, Part '{renderable.Part}' | Offset: {renderable.Offset},  Scale: {renderable.Scale},  Rotation: {renderable.Rotation},  Flipped: {renderable.FlipX}",
		// 	(renderable.FlipX, renderable.Face, renderable.Side, renderable.Rot, renderable.Slot).GetHashCode()
		// );
		var use_mesh = Mesh;
		if (renderable.FlipX)
		{
			use_mesh = use_mesh.GetFlippedMesh();
			// TSFacesMod.Logger.Verbose(
			// 	$"fleeping",
			// 	(renderable.FlipX, renderable.Face, renderable.Side, renderable.Rot, renderable.Slot).GetHashCode()
			// );
		}
		renderable = renderable with
		{
			// The projection seems to have a scale offset of 1.5 from orth -> map
			Offset = renderable.Offset.ScaledBy(new(PawnRenderNodeWorker_TSFace.ts_face_project_bonus, 1, PawnRenderNodeWorker_TSFace.ts_face_project_bonus))
		};
		GenDraw.DrawMeshNowOrLater(
			use_mesh,
			Mat * Matrix4x4.TRS(
				renderable.Offset,
				Quaternion.AngleAxis(renderable.Rotation, Vector3.up),
				renderable.Scale
			),
			renderable.Material,
			Parms.DrawNow
		);
		return true;
	}
}

public struct FacePartRenderable : IFaceRenderable
{
	public Material Material { get; }
	public float Order { get; }

	public Color Color;
	public Vector3 Scale;
	public Vector3 Offset;
	public float Rotation;

	public bool FlipX;
	public SlotDef? Slot;
	public FacePartDef? Part;
	public Texture2D? TextureOverride;

	public Func<FacePartRenderable, FacePartRenderable>? ModifyFunction;

	public Rot4 Rot;
	public FaceSide Side;
	public Comp_TSFace Face;

	public FacePartRenderable(Comp_TSFace face, Material mat, Color col, Vector3 offset, Vector3 scale, float rotation, bool flip_x, float? order = null) : this()
	{
		Face = face;
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryCreateFaceRenderableFromPart(
		Comp_TSFace face,
		FacePartWithTransform comp_part,
		SlotDef slot,
		FaceSide side,
		Rot4 rot,
		[NotNullWhen(true)] out FacePartRenderable? res,
		Vector3 extra_offset = default,
		float? extra_rotation = default,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		res = default;
		var pawn = face.Pawn;
		var part_def = comp_part.Def;
		if (part_predicate?.Invoke(part_def) == false)
			return false;

		string? path = comp_part.Def.GetGraphicPath(face, side);

		if (path.NullOrEmpty())
			return false; // this is fine, part may not have a graphic for this state

		var color = part_def.color.GetColorFor(face, side, part_def.customColor);
		var shader = GetShader(part_def.shader);
		var graphic = GraphicDatabase.Get<Graphic_Multi>(path, shader, Vector2.one, color);
		if (graphic is null)
		{
			TSFacesMod.Logger.Warning($"Unable to get graphic to draw slot {slot} for {pawn}", (comp_part, pawn, path).GetHashCode());
			return false;
		}

		bool flip = !part_def.noMirror && rot.AsInt switch
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
			+ part_def.offset.ToUpFacingVec3(slot.layerOffset + part_def.layerOffset + PawnRenderNodeWorker_TSFace.ts_face_tweak)
		;
		var draw_scale = part_def.drawSize.ToUpFacingVec3(1)
			.MultipliedBy(transform.Scale.ToUpFacingVec3(1))
		;
		var rotation = transform.RotationOffset
			+ extra_rotation ?? 0
			+ part_def.rotation
		;

		res = new(
			face,
			mat: part_mat,
			col: color,
			offset: pos,
			scale: draw_scale,
			rotation: rotation,
			flip_x: flip,
			order: slot.order
		)
		{
			Slot = slot,
			Part = part_def,
			Rot = rot,
			Side = side,
			ModifyFunction = args_in =>
			{
				if (args_in.Slot is null || args_in.Slot != SlotDefOf.Eye)
					return args_in;

				var edit_mat = args_in.Material;
				// TSFacesMod.Logger.Info($"Drawing an eye for '{pawn}',   side: '{rot.ToStringHuman()}',  part: '{args_in.Part}',   tex: '{edit_mat.mainTexture}'");

				Vector2 eye_offset = default;
				Vector2 iris_offset = default;
				Vector2 hl_offset = iris_offset + args_in.Part?.highlightOffset ?? default;
				Vector3 rotations = default;
				rotations.x = args_in.Rotation;

				Vector2 eye_scale = args_in.Scale / (args_in.Part?.drawSize ?? Vector2.one);
				Vector2 iris_scale = Vector2.one;

				// reset the detail textures so the last ones aren't used
				edit_mat.SetTexture("_IrisTex", null);
				edit_mat.SetTexture("_HLTex", null);

				if (face.TryGetPartForSlot(SlotDefOf.Iris, side, out var iris)
					&& iris.Def.GetGraphicPath(face, side) is string iris_path
					&& !string.IsNullOrEmpty(iris_path))
				{
					var iris_graphic = GraphicDatabase.Get<Graphic_Multi>(iris_path, ShaderDatabase.Cutout, Vector2.one, Color.white);
					var iris_side_mat = iris_graphic.MatAt(rot);
					edit_mat.SetTexture("_IrisTex", iris_side_mat.mainTexture);

					var iris_tr = iris.Transform.ForRot(rot);
					iris_offset += iris_tr.Offset.FromUpFacingVec3();
					iris_scale *= iris_tr.Scale;

					hl_offset += iris.Def.highlightOffset;
					rotations.y = iris.Def.rotation + iris_tr.RotationOffset;
				}

				if (face.TryGetPartForSlot(SlotDefOf.Highlight, side, out var hl)
					&& hl.Def.GetGraphicPath(face, side) is string hl_path
					&& !string.IsNullOrEmpty(hl_path))
				{
					var hl_graphic = GraphicDatabase.Get<Graphic_Multi>(hl_path, ShaderDatabase.Cutout, Vector2.one, Color.white);
					var hl_side_mat = hl_graphic.MatAt(rot);
					edit_mat.SetTexture("_HLTex", hl_side_mat.mainTexture);

					var hl_tr = hl.Transform.ForRot(rot);
					hl_offset += hl_tr.Offset.FromUpFacingVec3();
					rotations.z = hl.Def.rotation + hl_tr.RotationOffset;
				}

				edit_mat.SetVector("_EyeOffset", new(eye_offset.x, eye_offset.y));
				edit_mat.SetVector("_IrisOffset", new(iris_offset.x, iris_offset.y, hl_offset.x, hl_offset.y));
				Vector4 final_rotations = new(rotations.x, rotations.y, rotations.z, 0);
				// these hacks are getting stupid... sorry future me
				edit_mat.SetVector("_Rotations", final_rotations * (flip || rot == Rot4.West ? -1 : 1));
				edit_mat.SetVector("_Scalses", new(rotations.x, rotations.y, rotations.z));

				edit_mat.SetTexture("_EyeTex", edit_mat.mainTexture);

				edit_mat.SetColor("_SkinColor", pawn.story.SkinColor);
				edit_mat.SetColor("_LashColor", pawn.story.hairColor);
				edit_mat.SetColor("_ScleraColor", face.GetScleraColor(side));
				edit_mat.SetColor("_IrisColor", face.GetEyeColor(side));

				edit_mat.SetFloat("_Flipped", flip ? 1 : 0);
				var offset = args_in.Offset.FromUpFacingVec3();
				offset -= args_in.Part?.offset ?? default;

				return args_in with
				{
					// the shader handles flipping and some transforms
					FlipX = false,
					Rotation = 0,
					Offset = offset.ToUpFacingVec3(args_in.Offset.y),
				};
			},
		};
		return res is not null;
	}

	public static IEnumerable<FacePartRenderable> CollectOnFaceRenderables(
		this Comp_TSFace face,
		Rot4 rot
	)
	{
		var pawn = face.Pawn;
		if (pawn.style?.FaceTattoo is TattooDef tattoo
			&& !tattoo.noGraphic
			&& tattoo.GraphicFor(pawn, FaceRenderer.TattooColor).MatAt(rot).mainTexture is Texture2D texture
		)
		{
			yield return new(
				face,
				mat: new(FaceRenderer.TransparentShader),
				col: FaceRenderer.TattooColor,
				offset: default,
				scale: Vector3.one,
				rotation: 0,
				flip_x: false,
				order: SlotDefOf.SkinDecor.order + 1
			)
			{
				TextureOverride = texture
			};
		}


	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<FacePartRenderable> CollectNeededAndExtraRenderables(
		this FaceLayoutDef layout,
		Comp_TSFace face,
		Rot4 rot,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		return Enumerable.Empty<FacePartRenderable>()
			.Concat(CollectExtraRenderables(face, rot, part_predicate))
			.Concat(CollectLayoutRenderables(face.GetActiveFaceLayout(), face, rot, part_predicate))
		;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<FacePartRenderable> CollectExtraRenderables(
		this Comp_TSFace face,
		Rot4 rot,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		var fitting_parts = face.PersistentData.ExtraParts
			.Where(p => p.Def.force || p.Def.FilterFits(face.Pawn, out _))
			.SelectMany(p => p.GetFor(face))
		;
		foreach (var part in fitting_parts)
		{
			if (TryCreateFaceRenderableFromPart(
					face,
					comp_part: part,
					slot: part.Slot ?? part.Def.slot,
					side: part.Side,
					rot: rot,
					out var renderable,
					extra_offset: default,
					extra_rotation: default,
					part_predicate
				))
				yield return renderable.Value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static IEnumerable<FacePartRenderable> CollectLayoutRenderables(
		this FaceLayoutDef layout,
		Comp_TSFace face,
		Rot4 rot,
		Predicate<FacePartDef>? part_predicate = null
	)
	{
		var pawn = face.Pawn;
		// TSFacesMod.Logger.Verbose($"collecting layout renderables for {pawn}, rot: {rot.ToStringHuman()}");
		var side_layout = layout.ForRot(rot);

		foreach (var layout_part in side_layout.Parts)
		{
			var side = layout_part.side;
			if (!face.TryGetPartForSlot(layout_part.slot, layout_part.side, out var comp_part))
			{
				TSFacesMod.Logger.Warning($"FaceRenderer unable to get part for slot '{layout_part.slot.defName}' for pawn {pawn}", (pawn, layout_part, layout_part.side).GetHashCode());
				continue;
			}
			// TSFacesMod.Logger.Verbose($"part for slot {layout_part.slot}: {comp_part.Def}");
			// TSFacesMod.Logger.Verbose($"transform: ({layout_part.pos} : {layout_part.rotation})");

			if (TryCreateFaceRenderableFromPart(
					face,
					comp_part: comp_part,
					slot: layout_part.slot,
					side: layout_part.side,
					rot: rot,
					out var renderable,
					extra_offset: layout_part.pos.ToUpFacingVec3(),
					extra_rotation: layout_part.rotation,
					part_predicate
				))
				yield return renderable.Value;
		}
	}
}
