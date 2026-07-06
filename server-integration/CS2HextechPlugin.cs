using System;
using System.Collections.Generic;
using System.Linq;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Listeners;

namespace CS2HextechPlugin
{
    public static class HextechConfig
    {
        // 核心玩法模式：1 = 每回合重新抽取且不叠加，2 = 1/9/17回合抽取且叠加
        public static int CoreMode = 1;

        // 1. ❤️ 泰坦之躯 (Titan)
        public static int TitanMaxHealth = 200;
        public static int TitanSpawnHealth = 200;

        // 2. 🩸 吸血狂热 (Vampire)
        public static float VampireRatio = 0.3f; // 30%

        // 3. ⚡ 风行者 (Speed)
        public static float SpeedModifier = 1.3f; // 1.3倍速

        // 4. 💎 赏金猎人 (Bounty)
        public static int BountyKillReward = 2000;
        public static string BountyRefreshWeapon = "weapon_taser";

        // 5. 🌀 闪现星使 (Blink)
        public static int BlinkDoubleClickInterval = 300; // 300ms
        public static float BlinkDistance = 160f;
        public static int BlinkLimitPerRound = 2;

        // 6. 💥 玻璃大炮 (GlassCannon)
        public static float GlassCannonDamageMultiplier = 1.5f; // +50%
        public static int GlassCannonMaxHealthLimit = 60;

        // 7. 🛡️ 棘刺甲壳 (Thorns)
        public static float ThornsReflectRatio = 0.3f; // 30%

        // 8. 🎯 死神斩杀 (Execute)
        public static int ExecuteThreshold = 35; // 35 HP

        // 9. 🏃 狂暴杀戮 (KillRush)
        public static float KillRushSpeedBoostModifier = 1.5f;
        public static float KillRushBoostDuration = 5.0f; // 5秒

        // 10. ⏳ 时空回溯 (Chronobreak)
        public static float ChronobreakTraceDuration = 3.0f; // 3秒
        public static int ChronobreakLimitPerRound = 1;

        // 11. 🎖️ 重装坦克 (HeavyInfantry)
        public static float HeavyInfantryDamageReduction = 0.15f; // 15% 减伤
        public static int HeavyInfantrySpawnArmor = 100;
        public static bool HeavyInfantrySpawnHelmet = true;

        // 12. 🎲 武器大师 (WeaponMaster)
        public static readonly string[] WeaponMasterPrimaryPool = { "weapon_ak47", "weapon_m4a1_silencer", "weapon_awp", "weapon_negev", "weapon_xm1014" };
        public static readonly string[] WeaponMasterSecondaryPool = { "weapon_deagle", "weapon_fiveseven", "weapon_tec9" };

        // 13. 💣 自爆卡车 (LastStand)
        public static float LastStandExplosionRadius = 300f;
        public static int LastStandMaxExplosionDamage = 100;

        // 14. 🔥 狂徒 (Reckoner)
        public static float ReckonerRegenDelay = 10.0f;    // 连续无伤 10 秒后开始回血
        public static int ReckonerRegenPerSecond = 2;       // 每秒回复 2 HP
        public static float ReckonerRegenCap = 0.8f;       // 最多恢复至 MaxHealth 的 80%
    }

    public class PlayerStateRecord
    {
        public DateTime Time { get; set; }
        public int Health { get; set; }
        public Vector Position { get; set; } = new Vector();
    }

    public class CS2HextechPlugin : BasePlugin
    {
        public override string ModuleName => "CS2 Hextech Augment Menu (CenterHtmlMenu)";
        public override string ModuleVersion => "1.1.0";
        public override string ModuleAuthor => "Antigravity AI";
        public override string ModuleDescription => "Allows players to choose different Hextech augments in-game via a Center HTML Menu, with all 13 effects and multiple stacking modes implemented.";

        // 存储每个玩家选择的海克斯效果 (Key: SteamID, Value: HashSet of HexTypes)
        private readonly Dictionary<ulong, HashSet<string>> _playerHextechs = new();
        // 存储每个玩家本回合已使用的闪现次数 (Key: SteamID, Value: BlinkCount)
        private readonly Dictionary<ulong, int> _playerBlinksUsed = new();
        // 存储每个玩家本回合已使用的时空回溯次数 (Key: SteamID, Value: ChronobreakCount)
        private readonly Dictionary<ulong, int> _playerChronobreaksUsed = new();
        // 存储每个玩家上一帧按键状态
        private readonly Dictionary<ulong, PlayerButtons> _lastButtons = new();
        // 存储每个玩家上一次按下 E 键的时间
        private readonly Dictionary<ulong, DateTime> _lastUseKeyPressTime = new();
        // 存储每个玩家上一次选择海克斯时的回合数
        private readonly Dictionary<ulong, int> _playerLastChoiceRound = new();
        // 存储时空回溯历史记录
        private readonly Dictionary<ulong, Queue<PlayerStateRecord>> _playerHistory = new();
        // 存储狂暴杀戮加速状态结束时间
        private readonly Dictionary<ulong, DateTime> _playerSpeedBoostEndTime = new();
        // 存储每个玩家最后一次受到伤害的时间（狂徒回血计时用）
        private readonly Dictionary<ulong, DateTime> _playerLastHurtTime = new();
        // 存储每个玩家上一次狂徒回血 Tick 的时间（控制每秒频率）
        private readonly Dictionary<ulong, DateTime> _playerLastRegenTick = new();

