using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace TS_Faces.Data;

public class PartFilterEntry
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

    public float commonalityOffset = default;
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

        return $"PartFilter(defs={defs_str}, filter={filter}, offset={commonalityOffset})";
    }
}

public interface IPartFiltered
{
    IEnumerable<PartFilterEntry> FilterEntries { get; }
    float Commonality { get; }
}

public static class PartFilterExtensions
{
	public static T GetActiveFromFilters<T>(this IEnumerable<T> parts, Pawn pawn)
		where
			T: IPartFiltered
	{
		throw new NotImplementedException();
	}
}