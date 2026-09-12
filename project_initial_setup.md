# LINE 98 — Initial Project Setup Plan

## 1. Audit: what's actually in the repo today

| Area | Current state | Docs expect | Gap |
|---|---|---|---|
| Editor | `6000.6.0f1`, **only editor installed**, modules: **`Web` only** | `6000.0.x LTS` pinned, IL2CPP/ARM64 Android builds | **No Android module** → no Android/IL2CPP/ASTC build possible yet. Doc's version line is stale. |
| Template | URP 2D template (`Assets/Settings/Renderer2D.asset`) | URP **3D Forward** + perspective 2.5D | Renderer must be swapped, not configured |
| Renderer wiring | `UniversalRP.asset` → renderer list = `Renderer2D` | `UniversalRendererData` (3D) | Must add 3D renderer + remove 2D |
| Scene | Single `SampleScene.unity`, camera **orthographic**, hosts `Line98Game` | `Boot` / `Menu` / `Game`, perspective FOV 28°, tilt 58° | Scenes don't exist |
| Code | `Assets/Scripts/{Line98Game, Design, Model, Views}` in **`Assembly-CSharp`**, no asmdefs, `System.Random`, runtime-built uGUI, `Resources.Load` with **no `Assets/Resources` folder** (silently falls back to `CreateDefault()`) | 7 asmdefs, `XorShift128`, plan/commit pipeline | Prototype contradicts the entire arch §1/§3 contract |
| Packages | URP 17.6, Input System 1.20, uGUI 2.6, Test Framework 1.8, Rider 3.0.38, Timeline, VisualScripting, Collab, 2D suite, `com.unity.pipeline` 0.7.0-exp, ai.assistant, ai.inference | + Localization, Newtonsoft, Addressables, Purchasing, Memory Profiler, Android Logcat | **6 required packages missing**; several banned/2D packages present |
| ProjectSettings | Portrait? **auto-rotate (`4`)**, `productName: universal_2d_practice`, `AndroidTargetArchitectures: 2` (not ARM64-only), stripping default, Arch/IL2CPP ✅, Linear ✅, Input System ✅ | Portrait locked, ARM64-only, stripping High, GLES3 primary, ASTC 6×6 | One settings pass needed |
| Repo hygiene | git ✅ (4 commits, `master`), `.gitignore` ✅ | `.editorconfig`, CI, Git LFS, `Docs/` | Missing |

