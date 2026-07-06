# CS2 海克斯内战插件配置与玩法规范说明书

本文件用于记录和同步当前 **CS2HextechPlugin** 插件中已实现的所有海克斯效果、核心机制以及对应参数数值。

> 💡 **重要说明**：
> 后面您可以直接修改本文档中的**参数数值**、**文案提示**或**规则描述**。修改完成后发消息告诉我，我将自动读取本文档的最新修改，并重新编译插件更新服务端！

---

## 一、 核心玩法与选卡规则 (Global Rules)
*   **运行模式 (Core Mode)**：支持以下两种运行机制，您可以通过修改此处来选择：
    *   **模式 1（默认机制）**：每回合都重新抽取海克斯，海克斯取消等级分类，所有等级的混在一起让用户三选一（且不重复），且海克斯效果**不叠加**（每回合重置为当前回合选择的效果）。
    *   **模式 2（多阶段叠加机制）**：所有玩家仅在第 **1、6、11、16** 回合刷新三张不重复海克斯（所有玩家都为同一等级海克斯），海克斯效果**会叠加**（在对应回合选择后，效果持续累加生效）。
    *   *当前激活模式*：**模式 1**（您可在此修改为 `模式 1` 或 `模式 2`）
*   **海克斯总池数**：当前共有 **24** 种海克斯效果可用。
*   **每局抽卡数量**：在符合抽卡规则的轮次，玩家复活（Spawn）延迟 1.5 秒后，系统会从总池中**随机筛选 3 个互不重复**的海克斯选项，以中央 HTML 菜单（Center HTML Menu）呈现给玩家。
*   **手动唤出指令**：若符合抽卡资格但未选卡或需重新唤出菜单，可在游戏公屏输入 **`!hex`** 或 **`/hex`**，控制台可输入 **`css_hex`**。

---

## 二、 海克斯技能配置明细 (Augment Configurations)

您可以直接修改下方各技能的参数（如数值大小、限制次数或游戏内提示词），我会依此重写 C# 插件代码。当前海克斯分为 **🥈 银色 (8个)**、**🥇 金色 (8个)**、**🔮 彩色 (8个)** 三个级别。

---

## 🥈 银色海克斯 (Silver Augments)

### 1. ⚡ 风行者 (Speed)
*   **效果描述**：提升玩家的移动奔跑速度。
*   **实现可行性**：**完全可行**。在玩家 Spawn 时，通过修改玩家 Pawn 实体的 `VelocityModifier` 为 `1.3f` 即可实现速度提升。
*   **配置参数**：
    *   `SpeedModifier` (移动速度修正倍率)：**1.3** (代表提升 30% 速度)
*   **游戏内提示语**：`你已成功披上 风行者之翼，移动速度提升 30%！`

### 2. 💎 赏金猎人 (Bounty)
*   **效果描述**：每击杀一次敌人，获得现金奖励并刷新近战电击枪。
*   **实现可行性**：**完全可行**。监听玩家死亡事件 `EventPlayerDeath`，若攻击者拥有此海克斯，则增加其 `Account` 钱包金额并调用 `GiveNamedItem("weapon_taser")`。
*   **配置参数**：
    *   `KillReward` (击杀额外奖励金额)：**$2000**
    *   `RefreshWeapon` (刷新并赠送的装备代码)：`weapon_taser` (电击枪)
*   **游戏内提示语**：
    *   激活技能时：`你已成功雇佣 赏金猎人，每完成一次击杀将获得 $2000 现金并刷新 电击枪！`
    *   每次击杀获得奖励时：`击杀成功！额外获得 $2000 现金 并已刷新 电击枪！`

### 3. 🛡️ 棘刺甲壳 (Thorns)
*   **效果描述**：受到伤害时，将受击物理伤害的 30% 反弹给攻击者。
*   **实现可行性**：**完全可行**。监听受害者受伤事件 `EventPlayerHurt`。若受害者拥有该海克斯，且攻击者有效，则计算反弹伤害 并从攻击者的血量中扣除。
*   **配置参数**：
    *   `ReflectRatio` (反弹伤害比例)：**30%** (0.3)
*   **计算公式**：
    *   反弹结算：
        $$Health_{attacker} = \max(0, Health_{attacker} - \lfloor DmgHealth \times ReflectRatio \rfloor)$$
