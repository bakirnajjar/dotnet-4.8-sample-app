# Phase 1 assessment — 2026-10-20

## Summary

**Observed:** One legacy .NET Framework 4.8 ASP.NET application builds with Windows MSBuild. There is no test project; the requested full `dotnet test` command fails twice before test discovery because the .NET SDK lacks the classic WebApplication targets. **Recommended:** Add a separate SDK-style `net10.0` ASP.NET Core host and tests to the existing solution, retaining the untouched, buildable legacy application. Port the named routes and pages without YARP, IIS/deployment or CI changes. Overall risk **high**: no executable regression tests and token validation currently depends on live JWKS and contains security-sensitive semantics (`DescopeSampleApp/TokenValidator.cs:26-80`).

Baseline: build **GREEN**, 4 warning lines (2 NU1902, 1 MSB3247, 1 CS0168); tests **not discovered (0/0/0), command RED**, no lint configured. Zero tests is **not** evidence of a passing suite.

## Inventory

`git ls-files`: **134** tracked files; largest extensions: `.cs` 41, `.map` 24, `.cshtml` 23, `.css` 18, `.js` 12, `.config` 6, `.aspx` 2. One solution, one project; C# and Razor/WebForms/JavaScript; no tracked test project, global.json, central package properties, lockfile, Docker/compose/Helm/IaC, Azure Pipelines or GitHub workflow. README lines 20-34 documents Visual Studio restore/build/run, not CLI tests. No repository lint command.

| Unit | Path | Kind/stack | Current version evidence | Candidate target |
|---|---|---|---|---|
| Solution | `DescopeSampleApp.sln:2-17` | Visual Studio 17 solution; one project | Classic project GUID, Release configuration | Add new web and xUnit projects beside legacy |
| Legacy host | `DescopeSampleApp/DescopeSampleApp.csproj:2-19,375` | Non-SDK System.Web MVC 5, Web API 2, WebForms/IIS Express | `TargetFrameworkVersion=v4.8`; `Web.config` compilation/httpRuntime 4.8 | **Remain net48 unchanged** |
| New web host (proposed) | Not present | SDK-style ASP.NET Core, Razor Pages, MVC and API | None | `net10.0` |
| New test project (proposed) | Not present | xUnit + WebApplicationFactory | None | `net10.0` |

