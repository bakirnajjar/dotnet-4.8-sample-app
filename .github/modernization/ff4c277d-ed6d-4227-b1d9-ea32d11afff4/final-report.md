# Modernization report: first .NET Framework-to-.NET 10 increment

Outcome: PARTIAL - 11 of 22 ledger tasks completed (11 originals replaced/skipped for erroneous lint metadata); build GREEN; tests 79/79 passing (baseline 0/0, command RED before discovery).

## Summary

- Goal: Add an independently runnable `net10.0` ASP.NET Core host for the requested API, MVC controllers and Razor replacements for two Web Forms pages, with offline integration tests, while leaving the original .NET Framework project unchanged and buildable.
- Scenario: `dotnet-framework-to-dotnet` (scenario version unknown); path scope: whole repository. Strategy: side-by-side without YARP or IIS integration; the System.Web application cannot be directly retargeted. The implementation goal is met in the branch, but the ledger is not yet closed and the dependency scan is incomplete.
- Branch: `modernize/dotnet-framework-to-dotnet-ff4c277d` based on `master`; 10 implementation checkpoint commits through `2100b53` before the final report checkpoint. Source: run state, [plan](plan.md), `git log --oneline master..HEAD`.
- Plan amendment: original T001–T011 contained executable `lint: ["none defined"]`, although the scenario defines no lint command. T001's otherwise-green completion gate rejected this nonexistent executable; the approved amendment replaced each original with a `.1` task, preserving the exact build and test gates. **Skipped means metadata replacement, not an unperformed migration step.** See [T001](progress/T001.md) and [T001.1](progress/T001.1.md).

## Versions

