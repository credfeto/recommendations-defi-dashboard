import React from 'react';
import { FetchPools } from './fetchPools';

function App() {
  return (
      <div className="App">
        <header>
          <h1>Yield Pools Dashboard</h1>
        </header>
        <main>
            <FetchPools />
        </main>
      </div>
  );
}

export default App;