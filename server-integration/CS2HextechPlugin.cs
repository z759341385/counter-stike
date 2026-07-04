using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace CS2HextechPlugin
{
    public class CS2HextechPlugin : BasePlugin
    {
        public override string ModuleName => "CS2 Hextech Augment Menu (CenterHtmlMenu)";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Antigravity AI";
        public override string ModuleDescription => "Allows players to choose different Hextech augments in-game via a Center HTML Menu.";

        // 存储每个玩家选择的海克斯效果 (Key: SteamID, Value: HexType)
        private readonly Dictionary<ulong, string> _playerHextechs = new();
        // 存储每个玩家本回合已使用的闪现次数 (Key: SteamID, Value: BlinkCount)
        private readonly Dictionary<ulong, int> _playerBlinksUsed = new();
        // 存储每个玩家上一帧按键状态
        private readonly Dictionary<ulong, PlayerButtons> _lastButtons = new();
        // 存储每个玩家上一次按下 E 键的时间
        private readonly Dictionary<ulong, DateTime> _lastUseKeyPressTime = new();

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[HextechPlugin] 插件加载成功！");

            // 注册打开菜单指令，玩家在聊天框敲 !hex 或 /hex 即可触发
            AddCommand("css_hex", "打开海克斯效果选择菜单", CommandOpenHexMenu);

            // 监听玩家复活 (Spawn) 事件，自动为其弹出菜单
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

            // 监听玩家受伤 (Hurt) 事件，用于实现“吸血鬼”海克斯效果
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

            // 监听玩家死亡 (Death) 事件，用于实现“赏金猎人”海克斯效果
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

            // 监听回合开始事件，重置闪现次数
            RegisterEventHandler<EventRoundStart>(OnRoundStart);

            // 注册 OnTick 监听器以检测双击 E (Use) 按键
            RegisterListener<Listeners.OnTick>(OnTick);
        }

        /// <summary>
        /// 玩家聊天输入 !hex 时的命令处理
        /// </summary>
        private void CommandOpenHexMenu(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            ShowHextechMenu(player);
        }

        /// <summary>
        /// 玩家复活事件处理
        /// </summary>
        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot)
            {
                // 使用计时器延迟 1.5 秒，避开回合开局的黑屏和买枪UI冲突，以确保菜单能顺利显示在中央
                AddTimer(1.5f, () => ShowHextechMenu(player));
            }
            return HookResult.Continue;
        }

        /// <summary>
        /// 玩家受伤事件处理 (吸血鬼效果逻辑)
        /// </summary>
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var attacker = @event.Attacker; // 伤害造成者
            var dmg = @event.DmgHealth;     // 造成的伤害量

            if (attacker != null && attacker.IsValid && !attacker.IsBot)
            {
                // 判断攻击者是否拥有“吸血鬼 (Vampire)”海克斯
                if (_playerHextechs.TryGetValue(attacker.SteamID, out var hexType) && hexType == "Vampire")
                {
                    int healAmount = (int)(dmg * 0.3); // 30% 伤害转化为血量回复

                    var pawn = attacker.PlayerPawn.Value;
                    if (pawn != null && pawn.IsValid)
                    {
                        // 确保血量回复不会超过玩家最大血量上限
                        int newHealth = Math.Min(pawn.MaxHealth, pawn.Health + healAmount);
                        pawn.Health = newHealth;
                        
                        // 通知引擎实体属性改变，同步给所有客户端
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                        // 聊天框回血反馈提示
                        attacker.PrintToChat($" \x06[海克斯·吸血]\x01 成功恢复了 \x04{healAmount} HP\x01！");
                    }
                }
            }

            return HookResult.Continue;
        }

        /// <summary>
        /// 玩家死亡事件处理 (赏金猎人效果逻辑)
        /// </summary>
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker; // 击杀者
            if (attacker != null && attacker.IsValid && !attacker.IsBot)
            {
                // 判断击杀者是否拥有“赏金猎人 (Bounty)”海克斯
                if (_playerHextechs.TryGetValue(attacker.SteamID, out var hexType) && hexType == "Bounty")
                {
                    // 1. 额外奖励 $2000 现金
                    var moneyServices = attacker.InGameMoneyServices;
                    if (moneyServices != null)
                    {
                        moneyServices.Account += 2000;
                    }

                    // 2. 刷新并给予电击枪
                    attacker.GiveNamedItem("weapon_taser");

                    // 3. 聊天框反馈提示
                    attacker.PrintToChat(" \x06[海克斯·赏金]\x01 击杀成功！额外获得 \x04$2000 现金\x01 并已刷新 \x05电击枪\x01！");
                }
            }
            return HookResult.Continue;
        }

        /// <summary>
        /// 监听回合开始，重置玩家闪现次数
        /// </summary>
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _playerBlinksUsed.Clear();
            return HookResult.Continue;
        }

        /// <summary>
        /// 闪现星使控制台指令，对应聊天框 !blink
        /// </summary>
        [ConsoleCommand("css_blink", "闪现星使：瞬间向前传送 4 米")]
        public void OnBlinkCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid) return;
            TriggerBlink(player);
        }

        /// <summary>
        /// 核心闪现逻辑
        /// </summary>
        private void TriggerBlink(CCSPlayerController player)
        {
            // 检查玩家是否活着
            if (!player.PawnIsAlive)
            {
                player.PrintToChat(" \x02[闪现系统]\x01 你必须存活时才能使用闪现！");
                return;
            }

            // 检查是否拥有“闪现星使”海克斯
            if (!_playerHextechs.TryGetValue(player.SteamID, out var hexType) || hexType != "Blink")
            {
                player.PrintToChat(" \x02[闪现系统]\x01 你本局没有选择【闪现星使】海克斯！");
                return;
            }

            // 检查每回合限制使用 2 次
            _playerBlinksUsed.TryGetValue(player.SteamID, out var used);
            if (used >= 2)
            {
                player.PrintToChat(" \x02[闪现系统]\x01 本回合你的闪现次数已用完（限 2 次）！");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var origin = pawn.AbsOrigin;
            var angles = pawn.EyeAngles;
            if (origin == null || angles == null) return;

            // 将朝向视角角度转换为方向向量，并移动 160 码（约 4 米）
            float yawRad = angles.Y * (MathF.PI / 180f);
            float forwardX = MathF.Cos(yawRad);
            float forwardY = MathF.Sin(yawRad);

            var newPosition = new Vector(
                origin.X + (forwardX * 160f),
                origin.Y + (forwardY * 160f),
                origin.Z + 10f // 稍微向上抬高，防止卡在低处或地板中
            );

            // 执行传送
            pawn.Teleport(newPosition, null, null);

            // 更新并反馈次数
            _playerBlinksUsed[player.SteamID] = used + 1;
            player.PrintToChat($" \x06[闪现系统]\x01 闪现成功！本回合还剩 \x04{2 - (used + 1)}\x01 次。");
        }

        /// <summary>
        /// 每 Tick 监听按键，检测双击 E (Use) 按键
        /// </summary>
        private void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;

                var currentButtons = player.Buttons;
                _lastButtons.TryGetValue(player.SteamID, out var previousButtons);

                // 检测 E 键 (PlayerButtons.Use) 的按下瞬间 (即从 0 变为 1)
                bool isUsePressedThisTick = (currentButtons & PlayerButtons.Use) != 0;
                bool wasUsePressedLastTick = (previousButtons & PlayerButtons.Use) != 0;

                if (isUsePressedThisTick && !wasUsePressedLastTick)
                {
                    OnUseKeyDown(player);
                }

                _lastButtons[player.SteamID] = currentButtons;
            }
        }

        /// <summary>
        /// 玩家按下 E 键瞬间的事件处理
        /// </summary>
        private void OnUseKeyDown(CCSPlayerController player)
        {
            // 只有拥有 Blink 的玩家需要检测双击
            if (!_playerHextechs.TryGetValue(player.SteamID, out var hexType) || hexType != "Blink") return;
            if (!player.PawnIsAlive) return;

            var now = DateTime.Now;
            _lastUseKeyPressTime.TryGetValue(player.SteamID, out var lastPress);

            double elapsed = (now - lastPress).TotalMilliseconds;

            if (elapsed < 300.0) // 300 毫秒内判定为双击
            {
                // 重置时间，防止连续快速连按触发多次
                _lastUseKeyPressTime[player.SteamID] = DateTime.MinValue;
                TriggerBlink(player);
            }
            else
            {
                _lastUseKeyPressTime[player.SteamID] = now;
            }
        }

        private void ShowHextechMenu(CCSPlayerController player)
        {
            // 定义所有 5 个海克斯选项
            var allOptions = new List<HextechOption>
            {
                new HextechOption("Titan", "❤️ 泰坦之躯 (全额 200HP)", (p) => ApplyHextechEffect(p, "Titan")),
                new HextechOption("Vampire", "🩸 吸血狂热 (造成伤害回血 30%)", (p) => ApplyHextechEffect(p, "Vampire")),
                new HextechOption("Speed", "⚡ 风行者 (速度提升 1.3 倍)", (p) => ApplyHextechEffect(p, "Speed")),
                new HextechOption("Bounty", "💎 赏金猎人 (击杀+$2000 & 刷新电击枪)", (p) => ApplyHextechEffect(p, "Bounty")),
                new HextechOption("Blink", "🌀 闪现星使 (双击 E 键闪现，每回合2次)", (p) => ApplyHextechEffect(p, "Blink"))
            };

            // 随机挑选 3 个不同的选项
            var random = new Random();
            var selectedOptions = new List<HextechOption>();
            while (selectedOptions.Count < 3 && allOptions.Count > 0)
            {
                int index = random.Next(allOptions.Count);
                selectedOptions.Add(allOptions[index]);
                allOptions.RemoveAt(index);
            }

            // 初始化中央 HTML 菜单
            var hexMenu = new CenterHtmlMenu("<font color='#a4e6ff'><b>🧬 战术实验室：请选择本局海克斯效果</b></font>", this);

            // 将选中的 3 个海克斯加入菜单
            foreach (var opt in selectedOptions)
            {
                hexMenu.AddMenuOption(opt.DisplayName, (playerController, option) =>
                {
                    opt.ApplyAction(playerController);
                });
            }

            // 对该玩家进行展示，并在玩家选择后自动关闭
            MenuManager.OpenCenterHtmlMenu(this, player, hexMenu);
        }

        /// <summary>
        /// 应用玩家选择的海克斯效果
        /// </summary>
        private void ApplyHextechEffect(CCSPlayerController player, string hexType)
        {
            if (player == null || !player.IsValid) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            // 存储该玩家的海克斯效果配置
            _playerHextechs[player.SteamID] = hexType;

            // 1. 先清除其他海克斯带来的状态残留（如血量上限恢复正常 100）
            pawn.MaxHealth = 100;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            // 重置速度
            pawn.VelocityModifier = 1.0f;

            // 2. 根据海克斯类型应用特殊效果
            switch (hexType)
            {
                case "Titan":
                    pawn.MaxHealth = 200;
                    pawn.Health = 200;
                    // 设置属性已改变状态，让引擎向客户端同步 200 血量
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    player.PrintToChat(" \x06[海克斯系统]\x01 你已成功注入 \x07泰坦之躯\x01，血量已跃升至 \x04200 HP\x01！");
                    break;

                case "Vampire":
                    player.PrintToChat(" \x06[海克斯系统]\x01 你已成功掌握 \x02吸血狂热\x01，本局你造成的 30% 物理伤害将转化为回血！");
                    break;

                case "Speed":
                    // 修改玩家的速度修正系数
                    pawn.VelocityModifier = 1.3f;
                    player.PrintToChat(" \x06[海克斯系统]\x01 你已成功披上 \x05风行者之翼\x01，移动速度提升 \x0430%\x01！");
                    break;

                case "Bounty":
                    player.PrintToChat(" \x06[海克斯系统]\x01 你已成功雇佣 \x04赏金猎人\x01，每完成一次击杀将获得 \x04$2000\x01 现金并刷新 \x05电击枪\x01！");
                    break;

                case "Blink":
                    player.PrintToChat(" \x06[海克斯系统]\x01 你已成功注入 \x05闪现星使\x01，在游戏中 \x04双击 E 键\x01 即可向前闪现！");
                    break;
            }

            // 关闭当前菜单
            MenuManager.CloseActiveMenu(player);
        }
    }

    /// <summary>
    /// 海克斯选项辅助类
    /// </summary>
    public class HextechOption
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public Action<CCSPlayerController> ApplyAction { get; set; }

        public HextechOption(string id, string displayName, Action<CCSPlayerController> applyAction)
        {
            Id = id;
            DisplayName = displayName;
            ApplyAction = applyAction;
        }
    }
}
