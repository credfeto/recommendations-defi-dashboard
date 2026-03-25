module.exports = {
  displayName: 'server',
  preset: 'ts-jest',
  testEnvironment: 'node',
  rootDir: '.',
  testMatch: ['**/__tests__/**/*.test.ts'],
  collectCoverageFrom: ['src/server/server-fastify.ts'],
  coverageReporters: ['text', 'text-summary', 'html'],
  coverageThreshold: { global: { branches: 75, functions: 75, lines: 75, statements: 75 } },
  moduleNameMapper: {
    '^@shared$': '<rootDir>/../shared/src',
    '^@shared/(.*)$': '<rootDir>/../shared/src/$1',
  },
};
