import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Pool, PoolsResponse } from './types/pools';
import { cacheManager, getPoolsCacheKey } from './utils/cacheManager';

export const FetchPools: React.FC = () => {
  const [pools, setPools] = useState<Pool[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const url = 'https://yields.llama.fi/pools';
    const cacheKey = getPoolsCacheKey();
    
    // Try to get cached data first
    const cachedData = cacheManager.get<PoolsResponse>(cacheKey);
    if (cachedData) {
      const poolData = cachedData.data || [];
      setPools(poolData);
      setLoading(false);
      return;
    }
    
    // Fetch new data if cache is empty or expired
    axios
      .get<PoolsResponse>(url)
      .then((response) => {
        const poolData = response.data.data || [];
        setPools(poolData);
        cacheManager.set(cacheKey, response.data);
      })
      .catch((err) => {
        console.error('Error fetching pool data:', err);
        setError('Failed to fetch pool data');
      })
      .finally(() => {
        setLoading(false);
      });
  }, []);

  if (loading) return <div>Loading pools...</div>;
  if (error) return <div className="error">{error}</div>;

  const MIN_TVL = 1_000_000;

  const filterPools = (poolData: Pool[]): Pool[] => {
    return poolData.filter(pool => 
      pool.ilRisk === 'no' && 
      pool.tvlUsd >= MIN_TVL && 
      pool.apy > 0
    );
  };

  const ethereumPools = filterPools(pools.filter(pool => pool.symbol.toUpperCase().includes('ETH')));
  const stablecoinPools = filterPools(pools.filter(pool => pool.stablecoin));

  const renderTable = (tableTitle: string, poolData: Pool[]) => (
    <div className="table-section">
      <h3>{tableTitle}</h3>
      {poolData.length === 0 ? (
        <p>No pools found</p>
      ) : (
        <table className="pools-table">
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Chain</th>
              <th>Project</th>
              <th>TVL (USD)</th>
              <th>APY (%)</th>
              <th>APY Base</th>
              <th>APY Reward</th>
              <th>Stablecoin</th>
              <th>IL Risk</th>
            </tr>
          </thead>
          <tbody>
            {poolData.map((pool) => (
              <tr key={pool.pool}>
                <td>{pool.symbol}</td>
                <td>{pool.chain}</td>
                <td>{pool.project}</td>
                <td>${pool.tvlUsd.toLocaleString()}</td>
                <td>{pool.apy.toFixed(2)}%</td>
                <td>{pool.apyBase !== null ? pool.apyBase.toFixed(2) : '-'}%</td>
                <td>{pool.apyReward !== null ? pool.apyReward.toFixed(2) : '-'}%</td>
                <td>{pool.stablecoin ? 'Yes' : 'No'}</td>
                <td>{pool.ilRisk}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );

  return (
    <div className="pools-container">
      <h2>DeFi Pools</h2>
      {renderTable('ETH-Based Pools', ethereumPools)}
      {renderTable('Stablecoin Pools', stablecoinPools)}
    </div>
  );
};