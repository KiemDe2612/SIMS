# BÁO CÁO PHÂN TÍCH TOÀN DIỆN SOLUTION SIMS (APDP ANALYSIS)

> **Tài liệu tham chiếu:** `Project_SIMS` (Solution SIMS.sln)  
> **Môn học:** Applied Programming and Design Principles (APDP)  
> **Mục đích:** Phân tích kiến trúc, Design Pattern, nguyên lý SOLID, đối chiếu SRS, code smells, mô hình hóa UML và đánh giá hiện trạng so với yêu cầu đề bài.

---

## 1. Sơ đồ kiến trúc hiện tại theo từng project

Dự án áp dụng mô hình **Clean Architecture (Onion Architecture)** phân tách thành 6 project:

```
[ SIMS.Web ] (Presentation: ASP.NET Core MVC, Razor Views, Composition Root)
     │   ╲
     │    ╲──────────┐
     ▼               ▼
[ SIMS.Persistence ] [ SIMS.Infrastructure ] (Exporters, Caching Decorator)
     │               │
     ▼               ▼
[ SIMS.Application ] (Use Cases, DTOs, Services, Design Patterns)
     │
     ▼
[ SIMS.Domain ] (Core Entities, Repository Interfaces, Constants)
     ▲
     │
[ SIMS.Tests ] (xUnit, Moq, FluentAssertions, SQLite In-Memory)
```

### Bảng phân tích chi tiết từng Layer:

| Project | Loại Project / Target | Trách nhiệm chính trong hệ thống | Dependencies (Phụ thuộc thực tế) |
| :--- | :--- | :--- | :--- |
| **`SIMS.Domain`** | Class Library (.NET 8) | Chứa các thực thể lõi (`BaseEntity`, `AppUser`, `Student`, `Course`, `Faculty`, `Enrollment`, `Department`, `Semester`, `AcademicRecord`, `AuditLog`), interfaces (`IGenericRepository<T>`, `IUnitOfWork`) và hằng số (`AppConstants`, `AppRoles`). | **Không phụ thuộc project nào khác** trong solution. |
| **`SIMS.Application`** | Class Library (.NET 8) | Chứa Business Logic, DTOs, Service Interfaces & Implementations (`StudentService`, `CourseService`, `FacultyService`, `StudentPortalService`, `TranscriptService`), các patterns: Strategy (`IGpaCalculationStrategy`), Factory Method (`IUserFactory`), Observer (`IEnrollmentObserver`, `IEnrollmentSubject`). | `SIMS.Domain` |
| **`SIMS.Persistence`** | Class Library (.NET 8) | Cấu hình Entity Framework Core: `ApplicationDbContext`, Migration lịch sử, Seed data (`DbInitializer`), Global Query Filters cho xóa mềm (`!e.IsDeleted`), cài đặt cụ thể `GenericRepository<T>` và `UnitOfWork`. | `SIMS.Application` (và gián tiếp `SIMS.Domain`). |
| **`SIMS.Infrastructure`** | Class Library (.NET 8) | Cài đặt các dịch vụ hạ tầng & kỹ thuật: `ExportService` xuất đa định dạng (CSV kèm chống CSV injection, Excel `ClosedXML`, PDF `QuestPDF`), và Decorator `CachingStudentRepository` (`IMemoryCache`). | `SIMS.Application` (và gián tiếp `SIMS.Domain`). |
| **`SIMS.Web`** | Web App (ASP.NET Core 8 MVC) | Tầng Presentation: Controllers (`StudentsController`, `CoursesController`, `FacultyController`, `StudentPortalController`, `AccountController`), Razor Views, cấu hình DI và middleware (`Program.cs`). | `SIMS.Application`, `SIMS.Infrastructure`, `SIMS.Persistence`. *(Không tham chiếu trực tiếp Domain trong `.csproj`, nhận gián tiếp qua Application)*. |
| **`SIMS.Tests`** | xUnit Test Project (.NET 8) | Bộ kiểm thử tự động toàn diện (84 test cases), kết hợp Mocking (`Moq`), Assertion (`FluentAssertions`), SQLite In-Memory (`ApplicationDbContextIntegrationTests`) và đo độ bao phủ mã nguồn (`coverlet`). | `SIMS.Domain`, `SIMS.Application`, `SIMS.Infrastructure`, `SIMS.Persistence`. |

