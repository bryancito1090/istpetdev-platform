# Issue 112 Authorization Implementation Plan

> **For agentic workers:** Implement these steps in this session, in order, with test failures observed before production edits.

**Goal:** Let Auditor read simulations while keeping every simulation write forbidden, and establish an HTTP permission check that does not conflict with issue #128.

**Architecture:** Keep role checks at controller actions. Extend the existing simulation unit test and API integration test. Preserve field permissions until Bryan and Joseph decide whether Gerencia's field access is an ADR-24 exception; issue #128 explicitly uses that access.

**Tech Stack:** ASP.NET Core 8, xUnit, Testcontainers/PostGIS, Angular/Vitest.

---

### Task 1: Simulation permissions

**Files:** `tests/IstpetDev.Tests/Unit/Simulation/SimulationApiTests.cs`, `src/Api/Controllers/SimulationsController.cs`.

- [x] Change the attribute test to expect `Roles.Planning + "," + Roles.Auditor` for `ListComparisons`, `GetComparison`, and `ExportComparison`, and `Roles.Planning` for `CreateComparison` and `ImportComparison`. Assert Auditor is absent from both writes.
- [x] Run the focused `SimulationApiTests` test and observe the expected permission failure.
- [x] Add Auditor to the three GET attributes in the controller.
- [x] Run the focused `SimulationApiTests` suite and confirm the test passes.

### Task 2: HTTP authorization coverage

**Files:** `tests/IstpetDev.Tests/Integration/Api/AuditorRoleTests.cs`.

- [x] Extend the Auditor HTTP test with list, detail, and export routes; list expects 200, and missing detail/export resources expect 404 after authorization. Add create/import to the 403 write matrix.
- [x] Add an HTTP role matrix test for simulations with a token per human and technical role. Field write expectations for Gerencia remain open pending the joint policy decision.
- [x] Run the focused `AuditorRoleTests`, `SimulationApiTests`, and `AuthorizationSafetyTests` suites through VSTest/PostGIS: 19 passed.

### Task 3: Field policy coordination

**Files:** `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md` only after the joint decision.

- [x] Compare issue #112 with issue #128's new Receiver endpoint. Record the unresolved policy in the final report. Do not remove Gerencia from the field controllers or document a newly approved exception without Bryan and Joseph's joint decision.

### Task 4: Final verification

- [x] Run the focused unit and integration tests, `git diff --check`, and inspect the final diff.
- [x] Report exactly which #112 criteria pass and which remain pending the joint field permission decision.
