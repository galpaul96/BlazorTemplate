# Project compliance and production-readiness checklist

**Project:** Blazor (.NET 10, Blazor Web App with Interactive Server, ASP.NET Core Identity, EF Core/SQL Server, .NET Aspire)  
**Baseline review:** 2026-10-11  
**Scope:** Source-level starter-project review. No business domain, deployment environment, data inventory, user geography, operational evidence, or legal/compliance scope was provided.

This is a living engineering checklist, not a certification or legal opinion. A framework feature being present does not establish that it is configured or operated safely. Resolve applicability with the product owner, security/privacy owner, and counsel before claiming compliance.

## Status key

- `[x]` Observed in the repository; this records presence, not verified effectiveness.
- `[~]` Partially present or needs a security/design decision and evidence.
- `[ ]` Not found in this source review, or a future implementation/operational task.
- Use `[N/A — reason]` only after recording who decided and why.

## Initial repository findings

- `[x]` Targets `net10.0`, enables nullable reference types and implicit usings; projects are split into the web app, Aspire AppHost, and shared service defaults.
- `[x]` Uses server-side interactive Blazor, ASP.NET Core Identity, EF Core SQL Server, HTTPS redirection, antiforgery middleware, HSTS outside Development, and built-in Identity passkey/MFA/account-management pages.
- `[x]` Local development uses a LocalDB connection string in `appsettings.json`; no production topology or production connection configuration is present in the reviewed files.
- `[~]` Account confirmation is required, but `IEmailSender<ApplicationUser>` is wired to `IdentityNoOpEmailSender`; confirmation and reset messages cannot be delivered by this implementation.
- `[~]` The account personal-data download endpoint includes the Identity authenticator key in the export. Treat this as a sensitive MFA secret: remove it from export and review whether it was ever exposed in production.
- `[~]` Database developer exception filtering and the migrations endpoint are enabled only in Development; confirm the production migration/release process and ensure the app is never deployed with Development settings.
- `[ ]` No automated test project, CI/CD workflow, deployment manifests, centralized package-version policy, security headers policy, privacy notice, data retention schedule, or production monitoring configuration was found in the source review.

## 1. Scope, governance, and risk

- `[ ]` Name product owner, technical owner, security contact, privacy/DPO contact (where required), incident lead, and release approver.
- `[ ]` Define the app's users, supported countries, business purpose, critical workflows, external integrations, hosting model, availability needs, and trust boundaries.
- `[ ]` Maintain a lightweight threat model: assets, data flows, actors, trust boundaries, abuse cases, mitigations, residual risks, and review date.
- `[ ]` Classify data and services by sensitivity, business impact, confidentiality, integrity, availability, and recovery needs.
- `[ ]` Keep a decision log for accepted risks, compliance applicability, exceptions, owners, expiry dates, and compensating controls.
- `[ ]` Select a verification baseline and assurance level proportional to risk; use OWASP ASVS 5.0.0 as the application-control catalogue and map applicable requirements to evidence.
- `[ ]` Decide whether contract, customer, or sector obligations apply (for example SOC 2, ISO/IEC 27001, PCI DSS, health/financial rules, or government controls); these are not implied by the repository.

## 2. Application and platform security

### Identity, authentication, authorization, and sessions

