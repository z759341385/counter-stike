using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Menu;
using CounterStrikeSharp.API.Modules.Commands;

namespace CS2HextechPlugin
{
    public class CS2HextechPlugin : BasePlugin
    {
        public override string ModuleName => "CS2 海克斯选择插件";
        public override string ModuleVersion => "1.0.0";

        // 用于保存每个玩家的选择结果 (Key: SteamID, Value: 海克斯类型)
        public Dictionary<ulong, string> PlayerHextechs = new Dictionary<ulong, string>();

        public override void Load(bool hotReload)
        {
            // 注册控制台命令：玩家在聊天框输入 !hex 或 /hex 即可打开菜单
            AddCommand("css_hex", "打开海克斯选择菜单", CommandOpenHexMenu);
            
            // 监听玩家出生，自动弹出菜单
            RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
            {
                var player = @event.Userid;
                if (player != null && player.IsValid && !player.IsBot)
                {
                    // 在玩家刚复活时，异步弹出海克斯选择菜单
                    AddTimer(1.0f, () => ShowHextechMenu(player));
                }
                return HookResult.Continue;
            });
        }

        private void CommandOpenHexMenu(CCSPlayerController? player, CommandInfo commandInfo)
        {
            if (player == null || !player.IsValid) return;
            ShowHextechMenu(player);
        }

        private void ShowHextechMenu(CCSPlayerController player)
        {
            // 1. 创建一个屏幕中央 HTML 菜单
            // 支持使用 HTML 标签（如 <font color='#ff0000'>）进行调色和排版
            var hexMenu = new CenterHtmlMenu("<font color='#a4e6ff'><b>🧬 请选择本局海克斯效果</b></font>", this);

            // 2. 添加海克斯选项以及对应的选择回调函数
            hexMenu.AddMenuOption("❤️ 泰坦之躯 (200HP)", (playerController, option) =>
            {
                ApplyHexEffect(playerController, "Titan");
            });

            hexMenu.AddMenuOption("🩸 噬血渴望 (30%吸血)", (playerController, option) =>
            {
                ApplyHexEffect(playerController, "Vampire");
            });

            hexMenu.AddMenuOption("⚡ 迅捷斥候 (速度+30%)", (playerController, option) =>
            {
                ApplyHexEffect(playerController, "Speed");
            });

            // 3. 对该玩家展示菜单
            MenuManager.OpenCenterHtmlMenu(this, player, hexMenu);
        }

        private void ApplyHexEffect(CCSPlayerController player, string hexType)
        {
            if (player == null || !player.IsValid) return;

            // 记录玩家的选择
            PlayerHextechs[player.SteamID] = hexType;
            player.PrintToChat($" \x06[海克斯系统]\x01 你已成功选择：\x04{hexType}\x01，效果将在本回合生效！");

            // 根据选择立刻应用某些属性
            if (hexType == "Titan" && player.PlayerPawn.Value != null)
            {
                player.PlayerPawn.Value.MaxHealth = 200;
                player.PlayerPawn.Value.Health = 200;
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");
            }
            // 可以在此处为其他效果打上标记，在交火或移动事件中去读取判定
        }
    }
}