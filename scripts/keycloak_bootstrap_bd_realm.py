#!/usr/bin/env python3
"""Bootstrap Keycloak realm bd for BD SSO lab."""
from __future__ import annotations

import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request

BASE = os.environ.get("KEYCLOAK_URL", "http://127.0.0.1:5110").rstrip("/")
ADMIN_USER = os.environ.get("KEYCLOAK_ADMIN", "admin")
ADMIN_PASS = os.environ.get("KEYCLOAK_ADMIN_PASSWORD", "secret")
REALM = "bd"
DIRECTUS_SECRET = os.environ.get("DIRECTUS_CLIENT_SECRET", "bd-directus-lab-secret")
ABP_SECRET = os.environ.get("ABP_CLIENT_SECRET", "bd-abp-auth-lab-secret")
HCS_AUTH_SECRET = os.environ.get("HCS_AUTH_CLIENT_SECRET")
HCS_AUTH_PUBLIC_HOST = os.environ.get("HCS_AUTH_PUBLIC_HOST", "auth.hcs.localhost")
USER_PASS = os.environ.get("BD_TEST_USER_PASSWORD", "Passw0rd!")


def req(method: str, path: str, token: str | None = None, data=None):
    url = f"{BASE}{path}"
    body = None
    headers = {}
    if data is not None:
        if isinstance(data, dict):
            body = json.dumps(data).encode()
            headers["Content-Type"] = "application/json"
        elif isinstance(data, str):
            body = data.encode()
            headers["Content-Type"] = "application/x-www-form-urlencoded"
        else:
            body = data
            headers["Content-Type"] = "application/json"
    if token:
        headers["Authorization"] = f"Bearer {token}"
    request = urllib.request.Request(url, data=body, method=method, headers=headers)
    try:
        with urllib.request.urlopen(request) as resp:
            raw = resp.read()
            return resp.status, raw.decode() if raw else ""
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()


def get_token() -> str:
    status, raw = req(
        "POST",
        "/realms/master/protocol/openid-connect/token",
        data=urllib.parse.urlencode(
            {
                "username": ADMIN_USER,
                "password": ADMIN_PASS,
                "grant_type": "password",
                "client_id": "admin-cli",
            }
        ),
    )
    if status != 200:
        raise SystemExit(f"admin token failed {status}: {raw}")
    return json.loads(raw)["access_token"]


def ensure_realm(token: str) -> None:
    status, _ = req("GET", f"/admin/realms/{REALM}", token)
    payload = {
        "realm": REALM,
        "enabled": True,
        "loginWithEmailAllowed": True,
        "defaultSignatureAlgorithm": "RS256",
    }
    if status == 404:
        status, raw = req("POST", "/admin/realms", token, payload)
        if status not in (201, 204):
            raise SystemExit(f"create realm failed {status}: {raw}")
        print(f"+ realm {REALM}")
    else:
        status, raw = req("PUT", f"/admin/realms/{REALM}", token, payload)
        if status not in (200, 204):
            raise SystemExit(f"update realm failed {status}: {raw}")
        print(f"= realm {REALM} RS256")


def ensure_group(token: str, name: str) -> str:
    status, raw = req("GET", f"/admin/realms/{REALM}/groups?search={urllib.parse.quote(name)}", token)
    groups = json.loads(raw) if raw else []
    for g in groups:
        if g.get("name") == name:
            print(f"= group {name}")
            return g["id"]
    status, raw = req("POST", f"/admin/realms/{REALM}/groups", token, {"name": name})
    if status not in (201, 204):
        raise SystemExit(f"create group {name} failed {status}: {raw}")
    status, raw = req("GET", f"/admin/realms/{REALM}/groups?search={urllib.parse.quote(name)}", token)
    gid = next(g["id"] for g in json.loads(raw) if g.get("name") == name)
    print(f"+ group {name}")
    return gid


def ensure_user(token: str, email: str, group_ids: list[str]) -> None:
    username = email.split("@")[0]
    status, raw = req(
        "GET",
        f"/admin/realms/{REALM}/users?email={urllib.parse.quote(email)}&exact=true",
        token,
    )
    users = json.loads(raw) if raw else []
    if not users:
        status, raw = req(
            "POST",
            f"/admin/realms/{REALM}/users",
            token,
            {
                "username": username,
                "email": email,
                "enabled": True,
                "emailVerified": True,
                "firstName": username,
                "lastName": "BD",
                "credentials": [{"type": "password", "value": USER_PASS, "temporary": False}],
            },
        )
        if status not in (201, 204):
            raise SystemExit(f"create user {email} failed {status}: {raw}")
        status, raw = req(
            "GET",
            f"/admin/realms/{REALM}/users?email={urllib.parse.quote(email)}&exact=true",
            token,
        )
        uid = json.loads(raw)[0]["id"]
        print(f"+ user {email}")
    else:
        uid = users[0]["id"]
        print(f"= user {email}")
    for group_id in group_ids:
        status, raw = req("PUT", f"/admin/realms/{REALM}/users/{uid}/groups/{group_id}", token)
        if status not in (200, 204):
            raise SystemExit(f"join group failed {status}: {raw}")


