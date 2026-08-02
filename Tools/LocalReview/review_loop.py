#!/usr/bin/env python3
"""Local review-loop state, Unity smoke gates, and live dashboard."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import re
import subprocess
import sys
import time
import webbrowser
from datetime import datetime, timezone
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from typing import Any
from urllib.parse import unquote


ROOT = Path(__file__).resolve().parents[2]
STATE_DIR = ROOT / "Temp" / "LocalReview"
STATE_PATH = STATE_DIR / "state.json"
EVIDENCE_DIR = STATE_DIR / "evidence"
DASHBOARD_PATH = Path(__file__).with_name("dashboard.html")
PHASES = ("scope", "review", "fix", "unity", "done")
STATUSES = ("queued", "running", "passed", "failed", "blocked", "skipped")


def now() -> str:
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


def run(command: list[str], *, check: bool = True, timeout: int = 60) -> subprocess.CompletedProcess[str]:
    result = subprocess.run(command, cwd=ROOT, text=True, capture_output=True, timeout=timeout)
    if check and result.returncode != 0:
        detail = result.stderr.strip() or result.stdout.strip() or f"exit {result.returncode}"
        raise RuntimeError(f"{' '.join(command)}\n{detail}")
    return result


def git(*args: str, check: bool = True) -> str:
    return run(["git", *args], check=check).stdout.strip()


def default_state() -> dict[str, Any]:
    return {
        "version": 1,
        "session": None,
        "branch": None,
        "base": None,
        "round": 1,
        "currentPhase": "scope",
        "phases": {phase: "queued" for phase in PHASES},
        "agents": [],
        "checks": {},
        "evidence": [],
        "findings": [],
        "stack": [],
        "notes": [],
        "updatedAt": now(),
    }


def load_state(required: bool = True) -> dict[str, Any]:
    if not STATE_PATH.exists():
        if required:
            raise RuntimeError("No review session. Run `review_loop.py init --base <branch>` first.")
        return default_state()
    return json.loads(STATE_PATH.read_text(encoding="utf-8"))


def save_state(state: dict[str, Any]) -> None:
    STATE_DIR.mkdir(parents=True, exist_ok=True)
    state["updatedAt"] = now()
    refresh_git_state(state)
    temporary = STATE_PATH.with_suffix(".tmp")
    temporary.write_text(json.dumps(state, indent=2) + "\n", encoding="utf-8")
    temporary.replace(STATE_PATH)


def refresh_git_state(state: dict[str, Any]) -> None:
    state["branch"] = git("branch", "--show-current", check=False) or "detached"
    stack_result = run(["gh", "stack", "view"], check=False)
    branches: list[dict[str, Any]] = []
    if stack_result.returncode == 0:
        for line in stack_result.stdout.splitlines():
            match = re.match(r"^([●○])\s+(\S+)", line.strip())
            if match:
                branches.append({"name": match.group(2), "current": match.group(1) == "●"})
    state["stack"] = list(reversed(branches))


def set_check(state: dict[str, Any], name: str, status: str, note: str = "") -> None:
    state["checks"][name] = {"status": status, "note": note, "updatedAt": now()}
    save_state(state)


def capture_evidence(state: dict[str, Any], label: str, view: str) -> None:
    session = state.get("session") or "unscoped"
    session_dir = EVIDENCE_DIR / session
    session_dir.mkdir(parents=True, exist_ok=True)
    sequence = len(state.setdefault("evidence", [])) + 1
    slug = re.sub(r"[^a-z0-9]+", "-", label.lower()).strip("-") or "capture"
    filename = f"{sequence:02d}-{slug}.png"
    output = session_dir / filename
    normalized_view = view.lower()
    if normalized_view == "scene":
        unity_command("menu", path="Window/General/Scene")
    result = unity_command(
        "screenshot", view=normalized_view, output=output, width=1280, height=720)
    if isinstance(result, dict) and result.get("success") is False:
        raise RuntimeError(result.get("message") or f"Unity could not capture {view} view.")
    if not output.exists():
        raise RuntimeError(f"Unity did not create screenshot: {output}")
    state["evidence"].append(
        {
            "label": label,
            "view": view,
            "phase": state.get("currentPhase", "unity"),
            "url": f"/evidence/{session}/{filename}",
            "capturedAt": now(),
        }
    )
    save_state(state)


def validate_status(status: str) -> None:
    if status not in STATUSES:
        raise RuntimeError(f"Status must be one of: {', '.join(STATUSES)}")


def command_init(args: argparse.Namespace) -> None:
    git("rev-parse", "--verify", args.base)
    branch = git("branch", "--show-current")
    state = default_state()
    state.update(
        {
            "session": f"{datetime.now().strftime('%Y%m%d-%H%M%S')}-{branch.replace('/', '-')}",
            "branch": branch,
            "base": args.base,
            "currentPhase": "scope",
        }
    )
    state["phases"]["scope"] = "running"
    save_state(state)
    print(STATE_PATH)


def handwritten_metrics(base: str) -> dict[str, int]:
    result = git("diff", "--numstat", f"{base}...HEAD")
    added_files = set(git("diff", "--name-only", "--diff-filter=A", f"{base}...HEAD").splitlines())
    source_files = 0
    additions = 0
    runtime_components = 0
    for line in result.splitlines():
        fields = line.split("\t", 2)
        if len(fields) != 3 or not fields[0].isdigit():
            continue
        path = fields[2]
        if not path.endswith(".cs"):
            continue
        source_files += 1
        additions += int(fields[0])
        if path in added_files and "/Runtime/" in path:
            runtime_components += 1
    return {"sourceFiles": source_files, "sourceAdditions": additions, "runtimeComponents": runtime_components}


def command_preflight(args: argparse.Namespace) -> None:
    state = load_state()
    base = state["base"]
    branch = git("branch", "--show-current")
    problems: list[str] = []
    if not branch.startswith("stack/"):
        problems.append("Current branch is not a local stack/* branch.")
    if git("diff", "--name-only", "--diff-filter=U"):
        problems.append("A merge or rebase conflict is unresolved.")
    account = run(["gh", "api", "user", "--jq", ".login"], check=False).stdout.strip()
    if account != "Abe-54":
        problems.append(f"GitHub CLI account is {account or 'unknown'}, expected Abe-54.")
    remote_stack = git("ls-remote", "--heads", "origin", "refs/heads/stack/*", check=False)
    if remote_stack:
        problems.append("Remote stack branches exist; this workflow is configured for local-only review.")

    metrics = handwritten_metrics(base)
    needs_justification = (
        metrics["sourceAdditions"] > 1000
        or metrics["sourceFiles"] > 12
        or metrics["runtimeComponents"] >= 3
    )
    if needs_justification and not args.scope_justification:
        problems.append(
            "Branch exceeds a manageability threshold. Split it or rerun preflight with "
            "--scope-justification explaining why it remains one feature."
        )
    state["metrics"] = {**metrics, "needsSplitJustification": needs_justification}
    state["scopeJustification"] = args.scope_justification or ""
    state["phases"]["scope"] = "failed" if problems else "passed"
    state["currentPhase"] = "scope" if problems else "review"
    state["phases"]["review"] = "queued"
    set_check(state, "preflight", "failed" if problems else "passed", "; ".join(problems))
    print(json.dumps({"branch": branch, "base": base, "metrics": metrics, "warnings": problems}, indent=2))
    print(git("diff", "--stat", f"{base}...HEAD"))
    if problems:
        raise RuntimeError("Preflight failed.")


def command_phase(args: argparse.Namespace) -> None:
    validate_status(args.status)
    state = load_state()
    state["phases"][args.name] = args.status
    state["currentPhase"] = args.name
    if args.note:
        state["notes"].append({"phase": args.name, "text": args.note, "at": now()})
    save_state(state)


def command_agent(args: argparse.Namespace) -> None:
    validate_status(args.status)
    state = load_state()
    agent = next((item for item in state["agents"] if item["id"] == args.id), None)
    if agent is None:
        if not args.role or not args.task:
            raise RuntimeError("New agents require --role and --task.")
        agent = {"id": args.id, "role": args.role, "task": args.task}
        state["agents"].append(agent)
    if args.role:
        agent["role"] = args.role
    if args.task:
        agent["task"] = args.task
    agent.update({"status": args.status, "note": args.note or "", "updatedAt": now()})
    save_state(state)


def command_check(args: argparse.Namespace) -> None:
    validate_status(args.status)
    state = load_state()
    set_check(state, args.name, args.status, args.note or "")


def command_finding(args: argparse.Namespace) -> None:
    state = load_state()
    finding = next((item for item in state["findings"] if item["id"] == args.id), None)
    if finding is None:
        if not args.title or not args.severity:
            raise RuntimeError("New findings require --title and --severity.")
        finding = {"id": args.id, "title": args.title, "severity": args.severity}
        state["findings"].append(finding)
    finding["status"] = args.status
    finding["owner"] = args.owner or finding.get("owner", "main")
    finding["updatedAt"] = now()
    save_state(state)


def command_round(_: argparse.Namespace) -> None:
    state = load_state()
    if state["round"] >= 3:
        raise RuntimeError("Three rounds reached. Stop and ask the user for direction.")
    state["round"] += 1
    state["currentPhase"] = "review"
    state["phases"]["review"] = "running"
    state["phases"]["fix"] = "queued"
    state["agents"] = []
    save_state(state)


def unity_command(name: str, *, timeout: int = 60, **parameters: Any) -> Any:
    command = ["unity", "--format", "json", "command", name, "--project-path", str(ROOT)]
    for key, value in parameters.items():
        if value is None:
            continue
        command.extend([f"--{key}", str(value).lower() if isinstance(value, bool) else str(value)])
    result = run(command, timeout=timeout)
    payload = json.loads(result.stdout)
    if not payload.get("success"):
        raise RuntimeError(json.dumps(payload, indent=2))
    command_result = payload["data"]["result"]
    if isinstance(command_result, str):
        try:
            return json.loads(command_result)
        except json.JSONDecodeError:
            return command_result
    return command_result


def editor_is_connected() -> bool:
    result = run(["unity", "status"], check=False)
    return str(ROOT) in result.stdout


def ensure_editor() -> None:
    if not editor_is_connected():
        run(["unity", "open", str(ROOT)], timeout=20)
    deadline = time.time() + 60
    while time.time() < deadline:
        if editor_is_connected():
            try:
                status = unity_command("editor_status", timeout=10)
                if status.get("status") in ("ready", "playing"):
                    return
            except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError):
                pass
        time.sleep(1)
    raise RuntimeError("Unity Local Review Editor did not become ready within 60 seconds.")


def wait_for_recompile() -> None:
    deadline = time.time() + 60
    while time.time() < deadline:
        try:
            ensure_editor()
            status = unity_command("recompile_status", timeout=10)
            if status.get("status") in ("completed", "up_to_date", "idle"):
                return
        except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError):
            pass
        time.sleep(1)
    raise RuntimeError("Unity recompilation did not finish within 60 seconds.")


def stop_play_mode() -> None:
    deadline = time.time() + 30
    last_error: Exception | None = None
    while time.time() < deadline:
        try:
            status = unity_command("editor_status", timeout=10)
            if status.get("playMode") != "playing":
                return
            unity_command("editor_stop", timeout=10)
        except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError) as error:
            last_error = error
        time.sleep(1)
    raise RuntimeError(f"Could not leave Play Mode within 30 seconds: {last_error}")


def file_hash(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


RESET_PROBE = """
var roots = UnityEngine.Object.FindObjectsByType<CrazyMarket.TestCampus.TestZoneRoot>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
var zoneId = (CrazyMarket.TestCampus.TestZoneId)System.Enum.Parse(typeof(CrazyMarket.TestCampus.TestZoneId), \"{zone}\");
var zone = System.Array.Find(roots, item => item.ZoneId == zoneId);
if (zone == null) return new {{ success = false, reason = \"Zone not loaded\", zonesLoaded = roots.Length }};
var fixture = zone.GetComponentInChildren<CrazyMarket.TestCampus.TestResettableTransform>(true);
if (fixture == null) return new {{ success = false, reason = \"No resettable fixture\", zonesLoaded = roots.Length }};
var original = fixture.transform.position;
fixture.transform.position = original + new UnityEngine.Vector3(17f, 9f, -4f);
zone.ResetZone();
var restored = fixture.transform.position;
return new {{ success = UnityEngine.Vector3.Distance(original, restored) < 0.001f, fixture = fixture.name, zonesLoaded = roots.Length }};
""".strip()

TELEPORT_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var zoneId = (CrazyMarket.TestCampus.TestZoneId)System.Enum.Parse(typeof(CrazyMarket.TestCampus.TestZoneId), "{zone}");
var teleported = controller != null && controller.TeleportToZone(zoneId);
return new {{ success = teleported, currentZone = controller == null ? "No controller" : controller.CurrentZone.ToString() }};
""".strip()


def command_unity_smoke(args: argparse.Namespace) -> None:
    state = load_state()
    state["currentPhase"] = "unity"
    state["phases"]["unity"] = "running"
    state["evidence"] = []
    save_state(state)
    before_status = git("status", "--porcelain=v1")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("clear_console")
        unity_command("recompile", focus=False)
        wait_for_recompile()
        compile_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if compile_errors.get("total", 0):
            raise RuntimeError(f"Unity compilation produced {compile_errors['total']} errors.")
        set_check(state, "compile", "passed", "Unity scripts compiled with zero errors.")

        if args.regenerate:
            unity_command("menu", path="CrazyMarket/Test Campus/Build Existing Scenes")
            ensure_editor()
            set_check(state, "generation", "passed", "Scenes present in this stack layer regenerated.")
        else:
            set_check(state, "generation", "skipped", "Use --regenerate when generator or scene inputs changed.")

        unity_command("menu", path="CrazyMarket/Test Campus/Validate")
        ensure_editor()
        validation_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if validation_errors.get("total", 0):
            raise RuntimeError(f"Campus validation produced {validation_errors['total']} errors.")
        set_check(state, "campus-validator", "passed", "Campus validator completed with zero errors.")

        unity_command("open_scene", path=args.scene, additive=False)
        capture_evidence(state, "Scene ready for Play Mode", "Scene")
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        deadline = time.time() + 30
        scene_count = 0
        while time.time() < deadline:
            try:
                status = unity_command("editor_status")
                if status.get("playMode") == "playing":
                    scenes = unity_command("list_open_scenes")
                    scene_count = scenes.get("count", 0)
                    if scene_count >= args.expected_scenes:
                        break
            except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError):
                # Entering Play Mode can briefly reset the Pipeline connection.
                pass
            time.sleep(1)
        if scene_count < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes, found {scene_count}.")

        teleport_result = unity_command("eval", code=TELEPORT_PROBE.format(zone=args.zone), timeout=30)
        if not teleport_result.get("success") or not teleport_result.get("result", {}).get("success"):
            raise RuntimeError(f"Teleport probe failed: {json.dumps(teleport_result, indent=2)}")
        time.sleep(1)
        set_check(state, "zone-teleport", "passed", f"Teleported player to {args.zone} before visual validation.")
        capture_evidence(state, f"Play Mode loaded {scene_count} scenes", "Game")

        reset_result = unity_command("eval", code=RESET_PROBE.format(zone=args.zone), timeout=30)
        if not reset_result.get("success") or not reset_result.get("result", {}).get("success"):
            raise RuntimeError(f"Reset probe failed: {json.dumps(reset_result, indent=2)}")
        set_check(state, "reset-probe", "passed", f"Reset restored {reset_result['result'].get('fixture')}.")
        capture_evidence(state, f"{args.zone} reset verified", "Game")

        runtime_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if runtime_errors.get("total", 0):
            raise RuntimeError(f"Play Mode produced {runtime_errors['total']} errors.")
        set_check(state, "play-mode", "passed", f"Loaded {scene_count} scenes with zero Console errors.")
        capture_evidence(state, "Final clean Game view", "Game")
        set_check(state, "game-view", "passed", "Screenshot timeline captured.")
        stop_play_mode()
        playing = False
        capture_evidence(state, "Returned to Edit Mode", "Scene")

        if args.build_guard:
            build_settings = ROOT / "ProjectSettings" / "EditorBuildSettings.asset"
            before_hash = file_hash(build_settings)
            output_path = STATE_DIR / "ReleaseGuardTest.app"
            unity_command("build", target="StandaloneOSX", outputPath=output_path, confirm=True)
            deadline = time.time() + 60
            build_result = None
            while time.time() < deadline:
                build_result = unity_command("build_status")
                if build_result.get("status") == "completed":
                    break
                time.sleep(1)
            guard_text = json.dumps(build_result or {})
            if not build_result or build_result.get("result") != "Failed" or "Release build cancelled" not in guard_text:
                raise RuntimeError("Release build guard did not reject Test Campus scenes as expected.")
            if before_hash != file_hash(build_settings):
                raise RuntimeError("Release build guard changed EditorBuildSettings.asset.")
            set_check(state, "build-guard", "passed", "Release rejected; Build Settings hash unchanged.")
        else:
            set_check(state, "build-guard", "skipped", "Use --build-guard when build behavior changed.")

        state["phases"]["unity"] = "passed"
        state["phases"]["done"] = "passed"
        state["currentPhase"] = "done"
        set_check(state, "unity-smoke", "passed", "Unity smoke and screenshot timeline completed.")
        save_state(state)
        after_status = git("status", "--porcelain=v1")
        print(json.dumps({"success": True, "beforeStatus": before_status, "afterStatus": after_status}, indent=2))
    except Exception as error:
        state["phases"]["unity"] = "failed"
        set_check(state, "unity-smoke", "failed", str(error))
        raise
    finally:
        if playing:
            try:
                stop_play_mode()
            except Exception:
                pass


def command_status(args: argparse.Namespace) -> None:
    state = load_state()
    if args.json:
        print(json.dumps(state, indent=2))
        return
    print(f"{state['branch']} · round {state['round']} · {state['currentPhase']} · updated {state['updatedAt']}")
    for phase in PHASES:
        print(f"  {phase:8} {state['phases'].get(phase, 'queued')}")
    for agent in state["agents"]:
        print(f"  agent {agent['id']}: {agent['status']} — {agent['task']}")


class DashboardHandler(SimpleHTTPRequestHandler):
    def do_GET(self) -> None:  # noqa: N802
        if self.path == "/api/state":
            payload = load_state(required=False)
            body = json.dumps(payload).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path.startswith("/evidence/"):
            relative = Path(unquote(self.path.removeprefix("/evidence/")))
            evidence_root = EVIDENCE_DIR.resolve()
            image_path = (EVIDENCE_DIR / relative).resolve()
            if evidence_root not in image_path.parents or image_path.suffix.lower() != ".png":
                self.send_error(404)
                return
            try:
                body = image_path.read_bytes()
            except FileNotFoundError:
                self.send_error(404)
                return
            self.send_response(200)
            self.send_header("Content-Type", "image/png")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.path in ("/", "/dashboard.html"):
            body = DASHBOARD_PATH.read_bytes()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        self.send_error(404)

    def log_message(self, format: str, *args: Any) -> None:
        return


def command_dashboard(args: argparse.Namespace) -> None:
    address = (args.host, args.port)
    server = ThreadingHTTPServer(address, DashboardHandler)
    url = f"http://{address[0]}:{address[1]}"
    print(url, flush=True)
    if not args.no_open:
        webbrowser.open(url)
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


def parser() -> argparse.ArgumentParser:
    root = argparse.ArgumentParser(description=__doc__)
    subcommands = root.add_subparsers(dest="command", required=True)

    init = subcommands.add_parser("init", help="Start or reset local review state.")
    init.add_argument("--base", required=True)
    init.set_defaults(func=command_init)

    preflight = subcommands.add_parser("preflight", help="Check local stack scope and safety.")
    preflight.add_argument("--scope-justification")
    preflight.set_defaults(func=command_preflight)

    phase = subcommands.add_parser("phase", help="Update the active workflow phase.")
    phase.add_argument("name", choices=PHASES)
    phase.add_argument("status", choices=STATUSES)
    phase.add_argument("--note")
    phase.set_defaults(func=command_phase)

    agent = subcommands.add_parser("agent", help="Register or update a reviewer agent.")
    agent.add_argument("id")
    agent.add_argument("status", choices=STATUSES)
    agent.add_argument("--role")
    agent.add_argument("--task")
    agent.add_argument("--note")
    agent.set_defaults(func=command_agent)

    check = subcommands.add_parser("check", help="Record a deterministic check.")
    check.add_argument("name")
    check.add_argument("status", choices=STATUSES)
    check.add_argument("--note")
    check.set_defaults(func=command_check)

    finding = subcommands.add_parser("finding", help="Register or resolve a review finding.")
    finding.add_argument("id")
    finding.add_argument("status", choices=("open", "resolved", "deferred"))
    finding.add_argument("--title")
    finding.add_argument("--severity", choices=("blocker", "should-fix", "suggestion"))
    finding.add_argument("--owner")
    finding.set_defaults(func=command_finding)

    next_round = subcommands.add_parser("next-round", help="Start another bounded review round.")
    next_round.set_defaults(func=command_round)

    smoke = subcommands.add_parser("unity-smoke", help="Run connected-Editor Unity validation.")
    smoke.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    smoke.add_argument("--zone", default="Movement")
    smoke.add_argument("--expected-scenes", type=int, default=7)
    smoke.add_argument("--regenerate", action="store_true")
    smoke.add_argument("--build-guard", action="store_true")
    smoke.set_defaults(func=command_unity_smoke)

    status = subcommands.add_parser("status", help="Print current review-loop state.")
    status.add_argument("--json", action="store_true")
    status.set_defaults(func=command_status)

    dashboard = subcommands.add_parser("dashboard", help="Serve the live local dashboard.")
    dashboard.add_argument("--host", default="127.0.0.1")
    dashboard.add_argument("--port", type=int, default=8765)
    dashboard.add_argument("--no-open", action="store_true")
    dashboard.set_defaults(func=command_dashboard)
    return root


def main() -> int:
    try:
        args = parser().parse_args()
        args.func(args)
        return 0
    except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError) as error:
        print(f"review-loop: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
