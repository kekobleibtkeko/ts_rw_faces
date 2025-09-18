using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Faces.Mod;
using TS_Lib.Util;
using Verse;
using static TS_Lib.Util.TSUtil;

namespace TS_Faces.Data;

public class PawnFilterEntry
{
	public enum FilterLevel
	{
		None,
		Needed,
		Disallowed,
	}
	public GeneDef? gene;
	public TraitDef? trait;
	public HediffDef? hediff;
	public Gender gender = Gender.None;
	public FloatRange? beautyRange;
	public bool? ghoul;

	public int commonalityOffset = default;
	public FilterLevel filter = FilterLevel.None;

	public IEnumerable<string> GetConfigErrors()
	{
		Def?[] defs = [
			gene,
			trait,
			hediff
		];

		if (filter == FilterLevel.Disallowed && commonalityOffset != default)
			yield return $"can't set {nameof(filter)} to '{FilterLevel.Disallowed}' and {nameof(commonalityOffset)} on {GetType()}, filter: {ToString()}";

		yield break;
	}

	public override string ToString()
	{
		Def?[] defs = [
			gene,
			trait,
			hediff
		];
		var defs_str = string.Join(", ", defs.Select(x => x?.label ?? "none"));

		return $"PartFilter(defs={defs_str}, ghoul={ghoul}, beatuy={beautyRange} filter={filter}, offset={commonalityOffset})";
	}
}

public interface IPawnFilterable
{
	IEnumerable<PawnFilterEntry> FilterEntries { get; }
	int Commonality { get; }
}

public static class PartFilterExtensions
{
	public const int UNACCEPTABLE_VAL = -999;
	public const int FIT_VAL = 1;
	public const int UNFIT_VAL = -1;

	public const int COMMONALITY_OFFSET_VAL = 10;
	public const float BEAUTY_BONUS = 1.5f;

	public const float DEFAULT_FIT = 0.001f;

	public readonly struct RandomFilterContainer<T>(T value, Pawn pawn) : IWeightedRandom<T>
		where
			T : IPawnFilterable
	{
		public int Weight { get; } = value.Commonality + value.FilterEntries.Sum(x => x.CommonalityFor(pawn));
		public T Value { get; } = value;
	}

