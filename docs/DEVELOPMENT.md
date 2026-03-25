# Development Guide

## Prerequisites

- Node.js 20.x or higher
- npm 10.x or higher
- Git

## Getting Started

### 1. Clone and Install Dependencies

```bash
git clone https://github.com/markr/recommendations-defi-dashboard.git
cd recommendations-defi-dashboard
npm install
```

### 2. Development Mode

Run both frontend and backend in development mode:

```bash
npm run dev
```

This runs concurrently:

- **Frontend**: React dev server on `http://localhost:3000`
- **Backend**: Fastify on `http://localhost:5000`

### 3. Individual Commands

**Frontend only:**

```bash
npm start
```

**Backend only:**

```bash
npm run server
```

**Tests:**

```bash
npm test
npm test -- --coverage
npm test -- --watch
```

**Build:**

```bash
npm run build
```

## Project Structure

```
src/
├── App.tsx                 # Main React app component
├── FetchPools.tsx          # Pool dashboard component
├── FetchPools.css          # Component styling
├── server.ts               # Backend filtering logic
├── server-fastify.ts       # Fastify server setup
├── index.tsx               # React entry point
├── types/
│   ├── pools.ts            # Pool type definitions
│   └── poolTypes.ts        # Pool type configuration
└── __tests__/
    └── server.test.ts      # Unit tests
```

## Adding a New Pool Type

See [Pool Types Guide](./POOL_TYPES_GUIDE.md) for detailed instructions.

Quick summary:

1. Edit `src/types/poolTypes.ts`
2. Add to `POOL_TYPES` object:

```typescript
YOUR_TYPE: {
  id: 'YOUR_TYPE',
  name: 'Display Name',
  description: 'Description',
  predicate: (pool) => /* your logic */,
}
```

3. Save and rebuild

The system automatically creates the API endpoint, UI button, and data fetching.

## Running Tests

### Run All Tests

```bash
npm test
```

### Run with Coverage

```bash
npm test -- --coverage
```

### Run in Watch Mode

```bash
npm test -- --watch
```

### Run Specific Test Suite

```bash
npm test -- server.test.ts
```

## Code Style

The project uses:

- **TypeScript** for type safety
- **Prettier** for code formatting (via React Scripts)
- **ESLint** for linting (via React Scripts)

## Debugging

### Backend Debugging

The Fastify server logs to console. To see detailed logs:

```bash
DEBUG=fastify:* npm run server
```

### Frontend Debugging

Use React Developer Tools browser extension:

1. Install React Developer Tools
2. Open browser DevTools (F12)
3. Go to Components tab

### Debugging Tests

```bash
npm test -- --verbose
node --inspect-brk ./node_modules/.bin/jest --runInBand
```

## Environment Variables

Create a `.env` file in the root directory:

```
PORT=5000
NODE_ENV=development
```

## Building for Production

### Frontend Build

```bash
npm run build
```

Creates optimized build in `./build/` directory.

### Backend Build

The backend is TypeScript and runs with `ts-node`. For production, compile to JavaScript:

```bash
tsc src/server.ts src/server-fastify.ts
node server-fastify.js
```

Or use a Node.js TypeScript runtime like `tsx`.

## Deployment

### Option 1: Vercel (Frontend + Serverless Backend)

```bash
npm install -g vercel
vercel
```

### Option 2: Heroku

```bash
heroku create
git push heroku main
```

### Option 3: Traditional VPS

1. Build frontend: `npm run build`
2. Deploy `./build` to static hosting
3. Run backend on separate server/port

## Troubleshooting

### Port Already in Use

```bash
# Kill process on port 5000
lsof -ti:5000 | xargs kill -9

# Or use different port
PORT=5001 npm run server
```

### Dependencies Issues

```bash
# Clear cache and reinstall
rm -rf node_modules package-lock.json
npm install
```

### TypeScript Errors

```bash
# Run TypeScript compiler check
npx tsc --noEmit
```

### Tests Failing

```bash
# Clear Jest cache
npm test -- --clearCache

# Run with verbose output
npm test -- --verbose
```

## Performance Tips

1. **Use React DevTools Profiler** to identify slow components
2. **Enable Network throttling** in browser DevTools
3. **Monitor backend performance** with `console.time()`
4. **Use production build** for benchmarking: `npm run build && serve -s build`

## Common Development Tasks

### Add a New Pool Type

1. Update `src/types/poolTypes.ts`
2. Add tests to `src/__tests__/server.test.ts`
3. Run `npm test -- --coverage` to verify
4. Component automatically discovers the new type

### Modify Table Columns

Edit `src/FetchPools.tsx`:

- Add/remove `<th>` in table header
- Add/remove `<td>` in table rows

### Update Styling

Edit `src/FetchPools.css` for component styling or `src/App.css` for global styles.

### Change Base Filters

Edit `src/server.ts` function `applyBaseFilters()`:

```typescript
export const applyBaseFilters = (poolData: PoolData[]): PoolData[] => {
  return poolData.filter(
    (pool) =>
      // Modify filter criteria here
      pool.ilRisk === 'no' && pool.tvlUsd >= MIN_TVL && pool.apy > 0,
  );
};
```

### Update Cache TTL

Edit `src/server-fastify.ts`:

```typescript
const CACHE_TTL_MS = 60 * 60 * 1000; // Change this value (in milliseconds)
```

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/add-bitcoin-pools

# Make changes and test
npm test

# Commit
git add .
git commit -m "feat: add bitcoin pool type"

# Push
git push origin feature/add-bitcoin-pools

# Create Pull Request
```

## Code Review Checklist

- [ ] Tests pass: `npm test -- --coverage`
- [ ] Coverage maintained or improved
- [ ] TypeScript strict mode: `npx tsc --noEmit`
- [ ] No console warnings/errors
- [ ] Documentation updated
- [ ] Backward compatible (or major version bump)

## Release Process

1. Update version in `package.json`
2. Update `CHANGELOG.md`
3. Run final tests: `npm test -- --coverage`
4. Build: `npm run build`
5. Tag release: `git tag -a v1.0.0 -m "Release v1.0.0"`
6. Push: `git push --tags`
7. Deploy to production

## Support

For issues, questions, or suggestions:

1. Check existing documentation in `/docs`
2. Review test cases in `src/__tests__/`
3. Check [API Reference](./API.md)
4. Open an issue on GitHub
