# Documentation

This folder contains all architecture, development, and usage documentation for the DeFi Dashboard.

## Quick Navigation

### Getting Started

- **[Pool Types Guide](./POOL_TYPES_GUIDE.md)** - How to add and work with pool types

### Architecture

- **[System Architecture](./ARCHITECTURE.md)** - High-level system design and data flow
- **[API Reference](./API.md)** - API endpoints and responses

### Development

- **[Development Guide](./DEVELOPMENT.md)** - Setup, running, and testing locally
- **[Testing Guide](./TESTING.md)** - How to write and run tests

## Common Tasks

### Add a New Pool Type

See [Pool Types Guide](./POOL_TYPES_GUIDE.md)

### Understand the System

See [System Architecture](./ARCHITECTURE.md)

### Run Tests

See [Testing Guide](./TESTING.md)

### Deploy to Production

See [Development Guide](./DEVELOPMENT.md#docker)

## Quick Stats

- **Backend**: .NET 10, ASP.NET Core, native AOT
- **Pool Types**: 5 built-in types
- **Tests**: 268 passing (xunit v3)
- **Cache**: SQLite, 1-hour TTL
- **TLS**: Kestrel with self-signed PFX; HTTP/3 (QUIC) via libmsquic
