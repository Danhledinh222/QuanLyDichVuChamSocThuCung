# 🐾 PetCare - Hệ thống Quản lý Dịch vụ Chăm sóc Thú cưng

**PetCare** là một ứng dụng Web được phát triển dựa trên nền tảng **ASP.NET Core MVC (.NET 8)**, cung cấp giải pháp số hóa toàn diện cho các cơ sở kinh doanh dịch vụ thú cưng (Spa, Khám bệnh, Grooming). Dự án tập trung vào việc tự động hóa luồng đặt lịch, quản lý hồ sơ y tế tập trung và đặc biệt là hệ thống kiểm soát kho vật tư chặt chẽ thông qua SQL Triggers.

---

## 🚀 Các tính năng nổi bật (Core Features)

Hệ thống được chia thành 5 phân hệ chính, phục vụ đa dạng đối tượng người dùng (Admin, Staff, Customer):

### 1. Phân hệ Khách hàng & Sổ y bạ (Customer & Pet Profile)
- Đăng ký, đăng nhập và xác thực tài khoản an toàn.
- Quản lý danh sách thú cưng chi tiết theo hệ thống danh mục chuẩn (Loài/Giống - `PetSpecies` & `PetBreed`).
- Lưu trữ và tra cứu lịch sử khám bệnh, dịch vụ đã sử dụng của từng thú cưng.

### 2. Phân hệ Đặt lịch & Vận hành (Booking & Operations)
- Khách hàng đặt lịch hẹn trực tuyến đa bước, dễ dàng chọn dịch vụ theo Menu (`ServiceCatalog`).
- Thuật toán kiểm tra chống trùng lịch và quá tải khung giờ.
- Bảng phân công nhân viên (Mapping N-N) giúp theo dõi năng suất và tính KPI cho từng thợ/bác sĩ.
- Quản lý trạng thái luồng nghiêm ngặt thông qua bảng từ điển (`BookingStatus`, `DetailStatus`): *Pending -> In Progress -> Done*.

### 3. Phân hệ Kho vật tư Thông minh (Smart Inventory)
Đây là module cốt lõi và phức tạp nhất của hệ thống:
- **Tự động trừ kho:** Sử dụng Bảng định mức (`ServiceMaterialQuota`), hệ thống cấu hình sẵn 1 dịch vụ tiêu hao bao nhiêu vật tư. Khi dịch vụ hoàn thành, SQL Trigger tự động trừ chính xác số lượng trong kho (`MedicalSupply`).
- **Tự động hoàn kho:** Nếu trạng thái dịch vụ bị hủy hoặc quay lại trạng thái trước, vật tư được cộng trả lại tự động.
- **Nhật ký đối soát:** Mọi biến động xuất/nhập kho đều được ghi vết chi tiết (`InventoryTransaction`) giúp chủ shop chống thất thoát tài sản.

### 4. Phân hệ Hóa đơn & Tài chính (Billing & Finance)
- Tự động gom nhóm các dịch vụ hoàn thành để xuất Hóa đơn (`Invoice`).
- Áp dụng các thuật toán tính toán Thuế, Mã giảm giá (`Promotion`).
- Hỗ trợ ghi nhận nhiều phương thức thanh toán (`PaymentMethod`) cho cùng một hóa đơn (ví dụ: trả một nửa tiền mặt, một nửa chuyển khoản).

---

## 🗄️ Kiến trúc Cơ sở dữ liệu (Database Architecture)

Cơ sở dữ liệu **PetCareDB** được thiết kế chuẩn hóa 3NF cấp độ doanh nghiệp:
- **Tổng số lượng:** 22 Bảng thực thể.
- **Ràng buộc (Constraints):** Sử dụng triệt để Primary Key, Foreign Key, Unique (chống trùng lặp email/sđt) và Check Constraint (đảm bảo giá tiền, số lượng kho không bị âm).
- **Audit Columns:** 100% các bảng nghiệp vụ đều có các trường `CreatedAt`, `ModifiedAt`, và `IsDeleted` để phục vụ cơ chế **Xóa mềm (Soft Delete)**.
- **Logic xử lý dưới DB:** Ứng dụng mạnh mẽ Stored Procedures và Triggers để giảm tải cho tầng Application và đảm bảo tính toàn vẹn Transaction (ACID).

---

## 🛠️ Nền tảng Công nghệ (Tech Stack)

- **Ngôn ngữ Backend:** C# 12
- **Framework:** ASP.NET Core MVC (.NET 8.0 LTS)
- **Database:** Microsoft SQL Server 2022
- **ORM:** Entity Framework Core (Database First / Code First)
- **Frontend:** HTML5, CSS3, JavaScript, AJAX, Bootstrap 5 / Tailwind CSS
- **Quản lý phiên bản:** Git & GitHub

---

## ⚙️ Hướng dẫn cài đặt (Installation)

Để khởi chạy dự án trên môi trường Local, vui lòng làm theo trình tự các bước dưới đây:

### Yêu cầu môi trường (Prerequisites)
- Visual Studio 2022 (v17.8+) với workload *ASP.NET and web development*.
- .NET 8.0 SDK.
- SQL Server (2019/2022) & SQL Server Management Studio (SSMS).

### Các bước cài đặt
**Bước 1: Clone kho lưu trữ**
```bash
git clone [https://github.com/Danhledinh222/QuanLyDichVuChamSocThuCung.git](https://github.com/Danhledinh222/QuanLyDichVuChamSocThuCung.git)