*   **游戏内提示语**：
    *   激活技能时：`你已成功披上 棘刺甲壳，受到的 30% 伤害将直接反击给对手！`
    *   每次反伤触发时：`你受到了来自棘刺的反弹伤害 {reflectDmg} HP！`

### 4. 🔥 狂徒 (Reckoner)
*   **效果描述**：连续 10 秒未受到伤害后，每秒自动回复 2 HP，最多恢复至最大生命值的 80%。
*   **实现可行性**：**完全可行**。在 `EventPlayerHurt` 中更新玩家最后受击时间戳。在 `OnTick` 中，如果当前时间与最后受击时间间隔大于 10 秒，且距离上次回复 Tick 超过 1 秒，若当前血量小于最大血量的 80%，则给予 2 HP 回复。
*   **配置参数**：
    *   `RegenDelay` (触发回血所需的连续无伤时间)：**10** 秒
    *   `RegenPerSecond` (每秒回复血量)：**2 HP**
    *   `RegenCap` (回血上限，相对于最大生命值的百分比)：**80%** (0.8)
*   **计算公式**：
    *   回血门槛：$CurrentTime - LastHurtTime \ge RegenDelay$ 且距上一次回血时间 $\ge 1.0$ 秒。
    *   血量回复：若 $Health_{current} < \lfloor MaxHealth \times RegenCap \rfloor$：
        $$Health_{new} = \min(\lfloor MaxHealth \times RegenCap \rfloor, Health_{current} + RegenPerSecond)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 狂徒，10 秒无伤后将开始每秒回复 2 HP（上限为最大血量的 80%）！`
    *   每次触发回血时（静默，不输出提示，避免频繁刷屏）

### 5. 🔥 流血 (Bleed)
*   **效果描述**：攻击敌人时，造成总共 10 点流血伤害，每秒造成 2 伤害，持续 5 秒。5 秒内伤害不叠加。
*   **实现可行性**：**完全可行**。在 `EventPlayerHurt` 事件中监听，当攻击者拥有该海克斯时，为受害者打上流血标记，并在插件的全局 `OnTick` 监听或 Timer 中每秒扣除受害者血量，更新状态。5 秒内不叠加，可通过记录受害者流血结束时间戳来实现，流血期间再次受击不重置也不重叠时间。
*   **配置参数**：
    *   `BleedDamage` (每次流血造成的伤害)：**2 HP**
    *   `BleedDuration` (流血效果持续时间)：**5** 秒
*   **计算公式**：
    *   流血总伤害：$Damage_{total} = BleedDamage \times BleedDuration$
    *   每秒结算（若当前时间小于流血结束时间）：
        $$Health_{new} = \max(0, Health_{current} - BleedDamage)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 流血，攻击敌人将造成每秒造成 2 伤害，持续 5 秒的流血伤害！`
    *   每次触发流血伤害时（静默，不输出提示，避免频繁刷屏）

### 6. 🪄 精怪魔法 (FlashMagic)
*   **效果描述**：被闪光弹影响的敌人 3 秒内不能开火。
*   **实现可行性**：**完全可行**。监听玩家被闪光致盲事件（`player_blind`）。如果投掷闪光弹的攻击者拥有该海克斯，为受害者设定一个 3 秒的禁用开火状态。在 OnTick 循环中，强行清除其 `Buttons` 掩码中的 `PlayerButtons.Attack` 与 `PlayerButtons.Attack2` 属性，使其按鼠标左键/右键开火时没有任何响应。
*   **配置参数**：
    *   `DisableFireDuration` (不能开火持续时间)：**3** 秒
