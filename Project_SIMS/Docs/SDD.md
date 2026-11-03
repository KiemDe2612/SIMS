# Software Design Document (SDD) - SIMS

## 1. System Architecture
The SIMS project uses **Clean Architecture** (Onion Architecture) separated into 6 projects to ensure high maintainability, testability, and strict adherence to the Dependency Inversion Principle (DIP).

```
[ SIMS.Web ] (Presentation: ASP.NET Core MVC, Composition Root)
     │   ╲
     │    ╲──────────┐
     ▼               ▼
[ SIMS.Persistence ] [ SIMS.Infrastructure ]
     │               │
     ▼               ▼
[ SIMS.Application ] (Use Cases, DTOs, Services, Patterns)
     │
     ▼
[ SIMS.Domain ] (Core Entities, Repository Interfaces, Constants)
     ▲
     │
[ SIMS.Tests ] (xUnit, Moq, FluentAssertions, SQLite In-Memory)
```

### 1.1 Layers & Project Dependencies
- **`SIMS.Domain`**: Core enterprise business logic and types.
  - Contains entities (`BaseEntity`, `AppUser`, `Student`, `Course`, `Faculty`, `Enrollment`, `Semester`, `AcademicRecord`, `AuditLog`, `Department`).
  - Contains repository interfaces (`IGenericRepository<T>`, `IUnitOfWork`) and domain constants (`AppConstants`, `AppRoles`).
  - *Dependencies:* Has NO dependencies on any other project in the solution.
- **`SIMS.Application`**: Application business logic and use case coordination.
  - Defines DTOs (`StudentDtos`, `CourseDtos`, `ExportDtos`, etc.).
  - Interfaces and services (`IStudentService`, `ICourseService`, `IFacultyService`, `IStudentPortalService`, `ITranscriptService`).
  - Behavioral & creational pattern abstractions: `IGpaCalculationStrategy` (Strategy), `IUserFactory` (Factory Method), `IEnrollmentObserver` / `IEnrollmentSubject` (Observer).
  - *Dependencies:* Depends strictly only on `SIMS.Domain`.
- **`SIMS.Infrastructure`**: External concerns, technical implementations, and structural patterns.
  - Implements `IExportService` with multi-format exporters: `CsvExporter` (RFC 4180 BOM & CSV formula injection escape), `ExcelExporter` (ClosedXML), and `PdfExporter` (QuestPDF).
  - Implements Decorator pattern: `CachingStudentRepository` (decorates `IGenericRepository<Student>` with `IMemoryCache`).
  - *Dependencies:* Depends on `SIMS.Application` (and transitively `SIMS.Domain`).
- **`SIMS.Persistence`**: Data persistence and database infrastructure.
  - Implements Entity Framework Core: `ApplicationDbContext`, historical migrations, seed data (`DbInitializer`), Global Query Filters for soft deletion (`!e.IsDeleted`).
  - Implements `GenericRepository<T>` and `UnitOfWork`.
  - *Dependencies:* Depends on `SIMS.Application` (and transitively `SIMS.Domain`).
- **`SIMS.Web`**: Presentation layer and Composition Root.
  - ASP.NET Core 8 MVC with Controllers (`StudentsController`, `CoursesController`, `FacultyController`, `StudentPortalController`, `AccountController`), Razor Views, and ASP.NET Core Identity authentication.
  - Configures Dependency Injection in `Program.cs`.
  - *Dependencies:* References `SIMS.Application`, `SIMS.Infrastructure`, `SIMS.Persistence`. *(Note: Does not directly reference `SIMS.Domain` in `.csproj`, receiving it transitively via `SIMS.Application`)*.
- **`SIMS.Tests`**: Automated test suite.
  - Unit tests and Integration tests with SQLite in-memory.
  - *Dependencies:* References `SIMS.Domain`, `SIMS.Application`, `SIMS.Infrastructure`, `SIMS.Persistence`.

---

## 2. Design Patterns Implemented

