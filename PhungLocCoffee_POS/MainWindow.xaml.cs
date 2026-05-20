using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Input;

namespace PhungLocCoffee_POS
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
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

        private string placeholderText = "Tìm món, SĐT khách hàng, đơn hàng...";
        private DispatcherTimer notificationTimer;

        public MainWindow(UserSession user)
        {
            InitializeComponent();

            _currentUser = user;
            DataContext = this;

            FullName = user.FullName;
            ShortName = user.RoleName;
            AvatarChar = string.IsNullOrEmpty(user.FullName) ? "U" : user.FullName.Substring(0, 1);
            RoleAndBranch = $"{user.RoleName} • {user.BranchName}";
            CurrentShiftStatus = "Đang trong ca sáng (06:00 - 14:00)";
            PendingOfflineOrders = 0;

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

        private void ApplyPermission()
        {
            btnHome.Visibility = Visibility.Visible;
            btnSales.Visibility = Visibility.Visible;
            btnInventory.Visibility = Visibility.Visible;
            btnReports.Visibility = Visibility.Visible;

            if (_currentUser.IsAdmin || _currentUser.IsManager)
            {
                return;
            }

            if (_currentUser.IsStaff)
            {
                // Staff vẫn được xem Kho và Báo cáo chi nhánh.
                // Nhưng trong InventoryView sẽ chặn Kiểm kho / Nhập xuất.
                return;
            }

            if (_currentUser.IsInventoryKeeper)
            {
                btnSales.Visibility = Visibility.Collapsed;
                btnReports.Visibility = Visibility.Collapsed;
                return;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.ClearFocus();
            if (AdminPopup.IsOpen) AdminPopup.IsOpen = false;
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
                    txtNotificationMessage.Text = $"Không có dữ liệu nào khớp với '{keyword}'.";
                    SearchNotificationPopup.IsOpen = true;
                    notificationTimer.Stop();
                    notificationTimer.Start();
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

        private void MenuSales_Click(object sender, MouseButtonEventArgs e)
        {
            if (_currentUser.IsInventoryKeeper)
                return;

            SetActiveMenu(btnSales, iconSales, txtSales);
            MainContent.Children.Clear();
            MainContent.Children.Add(new SalesView(_currentUser));
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