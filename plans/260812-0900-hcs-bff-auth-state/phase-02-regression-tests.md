# Phase 2 — Add regression coverage

## Overview

Priority: P1. Cover the provider contract and the client-side failure modes that previously allowed a rendered route with an anonymous cascade.

## Files

- Create a focused test project under `/Users/nguyenlong/Documents/Projects/bd-workspace/test/` or follow the existing solution’s client-test convention if one exists during implementation.
- Modify `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/HCS.slnx` only to include the new test project if necessary.
- Optionally extend `/Users/nguyenlong/Documents/Projects/bd-workspace/services/HCS_web_free_license/gateways/web/HCS.WebGateway/HCS.WebGateway.Tests/BffSecurityTests.cs` only to lock the existing sanitized `/bff/user` contract.

## Test cases

1. `200` authenticated payload maps name, role, and public claims to an authenticated principal.
2. `401`, other non-success responses, malformed JSON, and an HTTP failure yield an anonymous principal and no exception.
3. Provider refresh raises an authentication-state notification and the subsequent cascade state changes from anonymous to authenticated.
4. Gate renders loading before resolution, child content only after authenticated state, and the existing redirect component for anonymous state.
5. Menu contributor regression: authenticated principal can see **Trao đổi**; anonymous principal cannot.
6. Gateway contract regression: `/bff/user` remains authorized and does not return token/private claims.

## Success criteria

- New tests run without a real Keycloak instance via an in-memory `HttpMessageHandler`; this is a real protocol-boundary test, not a fake authenticated UI state.
- Existing Gateway BFF tests remain green.

## Risk

Avoid asserting framework markup internals. Assert principal state, notification behavior, and the gate’s public render branches.
