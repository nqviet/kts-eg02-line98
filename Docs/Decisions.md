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

### D14: Mockup Asset Promotion via GUID-Preserving Move
- **Decision:** Move polished mockup materials and meshes into canonical paths under `Assets/Art/Materials/` and `Assets/Art/Meshes/` rather than re-authoring from scratch.
- **Rationale:** Moving assets preserves GUIDs, keeping all scene references and fine-tuned shader parameters (`_Size`, `_Radius`, `_Border`, `_Inset`, `_Padding`, `_Feather`, render queues) byte-for-byte identical without risk of visual drift or regression.
- **Status:** Approved & Implemented.

### D15: Ordered Replacement of Canonical Placeholders
- **Decision:** Pre-wire theme ScriptableObjects to mockup assets first, delete unpolished placeholder materials (`Universal Render Pipeline/Lit`), and then promote polished assets to canonical paths.
- **Rationale:** Prevents dangling missing-reference GUIDs in `BallThemeSO` and `BoardThemeSO` while establishing the canonical `M_Ball_*` and `M_Board*` naming.
- **Status:** Approved & Implemented.

### D16: Single Canonical Background Consolidation
- **Decision:** Retain the high-resolution stylized anime alpine lake painting as the canonical `T_Background_AlpineLake.png` and remove duplicate/stock photo textures.
- **Rationale:** Perfectly matches the game's modern 2.5D visual aesthetic and eliminates 4.8 MB of redundant texture storage.
- **Status:** Approved & Implemented.

### D17: Design Reference Isolation
- **Decision:** Move `main_scene_mockup.png` out of `Assets/` to `Docs/Design/main_scene_mockup.png`, served in-editor via an ephemeral non-saving overlay tool (`[MenuItem("Line98/Dev/Show Design Reference Overlay _F9")]`).
- **Rationale:** Prevents design mockups from being imported by Unity's AssetDatabase, generating meta files, or bundling into release builds.
- **Status:** Approved & Implemented.

### D18: Strict UI Element Ownership & Sprite Deprecation
- **Decision:** Enforce that uGUI `Image` components carry either a Sprite or a procedural material (`M_Ui_*`), never both or stale unreferenced references. Remove orphaned UI bitmap textures superseded by procedural frosted panel shaders and vector graphics (`VectorIconGraphic`).
- **Rationale:** Eliminates 13 orphaned UI sprites, prevents canvas redraws on duplicate image layers, and reduces package download size.
- **Status:** Approved & Implemented.

### D19: Theme ScriptableObjects as Single Source of Truth
- **Decision:** Make `BallThemeSO` and `BoardThemeSO` authoritative for ball/board meshes and materials, establishing a data-driven pipeline for future cosmetic themes (GDD §16).
- **Rationale:** Eliminates duplicated asset references in `PresentationRoot`, decouples scene composition from visual themes, and enables runtime theme swapping.
- **Status:** Approved & Implemented.

### D20: Bottom-Anchored Action Bar
- **Decision:** Keep the action bar pinned above the safe bottom edge with a designed 240-unit margin; place only a capped share of surplus portrait height above the board.
- **Rationale:** Preserves the 1080×1920 mockup while keeping Undo, Hint, and New Game within the thumb zone on tall phones.
- **Status:** Approved & Implemented.

### D21: Degraded-Aspect Layout Column
- **Decision:** For safe areas squarer than 9:16, scale the entire HUD into a centred 9:16 layout column instead of allowing chrome or the board to overflow.
- **Rationale:** It preserves element proportions, produces non-negative board and camera viewport dimensions, and leaves the backdrop to absorb surplus horizontal space.
- **Status:** Approved & Implemented.

### D22: Action-Button Geometry Source
- **Decision:** Use the live mockup-matching action-card measurements: 296×162 units, with 71/73-unit outer margins.
- **Rationale:** These are the authored scene values and match the reference composition; keeping them in the solver and hierarchy builder eliminates badge and button drift.
- **Status:** Approved & Implemented.

### D23: Landscape V1 Policy
- **Decision:** Keep the player orientation locked to Portrait; editor and windowed landscape views use the centred layout-column fallback.
- **Rationale:** V1 remains a portrait-first mobile game while retaining a usable, non-inverted composition during editor resizing and desktop testing.
- **Status:** Approved & Implemented.