*   **计算公式**：
    *   状态持续判定：致盲时间起 3 秒内，在 OnTick 中强制清除开火键状态：
        $$Buttons_{new} = Buttons_{current} \ \& \ \sim(PlayerButtons.Attack \ | \ PlayerButtons.Attack2)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 精怪魔法，被闪光弹影响的敌人 3 秒内不能开火！`

### 7. 💣 道具大师 (GrenadeMaster)
*   **效果描述**：你的手雷、燃烧弹伤害提升 50%，并且允许购买两颗手雷两个燃烧瓶。
*   **实现可行性**：**部分可行（受引擎限制）**。
    *   *限制说明*：CS2 的购买栏与物品栏最大同类携带数受引擎与全局指令硬性限制，插件无法为单个特定玩家修改最大携带上限。
    *   *变通方案*：在插件中记录玩家本回合各类投掷物的投掷次数。在玩家投出第一颗 HE 手雷或燃烧弹后，若该类投掷物本回合累计投出次数小于 2，且当前背包中已无该道具，延迟 0.1 秒由代码后台为玩家自动补给一颗相同的道具。此机制可在体验上完美达到“可以使用两颗”的效果。
*   **配置参数**：
    *   `GrenadeDamageMultiplier` (手雷与燃烧弹伤害提升倍率)：**1.5** (50% 提升)
    *   `MaxHegrenadeCount` (手雷最大可用数量)：**2**
    *   `MaxMolotovCount` (燃烧弹最大可用数量)：**2**
*   **计算公式**：
    *   投掷物最终伤害（伤害源为手雷项目 HE Grenade Projectile 或燃烧弹 Inferno/Molotov）：
        $$Damage_{final} = Damage_{base} \times GrenadeDamageMultiplier$$
    *   补给判定：如果发生投掷事件，且该项投掷物的本回合 $ThrowCount < MaxCount$ 时，自动给该玩家 `GiveNamedItem("weapon_hegrenade/weapon_molotov")`。
*   **游戏内提示语**：
    *   激活技能时：`你已激活 道具大师，你的手雷、燃烧弹伤害提升 50%，并且允许购买两颗手雷两个燃烧瓶！`

### 8. 💣 自爆卡车 (LastStand)
*   **效果描述**：死亡时在原地引发爆炸，对 300 码范围内的所有敌人造成最多 100 点伤害。
*   **实现可行性**：**完全可行**。监听玩家死亡事件 `EventPlayerDeath`，若拥有自爆卡车，则遍历附近敌对玩家并计算空间距离 $dist$。在 `ExplosionRadius` 范围内计算距离衰减伤害并直接扣除玩家生命值。
*   **配置参数**：
    *   `ExplosionRadius` (自爆伤害范围)：**300** 码
    *   `MaxExplosionDamage` (中心最高爆炸伤害)：**100**
*   **计算公式**：
    *   距离公式：$dist = \sqrt{(x_1-x_2)^2 + (y_1-y_2)^2 + (z_1-z_2)^2}$
    *   伤害公式（若 $dist \le ExplosionRadius$）：
        $$Damage_{dealt} = \lfloor (1.0 - \frac{dist}{ExplosionRadius}) \times MaxExplosionDamage \rfloor$$
        $$Health_{new} = \max(0, Health_{current} - Damage_{dealt})$$
*   **游戏内提示语**：
    *   激活技能时：`你已装载 自爆卡车，在死亡的瞬间将拉线自爆，让周围敌人陪葬！`
    *   造成自爆伤害提示：`[自爆卡车] 死亡引发了爆炸，对敌方 {name} 造成了 {dmg} HP 的爆炸伤害！`
    *   受害者被炸提示：`[自爆卡车] 敌人在你身边自爆，你受到了 {dmg} HP 的爆炸伤害！`

### 9. 质变黄金 (GoldenChange)
*   **效果描述**：选择后随机获得一个金色阶（黄金阶）海克斯并应用效果。
*   **实现可行性**：**完全可行**。当玩家在菜单中选择“质变黄金”时，插件在后台逻辑中从全部 8 个金色阶海克斯中随机抽取一个，并立即调用该海克斯的 `ApplyAction` 应用效果，向玩家发送新海克斯提示。
*   **配置参数**：
    *   `ChangeTier` (质变的目标梯队)：**金色阶 (Gold Augments)**
*   **计算公式**：
    *   质变随机应用逻辑：设金色海克斯集合为 $G$，有：
        $$Augment_{applied} = RandomElement(G)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 质变黄金！正在为您发生质变... 恭喜获得金色海克斯：{augment_name}！`


---

## 🥇 金色海克斯 (Gold Augments)

### 1. ❤️ 泰坦之躯 (Titan)
*   **效果描述**：大幅提升最大生命值。
*   **实现可行性**：**完全可行**。在玩家 Spawn 延迟 0.1 秒后设置 Pawn 实体的 `MaxHealth` 为 200，并重设 `Health` 为 200，调用 `SetStateChanged` 广播。
*   **配置参数**：
    *   `MaxHealth` (最大生命值上限)：**200**
    *   `SpawnHealth` (复活时的初始血量)：**200**
