const hackInfoSchema = {
  type: 'object',
  properties: {
    name: { type: 'string' },
    date: { type: 'number' },
    amountUsd: { type: 'number' },
    classification: { type: 'string' },
    technique: { type: 'string' },
    source: { type: 'string' },
  },
  required: ['name', 'date', 'amountUsd', 'classification', 'technique', 'source'],
  additionalProperties: false,
} as const;

const depegAlertSchema = {
  type: 'object',
  properties: {
    symbol: { type: 'string' },
    currentPrice: { type: 'number' },
    pegPrice: { type: 'number' },
    deviation: { type: 'number' },
    severity: { type: 'string', enum: ['warning', 'critical'] },
  },
  required: ['symbol', 'currentPrice', 'pegPrice', 'deviation', 'severity'],
  additionalProperties: false,
} as const;

const predictionsSchema = {
  type: 'object',
  properties: {
    predictedClass: { type: ['string', 'null'] },
    predictedProbability: { type: ['number', 'null'] },
    binnedConfidence: { type: ['number', 'null'] },
  },
  required: ['predictedClass', 'predictedProbability', 'binnedConfidence'],
  additionalProperties: false,
} as const;

const poolSchema = {
  type: 'object',
  properties: {
    url: { type: ['string', 'null'] },
    chain: { type: 'string' },
    project: { type: 'string' },
    symbol: { type: 'string' },
    dataSource: { type: 'string' },
    tvlUsd: { type: 'number' },
    apyBase: { type: ['number', 'null'] },
    apyReward: { type: ['number', 'null'] },
    apy: { type: 'number' },
    rewardTokens: { type: ['array', 'null'], items: { type: 'string' } },
    pool: { type: 'string' },
    apyPct1D: { type: ['number', 'null'] },
    apyPct7D: { type: ['number', 'null'] },
    apyPct30D: { type: ['number', 'null'] },
    stablecoin: { type: 'boolean' },
    ilRisk: { type: 'string' },
    exposure: { type: 'string' },
    predictions: predictionsSchema,
    poolMeta: { type: ['string', 'null'] },
    mu: { type: 'number' },
    sigma: { type: 'number' },
    count: { type: 'number' },
    outlier: { type: 'boolean' },
    underlyingTokens: { type: ['array', 'null'], items: { type: 'string' } },
    il7d: { type: ['number', 'null'] },
    apyBase7d: { type: ['number', 'null'] },
    apyMean30d: { type: 'number' },
    volumeUsd1d: { type: ['number', 'null'] },
    volumeUsd7d: { type: ['number', 'null'] },
    apyBaseInception: { type: ['number', 'null'] },
    hacks: { type: 'array', items: hackInfoSchema },
    depegAlerts: { type: 'array', items: depegAlertSchema },
  },
  required: ['chain', 'project', 'symbol', 'tvlUsd', 'apy', 'stablecoin', 'ilRisk', 'exposure', 'pool', 'dataSource', 'hacks', 'depegAlerts'],
} as const;

const poolTypeMetadataSchema = {
  type: 'object',
  properties: {
    name: { type: 'string' },
    displayName: { type: 'string' },
  },
  required: ['name', 'displayName'],
  additionalProperties: false,
} as const;

const errorResponseSchema = {
  type: 'object',
  properties: {
    error: { type: 'string' },
  },
  required: ['error'],
  additionalProperties: false,
} as const;

export const getPoolTypesSchema = {
  response: {
    200: {
      type: 'object',
      properties: {
        status: { type: 'string', enum: ['ok'] },
        data: { type: 'array', items: poolTypeMetadataSchema },
      },
      required: ['status', 'data'],
      additionalProperties: false,
    },
    500: errorResponseSchema,
  },
} as const;

export const getPoolsByNameSchema = {
  params: {
    type: 'object',
    properties: {
      poolName: { type: 'string' },
    },
    required: ['poolName'],
    additionalProperties: false,
  },
  response: {
    200: {
      type: 'object',
      properties: {
        status: { type: 'string', enum: ['ok'] },
        data: { type: 'array', items: poolSchema },
      },
      required: ['status', 'data'],
      additionalProperties: false,
    },
    400: errorResponseSchema,
    500: errorResponseSchema,
  },
} as const;
