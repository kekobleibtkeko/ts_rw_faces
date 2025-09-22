using System.Collections.Generic;
using TS_Lib.Util;
using Verse;

namespace TS_Faces.Data;

public class PawnFilterDef : Def, IPawnFilterEntry
{
	public PawnFilterEntry values = default!;

	public List<string> GeneTags => ((IPawnFilterEntry)values).GeneTags;
	public bool SetGenesAreExcempt => ((IPawnFilterEntry)values).SetGenesAreExcempt;
	public Gender Gender => ((IPawnFilterEntry)values).Gender;
	public FloatRange? BeautyRange => ((IPawnFilterEntry)values).BeautyRange;
	public bool? Ghoul => ((IPawnFilterEntry)values).Ghoul;
	public int CommonalityOffset => ((IPawnFilterEntry)values).CommonalityOffset;
	public IPawnFilterEntry.FilterLevel Filter => ((IPawnFilterEntry)values).Filter;
	public IEnumerable<GeneDef> AllGenes => ((IPawnFilterEntry)values).AllGenes;
	public TSUtil.ListInclusionType GeneInclusion => ((IPawnFilterEntry)values).GeneInclusion;
	public IEnumerable<IPawnFilterEntry.TraitEntry> AllTraits => ((IPawnFilterEntry)values).AllTraits;
	public TSUtil.ListInclusionType TraitInclusion => ((IPawnFilterEntry)values).TraitInclusion;
	public IEnumerable<HediffDef> AllHediffs => ((IPawnFilterEntry)values).AllHediffs;
	public TSUtil.ListInclusionType HediffInclusion => ((IPawnFilterEntry)values).HediffInclusion;

	public override IEnumerable<string> ConfigErrors()
	{
		return base.ConfigErrors();
	}
}