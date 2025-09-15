using System;
using System.Collections.Generic;
using System.Linq;
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

public class FaceRTTarget(RenderTexture rt) : IFaceRenderTarget<TexShaderRenderable>
{
	public RenderTexture RT = rt;

	public bool Apply(TexShaderRenderable renderable)
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

public class FaceMeshTarget(Mesh mesh, Matrix4x4 mat, PawnDrawParms parms) : IFaceRenderTarget<TexShaderRenderable>
{
	public bool Apply(TexShaderRenderable renderable)
	{
		renderable = renderable.ModifyFunction?.Invoke(renderable) ?? renderable;
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

public struct TexShaderRenderable : IFaceRenderable
{
	public Shader Shader { get; }
	public Material Material { get; }
	public Color Color { get; }
	public Vector3 Scale { get; }
	public Vector3 Offset { get; }
	public float Rotation { get; }
	public bool FlipX { get; }
	public float Order { get; }

	public Texture2D? TextureOverride;
	public Func<TexShaderRenderable, TexShaderRenderable>? ModifyFunction;
}

public static class RendererExtensions
{
	public static bool ApplyAll<TRenderable>(this IFaceRenderTarget<TRenderable> render_target, IEnumerable<TRenderable> renderables)
		where
			TRenderable : IFaceRenderable
	{
		return renderables
			.OrderBy(x => x.Order)
			.All(render_target.Apply)
		;
	}
}
