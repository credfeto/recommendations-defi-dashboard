# Testing Guide

## Overview

The project uses **Jest** with **TypeScript** support (ts-jest) for unit testing. We maintain **100% code coverage** on backend logic.

## Test Suite Structure

```
src/__tests__/
└── server.test.ts          # 26 tests, 100% coverage
```

## Running Tests

### Run All Tests

```bash
npm test
```

### Run with Coverage Report

```bash
npm test -- --coverage
```

Output shows:

- Coverage percentage by type (statements, branches, functions, lines)
- Files and line numbers with uncovered code

### Run in Watch Mode

Automatically re-runs tests when files change:

```bash
npm test -- --watch
```

### Run Specific Test File

```bash
npm test -- server.test.ts
```

### Run Tests Matching Pattern

```bash
npm test -- --testNamePattern="ETH pools"
```

### Clear Jest Cache

```bash
npm test -- --clearCache
```

## Test Organization

Tests are organized into 6 describe blocks:

### 1. Pool Filtering (4 tests)

Tests the base filtering logic applied to all pool types.

```typescript
describe('Pool Filtering', () => {
  test('filters pools with no IL risk', () => {
    const filtered = filterPools(mockPoolData);
    expect(filtered.every((p) => p.ilRisk === 'no')).toBe(true);
  });
  // ... more tests
});
```

**What's tested:**

- IL Risk filter (`ilRisk === 'no'`)
- TVL filter (`tvlUsd >= $1,000,000`)
- APY filter (`apy > 0`)
- Combined filters

### 2. Pool Type Categorization (7 tests)

Tests filtering by specific pool types.

```typescript
describe('Pool Type Categorization', () => {
  test('identifies ETH-based pools correctly', () => {
    const ethPools = getPoolsByType(mockPoolData, 'ETH');
    expect(ethPools.length).toBe(4);
    expect(ethPools.map((p) => p.symbol)).toEqual(['STETH', 'WEETH', 'RETH', 'CBETH']);
  });
  // ... more tests for each type
});
```

**What's tested:**

- ETH-based pools
- Stablecoin pools
- Liquid staking tokens
- High yield pools
- Blue chip pools
- All filters applied together

### 3. Edge Cases (5 tests)

Tests boundary conditions and error handling.

```typescript
describe('Edge Cases', () => {
  test('handles empty pool list', () => {
    const filtered = filterPools([]);
    expect(filtered).toEqual([]);
  });
  // ... more tests
});
```

**What's tested:**

- Empty lists
- Zero TVL pools
- Zero APY pools
- Case-insensitive pool type matching
- Invalid pool types

### 4. Response Format (3 tests)

Tests that API responses have correct structure.

```typescript
test('API returns correct response structure', () => {
  const filtered = filterPools(mockPoolData);
  expect(Array.isArray(filtered)).toBe(true);
  expect(filtered.length > 0).toBe(true);
});
```

### 5. Available Pool Types (2 tests)

Tests pool type discovery and wrapper functions.

```typescript
test('getAvailableTypes returns all pool type configs', () => {
  const { getAvailableTypes } = require('../server');
  const types = getAvailableTypes();
  expect(Array.isArray(types)).toBe(true);
  expect(types.some((t) => t.id === 'ETH')).toBe(true);
});
```

### 6. Data Types (5 tests)

Tests that pool data has correct types.

```typescript
describe('Data Types', () => {
  test('TVL is a number', () => {
    const filtered = filterPools(mockPoolData);
    filtered.forEach((pool) => {
      expect(typeof pool.tvlUsd).toBe('number');
    });
  });
  // ... more type tests
});
```

## Test Data

The test suite uses `mockPoolData` array with 8 sample pools:

```typescript
const mockPoolData = [
  {
    symbol: 'STETH',
    tvlUsd: 20000000000,
    apy: 2.5,
    ilRisk: 'no',
    stablecoin: false,
  },
  // ... 7 more pools
];
```

Pools are designed to test different scenarios:

- High TVL (STETH: $20B)
- Low TVL (WETH: $500K)
- IL Risk Yes/No
- Stablecoin vs non-stablecoin
- Various APY levels

## Writing New Tests

### 1. Add Test Case

```typescript
describe('New Feature', () => {
  test('should do something', () => {
    // Arrange
    const input = mockPoolData;

    // Act
    const result = yourFunction(input);

    // Assert
    expect(result).toBeDefined();
  });
});
```