---

## 2. Danh sách Design Pattern đang dùng kèm bằng chứng code

| STT | Design Pattern | Phân loại (GoF) | Mục đích sử dụng | Bằng chứng Code (File & Dòng) |
| :---: | :--- | :--- | :--- | :--- |
| **1** | **Strategy Pattern** | Behavioral | Hoán đổi thuật toán tính điểm GPA/CPA giữa Thang 10 (`TenPointStrategy`) và Thang 4.0 (`FourPointStrategy`). Quản lý động qua `TranscriptService`. | - `SIMS.Application/Interfaces/IGpaCalculationStrategy.cs:14-37`<br>- `SIMS.Application/Services/Strategies/TenPointStrategy.cs:5-52`<br>- `SIMS.Application/Services/Strategies/FourPointStrategy.cs:5-56`<br>- `SIMS.Application/Services/TranscriptService.cs:7-67` |
| **2** | **Factory Method Pattern** | Creational | Đóng gói logic tạo người dùng `AppUser` kèm phân quyền Role (`Student`, `Faculty`, `Administrator`), tách hoàn toàn khỏi Controller. | - `SIMS.Application/Interfaces/IUserFactory.cs:5-15`<br>- `SIMS.Application/Services/UserFactory.cs:9-62`<br>- Gọi từ: `SIMS.Web/Controllers/StudentsController.cs:64-77` |
| **3** | **Decorator Pattern** | Structural | Bọc ngoài `IGenericRepository<Student>`, bổ sung bộ nhớ đệm `IMemoryCache` cho thao tác `GetByIdAsync` mà không sửa đổi repository gốc. | - `SIMS.Infrastructure/Repositories/CachingStudentRepository.cs:9-52`<br>- Đăng ký DI bọc Decorator: `SIMS.Web/Program.cs:33-38` |
| **4** | **Observer Pattern** | Behavioral | Khi sinh viên đăng ký môn học thành công, `EnrollmentSubject` phát sự kiện `EnrollmentCreatedEvent` tới các observers độc lập (`AuditLogEnrollmentObserver` để ghi nhật ký, `NotificationEnrollmentObserver` để gửi thông báo). | - `SIMS.Application/Interfaces/IEnrollmentObserver.cs:5-28`<br>- `SIMS.Application/Services/Observers/EnrollmentSubject.cs:5-40`<br>- `SIMS.Application/Services/Observers/AuditLogEnrollmentObserver.cs:8-38`<br>- Phát sự kiện: `SIMS.Application/Services/StudentPortalService.cs:135-146` |
| **5** | **Repository Pattern (Generic)** | Structural / Data Access | Trừu tượng hóa truy vấn và thao tác CRUD trên CSDL, che giấu EF Core DbSet. | - `SIMS.Domain/Interfaces/IGenericRepository.cs:5-15`<br>- `SIMS.Persistence/Repositories/GenericRepository.cs:9-60` |
| **6** | **Unit of Work Pattern** | Behavioral / Transactional | Gom các thao tác qua nhiều repository vào một transaction nhất quán qua `CompleteAsync()`. | - `SIMS.Domain/Interfaces/IUnitOfWork.cs:3-8`<br>- `SIMS.Persistence/Repositories/UnitOfWork.cs:8-41` |
| **7** | **Dependency Injection (IoC)** | Creational / Architectural | Tiêm phụ thuộc lỏng lẻo giữa Web -> Services -> Repositories -> Exporters. | - Đăng ký DI container: `SIMS.Web/Program.cs:18-64`<br>- Constructor injection trong các Service & Controller |
| **8** | **Data Transfer Object (DTO)** | Structural / Enterprise | Đóng gói dữ liệu trao đổi giữa Presentation và Application, ngăn rò rỉ Domain Entities ra View. | - `SIMS.Application/DTOs/StudentDtos.cs`<br>- `SIMS.Application/DTOs/CourseDtos.cs`<br>- `SIMS.Application/DTOs/ExportDtos.cs` |
| **9** | **Exporter Pattern (Strategy Subsystem)** | Structural | Tách biệt các bộ xuất file `CsvExporter` (escape CSV injection), `ExcelExporter` (`ClosedXML`), `PdfExporter` (`QuestPDF`) điều phối bởi `ExportService`. | - `SIMS.Application/Interfaces/IExportService.cs:8-25`<br>- `SIMS.Infrastructure/Services/ExportService.cs:12-230`<br>- `SIMS.Infrastructure/Exporters/*` |

