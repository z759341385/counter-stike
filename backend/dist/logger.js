"use strict";
/**
 * 简易日志模块
 * 后期可替换为 Winston / Pino 等专业日志库
 */
Object.defineProperty(exports, "__esModule", { value: true });
exports.logger = void 0;
function formatTime() {
    return new Date().toISOString();
}
function log(level, tag, message, data) {
    const prefix = `[${formatTime()}] [${level}] [${tag}]`;
    if (data !== undefined) {
        console.log(`${prefix} ${message}`, data);
    }
    else {
        console.log(`${prefix} ${message}`);
    }
}
exports.logger = {
    info: (tag, message, data) => log('INFO', tag, message, data),
    warn: (tag, message, data) => log('WARN', tag, message, data),
    error: (tag, message, data) => log('ERROR', tag, message, data),
    debug: (tag, message, data) => log('DEBUG', tag, message, data),
};
//# sourceMappingURL=logger.js.map