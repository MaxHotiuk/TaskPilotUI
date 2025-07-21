# TaskPilot API Documentation for Frontend Developers

Welcome to the TaskPilot API documentation! This guide provides comprehensive information about all available endpoints, request/response formats, and data models for frontend integration.

## Base URL
```
https://your-api-domain.com
```

## Authentication
TaskPilot uses Azure AD (Microsoft Identity) for authentication with JWT Bearer tokens.

### Headers Required
```
Authorization: Bearer {jwt_token}
Content-Type: application/json
```

## Data Models (DTOs)

### UserDto
```typescript
interface UserDto {
  id: string;           // Guid
  entraId: string;      // Azure AD Object ID
  username: string;
  email: string;
  role: string;         // "User" | "Admin"
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

### BoardDto
```typescript
interface BoardDto {
  id: string;           // Guid
  name: string;
  description?: string; // Optional
  ownerId: string;      // Guid
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

### TaskItemDto
```typescript
interface TaskItemDto {
  id: string;           // Guid
  boardId: string;      // Guid
  title: string;
  description?: string; // Optional
  stateId: number;      // Integer
  assigneeId?: string;  // Optional Guid
  dueDate?: string;     // Optional ISO 8601 DateTime
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

### StateDto
```typescript
interface StateDto {
  id: number;           // Integer
  boardId: string;      // Guid
  name: string;
  order: number;        // Integer for sorting
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

### BoardMemberDto
```typescript
interface BoardMemberDto {
  boardId: string;      // Guid
  userId: string;       // Guid
  role: string;         // "Member" | "Owner"
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

### CommentDto
```typescript
interface CommentDto {
  id: string;           // Guid
  taskId: string;       // Guid
  authorId: string;     // Guid
  content: string;
  createdAt: string;    // ISO 8601 DateTime
  updatedAt: string;    // ISO 8601 DateTime
}
```

## API Endpoints

### 🔐 Authentication & Users

#### Get Current User
```http
GET /api/users/me
```
**Authorization:** Required (User role)  
**Response:** `UserDto | null`

#### Get All Users
```http
GET /api/users
```
**Authorization:** Required (Admin role)  
**Response:** `UserDto[]`

#### Get User by ID
```http
GET /api/users/{id}
```
**Authorization:** Required  
**Parameters:**
- `id` (path): User ID (Guid)

**Response:** `UserDto | null`

#### Get User by Email
```http
GET /api/users/by-email?email={email}
```
**Authorization:** Required  
**Parameters:**
- `email` (query): User email address

**Response:** `UserDto | null`

#### Create User
```http
POST /api/users
```
**Authorization:** Required (User role)  
**Request Body:**
```typescript
interface CreateUserRequest {
  entraId: string;
  email: string;
  username: string;
  role: string;
}
```
**Response:** `string` (User ID)

#### Update User
```http
PUT /api/users/{id}
```
**Authorization:** Required  
**Parameters:**
- `id` (path): User ID (Guid)

**Request Body:**
```typescript
interface UpdateUserRequest {
  email: string;
  username: string;
  role: string;
}
```
**Response:** `204 No Content`

#### Update User Role
```http
PUT /api/users/{userId}/role
```
**Authorization:** Required (Admin role)  
**Parameters:**
- `userId` (path): User ID (Guid)

**Request Body:**
```typescript
interface UpdateUserRoleRequest {
  role: string;
}
```
**Response:** `204 No Content`

#### Delete User
```http
DELETE /api/users/{id}
```
**Authorization:** Required (Admin role)  
**Parameters:**
- `id` (path): User ID (Guid)

**Response:** `204 No Content`

### 📋 Boards

#### Get All Boards
```http
GET /api/boards
```
**Authorization:** Required (Admin role)  
**Response:** `BoardDto[]`

#### Get Board by ID
```http
GET /api/boards/{id}
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `id` (path): Board ID (Guid)

**Response:** `BoardDto | null`

#### Get Boards by User ID
```http
GET /api/users/{userId}/boards
```
**Authorization:** Required (User role)  
**Parameters:**
- `userId` (path): User ID (Guid)

**Response:** `BoardDto[]`

#### Create Board
```http
POST /api/boards
```
**Authorization:** Required (User role)  
**Request Body:**
```typescript
interface CreateBoardRequest {
  name: string;
  description?: string;
  ownerId: string; // Guid
}
```
**Response:** `string` (Board ID)

#### Update Board
```http
PUT /api/boards/{id}
```
**Authorization:** Required (Board owner)  
**Parameters:**
- `id` (path): Board ID (Guid)

**Request Body:**
```typescript
interface UpdateBoardRequest {
  name: string;
  description?: string;
}
```
**Response:** `204 No Content`

#### Delete Board
```http
DELETE /api/boards/{id}
```
**Authorization:** Required (Board owner)  
**Parameters:**
- `id` (path): Board ID (Guid)

**Response:** `204 No Content`

### 👥 Board Members

#### Get Board Members
```http
GET /api/boards/{boardId}/members
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)

**Response:** `BoardMemberDto[]`

#### Add Board Member
```http
POST /api/boards/{boardId}/members
```
**Authorization:** Required (Board owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)

**Request Body:**
```typescript
interface AddBoardMemberRequest {
  userId: string;     // Guid
  role?: string;      // Default: "Member"
}
```
**Response:** `204 No Content`

#### Update Board Member Role
```http
PUT /api/boards/{boardId}/members/{userId}/role
```
**Authorization:** Required (Board owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)
- `userId` (path): User ID (Guid)

**Request Body:**
```typescript
interface UpdateBoardMemberRoleRequest {
  role: string;
}
```
**Response:** `204 No Content`

#### Remove Board Member
```http
DELETE /api/boards/{boardId}/members/{userId}
```
**Authorization:** Required (Board owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)
- `userId` (path): User ID (Guid)

**Response:** `204 No Content`

### 📝 States (Task Columns)

#### Get States by Board ID
```http
GET /api/boards/{boardId}/states
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)

**Response:** `StateDto[]` (Ordered by `order` field)

#### Create State
```http
POST /api/boards/{boardId}/states
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)

**Request Body:**
```typescript
interface CreateStateRequest {
  name: string;
  order: number;
}
```
**Response:** `number` (State ID)

### ✅ Tasks

#### Get All Tasks
```http
GET /api/tasks
```
**Authorization:** Required  
**Response:** `TaskItemDto[]`

#### Get Task by ID
```http
GET /api/tasks/{id}
```
**Authorization:** Required  
**Parameters:**
- `id` (path): Task ID (Guid)

**Response:** `TaskItemDto | null`

#### Get Tasks by Board ID
```http
GET /api/boards/{boardId}/tasks
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `boardId` (path): Board ID (Guid)

**Response:** `TaskItemDto[]`

#### Create Task
```http
POST /api/tasks
```
**Authorization:** Required (Board member or owner)  
**Request Body:**
```typescript
interface CreateTaskRequest {
  boardId: string;      // Guid
  title: string;
  description?: string;
  stateId: number;
  assigneeId?: string;  // Optional Guid
  dueDate?: string;     // Optional ISO 8601 DateTime
}
```
**Response:** `string` (Task ID)

#### Update Task
```http
PUT /api/tasks/{id}
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `id` (path): Task ID (Guid)

**Request Body:**
```typescript
interface UpdateTaskRequest {
  title: string;
  description?: string;
  stateId: number;
  assigneeId?: string;  // Optional Guid
  dueDate?: string;     // Optional ISO 8601 DateTime
}
```
**Response:** `204 No Content`

#### Delete Task
```http
DELETE /api/tasks/{id}
```
**Authorization:** Required (Board member or owner)  
**Parameters:**
- `id` (path): Task ID (Guid)

**Response:** `204 No Content`

## HTTP Status Codes

### Success Codes
- `200 OK` - Request successful with data
- `201 Created` - Resource created successfully
- `204 No Content` - Request successful, no content returned

### Error Codes
- `400 Bad Request` - Invalid request data
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

## Error Response Format
```typescript
interface ErrorResponse {
  type: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
```

## Authorization Policies

### Role-Based Access
- **Admin**: Full access to all resources
- **User**: Standard user access

### Resource-Based Access
- **Board Owner**: Full control over owned boards and their resources
- **Board Member**: Can view and modify board content (tasks, states)
- **Authenticated User**: Can access their own profile and create boards

## Best Practices for Frontend Integration

### 1. Error Handling
Always handle different HTTP status codes appropriately:

```typescript
try {
  const response = await fetch('/api/boards', {
    headers: {
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    }
  });
  
  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.detail || 'Request failed');
  }
  
