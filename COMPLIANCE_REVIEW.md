# Security, privacy and compliance review

**Assessment date:** 13 October 2026  
**Scope:** Publicly exposed application serving general EU users; no identified regulated sector or specifically targeted regulated audience. Reviewed the Blazor API, web application, gateway, persistence libraries, Aspire AppHost, configuration, account/admin flows and repository deployment artifacts.

## 1. Executive assessment

**Resolve the account-security findings before a public production launch.** The most urgent issues are the reusable seeded administrator, publicly generated account-confirmation links, inclusion of the MFA authenticator secret in personal-data exports, and insufficient freshness checks around privileged operations.

The shared-secret approach can remain appropriate for gateway-to-API authentication. A dedicated identity service is not required merely because multiple web replicas call the API. However, a shared secret identifies a trusted service; it does not establish the end user's permissions or ownership of a record. Production transport, secret rotation, network isolation and user authorization must complete that boundary.

GDPR and applicable national implementations of ePrivacy are the principal privacy baseline. Additional obligations depend on the actual service, operator, contracts and deployment. The frameworks below are a researched selection of relevant current requirements and guidance, not a claim that every law or certification applies.

### Method and limits

- Source and configuration review, plus current primary-source research. Findings describe the reviewed workspace, not a deployed production environment.
- No live penetration test, browser accessibility audit, restore exercise or production infrastructure inspection was performed. Recommended acceptance checks below have not been executed.
- A read-only NuGet check completed successfully: `dotnet list src/Blazor/Blazor.slnx package --vulnerable --include-transitive --no-restore --format json`. It reported no known vulnerable packages for the ten solution projects against the NuGet.org source. This covers the currently restored dependency graph; it is not proof of absence of vulnerabilities or coverage of containers, JavaScript/CDN assets and projects outside the solution.
- Legal applicability still needs the operator's establishment, service/business model, actual data categories, provider locations and contracts. This document is an engineering assessment, not legal certification.
- No application remediation was performed as part of this report.

## 2. Existing controls and architecture

| Area | Observed status |
| --- | --- |
| API authentication | Custom API-key handler, constant-time comparison for equal-length keys, explicit gateway policy applied to controller endpoints, startup rejection of an empty secret. |
| Gateway | Server-side named HTTP client adds the API key; automatic redirects are disabled. |
| Configuration | `IConfiguration` correctly reads environment variables. `ApiAuthentication__SharedSecret` maps to `ApiAuthentication:SharedSecret`; direct calls to `Environment.GetEnvironmentVariable` are unnecessary. |
| Resource names | AppHost references align with `api-db`, `mongo-db`, `web-db` and `redis-cache`. |
| Account security | ASP.NET Core Identity, confirmed-account requirement, login lockout enabled, TOTP MFA functionality and admin page role restrictions are present. |
| HTTP/UI | HTTPS redirection, production web HSTS, antiforgery middleware, development-only detailed errors/Swagger and standard authorization-aware routing are present. |
| Dependencies | NuGet audit did not report known vulnerabilities in the restored solution graph. |
| Frontend assets | Bootstrap CDN assets have integrity metadata. This helps integrity, but does not settle third-party data-processing questions. |

The web application hosts interactive server components and calls the gateway library; the gateway is not an independently deployed security boundary in this design. Identity data is in PostgreSQL `web-db`; the API references PostgreSQL `api-db` and MongoDB `mongo-db`; Redis is configured for web output caching. No active API controllers were found to assess business-object authorization. The absence of API EF migrations is accepted for the current stage, as requested.

## 3. Prioritized technical findings

**Priority meaning:** P0 = resolve before public launch; P1 = complete before production use of the affected feature; P2 = planned hardening/governance. Priority reflects release risk, not a CVSS score. “Not evidenced” means the repository does not show a control; deployment may provide it.

### F01 — P0: Remove the reusable privileged bootstrap account

**Confirmed.** [SeedAdminAccount migration](src/Blazor/WebServices/Blazon.Web/Migrations/20261012160638_SeedAdminAccount.cs) inserts a fixed administrator identity with a fixed password hash, confirmed email and MFA disabled. [Web startup](src/Blazor/WebServices/Blazon.Web/Program.cs) runs migrations without restricting them to development. Each database receiving the migration consequently receives the same privileged credential material. The plaintext password was not established in this review.

