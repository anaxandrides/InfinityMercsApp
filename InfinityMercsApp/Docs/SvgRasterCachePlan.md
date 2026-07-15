# SVG Raster Cache + List Virtualization — Implementation Plan

## Goal

Remove the two biggest Android rendering costs without changing the SVG download/asset
pipeline:

1. **Eliminate the per-item live `SKCanvasView` surfaces** used to draw faction/unit logos.
   Replace them with a native MAUI `Image` bound to a **rasterized PNG** that is produced
   **once per (icon, pixel size)** and cached on disk + in memory.
2. **Virtualize the long lists** that currently use `BindableLayout` (no view recycling) by
   converting them to `CollectionView`.

SVG remains the network transport and on-disk cache format. Nothing about downloading or
storing SVGs changes. We only stop *rendering* them with a live Skia surface per widget.

## Non-goals

- Do **not** repackage SVGs or change the CDN/download flow in
  [`FactionLogoCacheService`](../Services/FactionLogoCacheService.cs) beyond adding a raster
  hook + invalidation.
- Do **not** touch procedural/decorative Skia drawing (stat cards, weapon range bars, etc.).
  Those are legitimate custom drawing, not icon blitting. See "Classification rule".
- Do **not** attempt an Avalonia/Kotlin migration. This plan is the in-MAUI Tier‑1 work.

## Background (why)

- 93+ `SKCanvasView` instances live in XAML; most are inside item templates, so the runtime
  count scales with visible items. On Android each `SKCanvasView` is a separately composited
  software surface, and each widget re-parses its SVG (`SKSvg.Load`) on bind/paint — see
  [`FactionListItemView.LoadSvgFromCacheAsync`](../Views/Controls/FactionListItemView.xaml.cs).
- 96 `BindableLayout` usages realize **every** item (and every item's canvas) up front; only
  32 `CollectionView` usages virtualize.
- A native `Image` bound to a cached PNG composites cheaply and lets MAUI's platform image
  loader handle its own decode/memory cache.

## Design overview

```
CDN ──download──▶ svg-cache/{id}.svg  (UNCHANGED)
                        │
                        ▼  (rasterize once per size, off UI thread)
                 svg-cache/raster/**/{stem}@{W}x{H}.png
                        │
                        ▼
        ISvgRasterCache.GetRasterPathAsync(svgPath, wLogical, hLogical)
                        │  returns PNG file path
                        ▼
        CachedSvgImage (ContentView wrapping a native Image)
                        │  Image.Source = FileImageSource(pngPath)
                        ▼
        CollectionView item template (virtualized)
```

Key invariants:

- One PNG per `(svg file, target pixel size)`. Pixel size = `logicalSize × display density`.
- Concurrent requests for the same key must **not** rasterize twice (dedupe by key).
- A PNG is invalid the moment its source SVG is rewritten; the writer deletes stale PNGs.

---

## Phase 0 — Shared logo-path resolver (prep)

The candidate-resolution logic (cached path first, packaged fallback, per-item-type id
math) currently lives privately in
[`FactionListItemView`](../Views/Controls/FactionListItemView.xaml.cs) as
`BuildCachedCandidates` / `BuildPackagedCandidates` / `OpenBestLogoStreamAsync`. The new
control and any other call site need the same resolution.

**Steps**
1. Create `Services/Logos/LogoPathResolver.cs` (static class).
2. Move `BuildCachedCandidates(IViewerListItem)` and `BuildPackagedCandidates(IViewerListItem)`
   into it as `public static IEnumerable<string?>`. Keep the exact ordering and id math
   (`ICompanyMercsEntry`, `ArmyUnitSelectionItem`, `ArmyFactionSelectionItem`). `IViewerListItem`
   lives in [`ViewModels/ViewerItems.cs`](../ViewModels/ViewerItems.cs).
3. Add `public static async Task<string?> ResolveBestSvgPathAsync(IViewerListItem item)` that
   returns the first **cached** path that `File.Exists`, else the first packaged path that
   `AppPackageFileExistsAsync` returns true for. Return the *path/logical name*, not a stream
   (the rasterizer opens it).
4. Update `FactionListItemView` to call the shared resolver (it will be deleted in Phase 4,
   but keeping it green in between avoids a broken build).

**Acceptance:** solution builds; `FactionListItemView` still renders logos exactly as before.

---

## Phase 1 — `SvgRasterCache` service

Create the core cache. New files under `Services/Logos/`.

**API** (`ISvgRasterCache.cs`):
```csharp
public interface ISvgRasterCache
{
    /// Returns a PNG file path rendered from svgPath at the given logical size,
    /// or null if the SVG is missing/unparseable. Safe to call from the UI thread;
    /// rasterization runs off-thread.
    Task<string?> GetRasterPathAsync(
        string svgPath, double logicalWidth, double logicalHeight,
        CancellationToken ct = default);

    /// Delete all rasters derived from svgPath. Call whenever the SVG is (re)written.
    void Invalidate(string svgPath);
}
```

**Implementation** (`SvgRasterCache.cs`):
1. Raster root = `Path.Combine(FileSystem.Current.AppDataDirectory, "svg-cache", "raster")`.
   Mirror the source subfolder so unit vs faction ids can't collide, e.g. a source of
   `svg-cache/units/202-15.svg` → `svg-cache/raster/units/202-15@{W}x{H}.png`, and
   `svg-cache/202.svg` → `svg-cache/raster/202@{W}x{H}.png`. For **packaged** sources
   (logical names like `SVGCache/factions/1199.svg`) key by a sanitized stem under
   `raster/packaged/`.
2. Compute pixel size: `density = DeviceDisplay.Current.MainDisplayInfo.Density`; guard
   `<= 0` → `1`. `W = (int)Math.Ceiling(logicalWidth * density)` (same for `H`). Clamp to a
   sane max (e.g. 1024) to avoid pathological allocations.
3. If the PNG exists and is newer than the source SVG's last-write time, return it. (Cheap
   freshness check in addition to explicit `Invalidate`; skip the mtime compare for packaged
   sources since those are immutable.)