Project graph: only the legacy project, no `<ProjectReference>`; its `packages.config` is at `DescopeSampleApp/packages.config:1-35`. New tests should reference the new host, not the System.Web host. `Global.asax.cs:14-23` wires MVC/Web API/routes/bundles; `App_Start/WebApiConfig.cs:15-21` maps `api/{controller}/{id}`; `App_Start/RouteConfig.cs:12-16` maps `{controller}/{action}/{id}` with Home/Index default. App configuration setting **names only**: `webpages:Version`, `webpages:Enabled`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled` (`DescopeSampleApp/Web.config`); `DESCOPE_PROJECT_ID` (`TokenValidator.cs:13`, `README.md:36-45`). No `connectionStrings` element identified.

## Version and support facts

| Fact | Value | Official source URL | Checked on |
|---|---|---|---|
| .NET 10 / ASP.NET Core 10 | LTS, active; latest patch **10.0.13**; end of support **2028-11-14**. Candidate TFM `net10.0`; installed SDK 10.0.402, runtime 10.0.13 (`dotnet --info`). | https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core ; https://learn.microsoft.com/en-us/dotnet/core/releases-and-support | 2026-10-20 |
| .NET Framework 4.8 | Support follows the parent Windows OS lifecycle; **no universal 4.8 end date**. Do not call it unsupported without the deployment OS. | https://learn.microsoft.com/en-us/lifecycle/faq/dotnet-framework | 2026-10-20 |
| MVC 5/Web API 2, jose-jwt 5.0.0, IdentityModel 7.5.1, WebGrease 1.6.0, Antlr 3.5.0.2 and bundled browser libraries | Package-specific EOS and compatible replacement releases **unverified**; System.Web host cannot be re-targeted directly to ASP.NET Core. | Versions: `DescopeSampleApp/packages.config:3-34`; platform migration: `DescopeSampleApp/DescopeSampleApp.csproj:12-17` | 2026-10-20 |

## Build, test and CI

Run from `C:\workspace\repo` (solution directory), unmodified sources; Windows 10.0.26100, .NET SDK 10.0.402/MSBuild 18.9.15, Windows BuildTools MSBuild available. Commands were exact, non-interactive; no result reporter was added to the mandated commands.

| Command | Source | Exit | Duration | Result |
|---|---|---:|---:|---|
| `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal` | Run baseline | 0 | 21.609 s | Built `DescopeSampleApp/bin/DescopeSampleApp.dll`; 4 warning lines: two NU1902 jQuery advisories, MSB3247 assembly binding conflicts, CS0168 unused variable at `Controllers/SampleController.cs:31`. |
| `dotnet test --no-build -c Release` | Run baseline, full suite attempt | 1 | 0.661 s | MSB4019: imported `Microsoft.WebApplication.targets` absent under dotnet SDK 10.0.402 at `DescopeSampleApp.csproj:375`; no test execution or result files. |
| `dotnet test --no-build -c Release` | Repeat on failure | 1 | 0.533 s | Identical MSB4019; stable **environment/toolchain incompatibility**, not flaky or a test assertion defect. |
| `& C:\shared\skills\dependency-vulnerability-remediation\scripts\scan-dependencies.ps1 -Root . -Only dotnet -NoRestore` | Scanner skill | 1 | Not timed | ERROR: `dotnet list package` hits same missing WebApplication targets; MANUAL: packages.config not covered. Audit incomplete, not clean. |

Test totals **passed 0 / failed 0 / skipped 0; discovered 0** (no TRX or JUnit files; no tracked tests). The CLI test exit is **1**, so do not report tests as passing. No known failing test IDs. The test command fails because it evaluates the classic web project with the SDK rather than using BuildTools MSBuild; new test-project discovery should be verified without reducing the run's full solution test gate. No CI definitions or pinned CI runtime found; no lint defined. Proposed later validation: original full MSBuild command; original full `dotnet test --no-build -c Release` with new tests (resolve legacy evaluation incompatibility without changing legacy), plus `dotnet build` and `dotnet test` for the new projects as supplemental gates; compare discovered test counts. No lint to invent.

## Dependencies

`packages.config:3-34` declares **32** packages, directly listed rather than a resolvable modern transitive graph. Principal groups: MVC 5.2.9/Razor 3.2.9/Web API 5.2.9 (System.Web-only, port to ASP.NET Core framework); Microsoft.AspNet.Web.Optimization 1.1.3, WebGrease 1.6.0, Antlr 3.5.0.2 (legacy bundles); jose-jwt 5.0.0, IdentityModel/Tokens/JWT 7.5.1 (evaluate net10-compatible modern versions); Newtonsoft.Json 13.0.3 (test JSON shape before serializer choice); bootstrap 5.2.3, jQuery 3.4.1, Modernizr 2.8.3 (browser assets); `System.*` 4.x and Bcl.AsyncInterfaces 1.1.0 (assess need in new host). The actual direct-vs-transitive provenance and package release support/deprecation for .NET 10 are **unverified**; do not claim these packages are safe or all are supported.

| Advisory | Severity | Package | Installed | First fixed | Evidence/status |
|---|---|---|---|---|---|
| GHSA-jpcq-cgw6-v4j6 / CVE-2020-11023 | Moderate | jQuery (direct entry) | 3.4.1 | 3.5.0 | MSBuild NU1902; https://api.github.com/advisories/GHSA-jpcq-cgw6-v4j6 (checked 2026-10-20); untrusted HTML DOM insertion XSS; reachability unverified. |
| GHSA-gxr4-xjj5-5px2 / CVE-2020-11022 | Moderate | jQuery (direct entry) | 3.4.1 | 3.5.0 | MSBuild NU1902; https://api.github.com/advisories/GHSA-gxr4-xjj5-5px2 (checked 2026-10-20); untrusted HTML DOM insertion XSS; reachability unverified. |

**Verified minimum: 2 moderate, 0 confirmed high/critical/low; complete totals unknown.** The bundled `Scripts/jquery-3.4.1*.js` is present. No tracked `nuget.config`, private feed config or npm lockfile; npm audit cannot inventory CDN-loaded JavaScript (`Login.aspx:7-8`, `AuthenticatedPage.aspx:7`). Do not suppress findings; first-increment dependency remediation requires a scope decision if it touches the unchanged legacy project.

## Blockers and hotspots

| Pattern | Tracked files affected | Example | Approach |
|---|---:|---|---|
| System.Web usage | 20 `.cs` files | `Global.asax.cs:4,14`; `Controllers/SampleController.cs:8-16` | Port only named endpoints to new hosting, no direct TFM change |
| WebForms pages | 2 `.aspx` pages plus code-behind/designers | `Login.aspx:1-26`, `AuthenticatedPage.aspx:1-60` | New Razor Pages; preserve paths/redirect intent with explicit route characterization |
| `HttpContext.Current` | 1 file (commented-out reference) | `Areas/HelpPage/App_Start/HelpPageConfig.cs` | No new-host port in this increment |
| `System.Drawing` | 1 file (using import) | `Controllers/SampleController.cs:3` | Avoid carrying unnecessary Windows-specific import into new host |
| WCF/Remoting/BinaryFormatter/Registry/EventLog | 0 detected by listed source-pattern scan | Source-pattern count, 2026-10-20 | Recheck if scope expands |
| Live JWKS and validation semantics | 1 validator and controller | `TokenValidator.cs:20-23,26-80`; `Controllers/SampleController.cs:16-38` | Injectable HttpClient/JWKS endpoint, entirely offline tests; pin errors, expiration, keys and response codes before refactor |

`TokenValidator.cs:20-23` prioritizes the environment-provided project ID over the constructor argument; `:71-80` creates a new HttpClient and downloads JWKS from a public provider. `:56-68` parses token expiry without itself validating signature; `:38-39` decodes with a JWK and returns raw payload. `SampleController.cs:31-34` catches only `SecurityTokenValidationException`, while validator throws/rethrows generic exceptions at `:47-53`; unknown-key/network error may produce server error. Preserve observed HTTP behavior until tests or an explicit security behavior decision. `AuthenticatedPage.aspx:18-28` uses client-side session checking/redirect; this is not server-enforced authorization.

## Test services

No tracked test suite or database/broker/cache requirements found. Offline JWKS xUnit/WebApplicationFactory tests should use deterministic local in-memory HTTP responses and keys; **never contact actual Descope endpoints or internal services**. Live application use requires `DESCOPE_PROJECT_ID` and external JWKS, neither necessary for offline tests. `docker version` exit 0: Windows client 29.8.2, remote Linux engine 29.8.2; environment has `DOCKER_HOST` and `TESTCONTAINERS_HOST_OVERRIDE` (names only); testcontainers must reach published ports via override host, **not localhost**. No test service containers required for the proposed suite; no service connections established.

## Risks

| Risk | Severity | Likelihood | Mitigation |
|---|---|---|---|
| Full solution CLI test command fails reliably before discovery; no legacy tests | High | Certain | Preserve documented red baseline, establish new runnable tests, diagnose full-solution test tooling separately; never claim 0 tests is green. |
| JWT verification/network failures, token expiration and authorization semantics may change silently | High | High | Characterization tests for valid/invalid/expired/missing bearer, JWKS errors, multiple keys, JSON/status and no-network guarantees; decide any tightened security behavior explicitly. |
| Legacy WebForms and MVC/Web API route/HTML/browser differences | High | High | Wire-level snapshots or isolated controller tests before ports; preserve URLs, redirects, JSON strings/content type and script dependencies. |
| Incomplete packages.config security coverage and 2 confirmed jQuery XSS advisories | Medium | High | Treat audit as incomplete; inventory CDN assets and address findings in a separately approved scope; do not rewrite unchanged legacy app. |
| MSB3247 binding conflict warnings in legacy build | Medium | Medium | Keep baseline warning count and prove unchanged legacy Release build after each increment. |

## Open questions

1. What should the new-host routes be for `/login.aspx`, `/AuthenticatedPage.aspx`, `/api/sample`, `/` and the HelpPage area? Recommendation: preserve named routes where possible; HelpPage stays legacy this increment.
2. Should the new host intentionally tighten JWT audience/issuer/expiration and network error responses, or preserve current observable behavior until a separate security change? Recommendation: get an explicit behavior decision after characterization.
3. Can the full `dotnet test --no-build -c Release` gate be made compatible with the unchanged classic WebApplication project in this environment? If not, retain and report its failure; do not silently narrow the gate.
4. Are browser/CDN and jQuery vulnerability updates authorized despite the “legacy unchanged” and no-deployment scope? Recommendation: separate approved follow-up.
5. Package-specific vendor support/EOS and target compatibility remain unverified until package registry and release-note review; no private feeds identified.

## Candidate tasks

1. **Baseline stabilization/validation design:** characterize the full `dotnet test` MSB4019 tooling limitation without changing the legacy project; ensure test gate will discover newly added tests. Skills: test-baseline-and-characterization, build-fix-loop.
2. **Characterization:** add offline behavioral baselines for HTTP routes, JSON/status, missing/invalid/expired bearer, JWKS failure, login redirect and page markup before corresponding new-host implementations, where executable against original code; document legacy-only gaps. Skill: test-baseline-and-characterization.
3. **New .NET 10 Web SDK project:** add alongside existing solution project, baseline shell/route and supplementary build. Skills: dotnet-framework-web-migration, dotnet-target-framework-upgrade.
4. **New net10 test project:** xUnit and WebApplicationFactory with offline JWKS fixture; ensure meaningful test discovery. Skills: test-baseline-and-characterization, dotnet-framework-web-migration.
5. **Port TokenValidator plus Web API/MVC routes:** explicit HTTP/JWKS injection and route/response equivalence, with auth behavior decision if changed. Skill: dotnet-framework-web-migration.
6. **Port Login and AuthenticatedPage to Razor Pages:** preserve intended flow and routes, test offline page/redirect behavior. Skill: dotnet-framework-web-migration.
7. **Final whole-solution build, full test attempt and dependency audit; report limitations and scope exclusions.** Skills: build-fix-loop, dependency-vulnerability-remediation, modernization-report. No legacy removal, YARP, IIS, deployment or CI changes in this increment.