*   **游戏内提示语**（绿色前缀）：`你已成功注入 泰坦之躯，血量已跃升至 200 HP！`

### 2. 🩸 吸血狂热 (Vampire)
*   **效果描述**：对敌人造成伤害时，按比例恢复自身生命值。
*   **实现可行性**：**完全可行**。在 `EventPlayerHurt` 中进行判定，按攻击伤害的 30% 回复攻击者血量，不超过当前最大血量限制。
*   **配置参数**：
    *   `VampireRatio` (伤害转化为血量恢复的比例)：**30%** (0.3)
*   **计算公式**：
    *   吸血结算：
        $$Health_{attacker} = \min(MaxHealth_{attacker}, Health_{attacker} + \lfloor DmgHealth \times VampireRatio \rfloor)$$
*   **游戏内提示语**：
    *   激活技能时：`你已成功掌握 吸血狂热，本局你造成的 30% 物理伤害将转化为回血！`
    *   每次造成伤害回血时：`成功恢复了 {healAmount} HP！`

### 3. 🌀 闪现星使 (Blink)
*   **效果描述**：允许玩家通过双击按键瞬间向前移动一段距离。
*   **实现可行性**：**完全可行**。在 OnTick 里判定当玩家连按交互键且时间间隔符合要求时，读取玩家的视线偏角（Yaw），沿视线计算位移后的全新 Vector 坐标，调用 Pawn 实体的 `Teleport` 函数完成坐标更新。
*   **配置参数**：
    *   `TriggerKey` (双击触发的物理按键)：**双击 E 键** (游戏内的 `+use` 交互键，`PlayerButtons.Use`)
    *   `DoubleClickInterval` (双击判定的最大毫秒时间间隔)：**300ms**
    *   `BlinkDistance` (瞬移距离)：**160** 码 (约合 4 米)
    *   `BlinkLimitPerRound` (单回合允许使用的次数限制)：**2** 次
*   **游戏内提示语**：
    *   激活技能时：`你已成功注入 闪现星使，在游戏中 双击 E 键 即可向前闪现！`
    *   每次闪现成功时：`闪现成功！本回合还剩 {remaining} 次。`
    *   次数用尽提示时：`本回合你的闪现次数已用完（限 2 次）！`
    *   死亡状态使用提示：`你必须存活时才能使用闪现！`

### 4. 🏃 狂暴杀戮 (KillRush)
*   **效果描述**：击杀敌人后，获得 3 秒的超高速增幅（+30% 速度）。
*   **实现可行性**：**完全可行**。监听玩家死亡事件 `EventPlayerDeath`，若攻击者拥有此海克斯，将其移速 `VelocityModifier` 修改为 `1.3f`，记录 3 秒后的结束时间戳。若在 3 秒到期且期间没有产生新击杀，则将其速度重置为常态。
*   **配置参数**：
    *   `SpeedBoostModifier` (加速期间速度修正)：**1.3** (1.3 倍速)
    *   `BoostDuration` (加速持续时间)：**3** 秒
*   **游戏内提示语**：`你已成功激活 狂暴杀戮，完成击杀后将获得 3 秒 30% 极限移速加成！`

### 5. 🎖️ 重装坦克 (HeavyInfantry)
*   **效果描述**：复活时自带全套防弹衣与头盔，且受到的所有伤害减少 15%。
*   **实现可行性**：**完全可行**。在玩家复活时调用 `GiveNamedItem("item_assaultsuit")`。在 `OnTakeDamage` 伤害预处理事件中，若受害者有此海克斯，直接将伤害值 `info.Damage` 乘以 `0.85`。
*   **配置参数**：
    *   `DamageReduction` (受击伤害减免比例)：**15%** (0.85)
    *   `SpawnArmor` (初始护甲值)：**100**
    *   `SpawnHelmet` (包含头盔)：**是**
*   **计算公式**：
    *   伤害削减：
        $$Damage_{final} = Damage_{base} \times (1.0 - DamageReduction)$$
*   **游戏内提示语**：`你已成功武装 重装坦克，获得免费防弹衣与头盔，且常驻减伤 15%！`