4. **Dedupe concurrent builds**: keep a `ConcurrentDictionary<string, Task<string?>>` keyed by
   the PNG path. First caller inserts the build task; others await the same task. Remove the
   entry in a `finally`.
5. **Rasterize off-thread** (`Task.Run`):
   - Open the source: `File.OpenRead` for cached paths, `OpenAppPackageFileAsync` for packaged
     logical names (detect by `File.Exists` first, else treat as packaged).
   - `var svg = new SKSvg(); var pic = svg.Load(stream);` Guard null / non-positive `CullRect`.
   - Fit-scale exactly like the existing paint code
     ([`FactionListItemView.OnLogoCanvasVerticalPaintSurface`](../Views/Controls/FactionListItemView.xaml.cs)):
     `scale = min(W/bounds.W, H/bounds.H)`, centered translate.
   - Draw into `new SKBitmap(W, H)` via `SKCanvas`, `Clear(Transparent)`, translate, scale,
     `DrawPicture(pic)`.
   - Encode `SKImage.FromBitmap(bmp).Encode(SKEncodedImageFormat.Png, 100)`.
   - **Write atomically**: write to `{png}.tmp` then `File.Move(tmp, png, overwrite: true)` so a
     half-written PNG is never observed by a reader.
6. On any exception, log via `Console.Error` (match existing style) and return `null` so the
   UI can show a blank/placeholder.
7. `Invalidate(svgPath)`: derive the raster subfolder+stem and delete `"{stem}@*.png"`
   (enumerate the directory; ignore missing).

**Optional in-memory tier (tuning, not required for v1):** MAUI's platform `FileImageSource`
already memory-caches decoded bitmaps by path, so a managed LRU is usually unnecessary. If
profiling shows repeated decode churn, add a bounded `LinkedList`+`Dictionary` LRU of the
last N `(key → pngPath)` lookups. Do **not** cache `SKBitmap`s in managed memory.