- `[x]` ASP.NET Core Identity, confirmed-account requirement, MFA/passkey management UI, antiforgery services, and authorized account-management endpoints are present.
- `[~]` Replace the no-op email sender with a production provider, verified sender domain, delivery monitoring, bounce handling, and protected provider credentials; test confirmation, reset, and email-change flows.
- `[ ]` Set explicit password, lockout, unique-email, sign-in, recovery-code, and session/cookie policies against the threat model and current NIST/OWASP guidance; document any reliance on framework defaults.
- `[ ]` Require MFA for privileged and sensitive actions; decide whether to require it for all users based on risk. Verify recovery and factor removal require appropriate re-authentication.
- `[ ]` Review account enumeration, login throttling, credential-stuffing protections, password-reset expiry/replay, confirmation-link expiry/replay, and rate limits across all account endpoints.
- `[ ]` Define roles/policies and least privilege before adding business features; authorize every server-side data operation and resource, not only page/navigation visibility.
- `[ ]` Review external identity providers, allowed callback URLs, issuer/audience validation, provider linking, unlinking, and account-takeover scenarios before enabling providers.
- `[ ]` Set and verify secure, HttpOnly, appropriate SameSite, bounded-lifetime authentication cookies; define idle/absolute session expiry, sign-out, credential-change revocation, and concurrent-session policy.
- `[ ]` Review WebAuthn/passkey relying-party ID, origin, HTTPS, authenticator lifecycle, and account recovery in every deployed hostname.
- `[ ]` Remove the authenticator key from `/Account/Manage/DownloadPersonalData`; assess exposure history and rotate/reset affected MFA factors if it has been accessible outside local development.
- `[ ]` Ensure recovery codes, authenticator seeds, reset tokens, password material, session identifiers, and full identity claims never enter logs, analytics, exports, or support tooling.

### Web and API defenses

- `[x]` HTTPS redirection, non-Development HSTS, and antiforgery middleware are configured in the request pipeline.
- `[ ]` Set a production HSTS policy after confirming TLS termination and all subdomains; configure secure transport at the load balancer/proxy and trust forwarded headers only from known proxies.
- `[ ]` Define and validate response headers: Content-Security-Policy (prefer nonce/hash-based script policy compatible with Blazor), frame protections, `X-Content-Type-Options`, `Referrer-Policy`, and an appropriate Permissions-Policy.
- `[ ]` Review CSP and third-party scripts/styles; self-host and pin assets where practical, and use integrity metadata where supported for externally loaded resources.
- `[ ]` Verify every state-changing endpoint uses antiforgery protection appropriate to its transport; test login, logout, passkeys, account changes, and future APIs.
- `[ ]` Validate all client input on the server; use parameterized EF Core queries, explicit output encoding, upload limits/content inspection, and safe URL/redirect allowlists.
- `[ ]` Check authorization against object ownership for every record lookup/update/delete to prevent IDOR/BOLA; avoid over-posting and mass assignment with narrow request models.
- `[ ]` Add request/body limits, concurrency limits, timeouts, throttling, and resource quotas appropriate to server-side circuits and expensive endpoints; assess denial-of-service and connection exhaustion.
- `[ ]` Ensure errors return safe, correlation-ID-bearing responses without stack traces, SQL, secrets, or personal data; retain detailed diagnostics only in access-controlled telemetry.
- `[ ]` Protect circuit state and sensitive UI from cross-user leakage; ensure scoped services and caches never share user-specific state across circuits.
- `[ ]` Review CORS, WebSocket origin validation, proxy configuration, and public endpoint exposure if APIs, custom origins, or reverse proxies are introduced.
- `[ ]` Restrict production health/readiness endpoints to the information and networks required by the platform; current Aspire defaults expose them only in Development.
- `[ ]` Review file downloads and generated exports for authorization, cache controls, safe filenames, content types, size limits, and temporary-file cleanup.

### Secrets, cryptography, and configuration

- `[ ]` Keep production credentials out of source, committed settings, images, logs, and browser-delivered code; use a managed secret store/identity and least-privilege access.
- `[ ]` Use .NET User Secrets only for local development. Scan repository history and build artifacts for accidentally committed secrets; rotate any exposed credentials.
- `[ ]` Persist ASP.NET Core Data Protection keys securely in production; protect keys at rest, restrict access, set application isolation deliberately, and share keys across replicas that must read the same cookies/tokens.
- `[ ]` Configure TLS versions/ciphers at the hosting edge, encryption at rest for database/backups/storage, and key/certificate issuance, rotation, expiry alerts, and revocation.
- `[ ]` Document cryptographic purposes and retention; use platform cryptography rather than custom algorithms and avoid inventing encryption schemes.
- `[ ]` Validate production configuration at startup, fail closed on missing/unsafe settings, and keep Development-only diagnostics and test credentials out of production.

