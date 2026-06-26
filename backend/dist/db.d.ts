/**
 * SQLite 数据库初始化与查询模块
 * 使用 better-sqlite3（同步 API）
 */
import Database from 'better-sqlite3';
/** 规则数据结构 */
export interface Rule {
    id: number;
    name: string;
    description: string;
    weight: number;
    is_active: number;
}
/** 初始化数据库：建表 + 插入默认数据 */
export declare function initDatabase(): void;
/** 获取所有启用的规则 */
export declare function getActiveRules(): Rule[];
/** 根据 ID 获取单条规则 */
export declare function getRuleById(id: number): Rule | undefined;
/** 获取所有规则（包含禁用的） */
export declare function getAllRules(): Rule[];
/** 添加规则 */
export declare function addRule(name: string, description: string, weight: number): number;
/** 更新规则 */
export declare function updateRule(id: number, name: string, description: string, weight: number, is_active: number): boolean;
/** 删除规则 */
export declare function deleteRule(id: number): boolean;
/** 清空所有规则 */
export declare function clearAllRules(): void;
/** 批量添加规则 */
export declare function bulkAddRules(rules: any[]): void;
/** 获取数据库实例（供高级用途） */
export declare function getDb(): Database.Database;
//# sourceMappingURL=db.d.ts.map