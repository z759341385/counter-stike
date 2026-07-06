using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace HpModifier;

public class HpModifier : BasePlugin
{
    public override string ModuleName => "HP Modifier Plugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Antigravity";

    public override void Load(bool hotReload)
    {
        // 注册事件：当玩家复活/生成时触发
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;

        // 验证玩家实体是否有效，过滤掉无效对象
        if (player == null || !player.IsValid || player.IsBot)
        {
            return HookResult.Continue;
        }

        // 稍微延迟 0.1 秒执行，以确保玩家角色（Pawn）已经完全在地图上初始化完毕
        AddTimer(0.1f, () =>
        {
            if (player != null && player.IsValid && player.PlayerPawn.Value != null)
            {
                // 修改最大生命值和当前生命值为 200
                player.PlayerPawn.Value.MaxHealth = 200;
                player.PlayerPawn.Value.Health = 200;

                // 强制同步网络状态（State Changed），以便游戏客户端界面（HUD）能正确更新生命值条
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
                Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

                player.PrintToChat(" [内战抽卡系统] 你的生命值已被修改为 200 HP！");
            }
        });

        return HookResult.Continue;
    }

    // 注册控制台命令：可以在游戏控制台输入 css_hp200 手动修改所有玩家 HP
    [ConsoleCommand("css_hp200", "将当前所有在线玩家的生命值修改为 200")]
    [CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnHp200Command(CCSPlayerController? caller, CommandInfo info)
    {
        var players = Utilities.GetPlayers();
        int count = 0;

        foreach (var player in players)
        {
            if (player == null || !player.IsValid || player.PlayerPawn.Value == null) 
                continue;

            player.PlayerPawn.Value.MaxHealth = 200;
            player.PlayerPawn.Value.Health = 200;

            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iMaxHealth");
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_iHealth");

            player.PrintToChat(" [内战抽卡系统] 管理员已将你的生命值修改为 200 HP！");
            count++;
        }

        info.ReplyToCommand($"已成功将 {count} 名玩家的生命值修改为 200 HP！");
    }
}
