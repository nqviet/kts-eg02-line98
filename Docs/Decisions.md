# Architecture Decision Records (ADR) — LINE 98: Color Lines

## Decisions Log (D1 – D8)

### D1: Editor Version Pinning
- **Decision:** Keep Unity `6000.6.0f1` pinned in `ProjectSettings/ProjectVersion.txt`. Do not auto-upgrade.
- **Rationale:** `6000.6.0f1` is the native editor already installed and used to import existing project assets. Downgrading to `6000.0.x LTS` would require a ~10 GB download and full reimport, blocking M1. Revisit at the scheduled M3 upgrade window.
- **Status:** Approved.

### D2: Android Build Support & Toolchain
- **Decision:** Install Android Build Support (`android`, `android-sdk-ndk-tools`, `android-open-jdk-17.0.18+8`) when running Android builds.
- **Rationale:** Required for ARM64 IL2CPP builds, ASTC texture compression verification, and AAB package size profiling.
- **Status:** Approved.

### D3: Prototype Deprecation & Removal
- **Decision:** Archive and remove the legacy prototype (`Line98Game` + `Assets/Scripts/{Design, Model, Views}`) from `Assets/`.
- **Rationale:** The legacy prototype lives in `Assembly-CSharp`, uses `System.Random`, lacks asmdef encapsulation, and violates the architectural separation mandated by Architecture §1 and §25. Full git history preserves the code at commits `bf91d07` and `1166701`. Palette and definition values are extracted and migrated into `Line98.Data`.
- **Status:** Approved.

### D4: Approval of 12 GDD-Gap Defaults
- **Decision:** Formally adopt all 12 proposed defaults from Concept Vocabularies §14.
- **Rationale:** Eliminates gameplay and architecture ambiguities before writing core simulation code.
- **Status:** Approved (see details below).

### D5: Analytics Vendor Abstraction
- **Decision:** Ship `DebugAnalyticsService` and `NoOpAnalyticsService` behind `IAnalyticsService` for V1.
- **Rationale:** Concrete vendor integration (Firebase vs. Unity Analytics) is deferred to M5 to prevent vendor SDK lock-in from polluting gameplay code.
- **Status:** Approved.

### D6: Unity Pro License for Splash Screen Removal
- **Decision:** Confirm Unity Pro seat before the M5 store release.
- **Rationale:** Unity Personal cannot disable the splash screen; disabling it is a Definition of Done criterion for the final release.
- **Status:** Approved.

### D7: CI on GitHub Actions (GameCI)
- **Decision:** Run verification gates locally via the Unity CLI (`unity test`, `unity build`) during M1; configure remote GitHub Actions CI at the conclusion of M1.
- **Rationale:** GameCI requires a Unity license secret; local CLI execution provides equivalent gate enforcement immediately without external credential configuration.
- **Status:** Approved.

### D8: Retention of Automation Packages
- **Decision:** Retain `com.unity.pipeline` (0.7.0-exp.1) and `com.unity.ai.assistant`.
- **Rationale:** Enables CLI and MCP-driven headless editor automation and live editor commands.
- **Status:** Approved.

### D9 (D-A): 7-Color Palette Alignment (Pink to Blue)
- **Decision:** Swap `BallColor.Pink` to `BallColor.Blue` in the domain model.
- **Rationale:** Aligns with `LINE_98_Design_assets.md`, `M_Ball_Blue.mat`, `sp_ball_blue.png`, and the icon specification.
- **Status:** Approved & Implemented.

### D10 (D-B): Zero-Allocation Move Pipeline
- **Decision:** Use pooled double-buffered `MovePlan` and pre-allocated `SpawnItemsBuffer` with pooled callback slots.
- **Rationale:** Guarantees 0 bytes allocated per move during gameplay across frame boundaries.
- **Status:** Approved & Implemented.

### D11 (D-C): Save Data Serialization Format
- **Decision:** Commit to `Newtonsoft.Json` for atomic, versioned, migration-friendly saves.
- **Rationale:** Outlined in Technical Stack §1 & Architecture §9.
- **Status:** Approved.

### D12 (D-D): Service Decomposition Timing
- **Decision:** Maintain `GameSession` unified command target through M2; extract distinct sub-services during M3 product layer.
- **Rationale:** Eliminates unnecessary abstraction churn while implementing presentation and game feel.
- **Status:** Approved.

### D13 (D-E): Quality Tiers & 60 FPS Framerate Target
- **Decision:** Prune `QualitySettings.asset` to Low, Medium, High; set `targetFrameRate = 60` and `vSyncCount = 0`.
- **Rationale:** Ensures deterministic benchmarks and prevents battery waste/throttling on mobile devices.
- **Status:** Approved & Implemented.

---

## GDD Gap Resolutions (Concept Vocabularies §14)

1. **Game Over Condition (§2):**
   Game over triggers when a spawn batch cannot place any balls (0 empty cells remain) OR when no ball on the board has an empty 4-directional neighbor. Evaluated after move resolution, prior to autosave.
2. **Partial Spawning (§2):**
   If only 1 or 2 empty cells remain, spawn as many balls as fit. If 0 cells fit, trigger Game Over. Preview tray displays the shortfall.
3. **Color Progression Ramp (§2):**
   Controlled via `SpawnColorPolicySO`. Start the run with 5 active colors; introduce the 6th color after 10 lines cleared, and the 7th color after 25 lines cleared.
4. **Combo Multiplier (§7):**
   Formula: `1.0 + 0.25 * (runCount - 1)`, capped at `2.0x`. Configured in `ScoreTableSO.comboStep`.
5. **Simultaneous Multi-line Clear (§6):**
   Award the score for each distinct run. The intersecting/shared ball is cleared once and its cell is not awarded twice.
6. **Free Undo Allowance (§12):**
   3 free undos granted per game session/run. Additional undos (up to 3 more) require a rewarded ad.
7. **Daily Challenge Undo Restriction (§12, §15):**
   Undo is strictly disabled in Daily Challenge mode to maintain competitive fairness for shared date seeds. Hints remain permitted.
8. **Zen Mode Rules (§3):**
   Endless play under standard game-over rules. Score is hidden, ads are disabled, undo is unlimited, ambience audio bus active, and session scores do not pollute career high score statistics.
9. **Interstitial Ad Placement (§21):**
   Capped at a minimum 150-second interval. Permitted only on `GameOver -> New Game` transitions and menu returns; never during active resolution, never within 2 moves of a clear, and never in the first session's first 3 games.
10. **Result Sharing (§15):**
    V1 formats a shareable emoji/text grid copied directly to the clipboard via `GUIUtility.systemCopyBuffer`. Native OS share sheet deferred to V1.1.
11. **Accessibility / Color-Vision Deficiency (§9):**
    Provide a toggleable pattern hint overlay (shape glyphs per color) rendered via accessibility texture masks.
12. **Camera Tuning Scope & 3D Orthogonal Camera (§10):**
    Camera architecture uses a **3D orthogonal camera** (orthographic projection tilted in 3D space at Pitch 58°, Yaw 0°), strictly governed by `CameraProfileSO` (`IsOrthographic = true`, `OrthographicSize = 7.5`, Pitch 58°, Yaw 0°, Distance 18.5, Parallax 0.05). This eliminates perspective foreshortening and edge distortion, ensuring perfectly uniform cell sizing and touch targets across all rows while preserving full 3D lighting, bevel depth, crystal reflections, and procedural screen adaptation across portrait aspect ratios (9:16 to 9:22) via `BoardFitSolver.SolveOrthographicSize`.
