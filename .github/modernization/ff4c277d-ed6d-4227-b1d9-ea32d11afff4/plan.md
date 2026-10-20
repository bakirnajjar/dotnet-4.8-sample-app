# Goal and target

Deliver the first migration increment in `DescopeSampleApp.sln`: a new SDK-style ASP.NET Core `net10.0` application serving the requested API, MVC and page functionality, plus offline xUnit/WebApplicationFactory coverage. Keep the .NET Framework 4.8 project and its files unchanged and building in the same solution.

The assessment verifies .NET 10 LTS, its `net10.0` TFM, an installed .NET 10 SDK, and the existing net48 project (`assessment.md`, lines 13–27; [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), checked in the assessment on 2026-10-20).

# Approach

Stabilize the **exact full-solution test gate first**. Add a separate SDK-style net48 characterization-test project so the first green test run must discover and execute meaningful tests against unchanged legacy code; do not treat zero discovered tests as success. Characterize observable behavior before implementing it in the new host. Then add the host and a separate net10 xUnit/WebApplicationFactory project, and port one concern at a time. Run both mandated full-solution gates at the end of **every** task.

T001 is a go/no-go prerequisite. Its proposed compatibility bridge uses repository-level MSBuild configuration, conditional on the .NET SDK evaluating the legacy project, to locate the **real, already-installed** BuildTools WebApplication targets. It must not replace those targets with a stub, alter the legacy project, exclude that project from the solution, or change the gate. If this cannot produce a green full gate with tests discovered, stop: T002–T011 cannot honestly satisfy their validation contract.

# Decisions

- **Side-by-side, without proxying:** The new host is independently runnable; no YARP, route removal or edits to the legacy host. Source: run goal and assessment summary/route inventory (lines 5, 15–20).
- **Target:** `net10.0`, rather than directly retargeting System.Web code. Source: assessment version and project evidence (lines 13–28).
- **Controller scope:** Port `SampleController`, `HomeController`, **and** the assessed generated `Areas/HelpPage/Controllers/HelpController`. The assessment recommends leaving HelpPage for a later increment (lines 81–84), but the stated goal says to port MVC controllers; including it avoids silently leaving a controller unported. Treat its documentation dependencies as a separate high-risk layer.
- **Behavior:** Preserve characterized routes, response shapes, redirects and client-side page behavior. Do not silently tighten token-validation or server-side authorization semantics; record security limitations and propose a separately approved change. Source: assessment findings (lines 63–65, 76–77).
- **Tooling:** The assessment reports a green build with four warning lines and two identical `MSB4019` test-gate failures with no discovered tests (lines 32–41). An existing BuildTools WebApplication targets installation was located at `C:\BuildTools\MSBuild\Microsoft\VisualStudio\v18.0\WebApplications\Microsoft.WebApplication.targets` during read-only planning. T001 must verify a usable configuration rather than assume that finding proves compatibility.

# Assumptions

- The installed .NET 10 SDK, .NET Framework build tools and package feeds remain available. No task assumes permission to install or change machine-wide tools; a missing prerequisite is a blocker to report.
- Legacy characterization can exercise selected controllers and services directly without IIS. Where a legacy HTTP response cannot be captured without hosting or a live JWKS request, record that evidence gap; do not label a new-host expectation as a verified legacy result.
- Offline tests use deterministic local keys and an in-memory JWKS HTTP handler. They contact neither Descope nor an internal service.

# Out of scope

Legacy-project edits or removal; IIS/YARP hosting; deployment, containers and CI changes; weakening or excluding tests; remediation of the two assessed legacy jQuery advisories. Report those advisories and the incomplete legacy dependency audit as follow-ups, without changing legacy files.

# Tasks

### T001: Stabilize full-solution test gate
- Depends on: -
- Skills: test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Add at least one substantive, offline xUnit test against unchanged legacy token-expiration behavior; establish and record a nonzero discovered-test count.
- Risk: **high** (SDK evaluation may remain incompatible with classic WebApplication targets); mitigate by testing the real-targets bridge and full gate immediately; **code review after task**.
- Done when: both exact gates pass, tests are actually discovered and pass, the net48 legacy project builds, and no legacy file or full-solution gate has changed.

