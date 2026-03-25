const MIN_TVL = 1_000_000;

const filterPools = (poolData) => {
  return poolData.filter(pool => 
    pool.ilRisk === 'no' && 
    pool.tvlUsd >= MIN_TVL && 
    pool.apy > 0
  );
};

const getPoolsByType = (allPools, poolType) => {
  let filteredPools = [];
  
  if (poolType.toUpperCase() === 'ETH') {
    filteredPools = allPools.filter(pool => 
      pool.symbol.toUpperCase().includes('ETH')
    );
  } else if (poolType.toUpperCase() === 'STABLES') {
    filteredPools = allPools.filter(pool => pool.stablecoin === true);
  }
  
  return filterPools(filteredPools);
};

module.exports = {
  filterPools,
  getPoolsByType,
};