### D24: Canvas-Scaler Width Match
- **Decision:** Retain `CanvasScaler.MatchWidthOrHeight = 0` with a 1080×1920 reference resolution.
- **Rationale:** Existing horizontal artwork is authored for this width. The fitter now derives both canvas axes from the scaler formula, so inset conversion remains correct if the setting changes later.
- **Status:** Approved & Implemented.

### D25: GDD §4 Folder Structure Alignment with 7-Assembly Architecture
- **Decision:** Honour the GDD §4 folder tree (`Core`, `Gameplay`, `UI`, `Audio`, `VFX`, `Data`, `Services`, `Monetization`, `Analytics`, `Editor`, `Tests`) as canonical subfolders under the 7-asmdef layout (`Assets/_Project/`), without creating redundant root folders or churn:
  - `Core` -> `_Project/Core`
  - `Gameplay` -> `_Project/Gameplay`
  - `UI` -> `_Project/Presentation/UI`
  - `Audio` -> `_Project/Presentation/Audio`
  - `VFX` -> `_Project/Presentation/Vfx`
  - `Data` -> `_Project/Data`
  - `Services` -> `_Project/Services`
  - `Monetization` -> `_Project/Services/Ads` and `_Project/Services/Iap`
  - `Analytics` -> `_Project/Services/Analytics`
  - `Editor` -> `_Project/Editor`
  - `Tests` -> `_Project/Tests`
- **Rationale:** Aligns physical file structure with compile units enforced mechanically by asmdefs and `ArchitectureTests`.
- **Status:** Approved & Implemented.

### D26: Frozen Interfaces Declaration and Purchase Service Alias
- **Decision:** Formally declare all 10 frozen interface contracts mandated by GDD P0.1 / Architecture §7: `IRandomSource`, `IScoreConfig`, `ISaveService`, `IDailyChallengeProvider`, `ILeaderboardProvider`, `IThemeProvider`, `IAchievementService`, `IAnalyticsService`, `IAdService`, `IPurchaseService`. Alias `IPurchaseService` to `IIapService` (`IPurchaseService : IIapService`).
- **Rationale:** Freezes all vendor/backend interfaces early, preventing architecture refactoring when Phase 3 (Product) and Phase 4 (Monetization) systems are integrated.
- **Status:** Approved & Implemented.

### D27: Single Palette Authority in BallThemeSO
- **Decision:** Maintain `BallThemeSO` as the single authoritative data asset for the 7 canonical ball colors and materials. Do not author a redundant `PaletteConfig` asset.
- **Rationale:** Prevents configuration drift and duplicate source-of-truth between theme visuals and gameplay palette data.
- **Status:** Approved & Implemented.

### D28: PlayMode Execution Protocol & Full Line-Detection Matrix
- **Decision:** PlayMode execution over CLI/HTTP requires asynchronous execution (`--async_tests true`) followed by polling `test_status` because entering Play Mode triggers domain/scene reload that drops synchronous HTTP connections. The full P1.3/P1.8 line-detection matrix (lengths 5, 6, 7, 8, 9 across H/V/Diagonals, intersecting multi-lines, separate simultaneous lines, and cross scoring) is fully implemented and tested in `LineDetectorTests`.
- **Rationale:** Ensures automated CI and local tooling accurately run and verify scene-level smoke tests without false negatives while guaranteeing mathematical correctness across all line geometries.
- **Status:** Approved & Implemented.

### D29: Session-End Payload Contract (SessionSummary) and Undo Reversibility
- **Decision:** Encapsulate the end-of-session data into an immutable `SessionSummary` struct in `Line98.Core` (`FinalScore`, `BestScore`, `LongestLine`, `LinesCleared`, `TotalMoves`, `CanContinue`). `GameSession.OnGameOver` emits `Action<SessionSummary>` and `GameSession.GetSummary()` provides pull access. `GameSnapshot` captures and restores `LongestLine` so that undo fully rolls back stats to their pre-move value. `GameOverPopup` presents all 5 stats rows and guarantees New Game is available ad-free.
- **Rationale:** Satisfies GDD [P1.7] exit criteria ("end-of-session payload carries every listed stat; New Game is always reachable without an ad") and guarantees zero drift during undo cycles.
- **Status:** Approved & Implemented.

