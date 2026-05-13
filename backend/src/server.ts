/**
 * CS-Rule-Engine 主服务入口
 */

import express from 'express';
import http from 'http';
import { Server } from 'socket.io';
import cors from 'cors';
import { initDatabase } from './db';
import * as rooms from './rooms';
import { startVote, submitVote } from './vote';
import { stopVoteAndSpin } from './spin';
import { logger } from './logger';

const PORT = process.env.PORT ? parseInt(process.env.PORT, 10) : 3000;
const DISCONNECT_TIMEOUT = 60 * 1000;

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
      socket.emit('reconnect_success', { roomId: result.roomId, room: rooms.getRoom(result.roomId) });
      io.to(result.roomId).emit('room_update', rooms.getRoom(result.roomId));
    }
  }

  socket.on('create_room', ({ playerName }, callback) => {
    const { roomId, token } = rooms.createRoom(socket.id, playerName);
    socket.join(roomId);
    if (typeof callback === 'function') callback({ success: true, roomId, token });
    io.to(roomId).emit('room_update', rooms.getRoom(roomId));
  });

  socket.on('join_room', ({ roomId, playerName }, callback) => {
    const result = rooms.joinRoom(roomId, socket.id, playerName);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
      return;
    }
    socket.join(roomId);
    if (typeof callback === 'function') callback({ success: true, roomId, token: result.token });
    io.to(roomId).emit('room_update', rooms.getRoom(roomId));
  });

  // 上座
  socket.on('take_seat', ({ targetIdx }) => {
    const roomId = rooms.findRoomBySocket(socket.id);
    const room = rooms.getRoom(roomId || '');
    const player = room?.players.find(p => p && p.socketId === socket.id) || 
                   room?.spectators.find(p => p.socketId === socket.id);

    if (roomId && player && rooms.takeSeat(roomId, player.token, targetIdx)) {
      logger.info('Room', `玩家 ${player.playerName} 上座成功: ${targetIdx}`);
      io.to(roomId).emit('room_update', rooms.getRoom(roomId));
    }
  });

  // 退到观战区
  socket.on('leave_seat', () => {
    const roomId = rooms.findRoomBySocket(socket.id);
    const room = rooms.getRoom(roomId || '');
    const player = room?.players.find(p => p && p.socketId === socket.id);

    if (roomId && player && rooms.leaveSeat(roomId, player.token)) {
      logger.info('Room', `玩家 ${player.playerName} 退回到观战区`);
      io.to(roomId).emit('room_update', rooms.getRoom(roomId));
    }
  });

  socket.on('start_vote', ({ roomId }, callback) => {
    const result = startVote(roomId, socket.id);
    if (typeof result === 'string') {
      if (typeof callback === 'function') callback({ success: false, error: result });
    } else {
      if (typeof callback === 'function') callback({ success: true });
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
      io.to(roomId).emit('spin_wheel', result);
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
        if (typeof callback === 'function') callback({ success: true });
        io.to(roomId).emit('room_reset');
        io.to(roomId).emit('room_update', rooms.getRoom(roomId));
      }
    }
  });

  socket.on('disconnect', () => {
    const roomId = rooms.handleDisconnect(socket.id);
    if (roomId) {
      io.to(roomId).emit('room_update', rooms.getRoom(roomId));
      setTimeout(() => {
        const room = rooms.getRoom(roomId);
        if (!room) return;
        const player = [...room.players, ...room.spectators].find(p => p && p.socketId === socket.id);
        if (player && !player.connected) {
          const result = rooms.removePlayer(roomId, player.token);
          if (result && !result.destroyed) {
            io.to(roomId).emit('room_update', rooms.getRoom(roomId));
          }
        }
      }, DISCONNECT_TIMEOUT);
    }
  });
});

server.listen(PORT, () => {
  logger.info('Server', `🚀 CS-Rule-Engine 已启动, 端口: ${PORT}`);
});
