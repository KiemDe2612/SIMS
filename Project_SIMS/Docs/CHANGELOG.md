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


---

## 9. Cải thiện giao diện UI/UX (chỉ tầng View & CSS, không đổi logic)

Báo cáo kiểm tra hiện trạng: `Docs/UI_AUDIT.md`. Không sửa Controllers, Application, Domain, Persistence, Tests, Migrations. Giữ nguyên tên action, route, tên field form, `[ValidateAntiForgeryToken]` (các form POST vẫn dùng tag helper `asp-action` nên token chống giả mạo tự sinh như cũ) và các thuộc tính `asp-*` đang dùng.

### 9.1. Hệ thống thiết kế – `SIMS.Web/wwwroot/css/site.css` (viết lại)
- Biến `:root` cho màu (`--sims-*`), khoảng cách (`--space-1..6`, thang 4px), bo góc (`--radius-*`), đổ bóng, kích thước bố cục. Một bảng màu dùng chung cho mọi trang.
- **Ánh xạ sang biến Bootstrap** (`--bs-primary`, `--bs-success`, `--bs-*-bg-subtle`, `--bs-link-color`, focus ring…) và biến component của `.btn-*`, `.alert`, `.card`, `.table`, `.dropdown` → mọi thành phần Bootstrap dùng cùng bảng màu. *Lý do:* trước đây màu riêng và màu Bootstrap lẫn lộn (C4).
- **Độ tương phản WCAG AA:** primary đổi từ #6366F1 (4.4:1) sang #4F46E5 (6.3:1); bỏ gradient tím (≈3:1); bỏ `btn-outline-info` (1.9:1). Các cặp chữ/nền chính đều ≥ 5:1 (đã đo: nút primary 6.29, success 5.02, muted 7.58, badge trạng thái ≥ 8).
- **Font hệ thống** (`system-ui, Segoe UI, Roboto, Noto Sans…`) thay Google Font “Outfit” vốn thiếu bộ dấu tiếng Việt → chữ có dấu hiển thị đồng nhất, bớt 1 phụ thuộc CDN.
- Bảng `.table-stack`: dưới 768px mỗi hàng thành một thẻ, nhãn cột lấy từ `data-label` → **không cuộn ngang ở 375px**. Hàng xen kẽ, header chữ hoa nhỏ, `code-badge` cho mã SV/mã môn, `status-badge` (nền nhạt, chữ đậm) cho trạng thái.
- Kiểu validation: `.input-validation-error` viền đỏ, `.field-validation-error` có biểu tượng cảnh báo ngay dưới trường, `.validation-summary-errors` thành khung cảnh báo có tiêu đề “Vui lòng kiểm tra lại các thông tin sau:”.
- `.form-section`, `.form-actions` (Hủy/Lưu tách vùng; trên mobile nút full-width, nút chính ở trên), `.page-header`, `.toolbar`, `.empty-state`, `.stat-card`, `.info-list`, skip link, `:focus-visible`, vùng bấm tối thiểu 40–44px, tôn trọng `prefers-reduced-motion`.

### 9.2. Bố cục – `Views/Shared/_Layout.cshtml`, `_LoginPartial.cshtml`
- **Menu trên điện thoại:** sidebar chuyển sang `offcanvas-md` của Bootstrap; nút ☰ mở menu ở < 768px. *Lý do:* trước đây sidebar `d-none d-md-block` nên mobile không có điều hướng (L1).
- Menu theo vai trò giữ nguyên quyền hiển thị (Administrator: Sinh viên, Khóa học, Giảng viên; Faculty: Giảng viên; Student: Góc học tập). Trạng thái active tính phía server theo controller/action, có `aria-current="page"`; link dùng `asp-controller/asp-action` thay vì URL cứng. Bỏ script JS gán active.
- **TempData tập trung trong layout**: `SuccessMessage` (xanh, `role=status`), `ErrorMessage` (đỏ, `role=alert`), hỗ trợ sẵn `WarningMessage`/`InfoMessage`; đã xóa khối alert lặp lại trong 5 view. *Lý do:* các trang như MyGrades/Faculty trước đây không hiển thị lỗi xuất file (N1).
- Nạp Bootstrap CSS/JS từ `wwwroot/lib` (5.3.3) thay CDN; **bổ sung `~/lib/jquery`** vì `_ValidationScriptsPartial` cần jQuery — trước đây validation phía client không chạy (L6). Bỏ Font Awesome (đổi sang Bootstrap Icons). Bootstrap Icons vẫn dùng CDN cũ vì chưa có trong `wwwroot/lib` (không thêm CDN mới). Chart.js chỉ nạp ở trang Home.
- `lang="vi"`, link “Bỏ qua điều hướng”, footer. `_LoginPartial`: bỏ `<ul>` lồng sai, tên người dùng rút gọn bằng dấu “…”, nhãn vai trò, nút Đăng xuất rõ ràng (form logout giữ nguyên thuộc tính).
- Script nhỏ inline lọc bảng phía client (`data-table-filter`), bỏ dấu khi so khớp (“nguyen” khớp “Nguyễn”), chỉ ẩn/hiện hàng, không gửi request.

