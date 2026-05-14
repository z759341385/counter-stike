/**
 * 轮盘结算模块
 * 防操盘算法：完全由后端主导角度计算与落点判定
 */

import { getRoom, VoteOption } from './rooms';
import { getVoteTally } from './vote';
import { logger } from './logger';

export interface Sector {
  optionId: number;
  name: string;
  description: string;
  votes: number;
  startAngle: number;
  endAngle: number;
}

export interface SpinResult {
  sectors: Sector[];
  targetAngle: number;
  duration: number;
  winner: VoteOption;
}

export function stopVoteAndSpin(roomId: string, socketId: string): SpinResult | string {
  const room = getRoom(roomId);
  if (!room) return '房间不存在';

  const host = room.players.find(p => p && p.socketId === socketId);
  if (!host || !host.isHost) return '只有房主可以停止投票';
  if (room.status !== 'VOTING') return '当前不在投票阶段';

  const tally = getVoteTally(roomId);
  if (!tally) return '无法获取投票数据';

  const options = room.voteOptions;
  const totalVotes = Array.from(tally.values()).reduce((s, v) => s + v, 0);

  const weights = options.map(opt => totalVotes === 0 ? 1 : (tally.get(opt.id) || 0));
  const totalWeight = weights.reduce((s, w) => s + w, 0);

  const sectors: Sector[] = [];
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

  let winner: VoteOption = options[0];
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

  logger.info('Spin', `房间 ${roomId} | 角度: ${targetAngle.toFixed(2)}° | 获胜: ${winner.name}`);

  return { sectors, targetAngle, duration: 5000, winner };
}