---

## 3. Đánh giá và Hiện trạng tuân thủ nguyên lý SOLID

| Nguyên lý | Tình trạng trước đây | Giải pháp đã khắc phục trong Code | Vị trí kiểm chứng |
| :--- | :--- | :--- | :--- |
| **SRP** *(Single Responsibility)* | `StudentsController` ôm đồm tạo `AppUser`, gán role, xử lý lỗi Identity; `StudentPortalService` kiêm nhiệm ghi nhật ký. | - Tách việc tạo user sang `IUserFactory`.<br>- Tách ghi log kiểm toán sang `AuditLogEnrollmentObserver`.<br>- Tách toàn bộ việc xuất file sang `IExportService`. | `UserFactory.cs:9-62`<br>`AuditLogEnrollmentObserver.cs:8-38`<br>`ExportService.cs:12-230` |
| **OCP** *(Open/Closed)* | Logic tính điểm bị gắn cứng; thêm thang điểm mới buộc phải sửa code cũ. | Áp dụng Strategy Pattern `IGpaCalculationStrategy`. Khi cần thêm thang điểm mới (ví dụ Thang điểm chữ A/B/C/D), chỉ cần tạo class mới hiện thực interface mà không sửa `TranscriptService`. | `TranscriptService.cs:9-38`<br>`TenPointStrategy.cs`<br>`FourPointStrategy.cs` |
| **LSP** *(Liskov Substitution)* | Repository giả định mọi entity đều dùng khóa Guid; Decorator phải thay thế được repository cơ sở. | - `CachingStudentRepository` thực thi toàn bộ hợp đồng `IGenericRepository<Student>`, thay thế hoàn hảo và trong suốt qua DI.<br>- Các entity thừa kế trọn vẹn `BaseEntity`. | `CachingStudentRepository.cs:9-52`<br>`Program.cs:33-38` |
| **ISP** *(Interface Segregation)* | Giao diện bị gộp nhiều trách nhiệm không liên quan. | Tách nhỏ các interface chuyên biệt: `IExportService`, `ITranscriptService`, `IUserFactory`, `IEnrollmentSubject`, `IEnrollmentObserver`. | `SIMS.Application/Interfaces/*` |
| **DIP** *(Dependency Inversion)* | Tầng Web phụ thuộc chi tiết DB; Service phụ thuộc trực tiếp vào class xuất file cụ thể. | Tầng Web và Application chỉ phụ thuộc vào các Abstraction (`IUnitOfWork`, `IExportService`, `IUserFactory`, `IGpaCalculationStrategy`). Hạ tầng (`Infrastructure`, `Persistence`) hiện thực các abstraction đó. | `Program.cs:30-64`<br>`ExportService.cs:12-30` |

---

## 4. So sánh với đặc tả yêu cầu (Docs/SRS.md)

