const contractSecuritySchema = {
  type: 'object',
  properties: {
    chain: { type: 'string' },
    address: { type: 'string' },
    parentAddress: { type: ['string', 'null'] },
    isOpenSource: { type: ['number', 'null'] },
    isHoneypot: { type: ['number', 'null'] },
    isProxy: { type: ['number', 'null'] },
    buyTax: { type: ['number', 'null'] },
    sellTax: { type: ['number', 'null'] },
    transferTax: { type: ['number', 'null'] },
    cannotBuy: { type: ['number', 'null'] },
    honeypotWithSameCreator: { type: ['number', 'null'] },
    tokenName: { type: ['string', 'null'] },
    tokenSymbol: { type: ['string', 'null'] },
  },
  required: ['chain', 'address', 'parentAddress'],
  additionalProperties: false,
} as const;

const auditInfoSchema = {
  type: 'object',
  properties: { audits: { type: 'number' }, auditLinks: { type: 'array', items: { type: 'string' } } },
  required: ['audits', 'auditLinks'],
  additionalProperties: false,
} as const;

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
    exposure: { type: ['string', 'null'] },
    predictions: { anyOf: [predictionsSchema, { type: 'null' }] },
    poolMeta: { type: ['string', 'null'] },
    mu: { type: ['number', 'null'] },
    sigma: { type: ['number', 'null'] },
    count: { type: ['number', 'null'] },
    outlier: { type: ['boolean', 'null'] },
    underlyingTokens: { type: ['array', 'null'], items: { type: 'string' } },
    il7d: { type: ['number', 'null'] },
    apyBase7d: { type: ['number', 'null'] },
    apyMean30d: { type: ['number', 'null'] },
    volumeUsd1d: { type: ['number', 'null'] },
    volumeUsd7d: { type: ['number', 'null'] },
    apyBaseInception: { type: ['number', 'null'] },
    hacks: { type: 'array', items: hackInfoSchema },
    depegAlerts: { type: 'array', items: depegAlertSchema },
    auditInfo: { anyOf: [auditInfoSchema, { type: 'null' }] },
    contractSecurity: { type: 'array', items: contractSecuritySchema },
  },
  required: [
    'chain',
    'project',
    'symbol',
    'tvlUsd',
    'apy',
    'stablecoin',
    'ilRisk',
    'pool',
    'dataSource',
    'hacks',
    'depegAlerts',
  ],
} as const;

const poolTypeMetadataSchema = {
  type: 'object',
  properties: { name: { type: 'string' }, displayName: { type: 'string' } },
  required: ['name', 'displayName'],
  additionalProperties: false,
} as const;

const errorResponseSchema = {
  type: 'object',
  properties: { error: { type: 'string' } },
  required: ['error'],
  additionalProperties: false,
} as const;

export const getPoolTypesSchema = {
  response: {
    200: {
      type: 'object',
      properties: { status: { type: 'string', enum: ['ok'] }, data: { type: 'array', items: poolTypeMetadataSchema } },
      required: ['status', 'data'],
      additionalProperties: false,
    },
    500: errorResponseSchema,
  },
} as const;

export const getPoolsByNameSchema = {
  params: {
    type: 'object',
    properties: { poolName: { type: 'string' } },
    required: ['poolName'],
    additionalProperties: false,
  },
  response: {
    200: {
      type: 'object',
      properties: { status: { type: 'string', enum: ['ok'] }, data: { type: 'array', items: poolSchema } },
      required: ['status', 'data'],
      additionalProperties: false,
    },
    400: errorResponseSchema,
    500: errorResponseSchema,
  },
} as const;
