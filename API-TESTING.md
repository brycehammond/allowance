# API Testing Guide

Quick reference for testing the Allowance Tracker REST API with curl.

## Setup

```bash
# Set base URL
API_URL="https://localhost:7001/api/v1"

# Or for development
API_URL="http://localhost:5000/api/v1"
```

## Authentication Flow

### 1. Register Parent Account

```bash
curl -X POST $API_URL/auth/register/parent \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe",
    "familyName": "Doe Family"
  }' | jq
```

**Response includes JWT token:**
```json
{
  "userId": "guid",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Parent",
  "familyId": "guid",
  "familyName": "Doe Family",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-10-10T12:00:00Z"
}
```

### 2. Login

```bash
curl -X POST $API_URL/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!",
    "rememberMe": false
  }' | jq
```

**Save the token:**
```bash
TOKEN=$(curl -X POST $API_URL/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john@example.com",
    "password": "SecurePass123!"
  }' -s | jq -r '.token')

echo "Token: $TOKEN"
```

### 3. Get Current User

```bash
curl -X GET $API_URL/auth/me \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Family Management

### Get Current Family

```bash
curl -X GET $API_URL/families/current \
  -H "Authorization: Bearer $TOKEN" | jq
```

### Get Family Members

```bash
curl -X GET $API_URL/families/current/members \
  -H "Authorization: Bearer $TOKEN" | jq
```

### Get Family Children

```bash
curl -X GET $API_URL/families/current/children \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Child Management

### Register Child (Parent Only)

```bash
curl -X POST $API_URL/auth/register/child \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "email": "alice@example.com",
    "password": "ChildPass123!",
    "firstName": "Alice",
    "lastName": "Doe",
    "weeklyAllowance": 15.00
  }' | jq

# Save child ID from response
CHILD_ID="guid-from-response"
```

### Get Child Details

```bash
curl -X GET $API_URL/children/$CHILD_ID \
  -H "Authorization: Bearer $TOKEN" | jq
```

### Update Child's Weekly Allowance (Parent Only)

```bash
curl -X PUT $API_URL/children/$CHILD_ID/allowance \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "weeklyAllowance": 20.00
  }' | jq
```

### Delete Child (Parent Only)

```bash
curl -X DELETE $API_URL/children/$CHILD_ID \
  -H "Authorization: Bearer $TOKEN"
```

## Transaction Management

### Create Transaction (Parent Only)

**Add Money (Credit):**
```bash
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 25.00,
    "type": "Credit",
    "description": "Completed weekly chores"
  }' | jq
```

**Deduct Money (Debit):**
```bash
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 10.00,
    "type": "Debit",
    "description": "Bought a toy"
  }' | jq
```

### Get Child Transactions

```bash
# Get last 20 transactions
curl -X GET "$API_URL/transactions/children/$CHILD_ID?limit=20" \
  -H "Authorization: Bearer $TOKEN" | jq

# Get with pagination
curl -X GET "$API_URL/transactions/children/$CHILD_ID?limit=10&offset=0" \
  -H "Authorization: Bearer $TOKEN" | jq
```

### Get Child Balance

```bash
curl -X GET $API_URL/transactions/children/$CHILD_ID/balance \
  -H "Authorization: Bearer $TOKEN" | jq
```

## Complete Workflow Example

```bash
#!/bin/bash

# Configuration
API_URL="https://localhost:7001/api/v1"

echo "1. Registering parent..."
PARENT_RESPONSE=$(curl -X POST $API_URL/auth/register/parent \
  -H "Content-Type: application/json" \
  -d '{
    "email": "parent@test.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "Parent",
    "familyName": "Test Family"
  }' -s)

TOKEN=$(echo $PARENT_RESPONSE | jq -r '.token')
echo "Token: $TOKEN"

echo -e "\n2. Adding child..."
CHILD_RESPONSE=$(curl -X POST $API_URL/auth/register/child \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "email": "child@test.com",
    "password": "Test123!",
    "firstName": "Test",
    "lastName": "Child",
    "weeklyAllowance": 10.00
  }' -s)

CHILD_ID=$(echo $CHILD_RESPONSE | jq -r '.childId')
echo "Child ID: $CHILD_ID"

echo -e "\n3. Getting family children..."
curl -X GET $API_URL/families/current/children \
  -H "Authorization: Bearer $TOKEN" -s | jq

echo -e "\n4. Adding money (chore completed)..."
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 5.00,
    "type": "Credit",
    "description": "Washed dishes"
  }' -s | jq

echo -e "\n5. Checking balance..."
curl -X GET $API_URL/transactions/children/$CHILD_ID/balance \
  -H "Authorization: Bearer $TOKEN" -s | jq

echo -e "\n6. Deducting money (purchase)..."
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 3.00,
    "type": "Debit",
    "description": "Bought candy"
  }' -s | jq

echo -e "\n7. Getting transaction history..."
curl -X GET "$API_URL/transactions/children/$CHILD_ID?limit=10" \
  -H "Authorization: Bearer $TOKEN" -s | jq

echo -e "\n8. Updating weekly allowance..."
curl -X PUT $API_URL/children/$CHILD_ID/allowance \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "weeklyAllowance": 15.00
  }' -s | jq

echo -e "\nDone!"
```

