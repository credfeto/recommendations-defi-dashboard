module.exports = {
  displayName: 'server',
  preset: 'ts-jest',
  testEnvironment: 'node',
  rootDir: '.',
  testMatch: ['**/*.test.ts'],
  testPathIgnorePatterns: ['/node_modules/', '\\.e2e\\.test\\.ts$'],
  collectCoverageFrom: ['src/server/server-fastify.ts'],
  coverageReporters: ['text', 'text-summary', 'html'],
  coverageThreshold: { global: { branches: 75, functions: 75, lines: 75, statements: 75 } },
  moduleNameMapper: { '^@shared$': '<rootDir>/../shared/src', '^@shared/(.*)$': '<rootDir>/../shared/src/$1' },
  transform: {
    '^.+\\.tsx?$': ['ts-jest', {
      tsconfig: './tsconfig.json',
      // Enable full type-checking so local tests catch the same errors as the Docker build.
      // ts-jest otherwise inherits transpileOnly:true from the ts-node block in tsconfig.json.
      diagnostics: true,
    }],
  },
};
