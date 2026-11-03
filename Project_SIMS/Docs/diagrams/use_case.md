# Mô tả Sơ đồ Use Case (Use Case Diagram)

Sơ đồ Use Case thể hiện phân quyền chức năng theo 3 tác nhân (actors) trong hệ thống SIMS:
- **Administrator**: Quản lý sinh viên, môn học (CRUD, xóa mềm) và xuất báo cáo phạm vi toàn trường (CSV/Excel).
- **Faculty**: Xem danh sách môn giảng dạy, nhập/sửa điểm sinh viên và xuất bảng điểm các môn được phân công.
- **Student**: Tra cứu hồ sơ, đăng ký/hủy học phần (tự động kiểm tra môn tiên quyết và trần tín chỉ), xem GPA/CPA và xuất kết quả học tập (CSV/Excel/PDF).

Các thao tác xuất dữ liệu và đăng ký học phần đều bao gồm (`<<include>>`) chức năng ghi nhật ký kiểm toán (`AuditLog`).

