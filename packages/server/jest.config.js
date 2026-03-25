module.exports = {
  preset: 'ts-jest',
  testEnvironment: 'node',
  roots: ['<rootDir>/src'],
  testMatch: ['<rootDir>/src/server/__tests__/**/*.test.ts'],
  collectCoverageFrom: ['src/server/server.ts'],
  coverageReporters: ['text', 'text-summary', 'html'],
  coverageThreshold: { global: { branches: 75, functions: 75, lines: 75, statements: 75 } },
};
