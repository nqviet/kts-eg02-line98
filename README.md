# LINE 98: Color Lines — Build Guide

How to produce the **Windows (Win32 desktop, x64)** and **Android** builds.

The project is pinned to Unity **`6000.6.0f1`** (`ProjectSettings/ProjectVersion.txt`) — build with that
exact version, never a different one mid-milestone.

**Demo**: https://monosnap.ai/file/xXdSdlwXjqfqInOwiqdkbJ1itavWvx

## Targets at a glance

|  | Windows | Android |
|---|---|---|
| Build target | `StandaloneWindows64` (x64) | `Android` |
| Scripting backend | Mono | IL2CPP |
| Architecture | x86_64 | ARM64 only |
| Graphics API | D3D11 | GLES3 (primary) |
| Output | `.exe` | `.apk` (device testing) / `.aab` (store) |
| Suggested output path | `Builds/Windows64/` | `Builds/Android/` |

## Prerequisites

- Unity **6000.6.0f1** installed via Unity Hub. Windows build support ships with the Editor on Windows;
  Android needs the **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** modules.
  Check with `unity editors --installed`.
- For the command-line steps below: the Unity CLI (`unity`), signed in and licensed
  (`unity auth status`, `unity license status`).

---

## 1. Windows build

### From the Editor

1. Open the project (`unity open`, or Unity Hub).
2. **File → Build Profiles**, then **Add Build Profile → Windows**.
3. Set **Architecture** to `x86_64` and **Scene List** to the three scenes already in Build Settings
   (`Boot`, `Menu`, `Game`).
4. **Build** (or **Build And Run**) into `Builds/Windows64/`.

### From the command line

Run from the project root (the CLI defaults the project to the current directory):

```sh
unity build --target StandaloneWindows64 --output-path Builds/Windows64/LINE98.exe
```

Result: `Builds/Windows64/LINE98.exe` plus the `LINE98_Data/` folder next to it.

---

## 2. Android build

Android is a non-desktop target, so `unity build` needs a **Build Profile** (or a custom
`--execute-method`); `--target Android` on its own is not enough.

### From the Editor

1. **File → Build Profiles**, then **Add Build Profile → Android**.
2. **Name the profile `Android`** — with that name the CLI steps below can reference it directly.
   It is saved as `Assets/Settings/Build Profiles/Android.asset`.
3. Configure it to match the project's Android settings:
   - **Scripting Backend** `IL2CPP`, **Target Architectures** `ARM64` only.
   - **Graphics APIs**: `OpenGLES3` first (add Vulkan second only when profiling says so).
   - **Orientation**: portrait, locked.
   - **Minimum API Level** `26`; set the target API level to the Play-mandated one at submission.
   - **Texture compression**: ASTC 6×6.
   - **Package name**: the project currently has no Android identifier set (only `Standalone` is defined
     in `ProjectSettings`), so set it here before the first store build.
4. Set **Export Format** to **APK** for device testing or **AAB** for a Play upload, then **Build**.

### From the command line

Test build for a device:

```sh
unity build --profile Android --android-export-type apk --output-path Builds/Android/LINE98.apk
```

Store build:

```sh
unity build --profile Android --android-export-type aab --output-path Builds/Android/LINE98.aab \
  --android-version-code 1 \
  --android-target-sdk-version <Play-mandated level>
```

Signed release build (App Bundle signing):

```sh
unity build --profile Android --android-export-type aab --output-path Builds/Android/LINE98.aab \
  --android-keystore-base64 "$KEYSTORE_BASE64" \
  --android-keystore-password "$KEYSTORE_PASSWORD" \
  --android-key-alias "$KEY_ALIAS" \
  --android-key-alias-password "$KEY_ALIAS_PASSWORD"
```

Keystore values passed as flags land in the process list and CI logs — keep them in a secret store and
mask them in CI output. Symbol upload (`--android-symbol-type public` or `debugging`) is available for
Play Console crash reporting.

---

## Notes

- **Close the Editor before a CLI build.** `unity build` spawns its own batch-mode Editor, and a second
  Unity instance cannot open the same project.
- **Uncommitted changes are rejected** by the build guard. Commit first, or pass `--allow-dirty-build`.
- Every build writes a log to `Logs/build-<target>-<timestamp>.log` (streamed to the console by default;
  `--no-tail` writes the file only) and a JSON provenance manifest beside the output.
- `.gitignore` excludes `*.apk` and `*.aab`, but not `Builds/` itself — keep Windows build output out of
  version control if you don't want the `.exe` tracked.
