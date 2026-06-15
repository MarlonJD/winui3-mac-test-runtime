# EMSI Windows Direct Runtime UI Gap Closure Plan

Date: 2026-06-15
Owner: `tools/winui3-mac-test-runtime`
Downstream validation target: `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`
Status: active

## 2026-06-15 Phase 0/1 Execution Update

Completed in this pass:

- Phase 0 gap matrix saved outside the repo at
  `/private/tmp/emsi_qa/windows/mac-runtime-direct/2026-06-15-phase0-gap-matrix.md`.
- Added fail-first direct runtime tests for app merged dictionaries, theme
  dictionaries, localized Login text, and localized TextBox placeholder export.
- Implemented the minimal direct-host resource/localization path:
  - generated direct hosts now populate `Application.Current.Resources` from
    source project theme dictionaries;
  - page resource lookup falls back to app resources;
  - generated direct hosts apply `Strings/en-us/Resources.resw` values to
    supported `x:Uid` properties after `InitializeComponent`;
  - `TextBox.PlaceholderText` is represented in the facade, tree, and
    accessibility output.
- Direct `login-light` evidence was written to
  `/private/tmp/emsi_qa/windows/mac-runtime-direct/login-light-phase1-r3/`.

Phase 1 result:

- `resource-failures.json`: zero failures.
- Login localized values are present in `tree.json` and `accessibility.json`:
  `Meeting Challenge`, Login subtitle, `Username`, `Password`, and `Sign In`.
- `unsupported-apis.json`: zero APIs.
- `run.json`: still failed because `UsernameBox.Text` has `DataContext is null`.
  This is the expected Phase 2 blocker.
- `visual/visual-run.json`: failed because no Windows reference image was
  provided; runtime image integrity passed and the image is nonblank.

Verification:

- `dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp|AutomationContract"` passed, 18 tests.
- `dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "Resource|Localization|Binding|Interaction|Accessibility|Frame|Navigation|Renderer|Layout|InfoBar|TextBox|PasswordBox"` passed, 78 tests, with existing nullable/MSTest analyzer warnings in `tests/WinUI3.MacRuntime.Tests/MacRuntimeTests.cs`.
- `dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj` passed with 0 warnings and 0 errors.

Next todo:

- Phase 2: add fail-first tests and direct-host bootstrap support for the
  Login page `DataContext` so `UsernameBox.Text` no longer fails on null
  `DataContext`, without backend calls or credentials.

## Goal

Make the Mac runtime useful against the real EMSI Windows app UI, not only
probe or sanitized XAML samples. The target is a phased path from today's
direct `LoginPage.xaml` screenshot to repeatable green direct-app gates for the
login route, shell, common product surfaces, and finally the Admin workbench.

The plan deliberately treats the current screenshot as valuable evidence but
not a green milestone. A production page rendered, direct project ingestion
passed, and login interaction visibility assertions passed. The run still
failed because app-level resources, localization/resource merging, and page
DataContext bootstrapping are incomplete.

## Current Evidence

- Direct scenario: `apps/windows/tests/MeetingChallenge.WinUI.DirectRuntimeScenarios/login-light.json`
- Direct project: `apps/windows/src/MeetingChallenge.Windows/MeetingChallenge.Windows.csproj`
- Latest direct output: `apps/windows/artifacts/winui3-mac/direct-meetingchallenge-windows-login-light-20260615-r3/`
- `project-ingestion.json`: passed against the production app project.
- `interactions.json`: passed 3/3 login visibility assertions.
- `run.json`: failed.
- Missing app/runtime resources observed:
  - `AppBackgroundBrush`
  - `SurfaceBrush`
  - `SurfaceBorderBrush`
  - `PageTitleTextBlockStyle`
  - `PageBodyTextBlockStyle`
  - `SecondaryTextBrush`
- Binding failure observed:
  - `UsernameBox.Text`: `DataContext is null`
- Historical production XAML gap families from
  `docs/compatibility/downstream-production-xaml-gap-summary.json`:
  - `admin-workbench`: 27
  - `home-read-surface`: 19
  - `messages`: 16
  - `channels`: 13
  - `notifications`: 13
  - `settings`: 13
  - `events`: 9
  - `login`: 9
  - `theme-dictionaries`: 2

## Scope

In scope:

- Runtime support needed for direct EMSI Windows app ingestion and rendering.
- Resource dictionary merging, theme dictionaries, and localized resource lookup
  needed by production EMSI XAML.
