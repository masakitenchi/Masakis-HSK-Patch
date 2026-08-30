# SOS2 当前版本适配计划

## 1.6 实施状态（2026-08-29）

- 四种穿梭机成本已迁移到 `Vehicles.VehicleBuildDef`，旧三种 Shuttle race 的护甲已迁移到对应 `Vehicles.VehicleDef`；Superheavy 未继承旧 race 护甲，保持 SOS2 1.6 上游值。
- 没有把旧 `stuffCategories/costStuffCount` 迁入 1.6 VehicleBuildDef。VF 从建造占位物生成 `VehiclePawn` 时不传递 `Stuff`，继续显示材质选择会造成“支付了材质但车辆不保留材质”的假兼容。
- `MakeShuttleFuelPods100` 使用旧 100 个配方的 HSK 成本；`MakeShuttleFuelPods1000` 按 10 倍缩放。`MakeHullFoam10` 同样按单份 HSK 配方的 10 倍缩放。
- 四件 EVA 装备已改为稀疏递归合并，保留 Odyssey `VacuumResistance` 与 SOS2 1.6 `displayPriority`。
- 旧 `CompShuttleStuff` XML/Harmony 逻辑仅保留给旧 Shuttle race 分支；1.6 `SOS2Compat.dll` 已排除该代码并成功构建到 `v1.6/AssembliesCompat`。
- 32 个当前 SOS2 XML 均可解析，1.6 新 XPath 静态唯一命中，程序集构建为 0 警告、0 错误。此前启动日志中的结构错误已经过诊断和修复；最终程序集归属调整后仍需用新的 `Player.log` 做运行验证。

## 比对基线

- 本地补丁：`Core_SK_Patch/LoadOnDemand/Save Our Ship 2` 与 `Source/SOS2Compat`。
- 上游：`D:\SteamLibrary\steamapps\workshop\content\294100\1909914131`，当前包含 SOS2 2.8 的 1.6 内容、Odyssey 扩展补丁和新的 `ShipsHaveInsides.dll`。
- 本地 `LoadFolders.xml` 已在 RimWorld 1.6 时加载 SOS2 XML 补丁；Core_SK_Patch 自身的动态兼容程序集已生成到 `v1.6/AssembliesCompat/SOS2Compat.dll`。
- 对本地 SOS2 XML 的 469 条 XPath 做了上游 1.5/1.6 的静态命中对照：466 条目标数量未变，3 条已因燃料配方改名失效。另有一组更早的穿梭机/转换系统 XPath 在当前 SOS2 中完全没有目标，不能视为仍然有效。

## P0：必须先修复的结构断点

### 1. 穿梭机 Def 已改为 Vehicle Framework 体系

**受影响文件**

- `LoadOnDemand/Save Our Ship 2/Patches/Buildings_Shuttle.xml`
- `LoadOnDemand/Save Our Ship 2/Patches/Creatures_SOS2.xml`
- `LoadOnDemand/Save Our Ship 2/Patches/SOS2MiscPatch.xml`
- `Source/SOS2Compat/Harmony.cs`、`Comp/CompShuttleStuff.cs` 及其旧的 `Replacement/*` 设计

**已失效的旧目标**

- `PersonalShuttle`、`CargoShuttle`、`HeavyShuttle`、`DropshipShuttle`（8 个建造成本/材质 XPath）。
- `BaseShuttle` 与 `ShuttlePersonalRace`、`ShuttleCargoRace`、`ShuttleHeavyRace`、`ShuttleDropshipRace`（`Creatures_SOS2.xml` 的 10 个属性修改，以及 `SOS2MiscPatch.xml` 添加 `CompShuttleStuff` 的操作）。
- `RimWorld.CompBecomePawn` 和 `RimWorld.CompBecomeBuilding`：当前 `ShipsHaveInsides.dll` 中不存在这两个类型，也不存在旧的 `Projectile_ExplosiveShipCombat` 类型。

**当前上游映射**

