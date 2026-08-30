# HSK × Vehicle Framework 适配计划

## 结论与比对边界

Vehicle Framework 当前 1.6 版本为 **1.6.2144**。其车辆不是普通 `ThingDef`：

- 运行中的车辆为 `Vehicles.VehicleDef`，实例类型为 `Vehicles.VehiclePawn`；
- 可建造的蓝图/成本为独立的 `Vehicles.VehicleBuildDef`；
- 武器为 `Vehicles.VehicleTurretDef`，世界旅行使用 Vehicle Caravan。

最新运行基线已启用 `smashphil.vehicleframework`、Core_SK、Core_SK_Patch 与 Combat Extended。`Unified.xml`（`2026-08-29 23:22:46`）包含 `3014915404/1.6` 的框架源路径；`Player.log`（`2026-08-29 23:25:24`）记录 VF 对 CE/Odyssey 等兼容层生效、CE 执行 `Installing Vehicle Framework`，且没有 Vehicle Framework、CE VehicleCompat、Def 加载或 PatchOperation 相关错误。

本次配置只启用了框架，没有启用实际车辆内容包或 SOS2/Engine Industries，因此最终合并结果中没有具体的 `Vehicles.VehicleDef`、`Vehicles.VehicleBuildDef`、`Vehicles.VehicleTurretDef`。这属于“框架加载成功但没有车辆 Def 可审计”，不能据此判定下游车辆内容已经适配。

## P0（框架级已完成）：VF 合并基线

1. **已通过**：Vehicle Framework、Core_SK、Core_SK_Patch、Combat Extended 的框架级启动；VF 与 CE 的互相检测和安装完成，没有 VF 专属错误。
2. **已通过**：刷新后的 `Unified.xml` 包含 `3014915404/1.6` 源路径；这证明 VF Def 文件实际进入本次合并，而非只在 Mod 列表中启用。
3. **符合预期**：具体 `VehicleDef`、`VehicleBuildDef`、`VehicleTurretDef` 均为 0，因为当前没有车辆内容包。后续启用实际车辆 Mod 后必须重新统计这些类型。
4. **仍待验证**：HSK+VF+SOS2 与 HSK+VF+Engine Industries。两者需要分别启用后刷新 `Unified.xml` 和 `Player.log`，不能沿用本次框架级结论。

本次结果确认“不需要为了让 VF 本体启动而新增 HSK 基础补丁”。后续工作应集中在实际车辆内容、SOS2 迁移和 Engine Industries 的显式不兼容处；任何具体车辆数值仍只能依据对应内容包启用后的最终 Def 制定。

## P0：Engine Industries 的显式不兼容处理

**受影响文件**：`LoadOnDemand/Engine Industries/Patches/Patch_ThingDefs.xml`

该补丁在 Engine Industries 启用时直接删除：

- `ComponentWheelTire`
- `ComponentWheelRoad`
- `ComponentWheelWooden`

文件本身标注“Not ready for Vehicle Framework atm”。这是当前 HSK 中唯一明确声明的 VF 未适配点；删除轮组会掩盖冲突，但也使 Engine Industries 的车辆材料/配方链无法参与 VF。

**实施要求**

1. 取得与测试配置相同版本的 Engine Industries，逐一追踪三种轮组的 recipe、costList、thingCategory、贸易和被引用 Def；当前工作区没有该 Mod 本体，不能直接假设它们的用途。
2. 以“保留轮组”为默认方向，用稀疏补丁把它们接入 VF 所需的建造、维修或升级链；只有确认为重复且没有引用后才删除。
3. 检查 `Patch_WorkTables.xml` 的 `CompProperties_Engine` 与 `StatPart_Engine`。它们目前只面向普通 `WorkTable`，不得泛化添加到 `Vehicles.VehicleDef` 或 `Vehicles.VehicleBuildDef`；车辆的驱动、燃料、维修和移动应继续由 VF 组件管理。
4. 将现有无条件删除改为带 `smashphil.vehicleframework` 条件的兼容分支：VF 未启用时维持现有 HSK 行为，VF 启用时加载新的轮组/VF 适配补丁。这样不会把“尚未完成”伪装成已兼容。

