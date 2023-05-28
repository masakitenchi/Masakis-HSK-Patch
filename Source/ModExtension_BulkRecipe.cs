using RimWorld;
using Verse;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Reflection.Emit;

namespace Core_SK_Patch;

public class ModExtension_BulkRecipe : DefModExtension
{
    public float MaterialMultiplier = 1.0f;
    public float ProductMultiplier = 1.0f;
    public float WorkAmountMultiplier = 1.0f;
}
