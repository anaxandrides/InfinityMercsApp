# Task 1 — Make the company-selection team-entries build fast (algorithmic fix)

> Handoff briefing for a fresh session. Everything you need to do this task cold is here.
> **Do not trust the code summaries below as gospel — re-read the actual methods. The
> characterization tests you write are the source of truth for behavior.**

## TL;DR

When you tap a faction on a company-selection page (Standard / Airborne / Inspiring / Cohesive),
the app builds the fireteam "team entries". On a real device (Samsung Galaxy Tab A SM-T510) this
step measured **~8 seconds** for one faction (~42 units). It is the single biggest remaining
faction-tap stall. It is an **algorithmic** problem, not a threading one:
`CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry` is called once per (team × unit-limit) and
each call re-materializes the whole unit list and does up to ~6 linear `FirstOrDefault` scans over
all units → **O(teams × limits × units × ~6)**.

**Goal:** turn per-limit resolution from O(units) into O(1) by precomputing lookup structures once
per build, **without changing the matching result for any input**. Target: teams phase **< 200 ms**.

## Background (how we got here — 1 paragraph)

This is the tail of a larger MAUI performance effort (see `SvgRasterCachePlan.md`). The
company-selection faction tap was originally ~9.7 s of UI-thread jank. We already off-loaded the
heavy pieces to worker threads in the shared base (`CompanySelectionPageBase`): the merged
units+teams build, the per-unit visibility compute + lookup-context build, the filter-popup-options
query, and the team-entries build itself. Instrumentation then showed the residual is dominated by
the team-matching **compute** (~8 s of CPU), which off-threading only hides. This task fixes the
compute. **The off-threading is already in place — do not redo it.**

## Where the work is

