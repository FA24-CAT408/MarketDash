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
# Unity recreates Temp when the Editor launches, so review history must live outside it.
STATE_DIR = ROOT / ".local-review"
STATE_PATH = STATE_DIR / "state.json"
EVIDENCE_DIR = STATE_DIR / "evidence"
DASHBOARD_PATH = Path(__file__).with_name("dashboard.html")
PHASES = ("scope", "review", "fix", "unity", "done")
STATUSES = ("queued", "running", "passed", "failed", "blocked", "skipped")

MOVEMENT_CHANGELOG = (
    {
        "type": "NEW",
        "title": "Movement room is part of the playable campus",
        "detail": "Core loads the Movement scene during play, and the scene is included in local Build Settings.",
        "file": "Assets/TestCampus/Scenes/TestCampus_Core.unity · ProjectSettings/EditorBuildSettings.asset",
    },
    {
        "type": "FIXED",
        "title": "Distance markers no longer interrupt movement",
        "detail": "The distance markers are visual guides without colliders, so the sprint lane stays smooth.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "NEW",
        "title": "Movement challenges cover distinct player behaviors",
        "detail": "The room now has a narrow beam, step heights, two slopes, jump targets, and a low ceiling.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "NEW",
        "title": "The room exercises the real moving platform",
        "detail": "The fixture is the production Moving Platform prefab, not a Test Campus imitation.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "VERIFIED",
        "title": "Local review now includes visible Unity proof",
        "detail": "Repeatable tours capture the room, player-on-fixture action shots, and two moments of platform motion.",
        "file": "Tools/LocalReview/review_loop.py",
    },
)

LIGHTING_CHANGELOG = (
    {
        "type": "FIXED",
        "title": "Lighting is reachable from the normal campus flow",
        "detail": "Core now loads the Lighting scene, and local Build Settings include it after Movement.",
        "file": "Assets/TestCampus/Scenes/TestCampus_Core.unity · ProjectSettings/EditorBuildSettings.asset",
    },
    {
        "type": "NEW",
        "title": "Five labeled lighting comparisons",
        "detail": "Identical spheres and cubes progress from cool to warm across alternating point and spot bays.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "FIXED",
        "title": "Each bay measures its own light",
        "detail": "Shorter light ranges and no shared ceiling fill keep neighboring bays from muddying the comparison.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "FIXED",
        "title": "Controls only claim real behavior",
        "detail": "The inert Low/Normal/Stress selector is omitted because this room does not implement lighting presets.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "VERIFIED",
        "title": "Lighting passed the local Unity gates",
        "detail": "Four scenes loaded, teleport and reset worked, the Console stayed clean, and the release guard rejected Test Campus scenes.",
        "file": "Tools/LocalReview/review_loop.py",
    },
)

NPC_CHANGELOG = (
    {
        "type": "FIXED",
        "title": "Apples are real production collectibles",
        "detail": "Walking through a glowing apple now uses Item's normal trigger pickup, and Reset restores it.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "FIXED",
        "title": "NPC patrols reset cleanly",
        "detail": "The production spline walker now owns its enable/disable lifecycle, so campus Reset restarts each patrol without orphaned tweens.",
        "file": "Assets/Scripts/NPCController.cs · Assets/TestCampus/Runtime/TestResettableActivation.cs",
    },
    {
        "type": "FIXED",
        "title": "The room only promises behavior it provides",
        "detail": "Fake count presets and line-of-sight claims are gone; labeled groups describe patrol, physical obstruction, and collection fixtures.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "FIXED",
        "title": "Large NPC colliders start above the floor",
        "detail": "Placement is calculated from each production collider's bounds instead of a fragile hard-coded height.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "VERIFIED",
        "title": "NPC interaction passed the local Unity gates",
        "detail": "The room loads through Core, production patrols move, apple pickup/reset works, and the Console remains clean.",
        "file": "Tools/LocalReview/review_loop.py",
    },
)

UI_CHANGELOG = (
    {
        "type": "NEW",
        "title": "Test Campus controls now use UI Toolkit",
        "detail": "A configured UIDocument renders a UXML hierarchy styled by USS; the presenter only binds campus commands and live state.",
        "file": "Assets/TestCampus/UI/TestCampusControlPanel.uxml · Assets/TestCampus/UI/TestCampusControlPanel.uss · Assets/TestCampus/Runtime/TestCampusControlPanel.cs",
    },
    {
        "type": "FIXED",
        "title": "The UI room is reachable",
        "detail": "Core loads the UI scene as the sixth campus scene and Build Settings include it after NPC Interaction.",
        "file": "Assets/TestCampus/Scenes/TestCampus_Core.unity · ProjectSettings/EditorBuildSettings.asset",
    },
    {
        "type": "NEW",
        "title": "Production UI fixtures have honest states",
        "detail": "Low hides both fixtures, Normal shows the production HUD, Stress adds the production pause overlay, and Reset hides both.",
        "file": "Assets/TestCampus/Runtime/TestCampusUiFixtureGallery.cs",
    },
    {
        "type": "FIXED",
        "title": "UI focus still protects gameplay input",
        "detail": "Opening the Toolkit panel exposes the cursor and disables player movement; closing it restores gameplay focus.",
        "file": "Assets/TestCampus/Runtime/TestCampusControlPanel.cs",
    },
    {
        "type": "VERIFIED",
        "title": "UI Toolkit setup is now a validation gate",
        "detail": "Core must contain exactly one UIDocument with PanelSettings, a visual tree, a presenter, and the shared EventSystem.",
        "file": "Assets/TestCampus/Editor/TestCampusValidator.cs",
    },
    {
        "type": "FIXED",
        "title": "Moving platforms initialize before physics",
        "detail": "Unity validation exposed a lifecycle race; the production mover now assigns its controller in Awake before the first physics simulation.",
        "file": "Assets/Scripts/MovingPlatform.cs",
    },
)

INTEGRATION_CHANGELOG = (
    {
        "type": "FIXED",
        "title": "Integration is reachable through the normal campus flow",
        "detail": "Core now loads the seventh scene, the Integration button resolves its spawn, and Build Settings retain the full ordered campus.",
        "file": "Assets/TestCampus/Scenes/TestCampus_Core.unity · ProjectSettings/EditorBuildSettings.asset",
    },
    {
        "type": "NEW",
        "title": "One component owns the cross-system scenario",
        "detail": "A focused adapter coordinates production NPC, light, moving-platform, and collectible fixtures while exposing concise diagnostics.",
        "file": "Assets/TestCampus/Runtime/TestCampusIntegrationScenario.cs",
    },
    {
        "type": "FIXED",
        "title": "Low, Normal, and Stress now change real load",
        "detail": "Presets scale active NPCs and lights and include or remove the moving platform instead of changing only a label.",
        "file": "Assets/TestCampus/Runtime/TestCampusIntegrationScenario.cs",
    },
    {
        "type": "FIXED",
        "title": "The production platform and collectible are on the route",
        "detail": "The moving platform is inside the arena, and a glowing production Apple provides a real pickup/reset interaction.",
        "file": "Assets/TestCampus/Editor/TestCampusSceneGenerator.cs",
    },
    {
        "type": "REMOVED",
        "title": "Deferred automated tests are out of this layer",
        "detail": "The added EditMode test assembly was removed; validation remains an interactive local Unity smoke and action pass.",
        "file": "Assets/TestCampus/Tests/EditMode",
    },
)


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