| Mục SRS | Chức năng theo đặc tả | Trạng thái hiện tại | Chi tiết đánh giá trong Code |
| :--- | :--- | :---: | :--- |
| **3.1** | Đăng ký sinh viên mới (Admin) | **Đã có** | `StudentsController.cs`, `UserFactory.cs`, `CreateStudentAsync` hoạt động trơn tru. |
| **3.1** | Xem và cập nhật hồ sơ sinh viên (Admin) | **Đã có** | `StudentsController.cs`, View Index, Details, Edit. |
| **3.1** | Xóa mềm (Soft-delete) sinh viên | **Đã có** | Đã sửa `DeleteStudentAsync`: chuyển sang cập nhật `IsDeleted = true`. Kiểm tra chặn xóa nếu đã có enrollment. |
| **3.2** | Tạo mới, cập nhật khóa học (Admin) | **Đã có** | `CoursesController.cs`, `CourseService.cs`. |
| **3.2** | Xóa mềm khóa học | **Đã có** | Đã sửa `DeleteCourseAsync`: chuyển sang cập nhật `IsDeleted = true`. Kiểm tra chặn xóa nếu có sinh viên đang đăng ký. |
| **3.2** | Khai báo tín chỉ, khoa, môn tiên quyết, gán giảng viên | **Đã có** | Đầy đủ thuộc tính `Credits`, `DepartmentId`, `AssignedFacultyId`, `PrerequisiteCourseId`. |
| **3.3** | Sinh viên đăng ký / hủy môn học | **Đã có** | `StudentPortalController.cs`, `StudentPortalService.cs`. |
| **3.3** | Kiểm tra môn tiên quyết (Prerequisite validation) | **Đã có** | Trong `EnrollInCourseAsync`: bắt buộc sinh viên đã học môn tiên quyết và đạt điểm $\ge 5.0$. |
| **3.3** | Giới hạn tín chỉ tối đa mỗi học kỳ | **Đã có** | Trong `EnrollInCourseAsync`: kiểm tra tổng tín chỉ không vượt quá `AppConstants.MaxCreditsPerSemester` (24 tín chỉ). |
| **3.4** | Giảng viên nhập điểm cho khóa học phụ trách | **Đã có** | `FacultyController.cs`, `FacultyService.cs`. |
| **3.4** | Tự động tính GPA (học kỳ) và CPA (tích lũy) | **Đã có** | Đã cài đặt qua Strategy Pattern (`TranscriptService`, `TenPointStrategy`, `FourPointStrategy`). |
| **3.5** | Xác thực đăng nhập/đăng xuất Identity | **Đã có** | ASP.NET Core Identity (`Program.cs`, `AccountController.cs`). |
| **3.5** | Xuất báo cáo dữ liệu (Export CSV, Excel, PDF) | **Đã có** | Đầy đủ cho 3 vai trò (Admin, Faculty, Student) kèm bảo mật RBAC, escape CSV injection và ghi `AuditLog`. |

---

## 5. Kết quả Khắc phục Code Smell & Lỗi Kỹ thuật

1. **Loại bỏ Magic Strings**:  
   Thay thế toàn bộ chuỗi hardcode role (`"Administrator"`, `"Faculty"`, `"Student"`) và domain email (`"@sims.com"`) bằng hằng số tập trung: `AppRoles.cs` và `AppConstants.cs`.
2. **Khắc phục triệt để N+1 Query**:  
   - `StudentsController.Index`: Nạp trước toàn bộ `AppUser` liên quan trong 1 truy vấn duy nhất theo danh sách `userIds`, loại bỏ hoàn toàn vòng lặp truy vấn `_userManager`.
   - `FacultyService.GetCourseGradesAsync`: Gom `studentIds` và `userIds` để truy vấn theo Dictionary, không lặp query DB trong vòng lặp.
3. **Chuyển đổi hoàn toàn sang Xóa mềm (Soft Delete)**:  
   - Sửa `StudentService.DeleteStudentAsync` và `CourseService.DeleteCourseAsync` cập nhật `IsDeleted = true` thay vì gọi `Remove()`.  
   - Tích hợp Global Query Filter trong EF Core `ApplicationDbContext`: `builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted)`.
4. **Xóa bỏ Placeholders & Dữ liệu chắp vá**:  
   - Thay thế các chuỗi tạm `"Student " + id` hay `"Name is mapped in Controller"` bằng họ tên thật (`FirstName + LastName`) lấy chính xác từ `AppUser`.
5. **Phòng chống tấn công CSV Formula Injection**:  
   - Trong `CsvExporter.cs`, các ô dữ liệu bắt đầu bằng `=`, `+`, `-`, `@` được tự động escape bằng ký tự `'` ở đầu, ngăn chặn thực thi mã độc trong Microsoft Excel.