        // 游戏是否已正式开始（结束热身）
        private bool _isGameStarted = false;
        // 存储已准备就绪的玩家 SteamID
        private readonly Dictionary<ulong, bool> _dummyToForceCorrectLineMapping = new(); // Just placeholder for layout
        private readonly HashSet<ulong> _readyPlayers = new();

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[HextechPlugin] 海克斯内战插件加载成功！所有13种技能与模式已就绪！");

            // 注册打开菜单指令，玩家在聊天框敲 !hex 或 /hex 即可触发
            AddCommand("css_hex", "打开海克斯效果选择菜单", CommandOpenHexMenu);

            // 注册准备指令
            AddCommand("css_r", "准备游戏", CommandReady);
            AddCommand("css_ready", "准备游戏", CommandReady);
            // 注册重置房间指令
            AddCommand("css_restart", "重置房间并重新开启准备", CommandRestartRoom);

            // 监听玩家复活 (Spawn) 事件，自动为其应用状态或弹出菜单
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

            // 监听玩家受伤 (Hurt) 事件，用于实现“吸血鬼”和“棘刺甲壳”效果
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

            // 监听玩家死亡 (Death) 事件，用于实现“赏金猎人”、“狂暴杀戮”和“自爆卡车”效果
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);

            // 监听回合开始事件，重置单回合限制、轨迹等
            RegisterEventHandler<EventRoundStart>(OnRoundStart);

            // 监听聊天事件拦截 .r 消息
            AddCommandListener("say", OnPlayerSay);
            AddCommandListener("say_team", OnPlayerSay);

            // 注册 OnTick 监听器以检测双击 E (Use) 按键、记录 Chronobreak 状态
            RegisterListener<Listeners.OnTick>(OnTick);

