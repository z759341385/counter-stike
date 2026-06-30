const Rcon = require('rcon');

// 这是一个单功能测试脚本，使用 RCON 协议与 CS2 游戏服务端进行通信。
// RCON (Remote Console) 是最简单的服务端集成方式，无需编写 C# 插件即可从 Node.js 控制游戏。

const rconOptions = {
    tcp: true,       // CS2 RCON 使用 TCP
    challenge: false // 大部分 Node rcon 库需要根据具体版本调整
};

// 【配置区】在这里填入您旧电脑（或本地测试服）的 IP、端口和 RCON 密码
const cs2ServerIp = '127.0.0.1'; // 如果在同一台电脑填 127.0.0.1，如果在局域网其他电脑填 192.168.x.x
const cs2ServerPort = 27015;     // 默认端口
const rconPassword = 'your_test_password'; // 您在 CS2 服务器启动项或 server.cfg 中设置的 rcon_password

const conn = new Rcon(cs2ServerIp, cs2ServerPort, rconPassword, rconOptions);

conn.on('auth', function() {
  console.log("✅ 成功连接并认证到 CS2 服务器 RCON！");
  
  // ==========================================
  // 测试功能 1：将抽到的规则以“公屏广播”的形式发送到游戏内
  // ==========================================
  const testRuleName = "西部牛仔";
  console.log(`正在下发单功能测试指令: 广播规则 [${testRuleName}]`);
  
  // 发送指令到游戏内公屏
  conn.send(`say [内战抽卡系统] 本回合规则已锁定: ${testRuleName}! 仅限沙鹰和左轮！`);
  
  // 测试功能 2：发送改变游戏物理引擎的指令 (比如低重力测试)
  // conn.send(`sv_gravity 400`); 
});

conn.on('response', function(str) {
  console.log("收到游戏服务器响应: " + str);
});

conn.on('error', function(err) {
  console.log("❌ 连接游戏服务器失败: " + err);
  console.log("请确认：1. 测试服已开启 2. IP和端口正确 3. 密码正确");
});

conn.on('end', function() {
  console.log("RCON 连接已断开。");
});

// 执行连接
console.log(`正在尝试连接到 CS2 服务器 (${cs2ServerIp}:${cs2ServerPort})...`);
conn.connect();

// 导出模块，方便以后您的 backend 调用
module.exports = {
    triggerRuleInGame: (ruleName, ruleDesc) => {
        if (conn.hasAuthed) {
            conn.send(`say [内战抽卡系统] ${ruleName}: ${ruleDesc}`);
        } else {
            console.log("RCON 未连接，无法下发指令。");
        }
    }
};
