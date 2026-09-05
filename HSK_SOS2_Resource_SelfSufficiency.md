# HSK 基础原材料与 SOS2 太空自给审计

审计日期：2026-09-05。有效 Def 快照：`Unified.xml`，最后生成于 2026-09-05 16:48:42。

## 结论与范围

当前安装不能简单概括为“上太空后所有矿产和石化原料都断供”。植物原料可以转成 Matter；Matter 可以合成多种矿石；生物化合燃料可以回灌 Rimefeller 管网，供应其精炼设备。

最明确的常规生产缺口是：**盐及氯/烧碱链、黏土、部分天然石种和宝石/化石**。原油、煤、泥炭、石英也缺少普通船内再生路线，但它们的很多用途有替代生产链。砂子、碎石等另有当前配方形成的净增产循环，不能在描述当前游戏行为时忽略。

这里区分两种自给：
- **船内自给**：常规 SOS2 船体，不依赖贸易、敌舰拆解、地表补给、定期新开矿点；允许带启动种苗、动物和机器，但库存用完后仍须能生产。
- **太空活动范围内补给**：允许访问小行星/太空站。这可以补充不少矿物，但属于外部采集，不等于船内闭环。

清单按当前 HSK 安装审计，包含 Core_SK、Rimefeller_SK、Materials Science、Metallurgy、Minerals、DBH 及 SOS2 已加载内容。附表完整列出游戏 `ResourcesRaw` 分类及其子分类中的 **134 个有效物品**；该游戏分类混入了一些加工品，正文已区分。另补充不在此分类下的燃料、冶炼材料、动植物产物。未逐件重复展开所有受同一缺料影响的装备、弹药和成品。

这是 Def、配方关联与部分实际 DLL/本地源码审计。未启动游戏验证船上每种建筑；特殊搬运、地形改造和资源循环均单列，不冒充已完成的运行测试。

## 1. 最基础的工业原料

| 原料（当前显示含义） | defName | 普通船内来源 | 判定 |
| --- | --- | --- | --- |
| 磁铁矿 | Iron | Matter → 矿石 | 后期可再生 |
| 镍黄铁矿 | Nickel | Matter → 矿石 | 后期可再生 |
| 黄铜矿 | Copper | Matter → 矿石 | 后期可再生 |
| 锡石 | Tin | Matter → 矿石 | 后期可再生 |
| 方铅矿 | Anglesite | Matter → 矿石 | 后期可再生 |
| 铝土矿 | Aluminium | Matter → 矿石 | 矿石可再生；实际冶炼另需盐/烧碱 |
| 金红石 | Ilmenite | Matter → 矿石 | 后期可再生 |
| 白钨矿（旧 ID 为 Wolframite） | Wolframite | Matter → 矿石 | 后期可再生 |
| 钛磁铁矿 | Titanomagnetite | Matter → 矿石 | 后期可再生 |
| 闪锌矿 | Sphalerite | Matter → 矿石 | 后期可再生 |
| 沼铁矿 | BogIron | 泥炭加工 | 无普通船内泥炭源；铁、锰可由其他矿石替代 |
| 钒钾铀矿 | Uranium | Matter → 矿石 | 后期可再生 |
| 自然金 | Gold | Matter；黄铜矿副产物 | 后期可再生 |
| 银币、纯银 | Silver / PureSilver | Matter 产的是 Silver；方铅矿冶炼产 PureSilver | 两者不可混同；纯银可经再生方铅矿获得 |
| 煤 | Coal | 天然煤矿 | 煤本身缺口；木炭/焦炭可替代部分用途 |
| 泥炭 | Peat | 天然地形采集 | 缺口；并非不可替代的工业根材料 |
| 硝石 | Nitre | 腐烂植物/尸体/粪污 + 灰烬 | 可再生，不能仅按硝石矿判断 |
| 硫 | Sulfur | 含硫矿石处理 | 后期随矿石链可再生；不必依赖地热口 |
| 盐 | Salt | 盐矿、泥岩/石灰岩提取 | 常规船内缺口，特殊盐矿搬运见后文 |
| 石英 | Quartz | 天然矿物 | 原石缺口；玻璃/硅原料可由沙子替代 |
| 岩块、各类天然石种 | Chunk* / ZF_Chunk* | 天然岩石 | 常规船内缺口；小行星可补给 |
| 碎石、砂子 | CrushedStone / SandResource | 岩块/石砖粉碎 | 常规依赖石料；存在净增产循环 |
| 黏土 | SoftClay | 黏土岩块提取、天然地形 | 常规船内缺口 |
| 土/淤泥 | Dirt | 砂子 + 肥料 + 碎石 + 堆肥 | 条件再生；受矿物骨料来源限制 |
| 灰烬、堆肥、肥料 | Ash / Compost / Fertilizer | 木料/有机废料加工 | 可再生 |
| 玉、黑曜石 | Jade / Obsidian | 天然矿物 | 缺口；可选择其他建筑材料 |
| 原宝石、超硬原宝石、化石 | RoughGem / RoughUltrahardGem / SmallFossil | 天然矿物/化石 | 缺口；不能把回收现有珠宝算作无限生产 |
| 辉光石、寒冷石 | Glowstone / Coldstone | 天然矿体/动态矿晶 | 特殊环境下可繁殖；普通船体无直接合成 |
| 原油 | OilBarrel / 管网原油 | 地下油田 | 原油本身缺口；精炼产品可绕开原油 |

