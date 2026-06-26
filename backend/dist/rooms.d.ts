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
export type RoomStatus = 'WAITING' | 'VOTING' | 'SPINNING' | 'RESULT' | 'IMPOSTER_ASSIGNED' | 'IMPOSTER_VOTING' | 'IMPOSTER_RESULT';
/** 游戏模式 */
export type GameMode = 'ROULETTE' | 'IMPOSTER';
/** 房间完整状态 */
export interface RoomState {
    roomId: string;
    players: (Player | null)[];
    spectators: Player[];
    gameMode: GameMode;
    status: RoomStatus;
    voteOptions: VoteOption[];
    votes: Map<string, number>;
    lastSpinResult?: any;
    usedRuleIds: number[];
    imposterCount: number;
    imposters: string[];
    imposterVotes: Map<string, string[]>;
}
/** 创建房间 */
export declare function createRoom(hostSocketId: string, hostName: string, gameMode?: GameMode, imposterCount?: number): {
    roomId: string;
    token: string;
};
/** 加入房间 */
export declare function joinRoom(roomId: string, socketId: string, playerName: string): {
    token: string;
} | string;
/** 坐下：从观战区或其他位置移到指定位置 */
export declare function takeSeat(roomId: string, token: string, targetIdx: any): boolean;
/** 退到观战区 */
export declare function leaveSeat(roomId: string, token: string): boolean;
/** 重连处理 */
export declare function reconnectPlayer(token: string, newSocketId: string): {
    roomId: string;
    player: Player;
} | null;
/** 断开处理 */
export declare function handleDisconnect(socketId: string): string | null;
/** 彻底移除 */
export declare function removePlayer(roomId: string, token: string): {
    destroyed: boolean;
} | null;
export declare function destroyRoom(roomId: string): boolean;
export declare function getRoom(roomId: string): RoomState | undefined;
/** 获取可序列化的房间数据 (用于 socket 发送) */
export declare function getRoomData(roomId: string): {
    votes: string[];
    imposterVotes: {
        voter: string;
        targets: string[];
    }[];
    roomId: string;
    players: (Player | null)[];
    spectators: Player[];
    gameMode: GameMode;
    status: RoomStatus;
    voteOptions: VoteOption[];
    lastSpinResult?: any;
    usedRuleIds: number[];
    imposterCount: number;
} | null;
export declare function getPlayerList(roomId: string): (Player | null)[];
export declare function getSpectators(roomId: string): Player[];
export declare function findRoomBySocket(socketId: string): string | null;
//# sourceMappingURL=rooms.d.ts.map