def capture_evidence(state: dict[str, Any], label: str, view: str, group: str | None = None) -> None:
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
    result = None
    for attempt in range(3):
        try:
            result = unity_command(
                "screenshot", view=normalized_view, output=output, width=1280, height=720)
            break
        except RuntimeError:
            if attempt == 2:
                raise
            ensure_editor()
            time.sleep(1)
    if isinstance(result, dict) and result.get("success") is False:
        raise RuntimeError(result.get("message") or f"Unity could not capture {view} view.")
    if not output.exists():
        raise RuntimeError(f"Unity did not create screenshot: {output}")
    evidence = {
        "label": label,
        "view": view,
        "phase": state.get("currentPhase", "unity"),
        "url": f"/evidence/{session}/{filename}",
        "capturedAt": now(),
    }
    if group:
        evidence["group"] = group
    state["evidence"].append(evidence)
    save_state(state)


def capture_runtime_screen_evidence(state: dict[str, Any], label: str, group: str) -> None:
    """Capture the composed player frame, including UI Toolkit overlays."""
    session = state.get("session") or "unscoped"
    session_dir = EVIDENCE_DIR / session
    session_dir.mkdir(parents=True, exist_ok=True)
    sequence = len(state.setdefault("evidence", [])) + 1
    slug = re.sub(r"[^a-z0-9]+", "-", label.lower()).strip("-") or "capture"
    filename = f"{sequence:02d}-{slug}.png"
    output = session_dir / filename
    code = (
        f"UnityEngine.ScreenCapture.CaptureScreenshot({json.dumps(str(output))}, 1); "
        "return new { success = true };"
    )
    result = unity_command("eval", code=code, timeout=30)
    if not result.get("success") or not result.get("result", {}).get("success"):
        raise RuntimeError(f"Runtime screenshot failed: {json.dumps(result, indent=2)}")
    deadline = time.time() + 10
    while time.time() < deadline and not output.exists():
        time.sleep(0.25)
    if not output.exists():
        raise RuntimeError(f"Unity did not create composed screenshot: {output}")
    state["evidence"].append({
        "label": label,
        "view": "Game + UI Toolkit",
        "phase": state.get("currentPhase", "unity"),
        "url": f"/evidence/{session}/{filename}",
        "capturedAt": now(),
        "group": group,
    })
    save_state(state)


def collect_change_summary(state: dict[str, Any]) -> dict[str, Any]:
    base = state.get("base")
    if not base:
        return {"files": [], "snippets": []}

    def safe_path(path: str) -> bool:
        candidate = Path(path)
        sensitive_names = {".env", ".npmrc", ".pypirc", "credentials", "credentials.json", "id_rsa", "id_ed25519"}
        sensitive_suffixes = {".key", ".pem", ".p12", ".pfx"}
        if candidate.name.lower() in sensitive_names or candidate.suffix.lower() in sensitive_suffixes:
            return False
        return (
            path.startswith("Assets/TestCampus/")
            or path.startswith("Tools/LocalReview/")
            or path == "Assets/Scripts/MovingPlatform.cs"
            or path == "ProjectSettings/EditorBuildSettings.asset"
        )

    file_totals: dict[str, dict[str, Any]] = {}
    output = git("diff", "--numstat", base, "--", check=False)
    for line in output.splitlines():
        parts = line.split("\t", 2)
        if len(parts) != 3:
            continue
        added, deleted, path = parts
        if not safe_path(path):
            continue
        item = file_totals.setdefault(path, {"path": path, "added": 0, "deleted": 0})
        if added.isdigit():
            item["added"] = int(added)
        if deleted.isdigit():
            item["deleted"] = int(deleted)

    files = []
    for item in sorted(file_totals.values(), key=lambda value: value["path"]):
        suffix = Path(item["path"]).suffix.lower()
        item["kind"] = "generated" if suffix in (".unity", ".meta") else "source" if suffix in (".cs", ".py", ".html") else "config"
        files.append(item)

    patches: dict[str, list[str]] = {}
    patch = git("diff", "--unified=2", base, "--", check=False)
    current_path = None
    for line in patch.splitlines():
        if line.startswith("diff --git "):
            current_path = None
        elif line.startswith("+++ b/"):
            path = line[6:]
            current_path = path if safe_path(path) else None
            if current_path:
                patches.setdefault(current_path, [])
        elif current_path is not None:
            patches[current_path].append(line)

    snippets = []
    for item in files:
        suffix = Path(item["path"]).suffix.lower()
        snippet_allowed = (
            (item["path"].startswith("Assets/TestCampus/") and suffix in (".cs", ".asmdef"))
            or (item["path"].startswith("Tools/LocalReview/") and suffix in (".py", ".html", ".md"))
            or item["path"] == "Assets/Scripts/MovingPlatform.cs"
            or item["path"] == "ProjectSettings/EditorBuildSettings.asset"
        )
        if not snippet_allowed:
            continue
        lines = patches.get(item["path"], [])
        hunks: list[dict[str, Any]] = []
        current: dict[str, Any] | None = None
        for line in lines:
            if line.startswith("@@"):
                if current and current["lines"]:
                    hunks.append(current)
                current = {"key": line, "lines": [], "search": []}
                if len(hunks) >= 4:
                    break
            elif current is not None and not line.startswith(("---", "+++")):
                current["search"].append(line)
                if len(current["lines"]) < 18:
                    current["lines"].append(line)
        if current and current["lines"] and len(hunks) < 4:
            hunks.append(current)
        for hunk in hunks:
            key = hunk["key"]
            joined_lines = "\n".join(hunk.pop("search"))
            key_hints = (
                ("def collect_change_summary", "Changed files and snippets"),
                ("LIGHTING_CHANGELOG", "Lighting changelog"),
                ("NPC_CHANGELOG", "NPC interaction changelog"),
                ("UI_CHANGELOG", "UI Toolkit changelog"),
                ("INTEGRATION_CHANGELOG", "Integration changelog"),
                ("LIGHTING_TOUR_STOPS", "Lighting screenshot tour"),
                ("NPC_TOUR_STOPS", "NPC interaction screenshot tour"),
                ("UI_TOUR_STOPS", "UI screenshot tour"),
                ("INTEGRATION_TOUR_STOPS", "Integration screenshot tour"),
                ("MOVEMENT_TOUR_STOPS", "Movement screenshot tour"),
                ("changed-files", "Changed-files dashboard"),
                ("TestCampus_Lighting.unity", "Lighting in Build Settings"),
                ("TestCampus_NPCInteraction.unity", "NPC interaction in Build Settings"),
                ("TestCampus_Movement.unity", "Movement in Build Settings"),
                ("Moving Platform.prefab", "Production moving platform fixture"),
                ("Distance ", "Non-colliding distance markers"),
                ("includePresetProvider", "Zone preset capability"),
                ("TestResettableActivation", "Fixture activation reset"),
                ("COLLECT_NPC_APPLE_PROBE", "Production Apple pickup proof"),
                ("OPEN_UI_TOOLKIT_PROBE", "UI focus handoff proof"),
                ("APPLY_UI_PRESET_PROBE", "Production UI fixture proof"),
                ("APPLY_INTEGRATION_PRESET_PROBE", "Integration load preset proof"),
                ("COLLECT_INTEGRATION_APPLE_PROBE", "Integration Apple pickup proof"),
                ("TestCampusIntegrationScenario", "Integration scenario component"),
                ("TestCampusControlPanel.uxml", "UI Toolkit document"),
                ("TestCampusUiFixtureGallery", "Production UI fixture states"),
                ("ScreenCapture.CaptureScreenshot", "Composed UI screenshot capture"),
                ("private void Awake", "Moving platform physics initialization"),
                ("TELEPORT_PROBE", "Screenshot zone teleport"),
            )
            hinted = False
            for marker, label in key_hints:
                if marker in joined_lines:
                    key = label
                    hinted = True
                    break
            for raw_line in (() if hinted else hunk["lines"]):
                candidate = raw_line[1:].strip() if raw_line[:1] in ("+", "-", " ") else raw_line.strip()
                match = re.search(r"\b(?:def|class)\s+([A-Za-z_]\w*)", candidate)
                if not match:
                    match = re.match(
                        r"(?:public|private|protected|internal)\s+(?:static\s+)?(?:[\w<>\[\],?]+\s+)+([A-Za-z_]\w*)\s*\(",
                        candidate,
                    )
                if not match:
                    match = re.match(r"([A-Z][A-Z0-9_]+)\s*=", candidate)
                if match:
                    key = match.group(1)
                    break
            snippets.append({"file": item["path"], "key": key, "lines": hunk["lines"]})

    unique_snippets = list({(item["file"], item["key"]): item for item in snippets}.values())
    priority_keys = {
        "Lighting changelog": 0,
        "UI Toolkit changelog": 0,
        "Integration changelog": 0,
        "Lighting screenshot tour": 0,
        "UI screenshot tour": 0,
        "Movement screenshot tour": 0,
        "Changed files and snippets": 0,
        "Changed-files dashboard": 0,
        "Non-colliding distance markers": 1,
        "Production moving platform fixture": 1,
        "Zone preset capability": 1,
        "Screenshot zone teleport": 1,
        "Lighting in Build Settings": 1,
        "Movement in Build Settings": 1,
    }
    unique_snippets.sort(
        key=lambda item: (priority_keys.get(item["key"], 3 if item["key"].startswith("@@") else 2), item["file"], item["key"]))
    return {"files": files, "snippets": unique_snippets[:8]}


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

