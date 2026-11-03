# Mô tả Sơ đồ Gói (Package Diagram)

Sơ đồ gói phản ánh chính xác cấu trúc phụ thuộc giữa 6 project theo Clean Architecture:
- `SIMS.Domain`: Lõi trung tâm, không phụ thuộc project nào khác.
- `SIMS.Application`: Chỉ phụ thuộc duy nhất vào `SIMS.Domain`.
- `SIMS.Persistence` & `SIMS.Infrastructure`: Độc lập lẫn nhau, cùng phụ thuộc vào `SIMS.Application` để hiện thực các hợp đồng giao diện (DIP).
- `SIMS.Web`: Đóng vai trò Composition Root, tham chiếu `Application`, `Persistence`, `Infrastructure` để đăng ký DI. Web không tham chiếu trực tiếp `Domain` mà nhận gián tiếp qua `Application`.
- `SIMS.Tests`: Tham chiếu cả 4 project để thực thi Unit và Integration test.

