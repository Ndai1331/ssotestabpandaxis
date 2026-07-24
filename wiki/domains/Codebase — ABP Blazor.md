---
type: domain
title: "Codebase — ABP Blazor"
updated: 2026-07-23
---

# Codebase — ABP Blazor

Path: `services/abp-blazor/`  
Template: `hanhchinhso` (ABP microservice)

## Layout
- `apps/auth-server` — OpenIddict AuthServer  
- `apps/blazor` — Blazor UI  
- `gateways/web` — BFF  
- `services/*` — identity, administration, audit-logging, gdpr, …  
- `etc/docker` — local dependencies  

## Role in BD
Digital Administration — OIDC client (external IdP = Keycloak).

## Pre-req (upstream)
.NET 10+, Node 18/20, Docker, Redis; generate `openiddict.pfx`; `abp install-libs`.

README: `services/abp-blazor/README.md`
