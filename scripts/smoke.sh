#!/usr/bin/env bash
set -euo pipefail

API_URL="${API_URL:-http://localhost:5080/api/v1}"
RESP_FILE="/tmp/smoke_resp.json"

echo "=== Daycare Smoke Test ==="
echo "Target API: $API_URL"

# Step 1: Login as admin, staff1, and parent1 (capture tokens)
echo "--- Step 1: Login as admin, staff1, and parent1 ---"
ADMIN_RESP=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@daycare.local","password":"Admin@12345"}')
ADMIN_TOKEN=$(echo "$ADMIN_RESP" | jq -r '.accessToken // empty')
test -n "$ADMIN_TOKEN" || { echo "Failed to login as admin"; echo "$ADMIN_RESP"; exit 1; }

STAFF1_RESP=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"staff1@daycare.local","password":"Staff@12345"}')
STAFF1_TOKEN=$(echo "$STAFF1_RESP" | jq -r '.accessToken // empty')
test -n "$STAFF1_TOKEN" || { echo "Failed to login as staff1"; echo "$STAFF1_RESP"; exit 1; }

PARENT1_RESP=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"parent1@daycare.local","password":"Parent@12345"}')
PARENT1_TOKEN=$(echo "$PARENT1_RESP" | jq -r '.accessToken // empty')
PARENT1_GUARDIAN_ID=$(echo "$PARENT1_RESP" | jq -r '.user.guardianId // empty')
test -n "$PARENT1_TOKEN" || { echo "Failed to login as parent1"; echo "$PARENT1_RESP"; exit 1; }

echo "Step 1 passed: Tokens captured for admin, staff1, parent1."

# Step 2: Admin creates a child, a guardian with an account, and links them (can pick up, payer, emergency contact, primary)
echo "--- Step 2: Admin creates child, guardian with account, and links them ---"

# Get Infants and Preschool classroom IDs
CLASSROOMS_RESP=$(curl -s -X GET "$API_URL/classrooms" -H "Authorization: Bearer $ADMIN_TOKEN")
INFANTS_ID=$(echo "$CLASSROOMS_RESP" | jq -r '.[] | select(.name=="Infants") | .id // empty')
PRESCHOOL_ID=$(echo "$CLASSROOMS_RESP" | jq -r '.[] | select(.name=="Preschool") | .id // empty')
test -n "$INFANTS_ID" || { echo "Failed to find Infants classroom"; exit 1; }
test -n "$PRESCHOOL_ID" || { echo "Failed to find Preschool classroom"; exit 1; }

# Assign staff1 to Preschool as well to ensure capacity headroom for smoke test
STAFF_LIST=$(curl -s -X GET "$API_URL/staff" -H "Authorization: Bearer $ADMIN_TOKEN")
STAFF1_ID=$(echo "$STAFF_LIST" | jq -r '.[] | select(.email=="staff1@daycare.local") | .id')
STAFF1_DATA=$(echo "$STAFF_LIST" | jq -r '.[] | select(.email=="staff1@daycare.local")')
curl -s -X PUT "$API_URL/staff/$STAFF1_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"firstName\": $(echo "$STAFF1_DATA" | jq .firstName),
    \"lastName\": $(echo "$STAFF1_DATA" | jq .lastName),
    \"phone\": $(echo "$STAFF1_DATA" | jq .phone),
    \"jobTitle\": $(echo "$STAFF1_DATA" | jq .jobTitle),
    \"hireDate\": $(echo "$STAFF1_DATA" | jq .hireDate),
    \"classroomIds\": [\"$INFANTS_ID\", \"$PRESCHOOL_ID\"]
  }" > /dev/null

# Create guardian with account
UNIQUE_SUFFIX=$(date +%s%N | cut -b1-13)
GUARDIAN_PHONE="+251911$((100000 + RANDOM % 899999))"
GUARDIAN_RESP=$(curl -s -X POST "$API_URL/guardians" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"firstName\": \"Tadesse\",
    \"lastName\": \"Smoke$UNIQUE_SUFFIX\",
    \"phone\": \"$GUARDIAN_PHONE\",
    \"email\": \"smoke.parent.$UNIQUE_SUFFIX@daycare.local\",
    \"address\": \"Bole, Addis Ababa\",
    \"createAccount\": true,
    \"temporaryPassword\": \"Parent@12345\"
  }")
