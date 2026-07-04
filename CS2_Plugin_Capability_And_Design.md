# CS2 插件开发能力概览与内战玩法设计指南

本文档整理了基于 **CounterStrikeSharp (CSS)** 框架开发 CS2 专用服务器插件时，能够修改和控制的游戏维度，并针对“内战随机规则抽卡系统”设计了一套从“手动自觉遵守”到“插件自动控制”的完整技术方案。

---

## 一、 CounterStrikeSharp 核心开发能力

使用 C# 编写 CSS 插件时，你可以直接操作 **Source 2 引擎的 SDK** 以及游戏内的实体（Entities）。主要可以控制以下五个维度的功能：

### 1. 玩家属性与状态控制 (Player Attributes)
*   **血量与护甲**：实时修改玩家当前血量（Health）、最大血量上限（MaxHealth）、护甲值（Armor）、是否携带头盔（HasHelmet）。
*   **移动速度**：通过修改玩家控制体（Pawn）的 `VelocityModifier` 或修改重力参数，实现加速（Speed Run）、慢速、低重力漂浮。
*   **金钱管理**：随时读取、增加、扣除、重置或冻结玩家的现金（`InGameMoneyServices`）。
*   **小地图与视觉渲染**：修改玩家模型的颜色（Color/Tint）、透明度（Alpha），实现半隐身、发光（Glow）或变色标记。
*   **生命夺取/回复**：监听伤害事件，实现吸血（Vampire）或持续回血（Regen）。

### 2. 武器、购买与装备限制 (Weapons & Buying)
*   **拦截武器购买**：通过 Hook 控制台 `buy` 指令或使用 CSS 的购买监听器，禁止玩家在开局购买特定枪支或投掷物。
*   **装备强行剥夺**：在玩家生成（Spawn）或回合开始时，剥夺（Strip）其身上的所有武器，或强行给予指定装备（如只给电击枪或沙鹰）。
*   **防止捡拾**：监听 `EventItemPickup`，当玩家捡起非本局允许的武器时，插件可自动强制其丢弃（Drop）或直接销毁武器实体。
*   **弹药逻辑**：实现无限备弹、无限弹夹，或开火不消耗子弹。

### 3. 操作与按键拦截 (Input & Cmd Hooks)
*   **按键监听与封禁**：通过 Hook 玩家的底层输入命令 `OnPlayerRunCmd`，检查其按下的键位（如 `PlayerButtons.Jump`、`PlayerButtons.Walk`、`PlayerButtons.Attack`）。
    *   *例如：若检测到按下了 Shift（走静步）或空格（跳跃），可以强制清空该按键输入，使其无法做出该动作，或触发惩罚机制（如扣血、打耳光）。*
*   **输入重定向**：在底层直接反转或修改玩家的移动向量 `cmd.Move`。
    *   *例如：直接修改底层的移动矢量，实现“W/S 键反向”、“A/D 键反向”，无需修改玩家客户端的本地 `bind` 绑定，体验极佳。*

### 4. 游戏规则与回合流程管理 (Game Rules)
*   **强行干预回合**：在特定条件下，直接调用 `CCSGameRules` 接口强制结束回合、宣判某一方（CT 或 T）获胜，或平局。
*   **时间与倒计时**：动态修改炸弹（C4）引爆倒计时、拆弹时间、局中回合时间、购买时间限制等。
*   **服务器参数（CVars）**：无需管理员手动输入，插件可根据游戏进程自动修改 `sv_gravity`、`sv_infinite_ammo`、`mp_damage_headshot_only` 等参数。

### 5. 交互界面与视觉反馈 (UI & Visual Feedback)
*   **HTML 中央菜单**：在玩家屏幕正中央弹出 HTML 格式的多选菜单（`CenterHtmlMenu`），用于技能选择、投票等。
*   **聊天框彩色文字**：支持利用颜色代码向聊天框发送排版精美的彩色广播。
*   **全屏警告与提示**：向玩家发送屏幕正中 Alert 提示、左侧教官提示（Instructor Hint）、HUD 提示条等。

---

## 二、 规则自动化升级设计（原“手动规则”的插件落地）

