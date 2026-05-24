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
namespace PhungLocCoffee_POS
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
            LoadDummyData();
        }

        private void LoadDummyData()
        {
            // Code nạp dữ liệu của bạn ở đây...
        }
    }
}