Add a separate SDK-style net48 characterization-test project to the solution. Use narrowly conditioned, repository-level MSBuild properties to let SDK MSBuild evaluate the unchanged classic project using existing BuildTools WebApplication targets; identify the legacy project accurately as a non-test project while leaving solution-wide test dispatch intact. Confirm the test project references and executes unchanged legacy code. If the real targets cannot be consumed, or another evaluation failure cannot be resolved in scope, **report T001 blocked and stop**—do not substitute a stub target, a solution filter, a narrower command, or a zero-test “pass.”

### T002: Characterize unchanged legacy behavior
- Depends on: T001
- Skills: test-baseline-and-characterization, dotnet-framework-web-migration, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Pin directly executable legacy controller/validator cases, route definitions, JSON/status expectations, expiration edge cases, and observable Login/AuthenticatedPage markup and redirects; document uncapturable live-JWKS or hosted-HTTP cases.
- Risk: **high** (untested HTTP behavior may be mistaken for an established contract); mitigate with source-linked assertions and an explicit evidence-gap list; **code review after task**.
- Done when: meaningful legacy tests pass without editing source, discovery has not decreased, and each planned port has a characterized case or documented limitation.

Extend the net48 harness before any behavioral port. Test missing and malformed bearer requests without network access, token-expiration behavior and controller results where direct invocation is practical. Record original routes, page URLs, client-side redirect intent and HelpPage actions. Do not make live calls merely to produce golden responses.

### T003: Add .NET 10 web host
- Depends on: T002
- Skills: dotnet-framework-web-migration, dotnet-target-framework-upgrade, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: T002 precedes host behavior; retain its legacy tests unchanged.
- Risk: **medium** (mixed legacy/SDK-style solution restore and build); mitigate by adding only the host skeleton and proving both gates.
- Done when: the `Microsoft.NET.Sdk.Web` `net10.0` project is in the solution, independently starts, and both full gates remain green.

Create only the new host, basic pipeline and configuration structure. Do not copy System.Web assemblies or introduce migrated routes yet. Keep its package and configuration choices compatible with a standalone ASP.NET Core application.

### T004: Add ASP.NET Core integration-test harness
- Depends on: T003
- Skills: test-baseline-and-characterization, dotnet-framework-web-migration, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Retain T002 assertions; add a real WebApplicationFactory host-start test and prepare an in-memory JWKS handler without claiming unimplemented routes pass.
- Risk: **medium** (test adapter or factory setup can yield false-green discovery); mitigate by checking executed counts for **both** test projects.
- Done when: a separate `net10.0` xUnit project references the new host, its WebApplicationFactory tests execute, and both full gates pass.

Build reusable deterministic key/JWKS fixtures and a factory override for HTTP dependencies. Reject unexpected outbound requests in the fixture. Keep net48 legacy tests separate from net10 host tests.

### T005: Port token validation
- Depends on: T004
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Compare with T002 for observable expiration and error behavior; add offline tests for valid/invalid/expired tokens, multiple keys, malformed JWKS and JWKS-fetch failure.
- Risk: **high** (signature, expiry and error semantics are security-sensitive); mitigate with deterministic signed tokens and explicit behavior comparisons; **code review after task**.
- Done when: the new-host validator has injectable JWKS HTTP access, tests prove no external calls, documented behavior is preserved or explicitly identified as unverified, and both gates pass.

Implement the new-host validator as a distinct service; do not edit or share-compilation-copy the legacy implementation. Preserve verified behavior while avoiding an accidental network call in tests. Any proposed security tightening that changes the characterized contract must be separately decided rather than slipped into this port.

### T006: Port Sample Web API controller
- Depends on: T005
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Use T002’s missing/invalid bearer cases; WebApplicationFactory tests cover `/api/sample` success, unauthorized responses, JSON shape and offline JWKS failures.
- Risk: **high** (authentication errors or serializer defaults can change wire behavior); mitigate with status/body assertions and reviewed exception handling; **code review after task**.
- Done when: the new host serves the API route through the injected validator, offline HTTP tests execute, and both gates pass.