**DI registration:** register as a singleton in
[`ServiceRegistryExtensions.cs`](../ServiceRegistryExtensions.cs) inside `AddAppServices`
(where `FactionLogoCacheService` is registered): `services.AddSingleton<ISvgRasterCache, SvgRasterCache>();`

**Tests** (in [`tests/InfinityMercsApp.UnitTests`](../../tests/InfinityMercsApp.UnitTests)):
- Rasterizing a known-good SVG produces a non-empty PNG of the requested pixel size.
- Second call with same size returns the same path without re-encoding (assert mtime unchanged).
- `Invalidate` deletes the PNG; next call regenerates it.
- Missing/garbage SVG returns `null` and does not throw.

**Acceptance:** tests pass; no UI wired yet.

---

## Phase 2 — Eager rasterization + invalidation in `FactionLogoCacheService`

Pay the rasterization cost at cache-build time (off the UI thread, once), and guarantee PNGs
never go stale.

**Steps** (edit [`FactionLogoCacheService.cs`](../Services/FactionLogoCacheService.cs)):
1. Inject `ISvgRasterCache` into the constructor (add the ctor param; DI already resolves it).
2. Define the standard tile sizes as constants (logical), e.g.
   `FactionTileSize = 96`, `UnitTileSize = 64` — confirm against the actual XAML render sizes
   in Phase 4 and adjust. Rasterize the sizes each icon is actually shown at.
3. After a successful **download or packaged-copy** in `EnsureFactionLogoAvailableAsync` /
   `EnsureUnitLogoAvailableAsync` (the `Downloaded` branches), call
   `Invalidate(localPath)` then `await GetRasterPathAsync(localPath, size, size)` for each
   standard size, so the PNG is warm before the list ever asks for it. Wrap in try/catch —
   a raster failure must never fail the SVG cache result.
4. In the `forceDownload` path (TagCompany faction `2003` / unit `1`,
   [line ~241](../Services/FactionLogoCacheService.cs)), always `Invalidate` before re-rastering
   so the forced re-download's PNG is fresh.
5. `Reused` branches (SVG already on disk from a prior run) intentionally do **not** eager-
   rasterize — Phase 3's control lazily rasters on first display, which covers existing
   installs without a migration pass.

**Acceptance:** after a fresh import, `svg-cache/raster/**` contains PNGs for downloaded
factions/units; re-downloading an icon regenerates its PNG.

---

## Phase 3 — `CachedSvgImage` display control

A drop-in replacement for the `SKCanvasView`+PaintSurface pattern in item templates.

**Steps** (new `Views/Controls/CachedSvgImage.xaml` + `.xaml.cs`):
1. XAML root is a single `Image` (`Aspect="AspectFit"`), no `SKCanvasView`.
2. Bindable properties:
   - `Item` (`IViewerListItem`) — resolved via `LogoPathResolver` from Phase 0; **or**
   - `SvgPath` (`string`) for call sites that already have a concrete path.
   - `LogicalWidth`, `LogicalHeight` (`double`) — the render size; default to the control's
     `WidthRequest`/`HeightRequest` if unset.
3. On `Item`/`SvgPath` change (and on `OnBindingContextChanged` for recycled cells):
   - Increment an `int _version` guard (identical pattern to the existing
     `_logoLoadVersion` in `FactionListItemView`) so a recycled cell's late result is dropped.
   - Resolve the SVG path (async), call
     `ISvgRasterCache.GetRasterPathAsync(...)`, and if the version still matches set
     `InnerImage.Source = ImageSource.FromFile(pngPath)` on the UI thread. Null → clear source.
4. Resolve `ISvgRasterCache` from the DI container. Prefer constructor injection where the
   control is created in code; for XAML-instantiated controls use the app's service provider
   accessor (follow whatever pattern the codebase already uses to reach services from a
   `ContentView` — check how existing controls obtain services; do **not** invent a new
   service-locator if one exists).
