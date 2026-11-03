# NHẬT KÝ NÂNG CẤP THIẾT KẾ & TỐI ƯU MÃ NGUỒN (CHANGELOG.md)

Tài liệu ghi lại toàn bộ các nâng cấp kiến trúc, bổ sung Design Patterns (Creational, Structural, Behavioral) và khắc phục các vi phạm nguyên lý SOLID trong Solution SIMS.

---

## 1. Strategy Pattern (Behavioral) - Tính điểm GPA & CPA
- **Tập tin thêm mới:**
  - `SIMS.Application/Interfaces/IGpaCalculationStrategy.cs`
  - `SIMS.Application/Services/Strategies/TenPointStrategy.cs`
  - `SIMS.Application/Services/Strategies/FourPointStrategy.cs`
  - `SIMS.Application/Interfaces/ITranscriptService.cs`
  - `SIMS.Application/Services/TranscriptService.cs`
  - `SIMS.Tests/GpaStrategyTests.cs`
- **Mô tả thay đổi:**
  - Định nghĩa interface `IGpaCalculationStrategy` với 2 cài đặt cụ thể: `TenPointStrategy` (thang 10) và `FourPointStrategy` (thang 4).
  - Tính GPA theo kỳ và CPA tích lũy có trọng số theo tín chỉ (`Credits`) từ `Enrollment.Grade`.
  - Tạo `TranscriptService` nhận `IGpaCalculationStrategy` qua constructor và cho phép hoán đổi chiến lược runtime (`SetStrategy`).
- **Lý do & Nguyên lý liên quan:**
  - **Open/Closed Principle (OCP):** Hệ thống có thể mở rộng thêm bất kỳ thang tính điểm nào (thang chữ ECTS, thang 100, thang điểm chuẩn hóa) chỉ bằng cách tạo class mới hiện thực `IGpaCalculationStrategy` mà không cần sửa code cũ.
  - **Single Responsibility Principle (SRP):** Tách thuật toán tính điểm và xếp loại học lực ra khỏi tầng Entity và Presentation.

---

## 2. Factory Method Pattern (Creational) - Khởi tạo tài khoản người dùng
- **Tập tin thêm mới/chỉnh sửa:**
  - `SIMS.Application/Interfaces/IUserFactory.cs` (Mới)
  - `SIMS.Application/Services/UserFactory.cs` (Mới)
  - `SIMS.Web/Controllers/StudentsController.cs` (Sửa)
  - `SIMS.Tests/UserFactoryTests.cs` (Mới)
- **Mô tả thay đổi:**
  - Tách toàn bộ logic tạo đối tượng `AppUser`, sinh email mặc định, gán mật khẩu khởi tạo và cấp quyền Role ra khỏi `StudentsController`.
  - `StudentsController` chỉ việc gọi `_userFactory.CreateUserWithRoleAsync(request)`.
- **Lý do & Nguyên lý liên quan:**
  - **Single Responsibility Principle (SRP):** Khắc phục lỗi "Fat Controller". Controller chỉ làm nhiệm vụ tiếp nhận HTTP request và điều phối luồng; việc khởi tạo đối tượng phức tạp và phân quyền Identity do Factory chuyên trách.
  - **Factory Method Pattern:** Đóng gói quy trình tạo lập tài khoản, nhất quán quy tắc đặt username/email và mật khẩu trên toàn hệ thống.

---

## 3. Decorator Pattern (Structural) - Bộ nhớ đệm CachingStudentRepository
- **Tập tin thêm mới/chỉnh sửa:**
  - `SIMS.Infrastructure/Repositories/CachingStudentRepository.cs` (Mới)
  - `SIMS.Persistence/Repositories/UnitOfWork.cs` (Sửa: hỗ trợ resolve custom/decorated repository từ `IServiceProvider`)
  - `SIMS.Web/Program.cs` (Sửa: đăng ký DI decorator với `IMemoryCache`)
  - `SIMS.Tests/CachingStudentRepositoryTests.cs` (Mới)
- **Mô tả thay đổi:**
  - `CachingStudentRepository` cài đặt `IGenericRepository<Student>` và bọc quanh instance gốc `GenericRepository<Student>`.
  - Cache kết quả truy vấn `GetByIdAsync(Guid id)`. Tự động xóa cache (Invalidation) khi có thao tác `Update`, `Remove`, `AddAsync`.