Port the handwritten Web API endpoint as an ASP.NET Core controller. Preserve supported route and response behavior; do not rely on `[ApiController]` defaults to introduce uncharacterized 400 responses or expose exception details. Leave the original endpoint untouched.

### T007: Port Home MVC controller and view
- Depends on: T006
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Pin the characterized `/` and `/Home/Index` outcomes, page title and relevant links with WebApplicationFactory.
- Risk: **medium** (layout, static assets and generated URLs can differ); mitigate with HTTP and rendered-markup assertions.
- Done when: Home routes and view work on the new host, link intent is retained, and both gates pass.

Move the small handwritten MVC surface as one concern. Replace legacy Razor bundle helpers only as needed for the new view; do not edit bundled legacy assets or change the legacy layout.

### T008: Port HelpPage MVC controller
- Depends on: T007
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Exercise characterized HelpPage routes and error paths; test the new host’s index, API-detail and resource-model actions and rendered responses offline.
- Risk: **high** (the generated area depends on Web API 2 `HttpConfiguration` and documentation models); mitigate by isolating an ASP.NET Core API Explorer-backed equivalent and reviewing route/content differences; **code review after task**.
- Done when: all three assessed HelpController actions have new-host equivalents with tested routes, neither legacy code nor its dependencies were edited, and both gates pass.

Port this generated documentation layer separately from Home. Use ASP.NET Core metadata rather than importing System.Web/old Web API assemblies. If exact legacy documentation output cannot be established without hosting, identify that gap and test the agreed route/action behavior; do not claim byte-for-byte equivalence.

### T009: Rewrite Login as a Razor Page
- Depends on: T008
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Compare the served `/login.aspx` route, component and script markup, and success redirect intent against T002.
- Risk: **medium** (WebForms-to-Razor markup and client events can drift); mitigate with rendered-page assertions and preserved path mapping.
- Done when: the new Razor Page serves the intended legacy login URL and passes offline HTTP/markup tests and both gates.

Rewrite the page in the new project rather than trying to execute `.aspx` on ASP.NET Core. Preserve the assessed client-side flow; do not add an unapproved authentication protocol or alter the original page.

### T010: Rewrite AuthenticatedPage as a Razor Page
- Depends on: T009
- Skills: dotnet-framework-web-migration, test-baseline-and-characterization, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Assert served `/AuthenticatedPage.aspx` markup, login redirect target, sample-API request intent and the assessed **client-side** session check.
- Risk: **high** (mistaking a browser check for server authorization would change behavior and security claims); mitigate with explicit HTTP/markup tests and a documented limitation; **code review after task**.
- Done when: the Razor Page and its offline tests pass, no unapproved server authorization behavior is asserted or introduced, and both gates pass.

Rewrite only this second page and the new-host assets it needs. Preserve the tested flow while clearly documenting that legacy authorization is client-side, not a server-enforced guarantee.

### T011: Final validation and report
- Depends on: T001, T002, T003, T004, T005, T006, T007, T008, T009, T010
- Skills: modernization-report, test-baseline-and-characterization, dependency-vulnerability-remediation, build-fix-loop
- Validation:
  - build: `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal`
  - test: `dotnet test --no-build -c Release`
  - lint: none defined
- Characterization: Reconcile legacy and new-host test counts and document every remaining hosted-HTTP or live-service evidence gap.
- Risk: **medium** (a report could overstate compatibility or vulnerability coverage); mitigate with gate outputs, task evidence and a final code review.
- Done when: both full gates pass after the last change, the unchanged legacy project still builds, the final review is complete, and `final-report.md` accurately records results and limitations.

Produce `.github/modernization/ff4c277d-ed6d-4227-b1d9-ea32d11afff4/final-report.md` from the task ledger and final validation. Include the existing four-warning build baseline, formerly red test gate, executed test counts, new-host package inventory, the two known legacy jQuery advisories, and the incomplete legacy audit. State deployment and CI work as **not performed**.

# Risks

The overriding risk is T001: installed targets have been located, **not proven compatible** with SDK-driven full-solution test evaluation. Failure to establish a green, nonzero-test gate blocks the sequence; it is not grounds to weaken validation. JWT and WebForms behavior have material security and compatibility gaps, so high-risk ports receive reviews and the final report must distinguish characterized facts from unverified equivalence.
