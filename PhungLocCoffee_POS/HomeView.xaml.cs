using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;

namespace PhungLocCoffee_POS
{
    public partial class HomeView : UserControl, INotifyPropertyChanged
    {
        private readonly UserSession _currentUser;

        private double _todayRevenue;
        public double TodayRevenue
        {
            get => _todayRevenue;
            set { _todayRevenue = value; OnPropertyChanged(); }
        }

        private int _todayCustomers;
        public int TodayCustomers
        {
            get => _todayCustomers;
            set { _todayCustomers = value; OnPropertyChanged(); }
        }

        private int _pendingSyncOrders;
        public int PendingSyncOrders
        {
            get => _pendingSyncOrders;
            set { _pendingSyncOrders = value; OnPropertyChanged(); }
        }

        private bool _isOfflineMode;
        public bool IsOfflineMode
        {
            get => _isOfflineMode;
            set { _isOfflineMode = value; OnPropertyChanged(); }
        }

        private int _newMembersToday;
        public int NewMembersToday
        {
            get => _newMembersToday;
            set { _newMembersToday = value; OnPropertyChanged(); }
        }

        public SeriesCollection SalesSeries { get; set; } = new SeriesCollection();
        public string[] TimeLabels { get; set; } = Array.Empty<string>();
        public Func<double, string> MoneyFormatter { get; set; } = value => value.ToString("N0");

        public ObservableCollection<StockAlert> LowStockAlerts { get; set; }
            = new ObservableCollection<StockAlert>();

        public ICommand ViewAllLowStockCommand { get; set; }
            = new RelayCommand(() => { });

        public HomeView(UserSession currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            LoadInitialFakeData();
            UpdatePendingSyncOrdersFromLocal();

            DataContext = this;
        }