5. Handle unloaded/disposed: no `SKPicture` to dispose anymore (a win); just null the source.

**Acceptance:** a scratch page showing a `CachedSvgImage` for a known faction renders the
logo identically to the old canvas version.

---

## Phase 4 — Migrate per-item logo controls

Replace the logo `SKCanvasView` with `CachedSvgImage` **only** in controls that draw a
faction/unit logo loaded from a file. Use the classification rule below per site.

### Classification rule (apply to each PaintSurface site)

- **MIGRATE** if the PaintSurface handler draws a single `SKPicture`/`SKBitmap` loaded from an
  SVG **file** (svg-cache or packaged logo) — i.e. it's icon blitting.
- **LEAVE ALONE** if the handler draws procedurally (shapes, paths, text, bars, gauges,
  gradients computed at runtime) — that is real custom drawing, cheaper as-is than a bitmap and
  not a logo.

### Known logo/icon item controls → migrate (verify each against the rule)

- [`FactionListItemView`](../Views/Controls/FactionListItemView.xaml.cs) — the reference case;
  do this one first and benchmark before continuing.
- [`CompanyViewerUnitTileView`](../Views/Controls/CompanyViewerUnitTileView.xaml.cs)
- [`SelectedFactionButtonView`](../Views/Controls/SelectedFactionButtonView.xaml.cs)
- [`UnitSelectionListItemView`](../Views/Controls/UnitSelectionListItemView.xaml.cs)
- [`MercsCompanyEntryCardView`](../Views/Controls/MercsCompanyEntryCardView.xaml.cs)
- [`ViewerListRowView`](../Views/Controls/ViewerListRowView.xaml.cs)
- [`FactionSlotSelectorView`](../Views/Controls/FactionSlotSelectorView.xaml.cs) — see
  [`CompanySelectionVisualIconWorkflow.LoadSlotIconAsync`](../Views/Common/CompanySelectionVisualIconWorkflow.cs)
- [`AddFactionButtonView`](../Views/Controls/AddFactionButtonView.xaml.cs)
- [`SeasonStartPointsView`](../Views/Controls/SeasonStartPointsView.xaml.cs) — verify (may be mixed)

### Likely LEAVE ALONE (verify — procedural drawing, not logos)

- `WeaponRangeBandBarView` (range bars), any stat-card / skeuomorphic gauge, decorative
  dividers, and the multi-icon header canvas in
  [`UnitDisplayConfigurationsView`](../Views/Controls/UnitDisplayConfigurationsView.xaml.cs)
  (draws several small bundled CB icons in one surface — low count, not per-item; migrating is
  optional and lower priority).

**Per-control steps**
1. In the XAML, replace the `skia:SKCanvasView` (+ its `SizeChanged`/paint wiring) with
   `<controls:CachedSvgImage Item="{Binding .}" LogicalWidth="…" LogicalHeight="…"/>`
   (or `SvgPath=` where the control has an explicit path).
2. Delete the now-dead code-behind: the `SKPicture` field, `LoadSvgFromCacheAsync`,
   `On…PaintSurface`, and disposal. Remove the `SkiaSharp`/`Svg.Skia` usings if unused.
3. Feed the size constants used in Phase 2 so eager rasters match the requested size (avoid
   generating a second PNG size on first display).

**Acceptance:** each migrated screen renders logos identically; no `SKCanvasView` remains in
the migrated controls (`grep skia:SKCanvasView` count drops by the number migrated).

---

## Phase 5 — Virtualize long lists (`BindableLayout` → `CollectionView`)

Only convert lists that can grow long (units, factions, company entries, encyclopedia,
marketplace rows). Short fixed lists may stay as `BindableLayout`.

