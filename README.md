# Thmanyah.CSharp

## Project Structure

- **src/Thmanyah.Api/**: Main API project, contains controllers, startup, and API-specific services.
- **src/Thmanyah.Cms/**: CMS module, manages programs and episodes, with its own domain, infrastructure, and services.
- **src/Thmanyah.Discovery/**: Discovery module, handles read-optimized models and queries for fast content discovery.
- **Thmanyah.Shared/**: Shared configurations and code used across modules.

## Problem Statement

The project aims to build a modular, scalable backend for a content platform, supporting authentication, content management (CMS), and fast discovery/search. The challenge is to keep each module independent, maintainable, and easy to scale, while sharing common infrastructure and configuration.

## Solution Approach

- **Modularization**: Each module (Auth, CMS, Discovery) is responsible for its own configuration and dependency registration, using extension methods (e.g., `AddAuthModule`).
- **Separation of Concerns**: Each module has its own DbContext, services, and domain logic, reducing coupling and making it easier to maintain and test.
- **Centralized Startup**: The main `Program.cs` only wires up modules and global middleware, keeping it clean and easy to extend.
- **Database Initialization**: On startup, the app ensures all databases are created and seeds initial roles and admin users.
- **Design Patterns**: The solution uses the Factory pattern (e.g., for episode storage strategies), Dependency Injection, and extension methods for modular registration.
- **Event-Driven Architecture**: The CMS publishes domain events, and the Discovery module subscribes to these events to update its read models, enabling eventual consistency and scalability.
- **Caching Mechanism**: The Discovery module uses a cache-aside pattern with Redis or in-memory caching to accelerate read operations and reduce database load.

## Strengths & Scalability

- **Clear Boundaries**: Modules can be developed, tested, and deployed independently.
- **Easy to Extend**: New modules or features can be added with minimal changes to the core API.
- **Scalable**: Each module can be moved to its own service or database as the system grows, supporting microservices or distributed architectures.
- **Maintainable**: Clean separation and modular registration make the codebase easy to understand and maintain.
- **Performance**: Caching and event-driven updates ensure fast reads and up-to-date data without overloading the database.

---

This structure is ideal for teams looking to grow their platform, add new features, or scale out services as demand increases.

## How to Run It

1. **Clone the repository**
2. **Set up PostgreSQL** and create the required databases. You can use the script in `docs/create_inter_database.sql` to create the `inter` database:

	```sql
	-- in psql or your SQL tool
	\i docs/create_inter_database.sql
	```

3. **Configure connection strings** in `appsettings.json` as needed for your environment.
4. **Build and run the solution**:
	- Using Visual Studio: Press F5 or use the Run button.
	- Using CLI:
	  ```sh
	  dotnet build
	  dotnet run --project src/Thmanyah.Api/Thmanyah.Api.csproj
	  ```
5. **Access the API** at the URL shown in the console (default: `https://localhost:57310/swagger` for Swagger UI).

---