| 旧目标 | 1.6 车辆实例 Def | 1.6 建造/成本 Def |
| --- | --- |
| PersonalShuttle | `Vehicles.VehicleDef[defName="SoS2_Shuttle_Personal"]` | `Vehicles.VehicleBuildDef[defName="SoS2_Shuttle_Personal_Blueprint"]` |
| CargoShuttle | `Vehicles.VehicleDef[defName="SoS2_Shuttle"]` | `Vehicles.VehicleBuildDef[defName="SoS2_Shuttle_Standard_Blueprint"]` |
| HeavyShuttle | `Vehicles.VehicleDef[defName="SoS2_Shuttle_Heavy"]` | `Vehicles.VehicleBuildDef[defName="SoS2_Shuttle_Heavy_Blueprint"]` |
| DropshipShuttle | `Vehicles.VehicleDef[defName="SoS2_Shuttle_Superheavy"]` | `Vehicles.VehicleBuildDef[defName="SoS2_Shuttle_Superheavy_Blueprint"]` |

**实施要求**

1. 将 `Buildings_Shuttle.xml` 的成本目标改为上表对应的 `Defs/Vehicles.VehicleBuildDef[...]`；车辆运行属性才使用 `Defs/Vehicles.VehicleDef[...]`。当前 SOS2 的成本位于 BuildDef，不能把旧 `ThingDef` 的成本 XPath 直接迁到 VehicleDef；材质字段也须先按 BuildDef 的实际支持情况验证。
2. 删除或改写所有针对 `BaseShuttle`、`Shuttle*Race` 和 `CompProperties_BecomePawn` 的 XML 操作。SOS2 1.6 的穿梭机已是 Vehicle Framework 车辆，不应再向不存在的 Pawn race 添加组件。
3. 移除 `ShuttleStuffPatch` 对 `CompBecomePawn.myPawn` 和 `CompBecomeBuilding.transform` 的 transpiler；如仍需保留材质/外观/升级状态，先以 SOS2 1.6 的 `SaveOurShip2.Vehicles.CompVehicleLoadData`（其存档字段包含 `pattern`、`one`、`two`、`three`、`fuel`、`tiles`、`upgrades`、`displacement`）和 Vehicle Framework 的实例生命周期重新设计钩子。
4. 禁止按旧 IL 偏移直接移植。新机制是否已经保存材质必须用四种穿梭机各完成一次建造、起飞/着陆、存读档的实测后再决定是否需要 Harmony 补丁。

### 2. 燃料舱配方已拆分并改名

**受影响文件**：`LoadOnDemand/Save Our Ship 2/Patches/SOS2RecipeHSK.xml`

`MakeShuttleFuelPods` 在 1.6 不存在，改为：

- `MakeShuttleFuelPods100`：产出 100 个；
- `MakeShuttleFuelPods1000`：产出 1000 个。

现有针对旧 Def 的 `ingredients`、`fixedIngredientFilter`、`recipeUsers` 三个 `PatchOperationReplace` 均为零命中，当前不会生效。

**实施要求**

1. 分别为两个新 RecipeDef 写补丁，并明确 HSK 原料配方按“每个燃料舱”还是“每批”缩放；不要把旧单配方的数量无判断复制两次。
2. 若仍要求只在 Matterfab 制作，分别替换两个 Def 的 `recipeUsers`；同时保留上游的 `researchPrerequisites`、`workAmount`、`targetCountAdjustment` 和产品数量。
3. `MakeHullFoam` 仍有有效命中，但 1.6 新增 `MakeHullFoam10`。确认 HSK 是否也要覆盖批量配方；这是功能覆盖审查，不是自动要求改数值。

### 3. 1.6 动态兼容程序集需要重新建立

**受影响文件**

- `Source/SOS2Compat/SOS2Compat.csproj`
- `Source/SOS2Compat/Harmony.cs`
- `v1.6/AssembliesCompat/SOS2Compat.dll`（已生成）

**问题与实施要求**

