# Hệ thống Quản lý Sinh viên (Student Information Management System - SIMS)

Dự án này là hệ thống quản lý sinh viên được xây dựng dựa trên kiến trúc **Clean Architecture** (Onion Architecture) sử dụng ASP.NET Core MVC 8, Entity Framework Core và SQL Server.

---

## 🛠 Yêu cầu hệ thống (Prerequisites)
1. **.NET 8 SDK**: Bắt buộc để biên dịch và chạy dự án ([dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)).
2. **SQL Server**: SQL Server Developer, Express Edition hoặc LocalDB.
3. **IDE**: Visual Studio 2022 (khuyên dùng), JetBrains Rider hoặc Visual Studio Code.

---

## 🚀 Hướng dẫn Cài đặt & Khởi chạy (Setup Guide)

### Bước 1: Cấu hình chuỗi kết nối Database (Connection String)
Trong file `SIMS.Web/appsettings.json`, chuỗi kết nối hiện đang để giá trị mẫu:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=SIMSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
}
```
*Tùy chỉnh theo môi trường máy cá nhân:*
- **Nếu dùng LocalDB:**  
  `Server=(localdb)\\mssqllocaldb;Database=SIMSDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
- **Nếu dùng tài khoản SQL Server Authentication (`sa`):**  
  `Server=localhost;Database=SIMSDb;User Id=sa;Password=MatKhauCuaBan;TrustServerCertificate=True`

### Bước 2: Build & Chạy ứng dụng
Hệ thống tích hợp sẵn cơ chế **Auto Migration & Seeding** trong `DbInitializer.cs`. Khi ứng dụng khởi chạy lần đầu, cơ sở dữ liệu và dữ liệu mẫu sẽ được tạo tự động.

**Chạy bằng Visual Studio 2022:**
1. Mở file `SIMS.sln`.
2. Đặt `SIMS.Web` làm **Startup Project**.
3. Nhấn **F5** (hoặc Ctrl+F5) để khởi chạy.

**Chạy bằng Terminal (CLI):**
```bash
dotnet restore Project_SIMS/SIMS.sln
dotnet build Project_SIMS/SIMS.sln
cd Project_SIMS/SIMS.Web
dotnet run
```
Truy cập ứng dụng tại địa chỉ hiển thị trên console (mặc định: `https://localhost:5001` hoặc `http://localhost:5000`).

---

## 🧪 Kiểm thử Tự động (Automated Testing)
Dự án được trang bị bộ kiểm thử toàn diện với **84 test cases** (Unit Tests & Integration Tests) sử dụng xUnit, Moq, FluentAssertions và SQLite In-Memory.

```bash
# Chạy toàn bộ test suite
dotnet test Project_SIMS/SIMS.sln

# Chạy kèm thu thập độ bao phủ mã nguồn (Code Coverage)
dotnet test Project_SIMS/SIMS.Tests/SIMS.Tests.csproj --collect:"XPlat Code Coverage"
```

| Project | Chỉ số Line Coverage | Đánh giá |
| :--- | :---: | :---: |
| **`SIMS.Domain`** | **100.00%** | Đạt chuẩn |
| **`SIMS.Application`** | **97.05%** | Đạt chuẩn |

---

## 🔑 Thông tin Tài khoản Mẫu (Test Accounts)

| Vai trò (Role) | Email Đăng nhập | Mật khẩu | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@sims.com` | `Admin@123` | Quản lý Sinh viên/Khóa học (CRUD, xóa mềm), Xuất báo cáo CSV/Excel, Xem AuditLog |
| **Faculty** | `faculty@sims.com` | `Faculty@123` | Xem môn được phân công, Nhập điểm sinh viên, Xuất bảng điểm lớp học |
| **Student** | `student@sims.com` | `Student@123` | Đăng ký/hủy học phần (kiểm tra tiên quyết & trần tín chỉ), Xem GPA/CPA, Xuất bảng điểm (CSV/Excel/PDF) |

---

## 📂 Cấu trúc Mã nguồn (Clean Architecture)

- **`SIMS.Domain`**: Lõi nghiệp vụ độc lập, chứa các Entities (`BaseEntity`, `Student`, `Course`, `Faculty`, `Enrollment`, `Department`, `Semester`, `AcademicRecord`, `AuditLog`, `AppUser`), Repository Interfaces (`IGenericRepository<T>`, `IUnitOfWork`), và hằng số (`AppConstants`, `AppRoles`).
- **`SIMS.Application`**: Chứa DTOs, Business Services (`StudentService`, `CourseService`, `FacultyService`, `StudentPortalService`, `TranscriptService`), cùng các interfaces và hiện thực Design Pattern.
- **`SIMS.Infrastructure`**: Hiện thực các dịch vụ hạ tầng & kỹ thuật: `ExportService` hỗ trợ xuất file CSV (kèm chống CSV Injection), Excel (`ClosedXML`), PDF (`QuestPDF`), và Decorator `CachingStudentRepository` (`IMemoryCache`).
- **`SIMS.Persistence`**: Cài đặt Entity Framework Core (`ApplicationDbContext`), `GenericRepository<T>`, `UnitOfWork`, Global Query Filters (xóa mềm), và Migration/Seed data.
- **`SIMS.Web`**: Tầng Presentation ASP.NET Core MVC, Controllers, Razor Views, xác thực Identity và Composition Root (`Program.cs`).
- **`SIMS.Tests`**: Bộ test suite tự động với 84 test cases, kiểm thử logic đăng ký môn học, tính GPA/CPA, xuất dữ liệu, bảo mật phân quyền và SQLite In-Memory.

---

## 📐 Danh sách Design Patterns Triển khai

1. **Strategy Pattern** (`IGpaCalculationStrategy`): Hoán đổi thuật toán tính GPA/CPA giữa thang điểm 10 (`TenPointStrategy`) và thang điểm 4.0 (`FourPointStrategy`).
2. **Factory Method Pattern** (`IUserFactory`, `UserFactory`): Tách biệt logic khởi tạo `AppUser` theo từng Role khỏi Presentation Controllers.
3. **Decorator Pattern** (`CachingStudentRepository`): Bọc ngoài repository cơ sở, bổ sung cơ chế caching trong bộ nhớ (`IMemoryCache`) mà không sửa đổi repository gốc.
4. **Observer Pattern** (`IEnrollmentSubject`, `IEnrollmentObserver`): Phát sự kiện `EnrollmentCreatedEvent` khi sinh viên đăng ký môn học thành công để tự động ghi `AuditLog` và gửi thông báo.
5. **Repository & Unit of Work**: Trừu tượng hóa truy cập CSDL và điều phối transaction nhất quán.