- Direct page/shell bootstrapping so representative pages receive the
  DataContext or navigation parameter they use on Windows.
- Renderer/compiler support for common EMSI UI constructs seen in Login, Home,
  Events, Channels, Messages, Notifications, Settings, and Admin.
- Scenario and evidence gates that prove direct app behavior without accepting
  `MeetingChallenge.WinUI.MacRuntimeProbe` screenshots as milestone evidence.

Out of scope unless explicitly reopened:

- Replacing the production Windows app architecture.
- Treating the Mac runtime as a full WinUI implementation.
- Backend/API correctness for EMSI product data.
- Branch operations or public GitHub writes.
- Claiming Windows-native parity from Mac runtime screenshots alone.

## Assumptions And Open Questions

- The runtime should continue to prefer direct project ingestion evidence over
  probe evidence for EMSI milestone claims.
- The first green path should be narrow: login light mode, then shell/common
  surfaces, then Admin.
- Some app services can be replaced by deterministic scenario seeds for Mac
  runtime validation. If a page absolutely requires real app service creation,
  that should be a deliberate gate, not an accidental dependency.
- Native Windows screenshots can be used as reference later, but this plan does
  not require public or CI Windows runner access for the first closure loop.

## Work Breakdown

### Phase 0 - Inventory And Gate Matrix

Objective: Turn the current "we know some things are missing" state into a
stable runtime worklist that can be tested in small pieces.

Steps:

1. Re-read the latest direct `run.json`, `resource-failures.json`,
   `binding-failures.json`, `tree.json`, `accessibility.json`, and
   `interactions.json`.
2. Create a gap matrix with one row per EMSI surface:
   - Login
   - Signed-out shell
   - Signed-in shell
   - Home
   - Events
   - Channels
   - Messages
   - Notifications
   - Settings
   - Admin overview
   - Admin workbench interactions
3. For each row, record:
   - required XAML files
   - required resource dictionaries
   - required page DataContext or navigation parameter
   - unsupported control/properties encountered
   - required interactions
   - expected output artifacts
4. Decide the first pass/fail contract for each surface:
   - `project-ingestion.json` status
   - `run.json` status
   - resource failures
   - binding failures
   - visual gate
   - interaction assertions
   - accessibility tree assertions
5. Add or refresh narrow scenario inventory docs only after direct evidence is
   captured. Do not reintroduce probe screenshots as accepted milestone proof.

Done when:

- The current direct login failures are categorized as P0.
- Common surface gaps are separated from Admin-specific gaps.
- Every planned runtime change has a named scenario or test gate.

### Phase 1 - P0 App Resource And Localization Gate

Objective: Load the app-level visual language that production pages expect:
theme dictionaries, merged dictionaries, app brushes, text styles, and localized
strings.

Steps:

1. Add fail-first runtime tests for direct app resource lookup:
   - merged `App.xaml` dictionaries
   - nested `ResourceDictionary.MergedDictionaries`
   - `ThemeDictionaries`
   - `StaticResource` lookup across app/page dictionaries
   - `x:Uid` or `.resw` localized text lookup where used by Login
2. Teach project ingestion or generated host setup to include the app resource
   dictionaries needed by the selected page scenario.
3. Implement deterministic resource resolution order:
   - page resources
   - app resources
   - merged dictionaries
   - theme dictionaries for the selected theme
   - runtime defaults only as a documented fallback
4. Add diagnostics that distinguish:
   - missing resource key
   - unsupported dictionary file
   - theme lookup miss
   - localization lookup miss
5. Re-run direct `login-light` and confirm these keys no longer fail:
   - `AppBackgroundBrush`
   - `SurfaceBrush`
   - `SurfaceBorderBrush`
   - `PageTitleTextBlockStyle`
   - `PageBodyTextBlockStyle`
   - `SecondaryTextBrush`
6. Confirm Login text values are real localized strings or expected fallback
   text, not null/empty placeholders.

Done when:

- Login direct run has no missing app resource failures.
- Login title, subtitle, username label, password label, and submit button are
  represented in `tree.json` and `accessibility.json`.
- Resource diagnostics remain actionable if a future surface adds a new missing
  key.

### Phase 2 - P0 Direct Page Bootstrap And DataContext Gate

Objective: Make direct page scenarios create the same minimal page state that
the production app supplies through navigation and app services.