MOVE_PLAYER_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
if (controller == null || controller.PlayerRoot == null) return new {{ success = false, reason = "Player unavailable" }};
var position = new UnityEngine.Vector3({x}f, {y}f, {z}f);
var rotation = UnityEngine.Quaternion.Euler(0f, {yaw}f, 0f);
var adapter = controller.PlayerRoot.GetComponent<CrazyMarket.TestCampus.TestCampusPlayerAdapter>();
if (adapter != null) adapter.TeleportTo(position, rotation);
else controller.PlayerRoot.SetPositionAndRotation(position, rotation);
return new {{ success = true, position = controller.PlayerRoot.position.ToString(), zone = controller.CurrentZone.ToString() }};
""".strip()

MOVEMENT_TOUR_STOPS = (
    ("Distance-marker lane", -82, 1, -8, 0),
    ("Balance beam and low-ceiling tests", -68, 1, -13, 0),
    ("Step-height progression", -70, 1, -2, 0),
    ("15° and 30° slope tests", -74, 1, 13, 0),
    ("Jump-target progression", -75, 1, 37, 0),
    ("Production moving platform at close range", -58, 1, 18, 0),
)

LIGHTING_TOUR_STOPS = (
    ("Lighting gallery — full cool-to-warm comparison", 0, 1, 55, 0),
    ("Cool point and cool-neutral spot bays", -15, 1, 60, 0),
    ("Neutral point reference bay", 0, 1, 60, 0),
    ("Warm-neutral spot and warm point bays", 15, 1, 60, 0),
)

NPC_TOUR_STOPS = (
    ("NPC room — full patrol and collection overview", -88, 1, -78, 45),
    ("Production NPC patrol lanes", -87, 1, -76, 45),
    ("Physical obstruction fixture", -84, 1, -53, 70),
    ("Glowing production collectible line", -84, 1, -44, 70),
)

UI_TOUR_STOPS = (
    ("UI Systems Lab — full contrast gallery", 70, 1, -66, 0),
    ("Bright production UI backdrop", 62, 1, -63, 0),
    ("Dark production UI backdrop", 78, 1, -63, 0),
)

INTEGRATION_TOUR_STOPS = (
    ("Integration route — checkpoints 1 through 3", -24, 1, -109, 25),
    ("Production NPC crossings and collectible", -9, 1, -91, 25),
    ("Route finale and production moving platform", 13, 1, -72, 25),
)

APPLY_INTEGRATION_PRESET_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var scenario = UnityEngine.Object.FindAnyObjectByType<CrazyMarket.TestCampus.TestCampusIntegrationScenario>();
var applied = controller != null && controller.ApplyPreset(CrazyMarket.TestCampus.TestZoneId.Integration, "{preset}");
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var npcs = System.Array.FindAll(all, item => item.name.StartsWith("Integration Crossing NPC") && item.scene.isLoaded);
var platform = System.Array.Find(all, item => item.name == "Integration Production Moving Platform" && item.scene.isLoaded);
var lights = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Light>();
var activeNpcCount = 0;
foreach (var npc in npcs) if (npc.activeSelf) activeNpcCount++;
var activeLightCount = 0;
foreach (var light in lights) if (light.gameObject.scene.name == "TestCampus_Integration" && light.gameObject.activeSelf) activeLightCount++;
return new {{ success = applied && scenario != null && platform != null,
    activeNpcCount = activeNpcCount, activeLightCount = activeLightCount,
    platformActive = platform != null && platform.activeSelf }};
""".strip()

