# Business Rules

## Facility Operating Hours and Time Zone
- The facility operates from 07:00 to 18:00 local time (configured by `Daycare:OpeningTime` and `Daycare:ClosingTime` in `appsettings.json`).
- All calendar dates and local timestamps are evaluated in the `Africa/Addis_Ababa` time zone (configured by `Daycare:TimeZoneId`).

## Attendance and Check-In Preconditions
- Child status must be `Active` (`CHILD_INACTIVE`). Inactive or withdrawn children cannot be checked in.
- Child must be assigned to an active classroom (`NO_CLASSROOM`, `CLASSROOM_INACTIVE`).
- Child must have at least one authorized pickup contact (`NO_PICKUP_GUARDIAN`).
- Child must have at least one emergency contact (`NO_EMERGENCY_CONTACT`).
- Child cannot already have an attendance record on the same date (`ALREADY_CHECKED_IN`, enforced by unique index `ix_attendance_records_child_id_date`).
- Classroom capacity must not be exceeded (`CLASSROOM_FULL`).

## Check-Out and Pickup Authorization
- Check-out requires an active `Present` attendance record (`NOT_CHECKED_IN`).
- A child cannot be checked out more than once per day (`ALREADY_CHECKED_OUT`).
- The pickup individual must be linked to the child with `CanPickUp = true` (`PICKUP_NOT_AUTHORIZED`).
- Staff must verify the pickup guardian photo ID (`idVerified = true`), otherwise check-out is rejected with `ID_NOT_VERIFIED`.
- Check-out time must be greater than or equal to check-in time (enforced by database check constraint `ck_attendance_records_checkout_after_checkin`).

## Late Pickup Fees
- Pickups occurring after closing time (18:00) accumulate late pickup minutes.
- Late fees are billed in 15-minute blocks (`Daycare:LateFeeBlockMinutes = 15`) at 50.00 ETB per block (`Daycare:LateFeePerBlock = 50.00`).
- Late fee line items (`InvoiceLineType.LateFee`) are automatically added during monthly invoice generation.

## Guardian Contacts
- Exactly one primary contact guardian is allowed per child (enforced in application logic and by PostgreSQL filtered unique index `ix_child_guardians_child_id` with `is_primary_contact = true`).

## Invoices and Billing
- Monthly tuition invoices are generated based on the active child assigned `FeePlan` monthly rate.
- Duplicate invoices for the same child in the same billing period are rejected (enforced by unique index `ix_invoices_child_id_period_start` where `status <> 'Void'`).
- Invoices are due 10 days after the period start date (`Daycare:InvoiceDueDays = 10`).
- Valid invoice statuses are strictly `Issued`, `PartiallyPaid`, `Paid`, and `Void`.
- Invoices cannot be voided if any active payments have been recorded against them (`INVOICE_HAS_PAYMENTS`).
- Void invoices cannot receive additional line items, adjustments, or payments (`INVOICE_VOID`).
- Overpayment is rejected: payments cannot exceed the remaining invoice balance (`OVERPAYMENT`).
- Voiding a payment restores the invoice balance and recalculates invoice status (`PAYMENT_ALREADY_VOIDED` if already voided).

## Daily Report Lock
- Daily report activity entries may only be added, updated, or removed on the current calendar day (`ENTRY_LOCKED`).

---