## 3. Privacy, personal data, and records

Applicability note: GDPR applies when the relevant territorial/material conditions are met; this source review cannot decide that. Map each control to the actual controller/processor roles, purposes, data subjects, and jurisdictions.

- `[ ]` Inventory personal and sensitive data, including account identifiers, emails, IP/device data, cookies, logs, telemetry, support exports, backups, and third-party disclosures.
- `[ ]` Document processing purposes and lawful basis, data categories, data-subject categories, recipients/processors, locations/transfers, retention periods, and deletion paths in a record of processing where required.
- `[ ]` Minimize collection and default visibility; do not collect optional profile or telemetry data without a defined purpose and retention period.
- `[ ]` Provide a clear privacy notice at collection, with controller/contact details, purposes/legal bases, recipients, retention, rights, complaints, and international transfer information as applicable.
- `[ ]` Implement workflows to locate, export, correct, restrict, object to, and erase personal data; verify identity, scope across derived systems/backups, and record completion.
- `[~]` Identity scaffolding includes personal-data download and delete-account screens; audit correctness, completeness, security, confirmation, downstream-system handling, and safe treatment of credentials/secrets before relying on them.
- `[ ]` Define retention/deletion schedules for account data, security logs, email events, telemetry, backups, and temporary exports; automate deletion and document backup expiry behavior.
- `[ ]` Maintain processor/vendor inventory and agreements, subprocessor review, transfer safeguards, data-location decisions, and access/security review.
- `[ ]` Decide and implement cookie/online tracking consent and preferences where required; classify essential vs. optional technologies and ensure consent is recorded and respected.
- `[ ]` Conduct and document a privacy impact/DPIA screening; complete a DPIA before processing likely to create high risk where legally required.
- `[ ]` Define personal-data breach triage, evidence preservation, controller/processor escalation, regulator notification timelines, and affected-person communications where required.
- `[ ]` Ensure logs, traces, metrics, and exception details are data-minimized, access-controlled, retained for a defined period, and scrubbed of secrets and unnecessary identifiers.
- `[ ]` Review the personal-data download endpoint: do not export authenticator secrets; prevent caching; avoid logging unnecessary user identifiers; test rights handling end to end.

## 4. Data, database, and resilience

- `[x]` EF Core SQL Server and an initial ASP.NET Identity schema migration are present; development configuration uses LocalDB.
- `[ ]` Define production database hosting, network boundaries/private access, identity-based authentication where available, least-privilege database grants, connection encryption, and secret rotation.
- `[ ]` Use reviewed, versioned EF migrations in a controlled deployment step; do not run unrestricted schema migrations on every app startup in production.
- `[ ]` Define data ownership, schema constraints, tenant isolation (if applicable), transaction boundaries, concurrency handling, and audit requirements before domain entities are added.
- `[ ]` Apply retention/minimization to database and derived data; decide whether field-level encryption/tokenization is needed for especially sensitive attributes.
- `[ ]` Establish encrypted backups, point-in-time recovery, restore drills, RPO/RTO targets, retention, access control, and deletion/expiry of backup media.
- `[ ]` Define availability/failover and disaster-recovery topology; document single points of failure and database capacity/connection-pool limits.
- `[ ]` Test migration rollback/forward-fix strategy and compatibility between old/new app instances during rolling releases.
- `[ ]` Restrict database administrator access, audit privileged activity, alert on anomalous access, and separate production from developer/test data.
- `[ ]` Use synthetic or anonymized data in development and test; prohibit production data copies unless approved, minimized, time-bounded, and protected.

## 5. Architecture, topology, and operational readiness