**Change:** Bootstrap administrators using a deployment-specific invitation or one-time credential, then require MFA. If this migration has already been deployed, disable/rotate that account and revoke its sessions using a controlled corrective migration or administrative procedure. Rewriting an already applied migration is insufficient. Avoid fixed password hashes or reusable production bootstrap secrets in source.

**Acceptance:** A fresh production database has no predictable administrator credential; bootstrap cannot be reused; existing seeded accounts are remediated.

### F02 — P0: Account confirmation currently bypasses mailbox ownership

**Confirmed.** [RegisterConfirmation.razor](src/Blazor/WebServices/Blazon.Web/Components/Account/Pages/RegisterConfirmation.razor) accepts an email query parameter and generates a confirmation link when the configured sender is `IdentityNoOpEmailSender`. [Program.cs](src/Blazor/WebServices/Blazon.Web/Program.cs) registers that sender without a development guard. A visitor can obtain a confirmation token without accessing the mailbox. Different responses also disclose account existence. This is not, by itself, proof that an arbitrary existing account's password can be taken over.

**Change:** Configure a real transactional email provider; remove publicly displayed confirmation tokens from production; fail startup if production uses the no-op sender. Use generic registration/recovery responses and rate limits. Verify reset, confirmation and email-change flows end to end.

**Acceptance:** Anonymous callers never receive confirmation/reset tokens in pages or logs; account activation requires the intended mailbox; recovery messages are actually delivered.

### F03 — P0: Personal-data export exposes the MFA authenticator secret

**Confirmed.** [IdentityComponentsEndpointRouteBuilderExtensions.cs](src/Blazor/WebServices/Blazon.Web/Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs) explicitly adds `Authenticator Key` to the authenticated export. Possession of this seed enables generation of the user's TOTP codes. Authentication is required, but there is no fresh-authentication requirement in this export handler.

**Change:** Exclude authentication secrets, recovery codes, password hashes and security stamps from exports. Use an explicit export DTO rather than unconstrained reflection as the schema grows. Require recent authentication for sensitive downloads and return `Cache-Control: no-store`. Assess historical exposure if this behavior has been used in production.

**Acceptance:** An export contains the intended personal data and no credential material; sensitive download responses are not cached.

### F04 — P0: Privileged actions need current authorization, step-up and audit

**Confirmed design gap.** Admin pages are role protected, but [IdentityAdminService.cs](src/Blazor/WebServices/Blazon.Web/Components/Admin/IdentityAdminService.cs) has no actor-aware authorization/audit boundary around its mutation methods. It can remove or rename the Admin role, remove membership, alter claims, confirm account details, reset passwords and disable MFA without safeguards for the last administrator or recent authentication.

**Change:** Check the current actor's authorization on every sensitive service operation; require recent MFA for credential/MFA/privilege changes. Protect the final active administrator, constrain editable security claims and record actor, target, action, outcome, time and correlation ID. Do not record passwords or authentication secrets. Notify users of relevant security changes. Make dependent changes transactional where appropriate.

**Acceptance:** A stale circuit or revoked admin cannot perform privileged mutations; the final administrator cannot be accidentally removed; sensitive actions produce protected audit events.

### F05 — P0: Role removal and account disabling do not promptly revoke access

**Confirmed.** [IdentityAdminService.cs](src/Blazor/WebServices/Blazon.Web/Components/Admin/IdentityAdminService.cs) changes roles/claims and lockout state without consistently rotating security stamps. [IdentityRevalidatingAuthenticationStateProvider.cs](src/Blazor/WebServices/Blazon.Web/Components/Account/IdentityRevalidatingAuthenticationStateProvider.cs) checks the stamp at 30-minute intervals, without independently checking current roles or lockout. The explicit revoke action updates the stamp, but the UI message overstates immediacy for existing circuits.

**Change:** Define a revocation-latency requirement. Invalidate sessions on security-sensitive changes, reauthorize privileged operations against current state and ensure disabled users are rejected. Consider server-side session tracking or circuit disconnection for immediate revocation. Align cookie validation and Blazor circuit behavior.

**Acceptance:** Exercise role removal, lockout, MFA reset and session revocation with an already open admin session; access stops within the documented interval.

### F06 — P1: Gateway accepts arbitrary destinations while attaching the API secret

