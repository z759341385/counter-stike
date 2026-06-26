/**
 * 投票模块
 * - 加权随机抽取规则
 * - 投票计分
 */
import { Rule } from './db';
import { VoteOption } from './rooms';
/**
 * 加权随机抽取 N 条不重复的规则
 */
export declare function weightedRandomPick(rules: Rule[], count: number): Rule[];
/**
 * 开始投票
 */
export declare function startVote(roomId: string, socketId: string): VoteOption[] | string;
/**
 * 提交投票
 */
export declare function submitVote(roomId: string, socketId: string, optionId: number): {
    totalVotes: number;
} | string;
/**
 * 获取各选项的得票统计
 */
export declare function getVoteTally(roomId: string): Map<number, number> | null;
//# sourceMappingURL=vote.d.ts.map