module.exports = {
  testEnvironment: 'node',
  roots: ['<rootDir>/src'],
  testMatch: ['<rootDir>/src/__tests__/**/*.test.ts', '<rootDir>/src/__tests__/**/*.test.js'],
  collectCoverageFrom: [
    'src/server.js',
  ],
  transformIgnorePatterns: [],
  coverageReporters: ['text', 'text-summary', 'html'],
  coverageThreshold: {
    global: {
      branches: 75,
      functions: 75,
      lines: 75,
      statements: 75,
    },
  },
};
