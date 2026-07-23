#!/usr/bin/env python3
"""Add the 4 test/staging proxy hosts to nginx-proxy-manager on do-122.

Creates (idempotent) proxy hosts + Let's Encrypt certs via the NPM REST API:
  test.task9.pro      -> task9-ui-test:8080
  testapi.task9.pro   -> task9-api-test:8080
  stag.task9.pro      -> task9-ui-staging:8080
  stagapi.task9.pro   -> task9-api-staging:8080

NPM talks to containers by name over proxy-network, so forward_host is the
container name and forward_port is the internal 8080 (not the host port).

Run ON do-122 (so 127.0.0.1:81 is the NPM admin API):
  NPM_PASS='<admin-password>' python3 npm-add-staging-hosts.py

Admin email defaults to tobias@eesgroups.com (override with NPM_EMAIL).
Target containers must already exist for the LE cert HTTP-01 challenge to pass;
if a container is missing the host is still created but cert issuance may fail
— re-run after the container is up.
"""
import json
import os
import sys
import urllib.request
import urllib.error

BASE = os.environ.get("NPM_BASE", "http://127.0.0.1:81/api")
EMAIL = os.environ.get("NPM_EMAIL", "tobias@eesgroups.com")
PASS = os.environ.get("NPM_PASS")

HOSTS = [
    {"domain": "test.task9.pro",    "host": "task9-ui-test",      "port": 8080, "ws": True},
    {"domain": "testapi.task9.pro", "host": "task9-api-test",     "port": 8080, "ws": True},
    {"domain": "stag.task9.pro",    "host": "task9-ui-staging",   "port": 8080, "ws": True},
    {"domain": "stagapi.task9.pro", "host": "task9-api-staging",  "port": 8080, "ws": True},
]


def api(method, path, token=None, body=None):
    url = BASE + path
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    if token:
        req.add_header("Authorization", "Bearer " + token)
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return r.status, json.loads(r.read().decode() or "null")
    except urllib.error.HTTPError as e:
        return e.code, json.loads(e.read().decode() or "null")


def main():
    if not PASS:
        sys.exit("❌ set NPM_PASS env var (NPM admin password)")

    code, tok = api("POST", "/tokens", body={"identity": EMAIL, "secret": PASS})
    if code != 200 or "token" not in (tok or {}):
        sys.exit(f"❌ login failed ({code}): {tok}")
    token = tok["token"]
    print(f"✅ authenticated as {EMAIL}")

    # existing hosts (skip duplicates)
    _, existing = api("GET", "/nginx/proxy-hosts", token=token)
    have = set()
    for h in existing or []:
        for d in h.get("domain_names", []):
            have.add(d)

    for spec in HOSTS:
        dom = spec["domain"]
        if dom in have:
            print(f"⏭  {dom} already exists — skip")
            continue

        # 1) request Let's Encrypt cert.
        # NPM v2.14 REST validates strictly: the certificate POST meta MUST be
        # empty ({}). The LE email comes from the admin account; passing
        # letsencrypt_email/agree here returns 400 "additional properties".
        cert_body = {
            "provider": "letsencrypt",
            "nice_name": dom,
            "domain_names": [dom],
            "meta": {},
        }
        code, cert = api("POST", "/nginx/certificates", token=token, body=cert_body)
        cert_id = (cert or {}).get("id", 0) if code in (200, 201) else 0
        if cert_id:
            print(f"🔐 {dom}: cert requested (id={cert_id})")
        else:
            print(f"⚠️  {dom}: cert request failed ({code}): {cert} — host created without SSL, re-run later")

        # 2) create proxy host
        ph_body = {
            "domain_names": [dom],
            "forward_scheme": "http",
            "forward_host": spec["host"],
            "forward_port": spec["port"],
            "access_list_id": 0,
            "certificate_id": cert_id,
            "ssl_forced": bool(cert_id),
            "http2_support": True,
            "block_exploits": True,
            "caching_enabled": False,
            "allow_websocket_upgrade": spec["ws"],
            "hsts_enabled": False,
            "hsts_subdomains": False,
            "meta": {"letsencrypt_agree": True},
            "advanced_config": "",
            "locations": [],
        }
        code, ph = api("POST", "/nginx/proxy-hosts", token=token, body=ph_body)
        if code in (200, 201):
            print(f"✅ {dom} -> {spec['host']}:{spec['port']} (proxy id={ph.get('id')})")
        else:
            print(f"❌ {dom}: proxy host create failed ({code}): {ph}")

    print("\nDone. Verify: https://test.task9.pro  https://stag.task9.pro")


if __name__ == "__main__":
    main()
