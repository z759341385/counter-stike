/**
 * 轮盘结算模块
 * 防操盘算法：完全由后端主导角度计算与落点判定
 */
import { VoteOption } from './rooms';
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
export declare function stopVoteAndSpin(roomId: string, socketId: string): SpinResult | string;
//# sourceMappingURL=spin.d.ts.map