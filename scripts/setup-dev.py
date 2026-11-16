#!/usr/bin/env python3
"""Create local development secrets without overwriting existing configuration."""
import json
import os
from pathlib import Path
import secrets

root = Path(__file__).resolve().parents[1]
env_path = root / ".env"
settings_path = root / "Template.Api/appsettings.Local.json"
if env_path.exists() or settings_path.exists():
    raise SystemExit("Local configuration already exists. Edit it manually; no files were changed.")
password = secrets.token_hex(24)
admin_password = "Aa1!" + secrets.token_urlsafe(24)
jwt_key = secrets.token_urlsafe(48)
values = {
    "POSTGRES_PASSWORD": password, "JWT_KEY": jwt_key,
    "ADMIN_EMAIL": "admin@example.test", "ADMIN_PASSWORD": admin_password,
    "POSTGRES_PORT": "5432", "API_PORT": "5249",
}
settings = {
    "ConnectionStrings": {"DefaultConnection": f"Host=localhost;Port=5432;Database=template;Username=template;Password={password}"},
    "Jwt": {"Key": jwt_key},
    "Seed": {"Enabled": True, "AdminEmail": values["ADMIN_EMAIL"], "AdminPassword": admin_password},
}
for path, content in [(env_path, "".join(f"{k}={v}\n" for k, v in values.items())),
                      (settings_path, json.dumps(settings, indent=2) + "\n")]:
    descriptor = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(descriptor, "w") as output:
        output.write(content)
print("Created .env and Template.Api/appsettings.Local.json (ignored by Git).")
print("Administrator: admin@example.test. Find its generated password in .env.")