## Testing with Child Account

```bash
# Login as child
CHILD_TOKEN=$(curl -X POST $API_URL/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "alice@example.com",
    "password": "ChildPass123!"
  }' -s | jq -r '.token')

# Get own information
curl -X GET $API_URL/auth/me \
  -H "Authorization: Bearer $CHILD_TOKEN" | jq

# Get own child profile
curl -X GET $API_URL/children/$CHILD_ID \
  -H "Authorization: Bearer $CHILD_TOKEN" | jq

# Get own transactions
curl -X GET $API_URL/transactions/children/$CHILD_ID \
  -H "Authorization: Bearer $CHILD_TOKEN" | jq

# Children CANNOT create transactions (will return 403 Forbidden)
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $CHILD_TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 100.00,
    "type": "Credit",
    "description": "Trying to cheat!"
  }'
```

## Error Handling Examples

**Invalid credentials:**
```bash
curl -X POST $API_URL/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "wrong@example.com",
    "password": "WrongPassword"
  }' | jq
# Returns 401 Unauthorized
```

**Missing authentication:**
```bash
curl -X GET $API_URL/families/current | jq
# Returns 401 Unauthorized
```

**Insufficient funds:**
```bash
curl -X POST $API_URL/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "'$CHILD_ID'",
    "amount": 10000.00,
    "type": "Debit",
    "description": "Trying to spend more than available"
  }' | jq
# Returns 400 Bad Request with INSUFFICIENT_FUNDS error
```

**Unauthorized access:**
```bash
# Parent trying to access another family's child
curl -X GET $API_URL/children/other-family-child-id \
  -H "Authorization: Bearer $TOKEN" | jq
# Returns 403 Forbidden
```

## Postman Collection

Import this JSON into Postman for easy testing:

```json
{
  "info": {
    "name": "Allowance Tracker API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://localhost:7001/api/v1"
    },
    {
      "key": "token",
      "value": ""
    }
  ],
  "item": [
    {
      "name": "Auth",
      "item": [
        {
          "name": "Register Parent",
          "request": {
            "method": "POST",
            "url": "{{baseUrl}}/auth/register/parent",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"email\": \"parent@example.com\",\n  \"password\": \"Test123!\",\n  \"firstName\": \"John\",\n  \"lastName\": \"Doe\",\n  \"familyName\": \"Doe Family\"\n}",
              "options": { "raw": { "language": "json" } }
            }
          }
        },
        {
          "name": "Login",
          "request": {
            "method": "POST",
            "url": "{{baseUrl}}/auth/login",
            "body": {
              "mode": "raw",
              "raw": "{\n  \"email\": \"parent@example.com\",\n  \"password\": \"Test123!\"\n}",
              "options": { "raw": { "language": "json" } }
            }
          }
        },
        {
          "name": "Get Current User",
          "request": {
            "method": "GET",
            "url": "{{baseUrl}}/auth/me",
            "header": [
              { "key": "Authorization", "value": "Bearer {{token}}" }
            ]
          }
        }
      ]
    }
  ]
}
```

## Notes

- All timestamps are in UTC
- JWT tokens expire after 24 hours
- Weekly allowances are processed automatically by background job
- Balance cannot go negative
- All money amounts use decimal precision
- Role-based authorization enforced on all endpoints

## Quick Reference

| Endpoint | Method | Auth | Role | Description |
|----------|--------|------|------|-------------|
| `/auth/register/parent` | POST | No | - | Register parent + family |
| `/auth/register/child` | POST | Yes | Parent | Register child |
| `/auth/login` | POST | No | - | Login |
| `/auth/me` | GET | Yes | Any | Get current user |
| `/families/current` | GET | Yes | Any | Get family info |
| `/families/current/members` | GET | Yes | Any | Get family members |
| `/families/current/children` | GET | Yes | Any | Get family children |
| `/children/{id}` | GET | Yes | Parent/Self | Get child details |
| `/children/{id}/allowance` | PUT | Yes | Parent | Update allowance |
| `/children/{id}` | DELETE | Yes | Parent | Delete child |
| `/transactions` | POST | Yes | Parent | Create transaction |
| `/transactions/children/{id}` | GET | Yes | Parent/Self | Get transactions |
| `/transactions/children/{id}/balance` | GET | Yes | Parent/Self | Get balance |

Happy testing! 🚀
