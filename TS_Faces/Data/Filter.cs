using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using TS_Faces.Mod;
using TS_Lib.Util;
using Verse;
using static TS_Lib.Util.TSUtil;

namespace TS_Faces.Data;

public interface IPawnFilterEntry
{
	public class TraitEntry
	{
		public TraitDef def = default!;
		public int? degree;

		public bool ValidForPawn(Pawn pawn)
		{
			if (degree.HasValue)
				return pawn.story.traits.HasTrait(def, degree.Value);
			else
				return pawn.story.traits.HasTrait(def);
		}
	}

	public enum FilterLevel
	{
		None,
		Needed,
		Disallowed,
	}
	List<string> GeneTags { get; }
	bool SetGenesAreExcempt { get; }

	Gender Gender { get; }
	FloatRange? BeautyRange { get; }
	bool? Ghoul { get; }

	int CommonalityOffset { get; }
	FilterLevel Filter { get; }

	IEnumerable<GeneDef> AllGenes { get; }
	ListInclusionType GeneInclusion { get; }
	IEnumerable<TraitEntry> AllTraits { get; }
	ListInclusionType TraitInclusion { get; }
	IEnumerable<HediffDef> AllHediffs { get; }
	ListInclusionType HediffInclusion { get; }

	public IEnumerable<string> ConfigErrors();
}

public class PawnFilterEntry : IPawnFilterEntry
{
	public GeneDef? gene;
	public List<GeneDef>? genes;
	public List<string> geneTags = [];
	public bool setGenesAreExcempt = false;

	public TraitDef? trait;
	public List<TraitDef>? traits;

	public IPawnFilterEntry.TraitEntry? traitDegree;
	public List<IPawnFilterEntry.TraitEntry>? traitDegrees;

	public HediffDef? hediff;
	public List<HediffDef>? hediffs;
	public Gender gender = Gender.None;
	public FloatRange? beautyRange;
	public bool? ghoul;

	public int commonalityOffset = default;
	public IPawnFilterEntry.FilterLevel filter = IPawnFilterEntry.FilterLevel.None;

	public Lazy<List<GeneDef>> Genes;
	public ListInclusionType geneInclusion;
	public Lazy<List<IPawnFilterEntry.TraitEntry>> Traits;
	public ListInclusionType traitInclusion;
	public Lazy<List<HediffDef>> Hediffs;
	public ListInclusionType hediffInclusion;
	

	List<string> IPawnFilterEntry.GeneTags => geneTags;
	bool IPawnFilterEntry.SetGenesAreExcempt => setGenesAreExcempt;
	Gender IPawnFilterEntry.Gender => gender;
	FloatRange? IPawnFilterEntry.BeautyRange => beautyRange;
	bool? IPawnFilterEntry.Ghoul => ghoul;
	int IPawnFilterEntry.CommonalityOffset => commonalityOffset;
	IPawnFilterEntry.FilterLevel IPawnFilterEntry.Filter => filter;
	IEnumerable<GeneDef> IPawnFilterEntry.AllGenes => Genes.Value;
	ListInclusionType IPawnFilterEntry.GeneInclusion => geneInclusion;

	IEnumerable<IPawnFilterEntry.TraitEntry> IPawnFilterEntry.AllTraits => Traits.Value;
	ListInclusionType IPawnFilterEntry.TraitInclusion => traitInclusion;

	IEnumerable<HediffDef> IPawnFilterEntry.AllHediffs => Hediffs.Value;
	ListInclusionType IPawnFilterEntry.HediffInclusion => hediffInclusion;

	public PawnFilterEntry()
	{
		static List<T> _get_list<T>(List<T>? potlist, T? single)
		{
			if (potlist is not null)
				return potlist;
			else if (single is not null)
				return [single];
			return [];
		}


		Genes = new(() => _get_list(genes, gene));
		Traits = new(()
			=> [.._get_list(traitDegrees, traitDegree)
				.Concat(_get_list(traits, trait)
					.Select(x => new IPawnFilterEntry.TraitEntry() { def = x })
				)
			]
		);
		Hediffs = new(() => _get_list(hediffs, hediff));
	}

	public IEnumerable<string> ConfigErrors()
	{
		if (filter == IPawnFilterEntry.FilterLevel.Disallowed && commonalityOffset != default)
			yield return $"can't set {nameof(filter)} to '{IPawnFilterEntry.FilterLevel.Disallowed}' and {nameof(commonalityOffset)} on {GetType()}, filter: {ToString()}";

		yield break;
	}

	public override string ToString()
	{
		// Def?[] defs = [
		// 	gene,
		// 	trait,
		// 	hediff
		// ];
		// var defs_str = string.Join(", ", defs.Select(x => x?.label ?? "none"));
		var defs_str = ""; // TODO: fix this

		return $"PartFilter(defs={defs_str}, ghoul={ghoul}, beatuy={beautyRange} filter={filter}, offset={commonalityOffset})";
	}
}

public interface IPawnFilterable
{
	IEnumerable<IPawnFilterEntry> FilterEntries { get; }
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