- **Lý do & Nguyên lý liên quan:**
  - **Decorator Pattern:** Bổ sung tính năng bộ nhớ đệm (caching) trong suốt cho repository mà không cần sửa đổi mã nguồn của lớp `GenericRepository<T>`.
  - **Open/Closed Principle (OCP):** Mở rộng hành vi truy cập dữ liệu mà không can thiệp vào code gốc.

---

## 4. Observer Pattern (Behavioral) - Xử lý sự kiện đăng ký học phần
- **Tập tin thêm mới/chỉnh sửa:**
  - `SIMS.Application/Interfaces/IEnrollmentObserver.cs` (Mới: chứa `EnrollmentCreatedEvent`, `IEnrollmentObserver`, `IEnrollmentSubject`)
  - `SIMS.Application/Services/Observers/EnrollmentSubject.cs` (Mới)
  - `SIMS.Application/Services/Observers/AuditLogEnrollmentObserver.cs` (Mới: tự động ghi AuditLog)
  - `SIMS.Application/Services/Observers/NotificationEnrollmentObserver.cs` (Mới: gửi thông báo xác nhận)
  - `SIMS.Application/Services/StudentPortalService.cs` (Sửa: phát sự kiện khi đăng ký thành công)
  - `SIMS.Tests/EnrollmentObserverAndRulesTests.cs` (Mới)
- **Mô tả thay đổi:**
  - Khi một lượt đăng ký học phần được tạo thành công, `StudentPortalService` không tự ghi AuditLog hay gửi thông báo mà thông báo cho `IEnrollmentSubject`.
  - Các observer (`AuditLogEnrollmentObserver`, `NotificationEnrollmentObserver`) tự động nhận sự kiện và xử lý độc lập.
- **Lý do & Nguyên lý liên quan:**
  - **Single Responsibility (SRP) & Dependency Inversion (DIP):** Service đăng ký học phần không bị phụ thuộc chặt vào logic ghi nhật ký hay gửi email/SMS.
  - **Open/Closed Principle (OCP):** Có thể thêm các tác vụ sau đăng ký (ví dụ: tạo hóa đơn học phí, thông báo giảng viên) bằng cách thêm Observer mới mà không chạm vào `StudentPortalService`.

---

## 5. Hiện thực hóa các quy tắc nghiệp vụ theo SRS
- **Tập tin chỉnh sửa:**
  - `SIMS.Domain/Constants/AppConstants.cs`: Cấu hình giới hạn tín chỉ `MaxCreditsPerSemester = 24`.
  - `SIMS.Application/Services/StudentPortalService.cs`:
    - **Kiểm tra môn tiên quyết (`PrerequisiteCourseId`):** Sinh viên chỉ được đăng ký môn học nếu đã hoàn thành môn tiên quyết với điểm tổng kết $\ge 5.0$.
    - **Kiểm tra giới hạn tín chỉ học kỳ:** Tính tổng số tín chỉ đã đăng ký trong học kỳ hiện tại, từ chối nếu vượt quá `AppConstants.MaxCreditsPerSemester`.
- **Lý do & Nguyên lý liên quan:**
  - Hoàn thiện nghiệp vụ cốt lõi theo đặc tả yêu cầu `docs/SRS.md`.
  - Đưa giới hạn tín chỉ vào file hằng số cấu hình tập trung thay vì hard-code số magic trong code.

---

## 6. Sửa lỗi kỹ thuật và Code Smells
1. **Xóa mềm (Soft Delete) có kiểm tra ràng buộc:**
   - Trong `StudentService.DeleteStudentAsync`: Chuyển sang gán `IsDeleted = true` và `UpdatedAt = DateTime.UtcNow`. Kiểm tra nếu sinh viên đã có lịch sử đăng ký học phần (`Enrollment`) thì từ chối xóa và ném `InvalidOperationException`.
   - Trong `CourseService.DeleteCourseAsync`: Chuyển sang gán `IsDeleted = true` và `UpdatedAt = DateTime.UtcNow`. Kiểm tra nếu môn học đã có sinh viên đăng ký thì từ chối xóa.