READ_INTEGRATION_PLATFORM_PROBE = """
var platform = UnityEngine.GameObject.Find("Integration Production Moving Platform");
var mover = platform == null ? null : platform.GetComponentInChildren<MovingPlatform>(true);
if (platform == null || mover == null) return new { success = false };
var p = mover.transform.position;
var insideArena = p.x >= -30f && p.x <= 30f && p.z >= -120f && p.z <= -50f && p.y >= -2f && p.y <= 25f;
return new { success = true, active = platform.activeSelf, insideArena = insideArena,
    x = p.x, y = p.y, z = p.z };
""".strip()

COLLECT_INTEGRATION_APPLE_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var apple = UnityEngine.GameObject.Find("Integration Route Apple");
if (controller == null || controller.PlayerRoot == null || apple == null)
    return new { success = false, reason = "Player or Integration Apple unavailable" };
var adapter = controller.PlayerRoot.GetComponent<CrazyMarket.TestCampus.TestCampusPlayerAdapter>();
if (adapter != null) adapter.TeleportTo(apple.transform.position, UnityEngine.Quaternion.identity);
else controller.PlayerRoot.SetPositionAndRotation(apple.transform.position, UnityEngine.Quaternion.identity);
UnityEngine.Physics.SyncTransforms();
return new { success = true };
""".strip()

READ_INTEGRATION_APPLE_PROBE = """
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var apple = System.Array.Find(all, item => item.name == "Integration Route Apple" && item.scene.isLoaded);
return new { success = apple != null, active = apple != null && apple.activeSelf };
""".strip()

RESET_INTEGRATION_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var reset = controller != null && controller.ResetZone(CrazyMarket.TestCampus.TestZoneId.Integration);
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var apple = System.Array.Find(all, item => item.name == "Integration Route Apple" && item.scene.isLoaded);
var npcs = System.Array.FindAll(all, item => item.name.StartsWith("Integration Crossing NPC") && item.scene.isLoaded);
var platform = System.Array.Find(all, item => item.name == "Integration Production Moving Platform" && item.scene.isLoaded);
var activeNpcCount = 0;
foreach (var npc in npcs) if (npc.activeSelf) activeNpcCount++;
return new { success = reset && apple != null && apple.activeSelf && activeNpcCount == 2 && platform != null && platform.activeSelf,
    appleActive = apple != null && apple.activeSelf, activeNpcCount = activeNpcCount,
    platformActive = platform != null && platform.activeSelf };
""".strip()

OPEN_UI_TOOLKIT_PROBE = """
var presenter = UnityEngine.Object.FindAnyObjectByType<CrazyMarket.TestCampus.TestCampusControlPanel>();
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
if (presenter == null || controller == null || controller.PlayerRoot == null)
    return new { success = false, reason = "Presenter or player unavailable" };
var open = presenter.GetType().GetMethod("SetPanelOpen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
open.Invoke(presenter, new object[] { true });
var document = presenter.GetComponent<UnityEngine.UIElements.UIDocument>();
var panel = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(document.rootVisualElement, "campus-panel");
var gameplayHud = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(document.rootVisualElement, "gameplay-hud");
var playerController = controller.PlayerRoot.GetComponent("KCCPlayerController");
var canMoveField = playerController == null ? null : playerController.GetType().GetField("canMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
var canMove = canMoveField == null || (bool)canMoveField.GetValue(playerController);
return new { success = panel.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.Flex && !canMove,
    panel = panel.resolvedStyle.display.ToString(), gameplayHud = gameplayHud.resolvedStyle.display.ToString(),
    cursorVisible = UnityEngine.Cursor.visible, canMove = canMove };
""".strip()

CLOSE_UI_TOOLKIT_PROBE = """
var presenter = UnityEngine.Object.FindAnyObjectByType<CrazyMarket.TestCampus.TestCampusControlPanel>();
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
if (presenter == null || controller == null || controller.PlayerRoot == null) return new { success = false };
var close = presenter.GetType().GetMethod("SetPanelOpen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
close.Invoke(presenter, new object[] { false });
var document = presenter.GetComponent<UnityEngine.UIElements.UIDocument>();
var panel = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(document.rootVisualElement, "campus-panel");
var gameplayHud = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.VisualElement>(document.rootVisualElement, "gameplay-hud");
var playerController = controller.PlayerRoot.GetComponent("KCCPlayerController");
var canMoveField = playerController == null ? null : playerController.GetType().GetField("canMove", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
var canMove = canMoveField == null || (bool)canMoveField.GetValue(playerController);
return new { success = panel.resolvedStyle.display == UnityEngine.UIElements.DisplayStyle.None && canMove,
    panel = panel.resolvedStyle.display.ToString(), gameplayHud = gameplayHud.resolvedStyle.display.ToString(), canMove = canMove };
""".strip()

APPLY_UI_PRESET_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var applied = controller != null && controller.ApplyPreset(CrazyMarket.TestCampus.TestZoneId.UI, "{preset}");
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var hud = System.Array.Find(all, item => item.name == "Production HUD Fixture" && item.scene.isLoaded);
var pause = System.Array.Find(all, item => item.name == "Production Pause Overlay Fixture" && item.scene.isLoaded);
return new {{ success = applied && hud != null && pause != null,
    hudActive = hud != null && hud.activeSelf, pauseActive = pause != null && pause.activeSelf }};
""".strip()

RESET_UI_FIXTURES_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var reset = controller != null && controller.ResetZone(CrazyMarket.TestCampus.TestZoneId.UI);
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var hud = System.Array.Find(all, item => item.name == "Production HUD Fixture" && item.scene.isLoaded);
var pause = System.Array.Find(all, item => item.name == "Production Pause Overlay Fixture" && item.scene.isLoaded);
return new { success = reset && hud != null && pause != null && !hud.activeSelf && !pause.activeSelf,
    hudActive = hud != null && hud.activeSelf, pauseActive = pause != null && pause.activeSelf };
""".strip()

TOGGLE_UI_PAUSE_PROBE = """
var presenter = UnityEngine.Object.FindAnyObjectByType<CrazyMarket.TestCampus.TestCampusControlPanel>();
if (presenter == null) return new { success = false, reason = "Presenter unavailable" };
var toggle = presenter.GetType().GetMethod("TogglePause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
toggle.Invoke(presenter, null);
var document = presenter.GetComponent<UnityEngine.UIElements.UIDocument>();
var panel = UnityEngine.UIElements.UQueryExtensions.Q<UnityEngine.UIElements.ScrollView>(document.rootVisualElement, "campus-panel");
return new { success = true, timeScale = UnityEngine.Time.timeScale,
    panel = panel.resolvedStyle.display.ToString(), cursorVisible = UnityEngine.Cursor.visible };
""".strip()

