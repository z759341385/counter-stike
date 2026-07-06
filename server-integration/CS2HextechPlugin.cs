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

        // --- 🥈 银色 (Silver) ---
        public static float SpeedModifier = 1.3f; // ⚡ 风行者
        public static int BountyKillReward = 2000; // 💎 赏金猎人
        public static string BountyRefreshWeapon = "weapon_taser";
        public static float ThornsReflectRatio = 0.3f; // 🛡️ 棘刺甲壳
        public static float ReckonerRegenDelay = 10.0f; // 🔥 狂徒
        public static int ReckonerRegenPerSecond = 2;
        public static float ReckonerRegenCap = 0.8f;
        public static int BleedDamage = 2; // 🔥 流血
        public static float BleedDuration = 5.0f;
        public static float FlashMagicDisableFireDuration = 3.0f; // 🪄 精怪魔法
        public static float GrenadeMasterDamageMultiplier = 1.5f; // 💣 道具大师
        public static int GrenadeMasterMaxHegrenadeCount = 2;
        public static int GrenadeMasterMaxMolotovCount = 2;
        public static float LastStandExplosionRadius = 300f; // 💣 自爆卡车
        public static int LastStandMaxExplosionDamage = 100;
        // 质变黄金 (GoldenChange) 无单独配置参数

        // --- 🥇 金色 (Gold) ---
        public static int TitanMaxHealth = 200; // ❤️ 泰坦之躯
        public static int TitanSpawnHealth = 200;
        public static float VampireRatio = 0.3f; // 🩸 吸血狂热
        public static int BlinkDoubleClickInterval = 300; // 🌀 闪现星使
        public static float BlinkDistance = 160f;
        public static int BlinkLimitPerRound = 2;
        public static float KillRushSpeedBoostModifier = 1.3f; // 🏃 狂暴杀戮
        public static float KillRushBoostDuration = 3.0f;
        public static float HeavyInfantryDamageReduction = 0.15f; // 🎖️ 重装坦克
        public static int HeavyInfantrySpawnArmor = 100;
        public static bool HeavyInfantrySpawnHelmet = true;
        public static readonly string[] WeaponMasterPrimaryPool = { "weapon_ak47", "weapon_m4a1_silencer", "weapon_awp", "weapon_negev", "weapon_xm1014" };
        public static readonly string[] WeaponMasterSecondaryPool = { "weapon_deagle", "weapon_fiveseven", "weapon_tec9" };
        public static float BackstabMultiplier = 3.0f; // 🗡️ 小丑背刺
        public static float BackstabAngle = 60.0f;
        public static int ExecuteThreshold = 35; // 🎯 死神斩杀
        public static int IndomitableWillBaseRegen = 1; // 坚韧不拔
        public static int IndomitableWillMaxRegenBonus = 4;
        public static float HeadshotMasterHealRatio = 1.0f; // 爆头大师
        public static int HeadshotMasterRoundStartHealth = 100;
        // 质变棱彩 (PrismaticChange) 无单独配置参数

        // --- 🔮 彩色 (Prismatic) ---
        public static float SoulOutDuration = 8.0f; // 👻 灵魂出窍
        public static int SoulOutHealthReward = 50;
        public static int SoulOutLimitPerRound = 1;
        public static float GlassCannonDamageMultiplier = 1.5f; // 💥 玻璃大炮
        public static int GlassCannonMaxHealthLimit = 60;
        public static float ChronobreakTraceDuration = 3.0f; // ⏳ 时空回溯
        public static int ChronobreakLimitPerRound = 1;
        public static float KillerMasterDamageIncreasePerKill = 0.3f; // 👑 我是高手
        public static float KillerMasterDamageReductionPerKill = 0.03f;
        public static float KillerMasterMaxDamageReduction = 0.90f;
        public static float TinySlayerModelScale = 0.7f; // 🛸 巨人杀手
        public static float TinySlayerSpeedModifier = 1.2f;
        public static float TinySlayerDamageMultiplier = 1.3f;
        public static float NoPrimaryDamageMultiplier = 1.5f; // 🚫 回归基本功
        public static float NoPrimaryLifestealRatio = 0.3f;
        public static float NoPrimarySpeedBoostModifier = 1.1f;
        public static int NoPrimarySpawnArmor = 100;
        public static bool NoPrimarySpawnHelmet = true;
        public static float GoliathModelScale = 1.3f; // 🌋 歌利亚巨人
        public static float GoliathHealthMultiplier = 1.5f;
        public static float GoliathDamageMultiplier = 1.2f;
        public static float UnluckyContractMaxDamageBonus = 0.5f; // ☠️ 不详契约
        public static float UnluckyContractMaxSpeedBonus = 0.3f;
        public static float UnluckyContractMaxLifestealRatio = 0.4f;
        public static int UnluckyContractSelfDamagePerShot = 1;
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
        public override string ModuleVersion => "2.0.0";
        public override string ModuleAuthor => "Antigravity AI";
        public override string ModuleDescription => "Hextech plugin with 28 modes and features";

        private readonly Dictionary<ulong, HashSet<string>> _playerHextechs = new();
        
        // Use/F key states
        private readonly Dictionary<ulong, PlayerButtons> _lastButtons = new();
        private readonly Dictionary<ulong, DateTime> _lastUseKeyPressTime = new();
        private readonly Dictionary<ulong, DateTime> _lastLookKeyPressTime = new();

        // Specific Ability Trackers
        private readonly Dictionary<ulong, int> _playerBlinksUsed = new();
        private readonly Dictionary<ulong, int> _playerChronobreaksUsed = new();
        private readonly Dictionary<ulong, int> _playerRoundKills = new();
        private readonly Dictionary<ulong, Queue<PlayerStateRecord>> _playerHistory = new();
        private readonly Dictionary<ulong, DateTime> _playerSpeedBoostEndTime = new();
        
        // Regen & Bleed
        private readonly Dictionary<ulong, DateTime> _playerLastHurtTime = new();
        private readonly Dictionary<ulong, DateTime> _playerLastRegenTick = new();
        private readonly Dictionary<ulong, DateTime> _playerBleedEndTime = new();
        private readonly Dictionary<ulong, DateTime> _playerDisableFireEndTime = new();
        private readonly Dictionary<ulong, DateTime> _playerLastBleedTick = new();
        private readonly Dictionary<ulong, DateTime> _playerLastIndomitableTick = new();
        
        // Grenades
        private readonly Dictionary<ulong, int> _playerHeThrown = new();
        private readonly Dictionary<ulong, int> _playerMolotovThrown = new();

        // Soul Out
        private readonly Dictionary<ulong, bool> _playerIsSoul = new();
        private readonly Dictionary<ulong, int> _playerSoulUsed = new();
        private readonly Dictionary<ulong, Vector> _playerSoulBodyPos = new();
        private readonly Dictionary<ulong, DateTime> _playerSoulEndTime = new();

        private readonly Dictionary<ulong, int> _playerLastChoiceRound = new();
        private bool _isGameStarted = false;
        private readonly HashSet<ulong> _readyPlayers = new();

        private readonly Random _random = new Random();

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[HextechPlugin] 28个海克斯能力插件加载成功！");

            AddCommand("css_hex", "打开海克斯效果选择菜单", CommandOpenHexMenu);
            AddCommand("css_r", "准备游戏", CommandReady);
            AddCommand("css_ready", "准备游戏", CommandReady);
            AddCommand("css_restart", "重置房间并重新开启准备", CommandRestartRoom);
            AddCommand("css_hex_reset", "初始化重置服务器状态与海克斯模式", CommandResetServer);

            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
            RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
            RegisterEventHandler<EventRoundStart>(OnRoundStart);
            RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
            RegisterEventHandler<EventPlayerBlind>(OnPlayerBlind);
            RegisterEventHandler<EventWeaponFire>(OnWeaponFire);
            RegisterEventHandler<EventGrenadeThrown>(OnGrenadeThrown);

            AddCommandListener("say", OnPlayerSay);
            AddCommandListener("say_team", OnPlayerSay);

            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterListener<Listeners.OnEntityTakeDamagePre>(OnTakeDamage);
            RegisterListener<Listeners.OnMapStart>(OnMapStart);

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
            ResetHextechServerState(1); 
        }

        private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            AddTimer(1.0f, () =>
            {
                var humanPlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot).ToList();
                if (humanPlayers.Count == 0)
                {
                    ResetHextechServerState(1); 
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
            if (commandInfo.ArgCount > 1) int.TryParse(commandInfo.GetArg(1), out targetMode);
            if (targetMode != 1 && targetMode != 2) targetMode = 1;

            ResetHextechServerState(targetMode);

            string msg = $" \x06[准备系统]\x01 服务器已手动重置！当前激活模式：\x04模式 {targetMode}\x01，开始热身！";
            if (player != null) player.PrintToChat(msg);
            else Console.WriteLine($"[HextechPlugin] {msg}");
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
            _playerRoundKills.Clear();
            _playerHistory.Clear();
            _playerLastChoiceRound.Clear();
            _playerSpeedBoostEndTime.Clear();
            _playerLastHurtTime.Clear();
            _playerLastRegenTick.Clear();
            _playerBleedEndTime.Clear();
            _playerDisableFireEndTime.Clear();
            _playerHeThrown.Clear();
            _playerMolotovThrown.Clear();
            _playerIsSoul.Clear();
            _playerSoulUsed.Clear();
            _readyPlayers.Clear();

            _isGameStarted = false;
            HextechConfig.CoreMode = mode;

            Server.ExecuteCommand("mp_warmuptime 9999");
            Server.ExecuteCommand("mp_warmup_start");
            Server.ExecuteCommand("mp_restartgame 1");
        }

        private HookResult OnPlayerSay(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;
            string text = commandInfo.GetArg(1).Trim();
            
            if (text.Equals(".r", StringComparison.OrdinalIgnoreCase) || text.Equals(".ready", StringComparison.OrdinalIgnoreCase) ||
                text.Equals("!r", StringComparison.OrdinalIgnoreCase) || text.Equals("!ready", StringComparison.OrdinalIgnoreCase))
            {
                HandlePlayerReady(player);
                return HookResult.Handled;
            }

            if (!_isGameStarted)
            {
                int targetMode = 0;
                if (text.Equals(".mode 1", StringComparison.OrdinalIgnoreCase) || text.Equals("!mode 1", StringComparison.OrdinalIgnoreCase) || text.Equals(".m1", StringComparison.OrdinalIgnoreCase)) targetMode = 1;
                else if (text.Equals(".mode 2", StringComparison.OrdinalIgnoreCase) || text.Equals("!mode 2", StringComparison.OrdinalIgnoreCase) || text.Equals(".m2", StringComparison.OrdinalIgnoreCase)) targetMode = 2;

                if (targetMode == 1 || targetMode == 2)
                {
                    ResetHextechServerState(targetMode);
                    Server.PrintToChatAll($" \x06[准备系统]\x01 玩家 \x04{player.PlayerName}\x01 将核心玩法切换为：\x04模式 {targetMode}\x01！");
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

            if (_readyPlayers.Contains(player.SteamID)) return;
            _readyPlayers.Add(player.SteamID);

            var activePlayers = Utilities.GetPlayers().Where(p => p != null && p.IsValid && !p.IsBot && (p.TeamNum == 2 || p.TeamNum == 3)).ToList();
            int totalNeeded = activePlayers.Count;
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

        private void CommandOpenHexMenu(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid || player.IsBot) return;
            if (!_isGameStarted)
            {
                player.PrintToChat(" \x02[准备系统]\x01 游戏尚未正式开始，热身期间无法选择海克斯！");
                return;
            }
            
            int currentRound = GetCurrentRound();
            if (HextechConfig.CoreMode == 2 && currentRound != 1 && currentRound != 9 && currentRound != 17)
            {
                player.PrintToChat(" \x02[海克斯]\x01 当前不是海克斯选择回合（仅在第 1、9、17 回合可选）！");
                return;
            }
            ShowHextechMenu(player);
        }

        private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot) return HookResult.Continue;

            // HeadshotMaster round reset
            if (HasHextech(player, "HeadshotMaster"))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.MaxHealth = HextechConfig.HeadshotMasterRoundStartHealth;
                    pawn.Health = HextechConfig.HeadshotMasterRoundStartHealth;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }

            _playerIsSoul[player.SteamID] = false; // reset soul out
            _playerBleedEndTime[player.SteamID] = DateTime.MinValue; // reset bleed
            _playerDisableFireEndTime[player.SteamID] = DateTime.MinValue; // reset blind

            AddTimer(0.1f, () =>
            {
                if (player == null || !player.IsValid) return;
                if (!_isGameStarted) return;

                int currentRound = GetCurrentRound();
                if (!_playerHextechs.ContainsKey(player.SteamID)) _playerHextechs[player.SteamID] = new HashSet<string>();

                if (HextechConfig.CoreMode == 1)
                {
                    _playerHextechs[player.SteamID].Clear();
                    AddTimer(1.4f, () => ShowHextechMenu(player));
                }
                else if (HextechConfig.CoreMode == 2)
                {
                    ReapplyActiveHextechs(player);
                    if (currentRound == 1 || currentRound == 9 || currentRound == 17)
                    {
                        _playerLastChoiceRound.TryGetValue(player.SteamID, out var lastRound);
                        if (lastRound != currentRound) AddTimer(1.4f, () => ShowHextechMenu(player));
                    }
                }
            });

            return HookResult.Continue;
        }

        private void ReapplyActiveHextechs(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            pawn.MaxHealth = 100;
            pawn.VelocityModifier = 1.0f;
            pawn.m_flModelScale = 1.0f; 

            bool hasTitan = HasHextech(player, "Titan");
            bool hasGlass = HasHextech(player, "GlassCannon");
            bool hasGoliath = HasHextech(player, "Goliath");

            int targetMaxHp = 100;
            if (hasTitan) targetMaxHp = HextechConfig.TitanMaxHealth; 
            if (hasGlass) targetMaxHp = HextechConfig.GlassCannonMaxHealthLimit; 
            if (hasGoliath) targetMaxHp = (int)(targetMaxHp * HextechConfig.GoliathHealthMultiplier);

            pawn.MaxHealth = targetMaxHp;
            pawn.Health = targetMaxHp;
            
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            float speedMod = 1.0f;
            if (HasHextech(player, "Speed")) speedMod = Math.Max(speedMod, HextechConfig.SpeedModifier);
            if (HasHextech(player, "TinySlayer")) speedMod = Math.Max(speedMod, HextechConfig.TinySlayerSpeedModifier);
            if (HasHextech(player, "NoPrimary")) speedMod = Math.Max(speedMod, HextechConfig.NoPrimarySpeedBoostModifier);
            pawn.VelocityModifier = speedMod;

            if (HasHextech(player, "TinySlayer")) pawn.m_flModelScale = HextechConfig.TinySlayerModelScale;
            if (HasHextech(player, "Goliath")) pawn.m_flModelScale = HextechConfig.GoliathModelScale;

            if (HasHextech(player, "HeavyInfantry") || HasHextech(player, "WeaponMaster") || HasHextech(player, "NoPrimary"))
            {
                player.GiveNamedItem("item_assaultsuit");
            }

            if (HasHextech(player, "WeaponMaster")) ApplyWeaponMaster(player);
        }

        private HookResult OnWeaponFire(EventWeaponFire @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && player.PawnIsAlive && !player.IsBot && HasHextech(player, "UnluckyContract"))
            {
                var pawn = player.PlayerPawn.Value;
                if (pawn != null && pawn.Health > HextechConfig.UnluckyContractSelfDamagePerShot) // keep at least 1 HP
                {
                    pawn.Health -= HextechConfig.UnluckyContractSelfDamagePerShot;
                    Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnGrenadeThrown(EventGrenadeThrown @event, GameEventInfo info)
        {
            var player = @event.Userid;
            if (player != null && player.IsValid && !player.IsBot && HasHextech(player, "GrenadeMaster"))
            {
                string weapon = @event.Weapon;
                if (weapon.Contains("hegrenade"))
                {
                    _playerHeThrown.TryGetValue(player.SteamID, out int heCount);
                    if (heCount < HextechConfig.GrenadeMasterMaxHegrenadeCount - 1)
                    {
                        AddTimer(0.1f, () => player.GiveNamedItem("weapon_hegrenade"));
                        _playerHeThrown[player.SteamID] = heCount + 1;
                    }
                }
                else if (weapon.Contains("molotov") || weapon.Contains("incgrenade"))
                {
                    _playerMolotovThrown.TryGetValue(player.SteamID, out int molCount);
                    if (molCount < HextechConfig.GrenadeMasterMaxMolotovCount - 1)
                    {
                        AddTimer(0.1f, () => player.GiveNamedItem(weapon)); // give back exact item thrown
                        _playerMolotovThrown[player.SteamID] = molCount + 1;
                    }
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerBlind(EventPlayerBlind @event, GameEventInfo info)
        {
            var victim = @event.Userid;
            var attacker = @event.Attacker;
            if (attacker != null && attacker.IsValid && !attacker.IsBot && HasHextech(attacker, "FlashMagic"))
            {
                if (victim != null && victim.IsValid)
                {
                    _playerDisableFireEndTime[victim.SteamID] = DateTime.Now.AddSeconds(HextechConfig.FlashMagicDisableFireDuration);
                }
            }
            return HookResult.Continue;
        }

        private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
        {
            var victimPlayer = @event.Userid;
            var attackerPlayer = @event.Attacker;
            var dmg = @event.DmgHealth;

            if (attackerPlayer != null && attackerPlayer.IsValid && !attackerPlayer.IsBot)
            {
                // Vampire & NoPrimary & UnluckyContract Lifesteal
                float totalLifesteal = 0;
                if (HasHextech(attackerPlayer, "Vampire")) totalLifesteal += HextechConfig.VampireRatio;
                if (HasHextech(attackerPlayer, "NoPrimary")) totalLifesteal += HextechConfig.NoPrimaryLifestealRatio;
                if (HasHextech(attackerPlayer, "UnluckyContract"))
                {
                    var p = attackerPlayer.PlayerPawn.Value;
                    if (p != null)
                    {
                        float loss = (p.MaxHealth - p.Health) / (float)p.MaxHealth;
                        if (loss < 0) loss = 0;
                        totalLifesteal += loss * HextechConfig.UnluckyContractMaxLifestealRatio;
                    }
                }

                if (totalLifesteal > 0 && dmg > 0)
                {
                    var attackerPawn = attackerPlayer.PlayerPawn.Value;
                    if (attackerPawn != null && attackerPawn.IsValid)
                    {
                        int healAmount = (int)(dmg * totalLifesteal);
                        if (healAmount > 0)
                        {
                            int newHealth = Math.Min(attackerPawn.MaxHealth, attackerPawn.Health + healAmount);
                            attackerPawn.Health = newHealth;
                            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                        }
                    }
                }
                
                // HeadshotMaster Heal
                if (HasHextech(attackerPlayer, "HeadshotMaster") && @event.Hitgroup == 1) // 1 = Head
                {
                    var attackerPawn = attackerPlayer.PlayerPawn.Value;
                    if (attackerPawn != null && attackerPawn.IsValid)
                    {
                        int healAmount = (int)(dmg * HextechConfig.HeadshotMasterHealRatio);
                        if (healAmount > 0)
                        {
                            attackerPawn.Health += healAmount; // No max cap!
                            Utilities.SetStateChanged(attackerPawn, "CBaseEntity", "m_iHealth");
                        }
                    }
                }

                // Bleed Apply
                if (HasHextech(attackerPlayer, "Bleed") && victimPlayer != null && victimPlayer.IsValid)
                {
                    _playerBleedEndTime[victimPlayer.SteamID] = DateTime.Now.AddSeconds(HextechConfig.BleedDuration);
                }
            }

            if (victimPlayer != null && victimPlayer.IsValid && !victimPlayer.IsBot)
            {
                // Thorns
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
                        }
                    }
                }

                // Reckoner Reset
                if (HasHextech(victimPlayer, "Reckoner") && dmg > 0)
                {
                    _playerLastHurtTime[victimPlayer.SteamID] = DateTime.Now;
                }
            }

            return HookResult.Continue;
        }

        private HookResult OnTakeDamage(CEntityInstance entity, CTakeDamageInfo info)
        {
            if (entity == null || !entity.IsValid) return HookResult.Continue;
            if (entity is not CCSPlayerPawn victimPawn) return HookResult.Continue;

            var victimPlayer = victimPawn.Controller.Value as CCSPlayerController;
            var attackerEntity = info.Attacker.Value;
            var attackerPlayer = GetPlayerFromEntity(attackerEntity);

            // Soul Out Damage Prevent
            if (victimPlayer != null && victimPlayer.IsValid)
            {
                if (_playerIsSoul.TryGetValue(victimPlayer.SteamID, out bool isSoul) && isSoul)
                {
                    info.Damage = 0; // Soul takes no damage
                    return HookResult.Changed;
                }
            }

            bool damageChanged = false;

            // Defensive mods
            if (victimPlayer != null && victimPlayer.IsValid && !victimPlayer.IsBot)
            {
                if (HasHextech(victimPlayer, "HeavyInfantry"))
                {
                    info.Damage *= (1.0f - HextechConfig.HeavyInfantryDamageReduction);
                    damageChanged = true;
                }
                if (HasHextech(victimPlayer, "KillerMaster"))
                {
                    _playerRoundKills.TryGetValue(victimPlayer.SteamID, out int kills);
                    float defRatio = Math.Min(HextechConfig.KillerMasterMaxDamageReduction, HextechConfig.KillerMasterDamageReductionPerKill * kills);
                    info.Damage *= (1.0f - defRatio);
                    damageChanged = true;
                }
            }

            // Offensive mods
            if (attackerPlayer != null && attackerPlayer.IsValid && !attackerPlayer.IsBot)
            {
                if (_playerIsSoul.TryGetValue(attackerPlayer.SteamID, out bool isSoulAttacker) && isSoulAttacker)
                {
                    info.Damage = 0; // Soul deals no damage
                    return HookResult.Changed;
                }

                float dmgMult = 1.0f;
                if (HasHextech(attackerPlayer, "GlassCannon")) dmgMult *= HextechConfig.GlassCannonDamageMultiplier;
                if (HasHextech(attackerPlayer, "TinySlayer")) dmgMult *= HextechConfig.TinySlayerDamageMultiplier;
                if (HasHextech(attackerPlayer, "NoPrimary")) dmgMult *= HextechConfig.NoPrimaryDamageMultiplier;
                if (HasHextech(attackerPlayer, "Goliath")) dmgMult *= HextechConfig.GoliathDamageMultiplier;
                
                if (HasHextech(attackerPlayer, "KillerMaster"))
                {
                    _playerRoundKills.TryGetValue(attackerPlayer.SteamID, out int kills);
                    dmgMult *= (1.0f + HextechConfig.KillerMasterDamageIncreasePerKill * kills);
                }

                if (HasHextech(attackerPlayer, "UnluckyContract"))
                {
                    var p = attackerPlayer.PlayerPawn.Value;
                    if (p != null)
                    {
                        float loss = (p.MaxHealth - p.Health) / (float)p.MaxHealth;
                        if (loss < 0) loss = 0;
                        dmgMult *= (1.0f + loss * HextechConfig.UnluckyContractMaxDamageBonus);
                    }
                }

                // Backstab
                if (HasHextech(attackerPlayer, "Backstab"))
                {
                    var apawn = attackerPlayer.PlayerPawn.Value;
                    if (apawn != null)
                    {
                        float deltaYaw = Math.Abs(apawn.EyeAngles.Y - victimPawn.EyeAngles.Y);
                        if (deltaYaw > 180) deltaYaw = 360 - deltaYaw;
                        if (deltaYaw <= HextechConfig.BackstabAngle)
                        {
                            dmgMult *= HextechConfig.BackstabMultiplier;
                        }
                    }
                }

                if (dmgMult != 1.0f)
                {
                    info.Damage *= dmgMult;
                    damageChanged = true;
                }

                if (HasHextech(attackerPlayer, "Execute") && victimPawn.Health < HextechConfig.ExecuteThreshold)
                {
                    info.Damage = victimPawn.Health + 100.0f; 
                    damageChanged = true;
                }
            }

            return damageChanged ? HookResult.Changed : HookResult.Continue;
        }

        private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var attacker = @event.Attacker; 
            var victim = @event.Userid;     
            
            if (attacker != null && attacker.IsValid && !attacker.IsBot)
            {
                if (HasHextech(attacker, "Bounty"))
                {
                    var moneyServices = attacker.InGameMoneyServices;
                    if (moneyServices != null) moneyServices.Account += HextechConfig.BountyKillReward;
                    attacker.GiveNamedItem(HextechConfig.BountyRefreshWeapon);
                }

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
                            if (DateTime.Now >= _playerSpeedBoostEndTime.GetValueOrDefault(attacker.SteamID, DateTime.MinValue))
                            {
                                p.VelocityModifier = HasHextech(attacker, "Speed") ? HextechConfig.SpeedModifier : 1.0f;
                            }
                        });
                    }
                }
                
                // KillerMaster Kill Count
                if (HasHextech(attacker, "KillerMaster"))
                {
                    _playerRoundKills.TryGetValue(attacker.SteamID, out int kills);
                    _playerRoundKills[attacker.SteamID] = kills + 1;
                }
            }

            if (victim != null && victim.IsValid && !victim.IsBot)
            {
                if (HasHextech(victim, "LastStand"))
                {
                    var victimPawn = victim.PlayerPawn.Value;
                    if (victimPawn != null && victimPawn.IsValid)
                    {
                        var origin = victimPawn.AbsOrigin;
                        if (origin != null) TriggerLastStandExplosion(victim, origin);
                    }
                }
            }
            return HookResult.Continue;
        }

        private void TriggerLastStandExplosion(CCSPlayerController victim, Vector explosionPos)
        {
            foreach (var target in Utilities.GetPlayers())
            {
                if (target == null || !target.IsValid || target.IsBot || !target.PawnIsAlive) continue;
                if (target.TeamNum == victim.TeamNum || target.TeamNum <= 1) continue; 

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
                    int dmg = (int)MathF.Max(0, (1.0f - (dist / HextechConfig.LastStandExplosionRadius)) * HextechConfig.LastStandMaxExplosionDamage);
                    if (dmg > 0)
                    {
                        pawn.Health = Math.Max(0, pawn.Health - dmg);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                    }
                }
            }
        }

        private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            _playerBlinksUsed.Clear();
            _playerChronobreaksUsed.Clear();
            _playerRoundKills.Clear();
            _playerHistory.Clear();
            _playerHeThrown.Clear();
            _playerMolotovThrown.Clear();
            _playerSoulUsed.Clear();
            _playerIsSoul.Clear();

            var gameRulesProxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
            if (gameRulesProxy != null && gameRulesProxy.GameRules != null && !gameRulesProxy.GameRules.WarmupPeriod && !_isGameStarted)
            {
                Server.ExecuteCommand("mp_warmuptime 9999");
                Server.ExecuteCommand("mp_warmup_start");
            }
            return HookResult.Continue;
        }

        private void TriggerBlink(CCSPlayerController player)
        {
            if (!player.PawnIsAlive) return;
            if (!HasHextech(player, "Blink")) return;

            _playerBlinksUsed.TryGetValue(player.SteamID, out var used);
            if (used >= HextechConfig.BlinkLimitPerRound) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            var origin = pawn.AbsOrigin;
            var angles = pawn.EyeAngles;
            if (origin == null || angles == null) return;

            float yawRad = angles.Y * (MathF.PI / 180f);
            float forwardX = MathF.Cos(yawRad);
            float forwardY = MathF.Sin(yawRad);

            var newPosition = new Vector(origin.X + (forwardX * HextechConfig.BlinkDistance), origin.Y + (forwardY * HextechConfig.BlinkDistance), origin.Z + 10f);
            pawn.Teleport(newPosition, null, null);
            _playerBlinksUsed[player.SteamID] = used + 1;
        }

        private void TriggerChronobreak(CCSPlayerController player)
        {
            if (!player.PawnIsAlive) return;
            if (!HasHextech(player, "Chronobreak")) return;

            _playerChronobreaksUsed.TryGetValue(player.SteamID, out var used);
            if (used >= HextechConfig.ChronobreakLimitPerRound) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            if (!_playerHistory.TryGetValue(player.SteamID, out var history) || history.Count == 0) return;
            double age = (DateTime.Now - history.Peek().Time).TotalSeconds;
            if (age < 2.5) return;

            var oldState = history.Peek();
            pawn.Teleport(oldState.Position, null, null);
            pawn.Health = oldState.Health;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            _playerChronobreaksUsed[player.SteamID] = used + 1;
        }

        private void TriggerSoulOut(CCSPlayerController player)
        {
            if (!player.PawnIsAlive) return;
            if (!HasHextech(player, "SoulOut")) return;
            
            _playerSoulUsed.TryGetValue(player.SteamID, out var used);
            if (used >= HextechConfig.SoulOutLimitPerRound) return;

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid) return;

            _playerIsSoul[player.SteamID] = true;
            _playerSoulUsed[player.SteamID] = used + 1;
            _playerSoulBodyPos[player.SteamID] = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z);
            
            // Render invisible/color
            pawn.Render = System.Drawing.Color.FromArgb(0, 255, 255, 255); // Alpha 0
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            
            _playerSoulEndTime[player.SteamID] = DateTime.Now.AddSeconds(HextechConfig.SoulOutDuration);
            player.PrintToChat(" \x06[海克斯]\x01 已化为灵魂！");
        }

        private void OnTick()
        {
            foreach (var player in Utilities.GetPlayers())
            {
                if (player == null || !player.IsValid || player.IsBot) continue;

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid) continue;

                // Bleed effect
                if (player.PawnIsAlive)
                {
                    if (_playerBleedEndTime.TryGetValue(player.SteamID, out var bleedEnd) && DateTime.Now < bleedEnd)
                    {
                        _playerLastBleedTick.TryGetValue(player.SteamID, out var lastBleed);
                        if ((DateTime.Now - lastBleed).TotalSeconds >= 1.0)
                        {
                            pawn.Health = Math.Max(0, pawn.Health - HextechConfig.BleedDamage);
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                            _playerLastBleedTick[player.SteamID] = DateTime.Now;
                        }
                    }
                }
                
                // Indomitable Will effect
                if (player.PawnIsAlive && HasHextech(player, "IndomitableWill"))
                {
                    _playerLastIndomitableTick.TryGetValue(player.SteamID, out var lastIndom);
                    if ((DateTime.Now - lastIndom).TotalSeconds >= 1.0)
                    {
                        if (pawn.Health < pawn.MaxHealth)
                        {
                            float loss = (pawn.MaxHealth - pawn.Health) / (float)pawn.MaxHealth;
                            if (loss < 0) loss = 0;
                            int regen = HextechConfig.IndomitableWillBaseRegen + (int)(loss * HextechConfig.IndomitableWillMaxRegenBonus);
                            pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + regen);
                            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        }
                        _playerLastIndomitableTick[player.SteamID] = DateTime.Now;
                    }
                }

                // FlashMagic (Disable fire)
                if (player.PawnIsAlive)
                {
                    if (_playerDisableFireEndTime.TryGetValue(player.SteamID, out var fireEnd) && DateTime.Now < fireEnd)
                    {
                        player.Buttons &= ~PlayerButtons.Attack;
                        player.Buttons &= ~PlayerButtons.Attack2;
                    }
                }

                // UnluckyContract Speed Bonus
                if (player.PawnIsAlive && HasHextech(player, "UnluckyContract"))
                {
                    float loss = (pawn.MaxHealth - pawn.Health) / (float)pawn.MaxHealth;
                    if (loss < 0) loss = 0;
                    pawn.VelocityModifier = 1.0f + (loss * HextechConfig.UnluckyContractMaxSpeedBonus);
                }

                // SoulOut End Check
                if (player.PawnIsAlive && _playerIsSoul.TryGetValue(player.SteamID, out bool isSoul) && isSoul)
                {
                    if (DateTime.Now >= _playerSoulEndTime[player.SteamID])
                    {
                        _playerIsSoul[player.SteamID] = false;
                        pawn.Render = System.Drawing.Color.FromArgb(255, 255, 255, 255); // Restore alpha
                        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
                        
                        if (_playerSoulBodyPos.TryGetValue(player.SteamID, out var pos))
                        {
                            pawn.Teleport(pos, null, null);
                        }
                        pawn.Health = Math.Min(pawn.MaxHealth, pawn.Health + HextechConfig.SoulOutHealthReward);
                        Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                        player.PrintToChat(" \x06[海克斯]\x01 灵魂持续时间结束，已重置肉身！");
                    }
                }
                
                // NoPrimary Weapon Strip
                if (player.PawnIsAlive && HasHextech(player, "NoPrimary"))
                {
                    var weaponServices = pawn.WeaponServices;
                    if (weaponServices != null)
                    {
                        foreach (var weapon in weaponServices.MyWeapons)
                        {
                            if (weapon != null && weapon.IsValid && weapon.Value != null && weapon.Value.IsValid)
                            {
                                string wname = weapon.Value.DesignerName ?? "";
                                if (wname.Contains("ak47") || wname.Contains("m4a1") || wname.Contains("awp") || 
                                    wname.Contains("negev") || wname.Contains("xm1014") || wname.Contains("mac10") || 
                                    wname.Contains("mp9") || wname.Contains("galil") || wname.Contains("famas") || wname.Contains("aug") || wname.Contains("sg556"))
                                {
                                    pawn.RemovePlayerItem(weapon.Value);
                                    weapon.Value.Remove();
                                }
                            }
                        }
                    }
                }

                // Chronobreak History
                if (player.PawnIsAlive && HasHextech(player, "Chronobreak"))
                {
                    var steamId = player.SteamID;
                    if (!_playerHistory.TryGetValue(steamId, out var history))
                    {
                        history = new Queue<PlayerStateRecord>();
                        _playerHistory[steamId] = history;
                    }
                    history.Enqueue(new PlayerStateRecord { Time = DateTime.Now, Health = pawn.Health, Position = new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z) });
                    var cutoff = DateTime.Now.AddSeconds(-HextechConfig.ChronobreakTraceDuration);
                    while (history.Count > 1 && history.ElementAt(1).Time < cutoff) history.Dequeue();
                }

                // Reckoner
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

                // Buttons Double Click Detection
                var currentButtons = player.Buttons;
                _lastButtons.TryGetValue(player.SteamID, out var previousButtons);

                // E Key (Use)
                if ((currentButtons & PlayerButtons.Use) != 0 && (previousButtons & PlayerButtons.Use) == 0)
                {
                    var now = DateTime.Now;
                    _lastUseKeyPressTime.TryGetValue(player.SteamID, out var lastPress);
                    if ((now - lastPress).TotalMilliseconds < HextechConfig.BlinkDoubleClickInterval)
                    {
                        _lastUseKeyPressTime[player.SteamID] = DateTime.MinValue;
                        if (HasHextech(player, "Blink")) TriggerBlink(player);
                        if (HasHextech(player, "Chronobreak")) TriggerChronobreak(player);
                    }
                    else _lastUseKeyPressTime[player.SteamID] = now;
                }
                
                // F Key (LookAtWeapon)
                if ((currentButtons & PlayerButtons.LookAtWeapon) != 0 && (previousButtons & PlayerButtons.LookAtWeapon) == 0)
                {
                    var now = DateTime.Now;
                    _lastLookKeyPressTime.TryGetValue(player.SteamID, out var lastPress);
                    if ((now - lastPress).TotalMilliseconds < HextechConfig.BlinkDoubleClickInterval)
                    {
                        _lastLookKeyPressTime[player.SteamID] = DateTime.MinValue;
                        if (HasHextech(player, "SoulOut")) TriggerSoulOut(player);
                    }
                    else _lastLookKeyPressTime[player.SteamID] = now;
                }

                _lastButtons[player.SteamID] = currentButtons;
            }
        }

        private void StripPlayerWeaponsExceptKnife(CCSPlayerController player)
        {
            var pawn = player.PlayerPawn.Value;
            if (pawn == null || pawn.WeaponServices == null) return;
            var weapons = new List<CHandle<CBasePlayerWeapon>>(pawn.WeaponServices.MyWeapons);
            foreach (var weapon in weapons)
            {
                if (weapon == null || !weapon.IsValid || weapon.Value == null || !weapon.Value.IsValid) continue;
                string name = weapon.Value.DesignerName ?? "";
                if (name.Contains("knife") || name.Contains("bayonet") || name.Contains("melee") || name.Contains("taser")) continue;
                pawn.RemovePlayerItem(weapon.Value);
                weapon.Value.Remove();
            }
        }

        private void ApplyWeaponMaster(CCSPlayerController player)
        {
            StripPlayerWeaponsExceptKnife(player);
            string primary = HextechConfig.WeaponMasterPrimaryPool[_random.Next(HextechConfig.WeaponMasterPrimaryPool.Length)];
            string secondary = HextechConfig.WeaponMasterSecondaryPool[_random.Next(HextechConfig.WeaponMasterSecondaryPool.Length)];
            player.GiveNamedItem(primary);
            player.GiveNamedItem(secondary);
        }

        private void ShowHextechMenu(CCSPlayerController player)
        {
            if (player == null || !player.IsValid) return;

            var allOptions = new List<HextechOption>
            {
                // Silver (9)
                new HextechOption("Speed", "⚡ 风行者 <font size='12'>(速度提升 1.3 倍)</font>", (p) => ApplyHextechEffect(p, "Speed")),
                new HextechOption("Bounty", "💎 赏金猎人 <font size='12'>(击杀+$2000 & 刷新电击枪)</font>", (p) => ApplyHextechEffect(p, "Bounty")),
                new HextechOption("Thorns", "🛡️ 棘刺甲壳 <font size='12'>(反弹 30% 受到伤害)</font>", (p) => ApplyHextechEffect(p, "Thorns")),
                new HextechOption("Reckoner", "🔥 狂徒 <font size='12'>(无伤 10 秒后每秒回复 2 HP)</font>", (p) => ApplyHextechEffect(p, "Reckoner")),
                new HextechOption("Bleed", "🔥 流血 <font size='12'>(攻击造成 5秒流血)</font>", (p) => ApplyHextechEffect(p, "Bleed")),
                new HextechOption("FlashMagic", "🪄 精怪魔法 <font size='12'>(致盲敌人3秒内不能开火)</font>", (p) => ApplyHextechEffect(p, "FlashMagic")),
                new HextechOption("GrenadeMaster", "💣 道具大师 <font size='12'>(投掷物伤害+50%并可用2颗)</font>", (p) => ApplyHextechEffect(p, "GrenadeMaster")),
                new HextechOption("LastStand", "💣 自爆卡车 <font size='12'>(死亡爆炸伤害周围敌人)</font>", (p) => ApplyHextechEffect(p, "LastStand")),
                new HextechOption("GoldenChange", "✨ 质变黄金 <font size='12'>(随机获得金色海克斯)</font>", (p) => ApplyHextechEffect(p, "GoldenChange")),

                // Gold (11)
                new HextechOption("Titan", "❤️ 泰坦之躯 <font size='12'>(全额 200HP)</font>", (p) => ApplyHextechEffect(p, "Titan")),
                new HextechOption("Vampire", "🩸 吸血狂热 <font size='12'>(造成伤害回血 30%)</font>", (p) => ApplyHextechEffect(p, "Vampire")),
                new HextechOption("Blink", "🌀 闪现星使 <font size='12'>(双击 E 键闪现，每回合2次)</font>", (p) => ApplyHextechEffect(p, "Blink")),
                new HextechOption("KillRush", "🏃 狂暴杀戮 <font size='12'>(击杀后 3秒 提升移速)</font>", (p) => ApplyHextechEffect(p, "KillRush")),
                new HextechOption("HeavyInfantry", "🎖️ 重装坦克 <font size='12'>(自带全套防弹衣 & 减伤15%)</font>", (p) => ApplyHextechEffect(p, "HeavyInfantry")),
                new HextechOption("WeaponMaster", "🎲 武器大师 <font size='12'>(每局随机主副武器 & 全套防弹衣)</font>", (p) => ApplyHextechEffect(p, "WeaponMaster")),
                new HextechOption("Backstab", "🗡️ 小丑背刺 <font size='12'>(背后攻击 3 倍伤害)</font>", (p) => ApplyHextechEffect(p, "Backstab")),
                new HextechOption("Execute", "🎯 死神斩杀 <font size='12'>(斩杀低于 35HP 敌人)</font>", (p) => ApplyHextechEffect(p, "Execute")),
                new HextechOption("IndomitableWill", "🛡️ 坚韧不拔 <font size='12'>(血量越低回血越快)</font>", (p) => ApplyHextechEffect(p, "IndomitableWill")),
                new HextechOption("HeadshotMaster", "🎯 爆头大师 <font size='12'>(爆头100%吸血)</font>", (p) => ApplyHextechEffect(p, "HeadshotMaster")),
                new HextechOption("PrismaticChange", "✨ 质变棱彩 <font size='12'>(随机获得彩色海克斯)</font>", (p) => ApplyHextechEffect(p, "PrismaticChange")),

                // Prismatic (8)
                new HextechOption("SoulOut", "👻 灵魂出窍 <font size='12'>(双击F灵魂分离 8秒)</font>", (p) => ApplyHextechEffect(p, "SoulOut")),
                new HextechOption("GlassCannon", "💥 玻璃大炮 <font size='12'>(伤害+50% & 血量上限60)</font>", (p) => ApplyHextechEffect(p, "GlassCannon")),
                new HextechOption("Chronobreak", "⏳ 时空回溯 <font size='12'>(双击 E 键回溯 3秒前状态)</font>", (p) => ApplyHextechEffect(p, "Chronobreak")),
                new HextechOption("KillerMaster", "👑 我是高手 <font size='12'>(击杀加伤害与减伤)</font>", (p) => ApplyHextechEffect(p, "KillerMaster")),
                new HextechOption("TinySlayer", "🛸 巨人杀手 <font size='12'>(体型变小，移速伤害提升)</font>", (p) => ApplyHextechEffect(p, "TinySlayer")),
                new HextechOption("NoPrimary", "🚫 回归基本功 <font size='12'>(禁主武器，加伤害吸血移速)</font>", (p) => ApplyHextechEffect(p, "NoPrimary")),
                new HextechOption("Goliath", "🌋 歌利亚巨人 <font size='12'>(体型变大，血量伤害提升)</font>", (p) => ApplyHextechEffect(p, "Goliath")),
                new HextechOption("UnluckyContract", "☠️ 不详契约 <font size='12'>(血越少伤害移速越高，开火自伤)</font>", (p) => ApplyHextechEffect(p, "UnluckyContract"))
            };

            var selectedOptions = new List<HextechOption>();
            while (selectedOptions.Count < 3 && allOptions.Count > 0)
            {
                int index = _random.Next(allOptions.Count);
                selectedOptions.Add(allOptions[index]);
                allOptions.RemoveAt(index);
            }

            var hexMenu = new CenterHtmlMenu("<font color='#a4e6ff'><b>🧬 战术实验室：请选择本局海克斯效果</b></font>", this);
            foreach (var opt in selectedOptions)
            {
                hexMenu.AddMenuOption(opt.DisplayName, (playerController, option) => { opt.ApplyAction(playerController); });
            }
            MenuManager.OpenCenterHtmlMenu(this, player, hexMenu);
        }

        private void ApplyHextechEffect(CCSPlayerController player, string hexType)
        {
            if (player == null || !player.IsValid) return;

            // Handle Changes Randomly
            if (hexType == "GoldenChange")
            {
                string[] goldens = { "Titan", "Vampire", "Blink", "KillRush", "HeavyInfantry", "WeaponMaster", "Backstab", "Execute", "IndomitableWill", "HeadshotMaster" };
                hexType = goldens[_random.Next(goldens.Length)];
                player.PrintToChat($" \x06[海克斯]\x01 你已激活 质变黄金！获得了金色海克斯：\x04{hexType}\x01！");
            }
            else if (hexType == "PrismaticChange")
            {
                string[] prismatics = { "SoulOut", "GlassCannon", "Chronobreak", "KillerMaster", "TinySlayer", "NoPrimary", "Goliath", "UnluckyContract" };
                hexType = prismatics[_random.Next(prismatics.Length)];
                player.PrintToChat($" \x06[海克斯]\x01 你已激活 质变棱彩！获得了彩色海克斯：\x04{hexType}\x01！");
            }

            if (!_playerHextechs.ContainsKey(player.SteamID)) _playerHextechs[player.SteamID] = new HashSet<string>();
            if (HextechConfig.CoreMode == 1) _playerHextechs[player.SteamID].Clear();
            _playerHextechs[player.SteamID].Add(hexType);
            _playerLastChoiceRound[player.SteamID] = GetCurrentRound();

            ReapplyActiveHextechs(player);
            player.PrintToChat($" \x06[海克斯]\x01 你已成功选择海克斯：\x04{hexType}\x01！");
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
            Id = id; DisplayName = displayName; ApplyAction = applyAction;
        }
    }
}
