# UI/UX AUDIT – SIMS.Web

Phạm vi kiểm tra: `Views/Shared/_Layout.cshtml`, `_LoginPartial.cshtml`, `Error.cshtml`, `Views/Home`, `Views/Students`, `Views/Courses`, `Views/Faculty`, `Views/StudentPortal` và `wwwroot/css/site.css`.
Trạng thái: ghi nhận hiện trạng **trước khi sửa** (Bước 1). Mức độ: 🔴 nghiêm trọng · 🟠 quan trọng · 🟡 nhỏ.

---

## 1. Bố cục & điều hướng (`_Layout.cshtml`)

| # | Mức | Vấn đề |
|---|-----|--------|
| L1 | 🔴 | Sidebar dùng `d-none d-md-block` → **trên điện thoại (< 768px) không còn menu điều hướng nào**. Nút hamburger chỉ mở phần đăng nhập/đăng xuất. |
| L2 | 🟠 | Trạng thái “active” của menu được gán bằng JavaScript sau khi tải trang → link “Tổng quan” luôn active trong HTML, nhấp nháy khi tải; không có `aria-current`. |
| L3 | 🟠 | Bố cục `row` + `col-md-3 col-lg-2` + các view lại bọc thêm `container-fluid px-4` → khoảng đệm bị cộng dồn (40px + 24px), nội dung bị bóp hẹp; trên mobile lề > 16px. |
| L4 | 🟠 | `_LoginPartial` render một `<ul>` lồng trong `<ul>` của layout (HTML không hợp lệ). Tên người dùng dài (email) chiếm hết thanh trên mobile. Không hiển thị vai trò hiện tại. |
| L5 | 🟠 | Bootstrap CSS/JS tải từ CDN (5.3.0) dù `wwwroot/lib/bootstrap` (5.3.3) có sẵn; Font Awesome tải từ CDN chỉ để dùng vài icon trùng với Bootstrap Icons. |
| L6 | 🔴 | jQuery **không được nạp** trong layout, trong khi `_ValidationScriptsPartial` cần jQuery → **validation phía client không chạy** ở mọi form Create/Edit (lỗi JS trên console). |
| L7 | 🟠 | Font “Outfit” (Google Fonts) không có bộ ký tự tiếng Việt (Latin Extended Additional) → các chữ như “ế, ờ, ữ” rơi về font khác, chữ bị lệch nét/không đồng đều. |
| L8 | 🟡 | `<html lang="en">` trong khi nội dung là tiếng Việt (ảnh hưởng trình đọc màn hình, tách từ). Không có link “Bỏ qua điều hướng” (skip link). |
| L9 | 🟡 | Chart.js được nạp trên **mọi** trang (chỉ trang Home dùng) và không cố định phiên bản. |
| L10 | 🟡 | Hiệu ứng `fadeIn`, `translateY` khi hover thẻ không tôn trọng `prefers-reduced-motion`. |

## 2. Màu sắc & độ tương phản (`site.css`)