SMOKE_GUARDIAN_ID=$(echo "$GUARDIAN_RESP" | jq -r '.guardian.id // .id // empty')
test -n "$SMOKE_GUARDIAN_ID" || { echo "Failed to create guardian"; echo "$GUARDIAN_RESP"; exit 1; }

# Create unauthorized guardian (relative who cannot pick up)
UNAUTH_PHONE="+251922$((100000 + RANDOM % 899999))"
UNAUTH_GUARDIAN_RESP=$(curl -s -X POST "$API_URL/guardians" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"firstName\": \"Unauthorized\",
    \"lastName\": \"Relative$UNIQUE_SUFFIX\",
    \"phone\": \"$UNAUTH_PHONE\",
    \"address\": \"Bole, Addis Ababa\"
  }")
UNAUTH_GUARDIAN_ID=$(echo "$UNAUTH_GUARDIAN_RESP" | jq -r '.guardian.id // .id // empty')
test -n "$UNAUTH_GUARDIAN_ID" || { echo "Failed to create unauthorized guardian"; exit 1; }

# Create child in Preschool (capacity 16, age 36 months)
CHILD_RESP=$(curl -s -X POST "$API_URL/children" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"firstName\": \"SmokeKid$UNIQUE_SUFFIX\",
    \"lastName\": \"Tadesse\",
    \"dateOfBirth\": \"2023-04-01\",
    \"gender\": \"Male\",
    \"enrollmentDate\": \"2026-01-01\",
    \"classroomId\": \"$PRESCHOOL_ID\"
  }")
SMOKE_CHILD_ID=$(echo "$CHILD_RESP" | jq -r '.id // empty')
test -n "$SMOKE_CHILD_ID" || { echo "Failed to create child"; echo "$CHILD_RESP"; exit 1; }

# Link authorized guardian (canPickUp=true, isPrimary=true, isPayer=true, isEmergencyContact=true)
LINK_RESP=$(curl -s -X POST "$API_URL/children/$SMOKE_CHILD_ID/guardians" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"guardianId\": \"$SMOKE_GUARDIAN_ID\",
    \"relationship\": \"Father\",
    \"isPrimaryContact\": true,
    \"canPickUp\": true,
    \"isPayer\": true,
    \"isEmergencyContact\": true
  }")
LINK_ID=$(echo "$LINK_RESP" | jq -r '.guardianId // .id // empty')
test -n "$LINK_ID" || { echo "Failed to link authorized guardian"; echo "$LINK_RESP"; exit 1; }

# Link unauthorized relative (canPickUp=false)
curl -s -X POST "$API_URL/children/$SMOKE_CHILD_ID/guardians" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"guardianId\": \"$UNAUTH_GUARDIAN_ID\",
    \"relationship\": \"Relative\",
    \"isPrimaryContact\": false,
    \"canPickUp\": false,
    \"isPayer\": false,
    \"isEmergencyContact\": false
  }" > /dev/null

echo "Step 2 passed: Child created and linked to authorized & unauthorized guardians."

# Step 3: Staff check-in the child (expect 200)
echo "--- Step 3: Staff check-in the child ---"
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/attendance/check-in" \
  -H "Authorization: Bearer $STAFF1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"childId\": \"$SMOKE_CHILD_ID\",
    \"droppedOffByGuardianId\": \"$SMOKE_GUARDIAN_ID\",
    \"notes\": \"Morning dropoff smoke test\"
  }")