铁、铜、镍、钴、铬、钨、钛、钼、钒、锌、锰、铅、锡、纯银等金属是冶炼产物，而不是每一种都需要独立矿脉。例如钛磁铁矿可产钒，金红石/白钨矿可产钼，锡石/闪锌矿可产锰。应按实际副产物配方判断，不能按元素名逐个增加“太空矿石”。

### Matter 是现有的后期通路

`ConvertResourcesRaw` 接受 `ResourcesRaw` 及其子分类：
- `PlantMatter` 在其下；RawCotton、RawFlax 等可以作为原料。
- 当前棉花、亚麻的 sowTags 包含 Hydroponic。
- 300 原材料 → 150 Matter，研究为 `Super_matter_D1`。
- 矿石转换研究为 `Super_matter_E1`，工作台为 `MatterConverter`。

| Matter 消耗 | 产出 |
| ---: | --- |
| 50 | Iron 100 / Copper 100 / Tin 100 / Anglesite 100（分别为独立配方） |
| 50 | Aluminium 80 |
| 150 | Nickel 20 |
| 200 | Uranium 50 / Ilmenite 20（独立配方） |
| 100 | Silver 300 |
| 60 | Gold 20 |
| 250 | Sphalerite 50 / Titanomagnetite 25 / Wolframite 10（独立配方） |
| 50 | Polymers 100 / Paraffins 100 / Sulphates 100（独立配方） |

因此，多数矿石应标为“飞船出航早期依赖库存，后期研究和农业建成后可生产”，而不是永久断供。物质转换器本身的建材、电力、散热和加工设备需要提前准备。

这里使用了工作树当前有效配置。材料科学的重复锡石配方已在未提交改动中注释；有效快照仍有原有 `ConvertToTin`。钨矿配方来自本补丁的 `Defs/RecipeDef/Recipe_Matter.xml`，并未因此消失。

## 2. Rimefeller：缺的是原油，不是整条石化工业

现有 `CompRefinery.FuelConsumedPerTick` 检查 `pipeNet.TotalFuel`；`ChemfuelSiphon` 把物品 Chemfuel 回灌燃料管网。精炼不要求必须使用本地图开采的原油。

可用路线：

**作物/有机原料 → BiofuelRefinery → Chemfuel → ChemfuelSiphon → 储罐与管道 → 精炼设备 → 卸货台**

仍需资源控制台的生产设置、电力和足够储运设施。当前八种设备的真实产物如下；不能按旧 defName 猜测产物。

| 建筑 defName | 当前产物 | 结论 |
| --- | --- | --- |
| PolymerRefiner | Polymers：乙烯 | 可由生物化合燃料供应 |
| NapalmRefiner | Coke：焦炭 | 可由生物化合燃料供应；已不是凝固汽油 |
| SynthreadRefiner | HMFibers：高模量纤维 | 可由生物化合燃料供应 |
| HyperweaveRefiner | SyntheticFibers：聚合物纤维 | 可由生物化合燃料供应；已不是 Hyperweave |
| NeutroamineRefiner | Butadiene：丁二烯 | 可由生物化合燃料供应；已不是中性胺 |
| ParaffinRefiner | Xylene：二甲苯 | 可由生物化合燃料供应 |
| SulphateRefiner | Sulphates：苯 | 可由生物化合燃料供应 |
| SyntheticAmmoniaRefiner | Propylene：丙烯 | 可由生物化合燃料供应；氨另走配方 |

相关后续材料：
- 焦炭 → 氨；焦炭 + 镍/钴 → 碳纳米管。
- 苯 + 乙烯 → 聚苯乙烯；丁二烯 + 聚苯乙烯 → 合成橡胶。
- 丙烯 → 异丙醇/有机玻璃；乙烯 + 二甲苯 → 聚合物纤维。
- **PVC、聚碳酸酯、尼龙**的当前配方需要氯，仍受盐链限制，不能因有 Rimefeller 就全部标绿。
- **环氧树脂 Compaste**常规配方需要烧碱；另有 DragonScales → Compaste 配方，但必须确认对应生物能长期产出龙鳞。
- **Paraffins 实际是 PTFE**，常规配方需要氯、碎石、硫；后期可用 Matter 直接合成绕开。
- **Neutroamine 中性胺**可用 NeutroPetals 8 + Hypericum 2 → 1；SOS2 的特殊药树也有来源，但需要相应研究和植株条件。不能把当前 NeutroamineRefiner 当作中性胺设备。

