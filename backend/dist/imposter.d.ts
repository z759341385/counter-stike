/**
 * 分配内鬼
 */
export declare function assignImposters(roomId: string, socketId: string): string[] | string;
/**
 * 开始内鬼投票
 */
export declare function startImposterVote(roomId: string, socketId: string): boolean | string;
/**
 * 提交内鬼选票
 */
export declare function submitImposterVote(roomId: string, socketId: string, targetTokens: string[]): {
    totalVotes: number;
} | string;
/**
 * 结束内鬼投票，计算结果
 */
export declare function endImposterVote(roomId: string, socketId: string): any | string;
//# sourceMappingURL=imposter.d.ts.map