# AGENTS.md

Unity project. No CI, no lint config, no build script, no README. This file is the only
project documentation.

## Editor and toolchain

- Pinned to **Unity 2022.3.62f3** (`ProjectSettings/ProjectVersion.txt`). Do not open in a
  different editor version — it will rewrite project settings and reimport everything.
- Installed at `C:\UnityEditors\2022.3.62f3\Editor\Unity.exe`. This is **not** the Hub default
  path (`C:\Program Files\Unity\Hub\Editor\...`), so don't guess or "repair" the install path.
- Project is in **2D behavior mode** (`EditorSettings.asset: m_DefaultBehaviorMode: 1`) with the
  `com.unity.feature.2d` set, including `com.unity.2d.pixel-perfect`. Active color space is
  **Linear** (`m_ActiveColorSpace: 1`) — flag it before assuming pixel-art color handling.
- **API Compatibility Level is .NET Standard, not .NET Framework** (`apiCompatibilityLevel: 6`;
  the Bee compile refs confirm `netstandard`). Some BCL APIs are unavailable.
- **Legacy Input Manager only** (`activeInputHandler: 0`). The new Input System package is not
  installed — use `UnityEngine.Input`; `UnityEngine.InputSystem` will not compile.
- Domain reload on Play is **enabled** (`m_EnterPlayModeOptionsEnabled: 0`), so statics and
  `[InitializeOnLoad]` state reset on every play. Don't design around persistent statics.

## Verification

Two gates, in order of preference.

### 1. In-Editor via the MCP bridge (preferred — keeps the Editor open)

`unity_advanced_tool` exposes the `testing` category:

- `unity_testing_list_tests` — `{ mode: "EditMode"|"PlayMode", nameFilter, maxResults }`
- `unity_testing_run_tests` — `{ mode, filter | groupNames | testNames | assemblies }`; returns a job id
- `unity_testing_get_job` — `{ jobId, waitTimeout, includeFailedOnly }`; poll for results

This works with the Editor open, so no lockfile dance. Use it while iterating.

### 2. Batchmode (fallback / CI-shaped)

Use when the Editor must be closed anyway, or to prove the project compiles from a cold start.

```
"C:\UnityEditors\2022.3.62f3\Editor\Unity.exe" -batchmode -projectPath "C:\UnityProjects\Pixel Island" \
  -runTests -testPlatform EditMode -testResults "TestResults\editmode.xml" -logFile "TestResults\editmode.log"
```

- `com.unity.test-framework` is **1.1.33**. Under this version tests cannot live in the
  predefined `Assembly-CSharp` — they need an assembly definition with **Test Assemblies**
  enabled (references to `UnityEngine.TestRunner` / `UnityEditor.TestRunner`,
  `precompiledReferences: ["nunit.framework.dll"]`, `defineConstraints: ["UNITY_INCLUDE_TESTS"]`).
  Switch `-testPlatform` to `PlayMode` for PlayMode tests.
- The Editor must be **closed** first. `Temp/UnityLockfile` is present while an editor instance
  holds the project and a second batchmode launch on the same path will fail. Check with
  `tasklist | findstr Unity.exe`.
- Domain reload is on, so PlayMode tests that cache statics across the run will not survive.
- PlayMode tests run in a **throwaway `Assets/InitTestScene<hash>.unity`**, never in the open
  scene. Do not assume scene objects exist while a PlayMode test runs, and do not assert on them.
- Writing a `.cs` file from outside the Editor does **not** trigger a recompile on its own. Refresh
  and request compilation through the bridge, then wait for `isCompiling: false` before running
  tests, otherwise the stale assembly is what gets tested.
- Prefer `unity_get_compilation_errors` over the console log for compile problems: it survives a
  console clear and log flooding.
- **An unfocused Editor stalls the whole pipeline.** The bridge dispatches on
  `EditorApplication.update`, which Unity throttles when the window is in the background, and so is
  asset import and script compilation. Symptoms: `unity_play_mode` reports `isPlaying: true` but
  `Time.time` stays at `0.02` forever; `unity_testing_run_tests` reports
  `blockedReason: "editor_unfocused"`; and a file edited on disk stays "unchanged" to
  `AssetDatabase.Refresh` for minutes. A `.cs` edit may sit uncompiled until something forces the
  pipeline (a test run, or a manual `ImportAsset`). **Watch the timestamp of the assembly in
  `Library/ScriptAssemblies`, not `isCompiling`**: the bridge's `isCompiling` is cached and lies.
  Asking the user to focus the Editor window is the real fix.
- Read `Library/ScriptAssemblies/*.dll` mtimes to confirm a recompile actually happened before
  trusting a test run. Bee writes artifacts elsewhere and copies them late.

## Unity 2022.3 / .NET Standard traps

- API Compatibility Level is .NET Standard 2.0, so some newer BCL conveniences are missing —
  notably the `Physics2D.OverlapBoxAll(..., List<Collider2D>)` overload. Use the
  `Collider2D[]`-returning overload or the `NonAlloc` variants.
- `Physics2D.BoxCastNonAlloc` is **overloaded ambiguously**: there is one overload without a
  `direction` and one with it, and a 6-argument call can bind to the wrong one
  (`CS1503: cannot convert RaycastHit2D[] to Vector2`). Always pass `Vector2.down` explicitly.
- `RaycastHit2D.distance` from a `BoxCast` is measured from the **edge of the cast box**, not
  from its origin. To place a body exactly on a surface, use `RaycastHit2D.point`, otherwise it
  lands half a box above.
