/**
 * 简易日志模块
 * 后期可替换为 Winston / Pino 等专业日志库
 */
export declare const logger: {
    info: (tag: string, message: string, data?: unknown) => void;
    warn: (tag: string, message: string, data?: unknown) => void;
    error: (tag: string, message: string, data?: unknown) => void;
    debug: (tag: string, message: string, data?: unknown) => void;
};
//# sourceMappingURL=logger.d.ts.map