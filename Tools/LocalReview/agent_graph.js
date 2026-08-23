(() => {
  const NODE_WIDTH = 172;
  const NODE_HEIGHT = 76;
  const EDGE_NS = "http://www.w3.org/2000/svg";

  function createAgentGraph(root) {
    const canvas = root.querySelector("[data-agent-canvas]");
    const nodesLayer = root.querySelector("[data-agent-nodes]");
    const edgesLayer = root.querySelector("[data-agent-edges]");
    const inspector = root.querySelector("[data-agent-inspector]");
    const count = root.querySelector("[data-agent-count]");
    const filters = [...root.querySelectorAll("[data-agent-filter]")];
    const reset = root.querySelector("[data-agent-reset]");
    const positions = new Map();
    const manuallyPlaced = new Set();
    let state = null;
    let mode = "current";
    let signature = "";
    let selectedId = "main";
    let nodes = [];
    let draggingId = null;
    let pendingState = null;

    const clamp = (value, minimum, maximum) => Math.min(Math.max(value, minimum), Math.max(minimum, maximum));
    const displayName = value => String(value || "unknown").replace(/[-_]/g, " ");

    function visibleAgents() {
      const agents = state?.agents || [];
      if (mode === "all") return agents;
      return agents.filter(agent => agent.round === state?.round);
    }

    function buildNodes() {
      const agents = visibleAgents();
      const visibleIds = new Set(agents.map(agent => agent.id));
      const knownIds = new Set((state?.agents || []).map(agent => agent.id));
      return [
        {
          id: "main",
          role: "Primary orchestrator",
          task: "Owns scope, integration, validation, and handoff.",
          status: "root",
          parent: null,
          root: true,
        },
        ...agents.map(agent => {
          const recordedParent = agent.parent || null;
          const parentInferred = !recordedParent;
          const parentInvalid = Boolean(recordedParent && recordedParent !== "main" && !knownIds.has(recordedParent));
          const parentHidden = Boolean(recordedParent && recordedParent !== "main" && knownIds.has(recordedParent) && !visibleIds.has(recordedParent));
          return {
            ...agent,
            recordedParent,
            parent: recordedParent && !parentInvalid && !parentHidden ? recordedParent : "main",
            parentInferred,
            parentInvalid,
            parentHidden,
          };
        }),
      ];
    }

    function depthFor(node, byId, trail = new Set()) {
      if (node.root) return 0;
      if (trail.has(node.id)) return 1;
      const nextTrail = new Set(trail).add(node.id);
      const parent = byId.get(node.parent) || byId.get("main");
      return Math.min(depthFor(parent, byId, nextTrail) + 1, 8);
    }

    function placeNodes(force = false) {
      const width = Math.max(canvas.clientWidth, 280);
      const usableWidth = Math.max(width - 32, NODE_WIDTH);
      const columns = Math.max(1, Math.floor((usableWidth + 18) / (NODE_WIDTH + 18)));
      const byId = new Map(nodes.map(node => [node.id, node]));
      const levels = new Map();
      nodes.forEach(node => {
        const depth = depthFor(node, byId);
        if (!levels.has(depth)) levels.set(depth, []);
        levels.get(depth).push(node);
      });

      let y = 18;
      [...levels.keys()].sort((a, b) => a - b).forEach(depth => {
        const level = levels.get(depth);
        const levelColumns = depth === 0 ? 1 : Math.min(columns, level.length);
        const rows = Math.ceil(level.length / levelColumns);
        level.forEach((node, index) => {
          if (!force && manuallyPlaced.has(node.id)) return;
          const row = Math.floor(index / levelColumns);
          const column = index % levelColumns;
          const rowCount = Math.min(levelColumns, level.length - row * levelColumns);
          const rowWidth = rowCount * NODE_WIDTH + (rowCount - 1) * 18;
          const startX = Math.max(16, (width - rowWidth) / 2);
          positions.set(node.id, { x: startX + column * (NODE_WIDTH + 18), y: y + row * (NODE_HEIGHT + 22) });
        });
        y += rows * (NODE_HEIGHT + 22) + 38;
      });
      canvas.style.height = `${Math.max(330, y - 16)}px`;
      clampAllPositions();
    }

    function clampAllPositions() {
      const maxX = canvas.clientWidth - NODE_WIDTH - 8;
      const maxY = canvas.clientHeight - NODE_HEIGHT - 8;
      positions.forEach((position, id) => positions.set(id, {
        x: clamp(position.x, 8, maxX),
        y: clamp(position.y, 8, maxY),
      }));
    }

    function applyPositions() {
      nodesLayer.querySelectorAll("[data-agent-id]").forEach(element => {
        const position = positions.get(element.dataset.agentId);
        if (position) element.style.transform = `translate(${position.x}px, ${position.y}px)`;
      });
      drawEdges();
    }

    function drawEdges() {
      edgesLayer.replaceChildren();
      const byId = new Map(nodes.map(node => [node.id, node]));
      nodes.filter(node => !node.root).forEach(node => {
        const parent = byId.get(node.parent) || byId.get("main");
        const start = positions.get(parent.id);
        const end = positions.get(node.id);
        if (!start || !end) return;
        const path = document.createElementNS(EDGE_NS, "path");
        const x1 = start.x + NODE_WIDTH / 2;
        const y1 = start.y + NODE_HEIGHT;
        const x2 = end.x + NODE_WIDTH / 2;
        const y2 = end.y;
        const bend = Math.max(24, Math.abs(y2 - y1) * .45);
        path.setAttribute("d", `M ${x1} ${y1} C ${x1} ${y1 + bend}, ${x2} ${y2 - bend}, ${x2} ${y2}`);
        path.classList.add("agent-connection");
        if (node.parentInferred) path.classList.add("inferred");
        if (node.parentHidden) path.classList.add("outside");
        if (node.parentInvalid) path.classList.add("invalid");
        edgesLayer.appendChild(path);
      });
    }

    function renderInspector(node) {
      if (!node) return;
      const lineage = node.root ? "Synthetic review root · orchestrator metadata is not recorded"
        : node.parentInferred ? "Parent not recorded · shown under the review root"
        : node.parentInvalid ? `Recorded parent ${node.recordedParent} does not exist`
        : node.parentHidden ? `Spawned by ${node.recordedParent} · parent is outside this view`
        : `Spawned by ${node.recordedParent}`;
      inspector.replaceChildren();
      const heading = document.createElement("h3");
      heading.textContent = node.role || node.id;
      const task = document.createElement("p");
      task.textContent = node.note || node.task || "No task detail recorded.";
      const meta = document.createElement("div");
      meta.className = "graph-inspector-meta";
      [lineage, node.model, node.effort ? `${node.effort} effort` : null, node.root ? null : node.status]
        .filter(Boolean).forEach(value => {
          const span = document.createElement("span");
          span.textContent = value;
          meta.appendChild(span);
        });
      inspector.append(heading, task, meta);
    }

    function selectNode(id) {
      selectedId = id;
      nodesLayer.querySelectorAll("[data-agent-id]").forEach(element => {
        const selected = element.dataset.agentId === id;
        element.classList.toggle("selected", selected);
        element.setAttribute("aria-pressed", String(selected));
      });
      renderInspector(nodes.find(node => node.id === id) || nodes[0]);
    }

    function moveNode(id, x, y) {
      const maxX = canvas.clientWidth - NODE_WIDTH - 8;
      const maxY = canvas.clientHeight - NODE_HEIGHT - 8;
      positions.set(id, { x: clamp(x, 8, maxX), y: clamp(y, 8, maxY) });
      manuallyPlaced.add(id);
      applyPositions();
    }

    function bindDrag(element, node) {
      let drag = null;
      element.addEventListener("pointerdown", event => {
        if (event.button !== 0) return;
        const position = positions.get(node.id);
        drag = { startX: event.clientX, startY: event.clientY, x: position.x, y: position.y, moved: false };
        draggingId = node.id;
        element.setPointerCapture(event.pointerId);
        element.classList.add("dragging");
      });
      element.addEventListener("pointermove", event => {
        if (!drag) return;
        const dx = event.clientX - drag.startX;
        const dy = event.clientY - drag.startY;
        drag.moved ||= Math.abs(dx) + Math.abs(dy) > 4;
        if (!drag.moved) return;
        moveNode(node.id, drag.x + dx, drag.y + dy);
      });
      const finish = event => {
        if (!drag) return;
        element.releasePointerCapture?.(event.pointerId);
        element.classList.remove("dragging");
        element.dataset.dragged = String(drag.moved);
        drag = null;
        draggingId = null;
        if (pendingState) {
          const nextState = pendingState;
          pendingState = null;
          render(nextState);
        }
      };
      element.addEventListener("pointerup", finish);
      element.addEventListener("pointercancel", finish);
      element.addEventListener("click", () => {
        if (element.dataset.dragged === "true") {
          element.dataset.dragged = "false";
          return;
        }
        selectNode(node.id);
      });
      element.addEventListener("keydown", event => {
        const delta = event.shiftKey ? 24 : 8;
        const direction = { ArrowLeft: [-delta, 0], ArrowRight: [delta, 0], ArrowUp: [0, -delta], ArrowDown: [0, delta] }[event.key];
        if (!direction) return;
        event.preventDefault();
        const position = positions.get(node.id);
        moveNode(node.id, position.x + direction[0], position.y + direction[1]);
      });
    }

    function createNode(node) {
      const element = document.createElement("button");
      element.type = "button";
      element.className = `agent-node ${node.status || "queued"}`;
      element.dataset.agentId = node.id;
      element.setAttribute("aria-label", `${node.role || node.id}, ${node.status || "queued"}. Drag or use arrow keys to reposition.`);
      const heading = document.createElement("span");
      heading.className = "agent-node-heading";
      const dot = document.createElement("span");
      dot.className = "agent-node-dot";
      dot.setAttribute("aria-hidden", "true");
      const role = document.createElement("strong");
      role.textContent = node.role || node.id;
      heading.append(dot, role);
      const task = document.createElement("span");
      task.className = "agent-node-task";
      task.textContent = node.root ? "Root agent" : node.task || node.id;
      const meta = document.createElement("span");
      meta.className = "agent-node-meta";
      meta.textContent = node.root ? "Review root" : [node.model, node.effort].filter(Boolean).join(" · ") || displayName(node.status);
      element.append(heading, task, meta);
      bindDrag(element, node);
      return element;
    }

    function render(nextState, force = false) {
      if (force) {
        draggingId = null;
        pendingState = null;
      }
      if (draggingId && !force) {
        pendingState = nextState;
        return;
      }
      state = nextState;
      const nextSignature = JSON.stringify([mode, state?.round, (state?.agents || []).map(agent => [
        agent.id, agent.parent, agent.role, agent.task, agent.note, agent.status, agent.model, agent.effort,
      ])]);
      if (!force && nextSignature === signature) return;
      signature = nextSignature;
      nodes = buildNodes();
      const visibleIds = new Set(nodes.map(node => node.id));
      [...positions.keys()].filter(id => !visibleIds.has(id)).forEach(id => positions.delete(id));
      [...manuallyPlaced].filter(id => !visibleIds.has(id)).forEach(id => manuallyPlaced.delete(id));
      count.textContent = `${nodes.length - 1} agent${nodes.length === 2 ? "" : "s"}`;
      const focusedId = document.activeElement?.dataset?.agentId;
      nodesLayer.replaceChildren(...nodes.map(createNode));
      placeNodes(force);
      applyPositions();
      selectNode(visibleIds.has(selectedId) ? selectedId : "main");
      if (focusedId && visibleIds.has(focusedId)) {
        nodesLayer.querySelector(`[data-agent-id="${CSS.escape(focusedId)}"]`)?.focus({ preventScroll: true });
      }
    }

    filters.forEach(filter => filter.addEventListener("click", () => {
      mode = filter.dataset.agentFilter;
      filters.forEach(item => item.setAttribute("aria-pressed", String(item === filter)));
      positions.clear();
      manuallyPlaced.clear();
      signature = "";
      render(state, true);
    }));
    reset.addEventListener("click", () => {
      positions.clear();
      manuallyPlaced.clear();
      placeNodes(true);
      applyPositions();
    });
    new ResizeObserver(() => {
      if (!state) return;
      placeNodes(false);
      applyPositions();
    }).observe(canvas);

    return { render };
  }

  window.createAgentGraph = createAgentGraph;
})();
