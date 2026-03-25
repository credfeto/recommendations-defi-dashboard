# API Reference

## Base URL

```
http://localhost:5000
```

## Endpoints

### Get Pools by Type

**Endpoint:**

```
GET /api/pools/:poolName
```

**Description:**
Retrieve filtered pools of a specific type, with base filters applied (IL risk = "no", TVL ≥ $1M, APY > 0).

**Parameters:**

| Name     | Type   | Required | Description                                       |
| -------- | ------ | -------- | ------------------------------------------------- |
| poolName | string | Yes      | Pool type identifier (see Pool Types table below) |

**Pool Types:**

| Type             | ID         | Description                                    |
| ---------------- | ---------- | ---------------------------------------------- |
| ETH-Based Pools  | ETH        | Pools containing ETH or ETH derivatives        |
| Stablecoin Pools | STABLES    | Pools for stablecoin tokens                    |
| Liquid Staking   | LST        | Pools with staking tokens (stETH, rETH, cbETH) |
| High Yield       | HIGH_YIELD | Pools with APY > 5%                            |
| Emerging Pools   | LOW_TVL    | Pools with TVL < $10M                          |
| Blue Chip Pools  | BLUE_CHIP  | Pools with TVL > $100M                         |

**Example Requests:**

```bash
# Get ETH-based pools
curl http://localhost:5000/api/pools/ETH

# Get stablecoin pools
curl http://localhost:5000/api/pools/STABLES

# Get high-yield pools
curl http://localhost:5000/api/pools/HIGH_YIELD
```

**Success Response:**

**Status Code:** 200 OK

**Body:**

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
      "apyBase": 2.425,
      "apyReward": null,
      "ilRisk": "no",
      "stablecoin": false,
      "pool": "747c1d2a-c668-4682-b9f9-296708a3dd90",
      "exposure": "single"
    },
    {
      "symbol": "USDC",
      "chain": "Ethereum",
      "project": "circle",
      "tvlUsd": 10000000000,
      "apy": 3.5,
      "apyBase": 3.0,
      "apyReward": 0.5,
      "ilRisk": "no",
      "stablecoin": true,
      "pool": "abc123def456",
      "exposure": "multi"
    }
  ]
}
```

**Error Response (Invalid Pool Type):**

**Status Code:** 400 Bad Request

**Body:**

```json
{
  "error": "Invalid pool name. Use ETH or STABLES"
}
```

**Error Response (Server Error):**

**Status Code:** 500 Internal Server Error

**Body:**

```json
{
  "error": "Failed to fetch pools"
}
```

## Response Schema

### Pool Object

```typescript
interface Pool {
  symbol: string; // Token symbol (e.g., "STETH")
  chain: string; // Blockchain name (e.g., "Ethereum")
  project: string; // Protocol name (e.g., "lido")
  tvlUsd: number; // Total Value Locked in USD
  apy: number; // Annual Percentage Yield
  apyBase: number | null; // Base APY component
  apyReward: number | null; // Reward APY component
  ilRisk: string; // Impermanent Loss risk ("no", "yes")
  stablecoin: boolean; // Is stablecoin pool
  pool: string; // Unique pool identifier (UUID)
  exposure: string; // Exposure type ("single", "multi")
  [key: string]: any; // Additional fields from API
}
```

### Response Envelope

```typescript
interface ApiResponse {
  status: string; // "ok" for success
  data: Pool[]; // Array of filtered pools
}

interface ErrorResponse {
  error: string; // Error message
}
```

## Filtering Applied

All requests automatically apply these base filters:

1. **IL Risk**: `ilRisk === "no"` (no impermanent loss)
2. **TVL**: `tvlUsd >= 1,000,000` (minimum $1M)
3. **APY**: `apy > 0` (positive yields)

Additionally, each pool type applies its own predicate:

- **ETH**: Symbol contains "ETH"
- **STABLES**: `stablecoin === true`
- **LST**: Symbol includes stETH, rETH, cbETH, swell, or lsETH
- **HIGH_YIELD**: `apy > 5`
- **LOW_TVL**: `tvlUsd < 10,000,000`
- **BLUE_CHIP**: `tvlUsd > 100,000,000`

## Caching

- **TTL**: 1 hour
- **Type**: In-memory
- **Strategy**: First request fetches from external API, subsequent requests served from cache

## Rate Limiting

Currently no rate limiting implemented. The 1-hour cache reduces load on the upstream API.

## CORS

CORS is enabled for development:

```
Access-Control-Allow-Origin: *
```

## Pagination

Not currently supported. Endpoints return all results matching the criteria.

## Sorting

Not currently supported. Results are in the order returned by the upstream API.

## Example Usage

### JavaScript/TypeScript

```typescript
import axios from 'axios';

interface PoolsResponse {
  status: string;
  data: any[];
}

// Fetch ETH-based pools
const response = await axios.get<PoolsResponse>('http://localhost:5000/api/pools/ETH');

if (response.data.status === 'ok') {
  response.data.data.forEach((pool) => {
    console.log(`${pool.symbol}: ${pool.apy}% APY`);
  });
}
```

### cURL

```bash
# Get all liquid staking pools
curl -X GET http://localhost:5000/api/pools/LST | jq '.data[] | {symbol, apy, tvlUsd}'

# Get blue chip pools with high TVL
curl -X GET http://localhost:5000/api/pools/BLUE_CHIP | jq '.data | sort_by(.tvlUsd) | reverse | .[0:5]'
```

### Python

```python
import requests

response = requests.get('http://localhost:5000/api/pools/HIGH_YIELD')
pools = response.json()['data']

for pool in pools:
    print(f"{pool['symbol']}: {pool['apy']}% APY on {pool['chain']}")
```

## Troubleshooting

### "Invalid pool name" Error

**Cause**: Pool type ID doesn't exist or is misspelled

**Solution**: Use one of the valid pool type IDs: ETH, STABLES, LST, HIGH_YIELD, LOW_TVL, BLUE_CHIP

### Empty Results

**Cause**: No pools match the criteria for that type

**Possible**: The filters are very strict (must have IL risk = "no", TVL ≥ $1M, APY > 0)

### Server Error

**Cause**: Cannot fetch from upstream API or internal error

**Solution**:

1. Check that the server is running (`npm run server`)
2. Verify internet connection
3. Check server logs for details
4. Try again (cache may have expired)

## Versioning

Currently on v1.0.0. No API versioning implemented.

## Authentication

No authentication required for public endpoints.
