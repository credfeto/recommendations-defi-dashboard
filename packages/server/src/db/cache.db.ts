import Database from 'better-sqlite3';
import path from 'path';
import fs from 'fs';

const DB_DIR = path.join(__dirname, '../../data');
const DB_PATH = path.join(DB_DIR, 'cache.db');

/** Re-fetch if data is older than this */
const FRESH_TTL_MS = 60 * 60 * 1000; // 1 hour
/** Use stale data up to this age if a fresh fetch fails */
const MAX_STALE_MS = 2 * 60 * 60 * 1000; // 2 hours

if (!fs.existsSync(DB_DIR)) {
  fs.mkdirSync(DB_DIR, { recursive: true });
}

const db = new Database(DB_PATH);

db.exec(`
  CREATE TABLE IF NOT EXISTS api_cache (
    key        TEXT PRIMARY KEY,
    data       TEXT    NOT NULL,
    fetched_at INTEGER NOT NULL
  )
`);

interface CacheRow {
  data: string;
  fetched_at: number;
}

export function getCached<T>(key: string): { data: T; age: number } | null {
  const row = db.prepare<string, CacheRow>('SELECT data, fetched_at FROM api_cache WHERE key = ?').get(key);
  if (!row) return null;
  return { data: JSON.parse(row.data) as T, age: Date.now() - row.fetched_at };
}

export function setCached<T>(key: string, data: T): void {
  db.prepare('INSERT OR REPLACE INTO api_cache (key, data, fetched_at) VALUES (?, ?, ?)').run(
    key,
    JSON.stringify(data),
    Date.now(),
  );
}

export function isFresh(age: number): boolean {
  return age < FRESH_TTL_MS;
}

export function isUsable(age: number): boolean {
  return age < MAX_STALE_MS;
}

/**
 * Returns cached data if fresh (< 1h). Otherwise calls fetcher to get new data
 * and updates the cache. If the fetch fails and stale data exists (< 2h), the
 * stale data is returned. If the fetch fails and data is > 2h old (or absent),
 * the error is re-thrown.
 */
export async function getCachedOrFetch<T>(key: string, fetcher: () => Promise<T>): Promise<T> {
  const cached = getCached<T>(key);

  if (cached && isFresh(cached.age)) {
    return cached.data;
  }

  try {
    const data = await fetcher();
    setCached(key, data);
    return data;
  } catch (err) {
    if (cached && isUsable(cached.age)) {
      return cached.data;
    }
    throw err;
  }
}