本地 HSK DLC 源码对非 Surface 星球层禁止生成 Rimefeller 油田；它检查的是星球层，不能等同于对所有 SOS2 地图都做了 IsSpace 判定。普通船体不应被假定有可再生油井；任何太空地图意外生成地下油田都要作为兼容问题单独验证。

## 3. 生物原料

| 原料组 | 例子 | 自给前提 |
| --- | --- | --- |
| 主粮、蔬果、菌类 | 米、土豆、玉米、豆类、番茄、胡萝卜、蘑菇等 | 可种植品种、种苗、气压温度、光照、供水/种植介质 |
| 工业作物 | RawCotton、RawFlax、RawDevilstrand | 水培/农田与研究；可继续制布、纤维素、部分合成材料 |
| 药用植物 | aloe、NeutroPetals、Hypericum 等 | 对应种苗；中性胺并非必然外购 |
| 木料、树皮、竹材 | 木板/原木/各树种薪柴、TreeBark、Bamboo | 普通树木大多不是水培作物，需船上可持续种树的地面；竹类和特殊产木植物另查 sowTags |
| 天然乳胶 | RawCaoutchouc | 橡胶树需要 Ground；合成橡胶可走石化路线替代 |
| 肉、蛋、奶、毛、皮、甲壳 | 牲畜及指定昆虫/动物 | 带种畜、可繁殖种群与饲料；捕猎野生动物不属于船内自给 |
| 生物燃料/特殊生物材料 | Tallow、FSX、Prometheum、Synchronite 等 | 来源不同；当前爆炸动物有 FSX 产出、Desiri 有 Synchronite 产出，需要对应动物或植物 |
| 生物铁、超凡材料 | Bioferrite、ArchotechExoticParticles 等 | 实体/专门动物/超凡科技条件；不属于普通开局工业链 |

“作物可种植”不是指任意种子都可直接放水培盆。附表列出了当前有 sowTags 且有实际收获物的 80 个植物 Def（排除无收获物的盆景）；只标 Ground 的品种要单独准备种植地面。稀有动物产品可以再生，不表示出航后还能凭空获得该动物。

SOS2 已提供正常的船上种植地面：`SoilShip`（artificial soil substrate，人工土壤基质），位于建筑菜单的 `Floors`（地板），研究前置为 `ShipBasics`（Starflight basics）。当前缓存确认其肥力为 100%，带 `Ship` 标签，提供 `GrowSoil` 与 `Diggable`，铺设需要 Heavy 承重。普通船板肥力为 0，不能直接代替种植土壤。铺好基质后仍须设置种植区并满足作物光照、温度、种苗及其他环境条件；树木还须单独确认屋顶限制。

本地 [SOS2MiscPatch.xml](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Core_SK_Patch/LoadOnDemand/Save Our Ship 2/Patches/SOS2MiscPatch.xml:112>) 将基质每格造价改成 Dirt 40、CrushedStone 20、SoftClay 20、Peat 20、SandResource 20、Fertilizer 20。因此软黏土和泥炭的缺口会限制船上农田扩建；这里是一次性建造成本，不是基质每轮种植必定消耗这些材料。无需先使用 FertileFields 地形改造来获得这条种植路线。

## 4. 无普通船内闭环的东西，以及实际影响

| 缺口 | 直接受影响的材料/用途 | 边界与替代 |
| --- | --- | --- |
| 盐 | 氯、烧碱、尼龙、PVC、聚碳酸酯；常规铝土矿冶炼；部分环氧/表面活性剂及腌制 | 盐矿、含盐石料、外部补给；普通船板不能直接建盐矿 |
| 黏土 | 陶瓷、黏土砖、陶器、部分铸造与坩埚工艺 | 船上库存/黏土岩块；铸造某些零件可换其他工艺，但陶瓷仍需单独解决 |
| 天然岩块/特定石种 | 石材建筑、特定矿物提取、盐/黏土等 | 小行星可带回；砂岩循环不能生成泥岩或黏土岩 |
| 石英原石 | 石英本身、部分原矿用途 | 玻璃原料可改用砂子，不等于石英必需 |
| 煤矿石 | 指定 Coal 的工艺 | 接受 Coal 分类的配方常可用 Charcoal；焦炭另有 Rimefeller 路线，不能笼统判整个冶金断供 |
| 泥炭/沼铁 | 对应天然材料 | 金属含量可由其他再生矿石路线替代 |
| 玉、黑曜石、原宝石/超硬原宝石、化石 | 特定石材、珠宝以及需要宝石的高阶材料 | 天然来源、外部采集；普通船内未找到直接制造路线 |
| 辉光石/寒冷石 | 对应材料及用它们制作的设施 | 动态矿晶条件培养属于例外，详见下一节 |
| 原油 | 原油物品/原油专用用途 | 不能据此判定石化产品断供；多数由 Chemfuel 绕行 |
| 特定野生物种/种苗 | 只有这些物种才产出的原料 | 缺种源时仍依赖外部；普通肉奶毛皮可由其他带上船的动物替代 |
| 天然河湖海资源、地下水 | 野外捕鱼、河流动力、取地下水的设备 | 养殖、其他能源和水回收需分别审计，不能把人工水面自动算作有鱼/有地下水 |