**Candidate files** (confirm each is a genuinely long, scrollable list before converting):
- [`UnitEncyclopediaPage.xaml`](../Views/UnitEncyclopedia/UnitEncyclopediaPage.xaml)
- [`MarketplacesPage.xaml`](../Views/MarketplacesPage/MarketplacesPage.xaml)
- [`CompanyUnitSelectionListPanelView.xaml`](../Views/Controls/CompanyUnitSelectionListPanelView.xaml)
- [`FactionSelectorOverlayView.xaml`](../Views/Controls/FactionSelectorOverlayView.xaml)
- the faction/unit lists in the company-selection pages
  ([`StandardCompanySelectionPage.xaml`](../Views/StandardCompany/StandardCompanySelectionPage.xaml),
  [`AirborneCompanySelectionPage.xaml`](../Views/AirborneCompany/AirborneCompanySelectionPage.xaml),
  [`InspiringCompanySelectionPage.xaml`](../Views/InspiringCompany/InspiringCompanySelectionPage.xaml),
  [`CohesiveCompanySelectionPage.xaml`](../Views/CohesiveCompany/CohesiveCompanySelectionPage.xaml))

**Per-list steps**
1. Replace the `BindableLayout`-hosting `StackLayout`/`FlexLayout` with a `CollectionView`
   (`ItemsSource` = same collection; move the item template into `CollectionView.ItemTemplate`).
   Use `ItemsLayout` (Grid vs List) matching the current arrangement.
2. **Critical:** a `CollectionView` must not be nested inside a `ScrollView` on the same axis —
   it breaks virtualization. Remove the wrapping `ScrollView` (14 pages nest `BindableLayout`
   in `ScrollView` today) and let `CollectionView` scroll itself. Keep non-scrolling headers
   in a `Grid` row above the `CollectionView`.
3. Ensure item templates use `CachedSvgImage` (Phase 4) so recycled cells rebind cleanly (the
   `_version` guard handles fast re-bind during fling scroll).
