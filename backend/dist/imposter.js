"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.assignImposters = assignImposters;
exports.startImposterVote = startImposterVote;
exports.submitImposterVote = submitImposterVote;
exports.endImposterVote = endImposterVote;
const rooms_1 = require("./rooms");
const logger_1 = require("./logger");
/**
 * 分配内鬼
 */
function assignImposters(roomId, socketId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
    if (!host || !host.isHost)
        return '只有房主可以分配内鬼';
    const teamA = room.players.slice(0, 5).filter(p => p !== null);
    const teamB = room.players.slice(5, 10).filter(p => p !== null);
    if (teamA.length < room.imposterCount || teamB.length < room.imposterCount) {
        return `每队人数不足以分配 ${room.imposterCount} 名内鬼`;
    }
    const pickRandom = (arr, count) => {
        const shuffled = [...arr].sort(() => 0.5 - Math.random());
        return shuffled.slice(0, count).map(p => p.token);
    };
    const impostersA = pickRandom(teamA, room.imposterCount);
    const impostersB = pickRandom(teamB, room.imposterCount);
    room.imposters = [...impostersA, ...impostersB];
    room.status = 'IMPOSTER_ASSIGNED';
    logger_1.logger.info('Imposter', `房间 ${roomId} 已分配 ${room.imposters.length} 名内鬼`);
    return room.imposters;
}
/**
 * 开始内鬼投票
 */
function startImposterVote(roomId, socketId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
    if (!host || !host.isHost)
        return '只有房主可以发起投票';
    room.status = 'IMPOSTER_VOTING';
    room.imposterVotes = new Map();
    logger_1.logger.info('Imposter', `房间 ${roomId} 开始内鬼投票`);
    return true;
}
/**
 * 提交内鬼选票
 */
function submitImposterVote(roomId, socketId, targetTokens) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    if (room.status !== 'IMPOSTER_VOTING')
        return '当前不在内鬼投票阶段';
    const player = room.players.find(p => p && p.socketId === socketId);
    if (!player)
        return '您不在座位上，无法投票';
    if (!Array.isArray(targetTokens) || targetTokens.length > room.imposterCount || targetTokens.length === 0) {
        return `请选择 1 到 ${room.imposterCount} 名玩家进行投票`;
    }
    const playerIdx = room.players.findIndex(p => p && p.token === player.token);
    const isTeamA = playerIdx !== -1 && playerIdx < 5;
    for (const targetToken of targetTokens) {
        const targetPlayer = room.players.find(p => p && p.token === targetToken);
        if (!targetPlayer)
            return '部分投票目标不存在';
        const targetIdx = room.players.findIndex(p => p && p.token === targetToken);
        const isTargetTeamA = targetIdx !== -1 && targetIdx < 5;
        if (isTeamA !== isTargetTeamA) {
            return '只能投给本队成员';
        }
    }
    room.imposterVotes.set(player.token, targetTokens);
    logger_1.logger.info('Imposter', `房间 ${roomId}: ${player.playerName} 投了 ${targetTokens.length} 票`);
    return { totalVotes: room.imposterVotes.size };
}
/**
 * 结束内鬼投票，计算结果
 */
function endImposterVote(roomId, socketId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
    if (!host || !host.isHost)
        return '只有房主可以结束投票';
    room.status = 'IMPOSTER_RESULT';
    const tally = new Map();
    for (const targetTokens of room.imposterVotes.values()) {
        for (const targetToken of targetTokens) {
            tally.set(targetToken, (tally.get(targetToken) || 0) + 1);
        }
    }
    const getMostVoted = (startIdx, endIdx) => {
        let maxVotes = -1;
        let mostVotedToken = null;
        for (let i = startIdx; i < endIdx; i++) {
            const p = room.players[i];
            if (p) {
                const votes = tally.get(p.token) || 0;
                if (votes > maxVotes) {
                    maxVotes = votes;
                    mostVotedToken = p.token;
                }
            }
        }
        return { token: mostVotedToken, votes: maxVotes };
    };
    const teamAVoted = getMostVoted(0, 5);
    const teamBVoted = getMostVoted(5, 10);
    const result = {
        realImposters: room.imposters,
        votedOut: [teamAVoted.token, teamBVoted.token].filter(t => t !== null),
        tally: Array.from(tally.entries()).map(([target, votes]) => {
            const name = room.players.find(p => p?.token === target)?.playerName || 'Unknown';
            return { target, name, votes };
        })
    };
    logger_1.logger.info('Imposter', `房间 ${roomId} 结束投票，真实内鬼: ${room.imposters.join(',')}, 被投出: ${result.votedOut.join(',')}`);
    return result;
}
//# sourceMappingURL=imposter.js.map