### D30: Feel-Data Plumbing, MotionProfile, FeedbackProfile, and Pooled Line Ribbons
- **Decision:** Re-authored `MotionProfile_Default.asset`, `FeedbackProfile_Tiers.asset`, `MotionPreset_Zen.asset`, and `MotionPreset_ReducedMotion.asset` with verified MonoScript GUIDs. Refactored `MoveAnimator` to consume `MotionProfileSO` (deleting all hardcoded timing constants) and implement waypoint decimation (≤ 10 waypoints if path > 12 cells per Animation §6.1 #8). Refactored `BoardAnimator` to consume `FeedbackTierRule` (stagger outward from placed ball by `StaggerMs * 0.001f`, scale pulse by `AnimationScale`, hold by `HoldMs`, glow by `GlowIntensity`). Implemented 2 pooled `LineRenderer` ribbons (H/V sharing one, diagonals sharing the second) with scrolling UV, gold tint for tier 4, and `CancelByOwner` release semantics. Extended `AssetHygieneTests` to assert all 6 config/profile assets exist and are script-resolvable (guarding against D11). Authored `MotionProfileAssetTests` with asset inspection and behavioural divergence proof.
- **Rationale:** Closes D11, D12, D13, D14; guarantees data-driven animation pacing per Animation §5 & §8 and prevents silent asset serialization breakage.
- **Status:** Approved & Implemented.

### D31: Shuriken VFX Layer, Catalog Architecture, and Pooled Particle Lifecycle
- **Decision:** Implemented the VFX layer via `VfxCatalogSO` and `VfxService`. `VfxCatalogSO` refactored from fixed fields to a structured key→entry table `VfxEntry[] { key, prefab, poolSize, lifetime }` with backward-compatible legacy properties. `VfxService` (`Presentation/Vfx/`) implements ring-buffer particle pooling, burst concurrency cap ≤ 4, popup concurrency cap ≤ 8, `ITickable` simulation, `CancelByOwner` lifecycle release, and 0 GC allocations per play. Authored 10 custom Shuriken particle prefabs in `Assets/_Project/Content/Prefabs/Vfx/` covering selection, flight trail, placement settle, invalid shake, clear tiers 1-4, score popup, and game over frost. Integrated into `PresentationRoot`, `MoveAnimator` (trail attachment during flight, detachment and clear on landing, placement settle), and `BoardAnimator` (centroid tier-based clear burst). Fully validated through `VfxServiceTests` and `AnimAssetAuditTests`.
- **Rationale:** Closes D7 and D15-vfx; feeds D9; guarantees visual juice compliant with Animation §6, §7, §8 and Design Assets §6 within strict mobile performance and memory constraints.
- **Status:** Approved & Implemented.

### D32: Audio Layer, Bus Routing, AudioService Architecture, and Procedural Music Assets
- **Decision:**
  - Created `MainMixer.mixer` at `Assets/_Project/Content/Audio/MainMixer.mixer` containing 5 buses (`Master`, `Music`, `SFX`, `UI`, `Ambience`) and a dedicated `Zen` snapshot.
  - Implemented `AudioService.cs` (`Presentation/Audio/`) featuring 8 pooled SFX AudioSources (voice cap ≤ 8) with oldest-voice eviction, 2 cross-fading music sources + 1 ambience source, immediate same-frame mute/unmute toggles (`SetMusicEnabled`, `SetSfxEnabled`), deterministic pitch variance using `XorShift128` (0 allocations, 0 `UnityEngine.Random`), and bus routing to corresponding mixer groups.
  - Extended `AudioCatalogSO.cs` to table-based `AudioEntry[] { Key, Clip, Bus, Volume, PitchVariance }` with stripped/prefixed alias resolution and migrated all 16 audio assets into `AudioCatalog_Default.asset`.
  - Synthesized and encoded both required BGM files: `bgm_classic_main.ogg` (warm 65 BPM Rhodes/acoustic guitar lo-fi loop, 44.1kHz stereo, seamless loop, Streaming) and `bgm_zen_ambience.ogg` (serene stream/wind/singing bowl pads, 44.1kHz stereo, seamless loop, Streaming).
  - Configured Unity `AudioImporter` settings (`loadType = AudioClipLoadType.Streaming`, `compressionFormat = AudioCompressionFormat.Vorbis`).
  - Wired gameplay call sites in `PresentationRoot.cs` (selection, deselection, invalid move attempt), `MoveAnimator.cs` (`sfx_ball_move_flight`, `sfx_ball_place_settle`), `BoardAnimator.cs` (`sfx_spawn_pop`, clear tier rule `rule.AudioKey`), and `UiButtonFx.cs` (`sfx_ui_button_click` on UI bus).
  - Authored comprehensive test suite `AudioServiceTests.cs` (asserting 8-voice concurrency cap, bus routing, immediate mute/unmute, and catalog resolution for all 16 keys and all feedback tier rules).
- **Rationale:** Closes D8, D15-audio, D21, D24. Satisfies GDD [P2.6], Design Assets §8, and Animation §6, §7, §8 while maintaining strict 0-allocation runtime performance and architectural determinism.
- **Status:** Approved & Implemented.

### D33: Colorblind Pattern Overlay, SRP Batcher Invariance, and Draw-Call Budget Compliance
- **Decision:**
  - Upgraded `PolishedBall.shader` and `BallRimGlow.shader` with an accessibility pattern overlay sampling path consuming `T_Ball_Accessibility_Patterns.png` (512×512 sub-tiles). All material properties (`_BaseColor`, `_PatternRect`, `_PatternColor`, `_PatternStrength`) are fully encapsulated in `CBUFFER_START(UnityPerMaterial)` / `CBUFFER_END`, ensuring complete SRP Batcher compatibility and zero broken batches across all 81 balls.
  - Authored distinct sub-tile rects on all 7 canonical ball materials matching GDD §2 / Concept Vocabularies §9: Red = Circle (`0.076, 0.742, 0.182, 0.182`), Orange = Cross (`0.416, 0.748, 0.170, 0.170`), Blue = Triangle (`0.750, 0.756, 0.170, 0.168`), Green = Diamond (`0.082, 0.406, 0.170, 0.186`), Yellow = Star (`0.414, 0.426, 0.172, 0.164`), Purple = Ring (`0.742, 0.406, 0.186, 0.186`), Cyan = Hexagon (`0.408, 0.086, 0.186, 0.160`).
  - Added `m_PatternsOn` toggle to `BallThemeSO` (default `false` / OFF per spec).
  - Implemented `AccessibilityAuthoring.SetPatternsEnabled(bool)` to toggle `_PatternStrength` (0.0f vs 1.0f) without altering material variants or invalidating batching.
  - Verified theoretical worst-case clear frame draw call budget: Board (1) + Balls (≤ 7 batch) + Shadows (1) + Ribbon (≤ 2) + VFX (≤ 4) = 15 draw calls (with UI Canvas strictly batched), passing within the ≤ 15 mobile draw call budget constraint.
  - Authored comprehensive test suite `AccessibilityAndDrawCallTests.cs` validating tier metrics monotonicity, all 7 unique shape rect assignments, toggle behavior, CBuffer layout, and draw call budget.
- **Rationale:** Closes D9, D10. Satisfies GDD [P2.2], [P2.3], [P2.4], [P2.5] and GDD Gap Resolution #11.
- **Status:** Approved & Implemented.

### D34: Theme Bundle Architecture, `IThemeProvider` Extension, and Projection-Invariant Board Themes
- **Decision:**
  - `ThemeDefinitionSO` models a theme as a named bundle (ball + board + UI + clear effect) with optional single-level `m_InheritsFrom`, listed by `ThemeCatalogSO`.
  - `IThemeProvider` remains **byte-identical** (per D26); UI and clear-effect selection are added by `ICosmeticService : IThemeProvider`.
  - `Presentation` never references `Line98.Services`: resolved `*SO` assets flow down, selection requests flow up through the presentation-owned `IThemeSelector`.
  - Cosmetic selection persists under the isolated key `line98_cosmetics` (`Version = 1`) via `ISaveBackend`, never inside `SaveData`.
  - `m_RowPitchScale = 1.1791784` is a **projection invariant** shared by every board theme, not a per-theme art value.
  - Ball palettes are hue-preserving across themes; `_PatternRect` assignments are identical per color index in every theme (per D33).
  - Today's shipped look is re-identified as the **`classic`** theme (parts: `BallTheme_Classic`, `BoardTheme_Classic`, `UiTheme_Default`, `ClearEffect_Classic`); **`crystal`** ships as the V1 alternative with placeholder colors.
  - Theme ids are **namespaced**: bundle ids and per-category part ids are independent namespaces with independent uniqueness, resolved through separate catalog lookups.
- **Rationale:** Satisfies GDD §16 / [P3.9] ("theme swap through `IThemeProvider` without touching gameplay code; only 2 themes in V1") while scaling to N themes at the cost of one `ThemeDefinitionSO` + one `BallThemeSO` + 7 materials. Preserves the 2.5D baked projection contract, the single-palette authority of D27, the colorblind guarantees of D33, and the frozen-interface commitment of D26.
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
