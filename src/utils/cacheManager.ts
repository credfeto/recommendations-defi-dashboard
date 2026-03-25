const CACHE_KEY = 'llama_pools_cache';
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour in milliseconds

interface CacheEntry<T> {
  data: T;
  timestamp: number;
}

export const cacheManager = {
  get<T>(key: string): T | null {
    try {
      const entry = localStorage.getItem(key);
      if (!entry) return null;

      const cacheEntry: CacheEntry<T> = JSON.parse(entry);
      const now = Date.now();
      const isExpired = now - cacheEntry.timestamp > CACHE_TTL_MS;

      if (isExpired) {
        localStorage.removeItem(key);
        return null;
      }

      return cacheEntry.data;
    } catch (error) {
      console.error(`Error reading cache for key "${key}":`, error);
      return null;
    }
  },

  set<T>(key: string, data: T): void {
    try {
      const cacheEntry: CacheEntry<T> = {
        data,
        timestamp: Date.now(),
      };
      localStorage.setItem(key, JSON.stringify(cacheEntry));
    } catch (error) {
      console.error(`Error writing cache for key "${key}":`, error);
    }
  },

  clear(key: string): void {
    try {
      localStorage.removeItem(key);
    } catch (error) {
      console.error(`Error clearing cache for key "${key}":`, error);
    }
  },
};

export const getPoolsCacheKey = () => CACHE_KEY;
