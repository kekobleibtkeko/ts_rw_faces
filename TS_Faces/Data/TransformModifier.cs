using System;
using HarmonyLib;
using TS_Lib.Transforms;
using TS_Lib.Util;
using UnityEngine;
using Verse;

namespace TS_Faces.Data;

public class TransformModifier : IMirrorable<TransformModifier>, ICreateCopy<TransformModifier>
{
	public enum MirrorType
	{
		Mirrored,
		None,
		Random,
	}

	public MirrorType mirror = MirrorType.Mirrored;
	
	public TransformModifierSide north = default!;
	public TransformModifierSide east = new();
	public TransformModifierSide south = new();
	public TransformModifierSide west = default!;

	public TransformModifierSide ForRot(Rot4 rot) => rot.AsInt switch
	{
		Rot4.EastInt => east,
		Rot4.WestInt => west ??= east.CreateCopy(),
		Rot4.NorthInt => north ??= south.Mirror(),
		Rot4.SouthInt or _ => south
	};
	public ref TransformModifierSide RefForRot(Rot4 rot)
	{
		switch (rot.AsInt)
		{
			case Rot4.EastInt:
				return ref east;
			case Rot4.WestInt:
				return ref west;
			case Rot4.NorthInt:
				return ref north;
			case Rot4.SouthInt:
				return ref south;
		}
		throw new Exception("invalid side");
	}

	public TransformModifier CreateCopy() => new()
	{
		north = north?.CreateCopy() ?? new(),
		east = east?.CreateCopy() ?? new(),
		south = south?.CreateCopy() ?? new(),
		west = west?.CreateCopy() ?? new()
	};

	public void ApplyTo(TSTransform4 transform)
	{
		Rot4.AllRotations.Do(rot => ForRot(rot).ApplyTo(transform.ForRot(rot)));
	}

	public TransformModifier Mirror()
	{
		var copy = CreateCopy();
		copy.east = copy.east.Mirror();
		copy.west = copy.west.Mirror();
		copy.south = copy.south.Mirror();
		copy.north = copy.north.Mirror();
		return copy;
	}

	public TransformModifier Collapse(int? seed = null)
	{
		var mod = new TransformModifier();
		var nn_seed = seed ?? Verse.Rand.Int;
		foreach (var rot in Rot4.AllRotations)
		{
			ref var mod_side = ref mod.RefForRot(rot);
			var act_seed = mirror switch
			{
				MirrorType.Random => nn_seed + rot.AsInt,
				_ => nn_seed
			};
			mod_side = ForRot(rot).Collapse(act_seed);
		}
		return mod;
	}

	public TSTransform4 ToTransform()
	{
		TSTransform4 res = new();
		foreach (var rot in Rot4.AllRotations)
		{
			// really no clue why this is necessary why ahhhhhhhhhh
			var flip = rot.IsHorizontal;
			var side = ForRot(rot);
			var tr = res.ForRot(rot);

			var offset = side.offset;
			if (flip)
				offset.x *= -1;
			tr.Offset = offset.ToUpFacingVec3();
			tr.RotationOffset = side.rotation;
			tr.Scale = side.scale;
		}
		return res;
	}
}
public class TransformModifierSide : IMirrorable<TransformModifierSide>, ICreateCopy<TransformModifierSide>
{
	public Vector2 offset = Vector2.zero;
	public Vector2? offsetMax;
	public Vector2 scale = Vector2.one;
	public Vector2? scaleMax;
	public float rotation = 0;
	public float? rotationMax;

	public TransformModifierSide CreateCopy() => this.DirtyClone() ?? throw new System.Exception("unable to clone face part modifier");

	public TransformModifierSide Mirror()
	{
		var cpy = CreateCopy();
		cpy.offset.x *= -1;
		if (offsetMax.HasValue)
			cpy.offsetMax = cpy.offsetMax!.Value with { x = cpy.offsetMax.Value.x * -1 };
		cpy.rotation = 360 - cpy.rotation;
		if (rotationMax.HasValue)
			cpy.rotationMax = 360 - cpy.rotationMax;
		return cpy;
	}

	public void ApplyTo(TSTransform transform)
	{
		// really no clue why this is necessary
		if (transform.Rotation.IsHorizontal)
		{
			transform.Offset -= offset.ToUpFacingVec3();
			// transform.RotationOffset -= rotation;
		}
		else
		{
			transform.Offset += offset.ToUpFacingVec3();
			// transform.RotationOffset += rotation;
		}
		transform.RotationOffset += rotation;
		transform.Scale *= scale;
	}

	public TransformModifierSide Collapse(int? seed = null)
	{
		using var _ = Verse.Rand.Block(seed ?? Verse.Rand.Int);
		return new()
		{
			rotation = Mathf.Lerp(rotation, rotationMax ?? rotation, Verse.Rand.Value),
			offset = new(
				Mathf.Lerp(offset.x, offsetMax?.x ?? offset.x, Verse.Rand.Value),
				Mathf.Lerp(offset.y, offsetMax?.y ?? offset.y, Verse.Rand.Value)
			),
			scale = new(
				Mathf.Lerp(scale.x, scaleMax?.x ?? scale.x, Verse.Rand.Value),
				Mathf.Lerp(scale.y, scaleMax?.y ?? scale.y, Verse.Rand.Value)
			),
		};
	}
}