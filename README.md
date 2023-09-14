# Chat Application Backend

This is the backend implementation of a minimal chat application using ASP.NET Core and Entity Framework. The goal of this project is to provide a set of APIs that enable user registration, authentication, conversation management, and message handling.

## Tech Stack

- ASP.NET Core 7
- EF Core 7 with Code-First approach
- SQL Server Express
- JWT-based Authentication

## Project Structure

The project is organized as follows:

- **Controllers**: Contains API controllers for user registration, login, user listing, message management, and log retrieval.

- **Models**: Defines the data models used in the application, such as User and Message.

- **Services**: Contains the business logic for user registration, authentication, message handling, and logging.

- **Data**: Includes the database context and migration files for Entity Framework.

- **Middleware**: Contains the custom request-logging middleware for capturing API request details.

- **Repositories**: Implements the repository pattern for data access.

## Implemented Functionalities

### User Registration

- **API Endpoint:** POST /api/register
- **Request Parameters:** email, name, password
- **Responses:** 200 OK, 400 Bad Request, 409 Conflict
- **Response Body:** userId, name, email (in case of success), error (in case of failure)
- Passwords are securely hashed and stored in the database.

### User Login

- **API Endpoint:** POST /api/login
- **Request Parameters:** email, password
- **Responses:** 200 OK, 400 Bad Request, 401 Unauthorized
- **Response Body:** token (JWT token), profile (user details), error (in case of failure)
- JWT token is generated upon successful login and used for authentication.

### Retrieve User List

- **API Endpoint:** GET /api/users
- **Responses:** 200 OK, 401 Unauthorized
- **Response Body:** users (array of user objects), error (in case of failure)
- The list of users does not include the user making the request.

### Send Message

- **API Endpoint:** POST /api/messages
- **Request Parameters:** receiverId, content
- **Request Headers:** Authorization (Bearer token)
- **Responses:** 200 OK, 400 Bad Request, 401 Unauthorized
- **Response Body:** messageId, senderId, receiverId, content, timestamp (in case of success), error (in case of failure)

### Edit Message

- **API Endpoint:** PUT /api/messages/{messageId}
- **Request Parameters:** messageId, content
- **Request Headers:** Authorization (Bearer token)
- **Responses:** 200 OK, 400 Bad Request, 401 Unauthorized, 404 Not Found
- **Response Body:** error (in case of failure)
- Users can only edit their own messages.

### Delete Message

- **API Endpoint:** DELETE /api/messages/{messageId}
- **Request Parameters:** messageId
- **Request Headers:** Authorization (Bearer token)
- **Responses:** 200 OK, 401 Unauthorized, 404 Not Found
- **Response Body:** error (string): Error message indicating the cause of the failure

Note: Users can only delete messages sent by them and not by other users.

### Retrieve Conversation History

- **API Endpoint:** GET /api/messages
- **Request Parameters:** userId, before, count, sort
- **Request Headers:** Authorization (Bearer token)
- **Responses:** 200 OK, 400 Bad Request, 401 Unauthorized, 404 Not Found
- **Response Body:** messages (array of message objects), error (in case of failure)

### Request-Logging Middleware

- Custom middleware logs API requests with details like IP, request body, time, and username (if authenticated).
- API to fetch logs:
  - **Endpoint:** GET /api/log
  - **Request Parameters:** EndTime, StartTime
  - **Request Headers:** Authorization (Bearer token)
  - **Responses:** 200 OK, 400 Bad Request, 401 Unauthorized, 404 Not Found
  - **Response Body:** Logs (array of log objects), error (in case of failure)

## Getting Started

1. Clone this repository to your local machine.
2. Set up the database connection string in `appsettings.json` to SQL Server.
3. Run the Entity Framework migrations to create the database: `dotnet ef database update`.
4. Build and run the application using `dotnet run`.
5. Access the API endpoints as described above.

## Contributing

1. Fork the repository.
2. Create a feature branch (`git checkout -b feature/your-feature-name`).
3. Commit your changes (`git commit -m 'Add some feature'`).
4. Push to the branch (`git push origin feature/your-feature-name`).
5. Create a new Pull Request.

## Code Review

To review the code, please check the `dev` branch. All code should be merged into the `dev` branch for testing and review before being merged into the `main` branch for production deployment.

Your feedback and contributions are highly appreciated!

## License

This project is licensed under the [MIT License](LICENSE).
