using System;
using System.Collections.Generic;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Modules.Commands;

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

        public override void Load(bool hotReload)
        {
            Console.WriteLine("[HextechPlugin] 插件加载成功！");

            // 注册打开菜单指令，玩家在聊天框敲 !hex 或 /hex 即可触发
            AddCommand("css_hex", "打开海克斯效果选择菜单", CommandOpenHexMenu);

            // 监听玩家复活 (Spawn) 事件，自动为其弹出菜单
            RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);

            // 监听玩家受伤 (Hurt) 事件，用于实现“吸血鬼”海克斯效果
            RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);
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
        /// 构建并弹出 CenterHtmlMenu
        /// </summary>
        private void ShowHextechMenu(CCSPlayerController player)
        {
            // 1. 初始化菜单，标题部分支持 HTML 标签（用于颜色和字体加粗）
            var hexMenu = new CenterHtmlMenu("<font color='#a4e6ff'><b>🧬 战术实验室：请选择本局海克斯效果</b></font>", this);

            // 2. 添加第一个选项：泰坦之躯 (200HP)
            hexMenu.AddMenuOption("❤️ 泰坦之躯 (全额 200HP)", (playerController, option) =>
            {
                ApplyHextechEffect(playerController, "Titan");
            });

            // 3. 添加第二个选项：吸血狂热 (30%伤害吸血)
            hexMenu.AddMenuOption("🩸 吸血狂热 (造成伤害回血 30%)", (playerController, option) =>
            {
                ApplyHextechEffect(playerController, "Vampire");
            });

            // 4. 添加第三个选项：风行者 (身轻如燕移动如风)
            hexMenu.AddMenuOption("⚡ 风行者 (速度提升 1.3 倍)", (playerController, option) =>
            {
                ApplyHextechEffect(playerController, "Speed");
            });

            // 5. 对该玩家进行展示，并在玩家选择后自动关闭
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
            }

            // 关闭当前菜单
            MenuManager.CloseActiveMenu(player);
        }
    }
}