| # | Mức | Vấn đề |
|---|-----|--------|
| C1 | 🟠 | `--primary: hsl(239 84% 67%)` (~#6366F1) với chữ trắng ≈ 4.4:1 — **dưới chuẩn AA 4.5:1** cho chữ thường; `.text-primary` trên nền trắng cũng ≈ 4.4:1. |
| C2 | 🟠 | Nút `.btn-primary` dùng gradient sang `--accent` (~#C266F0): chữ trắng trên phần tím chỉ ≈ 3:1. Link sidebar active dùng cùng gradient. |
| C3 | 🟠 | `btn-outline-info` (“Xuất DS Đăng ký”, Courses/Index): chữ #0dcaf0 trên nền trắng ≈ 1.9:1 — không đạt AA. |
| C4 | 🟡 | Bảng màu riêng (`--primary`) không đồng bộ với biến Bootstrap (`--bs-primary`) → `btn-outline-primary`, focus ring, badge, link mặc định vẫn là xanh #0d6efd. Trang Home dùng thêm mã màu cứng (#2563EB…). |
| C5 | 🟡 | Chưa có hệ thống khoảng cách / bo góc thống nhất (`0.5rem`, `0.75rem`, `1rem`, `1.5rem` dùng lẫn lộn); thẻ `.card` nhấc lên khi hover dù không bấm được. |
| C6 | 🟡 | Không có kiểu `:focus-visible` rõ ràng cho link sidebar / nút tùy biến. |

## 3. Bảng dữ liệu

| # | Mức | Trang | Vấn đề |
|---|-----|-------|--------|
| T1 | 🟠 | Tất cả | Không có hàng xen kẽ (`table-striped`), header bảng không có `scope="col"`, không có `<caption>`/tiêu đề gắn với bảng. |
| T2 | 🟠 | Students, Courses, Registration | **Không có trạng thái rỗng**: khi không có dữ liệu chỉ hiện header trống. |
| T3 | 🟠 | Students, Courses, Faculty | **Không có ô tìm kiếm / lọc**; không hiển thị tổng số bản ghi. |
| T4 | 🟠 | Tất cả | Trên 375px bảng 6 cột phải cuộn ngang trong thẻ; cột “Hành động” nằm ngoài màn hình. |
| T5 | 🟡 | Courses/Index | Nút Sửa/Xóa chỉ có icon, thiếu `aria-label` (chỉ có `title` ở nút Sửa, nút Xóa không có gì) → trình đọc màn hình đọc “button”. |
| T6 | 🟡 | MyGrades | Trạng thái “Passed / Not Passed” bằng tiếng Anh, lẫn với giao diện tiếng Việt. |

## 4. Nút bấm & hành động

| # | Mức | Vấn đề |
|---|-----|--------|
| B1 | 🟡 | Nút xóa (Students, Courses) và “Hủy lớp” (Registration) **đã có** `confirm()` → giữ nguyên, chỉ cần làm rõ nhãn nút. |
| B2 | 🟠 | Thanh công cụ trang danh sách: các nút đặt cạnh tiêu đề bằng `d-flex justify-content-between` không xuống dòng → tràn ngang trên mobile (Courses có 3 nút). |
| B3 | 🟡 | Courses/Index có 2 dropdown xuất dữ liệu tách rời + 1 dropdown “Điểm” trong từng hàng → toolbar rối. Có thể gộp thành một dropdown “Xuất dữ liệu” có nhóm. |
| B4 | 🟡 | Thứ tự nút trong form: “Lưu” trước “Hủy bỏ”, không có vùng hành động tách biệt; trên mobile nút nhỏ, khó bấm (< 44px). |
| B5 | 🟡 | Dropdown toggle trong hàng (Courses, Faculty) thiếu `aria-expanded`/`aria-label`. |

## 5. Form (Create / Edit)

| # | Mức | Vấn đề |
|---|-----|--------|
| F1 | 🔴 | **Students/Create luôn thất bại im lặng**: `CreateStudentDto.Email` có `[Required]` nhưng form không có trường Email; lỗi thuộc về field `Email` nên `asp-validation-summary="ModelOnly"` **không hiển thị** → người dùng bấm Lưu, trang tải lại mà không có thông báo. *(Nguyên nhân gốc nằm ở DTO/Controller — ngoài phạm vi chỉnh sửa UI; xem mục 9.)* |
| F2 | 🟠 | Các trường không được nhóm theo chủ đề (tài khoản / thông tin cá nhân / phân công). |
| F3 | 🟠 | Không đánh dấu trường bắt buộc (`*`), không có chú thích bắt buộc. |
| F4 | 🟠 | Lỗi validation chỉ là chữ đỏ nhỏ, input không đổi viền; tóm tắt lỗi không có khung/biểu tượng. |
| F5 | 🟡 | Thứ tự “Tên” trước “Họ & Tên đệm” ngược với thói quen người Việt. |
| F6 | 🟡 | Tiêu đề trang dùng `h1` 2.5rem + icon, chiếm nhiều chỗ trên mobile; breadcrumb đặt dưới tiêu đề. |
| F7 | 🟡 | Faculty/CourseGrades: ô nhập điểm **không có nhãn** (chỉ placeholder), không có gợi ý thang điểm 0–10. |

## 6. Thông báo TempData

| # | Mức | Vấn đề |
|---|-----|--------|
| N1 | 🟠 | Khối alert TempData bị **lặp lại trong 5 view**; các trang khác (MyGrades, Profile, Faculty/Index) không hiển thị → lỗi xuất file từ `StudentPortal.ExportMyGrades` / `Faculty.ExportGrades` có thể bị “nuốt”. |
| N2 | 🟡 | Một số thông báo từ controller là tiếng Anh (“Student created successfully!”) — nằm trong Controller, không sửa được ở bước UI. |
| N3 | 🟡 | Alert không phân biệt `role="status"` (thành công) và `role="alert"` (lỗi). |

## 7. Trang Home & trang lỗi

| # | Mức | Vấn đề |
|---|-----|--------|
| H1 | 🟠 | Toàn bộ trang Home là **tiếng Anh** (Dashboard, Total Students, View Details…). |
| H2 | 🟠 | Thẻ “Total Faculties” link tới `/Faculties` — **route không tồn tại (404)**. Thẻ Students/Courses link tới trang chỉ Admin truy cập được, nhưng hiển thị cho mọi vai trò (Student bấm vào sẽ bị từ chối). |
| H3 | 🟡 | Biểu đồ “Students by Department” dùng **dữ liệu cứng** (45/25/30) và bảng “Recent Enrollments” luôn rỗng — không có chú thích, dễ gây hiểu lầm. Canvas không có chiều cao cố định → biểu đồ co giãn bất thường. |
| H4 | 🟡 | Thẻ thống kê dùng `display-4` + nền đặc `bg-warning` → con số rất to trên mobile; `bg-primary bg-opacity-75` ở footer giảm tương phản. |
| E1 | 🟠 | `Error.cshtml` hoàn toàn tiếng Anh, giọng kỹ thuật (hướng dẫn biến môi trường) hiển thị cho người dùng cuối; không có nút quay về. |
| E2 | 🟡 | `Privacy.cshtml` tiếng Anh. |

## 8. Trang sinh viên

| # | Mức | Trang | Vấn đề |
|---|-----|-------|--------|
| S1 | 🟠 | Profile | Avatar tải từ dịch vụ ngoài `ui-avatars.com`, tên sinh viên **không được URL-encode** và bị gửi ra bên thứ ba (quyền riêng tư). |
| S2 | 🟡 | Profile | Danh sách thông tin dùng `row/col + <hr>` thay vì `<dl>`; nhãn “Khoa (Department)” lẫn tiếng Anh. |
| S3 | 🟡 | MyGrades | Dùng class Bootstrap 4 không còn tồn tại (`no-gutters`, `mr-2`, `text-xs`, `font-weight-bold`, `fa-2x` cho icon `bi`). |
| S4 | 🟡 | Registration | Tiêu đề thẻ màu `text-success` khác các trang còn lại. |

## 9. Ngoài phạm vi (cần sửa ở Controller/DTO – không sửa trong đợt này)

1. **F1** – `CreateStudentDto.Email` `[Required]` nhưng UserFactory đã tự sinh email khi trống → nên bỏ `[Required]` (giữ `[EmailAddress]`) hoặc thêm trường Email vào form.
2. `HomeController` không có action `Error` trong khi `Program.cs` dùng `UseExceptionHandler("/Home/Error")` → trang lỗi production sẽ 404.
3. Thông báo TempData tiếng Anh trong `StudentsController`, `CoursesController`, `FacultyController`.
4. `Courses/Index` so sánh `AssignedFacultyName == "Assigned"` (chuỗi cờ) — nên trả về tên giảng viên thật.
5. Dropdown giảng viên hiển thị `FacultyId` (mã) thay vì họ tên.
6. Bootstrap Icons không có trong `wwwroot/lib` → vẫn phải tải từ CDN đang dùng (jsdelivr, 1.11.0). Nên thêm bằng LibMan để chạy offline.
7. Form Đăng xuất trong `_LoginPartial` không khai báo `method="post"` (có từ trước, giữ nguyên để không đổi hành vi) → trình duyệt gửi GET tới trang Logout của Identity thay vì đăng xuất trực tiếp.
8. `Views/Home/Privacy.cshtml` không có action tương ứng trong `HomeController` (truy cập `/Home/Privacy` trả về 404).
9. Thông báo validation mặc định của DataAnnotations là tiếng Anh (“The StudentId field is required.”) — cần thêm `ErrorMessage` tiếng Việt trong DTO.