## P0：SOS2 车辆迁移必须按 VF Def 层级处理

**受影响文件**：`LoadOnDemand/Save Our Ship 2/Patches/Buildings_Shuttle.xml`、`Creatures_SOS2.xml`、`SOS2MiscPatch.xml`、`Source/SOS2Compat/*`。

SOS2 1.6 的四种穿梭机已经是 VF 车辆。旧的 `PersonalShuttle`/`CargoShuttle`/`HeavyShuttle`/`DropshipShuttle` 与 `BaseShuttle`/`Shuttle*Race` 不存在；应使用下列对应关系：

| 原 HSK 目标 | 车辆实例 | 建造成本 |
| --- | --- | --- |
| PersonalShuttle | `SoS2_Shuttle_Personal` | `SoS2_Shuttle_Personal_Blueprint` |
| CargoShuttle | `SoS2_Shuttle` | `SoS2_Shuttle_Standard_Blueprint` |
| HeavyShuttle | `SoS2_Shuttle_Heavy` | `SoS2_Shuttle_Heavy_Blueprint` |
| DropshipShuttle | `SoS2_Shuttle_Superheavy` | `SoS2_Shuttle_Superheavy_Blueprint` |

**实施要求**

1. 将成本修改放在 `Defs/Vehicles.VehicleBuildDef[...]`，不要错误地补到 `VehicleDef`；实例属性、组件和飞行/战斗行为才定位到 `Defs/Vehicles.VehicleDef[...]`。
2. 清除旧 `CompBecomePawn`、`CompBecomeBuilding`、`CompShuttleStuff` 与旧 Shuttle race 的 XML/Harmony 逻辑。当前 VF 生命周期并不经过旧的建筑↔Pawn 转换。
3. 由 VF 已有的 `CompVehicleLoadData` 和 Vehicle Framework 保存流程决定是否还需要保存 HSK 材质/外观/升级信息；先做建造、起飞/降落、存读档实测，禁止按旧 IL 偏移迁移。
4. 具体的 SOS2 Def、燃料配方和程序集任务见 `Patch_Plan.md`；本文件只定义其 VF 层级和验证约束，避免两份计划对同一数值作出冲突决定。

## P1：维护 VF 自动兼容补丁与加载顺序

### 世界对象扩展

VF 1.6 在 SOS2 激活时会为 `ShipOrbiting`、`ShipEnemy`、`SiteSpace`、`MoonPillarSite` 添加 `Vehicles.SpaceObjectDefModExtension`。当前 SOS2 1.6 自身也已在这些 WorldObjectDef 中提供该扩展，因此最终合并 Def 必须验证“恰好一个”实例；不能由 HSK 再盲目追加，也不能用整段 `modExtensions` 替换把它删除。

本次无 SOS2 的运行基线中，Odyssey 的 `OrbitalItemStash` 与 `AsteroidBasic` 各出现了两个同类扩展，但未产生 VF 红字。这来自 VF/Odyssey 的抽象 WorldObject 继承输出，不是 HSK 补丁或 SOS2 组合造成的证据。HSK 不应写全局清理补丁；只需对自己修改的目标和上述四个 SOS2 WorldObjectDef 做定点去重验证。

### HSK 侧的显式门控

1. 在 `Core_SK_Patch/LoadFolders.xml` 添加只在 `smashphil.vehicleframework` 激活时加载的 `LoadOnDemand/Vehicle Framework` 目录；该目录承载未来所有 VF XML 兼容补丁。
2. 在 `Core_SK_Patch/About/About.xml` 的 `loadAfter` 中加入 `smashphil.vehicleframework`，使 HSK 的 VF 专用 Def 修改在框架与相关子模组之后执行。
3. 保持 `Core_SK` 的基础依赖不强制要求 VF。VF 是可选框架，不能令未安装 VF 的 HSK 配置加载含 `Vehicles.*` 类或 Def 类型的 XML。
4. HSK 当前已部署的 `Core_SK_Patch` 1.6 DLL 没有对 `Vehicles.dll` 的直接程序集引用。若将来确实需要 C# 兼容层，使用独立、按 VF 激活后动态加载的程序集，并引用当前 `Vehicles.dll`；不得把强引用放入基础 DLL。

