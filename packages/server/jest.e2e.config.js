module.exports = {
  displayName: 'server-e2e',
  preset: 'ts-jest',
  testEnvironment: 'node',
  rootDir: '.',
  testMatch: ['**/*.e2e.test.ts'],
  testTimeout: 30000,
};
