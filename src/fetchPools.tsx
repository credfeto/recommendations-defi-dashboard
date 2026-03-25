import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Pool, PoolsResponse } from './types/pools';

export const FetchPools: React.FC = () => {
  const [ethPools, setEthPools] = useState<Pool[]>([]);
  const [stablePools, setStablePools] = useState<Pool[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchPools = async () => {
      try {
        const [ethResponse, stablesResponse] = await Promise.all([
          axios.get<PoolsResponse>('http://localhost:5000/api/pools/ETH'),
          axios.get<PoolsResponse>('http://localhost:5000/api/pools/STABLES'),
        ]);
        
        setEthPools(ethResponse.data.data || []);
        setStablePools(stablesResponse.data.data || []);
      } catch (err) {
        console.error('Error fetching pool data:', err);
        setError('Failed to fetch pool data');
      } finally {
        setLoading(false);
      }
    };

    fetchPools();
  }, []);

  if (loading) return <div>Loading pools...</div>;
  if (error) return <div className="error">{error}</div>;

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
      {renderTable('ETH-Based Pools', ethPools)}
      {renderTable('Stablecoin Pools', stablePools)}
    </div>
  );
};