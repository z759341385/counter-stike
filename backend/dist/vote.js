"use strict";
/**
 * 投票模块
 * - 加权随机抽取规则
 * - 投票计分
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.weightedRandomPick = weightedRandomPick;
exports.startVote = startVote;
exports.submitVote = submitVote;
exports.getVoteTally = getVoteTally;
const db_1 = require("./db");
const rooms_1 = require("./rooms");
const logger_1 = require("./logger");
/**
 * 加权随机抽取 N 条不重复的规则
 */
function weightedRandomPick(rules, count) {
    if (rules.length <= count)
        return [...rules];
    const picked = [];
    const remaining = [...rules];
    for (let i = 0; i < count; i++) {
        const totalWeight = remaining.reduce((sum, r) => sum + r.weight, 0);
        let rand = Math.random() * totalWeight;
        for (let j = 0; j < remaining.length; j++) {
            rand -= remaining[j].weight;
            if (rand <= 0) {
                picked.push(remaining[j]);
                remaining.splice(j, 1);
                break;
            }
        }
    }
    return picked;
}
/**
 * 开始投票
 */
function startVote(roomId, socketId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    // 权限校验：仅房主
    const host = room.players.find(p => p && p.socketId === socketId);
    if (!host || !host.isHost)
        return '只有房主可以发起投票';
    // 状态校验
    if (room.status === 'VOTING')
        return '投票已在进行中';
    // 从数据库抽取
    const allActiveRules = (0, db_1.getActiveRules)();
    let availableRules = allActiveRules.filter(r => !room.usedRuleIds.includes(r.id));
    // 如果剩余规则不足 3 条，重置可用规则池
    if (availableRules.length < 3) {
        room.usedRuleIds = [];
        availableRules = allActiveRules;
    }
    if (availableRules.length < 3)
        return `启用的规则不足 3 条（当前 ${availableRules.length} 条）`;
    const picked = weightedRandomPick(availableRules, 3);
    const options = picked.map(r => ({
        id: r.id,
        name: r.name,
        description: r.description,
    }));
    // 更新房间状态
    room.status = 'VOTING';
    room.voteOptions = options;
    room.votes = new Map();
    logger_1.logger.info('Vote', `房间 ${roomId} 开始投票，候选规则: ${options.map(o => o.name).join(', ')}`);
    return options;
}
/**
 * 提交投票
 */
function submitVote(roomId, socketId, optionId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    if (room.status !== 'VOTING')
        return '当前不在投票阶段';
    // 找到对应的玩家 token
    const player = room.players.find(p => p && p.socketId === socketId);
    if (!player)
        return '玩家不在房间内';
    // 校验选项是否在候选列表中
    if (!room.voteOptions.some(o => o.id === optionId))
        return '无效的投票选项';
    // 记录（每人只能投一票，使用 token 作为 key 保证重连后票数依然有效）
    room.votes.set(player.token, optionId);
    const totalVotes = room.votes.size;
    logger_1.logger.info('Vote', `房间 ${roomId} 收到 ${player.playerName} 的投票，当前总票数: ${totalVotes}`);
    return { totalVotes };
}
/**
 * 获取各选项的得票统计
 */
function getVoteTally(roomId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return null;
    const tally = new Map();
    for (const opt of room.voteOptions) {
        tally.set(opt.id, 0);
    }
    for (const optionId of room.votes.values()) {
        tally.set(optionId, (tally.get(optionId) || 0) + 1);
    }
    return tally;
}
//# sourceMappingURL=vote.js.map