---

## 6. Danh sách Class và Quan hệ mô hình hóa UML

Xem chi tiết mã nguồn sơ đồ tại [Project_SIMS/Docs/diagrams/class_diagram.puml](file:///Project_SIMS/Docs/diagrams/class_diagram.puml) và mô tả tại [class_diagram.md](file:///Project_SIMS/Docs/diagrams/class_diagram.md):
- **Kế thừa (Generalization)**: `BaseEntity <|-- Student`, `Course`, `Faculty`, `Enrollment`, `Department`, `Semester`, `AcademicRecord`, `AuditLog`.
- **Hợp thành (Composition)**: `Course "1" *-- "0..*" Enrollment`.
- **Tổng hợp (Aggregation)**: `Department "1" o-- "0..*" Faculty`, `Student`, `Course`.
- **Mẫu thiết kế (Patterns)**:
  - `TranscriptService` o-> `IGpaCalculationStrategy` (Strategy)
  - `UserFactory` implements `IUserFactory` (Factory Method)
  - `CachingStudentRepository` o-> `IGenericRepository<Student>` (Decorator)
  - `EnrollmentSubject` o-> `IEnrollmentObserver` (Observer)

---

## 7. Danh sách Use Case theo Actor

Xem chi tiết sơ đồ tại [Project_SIMS/Docs/diagrams/use_case.puml](file:///Project_SIMS/Docs/diagrams/use_case.puml) và mô tả tại [use_case.md](file:///Project_SIMS/Docs/diagrams/use_case.md):
- **Administrator**: Quản lý Sinh viên/Khóa học (CRUD, Xóa mềm), Xuất danh sách sinh viên, môn học, lượt đăng ký theo học kỳ, bảng điểm môn học (CSV/Excel).
- **Faculty**: Xem môn được phân công, Xem điểm lớp học, Nhập/Cập nhật điểm, Xuất bảng điểm môn phụ trách (CSV/Excel).
- **Student**: Tra cứu môn mở, Đăng ký học phần (tự động kiểm tra tiên quyết và trần tín chỉ), Hủy môn học, Xem điểm & GPA/CPA, Xuất kết quả học tập (CSV/Excel/PDF).
- **Dịch vụ Hệ thống**: `AuditLog` (tự động kích hoạt khi có sự kiện xuất dữ liệu hoặc đăng ký học phần).

---

## 8. Hiện trạng Đối chiếu với Tiêu chí Đề bài APDP

| Tiêu chí Đề bài | Hiện trạng trong Solution SIMS | Đánh giá |
| :--- | :--- | :---: |
| **Cơ chế lưu trữ File (CSV/Excel/PDF)** | Đã hiện thực `CsvExporter` (chuẩn UTF-8 BOM, RFC 4180, escape injection), `ExcelExporter` (`ClosedXML`), `PdfExporter` (`QuestPDF`) phân quyền theo vai trò. | **ĐẠT** |
| **Cơ sở dữ liệu quan hệ** | Entity Framework Core 8, Code-First Migrations, SQL Server, Global Query Filters cho Soft Delete. | **ĐẠT** |
| **Hiện thực Design Patterns** | Đã triển khai đầy đủ cả 3 nhóm Creational (Factory Method), Structural (Decorator, Repository), Behavioral (Strategy, Observer, Unit of Work). | **ĐẠT** |
| **Bộ kiểm thử tự động (Testing)** | 84 unit tests & integration tests với SQLite in-memory, đạt độ bao phủ Line Coverage: Domain **100%**, Application **97.05%**. | **ĐẠT** |
| **Quy tắc nghiệp vụ (Business Rules)** | Đã kiểm tra môn tiên quyết (điểm $\ge 5.0$), giới hạn tín chỉ tối đa 24 tín chỉ/học kỳ. | **ĐẠT** |
| **Tính toán GPA / CPA** | Tự động tính điểm GPA và CPA theo cả Thang 10 và Thang 4.0, xếp loại học lực chuẩn xác. | **ĐẠT** |