Steps:

1. Add fail-first tests proving `LoginPage` receives a usable
   `LoginViewModel` or scenario-provided equivalent.
2. Extend scenario schema only as much as needed to seed page state:
   - page route target
   - view model type or named fixture
   - simple property values
   - command availability
   - expected navigation parameter
3. Keep the generated host deterministic and offline:
   - no backend calls
   - no credential use
   - no production mutation
   - explicit fake service objects where needed
4. Wire direct page construction so `OnNavigatedTo`, `DataContext`, bindings,
   and commands are initialized in a Windows-like order.
5. Add binding diagnostics for:
   - null `DataContext`
   - missing property
   - unsupported binding mode
   - unsupported command binding
6. Re-run direct `login-light` and verify `UsernameBox.Text` no longer fails
   because of a null `DataContext`.

Done when:

- Login direct run has no P0 binding failures.
- Username/password/submit controls are visible and bindable.
- The login scenario still does not call real backend services.

### Phase 3 - P1 Common Surface Layout And State Gate

Objective: Support the repeated EMSI read-surface UI patterns before tackling
the Admin workbench.

Surfaces:

- Home
- Events
- Channels
- Messages
- Notifications
- Settings

Common constructs to close:

- `Grid.RowDefinitions` and `ColumnDefinitions`
- attached `Grid.Row`, `Grid.Column`, `Grid.RowSpan`, `Grid.ColumnSpan`
- `Grid.RowSpacing` and `ColumnSpacing`
- `Padding`, `Margin`, `MinHeight`, `MaxWidth`, `Width`, `Height`
- `BorderBrush`, `BorderThickness`, `CornerRadius`
- `ScrollViewer.HorizontalScrollBarVisibility`
- `TextBlock.TextWrapping`
- `TextBox.TextWrapping`, `MinHeight`, `AcceptsReturn`
- `ProgressRing.Width`, `ProgressRing.Height`, active state
- `InfoBar.IsClosable`, severity, message, title
- loading/content/empty/denied/error visual states

Steps:

1. Pick Home as the first representative common surface.
2. Add failing tests for the layout/property support Home requires.
3. Implement renderer support in reusable primitives, not page-specific hacks.
4. Add state fixture support for:
   - loading
   - content with at least one product card/panel
   - empty
   - denied
   - error
5. Expand the same gates to Events, Channels, Messages, Notifications, and
   Settings once Home passes.
6. Keep scenario assertions focused on visible controls and state-specific text,
   not pixel-perfect parity.

Done when:

- Each common surface has at least one direct-app scenario that passes.
- The renderer can show loading/content/empty/denied/error states without
  resource or binding failures.
- Unsupported diagnostics for these surfaces are either gone or explicitly
  deferred with a named reason.

### Phase 4 - P1 Shell And Navigation Gate

Objective: Prove the real production shell can host signed-out and signed-in
routes without relying on isolated page-only scenarios.

Constructs to close:

- `MainWindow.xaml` shell root
- `Frame` navigation
- signed-out `LoginPage` route
- signed-in `NavigationView` route
- nav item labels/icons
- pane footer account and logout controls
- `FontIcon` rendering or documented icon fallback
- selection state and content frame updates

Steps:

1. Add a signed-out shell scenario that starts at `MainWindow` and verifies the
   login frame.
2. Add a signed-in shell scenario with seeded session/user state.
3. Implement enough shell/window bootstrap to call the production route setup in
   a deterministic order.
4. Add route assertions for:
   - Home selected by default when signed in
   - switching to Messages changes `ContentFrame`
   - logout returns to login
5. Preserve diagnostics for Windows-only shell features instead of silently
   swallowing them.

Done when:

- Signed-out shell and signed-in shell scenarios pass.
- Navigation assertions can prove route changes in artifacts.
- Shell screenshots are accepted as direct app evidence because they come from
  `MeetingChallenge.Windows.csproj`.

### Phase 5 - P2 Product Panel Interactions

Objective: Move beyond static rendering and prove common product interactions
are wired enough for QA evidence.

Candidate interactions:

- Home feed product input and submit
- Events filter/refresh
- Channels selection
- Messages compose or selection flow
- Notifications refresh/dismiss state where available
- Settings preference toggle or save surface

Steps:

