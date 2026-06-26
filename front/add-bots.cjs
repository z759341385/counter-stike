const { io } = require('socket.io-client');

const roomId = process.argv[2];

if (!roomId) {
  console.error('❌ 请提供房间号! 用法: node add-bots.js <房间号>');
  process.exit(1);
}

const URL = 'http://localhost:3001';
console.log(`🤖 开始为房间 ${roomId} 添加测试机器人...`);

// 我们创建 9 个机器人，因为你自己占了 1 个位置
const BOTS_COUNT = 9;

for (let i = 1; i <= BOTS_COUNT; i++) {
  const socket = io(URL, { transports: ['websocket'] });
  const botName = `机器人_${i}`;

  socket.on('connect', () => {
    console.log(`[${botName}] 已连接服务器`);
    
    // 1. 加入房间
    socket.emit('join_room', { roomId, playerName: botName }, (res) => {
      if (res.success) {
        console.log(`[${botName}] 成功加入房间, 尝试寻找空座位...`);
        
        // 2. 监听房间更新，寻找空座位坐下
        socket.on('room_update', (room) => {
          // 找到当前自己是否在座位上
          const myPlayer = room.players.find(p => p && p.playerName === botName);
          if (myPlayer) return; // 已经坐下了

          // 找一个空座位坐下
          const emptyIdx = room.players.findIndex(p => p === null);
          if (emptyIdx !== -1) {
            socket.emit('take_seat', { targetIdx: emptyIdx });
          }
        });

      } else {
        console.error(`[${botName}] 加入失败:`, res.error);
        socket.disconnect();
      }
    });
  });
}
