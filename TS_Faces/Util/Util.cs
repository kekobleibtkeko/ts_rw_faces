using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;
using TS_Faces.Comps;
using TS_Faces.Data;
using TS_Lib.Util;
using UnityEngine;
using Verse;
using static Verse.PawnRenderNodeProperties;

namespace TS_Faces.Util;

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

	private static readonly Lazy<Dictionary<SlotDef, List<FacePartModifierDef>>> _PartModifiers = new(()
		=> DefDatabase<FacePartModifierDef>.AllDefsListForReading
			.GroupBy(def => def.slot)
			.ToDictionary(
				group => group.Key,
				group => group.ToList()
			)
	);
	public static Dictionary<SlotDef, List<FacePartModifierDef>> PartModifiers => _PartModifiers.Value;

	private static readonly Lazy<Dictionary<ExtraPartDef.PartDetail, List<ExtraPartDef>>> _ExtraParts = new(()
		=> DefDatabase<ExtraPartDef>.AllDefsListForReading
			.GroupBy(def => def.detail)
			.ToDictionary(
				group => group.Key,
				group => group.ToList()
			)
	);
	public static Dictionary<ExtraPartDef.PartDetail, List<ExtraPartDef>> ExtraParts => _ExtraParts.Value;

	private static readonly Lazy<Dictionary<ExtraPartDef.PartDetail, List<ExtraPartDef>>> _RandomExtraParts = new(()
		=> ExtraParts
			.ToDictionary(
				kv => kv.Key,
				kv => kv.Value.Where(x => x.commonality > 0).ToList()
			)
	);
	public static Dictionary<ExtraPartDef.PartDetail, List<ExtraPartDef>> RandomExtraParts => _RandomExtraParts.Value;

	private static readonly Lazy<List<HeadDef>> _Heads = new(() => [.. DefDatabase<HeadDef>.AllDefsListForReading]);
	public static List<HeadDef> Heads => _Heads.Value;

	public static TaggedString ModTranslate(this string str, params NamedArgument[] args) => $"Faces.{str.Replace(' ', '_')}".Translate(args);
	public static (TaggedString, TaggedString) ModLabelDesc(this string key, params NamedArgument[] args) => TSUtil.LabelDesc(key, ModTranslate, args);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static FaceSide Mirror(this FaceSide side) => side switch
	{
		FaceSide.Left => FaceSide.Right,
		FaceSide.Right => FaceSide.Left,
		FaceSide.None or _ => FaceSide.None,
	};
}