1. 工程的 `ShipsHaveInsides` HintPath 仍指向不存在的 `SaveOurShip2Experimental/$(Version)`，应改为可配置的 SOS2 根目录属性，并指向本机 Workshop 的 `$(Version)/Assemblies/ShipsHaveInsides.dll`。不要把机器绝对路径散落在多个项目项中。
2. 将 RimWorld 引用从 `Krafs.Rimworld.Ref 1.5.*` 升到与 1.6 一致的引用版本，再清理上述已删除类型的编译依赖。
3. 重新 Publicize 当前 `ShipsHaveInsides.dll` 后编译至 `v1.6/AssembliesCompat/SOS2Compat.dll`；确认主程序集的动态加载器能发现该文件。
4. `CrossCompat_Minerals_SOS2` 的目标 `SaveOurShip2.ShipInteriorMod2.PostGenerateShipDef` 在 1.6 仍存在且仍为 4 参数，可先保留；编译后再验证其 Harmony Prefix 参数绑定和 Minerals 已启用时的实际生成流程。
5. `Projectile/Projectile_ExplosiveShipCombatKinetic.cs` 目前已从工程排除，且其基类在 1.6 已不存在。保持排除或归档，不能在重建时意外重新纳入。

## P1：必须保留 SOS2 1.6 新行为的 Def 合并

### 4. EVA 装备会被整段替换，导致 Odyssey 真空抗性丢失

**受影响文件**：`LoadOnDemand/Save Our Ship 2/Patches/Apparel_Space.xml`

该文件整段替换四件 EVA 装备的 `equippedStatOffsets` 与 `recipeMaker`。SOS2 1.6 为四件装备新增了以下内容：

- 连体服与重型连体服：`VacuumResistance` = 0.3（`MayRequire="Ludeon.RimWorld.Odyssey"`）；
- 头盔与重型头盔：`VacuumResistance` = 0.7（同一条件）；
- 四个 `recipeMaker` 新增 `displayPriority`（420、440、460、470）。

现有全段 `PatchOperationReplace` 会覆盖这些上游字段。

**实施要求**：改为只替换 HSK 自有的数值叶节点，或在完整替换值中显式保留上述 `MayRequire` 节点和 `displayPriority`。完成后以 Odyssey 启用与未启用两套 Def 输出验证，避免把 DLC 专用 StatDef 写进无 Odyssey 的加载结果。

### 5. 飞船生命维持与 Odyssey 氧气系统不得被覆盖

**重点目标**：`Ship_LifeSupport`、`Ship_LifeSupport_Small`，以及 SOS2 1.6 新增/调整的气密、空气与 Vehicle Framework 元数据。

上游已在生命维持设备的 `comps` 中加入 `CompProperties_OxygenPusher`（分别为 2 和 1 air/百格/秒，且仅 Odyssey 启用时存在），并为多种建筑增加 `Vehicles.CustomCostDefModExtension`。当前 `Buildings_Ship.xml` 主要替换 `costList`、追加材质字段并删除设计器分组，尚未直接替换这些 `comps`；后续迁移不得把它们扩大为整 Def/整 `comps` 覆盖。

**实施要求**：逐项复核所有会改动 1.6 已变化 Def 的 `PatchOperationReplace` / `Remove`。尤其是 `Buildings_Ship.xml`、`Buildings_Archotech.xml`、`Buildings_Mech.xml`、`Buildings_ShipCombat.xml` 与 `Buildings_ShipCombatCannons.xml`；只改 HSK 所有的成本、材质和分类叶节点，保留上游的 `comps`、`modExtensions`、气密属性和 DLC 条件节点。

### 6. 上游大规模数值与功能更新后的覆盖审查

1.5 → 1.6 中，44 个同路径文件发生变化、16 个文件新增、1 个文件移除。当前本地补丁命中且上游内容已变化的选择器主要集中于：

- 建筑与战斗：`Buildings_Ship.xml`（76 处）、`Buildings_Archotech.xml`（32 处）、`Buildings_Mech.xml`（18 处）、`Buildings_ShipCombat.xml`（16 处）、`Buildings_ShipCombatCannons.xml`（7 处）、`Buildings_Joy.xml`（6 处）；
- EVA：`Apparel_Space.xml`（16 处）；
- 其他：`Creatures_SOS2.xml`、`Items_ShipCombat.xml`、`Patch_SOS2.xml`、`Scenarios_Patch.xml`、`ShipHologram.xml`、`SOS2Archostrich.xml`、`SOS2MiscPatch.xml` 与种子兼容补丁。

