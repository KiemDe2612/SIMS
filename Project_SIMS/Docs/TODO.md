# KẾ HOẠCH CÁC TÍNH NĂNG CẦN THỰC HIỆN TIẾP THEO (TODO.md)

## 1. Tính năng kiểm tra trùng lịch học (Timetable Collision Check)
- **Hiện trạng:** Chưa thực hiện trong đợt nâng cấp này vì hệ thống hiện tại chưa có các trường và thực thể lưu trữ thông tin thời khóa biểu.
- **Yêu cầu cần bổ sung về dữ liệu (Schema DB):**
  - Cần thêm bảng `Schedule` hoặc các trường trong `Course` / `CourseSection`:
    - `DayOfWeek` (Thứ trong tuần: Thứ 2, 3, 4, 5, 6, 7).
    - `StartSlot` / `StartTime` (Ca học bắt đầu: ví dụ Slot 1, Slot 2,...).
    - `EndSlot` / `EndTime` (Ca học kết thúc).
    - `Room` (Phòng học).
- **Câu hỏi gửi người dùng:**
  > *"Để hiện thực hóa kiểm tra trùng lịch (Timetable conflict), chúng ta cần bổ sung trường thời gian học (Thứ, Ca học / Slot) vào entity `Course` hoặc tách riêng thành bảng `CourseSchedule`. Bạn có đồng ý cho phép cập nhật Entity và tạo Migration bổ sung Schema DB cho phần này không?"*

---

## 2. Các hạng mục cải tiến tiếp theo
- [ ] Chức năng Phục hồi (Restore) cho sinh viên và môn học bị xóa mềm (`IsDeleted = false`).
- [ ] Policy-based Authorization (ví dụ: Giảng viên chỉ sửa điểm trong thời hạn cho phép).
- [ ] Tích hợp tính điểm tự động từ `TranscriptService` ghi thẳng vào bảng `AcademicRecord`.

