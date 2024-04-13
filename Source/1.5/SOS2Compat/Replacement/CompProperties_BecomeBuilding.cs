using System;
using Verse;

namespace Core_SK_Patch
{
	public class CompProperties_BecomeBuilding : CompProperties
	{
		public ThingDef buildingDef;
		public float fuelPerTile;
        public ThingDef skyfaller;

		public CompProperties_BecomeBuilding ()
		{
			this.compClass = typeof(Core_SK_Patch.CompBecomeBuilding);
		}
	}
}