- `[x]` Aspire AppHost currently launches one web project; the service-defaults library configures service discovery, standard outbound HTTP resilience, health checks, and OpenTelemetry instrumentation/export when an OTLP endpoint exists.
- `[ ]` Publish a deployment/context diagram showing browser, edge/WAF/reverse proxy, app instances, SQL Server, identity/email providers, telemetry, secret store, backup system, and trust/network boundaries.
- `[ ]` Decide hosting platform, region/data residency, ingress/egress paths, public/private endpoints, DNS, TLS termination, firewall rules, workload identity, and scaling model.
- `[ ]` For multi-instance server-side Blazor, verify Data Protection key sharing, circuit/WebSocket affinity or supported scale-out, graceful reconnect behavior, memory/circuit limits, and shared dependencies.
- `[ ]` Configure production OTLP destination, access controls, TLS, sampling, data retention, redaction, alerting, dashboards, and ownership; verify telemetry does not carry secrets or sensitive payloads.
- `[ ]` Define service health semantics: readiness checks required dependencies; liveness checks only process health; restrict diagnostics endpoints and avoid leaking dependency details.
- `[ ]` Set SLOs, capacity limits, queue/retry behavior, timeout budgets, graceful degradation, overload behavior, and operational alerts for critical user journeys.
- `[ ]` Document runbooks for deploy/rollback, database restore, key rotation, email outage, identity lockout, suspected compromise, privacy incident, and service degradation.
- `[ ]` Define environment separation, production access approvals, just-in-time administration, break-glass procedures, and audit logging.
- `[ ]` Confirm graceful shutdown, rolling deploys, background work ownership/idempotency, and recovery after process/node failure as the app grows.

## 6. Secure software supply chain and engineering structure

- `[x]` The solution separates web, AppHost, and shared defaults, with nullable reference types enabled in the web and shared defaults projects.
- `[ ]` Add a test project and CI checks for restore/build, unit/integration tests, authorization, antiforgery, account recovery, migrations, and critical business invariants before production release.
- `[ ]` Add a supported SDK pin (`global.json`) and centralized package version management; define a routine for .NET servicing and NuGet dependency updates.
- `[ ]` Run dependency vulnerability/license review on direct and transitive NuGet packages; fail or create tracked remediation for exploitable advisories.
- `[ ]` Generate an SBOM for release artifacts; verify provenance, build inputs, dependency sources, artifact integrity, and signing/attestation where the delivery platform supports it.
- `[ ]` Add secret scanning, static analysis, code review, formatting/analyzer rules, and infrastructure/configuration scanning to CI; protect default branches and release credentials.
- `[ ]` Build reproducibly in CI from reviewed source; publish immutable artifacts, separate build from deploy, and restrict deployment permissions and environment approvals.
- `[ ]` Add dynamic security testing and targeted penetration testing before public launch and after material authentication/data-flow changes; track findings to closure.
- `[ ]` Define vulnerability intake, severity/triage SLAs, patch windows, supported dependency policy, end-of-life response, and security contact/reporting process.
- `[ ]` Add architecture boundaries as features appear (UI/presentation, application/use cases, domain, infrastructure); keep business authorization and data rules out of presentation-only checks.
- `[ ]` Keep configuration, migrations, deployment infrastructure, and operational docs version-controlled; review the actual deployment pipeline because none was found in this source scan.
- `[ ]` Define code/data ownership and review requirements for authentication, authorization, cryptography, migrations, secret handling, and infrastructure changes.

## 7. Accessibility, usability, and content

