using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Data.SqlClient;
using System.Configuration;

using PhungLocCoffee_POS.Models;
using PhungLocCoffee_POS.Views;
using PhungLocCoffee_POS.Helpers;

namespace PhungLocCoffee_POS.Views
{
    public partial class LoginWindow : Window, INotifyPropertyChanged
    {
        private string _username = string.Empty;
        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
                HasError = false;
            }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        public bool IsNotLoading => !IsLoading;

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set { _hasError = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public LoginWindow()
        {
            InitializeComponent();
            DataContext = this;
            HasError = false;
        }

        private void txtPassword_GotFocus(object sender, RoutedEventArgs e)
        {
            txtPasswordWatermark.Visibility = Visibility.Collapsed;
        }

        private void txtPassword_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtPassword.Password))
                txtPasswordWatermark.Visibility = Visibility.Visible;
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPassword.Password))
                txtPasswordWatermark.Visibility = Visibility.Collapsed;
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                ErrorMessage = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
                HasError = true;
                return;
            }

            IsLoading = true;
            HasError = false;

            try
            {
                var userInfo = await CheckLoginLogicAsync(Username, txtPassword.Password);

                if (userInfo != null)
                {
                    MainWindow mainApp = new MainWindow(userInfo);
                    mainApp.Show();
                    Close();
                }
                else
                {
                    ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác.";
                    HasError = true;
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = "Không thể kết nối cơ sở dữ liệu: " + ex.Message;
                HasError = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task<UserSession?> CheckLoginLogicAsync(string username, string password)
        {
            string connectionString =
                ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

            using SqlConnection conn = new SqlConnection(connectionString);
            await conn.OpenAsync();

            string sql = @"
            SELECT 
                u.UserID,
                u.BranchID,
                u.Username,
                u.FullName,
                u.RoleID,
                r.RoleName,
                b.BranchName
            FROM Users u
            INNER JOIN Roles r 
                ON u.RoleID = r.RoleID
            INNER JOIN Branches b 
                ON u.BranchID = b.BranchID
            WHERE 
                u.Username = @Username
                AND u.Password = @Password
                AND u.IsActive = 1";

            using SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Password", password);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new UserSession
                {
                    UserID = Convert.ToInt32(reader["UserID"]),
                    BranchID = Convert.ToInt32(reader["BranchID"]),
                    RoleID = Convert.ToInt32(reader["RoleID"]),

                    Username = reader["Username"].ToString()?.Trim() ?? "",
                    FullName = reader["FullName"].ToString()?.Trim() ?? "",
                    RoleName = reader["RoleName"].ToString()?.Trim() ?? "",
                    BranchName = reader["BranchName"].ToString()?.Trim() ?? ""
                };
            }

            return null;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

