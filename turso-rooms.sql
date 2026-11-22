-- ============================================================
-- 游戏房间存储（Turso）
-- 用途：持久化狼人杀 / 派对 / 谁是卧底 / 棋盘对局等房间，重启不丢失
-- 执行方式：在 Turso 控制台的 SQL 编辑器里整段粘贴执行即可（幂等，可重复执行）
-- ============================================================

-- 1) 通用房间表（所有房间类游戏共用）
CREATE TABLE IF NOT EXISTS GameRooms (
    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId      TEXT NOT NULL UNIQUE,        -- 房间码，如 ABCD-EFGH
    GameType    TEXT NOT NULL,               -- werewolf | party | spy | boardgame
    HostName    TEXT DEFAULT '',             -- 房主昵称
    HostUserId  INTEGER,                     -- 房主账号 Id（游客局可为空）
    PlayerCount INTEGER DEFAULT 0,           -- 当前人数
    MaxPlayers  INTEGER DEFAULT 12,          -- 人数上限
    Status      TEXT DEFAULT 'waiting',      -- waiting | playing | ended
    Phase       TEXT DEFAULT '',             -- 游戏阶段（如 NightWerewolf / DayVoting）
    StateJson   TEXT DEFAULT '',             -- 完整房间状态 JSON（玩家/角色/投票等，用于恢复）
    CreatedAt   TEXT,
    UpdatedAt   TEXT
);

CREATE INDEX IF NOT EXISTS idx_gamerooms_type   ON GameRooms(GameType);
CREATE INDEX IF NOT EXISTS idx_gamerooms_status ON GameRooms(Status);

-- 2) 狼人杀房间明细表（管理端按座位/角色查询用）
CREATE TABLE IF NOT EXISTS WerewolfRooms (
    Id         INTEGER PRIMARY KEY AUTOINCREMENT,
    RoomId     TEXT NOT NULL,
    SeatNumber INTEGER NOT NULL,             -- 座位号（1-12）
    PlayerId   TEXT NOT NULL,                -- 玩家唯一 Id
    Nickname   TEXT DEFAULT '',
    Role       TEXT DEFAULT '',              -- 角色名（Villager/Werewolf/Seer/...）
    IsAlive    INTEGER DEFAULT 1,
    IsAI       INTEGER DEFAULT 0,
    UpdatedAt  TEXT,
    UNIQUE(RoomId, SeatNumber)
);

CREATE INDEX IF NOT EXISTS idx_werewolfrooms_room ON WerewolfRooms(RoomId);