Repo root: `c:\Users\user\source\repos\InfinityMercsApp\InfinityMercsApp\` (note the app project is
the inner `InfinityMercsApp\`). Git branch: `UI_Refactor` (has uncommitted perf work — that's
expected).

Primary file to optimize:
- `InfinityMercsApp/Services/CompanySelection/Teams/CompanyTeamMatchingWorkflow.cs`
  - `internal static TUnit? ResolveUnitForTeamEntry<TUnit>(string? allowedProfileName, string? allowedProfileSlug, IEnumerable<TUnit> sourceUnits, Func<TUnit,string?> readName, Func<TUnit,string?> readSlug)` — **the hot method** (~line 90). Multi-tier matcher.
  - `internal static ArmyUnitSelectionItem? ResolveUnitForTeamEntry(name, slug, sourceUnits)` — thin overload (~line 205).
  - Helpers it uses: `IsReinforcementTeamEntry`, `IsReinforcementUnit`, `NormalizeSlugForLookup`, `NormalizeTeamUnitName`, and the name-matching tiers **below line ~135 that this briefing did NOT read** — read them all.

Callers that invoke resolve O(teams × limits) times:
- `InfinityMercsApp/Services/CompanySelection/Teams/CompanyTeamProfilesWorkflow.cs`
  - `FilterCharacterUnitLimits<TUnit>` (~line 5 and the `ArmyUnitSelectionItem` overload ~line 28) — calls resolve per unit-limit.
  - `BuildAllowedTeamProfiles<TUnit,TTeamUnitLimitItem>` (~line 61) — builds per-limit items.
  - `BuildTeamUnitLimitItem<TUnit>` (~line 126) — calls resolve then reads matched unit's fields.
- `InfinityMercsApp/Services/CompanySelection/Core/CompanySelectionUnitLoadWorkflow.cs`
  - `BuildTeamEntriesFromMerged(...)` (~line 171) — the outer `foreach team` loop. `sourceUnits` is
    `merged.UnitsByKey.Values` and is **the same fixed set for every resolve in one build.**

Already-done off-threading (context, don't touch unless the optimization makes it pointless):
- `InfinityMercsApp/Views/Common/Core/CompanySelectionPageBase.UnitLoad.cs` →
  `BuildTeamEntriesFromMergedAsync` wraps the workflow in `Task.Run`.

## The current algorithm (VERIFY by reading — do not trust this)

`ResolveUnitForTeamEntry` (what I saw of it, lines ~90–135; the rest is unread):
1. `reinforcementOnly = IsReinforcementTeamEntry(allowedProfileName)`.
2. `preferredSourceUnits = reinforcementOnly ? units.Where(IsReinforcementUnit).ToList() : units.ToList()`.
   `fallbackSourceUnits = reinforcementOnly ? units.Where(!IsReinforcementUnit).ToList() : []`.
   **← both `.ToList()` run on EVERY call — a big part of the cost.**
3. Tiered `FirstOrDefault` over `preferredSourceUnits` (then presumably `fallbackSourceUnits`),
   returning the first match found, in this order (slug tiers confirmed; name tiers unread):
   - slug exact, `Ordinal`, trimmed
   - slug exact, `OrdinalIgnoreCase`, trimmed
   - normalized slug (`NormalizeSlugForLookup`), `Ordinal`
   - …then name-based tiers (exact / normalized via `NormalizeTeamUnitName`, etc.) — **read them.**

**Semantics that MUST be preserved:**
- Tier order (a higher tier wins even if a lower tier would also match).
- "First match wins" within a tier = first in the original enumeration order of `sourceUnits`.
- The reinforcement preferred/fallback partition and the order preferred-before-fallback.
- Exact string comparers per tier (`Ordinal` vs `OrdinalIgnoreCase`) and trimming/normalization.
- Null/empty handling (empty slug skips slug tiers; empty name; units with null slug/name).

## The fix

Build a precomputed resolver **once** from the fixed `sourceUnits`, then resolve in O(1) per limit.

Recommended shape:
1. Add a small immutable `TeamUnitResolver<TUnit>` (or a set of dictionaries) built once from
   `sourceUnits` that captures every tier as a keyed lookup with **first-occurrence-wins** (iterate
   `sourceUnits` in order; `TryAdd` so the first insertion wins, mirroring `FirstOrDefault`).
   - You need the reinforcement partition, so build the lookups over the correct subset. Simplest
     correct option: build **two** resolvers — one for the "reinforcement-preferred" case and one for
     the normal case — or store an `IsReinforcement` flag per unit and keep separate dictionaries for
     each partition + a "fallback" set. Match the current preferred-then-fallback probe order exactly.
   - Keys: raw-slug (Ordinal), raw-slug (OrdinalIgnoreCase → use a case-insensitive dictionary),
     normalized-slug, normalized-name, etc. — one dictionary per tier, all first-wins.
2. Change `ResolveUnitForTeamEntry` to accept/keep the precomputed resolver, or overload it so the
   old `IEnumerable`-scanning version delegates to a resolver built from the sequence (so existing
   call sites keep working) while `BuildTeamEntriesFromMerged` builds the resolver **once** and passes
   it down through `FilterCharacterUnitLimits` / `BuildAllowedTeamProfiles` / `BuildTeamUnitLimitItem`.
3. Keep the public/behavioral contract identical.

Lower-risk incremental option if the full resolver feels too big in one go: at minimum, materialize
`sourceUnits` once and hoist the reinforcement partition out of the per-call path, and **memoize**
`ResolveUnitForTeamEntry` by `(name, slug)` for the duration of one build (same inputs → same output,
no semantic change). This alone removes the repeated `.ToList()` + redundant scans. But the O(1)
lookup is the real win — prefer it.

## Correctness strategy — DO THIS FIRST

This code drives what the Teams tab shows; a wrong match is a silent bug. De-risk with
**characterization tests** before changing anything:

1. Add `tests/InfinityMercsApp.UnitTests/CompanyTeamMatchingTests.cs`.
2. The test project (`tests/InfinityMercsApp.UnitTests/InfinityMercsApp.UnitTests.csproj`) is a plain
   `net9.0` xUnit project that **links source files** via `<Compile Include="..\..\InfinityMercsApp\...">`
   (see how `SvgRasterCache.cs` is linked there). Link `CompanyTeamMatchingWorkflow.cs` (and any
   helper files it needs — check its `using`s; the generic method takes `readName`/`readSlug`
   delegates so you can test with a tiny `record TestUnit(string? Name, string? Slug)` and avoid
   pulling in `ArmyUnitSelectionItem`). If it won't isolate cleanly, note why and adjust.
3. Write a table of representative inputs covering **every tier and edge case**: exact slug, case
   differences, normalized-only slug matches, name-only matches, reinforcement vs non-reinforcement
   teams, duplicate keys (assert first-wins), null/empty slug and name, no-match → null.
4. Run against the **current** `ResolveUnitForTeamEntry` and lock in the outputs (these are your
   golden values).
5. Refactor to the precomputed resolver. Tests must stay green — identical results for every input.

If you can't fully characterize a tier from reading the code, add a test that exercises it and use
its current output as the oracle.

## Build / test / measure

Toolchain is installed (`dotnet 9.0.200`, MAUI android + windows workloads; `adb` on PATH).

- Fast compile check (Windows target, ~10 s):
  `dotnet build InfinityMercsApp/InfinityMercsApp.csproj -f net9.0-windows10.0.19041.0 -c Debug -v minimal`
- Unit tests:
  `dotnet test tests/InfinityMercsApp.UnitTests/InfinityMercsApp.UnitTests.csproj -v minimal`
- Deploy + launch on the connected device:
  `dotnet build InfinityMercsApp/InfinityMercsApp.csproj -f net9.0-android -c Debug -t:Run -v minimal`

### On-device measurement (the whole point)

Device: Samsung Galaxy Tab A (SM-T510), `adb devices` → `R52N50QHPBN device`. Low-end, good for perf.

Re-add temporary `[PERF]` instrumentation to time the phases (this is how the ~8 s was found).
In `InfinityMercsApp/Views/StandardCompany/StandardCompanySelectionPage.UnitSelection.cs`, inside
`LoadUnitsForActiveSlotAsync`, wrap phases with:
```csharp
var __sw = System.Diagnostics.Stopwatch.StartNew();
// ... await BuildMergedUnitsAndTeamsAsync(...)
Console.WriteLine($"[PERF] merged={__sw.ElapsedMilliseconds}ms"); __sw.Restart();
// ... PopulateUnitsCollection(...)
Console.WriteLine($"[PERF] populate={__sw.ElapsedMilliseconds}ms"); __sw.Restart();
// ... await BuildTeamEntriesFromMergedAsync(...)
Console.WriteLine($"[PERF] teams={__sw.ElapsedMilliseconds}ms"); __sw.Restart();
// ... await ApplyUnitVisibilityFiltersAsync(...)
Console.WriteLine($"[PERF] visibility={__sw.ElapsedMilliseconds}ms"); __sw.Restart();
// ... after TryAutoSelectFirstVisibleUnitAfterFactionLoad()
Console.WriteLine($"[PERF] autoselect={__sw.ElapsedMilliseconds}ms"); __sw.Restart();
// ... after the filter Task.Run
Console.WriteLine($"[PERF] filter={__sw.ElapsedMilliseconds}ms");
```
**Remove this instrumentation before finishing.** Note: `teams` is currently measured as off-thread
wall-clock (it's inside `Task.Run`); once you make it fast that number is what matters. If you want
pure UI-thread-blocking numbers, also watch the InputDispatcher/Choreographer lines below.

Navigate to the company-selection page and trigger a faction load. **Use landscape** — in portrait
the season-page buttons sit on the bottom nav-gesture strip and `adb input tap` there jumps to the
launcher.
```bash
adb shell settings put system accelerometer_rotation 0; adb shell settings put system user_rotation 1
# wait for app; then (each of these often needs a DOUBLE tap — the first tap on a fresh page is absorbed):
adb shell input tap 1438 626   # START NEW SEASON
adb shell input tap 1438 610   # CREATE A NEW TEAM
adb shell input tap 303 335    # Standard Company
adb shell input tap 662 585    # One Vanilla Faction  -> now on the company selection page
adb logcat -c
adb shell input tap 317 185    # tap Ariadna in the faction strip -> triggers the team build
sleep 6
adb logcat -d | grep -iE "\[PERF\]|spent.*ms.*MotionEvent|Choreographer.*Skipped"
adb exec-out screencap -p > shot.png    # verify units + the 112 profile render correctly
# when done:
adb shell settings put system user_rotation 0; adb shell settings put system accelerometer_rotation 1
```
Read `[PERF]` from logcat lines tagged `DOTNET  : [PERF] ...`. `Console.WriteLine` → logcat.

### Gotchas learned the hard way
- **Double-tap:** the first `adb input tap` on a freshly-rendered page is frequently absorbed; tap
  twice with a short sleep. Screenshot to confirm each navigation step actually advanced.
- **Bottom-edge = home gesture:** taps below ~y=1830 (portrait) hit the system nav; that's why we use
  landscape (buttons land mid-screen at the coords above).
- **Device drops:** it went `offline`/`unauthorized` once mid-session; recover with
  `adb kill-server && adb start-server` (may need you to accept the USB-debug prompt on the tablet).
- **Battery** was ~12% (charging) — fine, but don't dawdle.
- **The InputDispatcher metric is muddy** at this residual level (the unit-list `CollectionView`
  cell-realization layout overlaps the load window). Trust the `[PERF] teams=` number for this task,
  and confirm the on-screen list appears without a multi-second freeze.

## Baseline & success criteria

Baseline (one faction, ~42 units, on SM-T510): `[PERF] teams ≈ 6400–8400 ms`. Other phases already
small: `merged≈330ms, populate≈12ms, visibility≈850ms, filter≈1340ms (off-thread)`.

Done when:
- `[PERF] teams` drops to **< ~200 ms**.
- Characterization tests pass (identical matching for all inputs) + full unit suite green.
- On-device: Standard company faction tap loads the unit list with no multi-second freeze; the
  **Teams tab still shows the same fireteams/allowed-profiles as before** (spot-check a couple
  factions — Ariadna and one with sectorial/reinforcement teams).
- `[PERF]` instrumentation removed; Windows + Android builds clean.

## Out of scope (but related — mention if asked)

**Task 2 (separate):** `TeamEntries` only feeds the (hidden) Teams tab, so the whole team build could
be **deferred** until that tab is first opened, removing it from the faction-tap path entirely
(watch `AreTeamEntriesReady` in the Cohesive page). Lower algorithmic risk, touches tab/ready-state
control flow. If Task 1 makes the build fast enough, Task 2 may be unnecessary.