- `[ ]` Set the target standard based on product/contract/legal scope; use WCAG 2.2 AA as the engineering baseline unless an applicable law or contract specifies otherwise.
- `[ ]` Test keyboard-only operation, visible focus, logical focus order, skip navigation, focus restoration after navigation/dialogs, and no keyboard traps.
- `[ ]` Test screen-reader names/roles/states, headings/landmarks, form labels and validation announcements, status messages, and authentication/recovery flows.
- `[ ]` Verify contrast, text resizing/reflow, zoom, pointer target sizes, reduced motion, captions/transcripts for media, and accessible error handling.
- `[ ]` Test with automated scans plus manual keyboard and screen-reader checks; keep an accessibility issue log and regression checks.
- `[ ]` Review language metadata/localization, date/number formats, plain-language privacy/security notices, and accessible export formats.

## 8. Compliance applicability decisions to record

For each item, record **Applicable / Not applicable / Undetermined**, rationale, decision owner, date, obligations, evidence location, and next review date.

- `[ ]` GDPR and national data-protection/ePrivacy rules based on establishment, targeting, monitoring, data, and tracking technologies.
- `[ ]` EU accessibility requirements (including the European Accessibility Act) based on service type, market, provider size/exemptions, and applicable national implementation.
- `[ ]` NIS2 / national cybersecurity requirements based on entity and sector scope; do not infer applicability from software alone.
- `[ ]` EU Cyber Resilience Act / product cybersecurity obligations if software is placed on the market as an in-scope product with digital elements; determine role, product category, and transition dates.
- `[ ]` PCI DSS only if payment-card data or cardholder-data environments enter scope; prefer hosted/tokenized payment flows that keep the app out of scope.
- `[ ]` AI Act, sector-specific rules, data-localization/transfer requirements, customer contracts, and records-retention rules if the product's features or customers trigger them.
- `[ ]` Organizational assurance commitments such as ISO/IEC 27001 or SOC 2, if required by customers, contracts, or company policy; these require organizational/process evidence beyond this codebase.

## Suggested completion order

1. Resolve the authenticator-secret export and implement a real email delivery path.
2. Decide user/data scope, production hosting, secrets/Data Protection key storage, database migration and backup strategy, and privacy obligations.
3. Define authorization, account/session policy, abuse protections, error/logging behavior, and production headers.
4. Establish CI, tests, dependency/secret scanning, vulnerability handling, deployment controls, monitoring, and recovery evidence.
5. Complete accessibility and jurisdiction/sector applicability reviews; retain evidence for each checked item.

## Reference standards and primary guidance

- [OWASP Application Security Verification Standard (ASVS), stable 5.0.0](https://github.com/OWASP/ASVS/releases)
- [Microsoft: ASP.NET Core security](https://learn.microsoft.com/aspnet/core/security/)
- [Microsoft: Blazor security](https://learn.microsoft.com/aspnet/core/blazor/security/)
- [Microsoft: Securely maintain sensitive data and credentials](https://learn.microsoft.com/aspnet/core/security/app-secrets)
- [Microsoft: ASP.NET Core Data Protection](https://learn.microsoft.com/aspnet/core/security/data-protection/introduction)
- [Microsoft: Host and deploy server-side Blazor apps](https://learn.microsoft.com/aspnet/core/blazor/host-and-deploy/server/)
- [Microsoft .NET and .NET Core support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- [EU General Data Protection Regulation, Regulation (EU) 2016/679](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng)
- [W3C Web Content Accessibility Guidelines (WCAG) 2.2](https://www.w3.org/TR/WCAG22/)
- [NIST SP 800-218, Secure Software Development Framework](https://csrc.nist.gov/pubs/sp/800/218/final)
- [European Commission: NIS2 Directive](https://digital-strategy.ec.europa.eu/en/policies/nis2-directive)
- [European Commission: Cyber Resilience Act](https://digital-strategy.ec.europa.eu/en/policies/cyber-resilience-act)
- [European Commission: European Accessibility Act](https://ec.europa.eu/social/main.jsp?catId=1202)
- [PCI Security Standards Council: PCI DSS](https://www.pcisecuritystandards.org/standards/pci-dss/)

Review these references and applicability decisions at least annually, and after material changes to user geography, data processing, authentication, integrations, hosting, or legal requirements.
