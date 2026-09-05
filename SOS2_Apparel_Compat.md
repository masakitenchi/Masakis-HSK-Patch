# HSK 装备的 SOS2 真空防护

适用于 RimWorld 1.6，由 `LoadFolders.xml` 在 SOS2 启用时加载
`LoadOnDemand/Save Our Ship 2/Patches/HSKApparelforSOS2.xml`。

## 防护规则

| 装备 | 缺氧抗性 | 减压抗性 | 太空移动属性偏移 |
| --- | ---: | ---: | ---: |
| 名单内身体装甲（默认档） | 0 | 0.75 | +2 |
| 名单内密封头盔 | 1 | 0.25 | 0 |
| 轻、中型动力甲、相位、突击队、星际装甲 | 同身体装甲 | 同身体装甲 | +3 |
| 蝗虫装甲 | 同身体装甲 | 同身体装甲 | +4 |
| 合金背心（仅移动适配） | 不增加 | 不增加 | +1 |

SOS2 使用人物最终的 `HypoxiaResistance` 和 `DecompressionResistance` 判定真空生存，两项都必须达到 1。名单内任意身体装甲与头盔搭配即可达到该阈值。单件身体装甲不能供氧，单件头盔不能提供全身减压防护。

`VacuumSpeedMultiplier` 默认值为 1；因此默认身体装甲为 3 倍，机动档为 4 倍，蝗虫装甲为 5 倍，合金背心为 2 倍（未计其他来源）。这是为改善 HSK 护甲舱外作业速度而设置的游戏性分档，不表示每件护甲的原始说明都包含推进器。头盔保持 +0，避免套装重复加成；跳跃背包等其他装备仍可能叠加。

SOS2 的移动补丁只在进入 `EmptySpace` 地形时降低移动耗时，不加速普通船板上的移动，也不修改原有 `MoveSpeed`。倍率相对于同样穿戴、无该加成时的太空移动，不代表实际每秒移动格数等于地面速度的该倍数。

`CompEVA` 只负责 SOS2 的装备说明，不代替上述属性。Odyssey 的 `VacuumResistance`、温度防护、CE 护甲覆盖及其他原有属性保持不变；真空防护齐全不等于具备足够的耐寒、耐热能力。

## 身体装甲（20 件）

- `Apparel_PowerArmor`
- `Apparel_ArmorRecon`
- `Apparel_ArmorReconPrestige`
- `Apparel_ArmorMarinePrestige`
- `Apparel_ArmorCataphract`
- `Apparel_ArmorCataphractPrestige`
- `Apparel_ArmorLocust`
- `Apparel_ArmorMarineGrenadier`
- `Apparel_ArmorCataphractPhoenix`
- `Apparel_MechlordSuit`
- `Armor_PowerArmorLight`
- `Armor_PowerArmorMedium`
- `Armor_PhaseArmor`
- `Armor_Hive`
- `Armor_Steamhull`
- `Armor_HellPowerArmor`
- `Armor_Siege`
- `Armor_CQC`
- `Armor_Commando`
- `Armor_Interstellar`

## 密封头盔（21 件）

- `Apparel_PowerArmorHelmet`
- `Apparel_ArmorHelmetRecon`
- `Apparel_ArmorHelmetReconPrestige`
- `Apparel_ArmorMarineHelmetPrestige`
- `Apparel_ArmorHelmetCataphract`
- `Apparel_ArmorHelmetCataphractPrestige`
- `Apparel_ArmorHelmetMechCommander`
- `Apparel_ArmorHelmetMechlordHelmet`
- `Helmet_Phase`
- `Helmet_Siege`
- `Helmet_CQC`
- `Helmet_PsyPrototype`
- `Helmet_PsyPrototypeTwo`
- `Helmet_PsyPrototypeThree`
- `Helmet_Enviro`
- `Helmet_EnviroLight`
- `Helmet_Hive`
- `Helmet_Steamhull`
- `Helmet_PowerArmorLight`
- `Helmet_Doom`
- `Helmet_HellPowerArmor`

相位头盔虽然在 HSK 中使用 `UpperHead` 护甲覆盖，已有 Odyssey 真空防护仍将其作为密封装备。本补丁沿用这一环境防护定位，不改变其 CE 护甲覆盖。轻型环境头盔也沿用 HSK 已有的真空防护定位。

## 排除范围

合金背心、披风、大衣、兜帽、工作服、连体服、冷冻服和开放式穿梭机驾驶头盔不由本补丁授予 SOS2 防护。合金背心仅获得明确的 +1 移动偏移，不增加抗性或 EVA 组件。其他模组独立提供的适配不在这里清除。

本补丁使用具体 defName 和稀疏属性合并，不对整个科技等级或基类赋值。新增装备需明确加入名单。SOS2 自带 EVA 套装仍由其原有 Def 和补丁负责。

## 验证

移动分档更新：使用本地 HSK `PatchOperationReplaceExtended` 源码（仅将 Verse 日志与补丁基类替换为测试桩），对当前 Unified 中全部服装节点离线应用补丁。验证目标均存在、只有 `VacuumSpeedMultiplier` 改变、其他字段不变、重复应用结果一致。共改变 20 个不同 Def；蝗虫装甲仍为 +4，无需改值。缓存中重复 Def 按原节点逐一比较，未将不同来源节点混为同一条。

已使用 2026-09-05 的继承前 Def 缓存移除旧补丁的基类注入，再调用 HSK 的 `PatchOperationReplaceExtended` 实现离线应用新补丁：确认全部目标存在、数值正确、无重复属性和组件、其他字段保持不变、重复应用结果一致，以及缺少可选 DLC 装备时仍可执行。

这属于离线 XML 验证。重启游戏后，需要用新生成的 Unified 和实际人物属性确认加载结果；身体装甲与头盔搭配应显示缺氧、减压抗性均为 100%。
