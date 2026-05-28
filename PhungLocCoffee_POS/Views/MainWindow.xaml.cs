using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq;

using PhungLocCoffee_POS.Models;
using PhungLocCoffee_POS.Views;
using PhungLocCoffee_POS.Helpers;

namespace PhungLocCoffee_POS.Views
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private SalesView? _salesView;
        private readonly UserSession _currentUser;

        private string _currentTime = string.Empty;
        public string CurrentTime { get => _currentTime; set { _currentTime = value; OnPropertyChanged(); } }

        private string _avatarChar = string.Empty;
        public string AvatarChar { get => _avatarChar; set { _avatarChar = value; OnPropertyChanged(); } }

        private string _shortName = string.Empty;
        public string ShortName { get => _shortName; set { _shortName = value; OnPropertyChanged(); } }

        private string _fullName = string.Empty;
        public string FullName { get => _fullName; set { _fullName = value; OnPropertyChanged(); } }

        private string _roleAndBranch = string.Empty;
        public string RoleAndBranch { get => _roleAndBranch; set { _roleAndBranch = value; OnPropertyChanged(); } }

        private string _currentShiftStatus = string.Empty;
        public string CurrentShiftStatus { get => _currentShiftStatus; set { _currentShiftStatus = value; OnPropertyChanged(); } }

        private int _pendingOfflineOrders;
        public int PendingOfflineOrders { get => _pendingOfflineOrders; set { _pendingOfflineOrders = value; OnPropertyChanged(); } }

        private string placeholderText = "Tìm món hoặc nguyên liệu...";
        private DispatcherTimer notificationTimer;

        public MainWindow(UserSession user)
        {
            InitializeComponent();

            _currentUser = user;
            DataContext = this;

            FullName = user.FullName;
            ShortName = user.RoleName;
            AvatarChar = string.IsNullOrEmpty(user.FullName) ? "U" : user.FullName.Substring(0, 1);

            if (user.IsAdmin)
            {
                RoleAndBranch = user.RoleName; // Admin thì không hiện chi nhánh
            }
            else
            {
                RoleAndBranch = $"{user.RoleName} • {user.BranchName}";
            }

            CurrentShiftStatus = "Đang trong ca sáng (06:00 - 14:00)";
            PendingOfflineOrders = GetPendingOfflineOrders();

            ApplyPermission();

            MainContent.Children.Clear();
            MainContent.Children.Add(new HomeView(_currentUser));

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("dd/MM/yyyy • hh:mm tt");
            };
            timer.Start();

            notificationTimer = new DispatcherTimer();
            notificationTimer.Interval = TimeSpan.FromSeconds(3);
            notificationTimer.Tick += (s, e) =>
            {
                SearchNotificationPopup.IsOpen = false;
                notificationTimer.Stop();
            };
        }

        public MainWindow() : this(new UserSession())
        {
        }

        private void SearchProductsAndIngredients(string keyword)
        {
            try
            {
                string connStr = System.Configuration.ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
                using (var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr))
                {
                    conn.Open();
                    string sql = @"
                        SELECT 'Món ăn' as Type, ProductName as Name FROM Products WHERE ProductName LIKE @keyword AND IsActive = 1
                        UNION
                        SELECT 'Nguyên liệu', IngredientName FROM Ingredients WHERE IngredientName LIKE @keyword";

                    using (var cmd = new Microsoft.Data.SqlClient.SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@keyword", "%" + keyword + "%");
                        using (var reader = cmd.ExecuteReader())
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder();
                            int count = 0;
                            while (reader.Read() && count < 5)
                            {
                                sb.AppendLine($"- [{reader["Type"]}] {reader["Name"]}");
                                count++;
                            }

                            if (count > 0)
                            {
                                txtNotificationMessage.Text = $"Tìm thấy {count} kết quả:\n{sb.ToString()}";
                            }
                            else
                            {
                                txtNotificationMessage.Text = "Không tìm thấy món ăn hoặc nguyên liệu nào.";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                txtNotificationMessage.Text = "Lỗi khi tìm kiếm: " + ex.Message;
            }

            SearchNotificationPopup.IsOpen = true;
            notificationTimer.Stop();
            notificationTimer.Start();
        }

        private void ApplyPermission()
        {
            // Mặc định hiển thị tất cả
            btnHome.Visibility = Visibility.Visible;
            btnSales.Visibility = Visibility.Visible;
            btnInventory.Visibility = Visibility.Visible;
            btnReports.Visibility = Visibility.Visible;
            btnBOM.Visibility = Visibility.Visible;
            btnSuppliers.Visibility = Visibility.Visible;

            if (_currentUser.IsAdmin)
            {
                // Admin: Thấy tất cả (không làm gì thêm)
                return;
            }

            if (_currentUser.IsManager)
            {
                // Manager: Thấy tất cả (theo code hiện tại), nhưng logic ReportsView sẽ xử lý việc lọc chi nhánh
                return;
            }

            if (_currentUser.IsAccountant)
            {
                // Kế toán: Trang chủ, Kho hàng, Nhà cung cấp, Báo cáo.
                // Ẩn: Bán hàng, Công thức.
                btnSales.Visibility = Visibility.Collapsed;
                btnBOM.Visibility = Visibility.Collapsed;
                return;
            }

            if (_currentUser.IsStaff)
            {
                // Staff: Trang chủ, Bán hàng, Công thức, Kho hàng.
                // Ẩn: Nhà cung cấp, Báo cáo.
                btnSuppliers.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                return;
            }

            if (_currentUser.IsInventoryKeeper)
            {
                // Giữ nguyên logic cũ cho InventoryKeeper nếu cần
                btnSales.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                btnBOM.Visibility = Visibility.Collapsed;
                btnSuppliers.Visibility = Visibility.Collapsed;
                return;
            }
        }

        private int GetPendingOfflineOrders()
        {
            try
            {
                using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(
                    LocalDatabaseHelper.GetConnectionString()))
                {
                    conn.Open();

                    string sql = "SELECT COUNT(*) FROM LocalOrders WHERE IsSynced = 0";

                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(sql, conn))
                    {
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
                return 0;
            }
        }
        private async void BtnSyncNow_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentUser.IsInventoryKeeper) return;

                // 1. Chuyển sang tab Bán hàng để người dùng thấy progress đồng bộ
                SetActiveMenu(btnSales, iconSales, txtSales);
                if (_salesView == null)
                {
                    _salesView = new SalesView(_currentUser);
                }
                MainContent.Children.Clear();
                MainContent.Children.Add(_salesView);
                AdminPopup.IsOpen = false;

                // 2. Chờ đồng bộ hoàn tất
                await _salesView.TrySyncOfflineOrdersFromOutside();

                // 3. Cập nhật lại số lượng
                PendingOfflineOrders = GetPendingOfflineOrders();

                if (PendingOfflineOrders > 0)
                {
                    MessageBox.Show(
                        $"Chỉ đồng bộ được một phần.\nCòn {PendingOfflineOrders} đơn chưa thể gửi (kiểm tra lại kết nối server).",
                        "Đồng bộ chưa hoàn tất",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else
                {
                    MessageBox.Show(
                        "Tuyệt vời! Tất cả đơn hàng offline đã được đồng bộ lên hệ thống.",
                        "Đồng bộ thành công",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
            }
            catch (Exception ex)
            {
                PendingOfflineOrders = GetPendingOfflineOrders();
                MessageBox.Show("Lỗi đồng bộ: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            if (AdminPopup.IsOpen) AdminPopup.IsOpen = false;
        }
        public void RefreshPendingOfflineOrders()
        {
            PendingOfflineOrders = GetPendingOfflineOrders();
        }

        private void AdminContainer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AdminPopup.IsOpen = !AdminPopup.IsOpen;
            e.Handled = true;
        }

        private void AdminContainer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void txtSearch_GotFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (txtSearch.Text == placeholderText)
            {
                txtSearch.Text = "";
                txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"));
            }
        }

        private void txtSearch_LostFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            txtSearch.Text = placeholderText;
            txtSearch.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0AEC0"));
        }

        private void txtSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string keyword = txtSearch.Text.Trim();

                if (!string.IsNullOrEmpty(keyword) && keyword != placeholderText)
                {
                    SearchProductsAndIngredients(keyword);
                    Keyboard.ClearFocus();
                }
            }
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            AdminPopup.IsOpen = false;

            LoginWindow login = new LoginWindow();
            Application.Current.MainWindow = login;
            login.Show();

            Close();
        }

        private void ResetMenuState()
        {
            var brushTransparent = new SolidColorBrush(Colors.Transparent);
            var brushIconGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0AEC0"));
            var brushTextGray = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));

            btnHome.Background = brushTransparent;
            iconHome.Foreground = brushIconGray;
            txtHome.Foreground = brushTextGray;

            btnSales.Background = brushTransparent;
            iconSales.Foreground = brushIconGray;
            txtSales.Foreground = brushTextGray;

            btnInventory.Background = brushTransparent;
            iconInventory.Foreground = brushIconGray;
            txtInventory.Foreground = brushTextGray;

            btnReports.Background = brushTransparent;
            iconReports.Foreground = brushIconGray;
            txtReports.Foreground = brushTextGray;

            // Reset màu cho nút BOM
            if (btnBOM != null)
            {
                btnBOM.Background = brushTransparent;
                iconBOM.Foreground = brushIconGray;
                txtBOM.Foreground = brushTextGray;
            }

            // THÊM MỚI: Reset màu cho nút Nhà Cung Cấp
            if (btnSuppliers != null)
            {
                btnSuppliers.Background = brushTransparent;
                iconSuppliers.Foreground = brushIconGray;
                txtSuppliers.Foreground = brushTextGray;
            }
        }

        private void SetActiveMenu(Border btn,
                                   MahApps.Metro.IconPacks.PackIconMaterial? icon = null,
                                   TextBlock? txt = null)
        {
            ResetMenuState();

            btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF7EB"));

            var brushOrange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7A16A"));

            if (icon != null) icon.Foreground = brushOrange;
            if (txt != null) txt.Foreground = brushOrange;
        }

        private void MenuHome_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveMenu(btnHome, iconHome, txtHome);
            MainContent.Children.Clear();
            MainContent.Children.Add(new HomeView(_currentUser));
        }

        private void MenuBOM_Click(object sender, MouseButtonEventArgs e)
        {
            // Set active cho BOM
            SetActiveMenu(btnBOM, iconBOM, txtBOM);
            MainContent.Children.Clear();
            MainContent.Children.Add(new BOMView());
        }

        private void MenuSales_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser.IsInventoryKeeper)
                return;

            SetActiveMenu(btnSales, iconSales, txtSales);

            if (_salesView == null)
            {
                _salesView = new SalesView(_currentUser);
            }

            MainContent.Children.Clear();
            MainContent.Children.Add(_salesView);
        }

        private void MenuSuppliers_Click(object sender, MouseButtonEventArgs e)
        {
            // Nếu bạn muốn hạn chế quyền (ví dụ chỉ Admin/Quản lý mới xem được NCC)
            // thì uncomment dòng dưới, nếu không thì cứ để trống
            // if (_currentUser.IsInventoryKeeper) return;

            // 1. Cập nhật trạng thái menu (đổi màu icon/text của nút được chọn)
            SetActiveMenu(btnSuppliers, iconSuppliers, txtSuppliers);

            // 2. Xóa nội dung cũ trong màn hình chính
            MainContent.Children.Clear();

            // 3. Nạp màn hình quản lý nhà cung cấp
            // Giả sử constructor của SuppliersView nhận _currentUser giống các View khác
            MainContent.Children.Add(new SuppliersView(_currentUser));
        }




        private void MenuInventory_Click(object sender, MouseButtonEventArgs e)
        {
            SetActiveMenu(btnInventory, iconInventory, txtInventory);
            MainContent.Children.Clear();
            MainContent.Children.Add(new InventoryView(_currentUser));
        }

        private void MenuReports_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser.IsInventoryKeeper)
                return;

            SetActiveMenu(btnReports, iconReports, txtReports);
            MainContent.Children.Clear();
            MainContent.Children.Add(new ReportsView(_currentUser));
        }

        public void NavigateToInventory()
        {
            SetActiveMenu(btnInventory, iconInventory, txtInventory);
            MainContent.Children.Clear();
            MainContent.Children.Add(new InventoryView(_currentUser));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

