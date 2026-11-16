#!/usr/bin/env python3
"""Run tests against a disposable PostgreSQL container; never use an existing database."""
import os
from pathlib import Path
import secrets
import subprocess
import time
import uuid

root = Path(__file__).resolve().parents[1]
name = "template-tests-" + uuid.uuid4().hex[:12]
password = secrets.token_hex(24)
environment = os.environ.copy()
environment["POSTGRES_PASSWORD"] = password
started = False
try:
    subprocess.run([
        "docker", "run", "--detach", "--name", name,
        "--env", "POSTGRES_PASSWORD", "--env", "POSTGRES_USER=template",
        "--env", "POSTGRES_DB=template", "--publish", "127.0.0.1::5432", "postgres:16-alpine"
    ], env=environment, check=True, stdout=subprocess.DEVNULL)
    started = True
    for attempt in range(60):
        ready = subprocess.run(["docker", "exec", name, "pg_isready", "-U", "template", "-d", "template"],
                               stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
        if ready.returncode == 0:
            break
        time.sleep(0.5)
    else:
        raise RuntimeError("PostgreSQL did not become ready within 30 seconds.")
    endpoint = subprocess.check_output(["docker", "port", name, "5432"], text=True).strip()
    port = endpoint.rsplit(":", 1)[1]
    environment["TEST_POSTGRES_CONNECTION"] = (
        f"Host=127.0.0.1;Port={port};Database=template;Username=template;Password={password}"
    )
    result = subprocess.run(["dotnet", "test", "Template.sln", "-m:1", *os.sys.argv[1:]],
                            cwd=root, env=environment)
    raise SystemExit(result.returncode)
finally:
    if started:
        subprocess.run(["docker", "rm", "--force", "--volumes", name],
                       stdout=subprocess.DEVNULL, check=True)
