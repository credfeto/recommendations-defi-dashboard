import React from 'react';
import { Helmet, HelmetProvider } from 'react-helmet-async';
import { FetchPools } from './FetchPools';

function App() {
  return (
    <HelmetProvider>
      <Helmet>
        <title>DeFI Yields</title>
      </Helmet>
      <div className='App'>
        <main>
          <FetchPools />
        </main>
      </div>
    </HelmetProvider>
  );
}

export default App;
