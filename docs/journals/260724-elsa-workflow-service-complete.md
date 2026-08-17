# Elsa WorkflowService Integration Complete — WASM + Keycloak  

**Date**: 2026-07-24 16:41  
**Severity**: Medium  
**Component**: Elsa Pro 3.5 + ABP microservice  
**Status**: Resolved  

## What Happened

Completed full Elsa WorkflowService integration: WorkflowService (:44395) + Elsa Studio WASM (:44396) + Blazor UI menu link, wired to Keycloak auth via ABP.AuthServer.

## The Brutal Truth

Code review ate **6.5 hours**—way longer than planned. We had configuration blind spots (signing keys, UseOpenIdConnect setup, middleware ordering) that should have been caught earlier. The pain: shipping Studio WASM with hardcoded keys or misconfigured OIDC redirect URIs would have silently broken auth in prod.

## Technical Details

**Fixed in review cycle:**
- Signing key config only in Development profile (not bootstrapped to production)
- Studio `UseOpenIdConnect` callback URI → `http://localhost:44396/authentication/login-callback`
- Middleware order: auth before CORS
- Client appsettings missing `ElsaStudio:Url` injection

## Root Cause

We assumed config patterns from ABP scaffold applied 1:1 to external Elsa packages. They don't. Each package has its own bootstrap/auth contract.

## Lessons Learned

1. **Document the auth contract early**: What secret/keys does package X need, where do they come from, who owns rotation?
2. **Test OIDC redirect URIs in dev first**: Typos here silently fail until user tries login.
3. **Staging profile review mandatory**: Never let environment-specific configs slip to production template.

## Next Steps

- Add Elsa auth checklist to CLAUDE.md for future integrations
- Document Studio URL config pattern for Blazor clients
- Seed permissions + Elsa workflow templates for smoke test
