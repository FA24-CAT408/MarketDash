---
target: current CrazyMarket review dashboard design
total_score: 19
max_score: 40
na_heuristics:
p0_count: 3
p1_count: 2
timestamp: 2026-08-11T00-09-17Z
slug: tools-localreview-dashboard-html
---
## Design Health Score

| # | Heuristic | Score | Key issue |
|---|---|---:|---|
| 1 | Visibility of system status | 2/4 | Freshness can look authoritative after state stops changing. |
| 2 | Match system / real world | 2/4 | Raw workflow language such as `should-fix` and `recorded gates` leaks into the UI. |
| 3 | User control and freedom | 2/4 | The evidence panel and stack can reposition themselves; there is no manual refresh or filter. |
| 4 | Consistency and standards | 3/4 | Status vocabulary is coherent, but naming and token hierarchy drift in smaller details. |
| 5 | Error prevention | 1/4 | A stale or incomplete session can still look safe enough to act on. |
| 6 | Recognition rather than recall | 2/4 | Status colors carry several meanings without a legend or direct explanation. |
| 7 | Flexibility and efficiency | 2/4 | Disclosure state is preserved, but experts cannot filter failures or act on file paths. |
| 8 | Aesthetic and minimalist design | 3/4 | Restrained and coherent, though the largest region carries the least actionable information. |
| 9 | Error diagnosis and recovery | 1/4 | The failure summary tells the operator to search below instead of surfacing the recorded diagnosis. |
| 10 | Help and documentation | 1/4 | Required gates, severity meanings, and the three-round ceiling are unexplained. |
| **Total** |  | **19/40** | **Poor — strong visual foundation, unreliable operational hierarchy.** |

## Design Specificity Verdict

The dashboard feels authored rather than template-generated. Its semantic status palette, keyed evidence reconciliation, preserved disclosure state, reduced-motion support, and mobile phase reflow are deliberate decisions specific to a live engineering review tool.

Its weakness is not generic styling; it is generic judgment. The interface describes workflow state but often stops short of answering the operator's real questions: Is this fresh? What exactly failed? What should I do next? How many required checks remain? How close am I to the three-round limit?

The deterministic CLI detector returned zero findings. The injected live detector found 50 browser-level issues: 32 `tiny-text`, 17 `undersized-ui-text`, and one `first-viewport-column-overflow`. The browser evidence confirms that the page has no horizontal overflow at 1280×800 or 390×844, but on mobile the decision rail begins around y=1878—more than two viewport heights below the top. Eight tiny-text findings were inside collapsed snippet content and are state-dependent; the mobile ordering and small diagnostic text findings are high-confidence.

No reliable user-visible overlay remains open: T3 injection succeeded and reported findings, but the final preview visibility returned false.

## Overall Impression

The first impression is unusually good for a local engineering dashboard: quiet, deliberate, and credible. The central opportunity is to make the information architecture as decisive as the visual system. Today, the dashboard earns trust in the calm state and spends it when data is stale or a phase fails.

## What's Working

- Polling is thoughtfully implemented. Fingerprints prevent unnecessary reconstruction; snippets retain disclosure state; keyed evidence updates do not restart video playback.
- The visual system is restrained and consistent. Status colors are semantic, motion is limited to active work, and reduced-motion is respected.
- Responsive behavior is structural rather than cosmetic. The phase rail becomes a vertical sequence and the layout avoids horizontal overflow on mobile.
- Empty states are specific to their surfaces instead of falling back to generic “no data” copy.

## Priority Issues

### P0 — Freshness can make stale state look live

**Why it matters:** On mobile, any valid timestamp becomes the literal word “Live.” On desktop, an old timestamp retains the same green connection treatment as a recent one. If the state file stops advancing while the HTTP server still responds, the operator can make a release decision from stale evidence.

**Fix:** Show relative age at every viewport. Add amber and red staleness thresholds driven by a client clock, and reduce the authority of the rest of the page when state becomes stale.

**Suggested command:** `$impeccable harden`

### P0 — Failure state withholds the diagnosis already present in state

**Why it matters:** “The active phase needs attention” followed by “use the details below” forces the operator to scan a long gate list. On mobile, that list is far below the fold. The failing check name and note are already available to `renderDecision`.

**Fix:** Promote the first failing gate or blocker into the decision panel, render its note directly, and summarize additional failures as “+N more.”

**Suggested command:** `$impeccable clarify`

### P0 — The gate fraction can overstate readiness

**Why it matters:** The denominator contains only checks already recorded, and `skipped` counts as clear. A session can therefore show `3/3` while required gates were never registered or two of those three were skipped.

**Fix:** Source the required gate set from the review-loop state contract. Show passed, skipped, failed, and not-run counts separately rather than collapsing them into one reassuring fraction.

**Suggested command:** `$impeccable harden`

### P1 — Actionable information has the least space and arrives too late on mobile

**Why it matters:** Desktop gives roughly 70% of width to phases and reviewer cards while decision, gates, findings, and stack share the narrow rail. On mobile, reviewer activity pushes the decision panel more than two screens down.

**Fix:** Put the decision summary full-width at the top. Make gates and findings the primary work area; demote phase/reviewer history to a secondary column or disclosure.

**Suggested command:** `$impeccable layout`

### P1 — Findings do not prioritize blockers

**Why it matters:** Findings remain in insertion order. A blocker can sit below suggestions, and severity is carried by tiny text and a small dot rather than position and hierarchy.

**Fix:** Sort blocker → should-fix → suggestion, then open → deferred. Add severity counts to the section heading and give blockers a stronger non-color cue.

**Suggested command:** `$impeccable distill`

## Persona Red Flags

**Alex, power user:** The dashboard cannot filter directly to failed gates or blockers, file paths are inert, and the decision panel narrates status instead of presenting the next action. Alex still has to synthesize four panels manually.

**Sam, accessibility-dependent user:** The semantic structure and native disclosures are good, but the detector found extensive 10–11px status and note text. Routine timestamp updates use an `aria-live` region, which may announce noise instead of meaningful connection changes. Severity also relies too heavily on color and compact labels.

**Casey, mobile user:** The layout avoids horizontal overflow, but the most important decision content begins more than two viewport heights below the top. A stale timestamp is replaced by “Live,” which removes rather than compresses critical trust information.

## Minor Observations

- `--muted` and `--faint` currently resolve to the same values, creating a token distinction without a visible hierarchy.
- Evidence images use `object-fit: cover`, which can crop the Unity artifact being reviewed; video correctly uses `contain`.
- “Review control room” and “CrazyMarket Review” compete as product names.
- The round metric omits the `/3` ceiling even though the workflow stops after three rounds.
- Reviewer ordering prioritizes running status but does not consistently elevate blocked or failed reviewers.

## Questions to Consider

- Should the first viewport optimize for “what is happening,” or “what must I do next”?
- Is a skipped gate evidence of clearance, or a separate state that must remain visible?
- When the dashboard is stale, should the entire surface visibly lose authority rather than changing one status label?
- If only one panel could remain, would operators keep the phase diagram or the failing-gate diagnosis?
