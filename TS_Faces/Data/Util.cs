using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using TS_Faces.Comps;
using UnityEngine;
using Verse;
using static Verse.PawnRenderNodeProperties;

namespace TS_Faces.Data;

public enum PartColor
{
    None,
    Eye,
    Sclera,
    Skin,
    Hair,
}

public enum PawnState
{
    Normal,
    Sleeping,
    Dead,
    Dessicated
}

public enum FaceSide
{
    None,
    Left,
    Right,
}

public static class FacesUtil
{
	private static readonly Lazy<Dictionary<SlotDef, List<FacePartDef>>> _PartsForSlots = new(()
		=> DefDatabase<FacePartDef>.AllDefsListForReading
			.GroupBy(def => def.slot)
			.ToDictionary(
				group => group.Key,
				group => group.ToList()
			)
	);
	public static Dictionary<SlotDef, List<FacePartDef>> PartsForSlots => _PartsForSlots.Value;

	private static readonly Lazy<Dictionary<SlotDef, List<FacePartDef>>> _RandomParts = new(()
		=> PartsForSlots
			.ToDictionary(
				kv => kv.Key,
				kv => kv.Value.Where(x => x.commonality > 0).ToList()
			)
	);
	public static Dictionary<SlotDef, List<FacePartDef>> RandomParts => _RandomParts.Value;

	public static TaggedString ModTranslate(this string str) => $"Faces.{str.Replace(' ', '_')}".Translate();
}

public static class FaceSlotExtensions
{
	public const float EPSILON = 0.0004f;
	public const float EPSILON_2 = EPSILON * 2;
	public const float EPSILON_3 = EPSILON * 3;
	public const float EPSILON_4 = EPSILON * 4;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FaceSide Mirror(this FaceSide side) => side switch
	{
		FaceSide.Left => FaceSide.Right,
		FaceSide.Right => FaceSide.Left,
		FaceSide.None or _ => FaceSide.None,
	};
}