| Design Pattern | Category | Implementation & Role in SIMS |
| :--- | :--- | :--- |
| **Strategy Pattern** | Behavioral | `IGpaCalculationStrategy` with `TenPointStrategy` and `FourPointStrategy`. Injected and dynamically switched via `ITranscriptService` (`TranscriptService`) to calculate semester GPA, cumulative CPA, and academic honors. |
| **Factory Method Pattern** | Creational | `IUserFactory` & `UserFactory`. Encapsulates creation of `AppUser` accounts and role assignments (`Administrator`, `Faculty`, `Student`) away from MVC controllers. |
| **Decorator Pattern** | Structural | `CachingStudentRepository` wraps `IGenericRepository<Student>`, transparently intercepting `GetByIdAsync` to cache student profiles using `IMemoryCache`. |
| **Observer Pattern** | Behavioral | `IEnrollmentSubject` & `EnrollmentSubject` publish `EnrollmentCreatedEvent` to registered observers (`AuditLogEnrollmentObserver` for audit logging and `NotificationEnrollmentObserver` for notifications). |
| **Repository & Unit of Work** | Structural / Data Access | `IGenericRepository<T>` abstracts CRUD operations; `IUnitOfWork` coordinates transactional consistency via `CompleteAsync()`. |
| **Exporter Pattern** | Structural | Dedicated format exporters (`CsvExporter`, `ExcelExporter`, `PdfExporter`) coordinated by `ExportService` for role-based exports with CSV injection protection. |

---

## 3. Database Design
### 3.1 Entities
- `BaseEntity`: `Id` (Guid), `CreatedAt` (DateTime), `UpdatedAt` (DateTime?), `IsDeleted` (bool for soft deletion).
- `AppUser`: Extends `IdentityUser` with `FirstName`, `LastName`, `IsActive`.
- `Student`: `StudentId`, `DateOfBirth`, `Address`, `EnrollmentDate`, `DepartmentId`, `UserId`.
- `Faculty`: `FacultyId`, `Title`, `HireDate`, `DepartmentId`, `UserId`.
- `Course`: `Code`, `Title`, `Description`, `Credits`, `DepartmentId`, `AssignedFacultyId`, `PrerequisiteCourseId`.
- `Enrollment`: `StudentId`, `CourseId`, `SemesterId`, `EnrollmentDate`, `Grade` (nullable double).
- `Department`: `Code`, `Name`.
- `Semester`: `Name`, `StartDate`, `EndDate`, `IsActive`.
- `AcademicRecord`: `StudentId`, `SemesterId`, `GPA`, `CPA`, `AcademicStatus`.
- `AuditLog`: `UserId`, `Action`, `EntityName`, `EntityId`, `Changes`, `CreatedAt`.

### 3.2 Relationships
- `Department` 1 - N `Faculty` (Aggregation)
- `Department` 1 - N `Student` (Aggregation)
- `Department` 1 - N `Course` (Aggregation)
- `Course` 1 - N `Enrollment` (Composition)
- `Student` 1 - N `Enrollment`
- `Semester` 1 - N `Enrollment`
- `Faculty` 0..1 - N `Course` (AssignedFaculty)
- `Course` 0..1 - N `Course` (PrerequisiteCourse self-referencing)
- `AppUser` 1 - 0..1 `Student` / `Faculty`

---

## 4. SOLID Principles Addressed
- **SRP (Single Responsibility)**:
  - Controllers only handle HTTP concerns and delegate user creation to `IUserFactory`, export to `IExportService`, and business rules to Application services.
  - Observers handle decoupled side effects (audit logging, notifications) independently.
- **OCP (Open/Closed)**:
  - Adding new grading scales (e.g., Letter grades A/B/C/D) only requires implementing `IGpaCalculationStrategy` without touching existing GPA logic.
  - Adding new export formats only requires implementing a new exporter class.
- **LSP (Liskov Substitution)**:
  - `CachingStudentRepository` seamlessly substitutes `IGenericRepository<Student>` in DI without altering caller expectations.
  - All domain entities cleanly inherit and conform to `BaseEntity`.
- **ISP (Interface Segregation)**:
  - Dedicated, cohesive interfaces (`IExportService`, `ITranscriptService`, `IUserFactory`, `IEnrollmentSubject`, `IEnrollmentObserver`) tailored to specific consumer requirements.
- **DIP (Dependency Inversion)**:
  - High-level modules (`SIMS.Application`, `SIMS.Web`) depend entirely on abstractions (`IGenericRepository<T>`, `IUnitOfWork`, `IExportService`), never on low-level database or file system implementations.
