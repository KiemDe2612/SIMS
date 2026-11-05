# Hệ thống Quản lý Sinh viên (Student Information Management System - SIMS)

Dự án này là hệ thống quản lý sinh viên được xây dựng dựa trên kiến trúc **Clean Architecture** sử dụng ASP.NET Core MVC 8, Entity Framework Core và SQL Server.

## 🛠 Yêu cầu hệ thống (Prerequisites)
Để có thể chạy được mã nguồn này trên máy tính cá nhân, bạn cần cài đặt các phần mềm sau:
1. **.NET 8 SDK**: Bắt buộc phải có để biên dịch mã nguồn C# (Tải tại [dotnet.microsoft.com](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)).
2. **SQL Server**: Phiên bản SQL Server Developer/Express Edition. Hoặc dùng LocalDB.
3. **Visual Studio 2022** (Khuyên dùng) hoặc **Visual Studio Code**.

---

## 🚀 Hướng dẫn Cài đặt & Chạy dự án (Setup Guide)

### Bước 1: Cấu hình chuỗi kết nối Database (Connection String)
Khi copy dự án sang máy mới, môi trường SQL Server của mỗi người sẽ khác nhau. Bạn cần mở file cấu hình để sửa lại:
1. Mở file `SIMS.Web/appsettings.json`.
2. Tìm đến phần `"ConnectionStrings": { "DefaultConnection": "..." }`.
3. Thay đổi thông tin đăng nhập SQL Server cho khớp với máy của bạn.
   - *Ví dụ:* Nếu dùng tài khoản sa: `Server=.;Database=SIMSDb;User Id=sa;Password=Mật_khẩu_của_bạn;TrustServerCertificate=True;`
   - *Ví dụ:* Nếu dùng Windows Authentication (LocalDB): `Server=(localdb)\\mssqllocaldb;Database=SIMSDb;Trusted_Connection=True;MultipleActiveResultSets=true`

### Bước 2: Build & Chạy dự án (Tạo Database tự động)
Hệ thống được lập trình sẵn tính năng **Tự động tạo Database & Sinh dữ liệu mẫu (Auto Migration & Seeding)** mỗi khi khởi động. Do đó, bạn không cần phải gõ lệnh `update-database` thủ công!

**Cách chạy bằng Visual Studio 2022:**
1. Mở file `Project_SIMS.sln`.
2. Click chuột phải vào project **SIMS.Web** trong Solution Explorer -> Chọn **Set as Startup Project**.
3. Bấm nút **Start (mũi tên xanh - Phím F5)** để chạy.
4. Ở lần chạy đầu tiên, màn hình console có thể sẽ load hơi chậm (mất khoảng 5-10 giây) để hệ thống tự động tạo các bảng và chèn 10 bộ dữ liệu mẫu vào SQL Server.

**Cách chạy bằng Terminal/CMD:**
```bash
cd SIMS.Web
dotnet build
dotnet run
```
Sau đó mở trình duyệt và truy cập vào `http://localhost:5187` (hoặc cổng mà console thông báo).

---

## 🔑 Thông tin các Tài khoản mẫu (Test Accounts)

Sau khi hệ thống khởi chạy thành công, database `SIMSDb` đã có sẵn dữ liệu. Dưới đây là các tài khoản mặc định để bạn test các phân hệ chức năng:

| Vai trò (Role) | Email Đăng nhập | Mật khẩu | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Administrator** | `admin@sims.com` | `Admin@123` | Quản lý User, Sinh viên, Khóa học (CRUD) |
| **Faculty (Giảng viên)** | `faculty@sims.com` | `Faculty@123` | Xem lớp phụ trách, Nhập điểm sinh viên |
| **Student (Sinh viên)** | `student@sims.com` | `Student@123` | Student Portal, Đăng ký môn, Xem điểm (CPA) |

*(Ngoài ra còn có các tài khoản sinh viên khác như `se11111@sims.com`, `ba55555@sims.com`,... với chung mật khẩu `Student@123` để bạn test).*

---

## 📂 Cấu trúc mã nguồn (Clean Architecture)
Khi làm việc với dự án, bạn cần hiểu cấu trúc thư mục để sửa code đúng chỗ:
- **`SIMS.Domain`**: Chứa các Models/Entities lõi (Sinh viên, Lớp học, Điểm...) và các Interfaces. Không phụ thuộc vào thư viện ngoài.
- **`SIMS.Application`**: Chứa các DTOs (Data Transfer Objects) và Services (Business Logic) như `StudentService`, `FacultyService`.
- **`SIMS.Infrastructure`**: (Nếu có) Chứa các cấu hình gửi Email, kết nối hệ thống ngoài.
- **`SIMS.Persistence`**: Chứa `ApplicationDbContext`, cấu hình Entity Framework (Migrations) và cụ thể hóa Repository Pattern (GenericRepository, UnitOfWork).
- **`SIMS.Web`**: Là UI layer (ASP.NET Core MVC), chứa Controllers, Views (Razor), wwwroot (css, js) và cấu hình Program.cs.