**Two doc-vs-doc inconsistencies to record** (don't let them reach CI): vocab §2's asmdef graph omits `App → Presentation` (arch §1 adds it — arch decision #4 is correct); vocab §9's "1–2 draw calls via MaterialPropertyBlock" is superseded by arch decision #3 (7 shared materials). The guardrail test must assert **arch §1's** edge set.

---

## 2. Decisions needed before I touch anything

| # | Decision | Recommendation | Why |
|---|---|---|---|
| **D1** | Editor pin: `6000.6.0f1` (installed, project-native) vs `6000.0.83f1`/`6000.3.24f1` LTS | **Keep `6000.6.0f1`, pin it, stop auto-upgrading.** Record the deviation in the tech doc. | Only installed editor; assets were imported by it. Downgrading now is cheapest *but* costs a ~10 GB install + full reimport and blocks M1. Revisit at the M3 upgrade window. |
| **D2** | Install Android Build Support + OpenJDK/SDK/NDK | **Yes, now** (`unity install-modules -e 6000.6.0f1 -m android ...`) | Without it, IL2CPP/ARM64/ASTC/stripping issues surface at M4 instead of M1. Also blocks the AAB budget check. |
| **D3** | Fate of the existing prototype (`Line98Game` + `Design/` + `Model/` + `Views/`) | **Archive & remove from `Assets/`** (it stays in git history at `bf91d07`/`1166701`). Mine `DefaultLine98Theme.asset` for palette values first. | It cannot coexist with the asmdef edge test: `Assembly-CSharp` + `Model/Views/Design` re-introduces exactly the coupling arch §25 forbids. Keeping it as a second implementation guarantees drift. |
| **D4** | Approve the 12 GDD-gap defaults (vocab §14) | **Approve all 12** — especially #3 (5 colors → 6th at 10 lines → 7th at 25 lines) and #7 (undo disabled in Daily) | These are load-bearing for `SpawnColorPolicySO`, `IGameModeStrategy` and `ScoreTableSO`; changing them later means touching Core |
| **D5** | Analytics vendor | Ship `DebugAnalyticsService` + `NoOpAnalyticsService` (as documented) | Keeps the vendor decision at M5 with zero rework |
| **D6** | Unity Pro (splash removal) | Confirm before M5 | Personal cannot disable the splash; it's a DoD item for the store build |
| **D7** | CI on GitHub Actions (GameCI) | **Defer to end of M1**, keep the gate commands CLI-local until then | Needs a Unity license secret; `unity test` gives the same gate locally today |
| **D8** | Keep `com.unity.pipeline` (0.7.0-exp) | **Keep — do not remove** | It is what powers `unity command` / MCP-driven editor automation. Same for `ai.assistant` if you use the in-editor agent. |

---

## 3. Setup phases (each with a hard gate)

| Phase | Deliverable | Gate |
|---|---|---|
| **P0 — Decisions & freeze** | `Docs/Decisions.md` (ADR log with D1–D8 + the 12 gap defaults); tech doc version numbers corrected to what actually resolves | Doc committed |
| **P1 — Platform baseline** | Pin `ProjectVersion.txt`; product identity (`LINE 98: Color Lines`, company); Orientation **Portrait locked**; `AndroidTargetArchitectures` **ARM64 only**; scripting backend IL2CPP ✅; stripping **High** + `link.xml`; GLES3 primary / Vulkan off / Auto off; ASTC 6×6; min SDK 26 ✅; quality levels pruned to 3 tiers (`Low/Mid/High`) with `m_CurrentQuality` sane; vSync off, `targetFrameRate = 60`; Lighting: auto-generate off, no baked/realtime GI, flat ambient; `.gitattributes` + Git LFS for `*.fbx *.png *.psd *.wav *.ogg *.blend`; branch `master → main` | `unity build --target StandaloneWindows64` succeeds; settings diff reviewed |
| **P2 — Render pipeline switch (2D → 2.5D)** | Create `Assets/_Project/Content/Rendering/URP_UniversalRenderer_3D.asset` (`UniversalRendererData`); assign to `UniversalRP.asset` + all quality levels; delete `Renderer2D.asset`, `Lit2DSceneTemplate.scenetemplate`, `Settings/Scenes/`; URP asset: SRP Batcher ON, GPU instancing ON, **shadows off / distance 0**, depth+opaque texture off, render scale 1.0 (0.9 low tier), MSAA 2× (low) / 4× (mid/high), HDR off on low tier, camera `FOV 28 / pitch 58 / yaw 0` on a `CameraProfileSO` — **not** per-device | Game view renders the greybox board with real depth; Frame Debugger shows instanced balls |
| **P3 — Package manifest** | Apply the delta in §5; let Unity resolve; commit `packages-lock.json`; verify no compile errors and no silently re-added modules | `unity test --mode EditMode` compiles clean; manifest diff reviewed |
| **P4 — Repo skeleton & guardrails** | Folder tree + **7 asmdefs** (§4); `.editorconfig` (naming rules enforcing `m_` / `s_` / `On` conventions + Roslyn severities for `-warnaserror`); delete `Assets/Welcome`, `Assets/Scripts`, `Assets/Scenes/SampleScene`, `Assets/Data`; create `Boot`/`Menu`/`Game` scene stubs; `Docs/` (ReleaseChecklist, DesignReviewRubric, StoreListing) | **`ArchitectureTests` green**: exactly the 11 edge set, zero `UnityEngine.Random` in Core/Gameplay, zero banned type names |
| **P5 — M1 playable core** | `Line98.Core` then `Line98.Gameplay` then `Line98.App`, in arch §12's exact file order, **each file landing with its test**; `AppRoot.Tick` as the single Update; `IMovePacer` + `ImmediatePacer`; IMGUI `DebugHud` (`#if DEVELOPMENT_BUILD \|\| UNITY_EDITOR`); `Boot`/`Game` scenes wired | A full game playable with primitives: move, BFS path, clear, spawn 3, game over, score |
| **P6 — Gates & baseline** | EditMode suite (10 incl. `ArchitectureTests`, `DeterminismReplayTests`), PlayMode `SmokeTests`, Windows dev build, Android AAB baseline (after D2) | All green; sizes recorded in `Docs/ReleaseChecklist.md` (AAB ≤25 MB target) |
| **P7 — M2 kickoff (parallel art track)** | Greybox assets, palette SOs (design doc's exact 7 hexes), material stubs ×7, `CameraProfileSO`, `MainMixer` 5 buses, `VfxCatalogSO` (10 entries), `AudioCatalogSO` (16 entries), **font validation** (Be Vietnam Pro/Inter, VI diacritics — validate *now*, not at M5), 3 canvases + atlas skeleton | Placeholder board/ball render at 60 FPS in the Game scene |

**Mechanism note:** P1–P2 settings should be applied by an idempotent editor script (`Line98.Editor.ProjectSetup.Apply()`, menu item `Line98/Setup/Apply Project Baseline`) rather than hand-edited YAML, so the baseline is reviewable and re-runnable in CI.

---

## 4. Target layout & the asmdef contract

```text
Assets/_Project/
  Core/          Line98.Core.asmdef          (noEngineReferences: true)
  Data/          Line98.Data.asmdef          + Definitions/  (SO class definitions)
  Gameplay/      Line98.Gameplay.asmdef
  App/           Line98.App.asmdef
  Presentation/  Line98.Presentation.asmdef  Board/ UI/ Audio/ Vfx/ Camera/
  Services/      Line98.Services.asmdef      Save/ Ads/ Iap/ Analytics/ Localization/
  Editor/        Line98.Editor.asmdef        (Editor-only)
  Tests/EditMode/   Line98.Tests.EditMode.asmdef
  Tests/PlayMode/   Line98.Tests.PlayMode.asmdef
  Content/       Scenes/{Boot,Menu,Game}.unity · Prefabs/ · Materials/ · Themes/
                 Audio/ · Localization/ · Rendering/ · Art/ · Definitions/ (.asset instances)
Docs/  .editorconfig  .gitattributes  .github/ (deferred)
```

```csharp
// Assets/_Project/Core/Line98.Core.asmdef
{
  "name": "Line98.Core",
  "rootNamespace": "Line98.Core",
  "references": [],
  "includePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "autoReferenced": false,
  "noEngineReferences": true        // §25: Core cannot even see UnityEngine
}
```

Edge set the guardrail test must assert (one-way, no cycles) — **arch §1, not vocab §2**:

| From | To |
|---|---|
| `Line98.Data` | Core |
| `Line98.Gameplay` | Core, Data |
| `Line98.App` | Gameplay, Presentation, Services, Data |
| `Line98.Presentation` | Gameplay, Data |
| `Line98.Services` | Core, Data |
| `Line98.Editor` | Gameplay (+ Data, Presentation as needed) |
| `Tests.EditMode` | Core, Gameplay |
| `Tests.PlayMode` | Core, Gameplay, App |

Anything else — especially **Gameplay → Presentation** — fails CI.

---

## 5. `Packages/manifest.json` delta

| Action | Package | Note |
|---|---|---|
| **Add** | `com.unity.nuget.newtonsoft-json` | Versioned save + migrations (§23) — install now |
| **Add** | `com.unity.localization` | EN/VI (§28) — install now, tables can wait for M3 |
| **Add** | `com.unity.addressables` | Install now to lock the version; **create no groups until M2** (themes/audio, local only) |
| **Add** | `com.unity.purchasing` | Evaluate 5.x vs 4.12 at M4; IoC stub is `EditorStubIapService` |
| **Add** | `com.unity.memoryprofiler`, `com.unity.mobile.android-logcat` | Editor-only, zero runtime cost |
| **Remove** | `com.unity.visualscripting`, `com.unity.timeline`, `com.unity.collab-proxy`, `com.unity.learn.iet-framework` | Banned "framework gravity" / unused |
| **Remove** | `com.unity.2d.{animation, aseprite, psdimporter, spriteshape, tilemap, tilemap.extras, tooling}` | Project is 3D-forward; **keep `com.unity.2d.sprite`** for Sprite Atlas |
| **Remove modules (P3)** | `ai`, `terrain`, `terrainphysics`, `cloth`, `vehicles`, `wind`, `xr`, `tetgen`, `videoclip`*, `director`, `timelinefoundation`, `umbra`, `vectorgraphics`, `adaptiveperformance` | Exactly the tech doc §5 list + obvious template fat |
| **Keep (do not remove)** | `com.unity.pipeline`, `ai.assistant`/`ai.inference`, `ide.rider`, `ide.visualstudio`, `ugui`, `inputsysten`, `test-framework`, `modules.physics`/`physics2d` | `pipeline` = CLI/MCP automation; physics modules are referenced by uGUI's `*Raycaster` — removing them risks the UI assembly. Enforce "zero colliders" with a guardrail test instead, and defer "optional" module trims to the M5 size audit (with a build verification each time — modules get silently re-added by dependencies). |

Doc version lines are already superseded by reality (Input System 1.20 vs 1.11, Test Framework 1.8 vs 1.4, uGUI 2.6 vs 2.0) — treat the doc as intent, the registry as truth, and update the doc in P0.

---

## 6. Verification gates (exact commands)

```sh
unity install-modules -e 6000.6.0f1 -l                    # then -m <android module ids>
unity test  --mode EditMode  --output TestResults/editmode.xml
unity test  --mode PlayMode  --output TestResults/playmode.xml
unity build --target StandaloneWindows64 -o Builds/Win/Line98.exe
unity build --target Android --android-export-type aab -o Builds/Android/Line98.aab   # after D2
```

M1's exit criteria (from arch §12 + §11): all EditMode suites green, PlayMode smoke green, `-warnaserror` clean, Windows build runs, and `ArchitectureTests` + `DeterminismReplayTests` prove the structural rules rather than documenting them.

---

## 7. Top risks in this setup

1. **Android module absence** is the only true blocker in the list — install it in the background during P1.
2. **Renderer swap before M1** is non-negotiable; retrofitting 3D depth after `BoardView` exists means rebuilding positions, materials and the camera solver.
3. **Prototype drift** — if D3 is declined, `ArchitectureTests` must be relaxed, which weakens every downstream guardrail. This is the one decision I'd push hardest on.
4. **Vietnamese font** — validate glyph coverage at P7, not M5, or the whole UI atlas/typography pass gets redone.
