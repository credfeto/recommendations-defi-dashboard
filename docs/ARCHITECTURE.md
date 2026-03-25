# System Architecture

## Overview

The DeFi Pools Dashboard is a full-stack TypeScript application that displays liquidity pools from the Llama Yields API, filtered by various criteria and organized by pool type.

## Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                     FRONTEND (React)                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  FetchPools.tsx              FetchPools.css                     │
│  ────────────                ──────────────                      │
│  • Sidebar nav              • Grid layout                        │
│  • Pool selector            • Responsive                        │
│  • Data fetching            • Animations                        │
│  • Stats display                                                 │
│           │                                                      │
│           ├─► poolTypes.ts                                      │
│           │   (Pool type config)                                │
│           │                                                      │
│           └─► axios.get('/api/pools/:type')                    │
│                                                                  │
└────────────────────────┬─────────────────────────────────────────┘
                         │ HTTP
                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                    BACKEND (Fastify)                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  server-fastify.ts           server.ts                          │
│  ──────────────               ────────                           │
│  GET /api/pools/:poolName     • applyBaseFilters()            │
│  ├─ Route handler             • filterPoolsByType()           │
│  ├─ Cache management          • getAvailableTypes()           │
│  └─ Error handling            • getFilteredPools()            │
│           │                                                      │
│           ├─► poolTypes.ts                                      │
│           │   (Pool config + predicates)                        │
│           │                                                      │
│           └─► axios.get('https://yields.llama.fi/pools')      │
│                                                                  │
│  CACHE: 1-hour TTL in-memory cache                             │
│                                                                  │
└────────────────────────┬─────────────────────────────────────────┘
                         │
                         ▼
         External API: yields.llama.fi/pools
```

## Data Flow

### Pool Type Selection Flow

```
1. User clicks pool type button
      ▼
2. Component calls: GET /api/pools/{TYPE}
      ▼
3. Server receives request
      ▼
4. Server calls filterPoolsByType(allPools, TYPE)
      ├─► Gets PoolTypeConfig from poolTypes.ts
      ├─► Applies type predicate
      ├─► Applies base filters (IL risk, TVL, APY)
      ▼
5. Server returns filtered pools
      ▼
6. Component receives data
      ▼
7. Component renders table + statistics
      ▼
8. User sees results
```

## Module Organization

### Frontend Modules

**src/FetchPools.tsx**
- Main React component
- Manages pool type selection state
- Fetches data from backend
- Renders sidebar and table

**src/types/poolTypes.ts**
- Pool type configuration
- 6 built-in pool types
- Helper functions for type discovery

**src/FetchPools.css**
- Component styling
- CSS Grid layout
- Responsive design

### Backend Modules

**src/server-fastify.ts**
- Fastify server setup
- Route handlers
- CORS middleware
- Request logging

**src/server.ts**
- Pool filtering logic
- Cache management
- Filter functions:
  - `applyBaseFilters()` - Apply minimum criteria
  - `filterPoolsByType()` - Filter by pool type
  - `getAvailableTypes()` - List all types
  - `getFilteredPools()` - Wrapper function

**src/types/poolTypes.ts**
- Shared type definitions
- Pool type configuration
- Type predicates

### Testing

**src/__tests__/server.test.ts**
- 26 comprehensive unit tests
- 100% code coverage
- Tests for all pool types
- Edge case testing

## Base Filters

All pool types automatically apply these criteria:

- **IL Risk**: Must be "no" (no impermanent loss)
- **TVL**: Must be ≥ $1,000,000 (minimum liquidity)
- **APY**: Must be > 0 (positive yield)

## Built-in Pool Types

| Type | Filter | Use Case |
|------|--------|----------|
| ETH | Symbol contains "ETH" | Ethereum-based yields |
| STABLES | stablecoin = true | Stablecoin pools |
| LST | stETH, rETH, cbETH, etc. | Liquid staking yields |
| HIGH_YIELD | APY > 5% | Aggressive yield seeking |
| LOW_TVL | TVL < $10M | Early stage opportunities |
| BLUE_CHIP | TVL > $100M | Conservative, safe pools |

## API Endpoints

### Get Pools by Type

```
GET /api/pools/:poolName
```

**Parameters:**
- `poolName` (string): Pool type ID (ETH, STABLES, LST, HIGH_YIELD, LOW_TVL, BLUE_CHIP)

**Response:**
```json
{
  "status": "ok",
  "data": [
    {
      "symbol": "STETH",
      "chain": "Ethereum",
      "project": "lido",
      "tvlUsd": 19891310603,
      "apy": 2.425,
      "ilRisk": "no",
      "stablecoin": false,
      "pool": "747c1d2a-c668-4682-b9f9-296708a3dd90"
    }
  ]
}
```

**Error Response:**
```json
{
  "error": "Invalid pool name. Use ETH or STABLES"
}
```

## Cache Management

The backend implements a 1-hour TTL in-memory cache:

- First request: Fetches from Llama API, stores in cache
- Subsequent requests (within 1 hour): Returned from cache
- After 1 hour: Cache expires, refetches from API

```typescript
const CACHE_TTL_MS = 60 * 60 * 1000; // 1 hour
```

## Deployment Architecture

### Development
```
npm run dev
├─ npm run server    (Fastify on port 5000)
└─ npm start         (React on port 3000)
```

### Production
```
Build frontend:  npm run build     → ./build/
Run backend:     npm run server    (Production mode)
Serve frontend:  Static hosting (Vercel, Netlify, etc.)
```

## Technology Stack

### Frontend
- React 19.2.4
- TypeScript 4.9.5
- Axios 1.13.6
- CSS Grid

### Backend
- Fastify 5.8.4
- @fastify/cors 11.2.0
- TypeScript 4.9.5
- Node.js runtime

### Testing
- Jest 30.3.0
- ts-jest 29.4.6
- 26 tests, 100% coverage

## Performance Considerations

1. **Caching**: 1-hour TTL reduces API calls
2. **Parallel fetching**: Frontend fetches all pool types simultaneously
3. **Filtering on server**: Reduces payload size
4. **CSS Grid**: Efficient responsive layouts
5. **Type predicates**: O(n) filtering per pool type

## Security

- CORS enabled for development (`{ origin: true }`)
- No sensitive data in frontend code
- Environment variables for secrets (PORT)
- Input validation on pool type parameter

## Error Handling

- Invalid pool type → 400 Bad Request
- API fetch failure → 500 Internal Server Error
- Frontend catches errors → Shows error message to user

## Future Enhancements

- Redis caching for multi-instance deployments
- Database persistence of cache
- Real-time updates via WebSocket
- More advanced filtering options
- Pool-specific detail views
- Historical data tracking
- User preferences/bookmarks
