import React, { useEffect, useState } from 'react';
import axios from 'axios';
import { Pool, PoolsResponse, PoolTypeMetadata, HackInfo } from '@shared';
import { PoolTypeConfig } from './types/poolTypeConfig';
import { getAvailablePoolTypes } from './types/getAvailablePoolTypes';
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
  const [availablePoolTypes, setAvailablePoolTypes] = useState<PoolTypeMetadata[]>([]);
  const [isDarkMode, setIsDarkMode] = useState(window.matchMedia('(prefers-color-scheme: dark)').matches);

  useEffect(() => {
    const fetchPoolTypesAndData = async () => {
      try {
        // First, fetch available pool types from server
        const typesResponse = await axios.get<{ status: string; data: PoolTypeMetadata[] }>('/api/pools');
        const availableTypes = typesResponse.data.data || [];
        setAvailablePoolTypes(availableTypes);

        // Map server types to local types for data fetching
        const requests = availableTypes.map((serverType) =>
          axios
            .get<PoolsResponse>(`/api/pools/${serverType.name}`)
            .then((res) => ({ typeId: serverType.name, data: res.data.data || [] }))
            .catch(() => ({ typeId: serverType.name, data: [] })),
        );

        const results = await Promise.all(requests);
        const pools: PoolsByType = {};

        results.forEach(({ typeId, data }) => {
          pools[typeId] = data;
        });

        setPoolsByType(pools);
        // Set first available type as selected
        if (availableTypes.length > 0) {
          setSelectedType(availableTypes[0].name);
        }
      } catch (err) {
        console.error('Error fetching pool data:', err);
        setError('Failed to fetch pool data');
      } finally {
        setLoading(false);
      }
    };

    fetchPoolTypesAndData();
  }, []);

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

  const currentServerType = availablePoolTypes.find((t) => t.name === selectedType);
  const currentLocalType = poolTypes.find((t) => t.id === selectedType);
  const currentPools = poolsByType[selectedType] || [];

  return (
    <div className='pools-container'>
      <header className='pools-header'>
        <h1>DeFI Yields</h1>
        <p className='subtitle'>Filtered pools with no IL risk, greater than $1M TVL, and positive APY</p>
      </header>

      <div className='pools-content'>
        {/* Pool Type Selector */}
        <aside className='pool-types-sidebar'>
          <h3>Pool Types</h3>
          <nav className='pool-types-nav'>
            {availablePoolTypes.map((type) => (
              <button
                key={type.name}
                className={`pool-type-btn ${selectedType === type.name ? 'active' : ''}`}
                onClick={() => setSelectedType(type.name)}
                title={`${type.displayName} pools`}
              >
                <span className='pool-type-name'>{type.displayName}</span>
                <span className='pool-count'>{poolsByType[type.name]?.length || 0}</span>
              </button>
            ))}
          </nav>
        </aside>

        {/* Pool Details */}
        <main className='pool-details'>
          {currentLocalType && (
            <>
              <div className='pool-type-header'>
                <h2>{currentLocalType.name}</h2>
                <p className='pool-description'>{currentLocalType.description}</p>
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
                        <th>Source</th>
                        <th>Exploit Risk</th>
                        <th>TVL (USD)</th>
                        <th>APY (%)</th>
                        <th>APY Base</th>
                        <th>APY Reward</th>
                        <th>Stablecoin</th>
                      </tr>
                    </thead>
                    <tbody>
                      {currentPools.map((pool) => (
                        <tr key={pool.pool} className='pool-row'>
                          <td className='symbol'>{pool.symbol}</td>
                          <td>{pool.chain}</td>
                          <td>
                            {pool.url ? (
                              <a href={pool.url} target='_blank' rel='noopener noreferrer' className='project-link'>
                                {pool.project} ↗
                              </a>
                            ) : (
                              pool.project
                            )}
                          </td>
                          <td className='data-source'>{pool.dataSource}</td>
                          <td className='exploit-risk'>
                            {pool.hacks && pool.hacks.length > 0 ? (
                              <span
                                className='hack-badge'
                                title={pool.hacks
                                  .map(
                                    (h: HackInfo) =>
                                      `${h.name} (${new Date(h.date * 1000).getFullYear()}): $${(h.amountUsd / 1_000_000).toFixed(1)}M — ${h.technique}`,
                                  )
                                  .join('\n')}
                              >
                                ⚠️ {pool.hacks.length > 1 ? `${pool.hacks.length} incidents` : '1 incident'}
                              </span>
                            ) : (
                              <span className='hack-clean'>—</span>
                            )}
                          </td>
                          <td className='tvl'>
                            ${pool.tvlUsd.toLocaleString(undefined, { maximumFractionDigits: 0 })}
                          </td>
                          <td className='apy'>{pool.apy.toFixed(2)}%</td>
                          <td>{pool.apyBase !== null ? pool.apyBase.toFixed(2) : '-'}%</td>
                          <td>{pool.apyReward !== null ? pool.apyReward.toFixed(2) : '-'}%</td>
                          <td className='stablecoin'>{pool.stablecoin ? '✓' : '—'}</td>
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
