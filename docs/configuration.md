# Configuration Reference

The application reads settings from `appsettings.json` and environment variables:

| Section | Key | Purpose | Note |
|---|---|---|---|
| `ConnectionStrings` | `Default` | PostgreSQL connection string | Defaults to port 5433 |
| `Jwt` | `Issuer` | Token issuer claim (`daycare-api`) | Configurable |
| `Jwt` | `Audience` | Token audience claim (`daycare-web`) | Configurable |
| `Jwt` | `ExpiryMinutes` | Access token lifespan (120 minutes) | Configurable |
| `Jwt` | `Key` | Secret key for signing tokens | Override in production |
| `Daycare` | `Name` | Daycare center name | Defaults to Little Steps Daycare |
| `Daycare` | `TimeZoneId` | Facility time zone identifier | Defaults to Africa/Addis_Ababa |
| `Daycare` | `Currency` | Billing currency code | Defaults to ETB |
| `Daycare` | `OpeningTime` | Daily facility opening time | Defaults to 07:00 |
| `Daycare` | `ClosingTime` | Daily facility closing time | Defaults to 18:00 |
| `Daycare` | `LateFeeBlockMinutes` | Late pickup billing block size in minutes | Defaults to 15 |
| `Daycare` | `LateFeePerBlock` | Late pickup fee per block amount | Defaults to 50.00 |
| `Daycare` | `InvoiceDueDays` | Payment grace period days after period start | Defaults to 10 |
| `Cors` | `AllowedOrigins` | Allowed CORS origins for API requests | Defaults to http://localhost:4200 |
| `SeedAdmin` | `Email` | Default administrator account email | Defaults to admin@daycare.local |
| `SeedAdmin` | `Password` | Default administrator account initial password | Development default |
| `SeedAdmin` | `FullName` | Default administrator account display name | Defaults to Daycare Admin |
| `Photos` | `RootPath` | Local directory path for photo storage | Defaults to data/photos |
| `Photos` | `MaxUploadBytes` | Maximum upload size in bytes | Defaults to 2097152 (2 MB) |
| `Photos` | `MaxDimension` | Maximum image dimension in pixels | Defaults to 512 |
| `Photos` | `JpegQuality` | Encoded JPEG quality (1-100) | Defaults to 85 |

All password, connection string, and key values committed in the repository are development-only defaults and must be overridden using environment variables in production.

---