COLLECT_NPC_APPLE_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var apple = System.Array.Find(UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>(),
    item => item.name == "Collectible Apple 3" && item.scene.isLoaded);
if (controller == null || controller.PlayerRoot == null || apple == null)
    return new { success = false, reason = "Player or collectible unavailable" };
var adapter = controller.PlayerRoot.GetComponent<CrazyMarket.TestCampus.TestCampusPlayerAdapter>();
if (adapter != null) adapter.TeleportTo(apple.transform.position, UnityEngine.Quaternion.Euler(0f, 70f, 0f));
else controller.PlayerRoot.SetPositionAndRotation(apple.transform.position, UnityEngine.Quaternion.Euler(0f, 70f, 0f));
UnityEngine.Physics.SyncTransforms();
return new { success = true, applePosition = apple.transform.position.ToString() };
""".strip()

READ_NPC_APPLE_PROBE = """
var apple = System.Array.Find(UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>(),
    item => item.name == "Collectible Apple 3" && item.scene.isLoaded);
return new { success = apple != null, active = apple != null && apple.activeSelf };
""".strip()

RESET_NPC_INTERACTION_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
var reset = controller != null && controller.ResetZone(CrazyMarket.TestCampus.TestZoneId.NPCInteraction);
var apple = System.Array.Find(UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>(),
    item => item.name == "Collectible Apple 3" && item.scene.isLoaded);
return new { success = reset && apple != null && apple.activeSelf, restored = apple != null && apple.activeSelf };
""".strip()

READ_NPC_PATROLS_PROBE = """
var all = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.GameObject>();
var west = System.Array.Find(all, item => item.name == "West Patrol NPC" && item.scene.isLoaded);
var center = System.Array.Find(all, item => item.name == "Obstructed Center Patrol NPC" && item.scene.isLoaded);
var east = System.Array.Find(all, item => item.name == "East Patrol NPC" && item.scene.isLoaded);
if (west == null || center == null || east == null) return new { success = false };
var westWalker = System.Array.Find(west.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true), item => item.GetType().Name == "NPCSplineWalker");
var centerWalker = System.Array.Find(center.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true), item => item.GetType().Name == "NPCSplineWalker");
var eastWalker = System.Array.Find(east.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true), item => item.GetType().Name == "NPCSplineWalker");
if (westWalker == null || centerWalker == null || eastWalker == null) return new { success = false };
return new { success = true,
    westX = westWalker.transform.position.x, westZ = westWalker.transform.position.z,
    centerX = centerWalker.transform.position.x, centerZ = centerWalker.transform.position.z,
    eastX = eastWalker.transform.position.x, eastZ = eastWalker.transform.position.z };
""".strip()

MOVEMENT_ACTION_STOPS = (
    ("NEW: Player balancing on the narrow beam", -72, 3.25, -4, 0),
    ("NEW: Player standing on the tallest step", -60, 3.35, 4, 180),
    ("NEW: Player approaching the 30-degree slope", -67, 1, 15, 0),
    ("NEW: Player landed on an elevated jump target", -68, 3.85, 43, 270),
)

BOARD_MOVING_PLATFORM_PROBE = """
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
if (controller == null || controller.PlayerRoot == null) return new { success = false, reason = "Player unavailable" };
var platform = UnityEngine.GameObject.Find("Movement Production Moving Platform");
if (platform == null) return new { success = false, reason = "Moving platform unavailable" };
var mover = platform.GetComponentInChildren<MovingPlatform>(true);
if (mover == null) return new { success = false, reason = "MovingPlatform component unavailable" };
var renderers = mover.GetComponentsInChildren<UnityEngine.Renderer>(true);
if (renderers.Length == 0) return new { success = false, reason = "Moving platform has no renderer" };
var bounds = renderers[0].bounds;
for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
var position = new UnityEngine.Vector3(bounds.center.x, bounds.max.y + 1.05f, bounds.center.z);
var rotation = UnityEngine.Quaternion.Euler(0f, 180f, 0f);
var adapter = controller.PlayerRoot.GetComponent<CrazyMarket.TestCampus.TestCampusPlayerAdapter>();
if (adapter != null) adapter.TeleportTo(position, rotation);
else controller.PlayerRoot.SetPositionAndRotation(position, rotation);
return new { success = true, platformX = mover.transform.position.x, platformZ = mover.transform.position.z, player = controller.PlayerRoot.position.ToString() };
""".strip()

READ_MOVING_PLATFORM_PROBE = """
var platform = UnityEngine.GameObject.Find("Movement Production Moving Platform");
var controller = CrazyMarket.TestCampus.TestCampusController.Instance;
if (platform == null || controller == null || controller.PlayerRoot == null) return new { success = false };
var mover = platform.GetComponentInChildren<MovingPlatform>(true);
if (mover == null) return new { success = false };
var renderers = mover.GetComponentsInChildren<UnityEngine.Renderer>(true);
if (renderers.Length == 0) return new { success = false };
var bounds = renderers[0].bounds;
for (var i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
var player = controller.PlayerRoot.position;
var aboard = UnityEngine.Mathf.Abs(player.x - bounds.center.x) <= bounds.extents.x + 1f
    && UnityEngine.Mathf.Abs(player.z - bounds.center.z) <= bounds.extents.z + 1f
    && player.y >= bounds.max.y - 0.25f
    && player.y <= bounds.max.y + 2f;
return new { success = true, aboard = aboard, platformX = mover.transform.position.x, platformZ = mover.transform.position.z, player = player.ToString() };
""".strip()


def wait_for_loaded_scenes(expected_scenes: int) -> int:
    deadline = time.time() + 30
    scene_count = 0
    while time.time() < deadline:
        try:
            status = unity_command("editor_status")
            if status.get("playMode") == "playing":
                scenes = unity_command("list_open_scenes")
                scene_count = scenes.get("count", 0)
                if scene_count >= expected_scenes:
                    return scene_count
        except (RuntimeError, subprocess.TimeoutExpired, json.JSONDecodeError):
            # Entering Play Mode can briefly reset the Pipeline connection.
            pass
        time.sleep(1)
    return scene_count


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
        scene_count = wait_for_loaded_scenes(args.expected_scenes)
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


