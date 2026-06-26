"use strict";
/**
 * SQLite 数据库初始化与查询模块
 * 使用 better-sqlite3（同步 API）
 */
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.initDatabase = initDatabase;
exports.getActiveRules = getActiveRules;
exports.getRuleById = getRuleById;
exports.getAllRules = getAllRules;
exports.addRule = addRule;
exports.updateRule = updateRule;
exports.deleteRule = deleteRule;
exports.clearAllRules = clearAllRules;
exports.bulkAddRules = bulkAddRules;
exports.getDb = getDb;
const better_sqlite3_1 = __importDefault(require("better-sqlite3"));
const path_1 = __importDefault(require("path"));
const fs_1 = __importDefault(require("fs"));
const logger_1 = require("./logger");
const DB_DIR = path_1.default.resolve(__dirname, '..', 'data');
const DB_PATH = path_1.default.join(DB_DIR, 'rules.db');
let db;
/** 初始化数据库：建表 + 插入默认数据 */
function initDatabase() {
    // 确保 data 目录存在
    if (!fs_1.default.existsSync(DB_DIR)) {
        fs_1.default.mkdirSync(DB_DIR, { recursive: true });
        logger_1.logger.info('DB', `已创建数据目录: ${DB_DIR}`);
    }
    db = new better_sqlite3_1.default(DB_PATH);
    logger_1.logger.info('DB', `已连接数据库: ${DB_PATH}`);
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
    logger_1.logger.info('DB', 'rules 表已就绪');
    // 若表为空，插入 5 条默认规则
    const count = db.prepare('SELECT COUNT(*) AS cnt FROM rules').get();
    if (count.cnt === 0) {
        const insert = db.prepare('INSERT INTO rules (name, description, weight, is_active) VALUES (?, ?, ?, 1)');
        const defaultRules = [
            ['🤠 西部牛仔', '全员只能使用沙漠之鹰（Desert Eagle），不允许购买任何其他武器。考验你的手枪枪法！', 5],
            ['🔇 静音突击', '所有玩家禁止语音通讯，只能通过无线电指令交流。体验无声战场的紧张感！', 6],
            ['🌙 夜战模式', '所有玩家必须购买闪光弹并在每回合开局使用。模拟夜间作战的混乱场面！', 4],
            ['🔪 刀锋战士', '手枪回合！前 3 回合只能使用近战武器（刀）。活下来才是真英雄！', 7],
            ['🎯 狙击精英', '全员只能使用狙击步枪（AWP/SSG08），禁止购买步枪和冲锋枪。一枪定乾坤！', 5],
        ];
        const insertMany = db.transaction((rules) => {
            for (const [name, description, weight] of rules) {
                insert.run(name, description, weight);
            }
        });
        insertMany(defaultRules);
        logger_1.logger.info('DB', `已插入 ${defaultRules.length} 条默认规则`);
    }
}
/** 获取所有启用的规则 */
function getActiveRules() {
    return db.prepare('SELECT * FROM rules WHERE is_active = 1').all();
}
/** 根据 ID 获取单条规则 */
function getRuleById(id) {
    return db.prepare('SELECT * FROM rules WHERE id = ?').get(id);
}
/** 获取所有规则（包含禁用的） */
function getAllRules() {
    return db.prepare('SELECT * FROM rules').all();
}
/** 添加规则 */
function addRule(name, description, weight) {
    const info = db.prepare('INSERT INTO rules (name, description, weight, is_active) VALUES (?, ?, ?, 1)').run(name, description, weight);
    return info.lastInsertRowid;
}
/** 更新规则 */
function updateRule(id, name, description, weight, is_active) {
    const info = db.prepare('UPDATE rules SET name = ?, description = ?, weight = ?, is_active = ? WHERE id = ?').run(name, description, weight, is_active, id);
    return info.changes > 0;
}
/** 删除规则 */
function deleteRule(id) {
    const info = db.prepare('DELETE FROM rules WHERE id = ?').run(id);
    return info.changes > 0;
}
/** 清空所有规则 */
function clearAllRules() {
    db.prepare('DELETE FROM rules').run();
    // 重置自增 ID
    db.prepare("DELETE FROM sqlite_sequence WHERE name = 'rules'").run();
}
/** 批量添加规则 */
function bulkAddRules(rules) {
    const insert = db.prepare('INSERT INTO rules (name, description, weight, is_active) VALUES (?, ?, ?, ?)');
    const transaction = db.transaction((data) => {
        for (const rule of data) {
            insert.run(rule.name, rule.description, rule.weight || 5, rule.is_active ?? 1);
        }
    });
    transaction(rules);
}
/** 获取数据库实例（供高级用途） */
function getDb() {
    return db;
}
//# sourceMappingURL=db.js.map