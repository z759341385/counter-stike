/**
 * 简易日志模块
 * 后期可替换为 Winston / Pino 等专业日志库
 */

type LogLevel = 'INFO' | 'WARN' | 'ERROR' | 'DEBUG';

function formatTime(): string {
  return new Date().toISOString();
}

function log(level: LogLevel, tag: string, message: string, data?: unknown): void {
  const prefix = `[${formatTime()}] [${level}] [${tag}]`;
  if (data !== undefined) {
    console.log(`${prefix} ${message}`, data);
  } else {
    console.log(`${prefix} ${message}`);
  }
}

export const logger = {
  info: (tag: string, message: string, data?: unknown) => log('INFO', tag, message, data),
  warn: (tag: string, message: string, data?: unknown) => log('WARN', tag, message, data),
  error: (tag: string, message: string, data?: unknown) => log('ERROR', tag, message, data),
  debug: (tag: string, message: string, data?: unknown) => log('DEBUG', tag, message, data),
};