        private void UpdatePendingSyncOrdersFromLocal()
        {
            try
            {
                using (SqliteConnection conn =
                    new SqliteConnection(LocalDatabaseHelper.GetConnectionString()))
                {
                    conn.Open();

                    string sql = @"
                    SELECT COUNT(*)
                    FROM LocalOrders
                    WHERE IsSynced = 0";

                    using (SqliteCommand cmd = new SqliteCommand(sql, conn))
                    {
                        PendingSyncOrders =
                            Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }
            }
            catch
            {
            }
        }

        private void LoadInitialFakeData()
        {
            string connStr = ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString;

            double[] hourlyRevenue = new double[12];

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string revenueQuery = @"
                    SELECT ISNULL(SUM(TotalAmount), 0)
                    FROM Orders
                    WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    AND (@IsAdmin = 1 OR BranchID = @BranchID)";

                    using (SqlCommand cmd =
                        new SqlCommand(revenueQuery, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@BranchID",
                            _currentUser.BranchID);

                        cmd.Parameters.AddWithValue(
                            "@IsAdmin",
                            _currentUser.RoleName == "Admin" ? 1 : 0);

                        TodayRevenue =
                            Convert.ToDouble(cmd.ExecuteScalar());
                    }

                    string customerQuery = @"
                    SELECT COUNT(OrderID)
                    FROM Orders
                    WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    AND (@IsAdmin = 1 OR BranchID = @BranchID)";

                    using (SqlCommand cmd =
                        new SqlCommand(customerQuery, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@BranchID",
                            _currentUser.BranchID);

                        cmd.Parameters.AddWithValue(
                            "@IsAdmin",
                            _currentUser.RoleName == "Admin" ? 1 : 0);

                        TodayCustomers =
                            Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    using (SqliteConnection sqliteConn =
                        new SqliteConnection(
                            LocalDatabaseHelper.GetConnectionString()))
                    {
                        sqliteConn.Open();

                        string pendingSql = @"
                        SELECT COUNT(*)
                        FROM LocalOrders
                        WHERE IsSynced = 0";

                        using (SqliteCommand cmd =
                            new SqliteCommand(pendingSql, sqliteConn))
                        {
                            PendingSyncOrders =
                                Convert.ToInt32(cmd.ExecuteScalar());
                        }
                    }

                    string chartQuery = @"
                    SELECT DATEPART(HOUR, CreatedAt) AS SaleHour,
                           SUM(TotalAmount) AS Revenue
                    FROM Orders
                    WHERE CAST(CreatedAt AS DATE) = CAST(GETDATE() AS DATE)
                    AND (@IsAdmin = 1 OR BranchID = @BranchID)
                    GROUP BY DATEPART(HOUR, CreatedAt)";

                    using (SqlCommand cmd =
                        new SqlCommand(chartQuery, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@BranchID",
                            _currentUser.BranchID);

                        cmd.Parameters.AddWithValue(
                            "@IsAdmin",
                            _currentUser.RoleName == "Admin" ? 1 : 0);

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                int hour =
                                    Convert.ToInt32(reader["SaleHour"]);

                                double revenue =
                                    Convert.ToDouble(reader["Revenue"]);

                                if (hour >= 8 && hour <= 19)
                                {
                                    hourlyRevenue[hour - 8] =
                                        revenue / 1000;
                                }
                            }
                        }
                    }

                    LowStockAlerts =
                        new ObservableCollection<StockAlert>();

                    string lowStockQuery = @"
                    SELECT
                        b.BranchName,
                        i.IngredientName,
                        inv.CurrentQuantity,
                        i.MinStockLevel
                    FROM Inventory inv
                    INNER JOIN Ingredients i
                        ON inv.IngredientID = i.IngredientID
                    INNER JOIN Branches b
                        ON inv.BranchID = b.BranchID
                    WHERE inv.CurrentQuantity <= i.MinStockLevel
                    AND (@IsAdmin = 1 OR inv.BranchID = @BranchID)";

                    using (SqlCommand cmd =
                        new SqlCommand(lowStockQuery, conn))
                    {
                        cmd.Parameters.AddWithValue(
                            "@BranchID",
                            _currentUser.BranchID);

                        cmd.Parameters.AddWithValue(
                            "@IsAdmin",
                            _currentUser.RoleName == "Admin" ? 1 : 0);

                        using (SqlDataReader reader =
                            cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string ingredientName =
                                    reader["IngredientName"].ToString() ?? "";

                                string branchName =
                                    reader["BranchName"].ToString() ?? "";

                                double currentQty =
                                    Convert.ToDouble(reader["CurrentQuantity"]);

                                double minQty =
                                    Convert.ToDouble(reader["MinStockLevel"]);

                                LowStockAlerts.Add(new StockAlert
                                {
                                    ItemName =
                                        $"{ingredientName} - {branchName}",

                                    StatusMessage =
                                        $"Còn lại: {currentQty} (Dưới mức {minQty})",

                                    BackgroundColor = "#FFF5F5",
                                    BorderColor = "#FED7D7",
                                    TitleForeground = "#C53030",
                                    MessageForeground = "#E53E3E"
                                });
                            }
                        }
                    }

                    IsOfflineMode = false;
                }
            }
            catch
            {
                IsOfflineMode = true;
            }

            NewMembersToday = 0;

            SalesSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",

                    Values =
                        new ChartValues<double>(hourlyRevenue),

                    Fill =
                        new SolidColorBrush(
                            (Color)ColorConverter.ConvertFromString("#D7A16A")),

                    MaxColumnWidth = 25,
                    ColumnPadding = 8
                }
            };

            TimeLabels = new[]
            {
                "8h", "9h", "10h", "11h",
                "12h", "13h", "14h", "15h",
                "16h", "17h", "18h", "19h"
            };

            MoneyFormatter =
                value => value.ToString("N0") + "k";

            ViewAllLowStockCommand =
                new RelayCommand(ViewAllAlerts);
        }

        private void ViewAllAlerts()
        {
            Window parentWindow = Window.GetWindow(this);

            if (parentWindow is MainWindow mainWindow)
            {
                mainWindow.NavigateToInventory();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }

    public class StockAlert
    {
        public string ItemName { get; set; } = string.Empty;
        public string StatusMessage { get; set; } = string.Empty;
        public string BackgroundColor { get; set; } = string.Empty;
        public string BorderColor { get; set; } = string.Empty;
        public string TitleForeground { get; set; } = string.Empty;
        public string MessageForeground { get; set; } = string.Empty;
    }
}