echo "Check-in status: $HTTP_CODE"
test "$HTTP_CODE" -eq 200 || { echo "Check-in failed with code $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
ATTENDANCE_ID=$(jq -r '.record.id // .attendance.id // .id // empty' "$RESP_FILE")
test -n "$ATTENDANCE_ID" || { echo "Attendance ID missing in check-in response"; cat "$RESP_FILE"; exit 1; }
echo "Step 3 passed: Child checked in successfully (Attendance ID: $ATTENDANCE_ID)."

# Step 4: Check-out with a non-authorized guardian (expect 422 PICKUP_NOT_AUTHORIZED)
echo "--- Step 4: Check-out with unauthorized guardian ---"
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/attendance/$ATTENDANCE_ID/check-out" \
  -H "Authorization: Bearer $STAFF1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"pickedUpByGuardianId\": \"$UNAUTH_GUARDIAN_ID\",
    \"idVerified\": true
  }")
echo "Unauthorized checkout status: $HTTP_CODE"
test "$HTTP_CODE" -eq 422 || { echo "Expected 422 for unauthorized checkout, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
ERR_CODE=$(jq -r '.code // empty' "$RESP_FILE")
test "$ERR_CODE" = "PICKUP_NOT_AUTHORIZED" || { echo "Expected error code PICKUP_NOT_AUTHORIZED, got $ERR_CODE"; cat "$RESP_FILE"; exit 1; }
echo "Step 4 passed: Unauthorized pickup correctly rejected with 422 PICKUP_NOT_AUTHORIZED."

# Step 5: Check-out with authorized guardian and idVerified: true (expect 200)
echo "--- Step 5: Check-out with authorized guardian and idVerified: true ---"
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/attendance/$ATTENDANCE_ID/check-out" \
  -H "Authorization: Bearer $STAFF1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"pickedUpByGuardianId\": \"$SMOKE_GUARDIAN_ID\",
    \"idVerified\": true
  }")
echo "Authorized checkout status: $HTTP_CODE"
test "$HTTP_CODE" -eq 200 || { echo "Expected 200 for authorized checkout, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
CHECKOUT_AT=$(jq -r '.checkOutAt // empty' "$RESP_FILE")
test -n "$CHECKOUT_AT" || { echo "checkOutAt missing in response"; cat "$RESP_FILE"; exit 1; }
echo "Step 5 passed: Authorized checkout succeeded with checkOutAt populated."

# Step 6: Staff adds a daily report entry for a different, not-checked-in child (expect 422 or 409)
echo "--- Step 6: Staff adds daily report entry for not-checked-in child ---"
# Create a child in Infants that is not checked in
UNCHECKED_CHILD_RESP=$(curl -s -X POST "$API_URL/children" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"firstName\": \"Unchecked$UNIQUE_SUFFIX\",
    \"lastName\": \"Test\",
    \"dateOfBirth\": \"2023-05-01\",
    \"gender\": \"Female\",
    \"enrollmentDate\": \"2026-01-01\",
    \"classroomId\": \"$PRESCHOOL_ID\"
  }")
UNCHECKED_CHILD_ID=$(echo "$UNCHECKED_CHILD_RESP" | jq -r '.id // empty')
test -n "$UNCHECKED_CHILD_ID" || { echo "Failed to create unchecked child"; exit 1; }

TODAY=$(date +%Y-%m-%d)
NOW_TIMESTAMP=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/children/$UNCHECKED_CHILD_ID/daily-reports/$TODAY/entries" \
  -H "Authorization: Bearer $STAFF1_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"entryType\": \"Meal\",
    \"occurredAt\": \"$NOW_TIMESTAMP\",
    \"mealType\": \"Lunch\",
    \"amountEaten\": \"All\",
    \"description\": \"Attempt on not-checked-in child\"
  }")
echo "Not-checked-in daily report entry status: $HTTP_CODE"
test "$HTTP_CODE" -eq 422 -o "$HTTP_CODE" -eq 409 || { echo "Expected 422 or 409, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
ERR_CODE=$(jq -r '.code // empty' "$RESP_FILE")
test "$ERR_CODE" = "NOT_CHECKED_IN" || { echo "Expected error code NOT_CHECKED_IN, got $ERR_CODE"; cat "$RESP_FILE"; exit 1; }
echo "Step 6 passed: Report entry rejected with $ERR_CODE for not-checked-in child."

# Step 7: Parent1 requests another family's child (expect 403)
echo "--- Step 7: Parent1 requests another family's child ---"
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X GET "$API_URL/children/$SMOKE_CHILD_ID" \
  -H "Authorization: Bearer $PARENT1_TOKEN")