这不是要求回滚 HSK 配方/成本，而是逐项决定覆盖优先级。SOS2 2.8 引入飞船命中/闪避、穿梭机自动火力、Vehicle Framework 行为及 Odyssey 兼容；任何旧补丁若整段替换 `projectile`、`verbs`、`comps`、`recipeMaker`、`equippedStatOffsets` 或场景根节点，都必须以 1.6 定义为底稿重做成稀疏补丁。

## P2：清理陈旧或静默无效的操作

以下项目不应阻塞 P0，但应在本轮清理，以免日志持续产生 XPath 失败并误导后续维护：

- `Buildings_Ship.xml` 中旧的 `ShipPilotSeat` ThingDef 目标：当前可建造 Def 是 `ShipPilotSeatMini`，`ShipPilotSeat` 仅作为研究 Def 名存在；旧的三条 ThingDef 操作应删除或迁移到正确 Def。
- `Creatures_SOS2.xml` 的 `BaseShuttle` 和 `Shuttle*Race` 旧生物/车辆平衡操作：随 Vehicle Framework 迁移而删除或按新 VehicleDef 属性重新定义，不可继续静默零命中。
- `SOS2MiscPatch.xml` 对 `Shuttle*Race/comps` 添加 `CompShuttleStuff` 的操作：与已删除的 C# 转换补丁一并移除。
- `Buildings_Ship.xml` 中先 Remove 不存在的 `stuffCategories` / `costStuffCount` 再 Add 的旧序列：收敛为单个、幂等的添加/替换操作；保留现有上游字段，不用失败 Remove 作为流程前提。
- `ShipInside_PassiveCooler*`、`ArmorSpacerBase`、`ApparelArmorHelmetMechanitorBase`、`HiTechResearchBench` 等不是 SOS2 自有 Def 的跨模组目标：分别在完整 HSK Def 集中验证。若目标属于 Core_SK 或 DLC，应加正确的 `IfModActive` / 条件操作；若已移除则删除陈旧规则。
- `Scenarios_Patch.xml`：上游场景现在显式继承 `ScenarioBase` 并移除了内嵌 `playerFaction`。重建本地两个场景时以 1.6 场景结构为基底，保留 `ParentName="ScenarioBase"`、新 `surfaceLayer` 等上游结构，仅替换 HSK 起始物资。

## 验证顺序与完成标准

1. **静态 XML**：检查所有 SOS2 补丁 XML 可解析；对 1.6 上游 Def 聚合后，P0/P1 XPath 均有预期命中，且没有旧穿梭机、`CompBecome*` 或旧燃料配方的零命中。
2. **构建**：以 1.6 的 `ShipsHaveInsides.dll` 和 RimWorld 1.6 引用编译 `SOS2Compat`；确认只部署到 `v1.6/AssembliesCompat/SOS2Compat.dll`，并检查 DLL 引用中没有 `RimWorld.CompBecomePawn` / `RimWorld.CompBecomeBuilding`。
3. **最终 Def**：启动前导出/检查合并后的 Def；确认四种 `SoS2_Shuttle_*` VehicleDef、两个燃料配方、四件 EVA 装备、生命维持机和关键战斗 Def 同时保有 HSK 改动与 SOS2 1.6/Odyssey 字段。
4. **运行时**：使用启用 SOS2、Vehicle Framework、Core_SK、Core_SK_Patch 的新启动日志；另做 Odyssey 启用的启动。确认无 XPath、Def 继承、缺失类型、Harmony patch 或程序集加载错误。
5. **功能回归**：依次测试四种穿梭机建造/材质/起飞着陆/存读档，Matterfab 的两档燃料配方，EVA 真空防护，生命维持氧气行为，以及一场飞船战斗。

在上述结构和运行时验证完成前，不将 SOS2 2.8 的上游成本改动混入 HSK 数值再平衡；数值取舍应作为后续独立步骤处理。