### 6. 🎲 武器大师 (WeaponMaster)
*   **效果描述**：每回合复活时随机获得一把主武器与一把副武器，并附赠全套防弹衣与头盔。
*   **实现可行性**：**完全可行**。玩家 Spawn 时检测，先剥离除匕首、电击枪外玩家现存的武器。随机从主武器池和副武器池中抽取武器并发放。
*   **配置参数**：
    *   `PrimaryPool` (主武器池)：AK47, M4A1-S, AWP, Negev, XM1014
    *   `SecondaryPool` (副武器池)：Deagle, Five-Seven, Tec-9
*   **游戏内提示语**：`你已成功觉醒 武器大师，本局免费随机赠送主副武器（{primary} + {secondary}）与全套防弹衣！`

### 7. 🗡️ 小丑背刺 (Backstab)
*   **效果描述**：从背后攻击敌人时，造成 3 倍伤害。
*   **实现可行性**：**完全可行**。在 `OnTakeDamage` 伤害预处理中，计算攻击者的视线方向向量（朝向向量）和被攻击者视线方向向量的夹角，并结合两者的坐标相对向量进行背后判定。
*   **配置参数**：
    *   `BackstabMultiplier` (背刺伤害倍率)：**3** (3 倍伤害)
    *   `BackstabAngle` (背刺判定角度范围)：**60** 度
*   **计算公式**：
    *   背刺判定条件（设 $\vec{d}_{a}$ 为攻击者朝向向量，$\vec{d}_{v}$ 为受害者朝向向量，且攻击者在受害者的后方半球）：
        $$\theta = \arccos(\vec{d}_{a} \cdot \vec{d}_{v}) \le BackstabAngle$$
    *   伤害提升：
        $$Damage_{final} = Damage_{base} \times BackstabMultiplier$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 背刺，从背后攻击敌人将造成 3 倍伤害！`

### 8. 🎯 死神斩杀 (Execute)
*   **效果描述**：攻击生命值低于 35 HP 的目标时直接触发斩杀（一击必杀）。
*   **实现可行性**：**完全可行**。在 `OnEntityTakeDamagePre` 中判定如果受害玩家当前血量低于 35 HP，直接将伤害值 `info.Damage` 修改为 `Health + 100f` 强制秒杀。
*   **配置参数**：
    *   `ExecuteThreshold` (斩杀触发线)：**35 HP**
*   **游戏内提示语**：
    *   激活技能时：`你已成功契约 死神斩杀，将无情斩杀任何低于 35 HP 的敌人！`
    *   触发斩杀时：`[死神斩杀] 目标生命值低于 35 HP，已强力斩杀！`


### 9. 坚韧不拔 (IndomitableWill)
*   **效果描述**：获得基础生命恢复，生命值越低恢复越快。
*   **实现可行性**：**完全可行**。在插件的 OnTick 循环中检测玩家的当前血量。如果当前血量低于上限，则启动生命自动恢复。回血的频率为每 1 秒结算一次，且回血的数值随生命值损失比例 $L$ 的增加而动态提升。
*   **配置参数**：
    *   `BaseRegenPerSecond` (基础每秒回血)：**1 HP**
    *   `MaxRegenBonus` (损失 100% 生命时的最大额外回血加成)：**4 HP**
*   **计算公式**：
    *   已损失生命比例：$L = \frac{MaxHealth - Health_{current}}{MaxHealth}$
    *   每秒回复结算（若当前生命值小于最大生命值）：
        $$Regen = BaseRegenPerSecond + \lfloor L \times MaxRegenBonus \rfloor$$
        $$Health_{new} = \min(MaxHealth, Health_{current} + Regen)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 坚韧不拔，获得常驻生命恢复，且血量越低恢复速度越快！`

### 10. 爆头大师 (HeadshotMaster)
*   **效果描述**：将对敌人的爆头伤害转化为自己的生命值（无上限），每回合开始生命值重置到 100。
*   **实现可行性**：**完全可行**。
    *   *爆头吸血*：在 `OnTakeDamage` 预处理中，可以通过检测伤害信息或在 `EventPlayerHurt` 事件里判定受伤部位 `hitgroup == 1`（代表头部爆头）。将该次对敌人造成的最终伤害值按比例转化为攻击者自身的生命值。此回复**不设 MaxHealth 上限**，可以直接突破最大生命上限。
    *   *回合血量重置*：在玩家复活事件 `EventPlayerSpawn` 中，将玩家的 MaxHealth 恢复重置为 100，同时将 Health 设为 100。