def ensure_client(token: str, client_id: str, secret: str, redirects: list[str], origins: list[str]) -> str:
    status, raw = req("GET", f"/admin/realms/{REALM}/clients?clientId={urllib.parse.quote(client_id)}", token)
    clients = json.loads(raw) if raw else []
    payload = {
        "clientId": client_id,
        "enabled": True,
        "protocol": "openid-connect",
        "publicClient": False,
        "secret": secret,
        "redirectUris": redirects,
        "webOrigins": origins,
        "standardFlowEnabled": True,
        "directAccessGrantsEnabled": False,
        "attributes": {"pkce.code.challenge.method": "S256"},
    }
    if not clients:
        status, raw = req("POST", f"/admin/realms/{REALM}/clients", token, payload)
        if status not in (201, 204):
            raise SystemExit(f"create client {client_id} failed {status}: {raw}")
        print(f"+ client {client_id}")
        status, raw = req("GET", f"/admin/realms/{REALM}/clients?clientId={urllib.parse.quote(client_id)}", token)
        cid = json.loads(raw)[0]["id"]
    else:
        cid = clients[0]["id"]
        status, raw = req("GET", f"/admin/realms/{REALM}/clients/{cid}", token)
        existing = json.loads(raw)
        existing.update(payload)
        existing["id"] = cid
        status, raw = req("PUT", f"/admin/realms/{REALM}/clients/{cid}", token, existing)
        if status not in (200, 204):
            raise SystemExit(f"update client {client_id} failed {status}: {raw}")
        print(f"= client {client_id}")

    status, raw = req("GET", f"/admin/realms/{REALM}/clients/{cid}/protocol-mappers/models", token)
    mappers = json.loads(raw) if raw else []
    if not any(m.get("name") == "groups" for m in mappers):
        mapper = {
            "name": "groups",
            "protocol": "openid-connect",
            "protocolMapper": "oidc-group-membership-mapper",
            "consentRequired": False,
            "config": {
                "full.path": "false",
                "id.token.claim": "true",
                "access.token.claim": "true",
                "userinfo.token.claim": "true",
                "claim.name": "groups",
            },
        }
        status, raw = req("POST", f"/admin/realms/{REALM}/clients/{cid}/protocol-mappers/models", token, mapper)
        if status not in (201, 204):
            raise SystemExit(f"mapper failed {status}: {raw}")
        print(f"+ mapper groups on {client_id}")
    else:
        print(f"= mapper groups on {client_id}")
    return cid


def main() -> None:
    if not HCS_AUTH_SECRET:
        raise SystemExit("HCS_AUTH_CLIENT_SECRET is required to configure the hcs-free-auth client.")

    token = get_token()
    ensure_realm(token)
    # Role groups (permission inside apps) + app entitlement groups (which apps allowed)
    group_names = (
        "bd-admin",
        "bd-bacsi",
        "bd-lanhdao",
        "bd-nhanvien",
        "bd-app-axis",
        "bd-app-hcs",
    )
    groups = {name: ensure_group(token, name) for name in group_names}
    app_both = [groups["bd-app-axis"], groups["bd-app-hcs"]]
    # Lab users: both apps + one role (override groups in Admin UI to test single-app)
    ensure_user(token, "admin@benhvien.vn", [groups["bd-admin"], *app_both])
    ensure_user(token, "bacsi@benhvien.vn", [groups["bd-bacsi"], *app_both])
    ensure_user(token, "lanhdao@benhvien.vn", [groups["bd-lanhdao"], *app_both])
    ensure_user(token, "nhanvien@benhvien.vn", [groups["bd-nhanvien"], *app_both])
    ensure_client(
        token,
        "directus",
        DIRECTUS_SECRET,
        ["http://localhost:8055/auth/login/keycloak/callback"],
        ["http://localhost:8055", "http://localhost:8080", "+"],
    )
    ensure_client(
        token,
        "abp-auth",
        ABP_SECRET,
        ["http://localhost:44372/signin-oidc", "http://localhost:44372/signin-keycloak"],
        ["http://localhost:44372", "http://localhost:44306", "+"],
    )
    ensure_client(
        token,
        "hcs-free-auth",
        HCS_AUTH_SECRET,
        [f"https://{HCS_AUTH_PUBLIC_HOST}/signin-oidc"],
        [f"https://{HCS_AUTH_PUBLIC_HOST}", "+"],
    )
    status, _ = req("GET", f"/realms/{REALM}/.well-known/openid-configuration")
    print()
    print("=== DONE ===")
    print(f"Discovery: {BASE}/realms/{REALM}/.well-known/openid-configuration ({status})")
    print(f"Users password: {USER_PASS}")
    print(f"directus secret: {DIRECTUS_SECRET}")
    print(f"abp-auth secret: {ABP_SECRET}")


if __name__ == "__main__":
    main()
