# Software Design Document (SDD)
## 1. System Architecture
The SIMS project uses **Clean Architecture** (or Onion Architecture) separated into multiple projects to ensure separation of concerns and maintainability.

### 1.1 Layers
- **SIMS.Domain**: Contains enterprise logic and types. Entities (User, Student, Course, etc.), Enums, Exceptions, and Repository Interfaces. This layer has NO dependencies on any other project.
- **SIMS.Application**: Contains business logic (Use Cases, Services). Defines DTOs, ViewModels, AutoMapper profiles, FluentValidation rules. Depends only on `SIMS.Domain`.
- **SIMS.Infrastructure**: Contains external concerns like Email Services, third-party APIs. Depends on `SIMS.Application`.
- **SIMS.Persistence**: Contains Entity Framework Core implementation (DbContext, Migrations) and concrete classes for Repositories (Generic Repository, Unit of Work). Depends on `SIMS.Application`.
- **SIMS.Web**: The ASP.NET Core MVC application. It is the presentation layer containing Controllers, Views, Program.cs (Composition Root for Dependency Injection). Depends on `SIMS.Application`, `SIMS.Infrastructure`, and `SIMS.Persistence`.

## 2. Design Patterns
- **Repository & Unit of Work**: Abstracts database operations and transactions. `IGenericRepository<T>` for common CRUD, combined via `IUnitOfWork`.
- **Dependency Injection (DI)**: Extensively used to inject Repositories and Services.
- **Service Layer**: Business logic is encapsulated in services (e.g., `IStudentService`, `ICourseService`).
- **Factory Method**: Used for creating specific complex objects, particularly when mapping or initiating specialized users.
- **Strategy**: Used for GPA/CPA calculation rules (if rules vary by faculty or program).

## 3. Database Design
### 3.1 Entities
- `AppUser` (extends `IdentityUser`): Role mapping (Admin, Faculty, Student).
- `Student`: Information like DOB, Address, EnrollmentDate.
- `Faculty`: Department, HireDate.
- `Course`: Code, Title, Credits, Prerequisites.
- `Enrollment`: Link between Student and Course, Status, Grade.
- `Department`: Name, Head.

### 3.2 Relationships
- `Student` 1-N `Enrollment`
- `Course` 1-N `Enrollment`
- `Faculty` 1-N `Course`
- `Department` 1-N `Faculty`, 1-N `Course`

## 4. SOLID Principles Addressed
- **SRP (Single Responsibility)**: Each class has one reason to change. Repositories handle DB, Services handle logic, Controllers handle HTTP requests.
- **OCP (Open/Closed)**: We can add new features (like new GPA calculation rules) by creating new classes implementing interfaces, without modifying existing code.
- **LSP (Liskov Substitution)**: Subclasses (if any) can replace their base classes.
- **ISP (Interface Segregation)**: Interfaces are small and focused (`IReadRepository`, `IWriteRepository`).
- **DIP (Dependency Inversion)**: High-level modules (Application) do not depend on low-level (Persistence). Both depend on abstractions (`SIMS.Domain` interfaces).
