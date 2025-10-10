# Complete REST API Specification

## Overview
Comprehensive REST API for the Allowance Tracker application, enabling mobile apps and third-party integrations. All endpoints follow RESTful conventions with JWT Bearer authentication.

## Authentication

All API endpoints (except `/api/auth/*`) require JWT Bearer token authentication.

### Headers
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

---

## API Endpoints

### 1. Authentication API (`/api/v1/auth`)

#### 1.1 Register Parent
Creates a new parent account and associated family.

**Endpoint:** `POST /api/v1/auth/register/parent`
**Authentication:** None (public)

**Request Body:**
```json
{
  "email": "parent@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe",
  "familyName": "Doe Family"
}
```

**Response (201 Created):**
```json
{
  "userId": "guid",
  "email": "parent@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Parent",
  "familyId": "guid",
  "familyName": "Doe Family",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-10-10T12:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Validation error
- `409 Conflict` - Email already exists

---

#### 1.2 Register Child
Creates a child account and associates with existing family.

**Endpoint:** `POST /api/v1/auth/register/child`
**Authentication:** Required (Parent role only)

**Request Body:**
```json
{
  "email": "child@example.com",
  "password": "ChildPass123!",
  "firstName": "Alice",
  "lastName": "Doe",
  "weeklyAllowance": 15.00
}
```

**Response (201 Created):**
```json
{
  "userId": "guid",
  "childId": "guid",
  "email": "child@example.com",
  "firstName": "Alice",
  "lastName": "Doe",
  "role": "Child",
  "familyId": "guid",
  "weeklyAllowance": 15.00,
  "currentBalance": 0.00
}
```

**Error Responses:**
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Not a parent
- `409 Conflict` - Email already exists

---

#### 1.3 Login
Authenticates user and returns JWT token.

**Endpoint:** `POST /api/v1/auth/login`
**Authentication:** None (public)

**Request Body:**
```json
{
  "email": "parent@example.com",
  "password": "SecurePass123!",
  "rememberMe": false
}
```

**Response (200 OK):**
```json
{
  "userId": "guid",
  "email": "parent@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Parent",
  "familyId": "guid",
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAt": "2025-10-10T12:00:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid request
- `401 Unauthorized` - Invalid credentials

---

#### 1.4 Get Current User
Returns current authenticated user information.

**Endpoint:** `GET /api/v1/auth/me`
**Authentication:** Required

**Response (200 OK):**
```json
{
  "userId": "guid",
  "email": "parent@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "Parent",
  "familyId": "guid",
  "familyName": "Doe Family"
}
```

**Error Responses:**
- `401 Unauthorized` - Not authenticated

---

### 2. Families API (`/api/v1/families`)

#### 2.1 Get Current Family
Returns current user's family information.

**Endpoint:** `GET /api/v1/families/current`
**Authentication:** Required

**Response (200 OK):**
```json
{
  "id": "guid",
  "name": "Doe Family",
  "createdAt": "2025-01-01T00:00:00Z",
  "memberCount": 3,
  "childrenCount": 2
}
```

---

#### 2.2 Get Family Members
Returns all members of current user's family.

**Endpoint:** `GET /api/v1/families/current/members`
**Authentication:** Required

**Response (200 OK):**
```json
{
  "familyId": "guid",
  "familyName": "Doe Family",
  "members": [
    {
      "userId": "guid",
      "email": "parent@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "Parent"
    },
    {
      "userId": "guid",
      "email": "child@example.com",
      "firstName": "Alice",
      "lastName": "Doe",
      "role": "Child"
    }
  ]
}
```

---

#### 2.3 Get Family Children
Returns all children in current user's family with balances.

**Endpoint:** `GET /api/v1/families/current/children`
**Authentication:** Required

**Response (200 OK):**
```json
{
  "familyId": "guid",
  "familyName": "Doe Family",
  "children": [
    {
      "childId": "guid",
      "userId": "guid",
      "firstName": "Alice",
      "lastName": "Doe",
      "email": "child@example.com",
      "currentBalance": 125.50,
      "weeklyAllowance": 15.00,
      "lastAllowanceDate": "2025-10-02T00:00:00Z",
      "nextAllowanceDate": "2025-10-09T00:00:00Z"
    }
  ]
}
```

---

### 3. Children API (`/api/v1/children`)

#### 3.1 Get Child by ID
Returns detailed information about a specific child.

**Endpoint:** `GET /api/v1/children/{childId}`
**Authentication:** Required (Parent or the child themselves)

**Response (200 OK):**
```json
{
  "childId": "guid",
  "userId": "guid",
  "firstName": "Alice",
  "lastName": "Doe",
  "email": "child@example.com",
  "currentBalance": 125.50,
  "weeklyAllowance": 15.00,
  "lastAllowanceDate": "2025-10-02T00:00:00Z",
  "nextAllowanceDate": "2025-10-09T00:00:00Z",
  "createdAt": "2025-01-01T00:00:00Z"
}
```

**Error Responses:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Not authorized to access this child
- `404 Not Found` - Child not found

---

#### 3.2 Update Child Allowance
Updates weekly allowance for a child (Parent only).

**Endpoint:** `PUT /api/v1/children/{childId}/allowance`
**Authentication:** Required (Parent role only)

**Request Body:**
```json
{
  "weeklyAllowance": 20.00
}
```

**Response (200 OK):**
```json
{
  "childId": "guid",
  "weeklyAllowance": 20.00,
  "message": "Weekly allowance updated successfully"
}
```

**Error Responses:**
- `400 Bad Request` - Invalid amount
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Not a parent or not same family
- `404 Not Found` - Child not found

---

#### 3.3 Delete Child
Removes a child from the family (Parent only).

**Endpoint:** `DELETE /api/v1/children/{childId}`
**Authentication:** Required (Parent role only)

**Response (204 No Content)**

**Error Responses:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Not a parent or not same family
- `404 Not Found` - Child not found

---

### 4. Transactions API (`/api/v1/transactions`)

#### 4.1 Get Child Transactions
Returns transaction history for a child.

**Endpoint:** `GET /api/v1/transactions/children/{childId}`
**Authentication:** Required
**Query Parameters:**
- `limit` (optional, default: 20) - Number of transactions to return
- `offset` (optional, default: 0) - Pagination offset

**Response (200 OK):**
```json
{
  "childId": "guid",
  "totalCount": 45,
  "limit": 20,
  "offset": 0,
  "transactions": [
    {
      "id": "guid",
      "childId": "guid",
      "amount": 25.00,
      "type": "Credit",
      "description": "Completed chores",
      "balanceAfter": 125.50,
      "createdBy": "guid",
      "createdByName": "John Doe",
      "createdAt": "2025-10-09T10:30:00Z"
    }
  ]
}
```

---

#### 4.2 Create Transaction
Creates a new transaction (Parent only).

**Endpoint:** `POST /api/v1/transactions`
**Authentication:** Required (Parent role only)

**Request Body:**
```json
{
  "childId": "guid",
  "amount": 25.00,
  "type": "Credit",
  "description": "Completed weekly chores"
}
```

**Response (201 Created):**
```json
{
  "id": "guid",
  "childId": "guid",
  "amount": 25.00,
  "type": "Credit",
  "description": "Completed weekly chores",
  "balanceAfter": 150.50,
  "createdBy": "guid",
  "createdAt": "2025-10-09T10:30:00Z"
}
```

**Error Responses:**
- `400 Bad Request` - Validation error or insufficient funds
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Not a parent
- `404 Not Found` - Child not found

---

#### 4.3 Get Child Balance
Returns current balance for a child.

**Endpoint:** `GET /api/v1/transactions/children/{childId}/balance`
**Authentication:** Required

**Response (200 OK):**
```json
{
  "childId": "guid",
  "currentBalance": 125.50,
  "weeklyAllowance": 15.00,
  "lastAllowanceDate": "2025-10-02T00:00:00Z"
}
```

---

### 5. Dashboard API (`/api/v1/dashboard`)

#### 5.1 Get Parent Dashboard
Returns summary for parent dashboard.

**Endpoint:** `GET /api/v1/dashboard/parent`
**Authentication:** Required (Parent role only)

**Response (200 OK):**
```json
{
  "familyName": "Doe Family",
  "totalChildren": 2,
  "totalBalance": 250.75,
  "totalWeeklyAllowance": 30.00,
  "children": [
    {
      "childId": "guid",
      "firstName": "Alice",
      "lastName": "Doe",
      "currentBalance": 125.50,
      "weeklyAllowance": 15.00,
      "recentTransactionCount": 5
    },
    {
      "childId": "guid",
      "firstName": "Bob",
      "lastName": "Doe",
      "currentBalance": 125.25,
      "weeklyAllowance": 15.00,
      "recentTransactionCount": 3
    }
  ]
}
```

---

#### 5.2 Get Child Dashboard
Returns summary for child dashboard.

**Endpoint:** `GET /api/v1/dashboard/child`
**Authentication:** Required (Child role only)

**Response (200 OK):**
```json
{
  "childId": "guid",
  "firstName": "Alice",
  "currentBalance": 125.50,
  "weeklyAllowance": 15.00,
  "lastAllowanceDate": "2025-10-02T00:00:00Z",
  "nextAllowanceDate": "2025-10-09T00:00:00Z",
  "daysUntilNextAllowance": 0,
  "recentTransactions": [
    {
      "id": "guid",
      "amount": 25.00,
      "type": "Credit",
      "description": "Completed chores",
      "balanceAfter": 125.50,
      "createdAt": "2025-10-09T10:30:00Z"
    }
  ],
  "monthlyStats": {
    "totalEarned": 100.00,
    "totalSpent": 25.00,
    "netChange": 75.00
  }
}
```

---

## Error Response Format

All error responses follow this format:

```json
{
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message",
    "details": {
      "field": "Additional error details"
    }
  }
}
```

### Common Error Codes
- `VALIDATION_ERROR` - Request validation failed
- `AUTHENTICATION_REQUIRED` - User not authenticated
- `FORBIDDEN` - User not authorized
- `NOT_FOUND` - Resource not found
- `CONFLICT` - Resource already exists
- `INSUFFICIENT_FUNDS` - Transaction would result in negative balance
- `INTERNAL_ERROR` - Server error

---

## Rate Limiting

- **Authenticated requests:** 1000 requests per hour
- **Unauthenticated requests:** 100 requests per hour

Rate limit headers:
```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1633024800
```

---

## Pagination

List endpoints support pagination:
- `limit` - Number of items per page (default: 20, max: 100)
- `offset` - Number of items to skip (default: 0)

Response includes pagination metadata:
```json
{
  "totalCount": 150,
  "limit": 20,
  "offset": 40,
  "items": []
}
```

---

## Example API Usage

### Register and Login Flow

```bash
# 1. Register parent
curl -X POST https://api.allowancetracker.com/api/v1/auth/register/parent \
  -H "Content-Type: application/json" \
  -d '{
    "email": "parent@example.com",
    "password": "SecurePass123!",
    "firstName": "John",
    "lastName": "Doe",
    "familyName": "Doe Family"
  }'

# Response includes JWT token
# {
#   "token": "eyJhbGciOiJIUzI1NiIs...",
#   "userId": "...",
#   ...
# }

# 2. Use token for authenticated requests
TOKEN="eyJhbGciOiJIUzI1NiIs..."

# 3. Add a child
curl -X POST https://api.allowancetracker.com/api/v1/auth/register/child \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "email": "alice@example.com",
    "password": "ChildPass123!",
    "firstName": "Alice",
    "lastName": "Doe",
    "weeklyAllowance": 15.00
  }'

# 4. Create a transaction
curl -X POST https://api.allowancetracker.com/api/v1/transactions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "childId": "child-guid",
    "amount": 25.00,
    "type": "Credit",
    "description": "Completed chores"
  }'

# 5. Get dashboard
curl -X GET https://api.allowancetracker.com/api/v1/dashboard/parent \
  -H "Authorization: Bearer $TOKEN"
```

---

## Testing Endpoints

All endpoints have comprehensive test coverage:
- Unit tests for business logic
- Integration tests for API controllers
- Authentication/authorization tests
- Error handling tests

Target: >95% code coverage for API layer

---

## API Versioning

Current version: `v1`

Future versions will be accessible via `/api/v2/...` while maintaining backward compatibility with v1.

---

## WebSocket Support (Future)

Real-time updates via SignalR hub:
- Connection: `/hub/family`
- Events: `TransactionCreated`, `BalanceUpdated`, `AllowancePaid`

---

## Summary

This REST API provides complete programmatic access to all Allowance Tracker features, enabling:
- Mobile app development (iOS/Android)
- Third-party integrations
- Automation and scripting
- Webhook integration (future)

All endpoints follow REST best practices with proper status codes, error handling, and JWT authentication.