1. Select one interaction per common surface.
2. Add scenario fixture data that makes the target interaction visible.
3. Add interaction assertions to `interactions.json`:
   - target exists
   - click/type action succeeds
   - visible state changes or command records invocation
4. Keep action effects deterministic and local to the generated host.
5. Add diagnostics when an interaction cannot run because command binding or
   routed events are unsupported.

Done when:

- At least one meaningful interaction passes for every common product surface.
- Interaction artifacts are readable enough for a reviewer to see what changed.
- No product interaction requires live backend connectivity.

### Phase 6 - P3 Admin Overview Then Admin Workbench

Objective: Split the heaviest EMSI UI area into two realistic gates.

Admin overview constructs:

- `AdminStatus` `InfoBar`
- `AdminModuleNavigation`
- module list
- selected module title/subtitle
- summary cards
- loading/denied/error fallback states

Admin workbench constructs:

- `CommandBar.Content`
- `AutoSuggestBox`
- `AppBarButton`
- `ListView.ItemTemplate`
- `ListView.SelectionChanged`
- `ListView.SelectionMode`
- `ListView.IsItemClickEnabled`
- `ItemsControl.ItemTemplate`
- three-pane workspace layout
- refresh/search/select module interactions

Steps:

1. Create an Admin overview scenario with static seeded module data.
2. Close only the layout/resource/binding gaps needed for the overview.
3. Add Admin workbench tests for unsupported templating and selection behavior.
4. Implement template materialization in a constrained way:
   - support templates used by EMSI Admin first
   - emit diagnostics for unsupported template features
   - avoid generic full WinUI template emulation unless needed
5. Add search/select/refresh interaction gates.
6. Capture separate artifacts for Admin overview and Admin workbench so the
   large surface does not block earlier milestones.

Done when:

- Admin overview direct scenario passes before deeper workbench interactions.
- Admin workbench has a green static gate and at least one green interaction
  gate.
- Remaining Admin limitations are documented as explicit follow-up work.

### Phase 7 - Visual And Accessibility Hardening

Objective: Turn "renders" into evidence that is useful for EMSI UI review.

Steps:

1. Add light/dark runs for login and one common surface.
2. Add focus, hover, pressed, disabled, and selected state checks where the
   runtime supports them.
3. Verify `AutomationProperties.Name`, headings, live-region-style status text,
   and actionable button names in `accessibility.json`.
4. Compare Mac runtime screenshots to native Windows reference screenshots when
   available, but keep that as a separate parity label.
5. Document known non-parity areas such as icon metrics, font rendering, and
   platform-native control chrome.

Done when:

- Visual artifacts are good enough to review layout, hierarchy, copy, and
  obvious state regressions.
- Accessibility artifacts can support UIA/FlaUI-style downstream assertions.
- Parity claims clearly separate Mac-runtime evidence from native Windows
  evidence.

### Phase 8 - Documentation, Evidence, And Release Readiness

Objective: Keep the runtime's public/support story honest while the direct app
support matures.

Steps:

1. Update README/quick-start docs only after each green direct scenario exists.
2. Update support policy language to say which production EMSI surfaces are
   directly validated and which remain experimental.
3. Keep `MeetingChallenge.WinUI.MacRuntimeProbe` screenshots marked as
   non-milestone evidence.
4. Store private EMSI screenshots outside public runtime docs unless the user
   explicitly asks to publish or copy them.
5. Add a final evidence summary that lists:
   - scenario
   - project path
   - artifact directory
   - pass/fail status
   - known limitations

Done when:

- Docs do not overclaim.
- Each green milestone has a direct app artifact directory.
- Failed or partial runs are preserved as useful diagnostics, not hidden.

## Verification Gates

Targeted unit/runtime checks:

```sh
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp|AutomationContract"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "Resource|Localization|Binding|Interaction|Accessibility|Frame|Navigation|Renderer|Layout|InfoBar|TextBox|PasswordBox"
dotnet test --project tests/WinUI3.MacXaml.Tests/WinUI3.MacXaml.Tests.csproj
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
```

Direct EMSI app gates should write to a private evidence root, for example:

```sh
dotnet run --project src/WinUI3.MacRunner/WinUI3.MacRunner.csproj -- run-scenario /Users/marlonjd/Developer/monorepos/emsi_monorepo/apps/windows/tests/MeetingChallenge.WinUI.DirectRuntimeScenarios/login-light.json --output /private/tmp/emsi_qa/windows/mac-runtime-direct/login-light
```

