import { logger } from './logger';

/** 玩家信息 */
export interface Player {
  socketId: string;
  token: string;
  playerName: string;
  isHost: boolean;
  connected: boolean;
}

/** 投票选项 */
export interface VoteOption {
  id: number;
  name: string;
  description: string;
}

/** 房间状态 */
export type RoomStatus = 'WAITING' | 'VOTING' | 'SPINNING' | 'RESULT';

/** 房间完整状态 */
export interface RoomState {
  roomId: string;
  players: (Player | null)[]; // 10 个固定席位 (0-4 Team A, 5-9 Team B)
  spectators: Player[];       // 观战区玩家
  status: RoomStatus;
  voteOptions: VoteOption[];
  votes: Map<string, number>;
  lastSpinResult?: any;
  usedRuleIds: number[];
}

const rooms = new Map<string, RoomState>();
const tokenToRoom = new Map<string, string>();

function generateRoomId(): string {
  let id: string;
  do {
    id = String(Math.floor(1000 + Math.random() * 9000));
  } while (rooms.has(id));
  return id;
}

function generateToken(): string {
  return Math.random().toString(36).substring(2, 15);
}

/** 创建房间 */
export function createRoom(hostSocketId: string, hostName: string): { roomId: string; token: string } {
  const roomId = generateRoomId();
  const token = generateToken();
  
  const room: RoomState = {
    roomId,
    players: new Array(10).fill(null),
    spectators: [],
    status: 'WAITING',
    voteOptions: [],
    votes: new Map(),
    usedRuleIds: [],
  };

  room.players[0] = { socketId: hostSocketId, token, playerName: hostName, isHost: true, connected: true };
  
  rooms.set(roomId, room);
  tokenToRoom.set(token, roomId);
  return { roomId, token };
}

/** 加入房间 */
export function joinRoom(roomId: string, socketId: string, playerName: string): { token: string } | string {
  const room = rooms.get(roomId);
  if (!room) return '房间不存在';
  
  const token = generateToken();
  room.spectators.push({ socketId, token, playerName, isHost: false, connected: true });
  tokenToRoom.set(token, roomId);

  return { token };
}

/** 坐下：从观战区或其他位置移到指定位置 */
export function takeSeat(roomId: string, token: string, targetIdx: any): boolean {
  const room = rooms.get(roomId);
  const idx = parseInt(targetIdx, 10);
  
  if (!room || isNaN(idx) || idx < 0 || idx >= 10) {
    logger.error('Room', `takeSeat 失败: 房间无效或索引无效 (${targetIdx})`);
    return false;
  }
  
  if (room.players[idx]) {
    logger.error('Room', `takeSeat 失败: 位置 ${idx} 已被占用`);
    return false;
  }

  let player: Player | null = null;

  // 1. 尝试从已有席位中移除
  const currentIdx = room.players.findIndex(p => p?.token === token);
  if (currentIdx !== -1) {
    player = room.players[currentIdx];
    room.players[currentIdx] = null;
    logger.info('Room', `玩家从旧席位 ${currentIdx} 移出`);
  } else {
    // 2. 尝试从观战区移除
    const specIdx = room.spectators.findIndex(p => p.token === token);
    if (specIdx !== -1) {
      player = room.spectators[specIdx];
      room.spectators.splice(specIdx, 1);
      logger.info('Room', `玩家从观战区移出`);
    }
  }

  if (player) {
    room.players[idx] = player;
    logger.info('Room', `玩家 ${player.playerName} 成功占领位置 ${idx}`);
    return true;
  }
  
  logger.error('Room', `takeSeat 失败: 找不到 token 为 ${token} 的玩家`);
  return false;
}

/** 退到观战区 */
export function leaveSeat(roomId: string, token: string): boolean {
  const room = rooms.get(roomId);
  if (!room) return false;

  const currentIdx = room.players.findIndex(p => p?.token === token);
  if (currentIdx === -1) return false;

  const player = room.players[currentIdx]!;
  room.players[currentIdx] = null;
  room.spectators.push(player);
  return true;
}

/** 重连处理 */
export function reconnectPlayer(token: string, newSocketId: string): { roomId: string; player: Player } | null {
  const roomId = tokenToRoom.get(token);
  const room = rooms.get(roomId || '');
  if (!room) return null;

  let player = room.players.find(p => p?.token === token) || room.spectators.find(p => p.token === token);
  if (!player) return null;

  player.socketId = newSocketId;
  player.connected = true;
  return { roomId: room.roomId, player };
}

/** 断开处理 */
export function handleDisconnect(socketId: string): string | null {
  for (const [roomId, room] of rooms) {
    const player = room.players.find(p => p?.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
    if (player) {
      player.connected = false;
      return roomId;
    }
  }
  return null;
}

/** 彻底移除 */
export function removePlayer(roomId: string, token: string): { destroyed: boolean } | null {
  const room = rooms.get(roomId);
  if (!room) return null;

  let removedPlayer: Player | null = null;
  const idx = room.players.findIndex(p => p?.token === token);
  if (idx !== -1) {
    removedPlayer = room.players[idx];
    room.players[idx] = null;
  } else {
    const sIdx = room.spectators.findIndex(p => p.token === token);
    if (sIdx !== -1) {
      removedPlayer = room.spectators[sIdx];
      room.spectators.splice(sIdx, 1);
    }
  }

  if (!removedPlayer) return null;
  tokenToRoom.delete(token);

  const activePlayers = [...room.players.filter(p => p !== null), ...room.spectators] as Player[];
  if (activePlayers.length === 0) {
    rooms.delete(roomId);
    return { destroyed: true };
  }

  if (removedPlayer.isHost) {
    const nextHost = activePlayers.find(p => p.connected) || activePlayers[0];
    nextHost.isHost = true;
  }
  return { destroyed: false };
}

export function destroyRoom(roomId: string): boolean {
  const room = rooms.get(roomId);
  if (!room) return false;

  // 清理所有玩家的 token 映射
  [...room.players, ...room.spectators].forEach(p => {
    if (p) tokenToRoom.delete(p.token);
  });

  rooms.delete(roomId);
  return true;
}

export function getRoom(roomId: string) {
  return rooms.get(roomId);
}

/** 获取可序列化的房间数据 (用于 socket 发送) */
export function getRoomData(roomId: string) {
  const room = rooms.get(roomId);
  if (!room) return null;
  return {
    ...room,
    votes: Array.from(room.votes.keys()), // 将 Map 转换为已投票者的 token 数组
  };
}
export function getPlayerList(roomId: string) { return rooms.get(roomId)?.players ?? []; }
export function getSpectators(roomId: string) { return rooms.get(roomId)?.spectators ?? []; }

export function findRoomBySocket(socketId: string): string | null {
  for (const [roomId, room] of rooms) {
    if (room.players.some(p => p?.socketId === socketId) || room.spectators.some(p => p.socketId === socketId)) {
      return roomId;
    }
  }
  return null;
}
