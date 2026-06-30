/**
 * CS-Rule-Engine 主服务入口
 */

import express from 'express';
import http from 'http';
import { Server } from 'socket.io';
import cors from 'cors';
import { initDatabase, getAllRules, addRule, updateRule, deleteRule, clearAllRules, bulkAddRules } from './db';
import * as rooms from './rooms';
import { startVote, submitVote } from './vote';
import { stopVoteAndSpin } from './spin';
import { assignImposters, startImposterVote, submitImposterVote, endImposterVote } from './imposter';
import { logger } from './logger';

const PORT = process.env.PORT ? parseInt(process.env.PORT, 10) : 3001;
const DISCONNECT_TIMEOUT = 60 * 60 * 1000; // 1小时，防止后台挂机掉线
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD || 'admin888';

const app = express();
app.use(cors());
app.get('/health', (_req, res) => res.json({ status: 'ok', uptime: process.uptime() }));

const server = http.createServer(app);
const io = new Server(server, {
  cors: { origin: '*', methods: ['GET', 'POST'] },
});

initDatabase();

io.on('connection', (socket) => {
  const { token } = socket.handshake.auth;

  if (token) {
    const result = rooms.reconnectPlayer(token, socket.id);
    if (result) {
      socket.join(result.roomId);
      socket.emit('reconnect_success', { roomId: result.roomId, room: rooms.getRoomData(result.roomId) });
      io.to(result.roomId).emit('room_update', rooms.getRoomData(result.roomId));
    } else {
      socket.emit('reconnect_failed');
    }
  }

  socket.on('create_room', ({ playerName, gameMode, imposterCount }, callback) => {
    const count = typeof imposterCount === 'number' ? imposterCount : 1;
    const mode = gameMode === 'IMPOSTER' ? 'IMPOSTER' : 'ROULETTE';
    const { roomId, token } = rooms.createRoom(socket.id, playerName, mode, count);
    socket.join(roomId);
    if (typeof callback === 'function') callback({ success: true, roomId, token });
    io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
  });

  socket.on('join_room', ({ roomId, playerName }, callback) => {
    const result = rooms.joinRoom(roomId, socket.id, playerName);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
      return;
    }
    socket.join(roomId);
    if (typeof callback === 'function') callback({ success: true, roomId, token: result.token });
    io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
  });

  // 上座
  socket.on('take_seat', ({ targetIdx }) => {
    const roomId = rooms.findRoomBySocket(socket.id);
    const room = rooms.getRoom(roomId || '');
    const player = room?.players.find(p => p && p.socketId === socket.id) ||
      room?.spectators.find(p => p.socketId === socket.id);

    if (roomId && player && rooms.takeSeat(roomId, player.token, targetIdx)) {
      logger.info('Room', `玩家 ${player.playerName} 上座成功: ${targetIdx}`);
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  // 退到观战区
  socket.on('leave_seat', () => {
    const roomId = rooms.findRoomBySocket(socket.id);
    const room = rooms.getRoom(roomId || '');
    const player = room?.players.find(p => p && p.socketId === socket.id);

    if (roomId && player && rooms.leaveSeat(roomId, player.token)) {
      logger.info('Room', `玩家 ${player.playerName} 退回到观战区`);
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  socket.on('start_vote', ({ roomId }, callback) => {
    const result = startVote(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      const room = rooms.getRoom(roomId);
      if (room) room.lastSpinResult = null; // 开始新投票时清除旧结果
      io.to(roomId).emit('vote_started', result);
    }
  });

  socket.on('submit_vote', ({ roomId, optionId }, callback) => {
    const result = submitVote(roomId, socket.id, optionId);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      io.to(roomId).emit('vote_progress', { totalVotes: result.totalVotes });
    }
  });

  socket.on('stop_vote_and_spin', ({ roomId }, callback) => {
    const result = stopVoteAndSpin(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      const room = rooms.getRoom(roomId);
      if (room) room.lastSpinResult = result;
      io.to(roomId).emit('spin_wheel', result);
    }
  });

  // 内鬼模式相关事件
  socket.on('assign_imposters', ({ roomId }, callback) => {
    const result = assignImposters(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      const impostersTokens = result;
      // 给对应的玩家私信身份
      const room = rooms.getRoom(roomId);
      if (room) {
        for (const token of impostersTokens) {
          const player = room.players.find(p => p && p.token === token);
          if (player && player.socketId) {
            io.to(player.socketId).emit('you_are_imposter');
          }
        }
      }
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  socket.on('start_imposter_vote', ({ roomId }, callback) => {
    const result = startImposterVote(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  socket.on('submit_imposter_vote', ({ roomId, targetTokens }, callback) => {
    const result = submitImposterVote(roomId, socket.id, targetTokens);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      io.to(roomId).emit('imposter_vote_progress', { totalVotes: result.totalVotes });
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  socket.on('end_imposter_vote', ({ roomId }, callback) => {
    const result = endImposterVote(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
      io.to(roomId).emit('imposter_result', result);
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
    }
  });

  socket.on('reset_room', ({ roomId }, callback) => {
    const room = rooms.getRoom(roomId);
    if (room) {
      const host = [...room.players, ...room.spectators].find(p => p && p.socketId === socket.id && p.isHost);
      if (host) {
        room.status = 'WAITING';
        room.voteOptions = [];
        room.votes = new Map();
        room.lastSpinResult = null;
        if (typeof callback === 'function') callback({ success: true });
        io.to(roomId).emit('room_reset');
        io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
      }
    }
  });

  socket.on('destroy_room', ({ roomId }, callback) => {
    const room = rooms.getRoom(roomId);
    if (room) {
      const host = [...room.players, ...room.spectators].find(p => p && p.socketId === socket.id && p.isHost);
      if (host) {
        io.to(roomId).emit('room_destroyed');
        rooms.destroyRoom(roomId);
        if (typeof callback === 'function') callback({ success: true });
      }
    }
  });

  socket.on('disconnect', () => {
    const roomId = rooms.handleDisconnect(socket.id);
    if (roomId) {
      io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
      setTimeout(() => {
        const room = rooms.getRoom(roomId);
        if (!room) return;
        const player = [...room.players, ...room.spectators].find(p => p && p.socketId === socket.id);
        if (player && !player.connected) {
          const result = rooms.removePlayer(roomId, player.token);
          if (result && !result.destroyed) {
            io.to(roomId).emit('room_update', rooms.getRoomData(roomId));
          }
        }
      }, DISCONNECT_TIMEOUT);
    }
  });

  // ── 管理员接口 ──
  socket.on('admin_login', (password, callback) => {
    if (password === ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: true });
    } else {
      if (typeof callback === 'function') callback({ success: false, error: '密码错误' });
    }
  });

  socket.on('admin_get_rules', (password, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }
    if (typeof callback === 'function') callback({ success: true, rules: getAllRules() });
  });

  socket.on('admin_add_rule', ({ password, rule }, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }
    const id = addRule(rule.name, rule.description, rule.weight || 5);
    if (typeof callback === 'function') callback({ success: true, id });
  });

  socket.on('admin_update_rule', ({ password, rule }, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }
    updateRule(rule.id, rule.name, rule.description, rule.weight, rule.is_active);
    if (typeof callback === 'function') callback({ success: true });
  });

  socket.on('admin_delete_rule', ({ password, id }, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }
    deleteRule(id);
    if (typeof callback === 'function') callback({ success: true });
  });

  socket.on('admin_clear_rules', (password, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }
    clearAllRules();
    if (typeof callback === 'function') callback({ success: true });
  });

  socket.on('admin_import_rules', ({ password, rules, mode }, callback) => {
    if (password !== ADMIN_PASSWORD) {
      if (typeof callback === 'function') callback({ success: false, error: '权限不足' });
      return;
    }

    if (mode === 'overwrite') {
      clearAllRules();
    }

    bulkAddRules(rules);
    if (typeof callback === 'function') callback({ success: true });
  });
});

server.listen(PORT, () => {
  logger.info('Server', `🚀 CS-Rule-Engine 已启动, 端口: ${PORT}`);
});
