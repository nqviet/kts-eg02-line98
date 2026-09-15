# Asset Conventions & Authority — LINE 98: Color Lines

## 1. Directory Structure & Folder Authority

All art assets in the project reside strictly under `Assets/Art/`, partitioned by type:

```text
Assets/
  Art/
    Materials/                M_Ball_*.mat (7 hues), M_BoardCell, M_BoardFrame, M_Backdrop
      Themes/Crystal/         M_Ball_Crystal_*.mat (7 gemstone hues with D33 pattern parity)
      UI/                     M_Ui_Card, M_Ui_Action, M_Ui_Square, M_Ui_Tray, M_Ui_Hint, M_Ui_HintGlow
                              M_Ui_Shadow_*.mat (7 procedural drop shadows)
    Meshes/                   MESH_BoardCell, MESH_BoardFrame, MESH_Ball_Gem_Centered, MESH_Backdrop_Quad
    Models/                   SM_*.obj, SM_*.fbx (DCC source geometries, e.g. SM_BlobShadow)
    Sprites/
      Balls/                  sp_ball_*.png (2D preview queue icons)
      UI/                     ui_icon_crown_gold.png, ui_brand_ball_quad.png, ui_overlay_modal_scrim.png
      VFX/                    Particle textures and masks
    Textures/                 T_* (spec textures, e.g. T_Background_AlpineLake.png)
  _Project/
    Content/
      Definitions/            ScriptableObject definitions (ThemeDefinitionSO, ThemeCatalogSO, BallThemeSO, BoardThemeSO, ClearEffectSO, etc.)
      Scenes/                 Authored scenes (Game.unity)
      Shaders/                ShaderLab, HLSL, and Shader Graph shaders
Docs/
  Design/                     Design reference art, mockups, baseline captures (outside Assets/, zero build footprint)
```

---

## 2. Naming Prefixes & Taxonomy

| Asset Type | Prefix | Target Directory | Examples |
|---|---|---|---|
| Material | `M_*` | `Assets/Art/Materials/` | `M_Ball_Red.mat`, `M_BoardCell.mat`, `M_Backdrop.mat` |
| UI Material | `M_Ui_*` | `Assets/Art/Materials/UI/` | `M_Ui_Card.mat`, `M_Ui_Action.mat`, `M_Ui_Shadow_Card_Score.mat` |
| Baked Mesh | `MESH_*` | `Assets/Art/Meshes/` | `MESH_BoardCell.asset`, `MESH_Ball_Gem_Centered.asset` |
| DCC Source Model | `SM_*` | `Assets/Art/Models/` | `SM_BlobShadow.obj`, `SM_Ball_Gem.fbx` |
| Texture | `T_*` | `Assets/Art/Textures/` | `T_Background_AlpineLake.png`, `T_Blob_Shadow_Soft.png` |
| Gameplay Sprite | `sp_*` | `Assets/Art/Sprites/Balls/` | `sp_ball_red.png`, `sp_ball_blue.png` |
| UI Sprite | `ui_*` | `Assets/Art/Sprites/UI/` | `ui_icon_crown_gold.png`, `ui_brand_ball_quad.png` |
| VFX Prefab | `VFX_*` | `Assets/Art/VFX/` | `VFX_Clear_Tier1_5Balls.prefab` |

---

## 3. Hygiene Rules & Banned Tokens

1. **Banned Path Substrings:**
   No asset path under `Assets/Art/` or `Assets/_Project/` may contain:
   `mockup | mock | copy | temp | placeholder` (case-insensitive).
   *Enforcement:* Automated EditMode test `Line98.Tests.EditMode.AssetHygieneTests.NoProjectAssetPath_ContainsBannedTokens`.

2. **Design Reference Assets:**
   Design mockups, UI reference comps, and alignment reference images must live under `Docs/Design/` outside the `Assets/` tree. They are never imported by Unity's AssetDatabase and never packaged into player builds.
   *Editor Overlay:* The editor alignment overlay (`Line98/Dev/Show Design Reference Overlay _F9`) loads `Docs/Design/main_scene_mockup.png` via direct file I/O at edit time using `HideFlags.DontSave`.

3. **No Unreferenced Assets:**
   Every material, mesh, and sprite checked into `Assets/` must have an active reference in an authored scene, prefab, or ScriptableObject definition.

4. **UI Element Ownership (Decision D18):**
   A uGUI `Image` component carries **either** a Sprite **or** a custom procedural material (`M_Ui_*`), never both and never a stale unreferenced sprite assignment. Neumorphic panels, cavities, and action buttons use procedural frosted shaders; icon graphics use `VectorIconGraphic` procedural vectors.

---

## 4. Preserved 2.5D Baked Conventions

The board visual presentation uses two mathematical contracts coupling the 3D meshes to the tilted orthographic camera (`Pitch = 58°`, `Yaw = 0°`):

### 4.1 Floor-Pivot Z-Shift Contract
In `MESH_Ball_Gem_Centered.asset`:
- The pivot of the ball mesh is baked with a z-shift:
  $$\Delta Z = \frac{\text{Radius}}{\tan(58^\circ)} = \frac{0.30}{\tan(58^\circ)} \approx 0.18746$$
- **Purpose:** Positions the pivot point directly on the resting cell floor on the board plane $(Y=0)$. This ensures that scale tweens, bounce animations, squash-and-stretch, and contact blob shadows ground naturally without requiring per-frame manual height offsets.

### 4.2 Row Pitch Scale Compensation
In `BoardTheme_Classic.asset`:
- `m_RowPitchScale = 1.1791784`:
  $$\text{RowPitchScale} = \frac{1}{\sin(58^\circ)} \approx 1.1791784$$
- **Purpose:** Compresses foreshortening along the tilted camera's vertical axis. When viewed through the orthographic camera at Pitch $58^\circ$, grid cells and spacing appear mathematically square ($1:1$) on the device screen.