需要特别注意铝：快照里还存在旧 `MakeAluminiumBars_Electric`（只消耗铝矿）的 RecipeDef，但它没有 recipeUsers，也未被任何工作台 recipes 列表引用；不能仅凭这个孤立配方宣布铝可绕过盐。实际 ElectricSmelter 当前关联的是需要盐或烧碱的铝土矿配方。因此，在盐问题未解决前，**铝及依赖它的合金/部件**仍是实际风险。

这份表列的是根材料缺口及直接影响；不同成品可能有回收、替代配方、特殊动物等旁路。不能把“某配方缺料”进一步推成“所有同名成品绝对无法获得”。

## 5. 必须单列的例外和配方问题

### 5.1 砂子净增产循环

有效配方：
1. `MakeBlocksSandstoneCementation`：100 SandResource → 50 BlocksSandstone。
2. `MakeCrushedStone_Hand`：20 任意 StoneBlocks → 20 CrushedStone。
3. `MakeSand_Hand`：10 CrushedStone → 25 SandResource。

为避免整数批次问题，可用：**200 沙子 → 100 砂岩砖 → 100 碎石 → 250 沙子**。每轮净增加 50 沙子；另耗燃料和工作时间。

因此当前配方数量上，砂子、碎石、砂岩砖，以及以它们为原料的玻璃料、硅、混凝土、助熔剂、土壤，都存在扩增可能。需要启动库存、可再生燃料及可运行的设备。CementationFurnace 有 `PlaceWorker_NotUnderRoof`，故仍须实测 SOS2 船外布置与炉体工作条件；本次没有把它记作已验证的稳定太空工厂。

普通 Kiln 的 50 沙子 → 20 砖路线回收只有 50 沙子，不产生净增量。不要把两种配方混为一谈。

### 5.2 盐矿不是“必须消耗盐脉”的配方

`VG_Minesalt` 不消耗原料，每次产 5 Salt，使用普通 Building_WorkTable。真正限制在建筑：`VG_SaltMine` 要求 `SmoothableStone`，而 SOS2 普通船内地板只有 Light/Medium/Heavy。

所以“普通船板上新建盐矿”不可行；但若把地面预建的盐矿随船运走，是否仍工作，需要实测搬运和地形保留。配方本身没有再次检查地下盐储量，不能声称绝对无法绕过。

### 5.3 Minerals 动态矿晶

ColdstoneCrystal / GlowstoneCrystal 有生长与繁殖逻辑，受温度、光照、邻近地形、屋顶等条件影响；其允许地形不包含普通船板。寒冷石还要求邻近冰/水等环境。若能合法携带活矿晶并保留适宜生态地形，可能形成条件再生来源；本次不将其与普通矿石一并判为绝对不可再生。

`randomlyDropResources` 是采矿伤害触发的掉落，不是仅把大矿晶放在船上就会定期吐资源。

### 5.4 深钻、采石场和地形改造

Quarry 默认至少要有约 60% 有效岩石格；普通船板不满足。HSK 地下提取器读取地图 deepResourceGrid，不能把“有电、有机器”当作“有地下资源”。FertileFields 的地形转换和随船搬运设备可能绕过部分限制，但需要验证是否保留船体、是否破坏气密及是否产生合法地形。未将开发者模式或 placeAnywhere 选项算作正常来源。

### 5.5 水和水培

实际已加载 `SpacerWaterRecoverySystem` 和无水冲洗的太空厕所。已检查的 BadHygiene DLL 中，回收器可让洗澡、洗手和水培的专门耗水路径跳过取水/耗水；这不是凭空增加水塔库存的制水机。

水培盆仍带水储量组件，其他饮水、设备用水与初始注水应独立测试。本地 DLC 源码禁止非 Surface 层生成浅层/深层地下水。不能把“有回收器”直接等同于所有水用户永远供水，也不能笼统说太空一定无法种植。

## 6. 若允许太空采矿

SOS2 的 `GenStep_ValuableAsteroids` 从已加载的自然资源岩石中抽取矿体，并不是只认原版钢/银/金。当前 HSK 的很多矿石、特殊石材可以进入这一机制的候选范围。