	public static int CommonalityFor(this PawnFilterEntry entry, Pawn pawn)
	{
		int res = entry.commonalityOffset;
		if (!FacesSettings.Instance.StrictGender && entry.gender != Gender.None)
		{
			res += entry.gender == pawn.gender
				? COMMONALITY_OFFSET_VAL
				: -COMMONALITY_OFFSET_VAL
			;
		}
		else
		{
			res += COMMONALITY_OFFSET_VAL;
		}

		if (!FacesSettings.Instance.StrictBeauty && entry.beautyRange.HasValue)
		{
			var dist = entry.beautyRange.Value.DistanceFrom(pawn.GetStatValue(StatDefOf.PawnBeauty, cacheStaleAfterTicks: 1));
			res += (int)((BEAUTY_BONUS - dist) * COMMONALITY_OFFSET_VAL);
		}

		return res;
	}
	public static float FilterValueFor(this PawnFilterEntry entry, Pawn pawn, StringBuilder? reason_builder = null)
	{
		float get_accept_val(
			bool _test_val,
			out bool _unacceptable,
			bool _extra_unacceptable_true = false,
			bool _extra_unacceptable_false = false
		)
		{
			float _res = 0;
			_unacceptable = false;
			if (_test_val)
			{
				if (entry.filter == PawnFilterEntry.FilterLevel.Disallowed || _extra_unacceptable_true)
				{
					_unacceptable = true;
					return UNACCEPTABLE_VAL;
				}
				_res += FIT_VAL;
			}
			else
			{
				if (entry.filter == PawnFilterEntry.FilterLevel.Needed || _extra_unacceptable_false)
				{
					_unacceptable = true;
					return UNACCEPTABLE_VAL;
				}
				_res += UNFIT_VAL;
			}
			return _res;
		}

		float res = 0;
		bool unacceptable;
		if (entry.gender != Gender.None)
		{
			res += get_accept_val(
				pawn.gender == entry.gender,
				out unacceptable,
				_extra_unacceptable_false: FacesSettings.Instance.StrictGender
			);
			if (unacceptable)
			{
				entry.AddReason(reason_builder, $"wrong gender, needs {entry.gender}");
				return res;
			}
		}

		if (ModsConfig.AnomalyActive && entry.ghoul.HasValue)
		{
			res += get_accept_val(
				pawn.gender == entry.gender,
				out unacceptable
			);
			if (unacceptable)
			{
				entry.AddReason(reason_builder, $"wrong ghoul state, needs {entry.ghoul.Value}");
				return res;
			}
		}

		if (FacesSettings.Instance.StrictBeauty && entry.beautyRange.HasValue)
		{
			var beauty = pawn.GetStatValue(StatDefOf.PawnBeauty, cacheStaleAfterTicks: 1);
			if (entry.beautyRange.Value.Includes(beauty))
				res += FIT_VAL;
			else
				res -= entry.beautyRange.Value.DistanceFrom(beauty);
		}

		if (entry.trait is not null)
		{
			res += get_accept_val(
				pawn.story.traits.HasTrait(entry.trait),
				out unacceptable
			);
			if (unacceptable)
			{
				entry.AddReason(reason_builder, $"trait {entry.trait} missing");
				return res;
			}
		}

		if (entry.hediff is not null)
		{
			res += get_accept_val(
				pawn.health.hediffSet.HasHediff(entry.hediff),
				out unacceptable
			);
			if (unacceptable)
			{
				entry.AddReason(reason_builder, $"hediff {entry.hediff} missing");
				return res;
			}
		}

		if (ModsConfig.BiotechActive && entry.gene is not null)
		{
			res += get_accept_val(
				pawn.genes.HasActiveGene(entry.gene),
				out unacceptable
			);
			if (unacceptable)
			{
				entry.AddReason(reason_builder, $"gene {entry.trait} missing");
				return res;
			}
		}

		if (res <= 0)
		{
			entry.AddReason(reason_builder, "score too low");
		}
		return res;
	}

	public static void AddReason(this PawnFilterEntry filterable, StringBuilder? builder, string reason)
	{
		builder.AppendReason($"[{filterable}] {reason}");
	}
	public static bool FilterFits<T>(this T filterable, Pawn pawn, out float fit_val, StringBuilder? reason_builder = null)
		where
			T : IPawnFilterable
	{
		fit_val = DEFAULT_FIT;
		foreach (var filter in filterable.FilterEntries)
		{
			fit_val += filter.FilterValueFor(pawn, reason_builder);
		}
		return fit_val > 0;
	}

	public static T? GetActiveFromFilters<T>(this IEnumerable<T> parts, Pawn pawn)
		where
			T : IPawnFilterable
	{
		T? highest = default;
		float highest_fit = 0;
		foreach (var part in parts)
		{
			if (!part.FilterFits(pawn, out var current_fit)
				|| current_fit <= highest_fit)
				continue;

			highest = part;
			highest_fit = current_fit;
		}

		return highest;
	}

	public static T? GetRandomFor<T>(this IEnumerable<T> defs, Pawn pawn, StringBuilder? reasons = null)
		where
			T : IPawnFilterable, IComparable<T>
	{
		var fitting = defs
			.Select(def =>
			{
				if (def.FilterFits(pawn, out _, reasons))
					return (T?)def;
				return default;
			})
			.Where(def => def is not null)
			.Select(def => new RandomFilterContainer<T>(def!, pawn))
		;
		return fitting.GetRandom<RandomFilterContainer<T>, T>();
	}
}