### 2. Run Test

```bash
npm test -- --watch
```

### 3. Verify Coverage

```bash
npm test -- --coverage
```

Coverage must remain at 100%.

## Coverage Thresholds

The project enforces 100% coverage for:

- **Statements**: All code paths executed
- **Branches**: All if/else branches taken
- **Functions**: All functions called
- **Lines**: All lines executed

### Increasing Coverage

If coverage drops below 100%, identify uncovered lines:

```bash
npm test -- --coverage
```

Then either:

1. Add tests for uncovered code
2. Add `/* istanbul ignore */` comments for untestable code
3. Refactor code to be testable

## Common Test Patterns

### Testing Function with Array Input

```typescript
test('processes array correctly', () => {
  const input = [item1, item2, item3];
  const result = yourFunction(input);
  expect(result).toHaveLength(3);
  expect(result.every((r) => r.property > 0)).toBe(true);
});
```

### Testing Filter Logic

```typescript
test('filters by criteria', () => {
  const filtered = pools.filter((p) => p.apy > 5);
  expect(filtered.every((p) => p.apy > 5)).toBe(true);
});
```

### Testing Pool Type Matching

```typescript
test('matches pool type predicate', () => {
  const config = getPoolTypeById('ETH');
  const ethPools = mockPoolData.filter(config.predicate);
  expect(ethPools.length).toBeGreaterThan(0);
});
```

## Jest Matchers

Common matchers used in tests:

```typescript
expect(value).toBe(expected); // Exact equality
expect(value).toEqual(expected); // Deep equality
expect(value).toBeDefined(); // Not undefined
expect(array).toHaveLength(5); // Array length
expect(array.every((x) => x > 0)).toBe(true); // All items match
expect(string).toContain('text'); // String contains
expect(() => fn()).toThrow(); // Function throws
```

## Debugging Tests

### Run Single Test

```typescript
test.only('debug this test', () => {
  // Only this test runs
});
```

### Skip Test

```typescript
test.skip('skip this test', () => {
  // This test is skipped
});
```

### Verbose Output

```bash
npm test -- --verbose
```

### Debug in Node

```bash
node --inspect-brk ./node_modules/.bin/jest --runInBand
```

Then open `chrome://inspect` in Chrome.

## CI/CD Integration

### GitHub Actions

Example workflow file:

```yaml
name: Tests
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - uses: actions/setup-node@v2
        with:
          node-version: '20'
      - run: npm install
      - run: npm test -- --coverage
```

## Performance Testing

### Test Execution Time

```bash
npm test -- --verbose
```

Shows duration of each test. Aim for sub-100ms tests.

### Coverage Report Details

```bash
npm test -- --coverage --collectCoverageFrom='src/**/*.ts'
```

## Test Maintenance

### Update Test Data

When API response structure changes, update `mockPoolData` in tests.

### Keep Tests DRY

Extract common test setup into helper functions:

```typescript
const createMockPool = (overrides) => ({
  symbol: 'TEST',
  apy: 2.5,
  ...overrides,
});
```

### Review Coverage Reports

Regular review of coverage reports helps identify:

- Dead code
- Overly complex logic
- Missing edge cases

## Troubleshooting

### Tests Failing

```bash
# Clear cache
npm test -- --clearCache

# Run with verbose output
npm test -- --verbose

# Run single test file
npm test -- server.test.ts
```

### Coverage Not 100%

```bash
# Check which lines are uncovered
npm test -- --coverage

# Review the specific file
cat coverage/lcov-report/src/server.ts.html
```

### Timeout Errors

Increase timeout for slow tests:

```typescript
test('slow test', async () => {
  // ... test code
}, 10000); // 10 second timeout
```

## Best Practices

1. **Write tests first** (TDD approach)
2. **Keep tests focused** - one assertion per test where possible
3. **Use descriptive test names** - test name should explain what it tests
4. **Test behavior, not implementation** - test public API, not internals
5. **Maintain test data** - keep mock data realistic
6. **Mock external dependencies** - isolate code under test
7. **Review coverage regularly** - identify gaps early

## Resources

- [Jest Documentation](https://jestjs.io/)
- [Testing Library Guide](https://testing-library.com/)
- [TypeScript Testing](https://www.typescriptlang.org/docs/handbook/testing.html)
