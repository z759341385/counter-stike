"use strict";
/**
 * 轮盘结算模块
 * 防操盘算法：完全由后端主导角度计算与落点判定
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.stopVoteAndSpin = stopVoteAndSpin;
const rooms_1 = require("./rooms");
const vote_1 = require("./vote");
const logger_1 = require("./logger");
function stopVoteAndSpin(roomId, socketId) {
    const room = (0, rooms_1.getRoom)(roomId);
    if (!room)
        return '房间不存在';
    const host = room.players.find(p => p && p.socketId === socketId);
    if (!host || !host.isHost)
        return '只有房主可以停止投票';
    if (room.status !== 'VOTING')
        return '当前不在投票阶段';
    const tally = (0, vote_1.getVoteTally)(roomId);
    if (!tally)
        return '无法获取投票数据';
    const options = room.voteOptions;
    const totalVotes = Array.from(tally.values()).reduce((s, v) => s + v, 0);
    const weights = options.map(opt => totalVotes === 0 ? 1 : (tally.get(opt.id) || 0));
    const totalWeight = weights.reduce((s, w) => s + w, 0);
    const sectors = [];
    let currentAngle = 0;
    for (let i = 0; i < options.length; i++) {
        const angle = (weights[i] / totalWeight) * 360;
        sectors.push({
            optionId: options[i].id,
            name: options[i].name,
            description: options[i].description,
            votes: tally.get(options[i].id) || 0,
            startAngle: currentAngle,
            endAngle: currentAngle + angle,
        });
        currentAngle += angle;
    }
    const targetAngle = Math.random() * 360;
    let winner = options[0];
    for (const sector of sectors) {
        if (targetAngle >= sector.startAngle && targetAngle < sector.endAngle) {
            winner = { id: sector.optionId, name: sector.name, description: sector.description };
            break;
        }
    }
    // 记录已使用规则
    if (!room.usedRuleIds.includes(winner.id)) {
        room.usedRuleIds.push(winner.id);
    }
    room.status = 'RESULT';
    logger_1.logger.info('Spin', `房间 ${roomId} | 角度: ${targetAngle.toFixed(2)}° | 获胜: ${winner.name}`);
    return { sectors, targetAngle, duration: 5000, winner };
}
//# sourceMappingURL=spin.js.map