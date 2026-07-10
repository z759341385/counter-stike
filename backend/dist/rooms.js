"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.createRoom = createRoom;
exports.joinRoom = joinRoom;
exports.takeSeat = takeSeat;
exports.leaveSeat = leaveSeat;
exports.reconnectPlayer = reconnectPlayer;
exports.handleDisconnect = handleDisconnect;
exports.removePlayer = removePlayer;
exports.destroyRoom = destroyRoom;
exports.getRoom = getRoom;
exports.getRoomData = getRoomData;
exports.getPlayerList = getPlayerList;
exports.getSpectators = getSpectators;
exports.findRoomBySocket = findRoomBySocket;
const logger_1 = require("./logger");
const rooms = new Map();
const tokenToRoom = new Map();
function generateRoomId() {
    let id;
    do {
        id = String(Math.floor(1000 + Math.random() * 9000));
    } while (rooms.has(id));
    return id;
}
function generateToken() {
    return Math.random().toString(36).substring(2, 15);
}
/** 创建房间 */
function createRoom(hostSocketId, hostName, gameMode = 'ROULETTE', imposterCount = 1, mapName = 'de_dust2') {
    const roomId = generateRoomId();
    const token = generateToken();
    const room = {
        roomId,
        players: new Array(10).fill(null),
        spectators: [],
        gameMode,
        status: 'WAITING',
        voteOptions: [],
        votes: new Map(),
        usedRuleIds: [],
        imposterCount,
        imposters: [],
        imposterVotes: new Map(),
        mapName,
    };
    room.players[0] = { socketId: hostSocketId, token, playerName: hostName, isHost: true, connected: true };
    rooms.set(roomId, room);
    tokenToRoom.set(token, roomId);
    return { roomId, token };
}
/** 加入房间 */
function joinRoom(roomId, socketId, playerName) {
    const room = rooms.get(roomId);
    if (!room)
        return '房间不存在';
    const token = generateToken();
    room.spectators.push({ socketId, token, playerName, isHost: false, connected: true });
    tokenToRoom.set(token, roomId);
    return { token };
}
/** 坐下：从观战区或其他位置移到指定位置 */
function takeSeat(roomId, token, targetIdx) {
    const room = rooms.get(roomId);
    const idx = parseInt(targetIdx, 10);
    if (!room || isNaN(idx) || idx < 0 || idx >= 10) {
        logger_1.logger.error('Room', `takeSeat 失败: 房间无效或索引无效 (${targetIdx})`);
        return false;
    }
    if (room.players[idx]) {
        logger_1.logger.error('Room', `takeSeat 失败: 位置 ${idx} 已被占用`);
        return false;
    }
    let player = null;
    // 1. 尝试从已有席位中移除
    const currentIdx = room.players.findIndex(p => p?.token === token);
    if (currentIdx !== -1) {
        player = room.players[currentIdx];
        room.players[currentIdx] = null;
        logger_1.logger.info('Room', `玩家从旧席位 ${currentIdx} 移出`);
    }
    else {
        // 2. 尝试从观战区移除
        const specIdx = room.spectators.findIndex(p => p.token === token);
        if (specIdx !== -1) {
            player = room.spectators[specIdx];
            room.spectators.splice(specIdx, 1);
            logger_1.logger.info('Room', `玩家从观战区移出`);
        }
    }
    if (player) {
        room.players[idx] = player;
        logger_1.logger.info('Room', `玩家 ${player.playerName} 成功占领位置 ${idx}`);
        return true;
    }
    logger_1.logger.error('Room', `takeSeat 失败: 找不到 token 为 ${token} 的玩家`);
    return false;
}
/** 退到观战区 */
function leaveSeat(roomId, token) {
    const room = rooms.get(roomId);
    if (!room)
        return false;
    const currentIdx = room.players.findIndex(p => p?.token === token);
    if (currentIdx === -1)
        return false;
    const player = room.players[currentIdx];
    room.players[currentIdx] = null;
    room.spectators.push(player);
    return true;
}
/** 重连处理 */
function reconnectPlayer(token, newSocketId) {
    const roomId = tokenToRoom.get(token);
    const room = rooms.get(roomId || '');
    if (!room)
        return null;
    let player = room.players.find(p => p?.token === token) || room.spectators.find(p => p.token === token);
    if (!player)
        return null;
    player.socketId = newSocketId;
    player.connected = true;
    return { roomId: room.roomId, player };
}
/** 断开处理 */
function handleDisconnect(socketId) {
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
function removePlayer(roomId, token) {
    const room = rooms.get(roomId);
    if (!room)
        return null;
    let removedPlayer = null;
    const idx = room.players.findIndex(p => p?.token === token);
    if (idx !== -1) {
        removedPlayer = room.players[idx];
        room.players[idx] = null;
    }
    else {
        const sIdx = room.spectators.findIndex(p => p.token === token);
        if (sIdx !== -1) {
            removedPlayer = room.spectators[sIdx];
            room.spectators.splice(sIdx, 1);
        }
    }
    if (!removedPlayer)
        return null;
    tokenToRoom.delete(token);
    const activePlayers = [...room.players.filter(p => p !== null), ...room.spectators];
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
function destroyRoom(roomId) {
    const room = rooms.get(roomId);
    if (!room)
        return false;
    // 清理所有玩家的 token 映射
    [...room.players, ...room.spectators].forEach(p => {
        if (p)
            tokenToRoom.delete(p.token);
    });
    rooms.delete(roomId);
    return true;
}
function getRoom(roomId) {
    return rooms.get(roomId);
}
/** 获取可序列化的房间数据 (用于 socket 发送) */
function getRoomData(roomId) {
    const room = rooms.get(roomId);
    if (!room)
        return null;
    const { imposters, ...roomDataWithoutImposters } = room;
    return {
        ...roomDataWithoutImposters,
        votes: Array.from(room.votes.keys()), // 将 Map 转换为已投票者的 token 数组
        imposterVotes: Array.from(room.imposterVotes.entries()).map(([voter, targets]) => ({ voter, targets })), // 转换复杂的Map
    };
}
function getPlayerList(roomId) { return rooms.get(roomId)?.players ?? []; }
function getSpectators(roomId) { return rooms.get(roomId)?.spectators ?? []; }
function findRoomBySocket(socketId) {
    for (const [roomId, room] of rooms) {
        if (room.players.some(p => p?.socketId === socketId) || room.spectators.some(p => p.socketId === socketId)) {
            return roomId;
        }
    }
    return null;
}
//# sourceMappingURL=rooms.js.map