- A `BoxCollider2D` carries a hidden `m_SpriteTilingProperty` copy of its `SpriteRenderer`. When
  the renderer's `size` changes in `SpriteDrawMode.Tiled`, Unity re-aligns the collider and leaves
  `offset` at half the sprite's original size. Set `offset` **after** sizing the renderer, or the
  collider silently drifts half a tile away from the art.
- `SpriteDrawMode.Tiled` draws **centred** on the transform, so a tile covering the rect
  `(x, y) .. (x + w, y + h)` goes at position `(x + w/2, y + h/2)` with `offset = Vector2.zero`.
- Dynamic `Rigidbody2D` + a sensor that zeroes vertical velocity will strand the body in mid-air
  wherever the sensor first fires. Snap the body to the contact point instead of freezing it.
- **`Awake` does not run when a scene is opened in EditMode**, so a `MonoBehaviour` that caches a
  `GetComponent` result in `Awake` exposes `null` to editor tooling and to EditMode tests. Resolve
  it lazily in the property instead.
- Anything a component computes from the **screen aspect** has to be recomputed at runtime, not
  just when the scene is authored: the Game view window can be a different shape at play time than
  it was when the scene was built. A sprite quad sized once at build time will show its tile edge.
- `TextureImporter.DoesSourceTextureHaveAlpha()` is about the **source file**, and with
  `isReadable = false` you cannot read the imported pixels from a test to find out. Assert the
  importer settings, and remember a fully opaque layer should have *no* alpha channel.

## Code layout

- Gameplay code goes in `Assets/Scripts/<Feature>/` with one asmdef per feature, not loose files
  in `Assets/`. Keep `Editor/`-only code in an asmdef with `includePlatforms: ["Editor"]`.
- State pattern base classes (`PlayerState`, and the enemy equivalent to come) are plain C# objects,
  not `MonoBehaviour`s: they hold a reference to their owner and decide their own transitions, which
  is what makes them testable without a scene.
- **A PlayMode test must not load a game scene.** `SceneManager.LoadScene(..., Single)` replaces the
  runner's throwaway scene and it is never unloaded, so its ground and its player then collide with
  every later test in the run and the player tests fail with "the explorer should walk". Build the
  rig in memory (as `PlayerMovementPlayModeTests` does) and validate the *saved scene* from an
  EditMode test with `EditorSceneManager.GetSceneManagerSetup()` /
  `RestoreSceneManagerSetup()` so the user's open scene survives.
- Sprite pivot is **bottom-center** everywhere and the player's feet are at `y = 0` of the
  transform. Collider offsets, sensor boxes and spawn heights all assume that.
- Write relative-paths-free `using` style consistent with Unity defaults; no formatter or
  analyzer is configured, so match surrounding code rather than introducing a new style.

## Version control gotchas

- `VersionControlSettings.asset` uses **Visible Meta Files** and Editor serialization is
  **Force Text**. Commit every `.meta` file next to its asset; a missing `.meta` breaks every
  reference by GUID.
- `.gitattributes` assigns `merge=unityyamlmerge` to Unity YAML. Scene/prefab/asset merges go
  through the Unity merge driver — don't hand-resolve YAML conflicts as plain text.
- `.gitignore` excludes `*.sln`, `*.csproj`, `.vs/`, `/[Ll]ibrary/`, `/[Tt]emp/`, `/[Ll]ogs/`,
  `/[Uu]ser[Ss]ettings/`. The `Pixel Island.sln` on disk is therefore **untracked** and is
  regenerated by the IDE — do not add it. `.vsconfig` *is* tracked (pins the VS
  ManagedGame workload).

## Unity MCP bridge (primary way to drive the Editor)

`Packages/manifest.json` installs `com.anklebreaker.unity-mcp` from
`https://github.com/AnkleBreaker-Studio/unity-mcp-plugin.git#main`. It exposes the Editor over a
loopback HTTP bridge so an agent can author scenes, edit scripts, run tests and read the console.

- The manifest tracks the **floating `#main` ref** while `Packages/packages-lock.json` pins
  hash `365af34d3b...` (v2.40.1). A re-resolve can pull a different commit and dirty the lockfile —
  check the lock diff before committing, and don't "fix" it to a tag unless asked.
- Bridge state: **Window → AB Unity MCP → Dashboard** (also under `Tools → AnkleBreaker`).
  Bridge listens on **port 7890**; when manual port is off it auto-selects within **7890-7899**
  per editor instance. Discover the live port instead of assuming 7890 survives a restart.
- Commands return a queue ticket and execute on the Editor **main thread** via
  `EditorApplication.update`. Reads are batched (up to 5), writes are one at a time.
- Menu/status/category toggles live in **EditorPrefs**, not in the repo, so bridge config is
  per-machine and never version controlled.
- **Wired up and working.** `opencode.json` (repo root, tracked) declares the `unity` MCP server
  as `type: local`, launching `node C:/Users/cruzr/unity-mcp-server/src/index.js`. The companion
  server therefore lives **outside the repo, at an absolute path** — it is not reproducible from
  a clone and it will not run on another machine without that directory.
- Discover instances with `unity_list_instances` rather than assuming a port. Multi-instance is
  supported: when more than one Editor is up, ask the user which to target and call
  `unity_select_instance`, then pass `port` on every subsequent `unity_*` call.
- The bridge binds to loopback and its code-execution tools run with the full authority of the
  Editor process. Only use it from a trusted local client.