这意味着许多“船内无法制造”的石料、盐矿、黏土岩、宝石类材料可以尝试从太空地点补充；具体生成仍受候选 Def、随机选择和地图生成兼容情况影响，不保证每个小行星都有。每张地图的矿体有限，持续探索新矿点是外部采集。

## 7. 后续适配优先级

1. **盐与氯/烧碱**：决定多条塑料和铝冶炼链，优先确认是否需要正常的太空来源。
2. **黏土和基础石料**：为陶瓷、玻璃、硅和工业扩建提供明确的正常路线，同时决定是否修复砂子扩增配方。
3. **水培、饮水及工业用水实测**：先证明农业和燃料原料能持续供应，再计算产能。
4. **原宝石/超硬宝石、玉、黑曜石和动态矿晶**：决定保留探索依赖，还是增加高阶合成/培养。
5. **原油本身**：优先级低于盐；现有 Chemfuel 回灌已能支持大部分精炼。

本次仅整理资料，没有改动资源、配方、模组配置或现有未提交工作。

## 8. 证据入口

- 有效缓存：[Unified.xml](<C:/Users/fucon/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/MissileGirl/Cache/Unified.xml>)。它保存了来源路径；重复 Def 的最终运行归属仍以游戏加载为准。
- Matter：[Recipes_Matter.xml](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Core_SK/Defs/RecipeDefs/Recipes_Matter.xml>)；[钨矿转换](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Core_SK_Patch/Defs/RecipeDef/Recipe_Matter.xml>)；[材料科学转换补丁](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Core_SK_Patch/LoadOnDemand/Material Science/Defs/MatterConversion.xml>)。
- 砂石粉碎：[Recipes_Materials_Resources.xml](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Core_SK/Defs/RecipeDefs/Recipes_Materials_Resources.xml>)；砂岩烧制：[RecipesProcesses.xml](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Vile's Metallurgy/Defs/Recipes/RecipesProcesses.xml>)。
- 盐/氯/烧碱：[Recipes_Chemical.xml](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/Vile's Materials Science/Common/Defs/RecipesDefs/Recipes_Chemical.xml>)。
- Rimefeller 精炼：[CompRefinery.cs](<E:/CACHE/Hardcore-SK-Source/Source/Rimefeller/Rimefeller/CompRefinery.cs>)；已交叉检查部署的 Rimefeller.dll。产品使用有效缓存，不能只读取模组原始建筑 XML。
- 小行星：[GenStep_ValuableAsteroids.cs](<D:/SteamLibrary/steamapps/common/RimWorld/Mods/SaveOurShip2/Source/1.6/MapGen/GenStep_ValuableAsteroids.cs>)。
- 动态矿晶：[DynamicMineral.cs](<E:/CACHE/Hardcore-SK-Source/Source/Minerals/Source/Minerals/DynamicMineral.cs>)、[StaticMineral.cs](<E:/CACHE/Hardcore-SK-Source/Source/Minerals/Source/Minerals/StaticMineral.cs>)。
- 太空油田/地下水生成限制：[Patch_OilGrid_GenerateFields.cs](<E:/CACHE/Hardcore-SK-Source/Source/HSK/DLCModule/HSK_OdysseyChanges/Rimefeller/Patch_OilGrid_GenerateFields.cs>)、[Patch_MapComponent_Hygiene_RegenWaterGrid.cs](<E:/CACHE/Hardcore-SK-Source/Source/HSK/DLCModule/HSK_OdysseyChanges/BadHygiene/Patch_MapComponent_Hygiene_RegenWaterGrid.cs>)。

## 附表 A：有效原资源分类的完整物品清单（134 项）

“后期可合成”只保证该原料有路线，整条工厂还需辅料、工作台、研究、电力、供水与环境。表中保留当前英文 label，避免沿用旧译名误认化学品。

| defName | 当前 label | 分类结论 | 依据/限制 |
| --- | --- | --- | --- |
| MedicineHerbal | Raw herbal medicine | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| RawHops | hops | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| PsychoidLeaves | psychoid leaves | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| SmokeleafLeaves | smokeleaf leaves | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| Silver | Silver Coin | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Gold | Native Gold (Au) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| WoodLog | riven boards | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Uranium | Carnotite (U+V) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Jade | jade | 缺口 | 天然矿物/化石；小行星或其他外部来源不属于船内生产 |
| BlocksSandstone | sandstone blocks | 条件/配方循环 | 正常依赖石料；砂岩烧制与粉碎存在净增产循环，需设施可用和启动库存 |
| BlocksGranite | granite blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| BlocksLimestone | limestone blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| BlocksSlate | slate blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| BlocksMarble | marble blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| Dye | dye | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| Bioferrite | bioferrite | 特殊条件 | 实体收容或指定尸体处理；需要启动来源、相应 DLC 与设施 |
| Obsidian | obsidian | 缺口 | 天然矿物/化石；小行星或其他外部来源不属于船内生产 |
| BlocksVacstone | vacstone blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| DriedLeavesSmokeleaf | dried smokeleaf leaves | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| DriedLeavesTobacco | dried tobacco leaves | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| CoffeeBeans | coffee beans | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| PoisonGland | venom gland | 特殊条件 | 依赖能持续获得的特定生物 |
| Polymers | Ethylene | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Rubber | Rubber | 可再生 | 植物或石化衍生链；不必依赖天然乳胶 |
| SyntheticFibers | Polymer Fibers | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Carbon | carbon nanotube | 可再生/后期 | 化合燃料制焦炭；碳纳米管另需镍钴 |
| SyntheticAmmonia | Ammonia | 可再生/后期 | 化合燃料制焦炭；碳纳米管另需镍钴 |
| Paraffins | Teflon | 后期可合成 | PTFE 配方含碎石与氯，另有 Matter 直接产出路线 |
| Sulfur | Sulfur | 后期可再生 | 合成矿石后提硫/冶炼副产物；无需依赖天然地热口 |
| Sulphates | Benzene | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Compaste | Epoxy Resin | 特殊条件 | 常规环氧/表面活性剂链依赖盐或烧碱；龙鳞环氧路线需对应动物持续产出 |
| RawCotton | Raw Cotton | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| RawFlax | Raw flax | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| RawDevilstrand | Raw Devilstrand | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| NeutroPetals | Neutro Flower Petals | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| aloe | Aloe Leaves | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| RawCaoutchouc | natural latex | 条件可再生 | 种植/采集和加工；需种苗，水培适用性见种植附表 |
| Salt | salt | 缺口 | 盐依赖盐矿/含盐石料；氯与烧碱依赖盐 |
| Glowstone | Glowstone | 特殊条件 | 动态矿晶可生长繁殖，但需种晶、适宜自然地形和环境；普通船板不满足 |
| Coldstone | Coldstone | 特殊条件 | 动态矿晶可生长繁殖，但需种晶、适宜自然地形和环境；普通船板不满足 |
| Peat | Peat | 缺口但可替代 | 泥炭需天然来源；沼铁可由其他矿石替代 |
| SoftClay | Soft Clay | 缺口 | 黏土岩块/天然地形；未找到船上合成配方 |
| Nitre | Nitre | 可再生 | 腐烂植物/粪污/尸体与灰烬制硝 |
| Coal | Coal Ore | 缺口但可替代 | 煤矿石无合成；部分用途可改用木炭或化合燃料制焦炭 |
| Charcoal | Charcoal | 可再生 | 生物废料/木料加工；需建成相应设备及满足工作环境 |
| Ash | ash | 可再生 | 生物废料/木料加工；需建成相应设备及满足工作环境 |
| Iron | Magnetite (Fe+Cr+Zn) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Nickel | Pentlandite (Ni/Fe+Co) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Copper | Kalcopyrite (Cu+Fe+Au) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Tin | Cassiterite (Sn+Ag+W) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Anglesite | Galena (Pb/Ag+Cu) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Aluminium | Bauxite (Al+Si+Cr) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Ilmenite | Rutile (Ti+Mo) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Wolframite | Scheelite (W+Mo) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Dirt | silt | 条件/配方循环 | 正常依赖石料；砂岩烧制与粉碎存在净增产循环，需设施可用和启动库存 |
| SandResource | sand | 条件/配方循环 | 正常依赖石料；砂岩烧制与粉碎存在净增产循环，需设施可用和启动库存 |
| CrushedStone | rubble | 条件/配方循环 | 正常依赖石料；砂岩烧制与粉碎存在净增产循环，需设施可用和启动库存 |
| Compost | compost | 可再生 | 生物废料/木料加工；需建成相应设备及满足工作环境 |
| Fertilizer | fertilizer | 可再生 | 生物废料/木料加工；需建成相应设备及满足工作环境 |
| RedWoodLog | redwood log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Bamboo | bamboo log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| BambooPlank | bamboo plank | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| RedWoodPlank | redwood plank | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Surfactant | Surfactant Fuel | 特殊条件 | 常规环氧/表面活性剂链依赖盐或烧碱；龙鳞环氧路线需对应动物持续产出 |
| OilBarrel | Crude Oil | 缺口但可替代 | 原油依赖油田；多数精炼产品可以由生物化合燃料绕过原油生产 |
| ZF_BlocksBasalt | basalt blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| ZF_BlocksClay | clay blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| ZF_BlocksMudstone | mudstone blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| ZF_BlocksAlabaster | alabaster blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| ZF_BlocksPegmatite | pegmatite blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| ZF_BlocksDunite | dunite blocks | 缺口/特定石种 | 天然石种来源；砂岩循环不能自动生成其他石种 |
| SmallFossil | Small fossil | 缺口 | 天然矿物/化石；小行星或其他外部来源不属于船内生产 |
| RoughGem | Rough Gem | 缺口 | 天然矿物/化石；小行星或其他外部来源不属于船内生产 |
| RoughUltrahardGem | Rough Ultrahard Gem | 缺口 | 天然矿物/化石；小行星或其他外部来源不属于船内生产 |
| CannonParts | cannon barrel part | 加工品 | 虽在原资源分类中，但并非基础原料；取决于金属/木材及配方辅料 |
| Sudis | sudis | 加工品 | 虽在原资源分类中，但并非基础原料；取决于金属/木材及配方辅料 |
| Firewood_Cecropia | cecropia firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Cecropia | cecropia firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Pine | pine firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Pine | pine firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Willow | willow firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Willow | willow firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Redwood | redwood firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Redwood | redwood firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Spruce | spruce firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Spruce | spruce firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Cypress | cypress firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Cypress | cypress firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Poplar | poplar firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Poplar | poplar firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Maple | maple firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Maple | maple firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Teak | teak firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Teak | teak firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Birch | birch firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Birch | birch firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Oak | oak firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Oak | oak firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Bamboo | bamboo firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Bamboo | bamboo firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Dragonwood | dragonwood firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Dragonwood | dragonwood firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Firewood_Mangrove | mangrove firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Mangrove | mangrove firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| CecropiaLog | cecropia log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| PineLog | pine log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| WillowLog | willow log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| CypressLog | cypress log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| PoplarLog | poplar log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| MapleLog | maple log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| TeakLog | teak log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| BirchLog | birch log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| OakLog | oak log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| AcaciaLog | acacia log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| DragonwoodLog | Dragonblood log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| MangroveLog | Mangrove log | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FluxPowder | flux | 条件/配方循环 | 正常依赖石料；砂岩烧制与粉碎存在净增产循环，需设施可用和启动库存 |
| TreeBark | tree bark | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| Chlorine | Chlorine | 缺口 | 盐依赖盐矿/含盐石料；氯与烧碱依赖盐 |
| IsopropylAlcohol | Isopropyl Alcohol | 可再生 | 植物或石化衍生链；不必依赖天然乳胶 |
| Lye | Lye (sodium hydroxide) | 缺口 | 盐依赖盐矿/含盐石料；氯与烧碱依赖盐 |
| Cellulose | Cellulose | 可再生 | 植物或石化衍生链；不必依赖天然乳胶 |
| Titanomagnetite | Titanomagnetite (Fe+Ti+V) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| Sphalerite | Sphalerite (Zn+Mn+Co) | 后期可合成 | Matter 配方；矿石可再生不等于其所有冶炼辅料已闭环 |
| BogIron | Bog Iron (Fe + Mn) | 缺口但可替代 | 泥炭需天然来源；沼铁可由其他矿石替代 |
| Quartz | Quartz (SiO_2) | 缺口但可替代 | 无石英合成；硅料可用沙子制取 |
| Propylene | Propylene | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Butadiene | Butadiene | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Xylene | Xylene | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Nylon | Nylon | 缺口 | 目前合成配方需要氯，受盐供应限制 |
| HMFibers | High-Modulus Fibers | 可再生 | Rimefeller 精炼化合燃料；有效产品已按材料科学补丁变化 |
| Polystyrene | Polystyrene | 可再生 | 植物或石化衍生链；不必依赖天然乳胶 |
| Firewood_Acacia | acacia firewood | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |
| FirewoodSeasoned_Acacia | acacia firewood, seasoned | 条件可再生 | 对应树种/竹类；普通树木需地面种植空间或特殊种植设施 |

## 附表 B：当前可播种植物的产物与种植标签

此表按有 sowTags 的植物 Def 提取；不把野生可采集但不可播种的植物误算成可再生作物。稀有 SOS2 植物还需额外研究和种源。其他生物产物按第 3 节管理。

| 植物 defName | 收获物 defName | sowTags |
| --- | --- | --- |
| Glowstool | RawGlowbulb | Fungiponics |
| Agarilux | GleamcapStem | Fungiponics |
| Bryolux | RawShimmershroom | Fungiponics |
| Plant_Rice | RawRice | Ground,Hydroponic |
| Plant_Potato | RawPotatoes | Ground,Hydroponic |
| Plant_Corn | RawCorn | Ground,Hydroponic |
| Plant_Strawberry | RawBerries | Ground,Hydroponic |
| Plant_Haygrass | Hay | Ground,Hydroponic |
| Plant_Cotton | RawCotton | Ground,Hydroponic |
| Plant_Devilstrand | RawDevilstrand | Hydroponic |
| Plant_Hops | RawHops | Ground,Hydroponic |
| Plant_Smokeleaf | SmokeleafLeaves | Ground,Hydroponic |
| Plant_Psychoid | PsychoidLeaves | Ground,Hydroponic |
| Plant_TreeCocoa | Rawcocoa | Ground,Hydroponic |
| Plant_Tinctoria | Dye | Ground,Hydroponic |
| Plant_Timbershroom | WoodLog | Fungiponics |
| Plant_SaguaroCactus | WoodLog | Ground |
| Plant_TreeDrago | DragonwoodLog | Ground |
| Plant_Bush | Kindling | Ground |
| Plant_TreeWillow | WillowLog | Ground |
| Plant_TreeCypress | CypressLog | Ground |
| Plant_TreeMaple | MapleLog | Ground |
| Plant_TreeOak | OakLog | Ground |
| Plant_TreePoplar | PoplarLog | Ground |
| Plant_TreePine | PineLog | Ground |
| Plant_TreeBirch | BirchLog | Ground |
| Plant_TreeTeak | TeakLog | Ground |
| Plant_TreeCecropia | CecropiaLog | Ground |
| Plant_TreePalm | RawCoconut | Ground |
| Plant_TreeBamboo | Bamboo | Ground,Hydroponic |
| Plant_Nutrifungus | RawFungus | Ground,Hydroponic,Fungiponics |
| Plant_Fibercorn | WoodLog | Ground,Hydroponic |
| Plant_Toxipotato | RawToxipotato | Ground,Hydroponic |
| Plant_PebbleCactus | WoodLog | Ground |
| Plant_TreeGrayPine | WoodLog | Ground |
| Plant_Witchwood | WoodLog | Ground |
| Plant_RatPalm | WoodLog | Ground |
| Plant_TreeSnagroot | WoodLog | Ground |
| Plant_Vanoroot | RawVanoroot | Ground,Hydroponic |
| Plant_Slaughtermelon | Chemfuel | Ground,Hydroponic |
| Plant_Pharmatree | Neutroamine | Ground,Hydroponic |
| Plant_Meatbush | Meat_Muffalo | Ground,Hydroponic |
| Plant_AmbrosiaArchotech | Ambrosia | Ground,Hydroponic |
| PlantBlazebulb | Prometheum | Hydroponic |
| Plant_Rhododendron | Kindling | Ground |
| Plant_Watermelon | RawWatermelon | Ground,Hydroponic |
| Plant_TreeApple | Rawapple | Ground |
| Plant_TreeBanana | Rawbanana | Ground |
| Plant_TreeOrange | Raworange | Ground |
| Plant_TreePeach | Rawpeach | Ground |
| Plant_TreeGrape | Rawgrape | Ground,Hydroponic |
| Plant_Flax | RawFlax | Ground,Hydroponic |
| Plant_Sugarcane | Rawsugarcane | Ground,Hydroponic |
| Plantwheat | Rawwheat | Ground,Hydroponic |
| Plant_Tobacco | RawTobacco | Ground,Hydroponic |
| Plant_Coffee | RawCoffee | Ground,Hydroponic |
| Plant_Tea | RawTea | Ground,Hydroponic |
| Plant_TreeHevea | RawCaoutchouc | Ground,Ground |
| Plant_Aloe | aloe | Ground,Hydroponic |
| Plant_NeutroFlower | NeutroPetals | Hydroponic |
| Plantbean | Rawbean | Ground,Hydroponic |
| PlantTomato | RawTomatoes | Ground,Hydroponic |
| PlantCarrot | RawCarrots | Ground,Hydroponic |
| Plant_Mushroom | Rawmushroom | Ground,Hydroponic |
| Plant_Mangrove | MangroveLog | Ground |
| PlantTreeRedwood | RedWoodLog | Ground |
| Plant_WildRose | WildRose | Hydroponic |
| Plant_Hypericum | Hypericum | Hydroponic |
| Plant_Mint | MintLeaves | Hydroponic |
| Plant_Onion | Rawonion | Ground,Hydroponic |
| Plant_TreeSpruce | Firewood_Spruce | Ground |
| Plant_TreePalmetto | WoodLog | Ground |
| Plant_TreeKapok | Kindling | Ground |
| Plant_PricklyPear | RawPricklyPear | Ground,Hydroponic |
| Plant_Cranberry | RawCranberries | Ground,Hydroponic |
| Plant_Blueberry | Rawblueberry | Ground,Hydroponic |
| Plant_TreeAcacia | AcaciaLog | Ground |
| Plant_Juniper | Kindling | Ground |
| Plant_TreeDwarfWillow | Firewood_Willow | Ground |
| PepperPlant | RawPeppers | Ground,Hydroponic |
