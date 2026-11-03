# Mô tả Sơ đồ Lớp (Class Diagram)

Sơ đồ lớp thể hiện cấu trúc thực thể nghiệp vụ và 4 mẫu thiết kế chính:
- **Kế thừa & Quan hệ Domain**: Tất cả thực thể kế thừa `BaseEntity`. `Department` liên kết tổng hợp (`Aggregation`) với `Faculty`, `Student`, `Course`. `Course` liên kết hợp thành (`Composition`) với các lượt đăng ký `Enrollment`.
- **Strategy Pattern**: `ITranscriptService` hoán đổi linh hoạt giữa `TenPointStrategy` và `FourPointStrategy` để tính điểm GPA/CPA.
- **Factory Method Pattern**: `IUserFactory` đóng gói khởi tạo `AppUser` theo Role.
- **Decorator Pattern**: `CachingStudentRepository` bọc ngoài repository cơ sở để bổ sung bộ nhớ đệm (`IMemoryCache`).
- **Observer Pattern**: `EnrollmentSubject` phát sự kiện `EnrollmentCreatedEvent` tới `AuditLogEnrollmentObserver` và `NotificationEnrollmentObserver`.

