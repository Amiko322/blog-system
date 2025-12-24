# BlogSystem

## Introduction
BlogSystem is a service for managing users and their posts.
The system provides operations for user registration, authentication, retrieving user information, and creating and managing posts.

## Installation
Follow the steps below to install and run the application.

1. Clone the repo
   ```
   git clone https://github.com/username/blog-system.git
   ```
2. Navigate into project folder
    ```
    cd BlogSystem/BlogSystem
    ```
3. Start required services (Redis)
    ```
    docker run -d -p 6379:6379 --name redis redis:latest
    ```
4. Run the application
    ```
    dotnet run --project BlogSystem.csproj
    ```
5. Open Swagger UI
    ```
    http://localhost:5066/swagger
    ```
## REST API
The REST API to the example app is described below.
### [1] Register
#### Request
`POST /api/v2/auth/register`
```
curl -X POST \
  'http://localhost:5066/api/v2/auth/register' \
  -H 'accept: application/json' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000000' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "user",
    "password": "user",
    "lastName": "ivanov",
    "firstName": "ivan"
  }'
```
#### Response
```
{
  "id": "<USER_ID>",
  "login": "user",
  "lastName": "ivanov",
  "firstName": "ivan",
  "registeredAt": "<ISO_TIMESTAMP>"
}
```

### [2] Login
#### Request
`POST /api/v2/auth/login`
```
curl -X POST \
  'http://localhost:5066/api/v2/auth/login' \
  -H 'accept: application/json' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "user",
    "password": "user"
  }'
```
#### Response body
```
{
  "access": "<YOUR_ACCESS_TOKEN>"
}
```

### [3] Get list of Users
#### Request
`GET /api/v2/users`
```
curl -X GET \
  'http://localhost:5066/api/v2/users?fields=id,login,firstname&pageNumber=1&pageSize=4' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
[
  {
    "id": "<USER_ID>",
    "login": "user",
    "firstName": "ivan"
  }
]
```

### [4] Create post
#### Request
`POST /api/v2/posts`
```
curl -X POST \
  'http://localhost:5066/api/v2/posts' \
  -H 'accept: application/json' \
  -H 'IdempotencyKey: 00000000-0000-0000-0000-000000000001' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>' \
  -H 'Content-Type: application/json' \
  -d '{
    "title": "blog",
    "content": "my life",
    "userId": "<USER_ID>"
}'
```
#### Response body
```
{
  "id": "<POST_ID>",
  "title": "blog",
  "content": "my life",
  "createdAt": "<ISO_TIMESTAMP>",
  "userId": "<USER_ID>"
}
```

### [5] Get post by ID
#### Request
`GET /api/v2/posts/{id}`
```
curl -X GET \
  'http://localhost:5066/api/v2/posts/<POST_ID>?fields=id,title,content' \
  -H 'accept: application/json' \
  -H 'Authorization: Bearer <YOUR_ACCESS_TOKEN>'
```
#### Response body
```
{
  "statusCode": 429,
  "message": "Too many requests. Please try again later."
}
```
#### Response headers
```
retry-after: 10
x-limit-remaining: 0
```