echo "Cross-family child request status: $HTTP_CODE"
test "$HTTP_CODE" -eq 403 || { echo "Expected 403 Forbidden for cross-family child request, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
echo "Step 7 passed: Cross-family access rejected with 403 Forbidden."

# Step 8: Admin generates invoices for current month, records partial payment, then overpayment (expect 422 OVERPAYMENT)
echo "--- Step 8: Admin generates invoices, records partial payment, attempts overpayment ---"
# Assign fee plan to SMOKE_CHILD
FEE_PLANS_RESP=$(curl -s -X GET "$API_URL/fee-plans" -H "Authorization: Bearer $ADMIN_TOKEN")
FEE_PLAN_ID=$(echo "$FEE_PLANS_RESP" | jq -r '.[0].id // empty')
test -n "$FEE_PLAN_ID" || { echo "Failed to find fee plan"; exit 1; }

curl -s -X PUT "$API_URL/children/$SMOKE_CHILD_ID/fee-plan" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"feePlanId\":\"$FEE_PLAN_ID\"}" > /dev/null

CURRENT_YEAR=$(date +%Y)
CURRENT_MONTH=$(( 10#$(date +%m) ))

GEN_RESP=$(curl -s -X POST "$API_URL/invoices/generate" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{\"year\":$CURRENT_YEAR,\"month\":$CURRENT_MONTH}")
CREATED_COUNT=$(echo "$GEN_RESP" | jq -r '.created // (.invoiceIds | length) // 0')
echo "Generated invoices count: $CREATED_COUNT"
test "$CREATED_COUNT" -gt 0 || { echo "Expected created count > 0"; echo "$GEN_RESP"; exit 1; }

# Find generated invoice for smoke child or one of the generated invoices
INVOICES_RESP=$(curl -s -X GET "$API_URL/invoices?childId=$SMOKE_CHILD_ID" \
  -H "Authorization: Bearer $ADMIN_TOKEN")
INVOICE_ID=$(echo "$INVOICES_RESP" | jq -r '.items[0].id // empty')
if [ -z "$INVOICE_ID" ]; then
  # Fall back to first generated invoice id
  INVOICE_ID=$(echo "$GEN_RESP" | jq -r '.invoiceIds[0] // empty')
fi
test -n "$INVOICE_ID" || { echo "Failed to find generated invoice"; exit 1; }

# Record partial payment (e.g. 1000 ETB)
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/invoices/$INVOICE_ID/payments" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"amount\": 1000.00,
    \"method\": \"Cash\",
    \"reference\": \"SMOKE-PARTIAL-$UNIQUE_SUFFIX\",
    \"notes\": \"Smoke test partial payment\"
  }")
echo "Partial payment status: $HTTP_CODE"
test "$HTTP_CODE" -eq 200 || { echo "Partial payment failed with code $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }

# Attempt overpayment (e.g. 500000 ETB)
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X POST "$API_URL/invoices/$INVOICE_ID/payments" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d "{
    \"amount\": 500000.00,
    \"method\": \"Cash\"
  }")
echo "Overpayment attempt status: $HTTP_CODE"
test "$HTTP_CODE" -eq 422 || { echo "Expected 422 for overpayment, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
OVERPAY_ERR=$(jq -r '.code // empty' "$RESP_FILE")
test "$OVERPAY_ERR" = "OVERPAYMENT" || { echo "Expected error code OVERPAYMENT, got $OVERPAY_ERR"; cat "$RESP_FILE"; exit 1; }
echo "Step 8 passed: Partial payment recorded; overpayment correctly rejected with 422 OVERPAYMENT."

# Step 9: Parent1 lists invoices and sees only their own
echo "--- Step 9: Parent1 lists invoices and sees only their own ---"
PARENT_INVOICES_RESP=$(curl -s -X GET "$API_URL/invoices" \
  -H "Authorization: Bearer $PARENT1_TOKEN")
PARENT_INVOICE_COUNT=$(echo "$PARENT_INVOICES_RESP" | jq -r '.items | length')
echo "Parent1 invoices returned: $PARENT_INVOICE_COUNT"

# Verify all invoices belong to parent1's guardian ID
if [ "$PARENT_INVOICE_COUNT" -gt 0 ]; then
  OTHER_GUARDIAN_INVOICES=$(echo "$PARENT_INVOICES_RESP" | jq -r --arg gid "$PARENT1_GUARDIAN_ID" '.items[] | select(.guardianId != $gid) | .id')
  test -z "$OTHER_GUARDIAN_INVOICES" || { echo "Parent1 saw other guardians' invoices: $OTHER_GUARDIAN_INVOICES"; exit 1; }
fi

# Try to directly access another parent's invoice with Parent1 token
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X GET "$API_URL/invoices/$INVOICE_ID" \
  -H "Authorization: Bearer $PARENT1_TOKEN")
echo "Direct cross-parent invoice access status: $HTTP_CODE"
test "$HTTP_CODE" -eq 403 || { echo "Expected 403 Forbidden for cross-parent invoice access, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }

# Step 10: Profile photo upload, download, and non-admin refusal (BR-26, BR-27)
echo "--- Step 10: Profile photo upload, download, and non-admin refusal ---"
PHOTO_TEMP="/tmp/smoke_photo_upload.jpg"
PHOTO_DOWNLOAD="/tmp/smoke_photo_download.jpg"
python3 -c "from PIL import Image; Image.new('RGB', (64, 64), color='teal').save('$PHOTO_TEMP', 'JPEG')"

# 1. Admin uploads photo for child
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X PUT "$API_URL/children/$SMOKE_CHILD_ID/photo" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -F "file=@$PHOTO_TEMP;type=image/jpeg")
echo "Admin photo upload status: $HTTP_CODE"
test "$HTTP_CODE" -eq 200 || { echo "Admin photo upload failed with status $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }
HAS_PHOTO=$(jq -r '.hasPhoto // false' "$RESP_FILE")
test "$HAS_PHOTO" = "true" || { echo "Expected hasPhoto=true, got $HAS_PHOTO"; cat "$RESP_FILE"; exit 1; }

# 2. Download photo and verify non-empty
HTTP_CODE=$(curl -s -w "%{http_code}" -X GET "$API_URL/children/$SMOKE_CHILD_ID/photo" \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -o "$PHOTO_DOWNLOAD")
echo "Photo download status: $HTTP_CODE"
test "$HTTP_CODE" -eq 200 || { echo "Photo download failed with status $HTTP_CODE"; exit 1; }
test -s "$PHOTO_DOWNLOAD" || { echo "Downloaded photo file is empty"; exit 1; }

# 3. Non-admin (parent) attempts photo upload -> must be rejected with 403 Forbidden
HTTP_CODE=$(curl -s -o "$RESP_FILE" -w "%{http_code}" -X PUT "$API_URL/children/$SMOKE_CHILD_ID/photo" \
  -H "Authorization: Bearer $PARENT1_TOKEN" \
  -F "file=@$PHOTO_TEMP;type=image/jpeg")
echo "Non-admin photo upload status: $HTTP_CODE"
test "$HTTP_CODE" -eq 403 || { echo "Expected 403 Forbidden for non-admin photo upload, got $HTTP_CODE"; cat "$RESP_FILE"; exit 1; }

rm -f "$PHOTO_TEMP" "$PHOTO_DOWNLOAD"
echo "Step 10 passed: Photo upload, download, and non-admin refusal verified."

echo "=========================================="
echo "SMOKE TEST PASSED: ALL 10 STEPS SUCCEEDED!"
echo "=========================================="