def command_unity_tour(args: argparse.Namespace) -> None:
    state = load_state()
    group = f"{args.zone.lower()}-tour"
    state["evidence"] = [item for item in state.get("evidence", []) if item.get("group") != group]
    set_check(state, group, "running", f"Capturing in-game views around {args.zone}.")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("open_scene", path=args.scene, additive=False)
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        scene_count = wait_for_loaded_scenes(args.expected_scenes)
        if scene_count < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes, found {scene_count}.")

        teleport_result = unity_command("eval", code=TELEPORT_PROBE.format(zone=args.zone), timeout=30)
        if not teleport_result.get("success") or not teleport_result.get("result", {}).get("success"):
            raise RuntimeError(f"Teleport probe failed: {json.dumps(teleport_result, indent=2)}")

        stops_by_zone = {
            "Movement": MOVEMENT_TOUR_STOPS,
            "Lighting": LIGHTING_TOUR_STOPS,
            "NPCInteraction": NPC_TOUR_STOPS,
            "UI": UI_TOUR_STOPS,
            "Integration": INTEGRATION_TOUR_STOPS,
        }
        stops = stops_by_zone.get(args.zone, ())
        if not stops:
            raise RuntimeError(f"No screenshot tour is configured for zone {args.zone}.")
        for label, x, y, z, yaw in stops:
            move_result = unity_command(
                "eval", code=MOVE_PLAYER_PROBE.format(x=x, y=y, z=z, yaw=yaw), timeout=30)
            if not move_result.get("success") or not move_result.get("result", {}).get("success"):
                raise RuntimeError(f"Could not stage screenshot '{label}': {json.dumps(move_result, indent=2)}")
            time.sleep(2)
            capture_evidence(state, label, "Game", group=group)

        runtime_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if runtime_errors.get("total", 0):
            raise RuntimeError(f"Screenshot tour produced {runtime_errors['total']} Console errors.")
        set_check(state, group, "passed",
                  f"Captured {len(stops)} in-game views around {args.zone} with zero Console errors.")
    except Exception as error:
        set_check(state, group, "failed", str(error))
        raise
    finally:
        if playing:
            stop_play_mode()


def command_unity_action_tour(args: argparse.Namespace) -> None:
    state = load_state()
    group = "movement-action"
    state["evidence"] = [item for item in state.get("evidence", []) if item.get("group") != group]
    set_check(state, group, "running", "Capturing the player actively using Movement fixtures.")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("open_scene", path=args.scene, additive=False)
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        scene_count = wait_for_loaded_scenes(args.expected_scenes)
        if scene_count < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes, found {scene_count}.")

        teleport_result = unity_command("eval", code=TELEPORT_PROBE.format(zone="Movement"), timeout=30)
        if not teleport_result.get("success") or not teleport_result.get("result", {}).get("success"):
            raise RuntimeError(f"Teleport probe failed: {json.dumps(teleport_result, indent=2)}")

        for label, x, y, z, yaw in MOVEMENT_ACTION_STOPS:
            move_result = unity_command(
                "eval", code=MOVE_PLAYER_PROBE.format(x=x, y=y, z=z, yaw=yaw), timeout=30)
            if not move_result.get("success") or not move_result.get("result", {}).get("success"):
                raise RuntimeError(f"Could not stage screenshot '{label}': {json.dumps(move_result, indent=2)}")
            time.sleep(2)
            capture_evidence(state, label, "Game", group=group)

        board_result = unity_command("eval", code=BOARD_MOVING_PLATFORM_PROBE, timeout=30)
        if not board_result.get("success") or not board_result.get("result", {}).get("success"):
            raise RuntimeError(f"Could not board moving platform: {json.dumps(board_result, indent=2)}")
        time.sleep(0.5)
        first_result = unity_command("eval", code=READ_MOVING_PLATFORM_PROBE, timeout=30)
        if not first_result.get("success") or not first_result.get("result", {}).get("success"):
            raise RuntimeError(f"Could not sample moving platform: {json.dumps(first_result, indent=2)}")
        if not first_result["result"].get("aboard"):
            raise RuntimeError(f"Player was not aboard for the first platform capture: {json.dumps(first_result, indent=2)}")
        start_x = float(first_result["result"].get("platformX", 0))
        start_z = float(first_result["result"].get("platformZ", 0))
        capture_evidence(
            state, f"NEW: Moving platform — first position ({start_x:.1f}, {start_z:.1f})", "Game", group=group)
        deadline = time.time() + 5
        later_result = None
        moved = 0.0
        while time.time() < deadline and moved < 3:
            time.sleep(0.25)
            later_result = unity_command("eval", code=READ_MOVING_PLATFORM_PROBE, timeout=30)
            if not later_result.get("success") or not later_result.get("result", {}).get("success"):
                raise RuntimeError(f"Could not read moving platform: {json.dumps(later_result, indent=2)}")
            end_x = float(later_result["result"].get("platformX", 0))
            end_z = float(later_result["result"].get("platformZ", 0))
            moved = ((end_x - start_x) ** 2 + (end_z - start_z) ** 2) ** 0.5
        if later_result is None:
            raise RuntimeError("Could not sample moving platform motion.")
        if not later_result["result"].get("aboard"):
            raise RuntimeError(f"Player was not aboard for the later platform capture: {json.dumps(later_result, indent=2)}")
        capture_evidence(
            state, f"NEW: Moving platform — later position ({end_x:.1f}, {end_z:.1f})", "Game", group=group)

        runtime_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if runtime_errors.get("total", 0):
            raise RuntimeError(f"Action tour produced {runtime_errors['total']} Console errors.")
        if moved < 0.1:
            raise RuntimeError(f"Moving platform changed only {moved:.2f} m during the timed capture.")
        set_check(state, group, "passed",
                  f"Captured {len(MOVEMENT_ACTION_STOPS) + 2} action views; platform moved {moved:.1f} m; zero Console errors.")
    except Exception as error:
        set_check(state, group, "failed", str(error))
        raise
    finally:
        if playing:
            stop_play_mode()