4. Verify selection/tap commands still fire (`CollectionView.SelectionChanged` or the
   template's `TapGestureRecognizer` — keep whatever the item currently uses).

**Acceptance:** long lists scroll without materializing every row (verify realized-cell count
stays roughly proportional to viewport, not collection size); tap/selection behavior unchanged.

---

## Phase 6 — Verification & benchmarking

1. **Correctness:** run existing unit tests + the app on Android. Visit every migrated screen;
   logos and lists must look identical.
2. **Perf (real low-end Android device or emulator with low RAM):**
   - Cold-start to first interactive on a logo-heavy screen (company viewer / faction list).
   - Scroll a long list; capture frame timing (Android GPU profiler / `adb shell dumpsys
     gfxinfo com.companyname.infinitymercsapp` before/after).
   - Peak managed + native memory on the heaviest screen.
   Record before/after numbers in this doc or the PR description.
3. **Regression sweep:** confirm `grep -r "skia:SKCanvasView" Views` count dropped only for
   migrated logo controls and that all remaining ones are intentional procedural drawing.

**Success criteria:** measurable drop in scroll jank / dropped frames and peak memory on the
logo-heavy list screens, with no visual regressions.

---

## Rollout / risk notes

- **Ship phases as separate PRs** in order (0→5). Phases 1–2 are safe to merge before any UI
  changes (they only add files + a warm-cache side effect). Phase 4 is per-control and
  independently revertable.
- **Existing installs**: no forced re-download. SVGs already on disk get lazily rasterized on
  first display (Phase 3), and eagerly on the next import refresh (Phase 2 `Reused` icons are
  covered by lazy path).
- **Disk growth**: rasters add PNGs alongside SVGs. Bounded by icon count × sizes (small). If
  needed, add a cleanup that deletes rasters whose source SVG no longer exists.
- **DPI changes** (foldables, external displays): rasters are keyed by pixel size derived from
  density; a density change simply produces a new key. No stale-scale bug.
- **The one case to keep as a live surface**: any screen with continuous pinch-zoom on an icon
  needs vector rendering at arbitrary scale — keep `SKCanvasView` there specifically. (None
  identified currently; confirm during Phase 4.)

## File checklist

- [x] `Services/Logos/LogoPathResolver.cs` (Phase 0)
- [x] `Services/Logos/ISvgRasterCache.cs` + `SvgRasterCache.cs` (+ `SvgRasterCache.Platform.cs`) (Phase 1)
- [x] `ServiceRegistryExtensions.cs` — register `ISvgRasterCache` (Phase 1)
- [x] `Services/FactionLogoCacheService.cs` — inject cache, eager raster, invalidate (Phase 2)
- [x] `Views/Controls/CachedSvgImage.xaml(.cs)` (Phase 3)
- [x] Migrated logo controls (Phase 4 list) — see notes
- [x] Virtualized list pages (Phase 5 list) — see notes
- [x] Unit tests for `SvgRasterCache` (Phase 1)
- [ ] Before/after perf numbers recorded (Phase 6) — **requires a real Android device; not run in this environment**

## Implementation notes (execution)

**Phase 1 testability.** `SvgRasterCache` is split into a MAUI-free core (`SvgRasterCache.cs`,
constructor takes cache root + density provider + packaged-opener) and a DI/platform partial
(`SvgRasterCache.Platform.cs`, parameterless ctor wiring `FileSystem`/`DeviceDisplay`). The unit
test project links only the core + interface and references `SkiaSharp`/`Svg.Skia`, so the
rasterizer is exercised against a temp directory with no MAUI host. 6 tests added (all green).

**Phase 4 classification results.**
- Migrated (per-item, file-loaded logo → `CachedSvgImage`, no `SKCanvasView` left):
  `FactionListItemView` (48), `UnitSelectionListItemView` (32), `ViewerListRowView` (28),
  `CompanyViewerUnitTileView` (46 logo + two 16px badges; TagCompany fallback preserved by
  resolving the best path in code-behind and pushing it via `SvgPath`).
- `MercsCompanyEntryCardView`: its main logo is delegated to `UnitSelectionListItemView`
  (already migrated); its remaining canvases are parent-drawn CB status icons → left per rule.
- Left as live surfaces (LEAVE ALONE per classification rule): `AddFactionButtonView`
  (procedural "+"), `SelectedFactionButtonView` / `FactionSlotSelectorView` / `SeasonStartPointsView`
  (single, non-per-item controls; negligible scaling benefit), `WeaponRangeBandBarView` /
  `UnitDisplayConfigurationsView` (procedural / multi-icon, low count).

**Phase 5 virtualization results.**
- Converted to `CollectionView`: `UnitEncyclopediaPage` Factions + Units lists (removed the
  obsolete manual pan-to-scroll workaround — native cells scroll natively now),
  `CompanyUnitSelectionListPanelView`, `FactionSelectorOverlayView`.
- Already virtualized before this work: `FactionSelectionStripView` (the codebase's proven
  `RelativeSource AncestorType` command-binding pattern — reused for every conversion here since
  `x:Reference` does not resolve inside a `CollectionView.ItemTemplate` namescope).
- Left as `BindableLayout` (short/nested config lists the plan permits): company-page
  `TeamEntries`/`AllowedProfiles` fireteam config, UnitEncyclopedia `Fireteams`/`UnitLimits`,
  Marketplace `CartLines`.
- **`MarketplacesPage` deliberately not converted:** its Items/TroopTypes/Augments sections are
  all visible simultaneously inside one shared `SectionsScroll` (with adaptive multi-column
  reflow), so a per-section `CollectionView` swap would nest a scroller (breaks virtualization)
  and a correct conversion needs a single grouped `CollectionView` + UX rewrite. Those rows carry
  no `SKCanvasView`, so the Phase 4 driver is absent and the risk/benefit did not justify a blind
  rewrite.

**Verification done here:** Windows (`net9.0-windows`) build clean, Android (`net9.0-android`)
build clean, full unit suite green (112 tests). **Not done here:** on-device visual pass and the
Phase 6 Android frame-timing / memory benchmarks — these need a physical device/emulator.
