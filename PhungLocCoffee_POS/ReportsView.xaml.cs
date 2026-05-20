using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Data.SqlClient;

namespace PhungLocCoffee_POS
{
    public partial class ReportsView : UserControl
    {
        private readonly UserSession _currentUser;
        private bool _hasShownOfflineMessage = false;

        public SeriesCollection RevenueSeries { get; set; } = new SeriesCollection();
        public string[] BranchLabels { get; set; } = Array.Empty<string>();
        public Func<double, string> MoneyFormatter { get; set; } = value => value.ToString("N0");

        public ObservableCollection<RevenueItem> RevenueList { get; set; } = new ObservableCollection<RevenueItem>();
        public ObservableCollection<WasteItem> WasteList { get; set; } = new ObservableCollection<WasteItem>();
        public ObservableCollection<BranchItem> BranchFilterList { get; set; } = new ObservableCollection<BranchItem>();

        public ReportsView(UserSession currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            DataContext = this;

            LoadInitialData();
        }

        private void ShowOfflineMessageOnce()
        {
            if (_hasShownOfflineMessage)
                return;

            _hasShownOfflineMessage = true;

            MessageBox.Show(
                "Đang offline. Không thể tải dữ liệu báo cáo.",
                "Offline",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private void LoadInitialData()
        {
            LoadBranchesFilterFromDatabase();
            LoadRevenueReportFromDatabase();
            LoadWasteReportFromDatabase();
        }

        private void LoadRevenueReportFromDatabase()
        {
            try
            {
                RevenueList.Clear();

                string connStr = ConfigurationManager
                    .ConnectionStrings["DefaultConnection"]
                    .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                    SELECT 
                        b.BranchName,
                        COUNT(o.OrderID) AS OrderCount,
                        ISNULL(SUM(o.TotalAmount), 0) AS TotalRevenue
                    FROM Branches b
                    LEFT JOIN Orders o
                        ON b.BranchID = o.BranchID
                        AND o.CreatedAt >= @StartDate
                        AND o.CreatedAt < @EndDate
                    WHERE
                    (
                        @BranchID = 0
                        OR b.BranchID = @BranchID
                    )
                    AND
                    (
                        @IsAdmin = 1
                        OR b.BranchID = @UserBranchID
                    )
                    GROUP BY b.BranchName
                    ORDER BY b.BranchName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        int selectedBranchId = 0;

                        if (cboBranchFilter.SelectedValue != null)
                        {
                            selectedBranchId = Convert.ToInt32(cboBranchFilter.SelectedValue);
                        }
                        else if (!_currentUser.IsAdmin)
                        {
                            selectedBranchId = _currentUser.BranchID;
                        }

                        cmd.Parameters.AddWithValue("@BranchID", selectedBranchId);
                        cmd.Parameters.AddWithValue("@IsAdmin", _currentUser.IsAdmin ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UserBranchID", _currentUser.BranchID);

                        DateTime startDate;
                        DateTime endDate;

                        int timeIndex = cboTimeFilter.SelectedIndex;
                        DateTime today = DateTime.Today;

                        if (timeIndex == 0)
                        {
                            startDate = today;
                            endDate = today.AddDays(1);
                        }
                        else if (timeIndex == 1)
                        {
                            startDate = today.AddDays(-6);
                            endDate = today.AddDays(1);
                        }
                        else if (timeIndex == 3)
                        {
                            DateTime lastMonth = today.AddMonths(-1);
                            startDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                            endDate = startDate.AddMonths(1);
                        }
                        else if (timeIndex == 4)
                        {
                            int currentQuarter = ((today.Month - 1) / 3) + 1;
                            int startMonth = (currentQuarter - 1) * 3 + 1;

                            startDate = new DateTime(today.Year, startMonth, 1);
                            endDate = startDate.AddMonths(3);
                        }
                        else if (timeIndex == 5)
                        {
                            startDate = new DateTime(today.Year, 1, 1);
                            endDate = new DateTime(today.Year + 1, 1, 1);
                        }
                        else
                        {
                            startDate = new DateTime(today.Year, today.Month, 1);
                            endDate = startDate.AddMonths(1);
                        }

                        cmd.Parameters.AddWithValue("@StartDate", startDate);
                        cmd.Parameters.AddWithValue("@EndDate", endDate);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RevenueList.Add(new RevenueItem
                                {
                                    BranchName = reader["BranchName"].ToString() ?? "",
                                    OrderCount = Convert.ToInt32(reader["OrderCount"]),
                                    TotalRevenue = Convert.ToDouble(reader["TotalRevenue"])
                                });
                            }
                        }
                    }
                }

                dgRevenue.ItemsSource = RevenueList;

                RevenueSeries.Clear();

                ChartValues<double> values = new ChartValues<double>();
                string[] labels = new string[RevenueList.Count];

                for (int i = 0; i < RevenueList.Count; i++)
                {
                    values.Add(RevenueList[i].TotalRevenue / 1000000);
                    labels[i] = RevenueList[i].BranchName;
                }

                RevenueSeries.Add(new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = values,
                    Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3182CE")),
                    MaxColumnWidth = 40,
                    ColumnPadding = 10
                });