	public static int CommonalityFor(this IPawnFilterEntry entry, Pawn pawn)
	{
		int res = entry.CommonalityOffset;
		if (!FacesSettings.Instance.StrictGender && entry.Gender != Gender.None)
		{
			res += entry.Gender == pawn.gender
				? COMMONALITY_OFFSET_VAL
				: -COMMONALITY_OFFSET_VAL
			;
		}
		else
		{
			res += COMMONALITY_OFFSET_VAL;
		}

		if (!FacesSettings.Instance.StrictBeauty && entry.BeautyRange.HasValue)
		{
			var dist = entry.BeautyRange.Value.DistanceFrom(pawn.GetStatValue(StatDefOf.PawnBeauty, cacheStaleAfterTicks: 1));
			res += (int)((BEAUTY_BONUS - dist) * COMMONALITY_OFFSET_VAL);
		}

		return res;
	}
	public static float FilterValueFor(this IPawnFilterEntry entry, Pawn pawn, StringBuilder? reason_builder = null)
	{
		reason_builder?.AppendLine($"{entry}:");
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		bool get_expected_val(bool _test_val) => entry.Filter switch
		{
			IPawnFilterEntry.FilterLevel.Needed => true,
			IPawnFilterEntry.FilterLevel.Disallowed => false,
			IPawnFilterEntry.FilterLevel.None or _ => _test_val,
		};
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		float get_accept_val(
			string _test,
			bool _test_val,
			out bool _unacceptable,
			bool _extra_unacceptable_true = false,
			bool _extra_unacceptable_false = false
		)
		{
			var _expected = get_expected_val(_test_val);
			var _is_expected = _test_val == _expected;
			_unacceptable = !_is_expected;
			_unacceptable = _unacceptable || (_test_val ? _extra_unacceptable_true : _extra_unacceptable_false);
			if (_unacceptable)
				return 0;

			var offset = entry.Filter switch
			{
				IPawnFilterEntry.FilterLevel.None => _test_val,
				_ => _is_expected
			}
				? FIT_VAL
				: UNFIT_VAL
			;
			reason_builder?.AppendReason($"{_test}: ({_test_val} expected {_expected}) -> {(_is_expected ? "fit" : "unfit")} => + {offset}");
			return offset;
		}

		bool needed = entry.Filter == IPawnFilterEntry.FilterLevel.Needed;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		void _reason_need(
			string _thing
		)
		{
			reason_builder?.AppendReason($"{_thing} {(needed ? "missing" : "present")}");
		}

		float res = 0;
		bool specific = entry.Filter != IPawnFilterEntry.FilterLevel.None;
		bool unacceptable;
		if (FacesSettings.Instance.StrictBeauty && entry.BeautyRange.HasValue)
		{
			var beauty = pawn.GetStatValue(StatDefOf.PawnBeauty, cacheStaleAfterTicks: 1);
			if (entry.BeautyRange.Value.Includes(beauty))
			{
				reason_builder?.AppendReason($"beatuy in range -> + {FIT_VAL}");
				res += FIT_VAL;
			}
			else
			{
				var malus = entry.BeautyRange.Value.DistanceFrom(beauty);
				reason_builder?.AppendReason($"beatuy not in range range -> - {malus}");
				res -= malus;
			}
		}

		if (specific)
		{
			if (entry.Gender != Gender.None)
			{
				//	undoing unfit value
				//		fitting gender -> bonus
				//		unfitting gender -> no change
				res -= UNFIT_VAL;
				res += get_accept_val(
					"gender",
					pawn.gender == entry.Gender,
					out unacceptable,
					_extra_unacceptable_false: FacesSettings.Instance.StrictGender
				);
				if (unacceptable)
				{
					reason_builder?.AppendReason($"wrong gender, needs {entry.Gender}");
					return res;
				}
			}

			if (ModsConfig.AnomalyActive && entry.Ghoul.HasValue)
			{
				res += get_accept_val(
					"ghould state",
					pawn.IsGhoul == entry.Ghoul,
					out unacceptable
				);
				if (unacceptable)
				{
					reason_builder?.AppendReason($"wrong ghoul state, needs {entry.Ghoul.Value}");
					return res;
				}
			}

			var trait_incl_func = entry.TraitInclusion.GetFuncFor(entry.AllTraits);
			unacceptable = !trait_incl_func(trait =>
			{
				res += get_accept_val(
					"trait",
					trait.ValidForPawn(pawn),
					out unacceptable
				);
				if (unacceptable)
				{
					_reason_need($"trait {trait.def}, degree: {trait.degree}");
					return false;
				}
				return true;
			});

			var hediff_incl_func = entry.HediffInclusion.GetFuncFor(entry.AllHediffs);
			unacceptable = !hediff_incl_func(hediff =>
			{
				res += get_accept_val(
					"hediff",
					pawn.health.hediffSet.HasHediff(hediff),
					out unacceptable
				);
				if (unacceptable)
				{
					_reason_need($"hediff {hediff}");
					return false;
				}
				return true;
			});

			if (ModsConfig.BiotechActive)
			{
				var gene_incl_func = entry.GeneInclusion.GetFuncFor(entry.AllGenes);
				unacceptable = !gene_incl_func(gene =>
				{
					var has_gene = pawn.genes.HasActiveGene(gene);
					if (entry.SetGenesAreExcempt && has_gene)
						return true;
					res += get_accept_val(
						"gene",
						has_gene,
						out unacceptable
					);
					if (unacceptable)
					{
						_reason_need($"gene {gene}");
						return false;
					}
					return true;
				});
			}

			if (unacceptable)
				return res;
		}

		if (res <= 0)
		{
			reason_builder?.AppendLine($"score too low ({res} < 0)");
		}
		return res;
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

	public static T? GetRandomFilterableFor<T>(this IEnumerable<T> defs, Pawn pawn, StringBuilder? reasons = null)
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