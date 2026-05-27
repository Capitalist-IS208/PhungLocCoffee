using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PhungLocCoffee_POS.Models;
using PhungLocCoffee_POS.Views;
using PhungLocCoffee_POS.Helpers;

namespace PhungLocCoffee_POS.Views
{
    public partial class SuppliersView : UserControl
    {
        // 1. Khai báo biến để lưu thông tin User
        private readonly UserSession _currentUser;

        // 2. Sửa constructor nhận tham số UserSession
        public SuppliersView(UserSession currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser; // Gán giá trị

            // Gọi hàm nạp dữ liệu giả nếu có
            LoadDummyData();
        }

        // Nếu bạn muốn test mà không dùng currentUser, bạn có thể giữ thêm constructor rỗng này
        public SuppliersView()
        {
            InitializeComponent();
            _currentUser = new UserSession();
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            var dummySuppliers = new List<Supplier>
            {
                new Supplier { Name = "Công ty Cà phê Trung Nguyên", Phone = "028 3925 3333", Status = "Đang hợp tác" },
                new Supplier { Name = "Vinamilk Việt Nam", Phone = "1900 636 979", Status = "Đang hợp tác" },
                new Supplier { Name = "Đường Biên Hòa", Phone = "0251 383 6199", Status = "Tạm ngưng" },
                new Supplier { Name = "Nước khoáng La Vie", Phone = "1900 1906", Status = "Đang hợp tác" },
                new Supplier { Name = "Trà Phúc Long", Phone = "028 6263 0377", Status = "Đang hợp tác" },
                new Supplier { Name = "Bao bì Nhựa Tân Hiệp Phát", Phone = "0274 375 5678", Status = "Đang hợp tác" },
                new Supplier { Name = "Đá viên Tinh Khiết Sài Gòn", Phone = "0901 234 567", Status = "Đang hợp tác" }
            };

            dgSuppliers.ItemsSource = dummySuppliers;
        }
    }

    public class Supplier
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}