| Component | Before | After | Support/LTS status and source |
|---|---|---|---|
| Legacy application | .NET Framework 4.8, System.Web MVC 5.2.9 / Web API 5.2.9 / Web Forms | Unchanged; still in four-project solution | Framework 4.8 follows host Windows lifecycle; no universal end date. [Microsoft lifecycle FAQ](https://learn.microsoft.com/en-us/lifecycle/faq/dotnet-framework), checked 2026-10-20 in [assessment](assessment.md). |
| New application | Absent | ASP.NET Core `net10.0`, SDK-style `Microsoft.NET.Sdk.Web` | .NET 10 LTS, support through 2028-11-14 per [Microsoft .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core) and [releases and support](https://learn.microsoft.com/en-us/dotnet/core/releases-and-support), checked 2026-10-20 in assessment; installed SDK 10.0.402/runtime 10.0.13 at assessment time. Package-specific support beyond framework status unknown. |
| Test framework | No tracked tests | net48 and net10.0 xUnit 2.9.3, runner 3.1.4, Microsoft.NET.Test.Sdk 17.14.1; net10.0 Microsoft.AspNetCore.Mvc.Testing 10.0.0 | Versions from added test `.csproj` files; package-specific support dates unknown. |
| Token/JSON libraries | Legacy jose-jwt 5.0.0, IdentityModel/Tokens/JWT 7.5.1, Newtonsoft.Json 13.0.3 | Legacy unchanged; new host uses framework APIs and its own validator, with no added host PackageReference | Legacy versions: [assessment](assessment.md) / `DescopeSampleApp/packages.config`; new project: `DescopeSampleApp.Core/DescopeSampleApp.Core.csproj`. Library EOS unknown. |
| Browser assets | Legacy jQuery 3.4.1, Bootstrap 5.2.3, Modernizr 2.8.3 | Legacy unchanged; new host copies existing Bootstrap/Site assets but not jQuery or Modernizr; login/page scripts still refer to pinned external Descope assets | Versions/source: assessment, [T007.1](progress/T007.1.md), [T009.1](progress/T009.1.md); CDN availability and asset support unknown. |

## Tasks

| Key | Title | Status | Checkpoint | Result |
|---|---|---|---|---|
| T001 | Stabilizing full-solution test gate | Skipped | — | [Note](progress/T001.md): implementation and exact gates green; nonexistent executable lint metadata blocked completion. Replaced by T001.1. |
| T001.1 | Stabilize full-solution test gate | Completed | `92213f6` | [Note](progress/T001.1.md): real BuildTools-targets bridge, net48 test project, 3 passing tests. |
| T002 | Characterizing unchanged legacy behavior | Skipped | — | Replaced by T002.1 to remove erroneous executable lint metadata; no functionality dropped. |
| T002.1 | Characterize unchanged legacy behavior | Completed | `db5b7ab` | [Note](progress/T002.1.md): 23 offline net48 characterization tests total. |
| T003 | Adding .NET 10 web host | Skipped | — | Replaced by T003.1 for lint metadata only. |
| T003.1 | Add .NET 10 web host | Completed | `c2dc0fd` | [Note](progress/T003.1.md): standalone SDK-style web host. |
| T004 | Adding ASP.NET Core integration-test harness | Skipped | — | Replaced by T004.1 for lint metadata only. |
| T004.1 | Add ASP.NET Core integration-test harness | Completed | `3800305` | [Note](progress/T004.1.md): xUnit/WebApplicationFactory and offline JWKS fixture. |
| T005 | Porting token validation | Skipped | — | Replaced by T005.1 for lint metadata only. |
| T005.1 | Port token validation | Completed | `42d7fd6` | [Note](progress/T005.1.md): separate injected validator, offline signature/JWKS tests. |
| T006 | Porting Sample Web API controller | Skipped | — | Replaced by T006.1 for lint metadata only. |
| T006.1 | Port Sample Web API controller | Completed | `8390450` | [Note](progress/T006.1.md): API route, JSON success and empty 401/503 paths. |
| T007 | Porting Home MVC controller and view | Skipped | — | Replaced by T007.1 for lint metadata only. |
| T007.1 | Port Home MVC controller and view | Completed | `ab9983c` | [Note](progress/T007.1.md): Home view, layout and static assets. |
| T008 | Porting HelpPage MVC controller | Skipped | — | Replaced by T008.1 for lint metadata only. |
| T008.1 | Port HelpPage MVC controller | Completed | `676aa17` | [Note](progress/T008.1.md): three Help actions and API Explorer-backed catalog. |
| T009 | Rewriting Login as a Razor Page | Skipped | — | Replaced by T009.1 for lint metadata only. |
| T009.1 | Rewrite Login as a Razor Page | Completed | `553946e` | [Note](progress/T009.1.md): `/login.aspx` and offline markup tests. |
| T010 | Rewriting AuthenticatedPage as a Razor Page | Skipped | — | Replaced by T010.1 for lint metadata only. |
| T010.1 | Rewrite AuthenticatedPage as a Razor Page | Completed | `2100b53` | [Note](progress/T010.1.md): `/AuthenticatedPage.aspx` and offline markup tests. |
| T011 | Performing final validation and report | Skipped | — | Replaced by T011.1 for lint metadata only. |
| T011.1 | Final validation and report | Completed upon successful completion gate | Assigned by completion gate | [Note](progress/T011.1.md): full gates and review green; scanner incomplete; report prepared. Completion is contingent on the final gate. |

For T002–T011, the [approved amendment in run state](plan.md) replaced unfinished tasks before they acquired individual progress notes. No failed tasks or approved test exclusions are recorded.

## Changes

`git diff --stat master...HEAD` before this report: 51 files changed, 2,392 insertions; all listed tracked changes were additions except `DescopeSampleApp.sln`. Added `Directory.Build.props` to direct only SDK evaluation of the unchanged legacy web project to installed real WebApplication targets. Added `DescopeSampleApp.CharacterizationTests/`, `DescopeSampleApp.Core/` and `DescopeSampleApp.IntegrationTests/`; ported `TokenValidator`, Sample/Home/Help controllers and the two Razor Pages, views and selected static assets. The original `DescopeSampleApp/` files and `packages.config` were not changed. Sources: `git diff --name-status master...HEAD`, task notes above.

## Validation

| Gate | Exact command | Baseline | Final after `2100b53` |
|---|---|---|---|
| Full solution build | `msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal` | GREEN; 4 warning lines: 2 NU1902, MSB3247, CS0168 | GREEN; four projects including unchanged classic app; 3 warning lines: 2 NU1902 and MSB3247. Final validator and `.modernization-scratch/logs/final-build-transcript.txt` record exit 0 on 2026-10-20; transcript contains exit summary, not warning text. |
| Full solution tests | `dotnet test --no-build -c Release` | RED twice (MSB4019 before discovery); 0 passed / 0 failed / 0 skipped, **not a passing baseline** | GREEN; 23 net48 characterization + 56 net10 integration = **79 passed / 0 failed / 0 skipped**. Final validator and `.modernization-scratch/logs/final-test-transcript.txt` record exit 0 on 2026-10-20; transcript contains exit summary, not per-test output. |
| Lint | None defined | Not run | Not run; scenario lint list empty. |
| Dependency audit | `& C:\shared\skills\dependency-vulnerability-remediation\scripts\scan-dependencies.ps1 -Root . -OutputDir .modernization-scratch/scan` (final; baseline used `-Only dotnet -NoRestore`) | Exit 1; classic web-target evaluation error and `packages.config` manual gap | Exit 1, **INCOMPLETE**: `dotnet list package` for solution failed and 32-entry legacy `packages.config` cannot be audited by it. See `.modernization-scratch/logs/final-scan-transcript.txt` and `.modernization-scratch/scan/dotnet-DescopeSampleApp.sln.txt` (restore only, no finding inventory). .NET SDK 10.0.402 / NuGet 7.9.0-rc.611; scan 2026-10-20. |

Tests added: `DescopeSampleApp.CharacterizationTests/{TokenValidatorCharacterizationTests,ControllerCharacterizationTests,RouteAndMarkupCharacterizationTests}.cs`; `DescopeSampleApp.IntegrationTests/{HostIntegrationTests,TokenValidatorTests,SampleApiTests,HomeMvcTests,HelpMvcTests,LoginPageTests,AuthenticatedPageTests}.cs` with `OfflineHostFixture.cs`. The net48 tests directly inspect legacy behavior/route and source intent, **not** hosted legacy HTTP; net10 HTTP tests and JWKS are in-process/offline. Approved test exclusions: **none** (run state); final skipped tests: **0**. Whole-branch read-only review reported no new high-confidence blocker, with login destination and stricter expiry/JWKS failure differences noted; no production/browser smoke verification is claimed.

## Dependencies and security

| Package/dependency | Before | After | Change and reason |
|---|---|---|---|
| Legacy `packages.config` (32 entries), including jQuery | Existing versions unchanged | Unchanged | No legacy manifest edits; out of approved scope. |
| Microsoft.NET.Test.Sdk; xUnit; xUnit VS runner | Absent | 17.14.1; 2.9.3; 3.1.4 in both test projects | Added for discovered xUnit tests. |
| Microsoft.AspNetCore.Mvc.Testing | Absent | 10.0.0 in net10 test project | Added for WebApplicationFactory. |
| ASP.NET Core framework | No separate new host | `net10.0` Web SDK/framework reference | New host replaces System.Web functionality **in the parallel host only**, not the original project. |
| New-host Bootstrap/Site static assets | Legacy files already present | Existing assets copied into new host; no jQuery copy | Reuse layout styling without importing the vulnerable jQuery; no claim of comprehensive browser-dependency audit. |

Before: **at least two confirmed moderate** jQuery findings; other severity totals unknown. After: **the same two confirmed moderate remain in unchanged legacy app**; total critical/high/moderate/low counts unknown due to incomplete scans. No advisory is claimed fixed, suppressed or accepted. Source: [assessment](assessment.md), final scanner transcript and unchanged legacy manifest.

| Advisory | Severity | Package | From | To | Direct/transitive | Status / evidence |
|---|---|---|---|---|---|---|
| GHSA-jpcq-cgw6-v4j6 / CVE-2020-11023 | Moderate | jQuery | 3.4.1 | Unchanged (first fixed 3.5.0) | Direct `packages.config` entry; full graph unverified | Open; legacy NU1902 / [assessment](assessment.md), advisory checked there 2026-10-20. |
| GHSA-gxr4-xjj5-5px2 / CVE-2020-11022 | Moderate | jQuery | 3.4.1 | Unchanged (first fixed 3.5.0) | Direct `packages.config` entry; full graph unverified | Open; legacy NU1902 / [assessment](assessment.md), advisory checked there 2026-10-20. |

Owner decision to accept/mitigate these findings: **not recorded**; remediation and complete legacy audit require separately approved scope. Reachability remains unverified. The new host deliberately does not copy jQuery, which does **not** eliminate the legacy findings.

## Behavior and compatibility changes

| Change / impact | Decision and evidence | Covered by |
|---|---|---|
| Separate `net10.0` listener, not a transparent switch from IIS; routes `/`, `/Home/Index`, `/api/sample` (optional id), `/Help/...`, `/login.aspx`, `/AuthenticatedPage.aspx` are served by new host. | Side-by-side plan; no proxy/deployment changes. Legacy hosted route status, trailing slash, headers and content negotiation unverified ([T002.1](progress/T002.1.md)). | `HomeMvcTests`, `SampleApiTests`, `HelpMvcTests`, `LoginPageTests`, `AuthenticatedPageTests` in new host. |
| New validator verifies RS256/JWKS signatures and checks expiration; malformed/missing/expired claims rejected. Legacy sampled expiry checks all returned false through an exception, not a working comparison. | Intentional non-reproduction of observed legacy defect; legacy live-JWKS/nonempty-token equivalence unverified ([T001.1](progress/T001.1.md), [T005.1](progress/T005.1.md)). | `TokenValidatorCharacterizationTests`, `TokenValidatorTests`. |
| API success is a JSON string; absent/invalid bearer returns empty 401; malformed JWKS or fetch failure returns empty 503. This may differ from uncaptured legacy nonempty-token failures. | `JsonResult` instead of text/plain `Ok(string)`; explicit 503 safety behavior ([T006.1](progress/T006.1.md)). | `ControllerCharacterizationTests` (limited direct/formatter evidence), `SampleApiTests`. |
| Help uses Core API Explorer rather than Web API 2 sample generation; IDs, metadata, HTML and HTTP details may differ. Missing IDs/models render Error view (new-host HTTP 200). | No byte-level legacy hosted capture; test-only described model proves resource rendering ([T008.1](progress/T008.1.md)). | `HelpMvcTests`, legacy direct-action/source tests. |
| Login success navigates to `/AuthenticatedPage.aspx` rather than legacy `/`. Authentication page's new bearer request repairs a truncated/uncharacterizable legacy source line; anonymous GET still returns HTTP 200, with only a browser-side redirect check. | Explicit destination decision ([T009.1](progress/T009.1.md)); preserve client-only guard and no logout ([T010.1](progress/T010.1.md)). No server authorization or browser execution is claimed. | `LoginPageTests`, `AuthenticatedPageTests`, legacy source assertions. |
| New configuration binds `DESCOPE_PROJECT_ID` and optional `Descope:JwksUrl`; new host uses ASP.NET Core logging/configuration and local launch URLs, not legacy `Web.config` or IIS. | New-host `Program.cs`, appsettings and launch profiles; log formatting and actual deployment settings unverified. | `HostIntegrationTests`, validator/API integration tests; deployment not tested. |

## CI, deployment and configuration

- Workflow files changed: **none** (`git diff master...HEAD -- .github/workflows Dockerfile* '*.yml'`). No CI or deployment work performed; if workflows are changed in a later task, Futuristic requires additional confirmation before pushing.
- Runtime prerequisites: provision .NET 10 runtime/ASP.NET Core hosting prerequisites for the new host; building this mixed solution also requires .NET SDK 10, Windows .NET Framework 4.8 build tools and the real WebApplication targets at the runner-specific BuildTools path used by `Directory.Build.props`. The legacy application remains a separate deployment. No IIS/YARP integration performed.
- Configuration names only: `DESCOPE_PROJECT_ID` for real project selection; optional `Descope:JwksUrl` override; `ASPNETCORE_ENVIRONMENT` in launch profiles; ASP.NET Core `Logging`/`AllowedHosts` in new appsettings. Legacy `webpages:Version`, `webpages:Enabled`, `ClientValidationEnabled`, `UnobtrusiveJavaScriptEnabled` remain in legacy configuration. Review production JWKS and host URL settings; never use the test JWKS fixture in production.
- Containers: no Dockerfile or image changes. Launch profiles specify local HTTP port 5228 and HTTPS port 7071; production binding, TLS and network configuration are **not verified**. No health endpoint is documented.

## Risks and follow-ups

| Item | Severity | Recommendation |
|---|---|---|
| Scanner error plus unaudited 32-entry `packages.config`; two open moderate legacy jQuery XSS advisories | Medium; complete risk unknown | Authorize a separately scoped legacy dependency migration/remediation and rerun a scanner able to inventory legacy packages; decide explicitly on open advisories. Do not call this scan clean. |
| JWT expiry/JWKS/503 policy and changed login destination may affect existing clients | High compatibility/security sensitivity | Review intended behavior and client expectations before any traffic migration; capture legacy hosted responses where safe and test production-like settings without secrets in artifacts. |
| Web Forms browser flow and CDN execution, HelpPage rendered equivalence, and legacy hosted HTTP not tested | Medium | Perform browser and side-by-side HTTP acceptance tests in an approved environment; verify that anonymous page serving is acceptable. |
| Build bridge assumes installed Windows BuildTools WebApplication targets at a machine-specific path; MSB3247 persists | Medium | Reproduce full solution gates on target developer/CI images; resolve binding conflict separately without weakening validation. |
| No deployment, proxy, IIS or CI update | Medium | Plan hosting, runtime installation, configuration/secret provisioning and traffic cutover separately; do not treat this branch as an already deployed migration. |

## Open questions

- Should the new host's stricter expiry handling, empty JWKS-fault 503, and login navigation to the authenticated page become the approved client contract? Recommend explicit product/security review before deployment.
- What legacy-browser/HelpPage compatibility is required beyond the tested route/action intent? Recommend capturing real legacy hosted exchanges and browser flows before a cutover.
- Who owns remediation of legacy jQuery and full `packages.config` audit, and which build images will carry the required classic targets? No owner decision recorded.

## How to verify locally

From the solution root on a Windows machine with .NET SDK 10, .NET Framework 4.8 build tools and compatible classic web targets:

```powershell
msbuild -restore -p:RestorePackagesConfig=true -p:Configuration=Release -m -nologo -v:minimal
dotnet test --no-build -c Release
& C:\shared\skills\dependency-vulnerability-remediation\scripts\scan-dependencies.ps1 -Root . -OutputDir .modernization-scratch/scan
dotnet run --no-build --project DescopeSampleApp.Core/DescopeSampleApp.Core.csproj -c Release --launch-profile http
```

The last command starts the independent new host with its local profile (`http://localhost:5228/`); use `/api/sample` without credentials to check an expected 401. A real signed-token flow requires correctly provisioned `DESCOPE_PROJECT_ID` and reachable JWKS; not verified here. The scanner's exit 1 is expected until its error and legacy audit gap are resolved, **not** a clean security result.

## Artifacts

- [Assessment](assessment.md); [approved plan](plan.md); [task notes](progress/T001.md) and [replacement notes](progress/T001.1.md) through [T011.1](progress/T011.1.md).
- Local-only validation evidence (not committed): `.modernization-scratch/logs/final-{build,test,scan}-transcript.txt`, `.modernization-scratch/scan/dotnet-DescopeSampleApp.sln.txt`. The final validator supplied detailed build warning and test totals; transcripts themselves record command and exit code only.
- Review the branch and push it from Futuristic after the completion gate/checkpoint. This report was prepared before the gate; the T011.1 status above applies only if the gate succeeds.
