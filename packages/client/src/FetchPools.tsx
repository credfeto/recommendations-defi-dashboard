import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Pool, PoolsResponse } from './types/pools';
import { PoolTypeConfig, getAvailablePoolTypes } from './types/poolTypes';
import './FetchPools.css';

interface PoolsByType {
  [typeId: string]: Pool[];
}

export const FetchPools: React.FC = () => {
  const [poolsByType, setPoolsByType] = useState<PoolsByType>({});
  const [selectedType, setSelectedType] = useState<string>('ETH');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [poolTypes] = useState<PoolTypeConfig[]>(getAvailablePoolTypes());
  const [isDarkMode, setIsDarkMode] = useState(window.matchMedia('(prefers-color-scheme: dark)').matches);

  useEffect(() => {
    const fetchPools = async () => {
      try {
        const requests = poolTypes.map((type) =>
          axios
            .get<PoolsResponse>(`http://localhost:5000/api/pools/${type.id}`)
            .then((res) => ({ typeId: type.id, data: res.data.data || [] }))
            .catch(() => ({ typeId: type.id, data: [] })),
        );

        const results = await Promise.all(requests);
        const pools: PoolsByType = {};

        results.forEach(({ typeId, data }) => {
          pools[typeId] = data;
        });

        setPoolsByType(pools);
      } catch (err) {
        console.error('Error fetching pool data:', err);
        setError('Failed to fetch pool data');
      } finally {
        setLoading(false);
      }
    };

    fetchPools();
  }, [poolTypes]);

  const handleThemeToggle = () => {
    const newMode = !isDarkMode;
    setIsDarkMode(newMode);

    if (newMode) {
      document.documentElement.style.colorScheme = 'dark';
    } else {
      document.documentElement.style.colorScheme = 'light';
    }
  };

  if (loading) return <div className='loading'>Loading pools...</div>;
  if (error) return <div className='error-message'>{error}</div>;

  const currentType = poolTypes.find((t) => t.id === selectedType);
  const currentPools = poolsByType[selectedType] || [];

  return (
    <div className='pools-container'>
      <header className='pools-header'>
        <h1>🏊‍♂️ DeFi Pools Dashboard</h1>
        <p className='subtitle'>Filtered pools with no IL risk, greater than $1M TVL, and positive APY</p>
      </header>

      <div className='pools-content'>
        {/* Pool Type Selector */}
        <aside className='pool-types-sidebar'>
          <h3>Pool Types</h3>
          <nav className='pool-types-nav'>
            {poolTypes.map((type) => (
              <button
                key={type.id}
                className={`pool-type-btn ${selectedType === type.id ? 'active' : ''}`}
                onClick={() => setSelectedType(type.id)}
                title={type.description}
              >
                <span className='pool-type-name'>{type.name}</span>
                <span className='pool-count'>{poolsByType[type.id]?.length || 0}</span>
              </button>
            ))}
          </nav>
        </aside>

        {/* Pool Details */}
        <main className='pool-details'>
          {currentType && (
            <>
              <div className='pool-type-header'>
                <h2>{currentType.name}</h2>
                <p className='pool-description'>{currentType.description}</p>
              </div>

              {currentPools.length === 0 ? (
                <div className='no-pools'>
                  <p>No pools found for this category</p>
                </div>
              ) : (
                <div className='pools-table-wrapper'>
                  <table className='pools-table'>
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
                      {currentPools.map((pool) => (
                        <tr key={pool.pool} className='pool-row'>
                          <td className='symbol'>{pool.symbol}</td>
                          <td>{pool.chain}</td>
                          <td>{pool.project}</td>
                          <td className='tvl'>
                            ${pool.tvlUsd.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                          </td>
                          <td className='apy'>{pool.apy.toFixed(2)}%</td>
                          <td>{pool.apyBase !== null ? pool.apyBase.toFixed(2) : '-'}%</td>
                          <td>{pool.apyReward !== null ? pool.apyReward.toFixed(2) : '-'}%</td>
                          <td className='stablecoin'>{pool.stablecoin ? '✓' : '—'}</td>
                          <td className='il-risk'>{pool.ilRisk}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>

                  {/* Summary Stats */}
                  <div className='pools-summary'>
                    <div className='summary-stat'>
                      <span className='stat-label'>Total Pools:</span>
                      <span className='stat-value'>{currentPools.length}</span>
                    </div>
                    <div className='summary-stat'>
                      <span className='stat-label'>Total TVL:</span>
                      <span className='stat-value'>
                        ${(currentPools.reduce((sum, p) => sum + p.tvlUsd, 0) / 1_000_000_000).toFixed(2)}B
                      </span>
                    </div>
                    <div className='summary-stat'>
                      <span className='stat-label'>Avg APY:</span>
                      <span className='stat-value'>
                        {(currentPools.reduce((sum, p) => sum + p.apy, 0) / currentPools.length).toFixed(2)}%
                      </span>
                    </div>
                    <div className='summary-stat'>
                      <span className='stat-label'>Max APY:</span>
                      <span className='stat-value'>{Math.max(...currentPools.map((p) => p.apy)).toFixed(2)}%</span>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </main>
      </div>

      {/* Theme Toggle Button */}
      <button
        className='theme-toggle'
        onClick={handleThemeToggle}
        title={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
        aria-label={isDarkMode ? 'Switch to light mode' : 'Switch to dark mode'}
      >
        {isDarkMode ? '☀️' : '🌙'}
      </button>
    </div>
  );
};
