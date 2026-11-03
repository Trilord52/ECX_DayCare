# Profile Photos Specification

Profile photos are supported for Children, Staff, and Guardians.

- Storage Location: Uploaded files are stored on the local disk at `data/photos` (configured by `Photos:RootPath`).
- Image Processing: Managed by `SkiaImageProcessor` (`src/Daycare.Infrastructure/Services/SkiaImageProcessor.cs`). Decodes images using SkiaSharp, resizes with aspect ratio preserved so that the longest side is at most 512 pixels (`Photos:MaxDimension = 512`), and encodes to JPEG at 85 quality (`Photos:JpegQuality = 85`).
- Limits and Constraints: Maximum upload file size is 2 MB (2,097,152 bytes, enforced by `Photos:MaxUploadBytes`), with an endpoint request body limit of 3 MB (`[RequestSizeLimit(3 * 1024 * 1024)]`). Accepted formats are JPEG, PNG, and WebP.
- Decompression-Bomb Guard: Images with declared dimensions exceeding 8,000 pixels on either axis or exceeding 25,000,000 total pixels are rejected (`PHOTO_INVALID`).
- Database Tracking: Profile photo metadata is stored as an owned value object (`photo_key`, `photo_content_type`, `photo_size_bytes`, `photo_updated_at`). PostgreSQL check constraints `ck_*_photo_all_or_none` ensure all four columns are either populated or all null.
- Delivery and Caching: Photos are never served as static files. Delivered dynamically via controller endpoints verifying caller role and resource authorization (`IAccessGuard`). Responses include a strong ETag derived from `photo_updated_at` ticks supporting conditional HTTP 304 Not Modified responses, with HTTP response header `Cache-Control: private, no-cache`.

---