将 [CS_Inhouse_Rules.md](file:///c:/Users/Administrator/Documents/GitHub/counter-stike/CS_Inhouse_Rules.md) 中的手动规则用 CSS 插件自动强制执行，可极大提升游戏体验，避免玩家因“不自觉”而违规：

### 1. 武器限制类（自动执行）

| 规则名称 | 手动版要求 | 插件自动化实现方案 (C# 代码逻辑) |
| :--- | :--- | :--- |
| **西部牛仔** | 只能用 R8 或沙鹰 | 1. 监听 `EventPlayerSpawn`：清除玩家所有武器，给予沙鹰/左轮。<br>2. Hook `buy` 命令：直接返回拦截并弹窗提示 `本局禁止购买此武器`。<br>3. 监听 `EventItemPickup`：如果捡起步枪/冲锋枪，自动 `Drop` 或销毁。 |
| **喷子集会** | 只能买霰弹枪 | 1. 监听 `buy`：如果购买的不是 `nova` / `xm1014` / `sawedoff` / `mag7`，拒绝交易。<br>2. 限制捡枪，仅允许捡起霰弹枪。 |
| **电击派对** | 每人必须买电击枪 | 1. 回合开始自动给每人发一把电击枪。<br>2. 监测电击枪击杀事件，在聊天框发送华丽的广播。 |

### 2. 操作限制类（强行干预）

| 规则名称 | 手动版要求 | 插件自动化实现方案 (C# 代码逻辑) |
| :--- | :--- | :--- |
| **静音突击** | 禁止按 Shift 走静步 | 1. Hook `OnPlayerRunCmd`。<br>2. 判断 `if (cmd.Buttons.HasFlag(PlayerButtons.Walk))`。<br>3. **惩罚执行**：清除该按键状态 `cmd.Buttons &= ~PlayerButtons.Walk`，或者直接每秒扣除玩家 5 HP，并播放受罚音效。 |
| **帕金森模式** | 射击时必须快速蹲起 | 1. Hook `OnPlayerRunCmd`。<br>2. 如果玩家正在开火（`cmd.Buttons.HasFlag(PlayerButtons.Attack)`），但状态不处于蹲下（`!cmd.Buttons.HasFlag(PlayerButtons.Duck)`），则强制在其弹道上施加巨大偏差，或直接扣血惩罚。 |
| **反向驾驶** | W键与S键互换 | 1. Hook `OnPlayerRunCmd`。<br>2. 在底层代码中将移动向量进行反转：`cmd.Move = new Vector3(-cmd.Move.X, -cmd.Move.Y, cmd.Move.Z)`。不需要玩家输入任何控制台指令，即时生效！ |

---

## 三、 新增：C# 专属“海克斯科技卡牌”深度玩法设计

有了插件后，我们可以设计一些**纯手动无法实现**的高级技能卡牌，让 10 人内战充满趣味性：

### 1. 战术强化卡 (Tactic Buffs)

#### 🧬 【卡牌：泰坦之躯】 (已在插件中实现)
*   **效果**：玩家 HP 上限提升至 `200`，且出生时自带 `200` 满血。
*   **代码思路**：在玩家 Spawn 后延迟 0.1 秒，将 `player.PlayerPawn.Value.MaxHealth` 和 `Health` 设为 `200`，并调用 `Utilities.SetStateChanged` 同步网络属性。

#### 🩸 【卡牌：吸血狂热】 (已在插件中实现)
*   **效果**：本局内你对敌人造成的任何伤害，30% 将转化为自身血量回复。
*   **代码思路**：监听 `EventPlayerHurt`。当攻击者是该玩家时，计算 `dmg * 0.3`。若有伤害，则将攻击者的当前生命值增加对应点数（不超过最大生命值上限）。

#### ⚡ 【卡牌：风行者】 (已在插件中实现)
*   **效果**：移动速度提升 1.3 倍。
*   **代码思路**：设置 `player.PlayerPawn.Value.VelocityModifier = 1.3f`。

---

### 2. 趣味整蛊与超能力卡 (Fun & Superpower Buffs) - 推荐后续开发

#### 💥 【卡牌：自爆卡车】
*   **效果**：当玩家阵亡时，其尸体处会立刻产生一次巨大的爆炸，对周围的所有玩家（不论敌友）造成 80 点范围伤害。
*   **代码思路**：监听 `EventPlayerDeath`。获取阵亡玩家的坐标 `Vector`，在当前位置创建一个 `env_explosion` 实体并触发其 `Explode` 输入。

#### 🎯 【卡牌：锁头狂热】
*   **效果**：本局全员开启“仅限爆头”模式。子弹打在身体和四肢上伤害为 0，只有爆头才能造成伤害。
*   **代码思路**：监听 `EventPlayerHurt`。在事件参数中检查击中部位 `hitgroup`。如果不是 `HitGroup.Head`，则立刻将伤害结算值和扣血值覆盖为 0，或直接把扣掉的血加回去。

#### 💨 【卡牌：闪现星使】
*   **效果**：玩家在聊天框输入 `.blink` 或是双击 E 键，能瞬间向前传送 4 米，每回合限用 2 次。
*   **代码思路**：在 `AddCommand` 中注册 `.blink` 指令。获取玩家当前视角朝向的单位向量，将玩家 `Pawn` 实体的坐标位置 `Position` 增加 `朝向向量 * 150`。

#### 💎 【卡牌：赏金猎人】
*   **效果**：玩家每完成一次击杀，都可以立刻获得 +$2000 的额外现金奖励，并刷新电击枪。
*   **代码思路**：监听 `EventPlayerDeath`。当击杀者是该玩家时，获取其 `InGameMoneyServices.Account`，并直接增加 2000，同时调用 `GiveNamedItem("weapon_taser")`。