2. **Loại bỏ N+1 Query tại trang Danh sách Sinh viên (`StudentsController.Index`):**
   - Thay thế vòng lặp query từng `AppUser` đơn lẻ bằng một truy vấn SQL kết hợp: `_userManager.Users.Where(u => emails.Contains(u.Email)).ToDictionaryAsync(...)`.
   - Giảm số lượng truy vấn CSDL từ $N+1$ xuống còn đúng **1 câu truy vấn** duy nhất.
3. **Thay thế Magic Strings:**
   - Thay `"@sims.com"` bằng `AppConstants.DefaultEmailDomain`.
   - Thay các chuỗi vai trò `"Administrator"`, `"Faculty"`, `"Student"` bằng `AppRoles.Administrator`, `AppRoles.Faculty`, `AppRoles.Student`.
4. **Thay thế Placeholder bằng dữ liệu thật:**
   - Trong `FacultyService.GetCourseGradesAsync`: Thay placeholder `"Student " + student.StudentId` bằng họ tên thật truy vấn từ `AppUser` (`appUser.FirstName + appUser.LastName`).
   - Trong `StudentPortalService.GetMyProfileAsync`: Thay `"Name is mapped in Controller"` bằng dữ liệu thật từ `AppUser`.

---

## 7. Bổ sung Bộ Kiểm thử Toàn diện (Unit & Integration Tests)
- **Tập tin thêm mới:**
  - `SIMS.Tests/CourseServiceTests.cs`, `StudentServiceTests.cs`, `FacultyServiceAndPortalAdditionalTests.cs`
  - `SIMS.Tests/StudentPortalEnrollmentScenariosTests.cs`: Đủ 5 kịch bản đăng ký môn học (Thành công, Trùng môn, Vượt tín chỉ, Thiếu tiên quyết, Kỳ không active).
  - `SIMS.Tests/GpaCpaDetailedCalculationTests.cs`, `TranscriptServiceTests.cs`: Kiểm thử tính điểm GPA/CPA chi tiết đối chiếu số liệu tính tay.
  - `SIMS.Tests/ExportSecurityTests.cs`, `CsvInjectionTests.cs`, `ExportFormatAndContentTypeTests.cs`: Kiểm thử phân quyền RBAC và escape CSV Injection.
  - `SIMS.Tests/ApplicationDbContextIntegrationTests.cs`: Kiểm thử tích hợp EF Core với SQLite In-Memory (Global Query Filter xóa mềm và AuditLog).
  - `SIMS.Tests/DomainEntitiesTests.cs`: Kiểm thử thuộc tính và quan hệ các thực thể Domain.
- **Kết quả:**
  - Tổng cộng 84/84 tests pass (0 fail).
  - Line Coverage: `SIMS.Domain` đạt **100%**, `SIMS.Application` đạt **97.05%**.
- **Lý do & Nguyên lý liên quan:**
  - Đảm bảo chất lượng phần mềm, ngăn ngừa lỗi hồi quy (regression) và bảo đảm các quy tắc nghiệp vụ theo SRS hoạt động đúng chuẩn.

---

## 8. Chuẩn hóa Cấu trúc Thư mục & An toàn Cấu hình
- **Sắp xếp thư mục:**
  - Chuẩn hóa cấu trúc repo: Repo root chứa `.github/`, `.gitignore`, `Project_SIMS/`.
  - Hợp nhất và chỉ duy trì một thư mục tài liệu duy nhất tại `Project_SIMS/Docs/`, đồng bộ toàn bộ nội dung và xóa bỏ thư mục `docs/` trùng lặp ở root.
- **An toàn cấu hình (Configuration Security):**
  - Chuyển đổi chuỗi kết nối máy cá nhân trong `SIMS.Web/appsettings.json` thành chuỗi kết nối mẫu chuẩn (`Server=localhost;Database=SIMSDb;Trusted_Connection=True;...`), giữ nguyên cơ chế đọc cấu hình qua `IConfiguration.GetConnectionString("DefaultConnection")`.
- **Tài liệu hóa:**
  - Đồng bộ `SDD.md`, `README.md`, `APDP_analysis.md`, các sơ đồ PlantUML (`use_case.puml`, `class_diagram.puml`, `package_diagram.puml`) theo đúng kiến trúc và mã nguồn thực tế.