## P1：Combat Extended 的车辆武器边界

HSK 必带 Combat Extended，而 CE 已有独立的 VehicleCompat：它接管 `VehicleTurret` 发射、弹药查询、抛体计算和车辆碰撞体，并读取 `Vehicles.CETurretDataDefModExtension`。因此：

1. 不重写 CE 的通用 VehicleCompat，也不向普通 `ThingDef/Verb` 复制一套车辆发射逻辑。
2. 当 HSK 需要平衡某个 VF 车辆武器时，目标应为 `Defs/Vehicles.VehicleTurretDef[...]`；弹药/弹道数据以 CE 的 `Vehicles.CETurretDataDefModExtension` 和 AmmoSet 为入口。
3. 车辆本体的护甲、组件血量、CargoCapacity、质量和移动属性属于 `Vehicles.VehicleDef` 的各自节点；不要把 CE 普通炮塔的 `comps`、`verbs` 或整段 `statBases` 覆盖到车辆上。
4. 在 VF+CE 测试中至少覆盖：装填、射击、弹药消耗、载具被命中、修理、拆装武器和存读档。HSK 的弹药/武器分类补丁只有在这些路径出现遗漏时才增加 VF 专用规则。

## P2：在已合并 Def 中逐项审查的 HSK 系统

这些项目目前没有来自 VF 的最终 Def 可供判定，属于启用 VF 后必须检查的兼容面，而不是预设要修改的内容：

- **资源与制造**：所有 HSK 对 `ThingDef` 的通用成本、stuff、tradeability、recipeUsers 或物品筛选补丁，确认不会误把 `VehicleBuildDef` 当作普通 ThingDef，且不会移除轮组、维修件或车辆建造资源。
- **研究与设计器**：检查车辆蓝图的 `designationCategory`、researchPrerequisites 与 HSK 的研究解锁/分类迁移，保持 VF 的 `VF_Vehicles` 入口和 VehicleBuildDef 的建造流程。
- **运输、搬运与库存**：VF 车辆 cargo、乘员位和 Vehicle Caravan 使用独立的 Job/Caravan 流程；审查 HSK 的搬运、堆叠、携带容量、自动补给与 caravan 改动是否假设所有容器都是普通 `ThingWithComps`。
- **交易与事件**：VF 基类允许车辆贸易（受 FeatureFlag 控制），并有 Vehicle Caravan/raid/arrival Def；审查 HSK 的贸易、商队、袭击与 world object 过滤，避免漏掉车辆或把 VehiclePawn 当作普通殖民者/动物。
- **跨模组组合**：至少验证 HSK+VF、HSK+VF+CE、HSK+VF+SOS2、HSK+VF+Engine Industries 四组；只有发生 Def 覆盖或运行错误时再建立更细的子模组补丁。

## 验证完成标准

1. VF 启用后的 `Unified.xml` 含框架源路径；启用具体车辆内容包后含预期的车辆 Def，且所有新增 HSK XPath 均有唯一、预期目标。
2. HSK 修改的目标及四个 SOS2 WorldObjectDef 没有重复的 `Vehicles.SpaceObjectDefModExtension`，也没有旧 SOS2 `CompBecome*` / `Shuttle*Race` / `ThingDef` 穿梭机 XPath。Odyssey 继承产生的既存重复不作为 HSK 全局清理目标。
3. Engine Industries 轮组在 VF 分支中不再被盲删，并能完成设计指定的制造/使用流程。
4. 新鲜 `Player.log` 无 Vehicle Framework、CE VehicleCompat、Def 加载或 PatchOperation 错误；框架级基线已于 `2026-08-29 23:25:24` 通过，添加具体车辆内容后需重新验证。
5. 完成至少一辆普通 VF 车辆和四种 SOS2 穿梭机的建造、武器/货物、移动或起飞、存读档回归；数值平衡另行评审，不与结构适配混合提交。
