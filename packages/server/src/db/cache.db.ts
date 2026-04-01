import Database from 'better-sqlite3';
import path from 'path';
import fs from 'fs';
import { ContractSecurityInfo } from '@shared';

const DB_DIR = path.join(__dirname, '../../data');
const DB_PATH = path.join(DB_DIR, 'cache.db');

/** Re-fetch if data is older than this */
const FRESH_TTL_MS = 60 * 60 * 1000; // 1 hour
/** Use stale data up to this age if a fresh fetch fails */
const MAX_STALE_MS = 2 * 60 * 60 * 1000; // 2 hours
/** Contract security data is stable; re-check after 24 hours */
export const CONTRACT_SECURITY_TTL_MS = 24 * 60 * 60 * 1000;

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

db.exec(`
  CREATE TABLE IF NOT EXISTS contract_security (
    chain                       TEXT    NOT NULL,
    address                     TEXT    NOT NULL,
    parent_address              TEXT,
    is_open_source              INTEGER,
    is_honeypot                 INTEGER,
    is_proxy                    INTEGER,
    buy_tax                     REAL,
    sell_tax                    REAL,
    transfer_tax                REAL,
    cannot_buy                  INTEGER,
    honeypot_with_same_creator  INTEGER,
    token_name                  TEXT,
    token_symbol                TEXT,
    checked_at                  INTEGER NOT NULL,
    PRIMARY KEY (chain, address)
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

// ── Contract security table ────────────────────────────────────────────────

interface ContractSecurityRow {
  chain: string;
  address: string;
  parent_address: string | null;
  is_open_source: number | null;
  is_honeypot: number | null;
  is_proxy: number | null;
  buy_tax: number | null;
  sell_tax: number | null;
  transfer_tax: number | null;
  cannot_buy: number | null;
  honeypot_with_same_creator: number | null;
  token_name: string | null;
  token_symbol: string | null;
  checked_at: number;
}

function rowToInfo(row: ContractSecurityRow): ContractSecurityInfo & { checkedAt: number } {
  return {
    chain: row.chain,
    address: row.address,
    parentAddress: row.parent_address,
    isOpenSource: row.is_open_source,
    isHoneypot: row.is_honeypot,
    isProxy: row.is_proxy,
    buyTax: row.buy_tax,
    sellTax: row.sell_tax,
    transferTax: row.transfer_tax,
    cannotBuy: row.cannot_buy,
    honeypotWithSameCreator: row.honeypot_with_same_creator,
    tokenName: row.token_name,
    tokenSymbol: row.token_symbol,
    checkedAt: row.checked_at,
  };
}

export function getContractSecurity(
  chain: string,
  address: string,
): (ContractSecurityInfo & { checkedAt: number }) | null {
  const row = db
    .prepare<[string, string], ContractSecurityRow>(
      'SELECT * FROM contract_security WHERE chain = ? AND address = ?',
    )
    .get(chain, address.toLowerCase());
  return row ? rowToInfo(row) : null;
}

export function setContractSecurity(info: ContractSecurityInfo): void {
  db.prepare(
    `INSERT OR REPLACE INTO contract_security
      (chain, address, parent_address, is_open_source, is_honeypot, is_proxy,
       buy_tax, sell_tax, transfer_tax, cannot_buy, honeypot_with_same_creator,
       token_name, token_symbol, checked_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
  ).run(
    info.chain,
    info.address.toLowerCase(),
    info.parentAddress ?? null,
    info.isOpenSource ?? null,
    info.isHoneypot ?? null,
    info.isProxy ?? null,
    info.buyTax ?? null,
    info.sellTax ?? null,
    info.transferTax ?? null,
    info.cannotBuy ?? null,
    info.honeypotWithSameCreator ?? null,
    info.tokenName ?? null,
    info.tokenSymbol ?? null,
    Date.now(),
  );
}

/** Return all rows whose parent_address matches the given proxy address. */
export function getContractSecurityChildren(
  chain: string,
  parentAddress: string,
): (ContractSecurityInfo & { checkedAt: number })[] {
  const rows = db
    .prepare<[string, string], ContractSecurityRow>(
      'SELECT * FROM contract_security WHERE chain = ? AND parent_address = ?',
    )
    .all(chain, parentAddress.toLowerCase());
  return rows.map(rowToInfo);
}
