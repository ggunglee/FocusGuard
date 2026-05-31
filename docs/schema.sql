-- targets 테이블: 웹사이트 또는 프로그램 타겟 정보
CREATE TABLE IF NOT EXISTS targets (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    type TEXT NOT NULL CHECK(type IN ('website', 'program')),
    name TEXT NOT NULL,
    url TEXT,
    executable_path TEXT,
    process_name TEXT,
    window_title_keyword TEXT,
    website_lock_rule TEXT CHECK(website_lock_rule IN ('exact', 'domain', 'origin_path')),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- sessions 테이블: 집중 시간 기록
CREATE TABLE IF NOT EXISTS sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    target_id INTEGER,
    mode TEXT NOT NULL CHECK(mode IN ('manual', 'auto')),
    planned_study_seconds INTEGER NOT NULL,
    actual_focused_seconds INTEGER DEFAULT 0,
    distracted_seconds INTEGER DEFAULT 0,
    break_seconds INTEGER DEFAULT 0,
    started_at DATETIME,
    ended_at DATETIME,
    completed BOOLEAN DEFAULT 0,
    emergency_unlock_used BOOLEAN DEFAULT 0,
    FOREIGN KEY(target_id) REFERENCES targets(id)
);

-- daily_stats 테이블: 대시보드용 일간 통계 캐시
CREATE TABLE IF NOT EXISTS daily_stats (
    date TEXT PRIMARY KEY,
    total_focused_seconds INTEGER DEFAULT 0,
    total_sessions INTEGER DEFAULT 0,
    completed_sessions INTEGER DEFAULT 0,
    early_unlocks INTEGER DEFAULT 0
);

-- settings 테이블: 사용자 설정
CREATE TABLE IF NOT EXISTS settings (
    key TEXT PRIMARY KEY,
    value TEXT NOT NULL
);

-- 기본 설정값 삽입
INSERT OR IGNORE INTO settings (key, value) VALUES ('default_study_minutes', '50');
INSERT OR IGNORE INTO settings (key, value) VALUES ('default_break_minutes', '10');
INSERT OR IGNORE INTO settings (key, value) VALUES ('default_cycles', '3');
INSERT OR IGNORE INTO settings (key, value) VALUES ('launch_fullscreen', 'true');
INSERT OR IGNORE INTO settings (key, value) VALUES ('always_on_top_during_study', 'true');
INSERT OR IGNORE INTO settings (key, value) VALUES ('emergency_unlock_enabled', 'true');
