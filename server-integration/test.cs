using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace AutoDrawPlugin;

public class AutoDrawPlugin : BasePlugin
{
    public override string ModuleName => "Auto Draw Card Plugin";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Antigravity";

    private bool _hasDrawnThisMatch = false; // 确保每场比赛只抽一次

    public override void Load(bool hotReload)
    {
        // 注册回合开始事件
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        // 注册地图宣告热身（暖场）事件，用于重置状态
        RegisterEventHandler<EventRoundAnnounceWarmup>(OnWarmupStart);
    }

    private HookResult OnWarmupStart(EventRoundAnnounceWarmup @event, GameEventInfo info)
    {
        // 热身阶段重置抽卡状态
        _hasDrawnThisMatch = false;
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // 1. 获取当前游戏规则实例
        var gameRulesProxy = Utilities.FindAll<CCSGameRulesProxy>().FirstOrDefault();
        if (gameRulesProxy == null || gameRulesProxy.GameRules == null)
        {
            return HookResult.Continue;
        }

        var gameRules = gameRulesProxy.GameRules;

        // 2. 过滤掉热身阶段（暖场不抽卡）
        if (gameRules.WarmupPeriod)
        {
            return HookResult.Continue;
        }

        // 3. 判断是否是第一回合（手枪局），且本场比赛还没抽过卡
        // gameRules.TotalRoundsPlayed == 0 代表第一回合刚开始
        if (gameRules.TotalRoundsPlayed == 0 && !_hasDrawnThisMatch)
        {
            _hasDrawnThisMatch = true;

            // 延迟 2 秒执行抽卡和弹窗，避免跟游戏自带的 "ROUND 1" / "手枪局" 提示撞在一起
            AddTimer(2.0f, () =>
            {
                TriggerCardDraw();
            });
        }

        return HookResult.Continue;
    }

    private void TriggerCardDraw()
    {
        // 💡 模拟抽卡数据：实际开发中，这里可以发起一个异步 HTTP 请求访问你的 Node.js 后端接口
        string ruleName = "低重力沙鹰";
        string ruleDesc = "本回合重力减半！且只能使用沙鹰！";

        // 1. 在游戏内修改重力值 (通过控制台执行指令)
        Server.ExecuteCommand("sv_gravity 400"); 

        // 2. 全服玩家弹窗广播与音效
        foreach (var player in Utilities.GetPlayers())
        {
            if (player == null || !player.IsValid || player.IsBot) continue;

            // 播放升级/特殊提示音效
            player.ExecuteClientCommand("play sounds/ui/armsrace_demoted.vsnd");

            // 屏幕中央 HTML 闪亮弹窗
            player.PrintToCenterHtml(
                "<font color='orange' size='6'><b>🃏 内战卡牌已锁定 🃏</b></font><br>" +
                $"<font color='white' size='5'>本局规则：</font><font color='red' size='5'><b>{ruleName}</b></font><br>" +
                $"<font color='yellow' size='4'>{ruleDesc}</font>"
            );
            
            // 聊天框同时发送文字备份，防止玩家没看清弹窗
            player.PrintToChat($" \x04[抽卡系统]\x01 本局规则已锁定: \x02{ruleName}\x01 ({ruleDesc})");
        }
    }
}
