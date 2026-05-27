# ĐỒ ÁN: HỆ THỐNG QUẢN LÝ CHUỖI CỬA HÀNG CÀ PHÊ PHỤNG LỘC
**Mã đồ án:** IS208.Q21
**Loại dự án:** Desktop Application – Multi-branch  
**Nhóm thực hiện:** Capitalist

---

## 1. Giới thiệu & Vấn đề kinh doanh
Phụng Lộc Coffee là chuỗi F&B với 8 chi nhánh. Hệ thống này được xây dựng nhằm giải quyết các vấn đề:
- **Hao hụt nguyên liệu:** Hiện trạng ~5-7%, mục tiêu giảm xuống <2%.
- **Dữ liệu phân mảnh:** Chuyển đổi từ quản lý Excel rời rạc sang CSDL tập trung.
- **Tính thời gian thực:** Cung cấp báo cáo doanh thu và tồn kho real-time cho Ban giám đốc.
- **Hoạt động Offline:** Đảm bảo POS bán hàng vẫn hoạt động khi mất kết nối internet và đồng bộ lại khi có mạng.

## 2. Giải pháp kỹ thuật & Phạm vi
- **Công nghệ:** C# WPF (.NET 8), SQL Server (Trung tâm), SQLite (Local Sync).
- **Kiến trúc:** Hybrid (Waterfall cho phần lõi xử lý Kho/BOM; Scrum cho Dashboard báo cáo).
- **Phạm vi nghiệp vụ:**
  - **POS:** Bán hàng nhanh, xử lý giao dịch < 2 giây.
  - **BOM (Recipe):** Tự động trừ kho theo định mức công thức pha chế.
  - **Kho hàng:** Nhập - Xuất - Điều chuyển - Kiểm kê đa chi nhánh.
  - **Đồng bộ:** Cơ chế `IsSynced` giúp lưu trữ cục bộ tại SQLite và đẩy về SQL Server khi có mạng.

## 3. Cấu trúc Source Code
- `PhungLocCoffee_POS/`: Ứng dụng Desktop (WPF) theo mô hình MVVM/3-Tier.
- `Database/`: Script SQL Server và công cụ khởi tạo dữ liệu.
- `README.md`: Tài liệu hướng dẫn này.

## 4. Hướng dẫn cài đặt

### Bước 1: Khởi tạo Cơ sở dữ liệu (SQL Server)
Mở PowerShell tại thư mục `Database/` và chạy lệnh:
```powershell
.\Init-Database.ps1
```

### Bước 2: Build & Chạy ứng dụng
1. Mở file `PhungLocCoffee_POS.slnx` (hoặc `.sln`) bằng Visual Studio 2022.
2. Kiểm tra chuỗi kết nối trong `Helpers/LocalDatabaseHelper.cs` (mặc định trỏ về `localhost`).
3. Nhấn **F5** để chạy ứng dụng.

**Thông tin đăng nhập Demo:**
- **Tài khoản:** `admin`
- **Mật khẩu:** `123`

## 5. Tiêu chí thành công & Kết quả bản Demo
- **Tồn kho:** Hiển thị cảnh báo trực quan khi nguyên liệu dưới ngưỡng (MinStock).
- **Báo cáo:** Dashboard hiển thị doanh thu real-time của 8 chi nhánh.
- **BOM:** Công thức được cấu trúc theo nhóm (Cà phê, Sữa, Topping...) giúp quản lý dễ dàng.
- **Hiệu suất:** Giao diện tối ưu, phản hồi tức thì ngay cả khi xử lý lượng dữ liệu lớn.