**Confirmed latent risk; no externally controlled live call path established.** [GatewayClient.cs](src/Blazor/WebServices/Blazor.Web.Gateway/ExternalApis/GatewayClient.cs) accepts a caller-provided base URL and route. URI composition permits a different origin, while the named client carries the secret. Existing example service calls are commented out.

**Change:** Configure a fixed trusted API origin, accept relative routes only and validate the final scheme/authority before attaching credentials. Require HTTPS in production and restrict outbound destinations. Preserve disabled redirects. Do not expose URL selection to browser input.

**Acceptance:** Absolute routes, alternate hosts, HTTP production URLs and redirect attempts cannot disclose the secret. Test before enabling business calls.

### F07 — P1: Complete production secret and network management

**Confirmed development material; production controls not evidenced.** AppHost development configuration contains a known test secret. API/web appsettings contain intentionally requested example connection strings, including example PostgreSQL credentials. These are not established production leaks, but must not become production defaults.

**Change:** Inject independent per-environment secrets through the deployment secret store; reject known sample/empty values in production. Restrict API, databases, Redis, dashboards and telemetry endpoints to intended networks. Use TLS and authenticated database/cache connections with least-privileged users. Introduce a key ID and bounded dual-key rotation window if retaining API keys; audit callers and rotation. A shared key across replicas is workable but cannot distinguish individual replicas.

**Acceptance:** Only the public web ingress is reachable externally; the API rejects missing/wrong keys; deployment contains no test credentials; a rotation succeeds without exposing keys or prolonged downtime.

### F08 — P1: Add resource limits and safe retry behavior

**Not evidenced:** Application-level rate limits, bounded pagination and explicit request/circuit resource budgets. Login lockout is present, but does not cover registration, recovery, API traffic or expensive authenticated actions. Mongo repository enumeration is unbounded.

**Confirmed:** [ServiceDefaults](src/Blazor/Common/Blazor.ServiceDefaults/Extensions.cs) enables the standard HTTP resilience handler. Its default retry behavior includes unsafe methods, so future mutations can be duplicated. See [Microsoft HTTP resilience guidance](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience).

**Change:** Apply edge and application limits by appropriate IP/account/service identities; account for multiple replicas. Bound request bodies, query results, concurrent jobs, circuit messages and expensive component actions. Disable retries for unsafe methods unless operations have enforced idempotency keys/deduplication.

**Acceptance:** Repeated requests and circuit actions stay within resource budgets; simulated transient failures do not duplicate a mutation.

### F09 — P1: Correct interactive-server database lifetimes and migration execution

**Confirmed.** [IdentityAdminService.cs](src/Blazor/WebServices/Blazon.Web/Components/Admin/IdentityAdminService.cs) uses a scoped `ApplicationDbContext` in interactive server components. Circuit scopes can outlive individual operations and allow concurrent use of the same context. Web/API startup also performs schema migration work during service configuration.

