import tsParser from '@typescript-eslint/parser';

export default [
  {
    files: ['**/*.ts', '**/*.tsx'],
    languageOptions: { parser: tsParser, parserOptions: { ecmaVersion: 2018, sourceType: 'module' } },
    rules: {
      // TypeScript compiler handles import resolution; the linter environment
      // does not have node_modules installed so these rules produce false positives
      'n/no-missing-import': 'off',
      'n/no-unpublished-import': 'off',
      // Allow process.exit() in server entry-point startup/shutdown code
      'n/no-process-exit': 'off',
    },
  },
];