*   **配置参数**：
    *   `HeadshotHealRatio` (爆头伤害转化为生命恢复的比例)：**100%** (1.0)
    *   `RoundStartHealth` (回合开始生命值重置目标)：**100 HP**
*   **计算公式**：
    *   爆头回复结算（若伤害击中受害者的头部）：
        $$Health_{attacker} = Health_{attacker} + \lfloor DamageDealt \times HeadshotHealRatio \rfloor$$
    *   回合复活重置：
        $$MaxHealth_{player} = RoundStartHealth$$
        $$Health_{player} = RoundStartHealth$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 爆头大师，对敌人的爆头伤害将 100% 转化为你的生命值（无上限），每回合开始血量重置为 100！`

### 11. 质变棱彩 (PrismaticChange)
*   **效果描述**：选择后随机获得一个棱彩阶（彩色）海克斯并应用效果。
*   **实现可行性**：**完全可行**。在玩家选择此海克斯时，插件逻辑随机在 8 个彩色阶（棱彩阶）海克斯中挑选一个并立即应用，向玩家提示结果。
*   **配置参数**：
    *   `ChangeTier` (质变的目标梯队)：**彩色阶 (Prismatic Augments)**
*   **计算公式**：
    *   质变逻辑：设彩色海克斯集合为 $P$，有：
        $$Augment_{applied} = RandomElement(P)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 质变棱彩！正在为您发生质变... 恭喜获得彩色海克斯：{augment_name}！`


---

## 🔮 彩色海克斯 (Prismatic Augments)

### 1. 👻 灵魂出窍 (SoulOut)
*   **效果描述**：自身化为灵魂，肉体在原地保留（肉体可被攻击），持续 6 秒，可自由移动，期间灵魂无法造成伤害也不会收到伤害。6 秒后重置回本体（若存活获得 50 生命值，单回合限制使用 1 次）。
*   **实现可行性**：**部分可行（受引擎限制）**。
    *   *局限性说明*：CS2 引擎不支持直接复制出一个拥有完整动画和同步受击盒的静态“肉体傀儡”。
    *   *变通实现*：在触发时原地生成一个受击检测实体（或记录坐标作为伤害范围），玩家本身变为隐身、无敌且造成伤害为 0 的灵魂状态。若留在原地的肉体实体被攻击，按伤害比例同步扣除玩家本体血量。6秒结束后，将玩家传送（Teleport）回原地，销毁肉体，恢复常态并在存活时获得 50 HP 奖励。
    *   *按键与限制*：CS2 无法直接侦测物理按键“F”，需绑定在检视武器指令（`PlayerButtons.LookAtWeapon` 掩码），表现为“双击检视键(F)”触发。单回合限制使用 1 次。
*   **配置参数**：
    *   `SoulDuration` (灵魂保留时间)：**8** 秒
    *   `HealthReward` (灵魂结束后本体获得的生命值)：**50 HP**
    *   `TriggerKey` (双击触发的按键掩码)：`PlayerButtons.LookAtWeapon` (双击 F 键/检视键)
    *   `SoulLimitPerRound` (单回合使用次数限制)：**1** 次