def command_unity_ui_toolkit(args: argparse.Namespace) -> None:
    state = load_state()
    group = "ui-action"
    state["evidence"] = [item for item in state.get("evidence", []) if item.get("group") != group]
    set_check(state, group, "running", "Verifying UI Toolkit focus handoff and production fixture states.")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("open_scene", path=args.scene, additive=False)
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        if wait_for_loaded_scenes(args.expected_scenes) < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes.")

        teleport = unity_command("eval", code=TELEPORT_PROBE.format(zone="UI"), timeout=30)
        if not teleport.get("success") or not teleport.get("result", {}).get("success"):
            raise RuntimeError(f"UI room teleport failed: {json.dumps(teleport, indent=2)}")
        time.sleep(1)

        opened = unity_command("eval", code=OPEN_UI_TOOLKIT_PROBE, timeout=30)
        opened_state = opened.get("result", {})
        if not opened.get("success") or not opened_state.get("success") or not opened_state.get("cursorVisible"):
            raise RuntimeError(f"UI Toolkit panel did not take gameplay focus: {json.dumps(opened, indent=2)}")
        time.sleep(1)
        capture_runtime_screen_evidence(
            state, "UI Toolkit control panel — gameplay input paused", group)

        closed = unity_command("eval", code=CLOSE_UI_TOOLKIT_PROBE, timeout=30)
        if not closed.get("success") or not closed.get("result", {}).get("success"):
            raise RuntimeError(f"UI Toolkit panel did not restore gameplay focus: {json.dumps(closed, indent=2)}")
        time.sleep(1)
        capture_runtime_screen_evidence(
            state, "UI Toolkit gameplay HUD — movement restored", group)

        expected_states = (
            ("Normal", True, False, "NORMAL: Production HUD fixture"),
            ("Stress", True, True, "STRESS: Production HUD and pause overlay"),
        )
        for preset, expected_hud, expected_pause, label in expected_states:
            applied = unity_command("eval", code=APPLY_UI_PRESET_PROBE.format(preset=preset), timeout=30)
            result = applied.get("result", {})
            if (not applied.get("success") or not result.get("success")
                    or result.get("hudActive") is not expected_hud
                    or result.get("pauseActive") is not expected_pause):
                raise RuntimeError(f"UI preset {preset} produced the wrong fixtures: {json.dumps(applied, indent=2)}")
            time.sleep(1)
            capture_runtime_screen_evidence(state, label, group)

        reset = unity_command("eval", code=RESET_UI_FIXTURES_PROBE, timeout=30)
        if not reset.get("success") or not reset.get("result", {}).get("success"):
            raise RuntimeError(f"UI fixture reset failed: {json.dumps(reset, indent=2)}")
        time.sleep(1)
        capture_runtime_screen_evidence(state, "RESET: Production UI fixtures hidden", group)

        paused = unity_command("eval", code=TOGGLE_UI_PAUSE_PROBE, timeout=30)
        paused_state = paused.get("result", {})
        if (not paused.get("success") or not paused_state.get("success")
                or float(paused_state.get("timeScale", 1)) != 0
                or paused_state.get("panel") != "Flex"
                or not paused_state.get("cursorVisible")):
            raise RuntimeError(f"Pause did not preserve UI interaction: {json.dumps(paused, indent=2)}")
        resumed = unity_command("eval", code=TOGGLE_UI_PAUSE_PROBE, timeout=30)
        resumed_state = resumed.get("result", {})
        if (not resumed.get("success") or not resumed_state.get("success")
                or float(resumed_state.get("timeScale", 0)) != 1
                or resumed_state.get("panel") != "None"):
            raise RuntimeError(f"Resume did not restore simulation state: {json.dumps(resumed, indent=2)}")

        runtime_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if runtime_errors.get("total", 0):
            raise RuntimeError(f"UI Toolkit validation produced {runtime_errors['total']} Console errors.")
        set_check(
            state,
            group,
            "passed",
            "Panel focus handoff, pause/resume, gameplay HUD, Normal/Stress fixture states, Reset, and composed screenshots passed with zero Console errors.",
        )
    except Exception as error:
        set_check(state, group, "failed", str(error))
        raise
    finally:
        if playing:
            stop_play_mode()


def command_unity_integration(args: argparse.Namespace) -> None:
    state = load_state()
    group = "integration-action"
    state["evidence"] = [item for item in state.get("evidence", []) if item.get("group") != group]
    set_check(state, group, "running", "Verifying Integration load presets, platform motion, Apple pickup, and Reset.")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("open_scene", path=args.scene, additive=False)
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        if wait_for_loaded_scenes(args.expected_scenes) < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes.")
        teleport = unity_command("eval", code=TELEPORT_PROBE.format(zone="Integration"), timeout=30)
        if not teleport.get("success") or not teleport.get("result", {}).get("success"):
            raise RuntimeError(f"Integration teleport failed: {json.dumps(teleport, indent=2)}")

        expected = (
            ("Low", 1, 1, False),
            ("Normal", 2, 2, True),
            ("Stress", 4, None, True),
        )
        stress_lights = 0
        for preset, npc_count, light_count, platform_active in expected:
            applied = unity_command("eval", code=APPLY_INTEGRATION_PRESET_PROBE.format(preset=preset), timeout=30)
            result = applied.get("result", {})
            valid_lights = result.get("activeLightCount") == light_count if light_count is not None else result.get("activeLightCount", 0) >= 2
            if (not applied.get("success") or not result.get("success")
                    or result.get("activeNpcCount") != npc_count
                    or not valid_lights
                    or result.get("platformActive") is not platform_active):
                raise RuntimeError(f"Integration preset {preset} produced the wrong load: {json.dumps(applied, indent=2)}")
            stress_lights = max(stress_lights, int(result.get("activeLightCount", 0)))
            time.sleep(1)
            capture_runtime_screen_evidence(state, f"{preset.upper()}: real Integration fixture load", group)

        normal = unity_command("eval", code=APPLY_INTEGRATION_PRESET_PROBE.format(preset="Normal"), timeout=30)
        if not normal.get("success") or not normal.get("result", {}).get("success"):
            raise RuntimeError("Could not restore Normal Integration load before action checks.")
        platform_start = unity_command("eval", code=READ_INTEGRATION_PLATFORM_PROBE, timeout=30)
        start = platform_start.get("result", {})
        if not platform_start.get("success") or not start.get("success") or not start.get("insideArena"):
            raise RuntimeError(f"Integration moving platform started outside the arena: {json.dumps(platform_start, indent=2)}")
        start_position = (float(start.get("x", 0)), float(start.get("y", 0)), float(start.get("z", 0)))
        moved = 0.0
        platform_later = None
        deadline = time.time() + 5
        while time.time() < deadline and moved < 0.1:
            time.sleep(0.5)
            platform_later = unity_command("eval", code=READ_INTEGRATION_PLATFORM_PROBE, timeout=30)
            later = platform_later.get("result", {})
            if not platform_later.get("success") or not later.get("success") or not later.get("insideArena"):
                raise RuntimeError(f"Integration moving platform left the arena: {json.dumps(platform_later, indent=2)}")
            moved = sum((float(later.get(axis, 0)) - start_position[index]) ** 2 for index, axis in enumerate(("x", "y", "z"))) ** 0.5
        if moved < 0.1:
            raise RuntimeError(f"Integration moving platform moved only {moved:.2f} m.")

        unity_command("eval", code=MOVE_PLAYER_PROBE.format(x=-3, y=1, z=-90, yaw=0), timeout=30)
        time.sleep(1)
        capture_runtime_screen_evidence(state, "BEFORE: production Apple on the Integration route", group)
        collected = unity_command("eval", code=COLLECT_INTEGRATION_APPLE_PROBE, timeout=30)
        if not collected.get("success") or not collected.get("result", {}).get("success"):
            raise RuntimeError(f"Could not stage Integration Apple pickup: {json.dumps(collected, indent=2)}")
        time.sleep(2)
        apple = unity_command("eval", code=READ_INTEGRATION_APPLE_PROBE, timeout=30)
        if not apple.get("success") or apple.get("result", {}).get("active", True):
            raise RuntimeError(f"Integration Apple stayed active after pickup: {json.dumps(apple, indent=2)}")
        capture_runtime_screen_evidence(state, "AFTER: production Apple collected", group)

        reset = unity_command("eval", code=RESET_INTEGRATION_PROBE, timeout=30)
        if not reset.get("success") or not reset.get("result", {}).get("success"):
            raise RuntimeError(f"Integration Reset did not restore Normal load: {json.dumps(reset, indent=2)}")
        runtime_errors = unity_command("get_console_logs", severity="Error", limit=200)
        if runtime_errors.get("total", 0):
            raise RuntimeError(f"Integration action validation produced {runtime_errors['total']} Console errors.")
        set_check(state, group, "passed",
                  f"Low/Normal/Stress changed real load, {stress_lights} Stress lights were active, platform moved {moved:.1f} m inside the arena, Apple pickup/reset passed, and the Console stayed clean.")
    except Exception as error:
        set_check(state, group, "failed", str(error))
        raise
    finally:
        if playing:
            stop_play_mode()