                BranchLabels = labels;
                MoneyFormatter = value => value.ToString("0.0") + " Tr";
            }
            catch
            {
                ShowOfflineMessageOnce();
            }
        }

        private void LoadWasteReportFromDatabase()
        {
            try
            {
                WasteList.Clear();

                string connStr = ConfigurationManager
                    .ConnectionStrings["DefaultConnection"]
                    .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                    SELECT TOP 10
                        i.IngredientName,
                        iad.SystemQuantity AS SystemQty,
                        iad.ActualQuantity AS ActualQty,
                        b.BranchName
                    FROM InventoryAuditDetail iad
                    INNER JOIN InventoryAudit ia
                        ON iad.AuditID = ia.AuditID
                    INNER JOIN Ingredients i
                        ON iad.IngredientID = i.IngredientID
                    INNER JOIN Branches b
                        ON ia.BranchID = b.BranchID
                    WHERE (@IsAdmin = 1 OR ia.BranchID = @BranchID)
                    ORDER BY ia.AuditDate DESC, iad.DetailID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        cmd.Parameters.AddWithValue("@IsAdmin", _currentUser.IsAdmin ? 1 : 0);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string itemName = reader["IngredientName"].ToString() ?? "";

                                if (_currentUser.IsAdmin)
                                {
                                    string branchName = reader["BranchName"].ToString() ?? "";
                                    itemName = $"{itemName} - {branchName}";
                                }

                                WasteList.Add(new WasteItem
                                {
                                    ItemName = itemName,
                                    SystemQty = Convert.ToDouble(reader["SystemQty"]),
                                    ActualQty = Convert.ToDouble(reader["ActualQty"])
                                });
                            }
                        }
                    }
                }

                dgWaste.ItemsSource = WasteList;
            }
            catch
            {
                ShowOfflineMessageOnce();
            }
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OverlayLoading.Visibility = Visibility.Visible;

                await Task.Delay(300);

                LoadRevenueReportFromDatabase();
                LoadWasteReportFromDatabase();

                ChartRevenue.Update(true, true);
            }
            catch
            {
                ShowOfflineMessageOnce();
            }
            finally
            {
                OverlayLoading.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadBranchesFilterFromDatabase()
        {
            try
            {
                BranchFilterList.Clear();

                if (_currentUser.IsAdmin)
                {
                    BranchFilterList.Add(new BranchItem
                    {
                        BranchID = 0,
                        BranchName = "Tất cả chi nhánh"
                    });
                }

                string connStr = ConfigurationManager
                    .ConnectionStrings["DefaultConnection"]
                    .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                    SELECT BranchID, BranchName
                    FROM Branches
                    WHERE (@IsAdmin = 1 OR BranchID = @BranchID)
                    ORDER BY BranchName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@IsAdmin", _currentUser.IsAdmin ? 1 : 0);
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BranchFilterList.Add(new BranchItem
                                {
                                    BranchID = Convert.ToInt32(reader["BranchID"]),
                                    BranchName = reader["BranchName"].ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                cboBranchFilter.ItemsSource = BranchFilterList;
                cboBranchFilter.DisplayMemberPath = "BranchName";
                cboBranchFilter.SelectedValuePath = "BranchID";
                cboBranchFilter.SelectedIndex = 0;
            }
            catch
            {
                ShowOfflineMessageOnce();
            }
        }

        private void CboBranchFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            LoadRevenueReportFromDatabase();
            LoadWasteReportFromDatabase();

            ChartRevenue.Update(true, true);
        }

        private void CboTimeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            LoadRevenueReportFromDatabase();
            ChartRevenue.Update(true, true);
        }
    }

    public class RevenueItem
    {
        public string BranchName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public double TotalRevenue { get; set; }
    }

    public class WasteItem
    {
        public string ItemName { get; set; } = string.Empty;
        public double SystemQty { get; set; }
        public double ActualQty { get; set; }
        public double Difference => Math.Round(SystemQty - ActualQty, 2);
        public double WasteRate => SystemQty == 0 ? 0 : Math.Round((Difference / SystemQty) * 100, 1);
        public string WasteRateText => $"{WasteRate}%";
        public bool IsBad => WasteRate > 2.0;
        public string BgColor => IsBad ? "#FFF5F5" : "#F0FFF4";
        public string TextColor => IsBad ? "#E53E3E" : "#38A169";
    }
}