**Change:** Use a context factory or a fresh operation scope for database work, including compatible Identity manager/store lifetimes. Avoid shared tracked entities across circuit operations. Run production schema changes through a controlled migration job; give runtime users only required DML permissions. Dispose manually created contexts and define rollout/rollback procedures. This does not require adding API migrations before the API has a schema to migrate. See [Microsoft Blazor EF guidance](https://learn.microsoft.com/en-us/aspnet/core/blazor/blazor-ef-core?view=aspnetcore-10.0).

**Acceptance:** Concurrent admin actions do not reuse a context unsafely; ordinary replicas can start without schema-owner credentials or racing migration jobs.

### F10 — P1: Data rights need application-wide export and erasure

**Confirmed.** [DeletePersonalData.razor](src/Blazor/WebServices/Blazon.Web/Components/Account/Pages/Manage/DeletePersonalData.razor) deletes the Identity user only. [EF repository](src/Blazor/ApiServices/Blazor.Api.Ef/Repository.cs) and [Mongo repository](src/Blazor/Services/Blazor.MongoDb/Repository/Repository.cs) implement soft deletion. Neither establishes erasure across future application data, caches, exports, logs and backups. The current export covers Identity data, not every store. The deletion UI's permanent-removal wording is broader than the demonstrated implementation.

**Change:** Maintain an ownership/data inventory and orchestrate export, restriction, retention and deletion across actual stores. Add scheduled purge/anonymization with documented exceptions and backup expiry/recovery handling. Require fresh proof for deletion, including accounts without a local password. Use accurate UI language and record request completion without retaining the deleted content unnecessarily.

**Acceptance:** A sample account can be traced through every relevant store; deletion and export outcomes match documented policies. Soft deletion alone is never treated as completed erasure.

### F11 — P1: Production replicas need key persistence and circuit planning

**Not evidenced.** Shared persistent ASP.NET Core Data Protection keys, application-name isolation, key encryption at rest and replica affinity are not configured in the repository. Hosting may supply some of this. Redis output caching does not automatically share cookies, key rings or live Blazor circuit state.

**Change:** Configure supported shared key storage with access restrictions and encryption, consistent application identity per deployment and isolation across environments. Plan session affinity/circuit reconnection and deployment draining. Keep authenticated/personalized responses out of shared output caches unless deliberate safe partitioning is implemented. No active output-cache policies were found, so caching all user pages is not an established current defect.

**Acceptance:** Restart and replica routing preserve intended cookies without cross-environment acceptance; one user cannot receive another user's cached content.

### F12 — P1: Repair deployment and test artifacts

**Confirmed.** The [API Dockerfile](src/Blazor/ApiServices/Blazor.Api/Dockerfile) and [web Dockerfile](src/Blazor/WebServices/Blazon.Web/Dockerfile) reference .NET 8 and old `ConnectX.*` paths/assemblies, while the application targets .NET 10. [ConnectX.Tests.csproj](src/Blazor/Blazor.Tests/ConnectX.Tests.csproj) and [WebTests.cs](src/Blazor/Blazor.Tests/WebTests.cs) retain an obsolete AppHost/resource name and are outside the current solution.

**Change:** Align images, project paths and entry points with the current solution; use supported patched images, non-root execution and image scanning. Repair and include the test project in CI. Establish locked/reproducible dependency inputs, vulnerability checks, secret scanning, an SBOM and dependency update ownership. No working CI pipeline was evidenced.

**Acceptance:** Clean container builds and deployment smoke checks work; CI verifies critical authentication/authorization and privacy flows against the actual resource names.

### F13 — P1: Establish production health, backup and incident evidence

**Confirmed mismatch:** [ServiceDefaults](src/Blazor/Common/Blazor.ServiceDefaults/Extensions.cs) maps health endpoints only in development, while AppHost declares `/health` checks. **Not evidenced:** Production readiness configuration, database restore procedures, durable volume strategy, recovery objectives, security-alert routing or retention policies.

**Change:** Expose minimal liveness/readiness endpoints through a private or otherwise appropriately controlled route; include relevant dependency checks without disclosing secrets. Define RPO/RTO, encrypted backups, restore exercises, on-call ownership and incident runbooks. Avoid putting expensive dependency checks in liveness probes that can cause restart loops.

**Acceptance:** Production probes work as deployed; a restore meets documented recovery objectives; failed logins, privileged changes and abnormal API-key failures produce actionable alerts.

### F14 — P1: Finish HTTP boundary and telemetry controls

**Not evidenced:** Production CSP, explicit referrer/permissions policies, reverse-proxy trust configuration and a telemetry redaction policy. `AllowedHosts` is unrestricted in sample settings. Existing HTTPS redirection/HSTS are positive but do not prove ingress-to-service encryption or trusted forwarding.

**Change:** Set approved hosts and trusted proxies/networks for the actual hosting topology. Configure HTTPS forwarding correctly; deploy a CSP compatible with Blazor, initially observing reports where necessary. Review clickjacking, MIME-sniffing, referrer and browser feature restrictions. Exclude credentials, API keys, tokens, sensitive bodies and unnecessary identifiers from logs/traces. Protect telemetry access and define retention. Do not assume the present telemetry configuration already logs secrets; inspect emitted records.

**Acceptance:** Header checks and representative traces demonstrate the policy; spoofed forwarding headers cannot change security decisions.

### F15 — P1/P2: Fix authentication UX and accessibility gaps

**Confirmed source issues:** [Login.razor](src/Blazor/WebServices/Blazon.Web/Components/Account/Pages/Login.razor) has label `for` values without matching explicit input IDs; [Claims.razor](src/Blazor/WebServices/Blazon.Web/Components/Admin/Claims.razor) includes placeholder-only fields. Rendered behavior still needs inspection. Language metadata and focus-on-navigation support are already present.

**Change:** Add stable label associations and accessible names; check keyboard operation, focus visibility/order, error announcements, contrast, zoom/reflow and target sizes. Adopt WCAG 2.2 AA as the engineering target. Combine automated checks with manual keyboard and screen-reader review, especially login, MFA, recovery and admin dialogs.

**Acceptance:** Core journeys pass the selected criteria with recorded manual evidence. Legal accessibility scope is assessed separately in section 5.

### F16 — P1/P2: Strengthen credential policy and administrative reset semantics

**Confirmed.** Registration permits a six-character minimum at the model level; there is no demonstrated breached-password screening or mandatory admin MFA. `SetTemporaryPasswordAsync` in the admin service creates a usable password but has no enforced expiry or mandatory change; it also trims the provided password.

**Change:** Prefer short-lived reset invitations over passwords chosen by administrators. Do not silently normalize password whitespace. Configure server-side policy consistently and require MFA for admins. NIST's current guidance recommends a 15-character minimum for single-factor passwords, support for at least 64 characters, compromised/common-password screening and no arbitrary composition or periodic-change rules absent compromise. This is a security reference, not an automatic EU legal requirement. See [NIST SP 800-63B-4](https://pages.nist.gov/800-63-4/sp800-63b.html).

**Acceptance:** UI and server validation agree; passwords retain their entered meaning; administrative resets expire and cannot bypass required account security.

### F17 — P2: Align persistence options, licensing and existing documentation

- Mongo selects the database using a fixed `mongo-db` name. Ensure this is intentional in deployment or derive a validated database name from configuration; changing the URI's database alone currently does not change that selection.
- Review design-time EF connection defaults so tooling cannot silently target an unintended local database. The current runtime connection-string names are aligned.
- AutoMapper 16.2 is referenced. Verify entitlement and required configuration under the licensing introduced in version 15; this review does not establish a licensing violation. See the [official upgrade guide](https://docs.automapper.io/en/stable/15.0-Upgrade-Guide.html).
- Remove unused provider/dependency references where practical; retain third-party notices and a license inventory.
- Update the older [COMPLIANCE_CHECKLIST.md](src/Blazor/COMPLIANCE_CHECKLIST.md): its SQL Server/LocalDB, passkey and test-status descriptions do not accurately describe this workspace. Current persistence is PostgreSQL; TOTP exists, but passkey implementation was not evidenced; a stale test project exists.

## 4. EU privacy implementation checklist

GDPR requires a documented lawful basis, minimization, retention, transparency, appropriate security, processor arrangements and applicable rights handling. DPIA and DPO requirements are conditional; neither is automatically mandatory for every public application. Transfers outside the EEA require an appropriate mechanism where applicable; GDPR does not universally mandate EU-only hosting. Establish these decisions against the [GDPR text](https://eur-lex.europa.eu/eli/reg/2016/679/oj/eng/).

| Deliverable | Required project work/evidence | Suggested owner |
| --- | --- | --- |
| Processing inventory | Record purposes, data, lawful basis, recipients, storage locations, retention and controller/processor roles. Include ongoing account management and telemetry. | Product/privacy |
| Privacy notice | Publish controller identity/contact, purposes/bases, recipients/transfers, retention, rights and complaint route. Match actual processing. | Privacy/product |
| Rights workflow | Identity verification, request intake, tracking, export/correction/restriction/deletion and conditional portability. Generally respond within one month; document lawful extensions/exceptions. | Privacy/backend |
| Retention schedule | Separate active/deleted accounts, business records, authentication events, audit events, telemetry, caches and backups. Implement and monitor cleanup. | Privacy/backend/operations |
| Processor and transfer register | Review hosting, email, monitoring and support vendors; agreements, subprocessors, regions and transfer safeguards. Include CDN requests in the data-flow assessment. | Privacy/procurement |
| Cookies/storage inventory | Identify cookies, local storage and third-party tags, purposes and lifetimes. Obtain prior consent for nonessential tracking where required; make withdrawal accessible. Strictly necessary authentication/antiforgery storage may be exempt. | Frontend/privacy |
| Incident process | Record personal-data breaches; assess risk and notification duties. Controller notification to the authority is generally within 72 hours of awareness where required; high-risk cases also require notification to affected people without undue delay. | Security/privacy |
| Accountability | Assign owners; record access reviews, security training, risk assessments and vendor reviews. Assess whether a DPIA, DPO or EU representative is required. | Management/privacy |

Sources: [EDPB individual-rights guidance](https://www.edpb.europa.eu/sme/be-compliant/respect-individuals-rights_en), [EDPB breach guidance](https://www.edpb.europa.eu/sme/assess-the-risks/data-breaches_en), [CNIL cookie rules](https://cnil.fr/fr/cookies-et-autres-traceurs/que-dit-la-loi). CNIL illustrates a national implementation; confirm the applicable member-state rules. Do not add a consent banner merely because Identity uses necessary cookies; first inventory actual tracking.

### Minimum data inventory for this solution

| Store/flow | Known or expected inventory items | Particular action |
| --- | --- | --- |
| Identity PostgreSQL | Email, user IDs, password hashes, MFA material, roles/claims, lockout and account state | Separate credential protection from rights exports; privileged access review. |
| API PostgreSQL/Mongo | Future business entities and soft-delete metadata | Define ownership, tenancy, retention and purge before storing personal data. |
| Redis | Output-cache entries when policies are enabled | Restrict access, configure expiry, exclude secrets and unsafe personalized caching. |
| Logs/traces/audit | Request metadata and any identifiers actually emitted | Sample and redact; distinguish security audit from diagnostic retention. |
| Email/CDN | Recipient information; browser requests to external asset hosts | Document recipients/transfers; consider self-hosted assets. |
| Backups/downloads | Copies of the above and generated exports | Encrypt, restrict access, expire copies and prevent deleted data reappearing after restore. |

## 5. Current standards and conditional legal scope

### Recommended engineering and governance references

Versions/status were checked for this assessment date. Standards evolve; recheck when implementing or making a contractual compliance claim.

| Reference | Current baseline identified | How to apply |
| --- | --- | --- |
| [OWASP ASVS](https://owasp.org/projects/asvs) | Stable 5.0.0 | Use relevant Level 2 requirements as the main verification checklist, with requirement IDs and evidence. This is a recommendation, not a legal mandate. |
| [OWASP Web Top 10](https://top10.owasp.org/2025/0x00_2025-Introduction/) | 2025 | Awareness and risk categorization; it does not replace a verification standard. |
| [OWASP API Top 10](https://api-security.owasp.org/editions/2023/en/0x03-introduction/) | 2023 | Focus on object/function/property authorization, resource consumption, SSRF and downstream API trust. |
| [NIST digital identity](https://pages.nist.gov/800-63-4-Implementation-Resources/) | SP 800-63 revision 4, final July 2025 | Select suitable authentication/recovery controls; enforce admin MFA and review session lifecycle. |
| [NIST SSDF publications](https://csrc.nist.gov/projects/ssdf/publications) | Version 1.1 final; version 1.2 listed as draft | Establish secure development, dependency handling and vulnerability response. Do not describe the draft as final. |
| [NIST CSF](https://www.nist.gov/news-events/news/2024/02/nist-releases-version-20-landmark-cybersecurity-framework) | 2.0 | Organize governance, prevention, detection, response and recovery with named owners. |
| [WCAG](https://www.w3.org/TR/WCAG22/) | 2.2 Recommendation | Target AA and retain test evidence. WCAG 3 is still a [draft effort](https://www.w3.org/WAI/standards-guidelines/wcag/wcag3-intro/). |
| [ISO/IEC 27001](https://www.iso.org/standard/27001) / [ISO/IEC 27701](https://www.iso.org/standard/27701) | 27001:2022, including applicable amendment; 27701:2025 | Optional organizational security/privacy management assurance, potentially useful for customers. Certification is not established by fixing code. |

### Rules to screen against the actual business model

| Rule | Applicability decision and action |
| --- | --- |
| European Accessibility Act | Applies to specified products/services, including covered e-commerce services, since 28 June 2025. A public website alone does not settle scope. Check the service category, national implementation and exemptions. WCAG 2.2 AA is an engineering target, not by itself proof of all legal requirements. [Commission overview](https://commission.europa.eu/news-and-media/news/eu-becomes-more-accessible-all-2025-07-31_en). |
| NIS2 | Depends on sector, entity type, size and designation; public EU availability alone is insufficient. Reassess if the business qualifies as a covered digital/infrastructure provider. [Commission FAQs](https://digital-strategy.ec.europa.eu/en/faqs/directive-measures-high-common-level-cybersecurity-across-union-nis2-directive-faqs). |
| Cyber Resilience Act | Screen if supplying covered products with digital elements, including relevant integral remote processing. Do not assume every hosted web service is covered. Reporting obligations began 11 September 2026; main obligations apply 11 December 2027. If in scope, establish product vulnerability handling and the applicable reporting process now. [Commission summary](https://digital-strategy.ec.europa.eu/en/policies/cra-summary). |
| Digital Services Act | Assess if hosting/intermediating user content or operating an online platform; ordinary account functionality alone does not establish that role. Determine corresponding notice/action and transparency obligations if applicable. [Commission DSA overview](https://digital-strategy.ec.europa.eu/en/policies/digital-services-act). |
| Data Act | Screen whether the offering is a covered data-processing service, including relevant SaaS. Cloud switching/export/contract obligations may apply; switching charges are removed from 12 January 2027. This is separate from GDPR rights. [Commission explanation](https://digital-strategy.ec.europa.eu/en/factpages/data-act-explained). |
| Consumer contracts | If offering digital services to consumers, assess pre-contract information, withdrawal/cancellation, conformity and remedies, including applicable personal-data-funded models. [Consumer Rights Directive](https://commission.europa.eu/law/law-topic/consumer-protection-law/consumer-contract-law/consumer-rights-directive_en), [digital contract rules](https://commission.europa.eu/topics/business-and-industry/contract-rules/digital-contracts/digital-contract-rules_en). |
| Payment-card requirements | No card processing was found. If adding payments, assess PCI DSS scope with the provider/acquirer; outsourcing checkout can reduce scope but does not automatically remove every obligation. [PCI SSC v4.0.1 reference](https://blog.pcisecuritystandards.org/request-for-comments-pci-data-security-standard-pci-dss-v4.0.1). |

No AI functionality, special-category processing or child-targeted service was established in this review. Reassess scope before adding these features; a general public service should still consider foreseeable use by minors. Do not infer that every EU regulation applies merely from having EU users.

## 6. Delivery plan and release evidence

### Phase 1 — Account and trust-boundary fixes

- [ ] Replace seeded administrator credentials and require admin MFA (F01).
- [ ] Replace no-op email and remove public confirmation tokens (F02).
- [ ] Remove MFA secrets from exports (F03).
- [ ] Enforce current authorization, security-event auditing and revocation requirements (F04–F05).
- [ ] Restrict gateway destinations before activating business API calls (F06).
- [ ] Define each future API action's user/role/owner authorization; the gateway key alone is insufficient.

### Phase 2 — Production platform and data handling

- [ ] Establish secret rotation, private service networking, TLS and least privilege (F07).
- [ ] Add abuse limits and safe mutation retries (F08).
- [ ] Correct context lifetimes and migration deployment (F09).
- [ ] Implement whole-application data rights/retention and accurate account-deletion wording (F10).
- [ ] Configure replica keys/circuits and cache boundaries (F11).
- [ ] Repair containers/tests; add release checks and dependency/license inventory (F12, F17).
- [ ] Demonstrate production probes, backup recovery, alerting, headers and redacted telemetry (F13–F14).
- [ ] Complete accessibility and credential/reset improvements (F15–F16).

### Phase 3 — Compliance evidence and continuing operation

- [ ] Approve processing inventory, privacy notice, processor agreements and cookie/storage inventory.
- [ ] Record the legal applicability decisions in section 5, with an owner and review date.
- [ ] Maintain an ASVS requirement/evidence matrix and risk register with time-bound exceptions.
- [ ] Exercise a privacy request, administrator revocation, secret rotation, incident notification decision and backup restore.
- [ ] Review privileges, dependencies, retention jobs and provider changes on a defined schedule.

**Suggested acceptance suite:** missing/invalid API keys; malicious gateway destinations; cross-user object access; confirmation/recovery abuse; export secret exclusion; stale admin circuits; disabled accounts; last-admin protection; MFA resets; duplicate mutation retries; rate/circuit limits; multi-replica cookie behavior; cache isolation; deletion/export coverage; container startup; keyboard/screen-reader journeys; and recovery from backups.

Release approval should reference completed evidence and explicitly accepted residual risks. A clean dependency scan, the existence of this report or deployment through AppHost does not establish compliance on its own.