def command_unity_npc_interaction(args: argparse.Namespace) -> None:
    state = load_state()
    group = "npcinteraction-action"
    state["evidence"] = [item for item in state.get("evidence", []) if item.get("group") != group]
    set_check(state, group, "running", "Verifying production Apple pickup and campus Reset.")
    playing = False
    try:
        ensure_editor()
        stop_play_mode()
        unity_command("open_scene", path=args.scene, additive=False)
        unity_command("clear_console")
        unity_command("set_autotick", enable=True, interval_ms=100)
        unity_command("editor_play")
        playing = True
        if wait_for_loaded_scenes(args.expected_scenes) < args.expected_scenes:
            raise RuntimeError(f"Expected {args.expected_scenes} loaded scenes.")
        teleport = unity_command("eval", code=TELEPORT_PROBE.format(zone="NPCInteraction"), timeout=30)
        if not teleport.get("success") or not teleport.get("result", {}).get("success"):
            raise RuntimeError(f"NPC room teleport failed: {json.dumps(teleport, indent=2)}")
        patrol_start = unity_command("eval", code=READ_NPC_PATROLS_PROBE, timeout=30)
        if not patrol_start.get("success") or not patrol_start.get("result", {}).get("success"):
            raise RuntimeError(f"NPC patrol fixtures unavailable: {json.dumps(patrol_start, indent=2)}")
        unity_command("eval", code=MOVE_PLAYER_PROBE.format(x=-84, y=1, z=-44, yaw=70), timeout=30)
        time.sleep(1)
        capture_evidence(state, "BEFORE: Glowing production Apple is active", "Game", group=group)
        collected = unity_command("eval", code=COLLECT_NPC_APPLE_PROBE, timeout=30)
        if not collected.get("success") or not collected.get("result", {}).get("success"):
            raise RuntimeError(f"Apple pickup staging failed: {json.dumps(collected, indent=2)}")
        time.sleep(2)
        after_pickup = unity_command("eval", code=READ_NPC_APPLE_PROBE, timeout=30)
        if after_pickup.get("result", {}).get("active", True):
            raise RuntimeError("Production Apple stayed active after the player entered its trigger.")
        capture_evidence(state, "AFTER: Production Apple collected through its trigger", "Game", group=group)
        patrol_end = unity_command("eval", code=READ_NPC_PATROLS_PROBE, timeout=30)
        start = patrol_start["result"]
        end = patrol_end.get("result", {})
        patrol_motion = sum(
            ((float(end.get(f"{name}X", 0)) - float(start[f"{name}X"])) ** 2
             + (float(end.get(f"{name}Z", 0)) - float(start[f"{name}Z"])) ** 2) ** 0.5
            for name in ("west", "center", "east"))
        if not patrol_end.get("success") or not end.get("success") or patrol_motion < 0.3:
            raise RuntimeError(f"Production NPC patrols did not move: {json.dumps(patrol_end, indent=2)}")
        reset = unity_command("eval", code=RESET_NPC_INTERACTION_PROBE, timeout=30)
        if not reset.get("success") or not reset.get("result", {}).get("success"):
            raise RuntimeError(f"NPC room Reset did not restore the Apple: {json.dumps(reset, indent=2)}")
        time.sleep(1)
        capture_evidence(state, "RESET: Production Apple restored", "Game", group=group)
        errors = unity_command("get_console_logs", severity="Error", limit=200)
        if errors.get("total", 0):
            raise RuntimeError(f"NPC interaction check produced {errors['total']} Console errors.")
        set_check(state, group, "passed",
                  f"Apple pickup/Reset and {patrol_motion:.1f} m aggregate NPC patrol motion passed with zero Console errors.")
    except Exception as error:
        set_check(state, group, "failed", str(error))
        raise
    finally:
        if playing:
            stop_play_mode()


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
            payload["changes"] = collect_change_summary(payload)
            changelogs = {
                "stack/test-campus-movement": MOVEMENT_CHANGELOG,
                "stack/test-campus-lighting": LIGHTING_CHANGELOG,
                "stack/test-campus-npc-interaction": NPC_CHANGELOG,
                "stack/test-campus-ui": UI_CHANGELOG,
                "stack/test-campus-integration": INTEGRATION_CHANGELOG,
            }
            payload["changelog"] = changelogs.get(payload.get("branch"), ())
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

    tour = subcommands.add_parser("unity-tour", help="Capture an in-game screenshot tour of a loaded campus zone.")
    tour.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    tour.add_argument("--zone", default="Movement", choices=("Movement", "Lighting", "NPCInteraction", "UI", "Integration"))
    tour.add_argument("--expected-scenes", type=int, default=3)
    tour.set_defaults(func=command_unity_tour)

    action_tour = subcommands.add_parser(
        "unity-action-tour", help="Capture the player actively using Movement fixtures.")
    action_tour.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    action_tour.add_argument("--expected-scenes", type=int, default=3)
    action_tour.set_defaults(func=command_unity_action_tour)

    ui_toolkit = subcommands.add_parser(
        "unity-ui-toolkit", help="Verify and capture UI Toolkit focus and production UI fixture states.")
    ui_toolkit.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    ui_toolkit.add_argument("--expected-scenes", type=int, default=6)
    ui_toolkit.set_defaults(func=command_unity_ui_toolkit)

    integration = subcommands.add_parser(
        "unity-integration", help="Verify Integration load presets, platform motion, Apple pickup, and Reset.")
    integration.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    integration.add_argument("--expected-scenes", type=int, default=7)
    integration.set_defaults(func=command_unity_integration)

    npc_interaction = subcommands.add_parser(
        "unity-npc-interaction", help="Verify and capture production Apple pickup and NPC-room Reset.")
    npc_interaction.add_argument("--scene", default="Assets/TestCampus/Scenes/TestCampus_Core.unity")
    npc_interaction.add_argument("--expected-scenes", type=int, default=5)
    npc_interaction.set_defaults(func=command_unity_npc_interaction)

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
