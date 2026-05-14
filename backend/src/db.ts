/**
 * SQLite 数据库初始化与查询模块
 * 使用 better-sqlite3（同步 API）
 */

import Database from 'better-sqlite3';
import path from 'path';
import fs from 'fs';
import { logger } from './logger';

/** 规则数据结构 */
export interface Rule {
  id: number;
  name: string;
  description: string;
  weight: number;
  is_active: number; // 0 = 禁用, 1 = 启用
}

const DB_DIR = path.resolve(__dirname, '..', 'data');
const DB_PATH = path.join(DB_DIR, 'rules.db');

let db: Database.Database;

/** 初始化数据库：建表 + 插入默认数据 */
export function initDatabase(): void {
  // 确保 data 目录存在
  if (!fs.existsSync(DB_DIR)) {
    fs.mkdirSync(DB_DIR, { recursive: true });
    logger.info('DB', `已创建数据目录: ${DB_DIR}`);
  }

  db = new Database(DB_PATH);
  logger.info('DB', `已连接数据库: ${DB_PATH}`);

  // 开启 WAL 模式，提升并发读写性能
  db.pragma('journal_mode = WAL');

  // 建表
  db.exec(`
    CREATE TABLE IF NOT EXISTS rules (
      id          INTEGER PRIMARY KEY AUTOINCREMENT,
      name        TEXT    NOT NULL,
      description TEXT    NOT NULL,
      weight      INTEGER NOT NULL DEFAULT 5,
      is_active   INTEGER NOT NULL DEFAULT 1
    );
  `);
  logger.info('DB', 'rules 表已就绪');

  // 若表为空，插入 5 条默认规则
  const count = db.prepare('SELECT COUNT(*) AS cnt FROM rules').get() as { cnt: number };
  if (count.cnt === 0) {
    const insert = db.prepare(
      'INSERT INTO rules (name, description, weight, is_active) VALUES (?, ?, ?, 1)'
    );

    const defaultRules: [string, string, number][] = [
      ['🤠 西部牛仔', '全员只能使用沙漠之鹰（Desert Eagle），不允许购买任何其他武器。考验你的手枪枪法！', 5],
      ['🔇 静音突击', '所有玩家禁止语音通讯，只能通过无线电指令交流。体验无声战场的紧张感！', 6],
      ['🌙 夜战模式', '所有玩家必须购买闪光弹并在每回合开局使用。模拟夜间作战的混乱场面！', 4],
      ['🔪 刀锋战士', '手枪回合！前 3 回合只能使用近战武器（刀）。活下来才是真英雄！', 7],
      ['🎯 狙击精英', '全员只能使用狙击步枪（AWP/SSG08），禁止购买步枪和冲锋枪。一枪定乾坤！', 5],
    ];

    const insertMany = db.transaction((rules: [string, string, number][]) => {
      for (const [name, description, weight] of rules) {
        insert.run(name, description, weight);
      }
    });

    insertMany(defaultRules);
    logger.info('DB', `已插入 ${defaultRules.length} 条默认规则`);
  }
}

/** 获取所有启用的规则 */
export function getActiveRules(): Rule[] {
  return db.prepare('SELECT * FROM rules WHERE is_active = 1').all() as Rule[];
}

/** 根据 ID 获取单条规则 */
export function getRuleById(id: number): Rule | undefined {
  return db.prepare('SELECT * FROM rules WHERE id = ?').get(id) as Rule | undefined;
}

/** 获取所有规则（包含禁用的） */
export function getAllRules(): Rule[] {
  return db.prepare('SELECT * FROM rules').all() as Rule[];
}

/** 添加规则 */
export function addRule(name: string, description: string, weight: number): number {
  const info = db.prepare('INSERT INTO rules (name, description, weight, is_active) VALUES (?, ?, ?, 1)').run(name, description, weight);
  return info.lastInsertRowid as number;
}

/** 更新规则 */
export function updateRule(id: number, name: string, description: string, weight: number, is_active: number): boolean {
  const info = db.prepare('UPDATE rules SET name = ?, description = ?, weight = ?, is_active = ? WHERE id = ?').run(name, description, weight, is_active, id);
  return info.changes > 0;
}

/** 删除规则 */
export function deleteRule(id: number): boolean {
  const info = db.prepare('DELETE FROM rules WHERE id = ?').run(id);
  return info.changes > 0;
}

/** 获取数据库实例（供高级用途） */
export function getDb(): Database.Database {
  return db;
}
