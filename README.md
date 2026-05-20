<p align="center">
  <a href="https://www.uit.edu.vn/" title="Trường Đại học Công nghệ Thông tin" style="border: 5;">
    <img src="https://i.imgur.com/WmMnSRt.png" alt="Trường Đại học Công nghệ Thông tin | University of Information Technology">
  </a>
</p>

<!-- Title -->
<h1 align="center"><b>IS208 - QUẢN LÝ DỰ ÁN CNTT</b></h1>

# ☕ Phụng Lộc Coffee - Hệ Thống Quản Lý Chuỗi Cửa Hàng F&B

## Giới thiệu chung
- **Môn học:** Quản lý dự án CNTT (IS208.P21) - Năm học 2025-2026
- **Giảng viên:** ThS. Tạ Việt Phương

## Thành viên nhóm Capitalist
| STT | MSSV | Họ tên |
|-----|------|--------|
| 1 | 24520137 | Vũ Lê Minh Anh |
| 2 | 24521127 | Liên Yến Ngân | 
| 3 | 24521808 | Bùi Phan Giáng Trân | 
| 4 | 24521091 | Hoàng Ái Mỹ | 
| 5 | 24520419 | Nguyễn Thị Lam Giang | 

## 📌 Giới thiệu dự án
Phụng Lộc Coffee POS là giải pháp quản lý bán hàng và tồn kho tập trung cho chuỗi cửa hàng F&B. Hệ thống được xây dựng nhằm thay thế việc quản lý bằng Excel thủ công, giúp đồng bộ dữ liệu giữa 8 chi nhánh, kiểm soát hao hụt nguyên liệu và cung cấp báo cáo doanh thu thời gian thực cho Ban Giám đốc.

## Tính năng chính
- POS & Billing (offline mode)
- Quản lý kho, cảnh báo tồn
- Quản lý nhà cung cấp
- Báo cáo & xuất PDF
- Phân quyền (Admin, Quản lý, Nhân viên)

## 📌 Hướng dẫn cài đặt

Để triển khai dự án thành công, vui lòng thực hiện chi tiết theo các bước sau:

## 1. Yêu cầu hệ thống

- **Công cụ lập trình:** Visual Studio 2022 (với gói .NET Desktop Development).
- **Framework:** .NET 8.0 hoặc .NET 9.0.
- **Hệ quản trị CSDL:** SQL Server 2019/2022 và SQL Server Management Studio (SSMS).

---

## 2. Tải mã nguồn dự án

Sử dụng Git để clone repository về máy cục bộ:

```bash
git clone https://github.com/Capitalist-IS208/PhungLocCoffee.git
```

---

## 3. Thiết lập Cơ sở dữ liệu (Database)

- Mở SSMS và kết nối vào Server SQL của máy.
- Vào menu **File -> Open -> File...** và chọn file:

```text
Database/PhungLocCoffee_FullDB.sql
```

- Nhấn nút **Execute (F5)** để khởi tạo Database và dữ liệu mẫu.

---

## 4. Cài đặt thư viện (NuGet Packages)

Mở Project trong Visual Studio, chuột phải vào **Solution** -> chọn **Manage NuGet Packages for Solution** và cài đặt/kiểm tra các thư viện sau:

- **MahApps.Metro.IconPacks:** Hệ thống Icon Material/FontAwesome.
- **MaterialDesignThemes:** Thư viện UI components.
- **Microsoft.EntityFrameworkCore.SqlServer:** Thư viện kết nối SQL Server.
- **QuestPDF** hoặc **iTextSharp:** Thư viện hỗ trợ xuất file báo cáo PDF.

---

## 5. Cấu hình Chuỗi kết nối (Connection String)

Mở file `App.config` (đối với Frontend) hoặc `appsettings.json` (đối với Backend) và thay đổi `Data Source` phù hợp với tên máy:

```xml
<!-- Ví dụ trong App.config -->
<connectionStrings>
    <add name="PhungLocDB" 
         connectionString="Data Source=YOUR_PC_NAME;Initial Catalog=PhungLocCoffee;Integrated Security=True" 
         providerName="System.Data.SqlClient" />
</connectionStrings>
```

## Công nghệ
- **Ngôn ngữ:** C# (WPF) cho Desktop App.
- **Kiến trúc:** 3-Tier Architecture (Presentation - Business - Data).
- **Cơ chế đồng bộ:** Sử dụng GUID (Uniqueidentifier) đồng bộ từ SQLite lên SQL Server.
- **Quản lý:** Git/GitHub.
