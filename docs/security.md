# Security and Authentication

## Token Lifetime
Authentication uses stateless JSON Web Tokens (JWT) signed with HMAC-SHA256. Tokens are issued by POST /api/v1/Auth/login with a lifetime of 120 minutes (configured by `Jwt:ExpiryMinutes` in `appsettings.json`). Token claims include issuer `daycare-api` and audience `daycare-web`.

## Lockout Configuration
Brute-force protection is enforced by ASP.NET Core Identity:
- Maximum failed access attempts: 5
- Default lockout duration: 10 minutes
- Allowed for new users: true

## Per-Request Active-User Check
To prevent deactivated accounts from using unexpired JWT tokens, the JWT bearer event OnTokenValidated intercepts every authenticated request, resolves IIdentityService, and invokes ValidateUserActiveAsync. If the user account is marked IsActive = false, the token validation fails immediately and returns HTTP 401 Unauthorized.

## Role and Resource Authorization
- Roles: Three roles exist: Admin, Staff, and Parent.
- Controller Guards: Actions are restricted via [Authorize(Roles = "...")] attributes.
- Resource Guards: Domain-level resource scoping is enforced by IAccessGuard:
  - Parent role can only view children, daily reports, incidents, and invoices explicitly linked to their guardian record.
  - Staff role can only view and update records for children assigned to their classrooms, and are denied access to invoices.

---

---

## Architecture Characteristics and Limitations

The following architectural characteristics and limitations have been verified directly in the code:
- Stateless JWT: Tokens are signed with HMAC-SHA256 and expire after 120 minutes. There is no refresh token mechanism; users must re-authenticate upon expiration.
- Active Status Verification: Every authenticated request checks whether the user account is active via OnTokenValidated, ensuring immediate revocation if an account is disabled.
- Account Lockout: Enforces 5 failed password attempts before a 10-minute lockout.
- Rate Limiting: No rate limiting middleware or throttle policy is configured.
- Transport Security: HTTPS redirection is enabled via UseHttpsRedirection, but HTTP Strict Transport Security (HSTS) headers are not configured.
- Audit Logging: There is no dedicated audit log table or historical record tracking changes across entities.
- Multi-Tenancy: The application is designed for a single daycare center; there is no multi-tenant isolation.
- Photo Storage: Photos are stored on the local filesystem rather than cloud object storage (such as AWS S3 or Azure Blob Storage).
