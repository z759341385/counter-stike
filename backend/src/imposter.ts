import { getRoom, Player } from './rooms';
import { logger } from './logger';

/**
 * 分配内鬼
 */
export function assignImposters(roomId: string, socketId: string): string[] | string {
  const room = getRoom(roomId);
  if (!room) return '房间不存在';

  const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
  if (!host || !host.isHost) return '只有房主可以分配内鬼';

  const teamA = room.players.slice(0, 5).filter(p => p !== null) as Player[];
  const teamB = room.players.slice(5, 10).filter(p => p !== null) as Player[];

  if (teamA.length < room.imposterCount || teamB.length < room.imposterCount) {
    return `每队人数不足以分配 ${room.imposterCount} 名内鬼`;
  }

  const pickRandom = (arr: Player[], count: number) => {
    const shuffled = [...arr].sort(() => 0.5 - Math.random());
    return shuffled.slice(0, count).map(p => p.token);
  };

  const impostersA = pickRandom(teamA, room.imposterCount);
  const impostersB = pickRandom(teamB, room.imposterCount);

  room.imposters = [...impostersA, ...impostersB];
  room.status = 'IMPOSTER_ASSIGNED';
  
  logger.info('Imposter', `房间 ${roomId} 已分配 ${room.imposters.length} 名内鬼`);
  return room.imposters;
}

/**
 * 开始内鬼投票
 */
export function startImposterVote(roomId: string, socketId: string): boolean | string {
  const room = getRoom(roomId);
  if (!room) return '房间不存在';

  const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
  if (!host || !host.isHost) return '只有房主可以发起投票';

  room.status = 'IMPOSTER_VOTING';
  room.imposterVotes = new Map();
  logger.info('Imposter', `房间 ${roomId} 开始内鬼投票`);
  return true;
}

/**
 * 提交内鬼选票
 */
export function submitImposterVote(roomId: string, socketId: string, targetTokens: string[]): { totalVotes: number } | string {
  const room = getRoom(roomId);
  if (!room) return '房间不存在';
  if (room.status !== 'IMPOSTER_VOTING') return '当前不在内鬼投票阶段';

  const player = room.players.find(p => p && p.socketId === socketId);
  if (!player) return '您不在座位上，无法投票';

  if (!Array.isArray(targetTokens) || targetTokens.length > room.imposterCount || targetTokens.length === 0) {
    return `请选择 1 到 ${room.imposterCount} 名玩家进行投票`;
  }

  const playerIdx = room.players.findIndex(p => p && p.token === player.token);
  const isTeamA = playerIdx !== -1 && playerIdx < 5;

  for (const targetToken of targetTokens) {
    const targetPlayer = room.players.find(p => p && p.token === targetToken);
    if (!targetPlayer) return '部分投票目标不存在';

    const targetIdx = room.players.findIndex(p => p && p.token === targetToken);
    const isTargetTeamA = targetIdx !== -1 && targetIdx < 5;
    
    if (isTeamA !== isTargetTeamA) {
      return '只能投给本队成员';
    }
  }

  room.imposterVotes.set(player.token, targetTokens);
  logger.info('Imposter', `房间 ${roomId}: ${player.playerName} 投了 ${targetTokens.length} 票`);
  return { totalVotes: room.imposterVotes.size };
}

/**
 * 结束内鬼投票，计算结果
 */
export function endImposterVote(roomId: string, socketId: string): any | string {
  const room = getRoom(roomId);
  if (!room) return '房间不存在';

  const host = room.players.find(p => p && p.socketId === socketId) || room.spectators.find(p => p.socketId === socketId);
  if (!host || !host.isHost) return '只有房主可以结束投票';

  room.status = 'IMPOSTER_RESULT';

  const tally = new Map<string, number>();
  for (const targetTokens of room.imposterVotes.values()) {
    for (const targetToken of targetTokens) {
      tally.set(targetToken, (tally.get(targetToken) || 0) + 1);
    }
  }

  const getMostVoted = (startIdx: number, endIdx: number) => {
    let maxVotes = -1;
    let mostVotedToken: string | null = null;
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

  logger.info('Imposter', `房间 ${roomId} 结束投票，真实内鬼: ${room.imposters.join(',')}, 被投出: ${result.votedOut.join(',')}`);
  return result;
}