### 9.3. Trang danh sách – `Students/Index`, `Courses/Index`, `Faculty/Index`
- Tiêu đề trang + breadcrumb + mô tả; thanh công cụ gồm ô tìm kiếm (có nhãn ẩn cho trình đọc màn hình), bộ đếm bản ghi, dropdown xuất dữ liệu, nút thêm mới; tự xuống dòng trên mobile.
- Courses: gộp 2 dropdown xuất thành một “Xuất dữ liệu” có nhóm (Danh sách môn học / Đăng ký học phần), cùng action `Export`/`ExportEnrollments` như cũ. Nút Sửa/Xóa có chữ và `aria-label`.
- Bảng `table-striped`, `scope="col"`, `<caption>`; trạng thái rỗng và “không tìm thấy kết quả”. Dropdown trong hàng dùng Popper `strategy: fixed` để không bị `.table-responsive` cắt.
- Nút xóa **giữ nguyên** POST và `confirm()` hiện có.
- `Faculty/Index`: tiêu đề “Assigned Courses” → “Môn phụ trách”.

### 9.4. Trang form – `Students/Create|Edit`, `Courses/Create|Edit`
- Nhóm trường bằng `<fieldset>/<legend>`: “Tài khoản & Khoa” / “Thông tin cá nhân”; “Thông tin môn học” / “Khoa & Phân công”. Đặt “Họ & Tên đệm” trước “Tên”.
- Dấu `*` cho trường bắt buộc, khớp với quy tắc validation thực tế của server (gồm cả trường bắt buộc ngầm định do kiểu `string` non-nullable: Địa chỉ, Mô tả; và Ngày sinh). Gợi ý “Từ 1 đến 10” cho số tín chỉ.
- `asp-validation-summary` đổi `ModelOnly` → `All`. *Lý do:* ở Students/Create, lỗi `Email` (DTO yêu cầu nhưng form không có trường này) trước đây bị ẩn hoàn toàn nên người dùng không biết vì sao lưu thất bại (F1). Nguyên nhân gốc ở DTO — xem `UI_AUDIT.md` mục 9.
- `novalidate` để dùng thông báo của jQuery Validation thay bong bóng mặc định của trình duyệt.

### 9.5. Trang sinh viên & giảng viên
- `StudentPortal/Registration`: bộ đếm “Đã đăng ký x / y lớp”, badge trạng thái, trạng thái rỗng; form Enroll/Drop giữ nguyên.
- `StudentPortal/Profile`: avatar chữ viết tắt bằng CSS thay `ui-avatars.com` (không gửi tên sinh viên ra dịch vụ ngoài, S1); thông tin dùng `<dl>`.
- `StudentPortal/MyGrades`: thẻ thống kê mới (bỏ class Bootstrap 4 không tồn tại), “Passed/Not Passed” → “Đạt/Không đạt”, điểm tô màu đạt/không đạt; công thức tín chỉ và CPA giữ nguyên.
- `Faculty/CourseGrades`: mỗi ô điểm có `<label>` ẩn + `id`, gợi ý thang 0–10, nút “Quay lại” + “Lưu điểm số”; tên field `updates[i].EnrollmentId/Grade` và `courseId` giữ nguyên.

### 9.6. Trang Home & trang lỗi
- `Home/Index`: Việt hóa toàn bộ; thẻ thống kê chỉ là liên kết khi người dùng là Administrator (tránh dẫn Student/Faculty tới trang bị từ chối); bỏ link `/Faculties` (route không tồn tại). Biểu đồ dùng màu từ biến CSS, có khung chiều cao cố định, `aria-label` và nhãn “Dữ liệu minh họa” vì số liệu đang cố định trong view.
- `Shared/Error`: thông điệp tiếng Việt thân thiện, nút Quay lại / Về trang Tổng quan, phần dành cho nhà phát triển thu gọn trong `<details>`. `Home/Privacy`: Việt hóa.

### 9.7. Kiểm tra
- `dotnet build SIMS.sln`: 0 cảnh báo, 0 lỗi. `dotnet test SIMS.sln`: 84/84 pass.
- Render các trang theo 3 vai trò (dữ liệu seed của `DbInitializer`) ở 375px và 1280px: không trang nào cuộn ngang, không lỗi JavaScript; đã thử menu offcanvas, dropdown xuất dữ liệu, ô tìm kiếm và trạng thái lỗi validation của form.