  const boards = await response.json();
  return boards;
} catch (error) {
  console.error('Failed to fetch boards:', error);
  throw error;
}
```

### 2. Date Handling
All dates are in ISO 8601 format. Convert to local time as needed:

```typescript
const task: TaskItemDto = await fetchTask(id);
const localDueDate = task.dueDate ? new Date(task.dueDate) : null;
```

### 3. Optimistic Updates
For better UX, implement optimistic updates for state changes:

```typescript
// Update UI immediately
updateTaskInState(updatedTask);

try {
  await updateTask(task.id, updateData);
} catch (error) {
  // Revert on error
  revertTaskInState(originalTask);
  showError('Failed to update task');
}
```

### 4. Batch Operations
When possible, batch related operations to reduce API calls.

### 5. Caching
Implement appropriate caching strategies for frequently accessed data like user profiles and board lists.

## WebSocket/Real-time Updates
Currently, the API doesn't include real-time features. Consider implementing polling for real-time-like updates:

```typescript
// Poll for task updates every 30 seconds
setInterval(async () => {
  if (activeBoardId) {
    const updatedTasks = await fetchTasksByBoardId(activeBoardId);
    updateTasksInState(updatedTasks);
  }
}, 30000);
```

## Rate Limiting
Be mindful of API rate limits. Implement appropriate delays between requests and handle rate limit responses gracefully.

---

This documentation covers all available endpoints in the TaskPilot API. For additional questions or issues, please refer to the API's Swagger documentation at `/swagger` endpoint when running in development mode.
