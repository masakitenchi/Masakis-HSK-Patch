using System;
using Verse;

namespace Core_SK_Patch
{
	public class CompProperties_BecomePawn : CompProperties
	{
		public PawnKindDef pawnDef;

		public CompProperties_BecomePawn ()
		{
			this.compClass = typeof(Core_SK_Patch.CompBecomePawn);
		}
	}
}

