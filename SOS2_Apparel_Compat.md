# HSK 装备的 SOS2 真空防护

适用于 RimWorld 1.6，由 `LoadFolders.xml` 在 SOS2 启用时加载
`LoadOnDemand/Save Our Ship 2/Patches/HSKApparelforSOS2.xml`。

## 防护规则

| 装备 | 缺氧抗性 | 减压抗性 | 太空移动属性偏移 |
| --- | ---: | ---: | ---: |
| 名单内身体装甲 | 0 | 0.75 | 0 |
| 名单内密封头盔 | 1 | 0.25 | 0 |
| 轻、中型动力甲 | 同身体装甲 | 同身体装甲 | +1 |
| 蝗虫装甲 | 同身体装甲 | 同身体装甲 | +4 |

SOS2 使用人物最终的 `HypoxiaResistance` 和 `DecompressionResistance` 判定真空生存，两项都必须达到 1。名单内任意身体装甲与头盔搭配即可达到该阈值。单件身体装甲不能供氧，单件头盔不能提供全身减压防护。

`VacuumSpeedMultiplier` 默认值为 1；因此轻、中型动力甲的最终倍率为 2，蝗虫装甲为 5（未计其他来源）。轻、中型动力甲说明中明确含有推进器；蝗虫装甲保留 SOS2 原有推进加成。普通动力装甲的伺服助力不视为太空推进器。

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

合金背心、披风、大衣、兜帽、工作服、连体服、冷冻服和开放式穿梭机驾驶头盔不由本补丁授予 SOS2 防护。移除了旧补丁对 `ArmorSpacerBase` 的批量赋值，因此合金背心不再误继承防护、移动加成或 EVA 说明。其他模组独立提供的适配不在这里清除。

本补丁使用具体 defName 和稀疏属性合并，不对整个科技等级或基类赋值。新增装备需明确加入名单。SOS2 自带 EVA 套装仍由其原有 Def 和补丁负责。

## 验证

已使用 2026-09-05 的继承前 Def 缓存移除旧补丁的基类注入，再调用 HSK 的 `PatchOperationReplaceExtended` 实现离线应用新补丁：确认全部目标存在、数值正确、无重复属性和组件、其他字段保持不变、重复应用结果一致，以及缺少可选 DLC 装备时仍可执行。

这属于离线 XML 验证。重启游戏后，需要用新生成的 Unified 和实际人物属性确认加载结果；身体装甲与头盔搭配应显示缺氧、减压抗性均为 100%。