            // 注册伤害预处理监听器，实现 GlassCannon, HeavyInfantry, Execute 效果
            RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);

            // 注册 OnMapStart 监听
            RegisterListener<Listeners.OnMapStart>(OnMapStart);

            // 监听玩家退出事件，当玩家数量为0时自动重置服务器
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);

            // 注册重置命令供RCON/控制台调用并选定模式
            AddCommand("css_hex_reset", "初始化重置服务器状态与海克斯模式", CommandResetServer);

            if (hotReload)
            {
                var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
                if (gameRulesProxy != null && gameRulesProxy.GameRules != null)
                {
                    _isGameStarted = !gameRulesProxy.GameRules.WarmupPeriod;
                }
            }
        }

        private void OnMapStart(string mapName)
        {
            ResetHextechServerState(1); // 地图载入默认回到模式 1 并完全初始化
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            // 延迟 1 秒等待玩家完全移出在线列表
            AddTimer(1.0f, () =>
            {
                var humanPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot).ToList();
                if (humanPlayers.Count == 0)
                {
                    Console.WriteLine("[HextechPlugin] 检测到服务器内已无在线人类玩家，自动重置房间状态...");
                    ResetHextechServerState(1); // 自动重置并恢复默认模式 1
                }
            });
            return HookResult.Continue;
        }

        private void CommandResetServer(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player != null && !AdminManager_IsAdmin(player))
            {
                player.PrintToChat(" \x02[准备系统]\x01 你没有权限执行该指令！");
                return;
            }

            int targetMode = 1;
            if (commandInfo.ArgCount > 1)
            {
                int.TryParse(commandInfo.GetArg(1), out targetMode);
            }

            if (targetMode != 1 && targetMode != 2)
            {
                targetMode = 1;
            }

            ResetHextechServerState(targetMode);

            string msg = $" \x06[准备系统]\x01 服务器已手动重置！当前激活模式：\x04模式 {targetMode}\x01，开始热身！";
            if (player != null)
            {
                player.PrintToChat(msg);
            }
            else
            {
                Console.WriteLine($"[HextechPlugin] {msg}");
            }
        }

        private bool AdminManager_IsAdmin(CCSPlayerController player)
        {
            return player == null || !player.IsValid || player.IsBot;
        }

        private void ResetHextechServerState(int mode)
        {
            _playerHextechs.Clear();
            _playerBlinksUsed.Clear();
            _playerChronobreaksUsed.Clear();
            _playerHistory.Clear();
            _playerLastChoiceRound.Clear();
            _playerSpeedBoostEndTime.Clear();
            _playerLastHurtTime.Clear();
            _playerLastRegenTick.Clear();
            _readyPlayers.Clear();

            _isGameStarted = false;
            HextechConfig.CoreMode = mode;

            // 强制开始热身
            Server.ExecuteCommand("mp_warmuptime 9999");
            Server.ExecuteCommand("mp_warmup_start");
            Server.ExecuteCommand("mp_restartgame 1");

            Console.WriteLine($"[HextechPlugin] 服务器状态已完全初始化重置。当前海克斯模式：{mode}");
        }

        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot)
                return HookResult.Continue;

            string text = commandInfo.GetArg(1).Trim();
            
            // 准备就绪指令
            if (text.Equals(".r", StringComparison.OrdinalIgnoreCase) || 
                text.Equals(".ready", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("!r", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("!ready", StringComparison.OrdinalIgnoreCase))
            {
                HandlePlayerReady(player);
                return HookResult.Handled; // 拦截消息不让公屏刷屏
            }

            // 在准备/热身阶段，允许通过聊天框切换核心玩法模式
            if (!_isGameStarted)
            {
                int targetMode = 0;
                if (text.Equals(".mode 1", StringComparison.OrdinalIgnoreCase) || 
                    text.Equals("!mode 1", StringComparison.OrdinalIgnoreCase) ||
                    text.Equals(".m1", StringComparison.OrdinalIgnoreCase))
                {
                    targetMode = 1;
                }
                else if (text.Equals(".mode 2", StringComparison.OrdinalIgnoreCase) || 
                         text.Equals("!mode 2", StringComparison.OrdinalIgnoreCase) ||
                         text.Equals(".m2", StringComparison.OrdinalIgnoreCase))
                {
                    targetMode = 2;
                }

                if (targetMode == 1 || targetMode == 2)
                {
                    ResetHextechServerState(targetMode);
                    Server.PrintToChatAll($" \x06[准备系统]\x01 玩家 \x04{player.PlayerName}\x01 将核心玩法切换为：\x04模式 {targetMode}\x01（{(targetMode == 1 ? "每回合重新抽卡，不叠加" : "仅在 1,9,17 回合选卡，叠加生效")}）！");
                    return HookResult.Handled;
                }
            }

            return HookResult.Continue;
        }

        private void CommandReady(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            HandlePlayerReady(player);
        }

        private void CommandRestartRoom(CCSPlayerController? player, CommandInfo commandInfo)
        {
            ResetHextechServerState(HextechConfig.CoreMode);

            string executor = player == null ? "服务器控制台" : player.PlayerName;
            Server.PrintToChatAll($" \x02[准备系统]\x01 管理员 \x04{executor}\x01 重置了房间！所有已选海克斯已清除，重新开启准备阶段。");
        }

        private void HandlePlayerReady(CCSPlayerController player)
        {
            if (_isGameStarted)
            {
                player.PrintToChat(" \x02[准备系统]\x01 游戏已经开始，无需准备！");
                return;
            }

            if (_readyPlayers.Contains(player.SteamID))
            {
                player.PrintToChat(" \x06[准备系统]\x01 你已经处于准备就绪状态！");
                return;
            }

            _readyPlayers.Add(player.SteamID);

            // 获取所有非Bot的活跃玩家 (T=2, CT=3)
            var activePlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && (p.TeamNum == 2 || p.TeamNum == 3)).ToList();
            int totalNeeded = activePlayers.Count;
            // 过滤准备人员，防止玩家换队或退服后计数错误
            int readyCount = activePlayers.Count(p => _readyPlayers.Contains(p.SteamID));

            Server.PrintToChatAll($" \x06[准备系统]\x01 玩家 \x04{player.PlayerName}\x01 已准备！当前准备人数: \x04{readyCount}/{totalNeeded}\x01");

            if (readyCount >= totalNeeded && totalNeeded > 0)
            {
                _isGameStarted = true;
                _readyPlayers.Clear();
                Server.PrintToChatAll(" \x06[准备系统]\x01 所有人已准备就绪，游戏正式开始！结束热身！");
                Server.ExecuteCommand("mp_warmup_end");
            }
        }

        private bool HasHextech(CCSPlayerController player, string hexId)
        {
            if (player == null || !player.IsValid) return false;
            return _playerHextechs.TryGetValue(player.SteamID, out var hextechs) && hextechs.Contains(hexId);
        }

        private CCSPlayerController? GetPlayerFromEntity(CEntityInstance? entity)
        {
            if (entity == null || !entity.IsValid) return null;
            if (entity is CCSPlayerController controller) return controller;
            if (entity is CCSPlayerPawn pawn) return pawn.Controller.Value as CCSPlayerController;
            return null;
        }

        private int GetCurrentRound()
        {
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy != null && gameRulesProxy.GameRules != null)
            {
                if (gameRulesProxy.GameRules.WarmupPeriod) return 0;
                return gameRulesProxy.GameRules.TotalRoundsPlayed + 1;
            }
            return 1;
        }

        /// <summary>
        /// 玩家聊天输入 !hex 时的命令处理
        /// </summary>
        private void CommandOpenHexMenu(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;

            if (!_isGameStarted)
            {
                player.PrintToChat(" \x02[准备系统]\x01 游戏尚未正式开始，热身期间无法选择海克斯！");
                return;
            }
            
            // 检查当前回合是否符合选卡规则
            int currentRound = GetCurrentRound();
            if (HextechConfig.CoreMode == 2)
            {
                if (currentRound != 1 && currentRound != 9 && currentRound != 17)
                {
                    player.PrintToChat(" \x02[海克斯]\x01 当前不是海克斯选择回合（仅在第 1、9、17 回合可选）！");
                    return;
                }
            }

            ShowHextechMenu(player);
        }

        /// <summary>
        /// 玩家复活事件处理
        /// </summary>
        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            // 延迟 0.1 秒应用被动属性，并进行模式判定
            AddTimer(0.1f, () =>
            {
                if (player == null || !player.IsValid) return;

                // 如果游戏尚未正式开始，则在热身期间不发放海克斯
                if (!_isGameStarted) return;

                int currentRound = GetCurrentRound();

                // 初始化或清除
                if (!_playerHextechs.ContainsKey(player.SteamID))
                {
                    _playerHextechs[player.SteamID] = new HashSet<string>();
                }

                if (HextechConfig.CoreMode == 1)
                {
                    // 模式 1：每回合重新抽取，不叠加。所以每次复活清空历史，重新抽取
                    _playerHextechs[player.SteamID].Clear();
                    // 延迟 1.5 秒避开黑屏，弹出菜单
                    AddTimer(1.4f, () => ShowHextechMenu(player));
                }
                else if (HextechConfig.CoreMode == 2)
                {
                    // 模式 2：仅在 1, 9, 17 回合抽取。
                    // 重新应用已叠加的全部被动效果
                    ReapplyActiveHextechs(player);

                    if (currentRound == 1 || currentRound == 9 || currentRound == 17)
                    {
                        // 检查当前选卡轮次是否已选
                        _playerLastChoiceRound.TryGetValue(player.SteamID, out var lastRound);
                        if (lastRound != currentRound)
                        {
                            // 还没选，弹出菜单
                            AddTimer(1.4f, () => ShowHextechMenu(player));
                        }
                    }
                }
            });

            return HookResult.Continue;
        }

        /// <summary>
        /// 重新应用某个玩家已拥有的所有海克斯被动属性
        /// </summary>
        private void ReapplyActiveHextechs(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            // 1. 基础重置
            pawn.MaxHealth = 100;
            pawn.VelocityModifier = 1.0f;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // 2. 检查是否有泰坦之躯与玻璃大炮的生命值逻辑
            bool hasTitan = HasHextech(player, "Titan");
            bool hasGlass = HasHextech(player, "GlassCannon");

            int targetMaxHp = 100;
            if (hasTitan) targetMaxHp = HextechConfig.TitanMaxHealth; // 200
            if (hasGlass) targetMaxHp = HextechConfig.GlassCannonMaxHealthLimit; // 60 覆盖

            pawn.MaxHealth = targetMaxHp;
            pawn.Health = targetMaxHp;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            // 3. 风行者速度逻辑
            bool hasSpeed = HasHextech(player, "Speed");
            pawn.VelocityModifier = hasSpeed ? HextechConfig.SpeedModifier : 1.0f;

            // 4. 重装坦克与武器大师防弹衣逻辑
            bool hasHeavy = HasHextech(player, "HeavyInfantry");
            bool hasWeapon = HasHextech(player, "WeaponMaster");

            if (hasHeavy || hasWeapon)
            {
                player.GiveNamedItem("item_assaultsuit"); // 赠送防弹衣与头盔
            }

            // 5. 武器大师随机武器赠送逻辑
            if (hasWeapon)
            {
                ApplyWeaponMaster(player);
            }
        }

        /// <summary>
        /// 伤害发生时的回调 (Vampire, Thorns, Reckoner)
        /// </summary>
        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var victimPlayer = @event.Userid;
            var attackerPlayer = @event.Attacker;
            var dmg = @event.DmgHealth;

            // 1. Vampire (伤害转化回血)
            if (attackerPlayer != null && attackerPlayer.IsValid && !attackerPlayer.IsBot)
            {
                if (HasHextech(attackerPlayer, "Vampire"))
                {
                    var attackerPawn = attackerPlayer.PlayerPawn.Value;
                    if (attackerPawn != null && attackerPawn.IsValid && dmg > 0)
                    {
                        int healAmount = (int)(dmg * HextechConfig.VampireRatio);
                        if (healAmount > 0)
                        {
                            int newHealth = Math.Min(attackerPawn.MaxHealth, attackerPawn.Health + healAmount);
                            attackerPawn.Health = newHealth;
                            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                            attackerPlayer.PrintToChat($" \x06[海克斯·吸血]\x01 成功恢复了 \x04{healAmount} HP\x01！");
                        }
                    }
                }
            }

            // 2. Thorns (反弹受到伤害)
            if (victimPlayer != null && victimPlayer.IsValid && !victimPlayer.IsBot)
            {
                if (HasHextech(victimPlayer, "Thorns") && attackerPlayer != null && attackerPlayer.IsValid && !attackerPlayer.IsBot)
                {
                    var attackerPawn = attackerPlayer.PlayerPawn.Value;
                    if (attackerPawn != null && attackerPawn.IsValid && dmg > 0)
                    {
                        int reflectDmg = (int)(dmg * HextechConfig.ThornsReflectRatio);
                        if (reflectDmg > 0)
                        {
                            attackerPawn.Health = Math.Max(0, attackerPawn.Health - reflectDmg);
                            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                            attackerPlayer.PrintToChat($" \x06[海克斯·棘刺]\x01 你受到了来自棘刺的反弹伤害 \x02{reflectDmg}\x01 HP！");
                        }
                    }
                }

                // 3. Reckoner (狂徒：任何受到伤害都重置无伤计时器)
                if (HasHextech(victimPlayer, "Reckoner") && dmg > 0)
                {
                    _playerLastHurtTime[victimPlayer.SteamID] = DateTime.Now;
                }
            }

            return HookResult.Continue;
        }

        /// <summary>
        /// 伤害预处理回调 (GlassCannon, HeavyInfantry, Execute)
        /// </summary>
        private HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            if (entity is not CCSPlayerPawn victimPawn) return HookResult.Continue;

            var victimPlayer = victimPawn.Controller.Value as CCSPlayerController;
            var attackerEntity = info.Attacker.Value;

            bool damageChanged = false;

            // 1. HeavyInfantry (受击伤害减免 15%)
            if (victimPlayer != null && victimPlayer.IsValid && !victimPlayer.IsBot)
            {
                if (HasHextech(victimPlayer, "HeavyInfantry"))
                {
                    info.Damage *= (1.0f - HextechConfig.HeavyInfantryDamageReduction);
                    damageChanged = true;
                }
            }

            // 2. 攻击者相关效果
            var attackerPlayer = GetPlayerFromEntity(attackerEntity);
            if (attackerPlayer != null && attackerPlayer.IsValid && !attackerPlayer.IsBot)
            {
                // 玻璃大炮 (伤害提升 50%)
                if (HasHextech(attackerPlayer, "GlassCannon"))
                {
                    info.Damage *= HextechConfig.GlassCannonDamageMultiplier;
                    damageChanged = true;
                }

                // 死神斩杀 (目标低于 35 HP 强力斩杀)
                if (HasHextech(attackerPlayer, "Execute") && victimPawn.Health < HextechConfig.ExecuteThreshold)
                {
                    info.Damage = victimPawn.Health + 100.0f; // 确保秒杀
                    damageChanged = true;
                    attackerPlayer.PrintToChat(" \x06[海克斯·斩杀]\x01 [死神斩杀] 目标生命值低于 35 HP，已强力斩杀！");
                }
            }

            return damageChanged ? HookResult.Changed : HookResult.Continue;
        }

        /// <summary>
        /// 玩家死亡事件 (Bounty, KillRush, LastStand)
        /// </summary>
        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker; // 击杀者
            var victim = @event.Userid;     // 死亡者

            // 1. 击杀者奖励逻辑
            if (attacker != null && attacker.IsValid && !attacker.IsBot)
            {
                // Bounty (赏金猎人)
                if (HasHextech(attacker, "Bounty"))
                {
                    var moneyServices = attacker.InGameMoneyServices;
                    if (moneyServices != null)
                    {
                        moneyServices.Account += HextechConfig.BountyKillReward;
                    }
                    attacker.GiveNamedItem(HextechConfig.BountyRefreshWeapon);
                    attacker.PrintToChat($" \x06[海克斯·赏金]\x01 击杀成功！额外获得 \x04${HextechConfig.BountyKillReward} 现金\x01 并已刷新 \x05电击枪\x01！");
                }

                // KillRush (狂暴杀戮：击杀后加速)
                if (HasHextech(attacker, "KillRush"))
                {
                    var attackerPawn = attacker.PlayerPawn.Value;
                    if (attackerPawn != null && attackerPawn.IsValid)
                    {
                        attackerPawn.VelocityModifier = HextechConfig.KillRushSpeedBoostModifier;
                        _playerSpeedBoostEndTime[attacker.SteamID] = DateTime.Now.AddSeconds(HextechConfig.KillRushBoostDuration);

                        AddTimer(HextechConfig.KillRushBoostDuration, () =>
                        {
                            if (attacker == null || !attacker.IsValid) return;
                            var p = attacker.PlayerPawn.Value;
                            if (p == null || !p.IsValid) return;

                            // 如果在此期间没有拿到新的击杀（即结束时间没被更新），则恢复原速度
                            if (DateTime.Now >= _playerSpeedBoostEndTime.GetValueOrDefault(attacker.SteamID, DateTime.MinValue))
                            {
                                p.VelocityModifier = HasHextech(attacker, "Speed") ? HextechConfig.SpeedModifier : 1.0f;
                            }
                        });
                    }
                }
            }

            // 2. 死亡者自爆逻辑 (LastStand)
            if (victim != null && victim.IsValid && !victim.IsBot)
            {
                if (HasHextech(victim, "LastStand"))
                {
                    var victimPawn = victim.PlayerPawn.Value;
                    if (victimPawn != null && victimPawn.IsValid)
                    {
                        var origin = victimPawn.AbsOrigin;
                        if (origin != null)
                        {
                            TriggerLastStandExplosion(victim, origin);
                        }
                    }
                }
            }

            return HookResult.Continue;
        }

        /// <summary>
        /// 自爆卡车爆炸中心逻辑
        /// </summary>
        private void TriggerLastStandExplosion(CCSPlayerController victim, Vector explosionPos)
        {
            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || !target.IsValid || target.IsBot || !target.PawnIsAlive) continue;
                if (target.TeamNum == victim.TeamNum || target.TeamNum <= 1) continue; // 仅敌方

                var pawn = target.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;

                var targetPos = pawn.AbsOrigin;
                if (targetPos == null) continue;

                float dx = explosionPos.X - targetPos.X;
                float dy = explosionPos.Y - targetPos.Y;
                float dz = explosionPos.Z - targetPos.Z;
                float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist <= HextechConfig.LastStandExplosionRadius)
                {
                    // 距离衰减伤害
                    int dmg = (int)MathF.Max(0, (1.0f - (dist / HextechConfig.LastStandExplosionRadius)) * HextechConfig.LastStandMaxExplosionDamage);
                    if (dmg > 0)
                    {
                        pawn.Health = Math.Max(0, pawn.Health - dmg);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

                        victim.PrintToChat($" \x06[自爆卡车]\x01 死亡引发了爆炸，对敌方 \x02{target.PlayerName}\x01 造成了 \x02{dmg}\x01 HP 的爆炸伤害！");
                        target.PrintToChat($" \x06[自爆卡车]\x01 敌人在你身边自爆，你受到了 \x02{dmg}\x01 HP 的爆炸伤害！");
                    }
                }
            }
        }

        /// <summary>
        /// 监听回合开始，重置闪现、回溯限制
        /// </summary>
        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _playerBlinksUsed.Clear();
            _playerChronobreaksUsed.Clear();
            _playerHistory.Clear();

            // 如果游戏未开启，确保强制保持在热身状态
            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy != null && gameRulesProxy.GameRules != null && !gameRulesProxy.GameRules.WarmupPeriod && !_isGameStarted)
            {
                Server.ExecuteCommand("mp_warmuptime 9999");
                Server.ExecuteCommand("mp_warmup_start");
            }
            return HookResult.Continue;
        }

        /// <summary>
        /// 闪现控制台/公屏聊天指令
        /// </summary>
        [ConsoleCommand("css_blink", "闪现星使：瞬间向前传送 4 米")]
        public void OnBlinkCommand(CCSPlayerController? player, CommandInfo info)
        {
            if (player == null || !player.IsValid) return;
            TriggerBlink(player);
        }

        /// <summary>
        /// 闪现逻辑
        /// </summary>
        private void TriggerBlink(CCSPlayerController player)
        {
            if (!player.PawnIsAlive)
            {
                player.PrintToChat(" \x02[闪现系统]\x01 你必须存活时才能使用闪现！");
                return;
            }

            if (!HasHextech(player, "Blink"))
            {
                player.PrintToChat(" \x02[闪现系统]\x01 你本局没有选择【闪现星使】海克斯！");
                return;
            }

            _playerBlinksUsed.TryGetValue(player.SteamID, out var used);
            if (used >= HextechConfig.BlinkLimitPerRound)
            {
                player.PrintToChat($" \x02[闪现系统]\x01 本回合你的闪现次数已用完（限 {HextechConfig.BlinkLimitPerRound} 次）！");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var origin = pawn.AbsOrigin;
            var angles = pawn.EyeAngles;
            if (origin == null || angles == null) return;

            float yawRad = angles.Y * (MathF.PI / 180f);
            float forwardX = MathF.Cos(yawRad);
            float forwardY = MathF.Sin(yawRad);

            var newPosition = new Vector(
                origin.X + (forwardX * HextechConfig.BlinkDistance),
                origin.Y + (forwardY * HextechConfig.BlinkDistance),
                origin.Z + 10f // 稍微抬高避免卡死
            );

            pawn.Teleport(newPosition, null, null);
            _playerBlinksUsed[player.SteamID] = used + 1;
            player.PrintToChat($" \x06[闪现系统]\x01 闪现成功！本回合还剩 \x04{HextechConfig.BlinkLimitPerRound - (used + 1)}\x01 次。");
        }

        /// <summary>
        /// 时空回溯折跃逻辑
        /// </summary>
        private void TriggerChronobreak(CCSPlayerController player)
        {
            if (!player.PawnIsAlive)
            {
                player.PrintToChat(" \x02[时空回溯]\x01 你必须存活时才能使用时空回溯！");
                return;
            }

            if (!HasHextech(player, "Chronobreak"))
            {
                player.PrintToChat(" \x02[时空回溯]\x01 你本局没有选择【时空回溯】海克斯！");
                return;
            }

            _playerChronobreaksUsed.TryGetValue(player.SteamID, out var used);
            if (used >= HextechConfig.ChronobreakLimitPerRound)
            {
                player.PrintToChat($" \x02[时空回溯]\x01 本回合你的时空回溯次数已用完（限 {HextechConfig.ChronobreakLimitPerRound} 次）！");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            // 查找 3 秒前的状态记录
            if (!_playerHistory.TryGetValue(player.SteamID, out var history) || history.Count == 0)
            {
                player.PrintToChat(" \x02[时空回溯]\x01 没有找到足够的时空轨迹记录！");
                return;
            }

            // 检查历史记录最老的点是否足够久远（至少 2.5 秒前）
            double age = (DateTime.Now - history.Peek().Time).TotalSeconds;
            if (age < 2.5)
            {
                player.PrintToChat(" \x02[时空回溯]\x01 没有找到足够的时空轨迹记录！");
                return;
            }

            // 折跃回 3 秒前的状态
            var oldState = history.Peek();
            pawn.Teleport(oldState.Position, null, null);
            pawn.Health = oldState.Health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            _playerChronobreaksUsed[player.SteamID] = used + 1;
            player.PrintToChat(" \x06[时空回溯]\x01 时空折跃成功！你已回到 3 秒前的状态！");
        }

        /// <summary>
        /// 每 Tick 执行按键判定、时空记录等
        /// </summary>
        private void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;

                // 1. 处理 Chronobreak 轨迹记录
                if (player.PawnIsAlive && HasHextech(player, "Chronobreak"))
                {
                    var steamId = player.SteamID;
                    if (!_playerHistory.TryGetValue(steamId, out var history))
                    {
                        history = new Queue<PlayerStateRecord>();
                        _playerHistory[steamId] = history;
                    }

                    history.Enqueue(new PlayerStateRecord
                    {
                        Time = DateTime.Now,
                        Health = pawn.Health,
                        Position = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z)
                    });

                    // 剪枝 3 秒外的旧记录（保留队列头部最靠近 3 秒前的一点）
                    var cutoff = DateTime.Now.AddSeconds(-HextechConfig.ChronobreakTraceDuration);
                    while (history.Count > 1 && history.ElementAt(1).Time < cutoff)
                    {
                        history.Dequeue();
                    }
                }

                // 2. 狂徒 (Reckoner) 自动回血逻辑
                if (player.PawnIsAlive && HasHextech(player, "Reckoner"))
                {
                    _playerLastHurtTime.TryGetValue(player.SteamID, out var lastHurt);
                    double secondsSinceHurt = (DateTime.Now - lastHurt).TotalSeconds;

                    if (secondsSinceHurt >= HextechConfig.ReckonerRegenDelay)
                    {
                        _playerLastRegenTick.TryGetValue(player.SteamID, out var lastRegen);
                        double secondsSinceRegen = (DateTime.Now - lastRegen).TotalSeconds;

                        if (secondsSinceRegen >= 1.0)
                        {
                            int regenCap = (int)(pawn.MaxHealth * HextechConfig.ReckonerRegenCap);
                            if (pawn.Health < regenCap)
                            {
                                pawn.Health = Math.Min(regenCap, pawn.Health + HextechConfig.ReckonerRegenPerSecond);
                                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                            }
                            _playerLastRegenTick[player.SteamID] = DateTime.Now;
                        }
                    }
                }

                // 3. 双击 E 键按键状态判定
                var currentButtons = player.Buttons;
                _lastButtons.TryGetValue(player.SteamID, out var previousButtons);

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
        /// 按下 Use 键（E）时的双击逻辑判定
        /// </summary>
        private void OnUseKeyDown(CCSPlayerController player)
        {
            bool hasBlink = HasHextech(player, "Blink");
            bool hasChronobreak = HasHextech(player, "Chronobreak");

            if (!hasBlink && !hasChronobreak) return;
            if (!player.PawnIsAlive) return;

            var now = DateTime.Now;
            _lastUseKeyPressTime.TryGetValue(player.SteamID, out var lastPress);

            double elapsed = (now - lastPress).TotalMilliseconds;

            if (elapsed < HextechConfig.BlinkDoubleClickInterval) // 300 ms 判定为双击
            {
                // 重置时间防连按
                _lastUseKeyPressTime[player.SteamID] = DateTime.MinValue;

                if (hasBlink)
                {
                    TriggerBlink(player);
                }
                if (hasChronobreak)
                {
                    TriggerChronobreak(player);
                }
            }
            else
            {
                _lastUseKeyPressTime[player.SteamID] = now;
            }
        }

        /// <summary>
        /// 武器大师武器剥离与发放逻辑
        /// </summary>
        private void StripPlayerWeaponsExceptKnife(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.WeaponServices == null) return;

            var weapons = new List<CHandle<CBasePlayerWeapon>>(pawn.WeaponServices.MyWeapons);
            foreach (var weapon in weapons)
            {
                if (weapon == null || !weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid)
                    continue;

                string name = weapon.Value.DesignerName ?? "";
                if (name.Contains("knife") || name.Contains("bayonet") || name.Contains("melee") || name.Contains("taser"))
                    continue;

                pawn.RemovePlayerItem(weapon.Value);
                weapon.Value.Remove();
            }
        }

        private void ApplyWeaponMaster(CCSPlayerController player)
        {
            StripPlayerWeaponsExceptKnife(player);

            var rand = new Random();
            string primary = HextechConfig.WeaponMasterPrimaryPool[rand.Next(HextechConfig.WeaponMasterPrimaryPool.Length)];
            string secondary = HextechConfig.WeaponMasterSecondaryPool[rand.Next(HextechConfig.WeaponMasterSecondaryPool.Length)];

            player.GiveNamedItem(primary);
            player.GiveNamedItem(secondary);

            // 打印消息
            string primDisplay = primary.Replace("weapon_", "");
            string secDisplay = secondary.Replace("weapon_", "");
            player.PrintToChat($" \x06[武器大师]\x01 本局免费随机赠送主副武器（\x04{primDisplay}\x01 + \x04{secDisplay}\x01）与全套防弹衣！");
        }

        private void ShowHextechMenu(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var allOptions = new List<HextechOption>
            {
                new HextechOption("Titan", "❤️ 泰坦之躯 (全额 200HP)", (p) => ApplyHextechEffect(p, "Titan")),
                new HextechOption("Vampire", "🩸 吸血狂热 (造成伤害回血 30%)", (p) => ApplyHextechEffect(p, "Vampire")),
                new HextechOption("Speed", "⚡ 风行者 (速度提升 1.3 倍)", (p) => ApplyHextechEffect(p, "Speed")),
                new HextechOption("Bounty", "💎 赏金猎人 (击杀+$2000 & 刷新电击枪)", (p) => ApplyHextechEffect(p, "Bounty")),
                new HextechOption("Blink", "🌀 闪现星使 (双击 E 键闪现，每回合2次)", (p) => ApplyHextechEffect(p, "Blink")),
                new HextechOption("GlassCannon", "💥 玻璃大炮 (伤害+50% & 血量上限60)", (p) => ApplyHextechEffect(p, "GlassCannon")),
                new HextechOption("Thorns", "🛡️ 棘刺甲壳 (反弹 30% 受到伤害)", (p) => ApplyHextechEffect(p, "Thorns")),
                new HextechOption("Execute", "🎯 死神斩杀 (斩杀低于 35HP 敌人)", (p) => ApplyHextechEffect(p, "Execute")),
                new HextechOption("KillRush", "🏃 狂暴杀戮 (击杀后 5秒 提升50%移速)", (p) => ApplyHextechEffect(p, "KillRush")),
                new HextechOption("Chronobreak", "⏳ 时空回溯 (双击 E 键回溯 3秒前状态)", (p) => ApplyHextechEffect(p, "Chronobreak")),
                new HextechOption("HeavyInfantry", "🎖️ 重装坦克 (自带全套防弹衣 & 减伤15%)", (p) => ApplyHextechEffect(p, "HeavyInfantry")),
                new HextechOption("WeaponMaster", "🎲 武器大师 (每局随机主副武器 & 全套防弹衣)", (p) => ApplyHextechEffect(p, "WeaponMaster")),
                new HextechOption("LastStand", "💣 自爆卡车 (死亡拉线爆炸伤害周围敌人)", (p) => ApplyHextechEffect(p, "LastStand")),
                new HextechOption("Reckoner", "🔥 狂徒 (无伤 10 秒后每秒回复 2 HP)", (p) => ApplyHextechEffect(p, "Reckoner"))
            };

            var random = new Random();
            var selectedOptions = new List<HextechOption>();
            while (selectedOptions.Count < 3 && allOptions.Count > 0)
            {
                int index = random.Next(allOptions.Count);
                selectedOptions.Add(allOptions[index]);
                allOptions.RemoveAt(index);
            }

            var hexMenu = new CenterHtmlMenu("<font color='#a4e6ff'><b>🧬 战术实验室：请选择本局海克斯效果</b></font>", this);
            foreach (var opt in selectedOptions)
            {
                hexMenu.AddMenuOption(opt.DisplayName, (playerController, option) =>
                {
                    opt.ApplyAction(playerController);
                });
            }

            MenuManager.OpenCenterHtmlMenu(this, player, hexMenu);
        }

        private void ApplyHextechEffect(CCSPlayerController player, string hexType)
        {
            if (player == null || !player.IsValid) return;

            // 存储与选择记录
            if (!_playerHextechs.ContainsKey(player.SteamID))
            {
                _playerHextechs[player.SteamID] = new HashSet<string>();
            }

            if (HextechConfig.CoreMode == 1)
            {
                _playerHextechs[player.SteamID].Clear();
            }
            _playerHextechs[player.SteamID].Add(hexType);
            _playerLastChoiceRound[player.SteamID] = GetCurrentRound();

            // 重新计算并应用全部被动属性
            ReapplyActiveHextechs(player);

            // 输出所选海克斯提示语
            switch (hexType)
            {
                case "Titan":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功注入 \x07泰坦之躯\x01，血量已跃升至 \x04200 HP\x01！");
                    break;
                case "Vampire":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功掌握 \x02吸血狂热\x01，本局你造成的 30% 物理伤害将转化为回血！");
                    break;
                case "Speed":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功披上 \x05风行者之翼\x01，移动速度提升 \x0430%\x01！");
                    break;
                case "Bounty":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功雇佣 \x04赏金猎人\x01，每完成一次击杀将获得 \x04$2000\x01 现金并刷新 \x05电击枪\x01！");
                    break;
                case "Blink":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功注入 \x05闪现星使\x01，在游戏中 \x04双击 E 键\x01 即可向前闪现！");
                    break;
                case "GlassCannon":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功注入 \x03玻璃大炮\x01，伤害提升 \x0450%\x01，但生命上限降至 \x0260 HP\x01！");
                    break;
                case "Thorns":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功披上 \x05棘刺甲壳\x01，受到的 \x0430%\x01 伤害将直接反击给对手！");
                    break;
                case "Execute":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功契约 \x07死神斩杀\x01，将无情斩杀任何低于 \x0235 HP\x01 的敌人！");
                    break;
                case "KillRush":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功激活 \x05狂暴杀戮\x01，完成击杀后将获得 5 秒 50% 极限移速加成！");
                    break;
                case "Chronobreak":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功链接 \x05时空回溯\x01，游戏中 \x04双击 E 键\x01 即可折跃回 3 秒前的位置与血量！");
                    break;
                case "HeavyInfantry":
                    player.PrintToChat(" \x06[海克斯]\x01 你已成功武装 \x05重装坦克\x01，获得免费防弹衣与头盔，且常驻减伤 \x0415%\x01！");
                    break;
                case "WeaponMaster":
                    // 已经在 Reapply 中执行了 ApplyWeaponMaster
                    break;
                case "LastStand":
                    player.PrintToChat(" \x06[海克斯]\x01 你已装载 \x07自爆卡车\x01，在死亡的瞬间将拉线自爆，让周围敌人陪葬！");
                    break;
                case "Reckoner":
                    _playerLastHurtTime[player.SteamID] = DateTime.Now;
                    player.PrintToChat(" \x06[海克斯]\x01 你已激活 \x07狂徒\x01，\x04连续 10 秒无伤\x01 后将开始 \x04每秒回复 2 HP\x01（上限为最大血量的 \x0480%\x01）！");
                    break;
            }

            MenuManager.CloseActiveMenu(player);
        }
    }

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
