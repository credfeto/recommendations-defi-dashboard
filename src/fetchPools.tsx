import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Pool, PoolsResponse } from './types/pools';

export const FetchPools: React.FC = () => {
  const [pools, setPools] = useState<Pool[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const url = 'https://yields.llama.fi/pools';
    
    axios
      .get<PoolsResponse>(url)
      .then((response) => {
        const poolData = response.data.data || [];
        setPools(poolData);
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

  return (
    <div className="pools-container">
      <h2>DeFi Pools</h2>
      <ul id="poolList">
        {pools.map((pool) => (
          <li key={pool.pool}>
            {pool.symbol} ({pool.chain}) - {pool.project} - ${pool.tvlUsd.toLocaleString()} TVL - APY: {pool.apy.toFixed(2)}%
          </li>
        ))}
      </ul>
    </div>
  );
};