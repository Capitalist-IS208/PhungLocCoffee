using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PhungLocCoffee_POS
{
    public partial class InventoryView : UserControl
    {
        private readonly UserSession _currentUser;

        private bool _hasShownOfflineMessage = false;
        private bool _shownInventoryTabOffline = false;
        private bool _shownCheckTabOffline = false;
        private bool _shownTransactionTabOffline = false;

        public ObservableCollection<InventoryItem> InventoryList { get; set; } = new ObservableCollection<InventoryItem>();
        public ObservableCollection<InventoryCheckItem> CheckMaterialList { get; set; } = new ObservableCollection<InventoryCheckItem>();
        public ObservableCollection<BranchItem> BranchList { get; set; } = new ObservableCollection<BranchItem>();
        public ObservableCollection<InventoryCheckItem> TransactionMaterialList { get; set; } = new ObservableCollection<InventoryCheckItem>();

        public InventoryView(UserSession currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;
            DataContext = this;

            LoadInventoryFromDatabase();
            LoadCheckMaterialsFromDatabase();
            LoadBranchesFromDatabase();
            LoadTransactionMaterialsFromDatabase();
        }

        private bool IsStaff()
        {
            return _currentUser.RoleName == "Staff";
        }

        private string GetConnectionString()
        {
            return ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString;
        }

        private bool CanConnectServer()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private void ShowOfflineMessageOnce(string message)
        {
            if (_hasShownOfflineMessage)
                return;

            _hasShownOfflineMessage = true;

            MessageBox.Show(
                message,
                "Offline",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
        }

        private void LoadInventoryFromDatabase()
        {
            try
            {
                InventoryList.Clear();

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string query = @"
                    SELECT
                        inv.IngredientID,
                        i.IngredientName,
                        u.UnitName,
                        inv.CurrentQuantity,
                        i.MinStockLevel
                    FROM Inventory inv
                    INNER JOIN Ingredients i
                        ON inv.IngredientID = i.IngredientID
                    INNER JOIN Units u
                        ON i.UnitID = u.UnitID
                    WHERE inv.BranchID = @BranchID
                    ORDER BY i.IngredientName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                InventoryList.Add(new InventoryItem
                                {
                                    ID = "NL" + Convert.ToInt32(reader["IngredientID"]).ToString("000"),
                                    IngredientID = Convert.ToInt32(reader["IngredientID"]),
                                    Name = reader["IngredientName"].ToString() ?? "",
                                    Unit = reader["UnitName"].ToString() ?? "",
                                    CurrentQty = Convert.ToDouble(reader["CurrentQuantity"]),
                                    MinStock = Convert.ToDouble(reader["MinStockLevel"])
                                });
                            }
                        }
                    }
                }

                dgInventory.ItemsSource = InventoryList;
            }
            catch
            {
                ShowOfflineMessageOnce("Đang offline. Không thể tải dữ liệu kho.");
            }
        }

        private void LoadCheckMaterialsFromDatabase()
        {
            try
            {
                CheckMaterialList.Clear();

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string query = @"
                    SELECT
                        inv.IngredientID,
                        i.IngredientName,
                        u.UnitName,
                        inv.CurrentQuantity
                    FROM Inventory inv
                    INNER JOIN Ingredients i
                        ON inv.IngredientID = i.IngredientID
                    INNER JOIN Units u
                        ON i.UnitID = u.UnitID
                    WHERE inv.BranchID = @BranchID
                    ORDER BY i.IngredientName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                CheckMaterialList.Add(new InventoryCheckItem
                                {
                                    IngredientID = Convert.ToInt32(reader["IngredientID"]),
                                    IngredientName = reader["IngredientName"].ToString() ?? "",
                                    UnitName = reader["UnitName"].ToString() ?? "",
                                    SystemQty = Convert.ToDouble(reader["CurrentQuantity"])
                                });
                            }
                        }
                    }
                }

                cboMaterial.ItemsSource = CheckMaterialList;
                cboMaterial.DisplayMemberPath = "DisplayName";
                cboMaterial.SelectedValuePath = "IngredientID";
            }
            catch
            {
                ShowOfflineMessageOnce("Đang offline. Không thể tải dữ liệu kiểm kho.");
            }
        }

        private void UpdateCheckDifference()
        {
            if (cboMaterial.SelectedItem is not InventoryCheckItem selectedItem)
                return;

            txtSystemQty.Text = selectedItem.SystemQty.ToString("N2");
            txtUnitLabel.Text = " " + selectedItem.UnitName;

            if (string.IsNullOrWhiteSpace(txtActualQty.Text))
            {
                txtDiscrepancy.Text = "";
                return;
            }

            if (double.TryParse(txtActualQty.Text, out double actualQty))
            {
                double difference = actualQty - selectedItem.SystemQty;
                txtDiscrepancy.Text = difference.ToString("N2");
            }
            else
            {
                txtDiscrepancy.Text = "";
            }
        }

        private void ResetTabs()
        {
            var transparent = Brushes.Transparent;
            var grayText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));
            var borderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E2E8F0"));

            btnTab1.Background = transparent;
            btnTab1.BorderBrush = borderBrush;
            iconTab1.Foreground = grayText;
            txtTab1.Foreground = grayText;

            btnTab2.Background = transparent;
            btnTab2.BorderBrush = borderBrush;
            iconTab2.Foreground = grayText;
            txtTab2.Foreground = grayText;

            btnTab3.Background = transparent;
            btnTab3.BorderBrush = borderBrush;
            iconTab3.Foreground = grayText;
            txtTab3.Foreground = grayText;
        }

        private void SetActiveTab(Border btn, MahApps.Metro.IconPacks.PackIconMaterial icon, TextBlock txt)
        {
            ResetTabs();

            var orange = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7A16A"));

            btn.Background = orange;
            btn.BorderBrush = Brushes.Transparent;
            icon.Foreground = Brushes.White;
            txt.Foreground = Brushes.White;
        }

        private void BtnTab1_Click(object sender, MouseButtonEventArgs e)
        {
            if (!CanConnectServer() && !_shownInventoryTabOffline)
            {
                _shownInventoryTabOffline = true;

                MessageBox.Show(
                    "Đang offline. Không thể tải dữ liệu tồn kho.",
                    "Offline",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            SetActiveTab(btnTab1, iconTab1, txtTab1);
            MainTab.SelectedIndex = 0;

            LoadInventoryFromDatabase();
            LoadCheckMaterialsFromDatabase();
        }

        private void BtnTab2_Click(object sender, MouseButtonEventArgs e)
        {
            if (IsStaff())
            {
                MessageBox.Show(
                    "Bạn không có quyền kiểm kho.",
                    "Phân quyền",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            if (!CanConnectServer() && !_shownCheckTabOffline)
            {
                _shownCheckTabOffline = true;

                MessageBox.Show(
                    "Đang offline. Không thể tải dữ liệu kiểm kho.",
                    "Offline",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            SetActiveTab(btnTab2, iconTab2, txtTab2);
            MainTab.SelectedIndex = 1;

            LoadCheckMaterialsFromDatabase();

            if (cboMaterial.SelectedItem is InventoryCheckItem selectedItem)
            {
                txtActualQty.Text = selectedItem.SystemQty.ToString("N0");
            }

            UpdateCheckDifference();
        }

        private void BtnTab3_Click(object sender, MouseButtonEventArgs e)
        {
            if (IsStaff())
            {
                MessageBox.Show(
                    "Bạn không có quyền nhập/xuất/chuyển kho.",
                    "Phân quyền",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            if (!CanConnectServer() && !_shownTransactionTabOffline)
            {
                _shownTransactionTabOffline = true;

                MessageBox.Show(
                    "Đang offline. Không thể tải dữ liệu nhập/xuất/chuyển.",
                    "Offline",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }

            SetActiveTab(btnTab3, iconTab3, txtTab3);
            MainTab.SelectedIndex = 2;
        }

        private void BtnSaveInventory_Click(object sender, RoutedEventArgs e)
        {
            if (IsStaff())
            {
                MessageBox.Show(
                    "Bạn không có quyền kiểm kho.",
                    "Phân quyền",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                if (cboMaterial.SelectedItem is not InventoryCheckItem selectedItem)
                {
                    MessageBox.Show("Vui lòng chọn nguyên liệu cần kiểm kho.");
                    return;
                }

                if (!double.TryParse(txtActualQty.Text, out double actualQty))
                {
                    MessageBox.Show("Số lượng thực tế không hợp lệ.");
                    return;
                }

                double systemQty = selectedItem.SystemQty;
                int auditId;

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string auditQuery = @"
                    INSERT INTO InventoryAudit
                    (
                        BranchID,
                        UserID,
                        AuditDate,
                        Notes
                    )
                    VALUES
                    (
                        @BranchID,
                        @UserID,
                        GETDATE(),
                        @Notes
                    );

                    SELECT SCOPE_IDENTITY();
                    ";

                    using (SqlCommand cmd = new SqlCommand(auditQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        cmd.Parameters.AddWithValue("@UserID", _currentUser.UserID);
                        cmd.Parameters.AddWithValue("@Notes", "Kiểm kho nhanh từ app POS");
                        auditId = Convert.ToInt32(cmd.ExecuteScalar());
                    }

                    string detailQuery = @"
                    INSERT INTO InventoryAuditDetail
                    (
                        AuditID,
                        IngredientID,
                        SystemQuantity,
                        ActualQuantity
                    )
                    VALUES
                    (
                        @AuditID,
                        @IngredientID,
                        @SystemQuantity,
                        @ActualQuantity
                    )";

                    using (SqlCommand cmd = new SqlCommand(detailQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@AuditID", auditId);
                        cmd.Parameters.AddWithValue("@IngredientID", selectedItem.IngredientID);
                        cmd.Parameters.AddWithValue("@SystemQuantity", systemQty);
                        cmd.Parameters.AddWithValue("@ActualQuantity", actualQty);
                        cmd.ExecuteNonQuery();
                    }

                    string updateInventoryQuery = @"
                    UPDATE Inventory
                    SET CurrentQuantity = @ActualQuantity
                    WHERE BranchID = @BranchID
                    AND IngredientID = @IngredientID";

                    using (SqlCommand cmd = new SqlCommand(updateInventoryQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ActualQuantity", actualQty);
                        cmd.Parameters.AddWithValue("@IngredientID", selectedItem.IngredientID);
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        cmd.ExecuteNonQuery();
                    }
                }

                double difference = actualQty - systemQty;
                txtDiscrepancy.Text = difference.ToString("N2");

                LoadInventoryFromDatabase();

                MessageBox.Show(
                    "Lưu kiểm kho thành công!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kiểm kho: " + ex.Message);
            }
        }

        private void BtnCreateTransaction_Click(object sender, RoutedEventArgs e)
        {
            if (IsStaff())
            {
                MessageBox.Show(
                    "Bạn không có quyền thao tác kho.",
                    "Phân quyền",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );

                return;
            }

            try
            {
                if (cboTransType.SelectedItem is not ComboBoxItem selectedType)
                {
                    MessageBox.Show("Vui lòng chọn loại giao dịch.");
                    return;
                }

                if (cboMaterialTrans.SelectedItem is not InventoryCheckItem selectedMaterial)
                {
                    MessageBox.Show("Vui lòng chọn nguyên liệu.");
                    return;
                }

                if (!double.TryParse(txtTransQty.Text, out double qty) || qty <= 0)
                {
                    MessageBox.Show("Số lượng không hợp lệ.");
                    return;
                }

                string transType = selectedType.Content.ToString() ?? "";
                string note = txtTransNotes.Text;
                int ingredientId = selectedMaterial.IngredientID;

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    if (transType.Contains("Chuyển"))
                    {
                        if (cboBranch.SelectedValue == null)
                        {
                            MessageBox.Show("Vui lòng chọn chi nhánh nhận.");
                            return;
                        }

                        int toBranchId = Convert.ToInt32(cboBranch.SelectedValue);
                        Guid transferId = Guid.NewGuid();

                        string transferQuery = @"
                        INSERT INTO StockTransfer
                        (
                            TransferID,
                            FromBranchID,
                            ToBranchID,
                            CreatedBy,
                            Status,
                            CreatedAt
                        )
                        VALUES
                        (
                            @TransferID,
                            @FromBranchID,
                            @ToBranchID,
                            @CreatedBy,
                            N'Đã chuyển',
                            GETDATE()
                        );";

                        using (SqlCommand cmd = new SqlCommand(transferQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@TransferID", transferId);
                            cmd.Parameters.AddWithValue("@FromBranchID", _currentUser.BranchID);
                            cmd.Parameters.AddWithValue("@ToBranchID", toBranchId);
                            cmd.Parameters.AddWithValue("@CreatedBy", _currentUser.UserID);

                            cmd.ExecuteNonQuery();
                        }

                        string detailQuery = @"
                        INSERT INTO StockTransferDetail
                        (
                            TransferID,
                            IngredientID,
                            Quantity
                        )
                        VALUES
                        (
                            @TransferID,
                            @IngredientID,
                            @Quantity
                        )";

                        using (SqlCommand cmd = new SqlCommand(detailQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@TransferID", transferId);
                            cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                            cmd.Parameters.AddWithValue("@Quantity", qty);
                            cmd.ExecuteNonQuery();
                        }

                        string subtractQuery = @"
                        UPDATE Inventory
                        SET CurrentQuantity = CurrentQuantity - @Qty
                        WHERE BranchID = @BranchID
                        AND IngredientID = @IngredientID";

                        using (SqlCommand cmd = new SqlCommand(subtractQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                            cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                            cmd.ExecuteNonQuery();
                        }

                        string addQuery = @"
                        UPDATE Inventory
                        SET CurrentQuantity = CurrentQuantity + @Qty
                        WHERE BranchID = @BranchID
                        AND IngredientID = @IngredientID";

                        using (SqlCommand cmd = new SqlCommand(addQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@BranchID", toBranchId);
                            cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string updateQuery = transType.Contains("Nhập")
                            ? @"
                            UPDATE Inventory
                            SET CurrentQuantity = CurrentQuantity + @Qty
                            WHERE BranchID = @BranchID
                            AND IngredientID = @IngredientID"
                            : @"
                            UPDATE Inventory
                            SET CurrentQuantity = CurrentQuantity - @Qty
                            WHERE BranchID = @BranchID
                            AND IngredientID = @IngredientID";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@Qty", qty);
                            cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                            cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    string insertLogQuery = @"
                    INSERT INTO InventoryTransactions
                    (
                        TransactionID,
                        BranchID,
                        UserID,
                        Note,
                        TotalAmount,
                        CreatedAt,
                        TransactionType
                    )
                    VALUES
                    (
                        NEWID(),
                        @BranchID,
                        @UserID,
                        @Note,
                        0,
                        GETDATE(),
                        @Type
                    )";

                    using (SqlCommand cmd = new SqlCommand(insertLogQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        cmd.Parameters.AddWithValue("@UserID", _currentUser.UserID);
                        cmd.Parameters.AddWithValue("@Note", note);
                        cmd.Parameters.AddWithValue("@Type", transType);
                        cmd.ExecuteNonQuery();
                    }
                }

                LoadInventoryFromDatabase();
                LoadCheckMaterialsFromDatabase();
                LoadTransactionMaterialsFromDatabase();

                MessageBox.Show(
                    "Tạo giao dịch kho thành công!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                cboBranch.SelectedIndex = -1;
                cboMaterialTrans.SelectedIndex = -1;
                txtTransQty.Text = "";
                txtTransNotes.Text = "";
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CboMaterial_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboMaterial.SelectedItem is not InventoryCheckItem selectedItem)
                return;

            txtSystemQty.Text = selectedItem.SystemQty.ToString("N2");
            txtUnitLabel.Text = " " + selectedItem.UnitName;

            txtActualQty.Text = "";
            txtDiscrepancy.Text = "";
        }

        private void LoadBranchesFromDatabase()
        {
            try
            {
                BranchList.Clear();

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string query = @"
                    SELECT BranchID, BranchName
                    FROM Branches
                    WHERE BranchID <> @CurrentBranchID
                    ORDER BY BranchName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@CurrentBranchID", _currentUser.BranchID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                BranchList.Add(new BranchItem
                                {
                                    BranchID = Convert.ToInt32(reader["BranchID"]),
                                    BranchName = reader["BranchName"].ToString() ?? ""
                                });
                            }
                        }
                    }
                }

                cboBranch.ItemsSource = BranchList;
                cboBranch.DisplayMemberPath = "BranchName";
                cboBranch.SelectedValuePath = "BranchID";
                cboBranch.SelectedIndex = -1;
            }
            catch
            {
            }
        }

        private void LoadTransactionMaterialsFromDatabase()
        {
            try
            {
                TransactionMaterialList.Clear();

                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    conn.Open();

                    string query = @"
                    SELECT
                        inv.IngredientID,
                        i.IngredientName,
                        u.UnitName,
                        inv.CurrentQuantity
                    FROM Inventory inv
                    INNER JOIN Ingredients i
                        ON inv.IngredientID = i.IngredientID
                    INNER JOIN Units u
                        ON i.UnitID = u.UnitID
                    WHERE inv.BranchID = @BranchID
                    ORDER BY i.IngredientName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                TransactionMaterialList.Add(new InventoryCheckItem
                                {
                                    IngredientID = Convert.ToInt32(reader["IngredientID"]),
                                    IngredientName = reader["IngredientName"].ToString() ?? "",
                                    UnitName = reader["UnitName"].ToString() ?? "",
                                    SystemQty = Convert.ToDouble(reader["CurrentQuantity"])
                                });
                            }
                        }
                    }
                }

                cboMaterialTrans.ItemsSource = TransactionMaterialList;
                cboMaterialTrans.DisplayMemberPath = "DisplayName";
                cboMaterialTrans.SelectedValuePath = "IngredientID";
                cboMaterialTrans.SelectedIndex = -1;
            }
            catch
            {
                ShowOfflineMessageOnce("Đang offline. Không thể tải dữ liệu giao dịch kho.");
            }
        }
    }

    public class InventoryItem
    {
        public int IngredientID { get; set; }
        public string ID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public double CurrentQty { get; set; }
        public double MinStock { get; set; }

        public bool IsLowStock => CurrentQty <= MinStock;
        public string StatusText => IsLowStock ? "CẦN NHẬP GẤP" : "ĐANG AN TOÀN";
        public string StatusBgColor => IsLowStock ? "#FFF5F5" : "#F0FFF4";
        public string StatusTextColor => IsLowStock ? "#E53E3E" : "#38A169";
    }

    public class InventoryCheckItem
    {
        public int IngredientID { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public double SystemQty { get; set; }

        public string DisplayName => $"{IngredientName} ({UnitName})";
    }

    public class BranchItem
    {
        public int BranchID { get; set; }
        public string BranchName { get; set; } = string.Empty;
    }
}