*   **计算公式**：
    *   灵魂状态：输出伤害倍率 $= 0$，受到伤害倍率 $= 0$。
    *   肉体受击同步：$Health_{player} = \max(0, Health_{player} - Damage_{body})$
    *   结束重置生命增益（受最大血量限制）：
        $$Health_{new} = \min(MaxHealth, Health_{current} + HealthReward)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 灵魂出窍，双击 F 键触发，击杀敌人后你将化为灵魂保留 5 秒并重置，5秒后灵魂消失本体复活并获得 50 HP（单回合限制 1 次）！`

### 2. 💥 玻璃大炮 (GlassCannon)
*   **效果描述**：造成的所有伤害提升 50%，但最大生命值限制为 60 HP。
*   **实现可行性**：**完全可行**。玩家 Spawn 时将最大生命值及初始生命值强行设为 `60`。在 `OnTakeDamage` 预处理中，若攻击者拥有此海克斯，直接乘以 1.5 倍伤害系数。
*   **配置参数**：
    *   `DamageMultiplier` (伤害倍率提升)：**50%** (1.5)
    *   `MaxHealthLimit` (血量上限降低至)：**60 HP**
*   **游戏内提示语**：`你已成功注入 玻璃大炮，伤害提升 50%，但生命上限降至 60 HP！`

### 3. ⏳ 时空回溯 (Chronobreak)
*   **效果描述**：双击 E 键（使用键）回到 3 秒前的血量与位置。
*   **实现可行性**：**完全可行**。利用队列 `Queue` 在 OnTick 里实时记录玩家近 3 秒以内的所有坐标点与生命值。在玩家触发回溯时，查找队列中最古老且最接近 3 秒的一条记录，对玩家执行 `Teleport` 和血量赋值。
*   **配置参数**：
    *   `TriggerKey` (双击触发的物理按键)：**双击 E 键** (Interactive Key / `PlayerButtons.Use`)
    *   `TraceDuration` (回溯的时间窗口)：**3** 秒 
    *   `ChronobreakLimitPerRound` (单回合使用限制)：**1** 次
*   **游戏内提示语**：
    *   激活技能时：`你已成功链接 时空回溯，游戏中 双击 E 键 即可折跃回 3 秒前的位置与血量！`
    *   回溯成功时：`时空折跃成功！你已回到 3 秒前的状态！`
    *   次数用尽提示：`本回合你的时空回溯次数已用完（限 1 次）！`
    *   无历史轨迹提示：`没有找到足够的时空轨迹记录！`

### 4. 👑 我是高手 (KillerMaster)
*   **效果描述**：获得当前击杀总数量 x 后，的 (1+0.3x) 倍增伤和 (0.03x) 伤害减免。
*   **实现可行性**：**完全可行**。插件内动态维护每个玩家在当前回合的击杀数 $x$。在伤害预处理中读取并应用公式。为防止减伤达到 100% 造成无限无敌，需要设置减伤上限（建议最高 90% 减伤）。
*   **配置参数**：
    *   `DamageIncreasePerKill` (每次击杀伤害加成比例)：**0.3** (30%)
    *   `DamageReductionPerKill` (每次击杀伤害减免比例)：**0.03** (3%)
    *   `MaxDamageReduction` (伤害减免最高上限)：**0.90** (90%)
*   **计算公式**：
    *   攻击伤害加成：$Damage_{final} = Damage_{base} \times (1 + DamageIncreasePerKill \times x)$
    *   受击减伤结算（防爆头/重击秒杀）：
        $$Ratio_{def} = \min(MaxDamageReduction, DamageReductionPerKill \times x)$$
        $$Damage_{received} = Damage_{base} \times (1.0f - Ratio_{def})$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 我是高手，获得击杀数量的 x，(1+0.3x) 倍增伤和 (0.03x) 伤害减免！`

### 5. 🛸 巨人杀手 (TinySlayer)
*   **效果描述**：体型变小，获得 1.2 倍移动速度，造成 1.3 倍伤害
*   **实现可行性**：**完全可行**。在玩家 Spawn 后修改其 Pawn 属性 `m_flModelScale` 为 `0.7f`，使其视觉模型缩小。注意由于 Source2 引擎同步机制限制，物理碰撞体积（Hitbox）的改变由引擎自动计算，可能会存在微小偏差。速度与伤害在 Spawn 和 TakeDamage 中修改。
*   **配置参数**：
    *   `ModelScaleModifier` (体型缩小比例)：**0.7** (缩放至 70%)
    *   `SpeedModifier` (移动速度修正)：**1.2** (1.2 倍速)
    *   `DamageMultiplier` (伤害倍率)：**1.3** (1.3 倍伤害)