Expected direct gate artifacts:

- `project-ingestion.json`
- `run.json`
- `tree.json`
- `accessibility.json`
- `interactions.json`
- `resource-failures.json`
- `binding-failures.json`
- screenshot image

Success criteria for a green milestone:

- `project-ingestion.json` passed against `MeetingChallenge.Windows.csproj`.
- `run.json` passed.
- required visual screenshot exists.
- required interactions passed.
- resource failures are empty for the milestone surface.
- binding failures are empty for the milestone surface unless explicitly
  deferred by the plan.
- accessibility artifacts expose meaningful names for the asserted controls.

## Risks And Mitigations

- Risk: The work expands into a full WinUI emulator.
  Mitigation: close only constructs used by named EMSI surfaces and keep
  unsupported diagnostics explicit.

- Risk: DataContext bootstrapping accidentally executes production services.
  Mitigation: use deterministic scenario fixtures and fake services first.

- Risk: Admin blocks earlier evidence because it is much larger than the common
  surfaces.
  Mitigation: ship Admin overview separately from Admin workbench interactions.

- Risk: Mac runtime screenshots are mistaken for native Windows parity.
  Mitigation: label them as direct app Mac runtime evidence and reserve native
  parity claims for native Windows reference runs.

- Risk: Private EMSI screenshots leak into public runtime docs.
  Mitigation: keep screenshots under `/private/tmp/emsi_qa` or downstream
  private artifacts unless publication is explicitly requested.

## Dependencies And Ownership

Runtime-owned:

- project ingestion
- generated host/page bootstrap
- resource and localization loading
- renderer/compiler support
- scenario runner output artifacts
- tests and runtime docs

Downstream app-owned or read-only for this plan:

- production EMSI XAML and code-behind
- production app service graph
- app scenario files under `apps/windows` unless explicitly approved for edit
- private QA evidence publication

## Recovery

If a phase fails late, keep the last failing direct artifacts and add a narrow
unit test for the first missing construct. Do not broaden the runtime silently.
Prefer one new XAML/control capability plus one direct scenario improvement per
iteration.

## Execution Prompt

Use this prompt for the next implementation thread:

```text
Implement the EMSI Windows direct runtime UI gap closure plan, starting with
Phase 0 and Phase 1 only.

Required context:
- Read repo/root and tools/app AGENTS files plus UI_RULES.
- Use EMSI workflows: task router, plan artifact, native UI overlay, and
  verification gate.
- Use local windows-winui3-design guidance and Google engineering practices.
- Use TDD: add failing tests for app resource/theme/localization lookup before
  implementation.

Constraints:
- Work in /Users/marlonjd/Developer/monorepos/emsi_monorepo/tools/winui3-mac-test-runtime.
- Do not create/switch branches.
- Preserve unrelated existing changes.
- Do not use MeetingChallenge.WinUI.MacRuntimeProbe screenshots as milestone
  evidence.
- Do not make backend calls or use credentials.
- Write direct EMSI evidence under /private/tmp/emsi_qa/windows/mac-runtime-direct.

First milestone:
- Re-read the latest direct login-light artifacts from apps/windows.
- Build a gap matrix for Login, shell, common surfaces, and Admin.
- Add failing tests for app resource dictionaries, theme dictionaries, and
  localized Login text lookup.
- Implement the smallest resource/localization loading path that removes the
  Login missing resource failures:
  AppBackgroundBrush, SurfaceBrush, SurfaceBorderBrush,
  PageTitleTextBlockStyle, PageBodyTextBlockStyle, SecondaryTextBrush.
- Re-run login-light against MeetingChallenge.Windows.csproj and record whether
  resource failures are gone. Binding/DataContext failures may remain for
  Phase 2, but must be clearly reported.

Verification:
dotnet test --project tests/WinUI3.MacRunner.Tests/WinUI3.MacRunner.Tests.csproj --filter "ProjectIngestion|GeneratedHost|DirectApp|AutomationContract"
dotnet test --project tests/WinUI3.MacRuntime.Tests/WinUI3.MacRuntime.Tests.csproj --filter "Resource|Localization|Binding|Interaction|Accessibility|Frame|Navigation|Renderer|Layout|InfoBar|TextBox|PasswordBox"
dotnet build src/WinUI3.MacRunner/WinUI3.MacRunner.csproj
```