*   **计算公式**：
    *   模型缩放：$ModelScale = ModelScaleModifier$
    *   速度调整：$VelocityModifier = SpeedModifier$
    *   伤害结算：$Damage_{final} = Damage_{base} \times DamageMultiplier$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 巨人杀手，体型变小，获得 1.2 倍移动速度，造成 1.3 倍伤害！`

### 6. 🚫 回归基本功 (NoPrimary)
*   **效果描述**：不可以使用主武器 但是获得 50% 伤害提升、30% 吸血效果、10% 移速加成、免费防弹衣、免费头盔。
*   **实现可行性**：**完全可行**。在 `OnPlayerWeaponSwitch` 或 `OnTick` 中检测并移除玩家的主武器（Slot 0），阻止玩家购买或捡起主武器。在 `OnTakeDamage` 预处理中修改伤害倍数，在 `EventPlayerHurt` 中按比例给攻击者回复生命值，并应用速度修正与初始防弹衣。
*   **配置参数**：
    *   `DamageMultiplier` (伤害提升倍率)：**1.5** (50% 提升)
    *   `LifestealRatio` (吸血比例)：**0.3** (30%)
    *   `SpeedBoostModifier` (加速期间速度修正)：**1.1** (1.1 倍速)
    *   `SpawnArmor` (初始护甲值)：**100**
    *   `SpawnHelmet` (初始头盔)：**true**
*   **计算公式**：
    *   输出伤害：$Damage_{final} = Damage_{base} \times DamageMultiplier$
    *   吸血量：$Heal = Damage_{dealt} \times LifestealRatio$，新血量为：
        $$Health_{new} = \min(MaxHealth, Health_{current} + Heal)$$
    *   移速加成：$VelocityModifier = SpeedBoostModifier$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 回归基本功，不可以使用主武器 但是获得 50% 伤害提升、30% 吸血效果、10% 移速加成免费防弹衣、免费头盔！`

### 7. 🌋 歌利亚巨人 (Goliath)
*   **效果描述**：体型变大，血量提升 1.5 倍，伤害提升 20%
*   **实现可行性**：**完全可行**。与“巨人杀手”类似，修改 `m_flModelScale` 为 `1.3f` 放大视觉模型。在 Spawn 时计算并设置最大生命值，在 TakeDamage 中增伤 20%。
*   **配置参数**：
    *   `ModelScaleModifier` (体型放大比例)：**1.3** (缩放至 130%)
    *   `HealthMultiplier` (血量提升倍率)：**1.5** (1.5 倍)
    *   `DamageMultiplier` (伤害倍率)：**1.2** (1.2 倍伤害)
*   **计算公式**：
    *   模型缩放：$ModelScale = ModelScaleModifier$
    *   最大血量设定：$MaxHealth_{new} = MaxHealth_{base} \times HealthMultiplier$，$Health_{new} = MaxHealth_{new}$
    *   伤害结算：$Damage_{final} = Damage_{base} \times DamageMultiplier$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 歌利亚巨人，体型变大，血量提升 1.5 倍，伤害提升 20%！`

### 8. ☠️ 不详契约 (UnluckyContract)
*   **效果描述**：基于已损失生命值，获得伤害加成，移动速度和吸血。开火会消耗你 1HP。
*   **实现可行性**：**完全可行**。在 `OnTakeDamage` 预处理、`OnTick` 移速修正 and `OnPlayerHurt` 吸血逻辑中，实时通过当前血量占最大血量比例计算出已损失生命值比例 $L$。挂载 `weapon_fire` 事件，每次开火执行扣血，为避免自杀，扣血下限设为 1 HP。
*   **配置参数**：
    *   `MaxDamageBonus` (损失 100% 生命时的最大伤害加成)：**0.5** (50% 增伤)
    *   `MaxSpeedBonus` (损失 100% 生命时的最大移速加成)：**0.3** (30% 加速)
    *   `MaxLifestealRatio` (损失 100% 生命时的最大吸血比例)：**0.4** (40% 吸血)
    *   `SelfDamagePerShot` (每次开火消耗的生命值)：**1 HP**
*   **计算公式**：
    *   已损失生命比例：$L = \frac{MaxHealth - Health_{current}}{MaxHealth}$
    *   最终伤害：$Damage_{final} = Damage_{base} \times (1.0f + L \times MaxDamageBonus)$
    *   当前移速：$VelocityModifier = 1.0f + L \times MaxSpeedBonus$
    *   当前吸血比例：$LifestealRatio = L \times MaxLifestealRatio$
    *   开火自伤（每次触发开火且当前生命值 > 1）：
        $$Health_{new} = \max(1, Health_{current} - SelfDamagePerShot)$$
*   **游戏内提示语**：
    *   激活技能时：`你已激活 不详契约，基于已损失生命值，获得攻击加成，移动速度和吸血。开火会消耗你 1HP！`
