using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Data.SqlClient;
using System.Configuration;
using Microsoft.Data.Sqlite;

namespace PhungLocCoffee_POS
{
    public partial class SalesView : UserControl, INotifyPropertyChanged
    {
        private readonly UserSession _currentUser;

        private ObservableCollection<CategoryItem> _categories = new ObservableCollection<CategoryItem>();
        public ObservableCollection<CategoryItem> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ProductItem> _products = new ObservableCollection<ProductItem>();
        public ObservableCollection<ProductItem> Products
        {
            get => _products;
            set { _products = value; OnPropertyChanged(); }
        }

        private ObservableCollection<CartItem> _cart = new ObservableCollection<CartItem>();
        public ObservableCollection<CartItem> Cart
        {
            get => _cart;
            set { _cart = value; OnPropertyChanged(); }
        }

        private double _totalAmount;
        public double TotalAmount
        {
            get => _totalAmount;
            set { _totalAmount = value; OnPropertyChanged(); }
        }

        private bool _isCashSelected = true;
        public bool IsCashSelected
        {
            get => _isCashSelected;
            set { _isCashSelected = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsTransferSelected)); }
        }
        public bool IsTransferSelected => !IsCashSelected;

        private int _pendingOrderCount;
        public int PendingOrderCount
        {
            get => _pendingOrderCount;
            set { _pendingOrderCount = value; OnPropertyChanged(); }
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

                CommandManager.InvalidateRequerySuggested();
            }
        }
        public bool IsNotLoading => !IsLoading;

        public ICommand SelectCategoryCommand { get; private set; }
        public ICommand AddToCartCommand { get; private set; }
        public ICommand IncreaseQuantityCommand { get; private set; }
        public ICommand DecreaseQuantityCommand { get; private set; }
        public ICommand CheckoutCommand { get; private set; }

        public SalesView(UserSession currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;

            this.DataContext = this;

            SelectCategoryCommand = new RelayCommand<CategoryItem>(ExecuteSelectCategory);
            AddToCartCommand = new RelayCommand<ProductItem>(ExecuteAddToCart);
            IncreaseQuantityCommand = new RelayCommand<CartItem>(ExecuteIncreaseQuantity);
            DecreaseQuantityCommand = new RelayCommand<CartItem>(ExecuteDecreaseQuantity);
            CheckoutCommand = new RelayCommand(ExecuteCheckout, CanExecuteCheckout);

            LoadSampleData();
            TrySyncOfflineOrders();
            UpdatePendingOfflineCount();
            CheckServerConnection();
        }

        private void ExecuteSelectCategory(CategoryItem? category)
        {
            if (category == null) return;
            foreach (var c in Categories) c.IsSelected = (c == category);
            CategorySelected?.Invoke(this, category.Id);
        }

        private void ExecuteAddToCart(ProductItem? product)
        {
            if (product == null || product.IsOutOfStock) return;
            var existing = Cart.FirstOrDefault(x => x.ProductName == product.Name);
            if (existing != null)
                existing.Quantity++;
            else
                Cart.Add(new CartItem { ProductName = product.Name, Price = product.Price, Quantity = 1 });
            RecalculateTotal();
        }

        private void ExecuteIncreaseQuantity(CartItem? item)
        {
            if (item != null)
            {
                item.Quantity++;
                RecalculateTotal();
            }
        }

        private void ExecuteDecreaseQuantity(CartItem? item)
        {
            if (item == null) return;
            if (item.Quantity > 1)
            {
                item.Quantity--;
                RecalculateTotal();
            }
            else
            {
                var result = MessageBox.Show($"Bạn có chắc muốn xóa '{item.ProductName}' khỏi đơn hàng?",
                                             "Xóa món", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    Cart.Remove(item);
                    RecalculateTotal();
                }
            }
        }

        private bool CanExecuteCheckout() => Cart.Count > 0 && !IsLoading;

        private async void ExecuteCheckout()
        {
            if (!CanExecuteCheckout()) return;

            IsLoading = true;

            try
            {
                var checkoutInfo = new CheckoutInfo
                {
                    CartItems = Cart.ToList(),
                    PaymentMethod = IsCashSelected ? "Cash" : "Transfer",
                    TotalAmount = TotalAmount
                };

                var result = await OnCheckoutRequested(checkoutInfo);

                if (result.Success)
                {
                    if (!IsCashSelected)
                        ShowQrCode(result.QrCodeImageData, TotalAmount);
                    else
                        MessageBox.Show("Thanh toán thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                    Cart.Clear();
                    RecalculateTotal();
                }
                else
                {
                    MessageBox.Show(result.ErrorMessage, "Lỗi thanh toán", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void SelectCash_Click(object sender, MouseButtonEventArgs e)
        {
            IsCashSelected = true;
        }

        private void SelectTransfer_Click(object sender, MouseButtonEventArgs e)
        {
            IsCashSelected = false;
        }

        private void RecalculateTotal()
        {
            TotalAmount = Cart.Sum(x => x.SubTotal);
        }

        private void ShowQrCode(byte[] imageData, double amount)
        {
            if (imageData != null && imageData.Length > 0)
            {
                var bitmap = new BitmapImage();
                using (var stream = new System.IO.MemoryStream(imageData))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }
                QrCodeImage.Source = bitmap;
            }
            QrAmount.Text = amount.ToString("N0") + " đ";
            QrPopup.IsOpen = true;
        }

        private void CloseQrPopup_Click(object sender, RoutedEventArgs e)
        {
            QrPopup.IsOpen = false;
            MessageBox.Show("Cảm ơn quý khách!", "Thanh toán thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public event EventHandler<int>? CategorySelected;
        public event Func<CheckoutInfo, System.Threading.Tasks.Task<CheckoutResult>>? CheckoutRequested;

        protected virtual async System.Threading.Tasks.Task<CheckoutResult> OnCheckoutRequested(CheckoutInfo info)
        {
            try
            {
                string connStr = ConfigurationManager
                    .ConnectionStrings["DefaultConnection"]
                    .ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    foreach (var item in Cart)
                    {
                        int productId = Products
                            .FirstOrDefault(p => p.Name == item.ProductName)?.Id ?? 0;

                        string checkStockQuery = @"
                        SELECT 
                            ing.IngredientName,
                            i.CurrentQuantity,
                            r.Quantity * @SoldQuantity AS RequiredQuantity
                        FROM Recipes r
                        INNER JOIN Inventory i
                            ON r.IngredientID = i.IngredientID
                        INNER JOIN Ingredients ing
                            ON r.IngredientID = ing.IngredientID
                        WHERE r.ProductID = @ProductID
                        AND i.BranchID = @BranchID
                        AND i.CurrentQuantity < (r.Quantity * @SoldQuantity)";

                        using (SqlCommand checkCmd = new SqlCommand(checkStockQuery, conn))
                        {
                            checkCmd.Parameters.AddWithValue("@ProductID", productId);
                            checkCmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                            checkCmd.Parameters.AddWithValue("@SoldQuantity", item.Quantity);

                            using (SqlDataReader reader = await checkCmd.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string ingredientName = reader["IngredientName"].ToString() ?? "";
                                    double currentQty = Convert.ToDouble(reader["CurrentQuantity"]);
                                    double requiredQty = Convert.ToDouble(reader["RequiredQuantity"]);

                                    return new CheckoutResult
                                    {
                                        Success = false,
                                        ErrorMessage = $"Không đủ nguyên liệu: {ingredientName}\nHiện có: {currentQty:N2}\nCần: {requiredQty:N2}"
                                    };
                                }
                            }
                        }
                    }

                    // ========== ĐÃ SỬA: XÓA CustomerId VÀ NULL ==========
                    string orderQuery = @"
                    INSERT INTO Orders
                    (
                        BranchID,
                        UserID,
                        TotalAmount,
                        DiscountAmount,
                        PaymentMethod,
                        CreatedAt,
                        OfflineID,
                        IsSynced
                    )
                    VALUES
                    (
                        @BranchID,
                        @UserID,
                        @TotalAmount,
                        0,
                        @PaymentMethod,
                        GETDATE(),
                        @OfflineID,
                        1
                    );

                    SELECT SCOPE_IDENTITY();
                    ";

                    int orderId;

                    using (SqlCommand cmd = new SqlCommand(orderQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        cmd.Parameters.AddWithValue("@UserID", _currentUser.UserID);
                        cmd.Parameters.AddWithValue("@TotalAmount", info.TotalAmount);
                        cmd.Parameters.AddWithValue("@PaymentMethod", info.PaymentMethod);
                        cmd.Parameters.AddWithValue("@OfflineID", Guid.NewGuid().ToString());

                        orderId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    foreach (var item in Cart)
                    {
                        int productId = Products
                            .FirstOrDefault(p => p.Name == item.ProductName)?.Id ?? 0;

                        string detailQuery = @"
                INSERT INTO OrderDetails
                (
                    OrderID,
                    ProductID,
                    Quantity,
                    UnitPrice
                )
                VALUES
                (
                    @OrderID,
                    @ProductID,
                    @Quantity,
                    @UnitPrice
                )";

                        using (SqlCommand cmd = new SqlCommand(detailQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@OrderID", orderId);
                            cmd.Parameters.AddWithValue("@ProductID", productId);
                            cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                            cmd.Parameters.AddWithValue("@UnitPrice", item.Price);

                            await cmd.ExecuteNonQueryAsync();
                        }

                        string updateInventoryQuery = @"
                        UPDATE i
                        SET i.CurrentQuantity = i.CurrentQuantity - (r.Quantity * @SoldQuantity)
                        FROM Inventory i
                        INNER JOIN Recipes r 
                            ON i.IngredientID = r.IngredientID
                        WHERE i.BranchID = @BranchID
                        AND r.ProductID = @ProductID";

                        using (SqlCommand updateCmd = new SqlCommand(updateInventoryQuery, conn))
                        {
                            updateCmd.Parameters.AddWithValue("@SoldQuantity", item.Quantity);
                            updateCmd.Parameters.AddWithValue("@ProductID", productId);
                            updateCmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);

                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }

                    string transactionQuery = @"
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
                        @TotalAmount,
                        GETDATE(),
                        @TransactionType
                    )";

                    using (SqlCommand transactionCmd = new SqlCommand(transactionQuery, conn))
                    {
                        transactionCmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                        transactionCmd.Parameters.AddWithValue("@UserID", _currentUser.UserID);
                        transactionCmd.Parameters.AddWithValue("@Note", "Bán hàng POS");
                        transactionCmd.Parameters.AddWithValue("@TotalAmount", info.TotalAmount);
                        transactionCmd.Parameters.AddWithValue("@TransactionType", "SALE");

                        await transactionCmd.ExecuteNonQueryAsync();
                    }
                }

                OfflineBanner.Visibility = Visibility.Collapsed;
                TrySyncOfflineOrders();
                UpdatePendingOfflineCount();

                return new CheckoutResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                OfflineBanner.Visibility = Visibility.Visible;

                try
                {
                    SaveOrderOffline(info);
                    UpdatePendingOfflineCount();

                    return new CheckoutResult
                    {
                        Success = true,
                        ErrorMessage = "Mất kết nối server.\nĐơn đã lưu OFFLINE và sẽ đồng bộ sau."
                    };
                }
                catch
                {
                    return new CheckoutResult
                    {
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }
        private void SaveOrderOffline(CheckoutInfo info)
        {
            string localOrderId = Guid.NewGuid().ToString();

            using (var conn = new Microsoft.Data.Sqlite.SqliteConnection(
                LocalDatabaseHelper.GetConnectionString()))
            {
                conn.Open();

                string insertOrder = @"
                INSERT INTO LocalOrders
                (
                    LocalOrderID,
                    BranchID,
                    UserID,
                    TotalAmount,
                    PaymentMethod,
                    CreatedAt,
                    IsSynced
                )
                VALUES
                (
                    @LocalOrderID,
                    @BranchID,
                    @UserID,
                    @TotalAmount,
                    @PaymentMethod,
                    @CreatedAt,
                    0
                )";

                using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(insertOrder, conn))
                {
                    cmd.Parameters.AddWithValue("@LocalOrderID", localOrderId);
                    cmd.Parameters.AddWithValue("@BranchID", _currentUser.BranchID);
                    cmd.Parameters.AddWithValue("@UserID", _currentUser.UserID);
                    cmd.Parameters.AddWithValue("@TotalAmount", info.TotalAmount);
                    cmd.Parameters.AddWithValue("@PaymentMethod", info.PaymentMethod);
                    cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                    cmd.ExecuteNonQuery();
                }

                foreach (var item in Cart)
                {
                    int productId = Products
                        .FirstOrDefault(p => p.Name == item.ProductName)?.Id ?? 0;

                    string insertDetail = @"
                    INSERT INTO LocalOrderDetails
                    (
                        LocalOrderID,
                        ProductID,
                        ProductName,
                        Quantity,
                        UnitPrice
                    )
                    VALUES
                    (
                        @LocalOrderID,
                        @ProductID,
                        @ProductName,
                        @Quantity,
                        @UnitPrice
                    )";

                    using (var cmd = new Microsoft.Data.Sqlite.SqliteCommand(insertDetail, conn))
                    {
                        cmd.Parameters.AddWithValue("@LocalOrderID", localOrderId);
                        cmd.Parameters.AddWithValue("@ProductID", productId);
                        cmd.Parameters.AddWithValue("@ProductName", item.ProductName);
                        cmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        cmd.Parameters.AddWithValue("@UnitPrice", item.Price);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void LoadSampleData()
        {
            if (!CheckServerConnection())
            {
                LoadSampleDataFromLocal();
                UpdatePendingOfflineCount();
                return;
            }

            Categories.Clear();
            Products.Clear();
            Cart.Clear();

            string connStr = ConfigurationManager
                .ConnectionStrings["DefaultConnection"]
                .ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string categoryQuery = "SELECT * FROM Categories";

                    using (SqlCommand cmd = new SqlCommand(categoryQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Categories.Add(new CategoryItem
                            {
                                Id = Convert.ToInt32(reader["CategoryID"]),
                                Name = reader["CategoryName"].ToString() ?? "",
                                IsSelected = Categories.Count == 0
                            });
                        }
                    }


                    string productQuery = "SELECT * FROM Products WHERE IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(productQuery, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Products.Add(new ProductItem
                            {
                                Id = Convert.ToInt32(reader["ProductID"]),
                                Name = reader["ProductName"].ToString() ?? "",
                                Price = Convert.ToDouble(reader["Price"]),
                                IsOutOfStock = false
                            });
                        }
                    }
                    CacheProductsToLocal();
                }
            }
            catch
            {
                LoadSampleDataFromLocal();
                OfflineBanner.Visibility = Visibility.Visible;
            }

            RecalculateTotal();

            PendingOrderCount = 0;
        }

        private void LoadSampleDataFromLocal()
        {
            Categories.Clear();
            Products.Clear();

            using (var conn = new SqliteConnection(LocalDatabaseHelper.GetConnectionString()))
            {
                conn.Open();

                string categorySql = "SELECT CategoryID, CategoryName FROM LocalCategories";

                using (var cmd = new SqliteCommand(categorySql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Categories.Add(new CategoryItem
                        {
                            Id = Convert.ToInt32(reader["CategoryID"]),
                            Name = reader["CategoryName"].ToString() ?? "",
                            IsSelected = Categories.Count == 0
                        });
                    }
                }

                string productSql = "SELECT ProductID, ProductName, Price FROM LocalProducts WHERE IsActive = 1";

                using (var cmd = new SqliteCommand(productSql, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Products.Add(new ProductItem
                        {
                            Id = Convert.ToInt32(reader["ProductID"]),
                            Name = reader["ProductName"].ToString() ?? "",
                            Price = Convert.ToDouble(reader["Price"]),
                            IsOutOfStock = false
                        });
                    }
                }
            }

            RecalculateTotal();
        }

        private void CacheProductsToLocal()
        {
            using (var conn = new SqliteConnection(LocalDatabaseHelper.GetConnectionString()))
            {
                conn.Open();

                string deleteCategories = "DELETE FROM LocalCategories";
                using (var cmd = new SqliteCommand(deleteCategories, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string deleteProducts = "DELETE FROM LocalProducts";
                using (var cmd = new SqliteCommand(deleteProducts, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                foreach (var category in Categories)
                {
                    string insertCategory = @"
                    INSERT INTO LocalCategories
                    (
                        CategoryID,
                        CategoryName
                    )
                    VALUES
                    (
                        @CategoryID,
                        @CategoryName
                    )";

                    using (var cmd = new SqliteCommand(insertCategory, conn))
                    {
                        cmd.Parameters.AddWithValue("@CategoryID", category.Id);
                        cmd.Parameters.AddWithValue("@CategoryName", category.Name);
                        cmd.ExecuteNonQuery();
                    }
                }

                foreach (var product in Products)
                {
                    string insertProduct = @"
                    INSERT INTO LocalProducts
                    (
                        ProductID,
                        ProductName,
                        CategoryID,
                        Price,
                        IsActive
                    )
                    VALUES
                    (
                        @ProductID,
                        @ProductName,
                        0,
                        @Price,
                        1
                    )";

                    using (var cmd = new SqliteCommand(insertProduct, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", product.Id);
                        cmd.Parameters.AddWithValue("@ProductName", product.Name);
                        cmd.Parameters.AddWithValue("@Price", product.Price);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool CheckServerConnection()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(
                    ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                {
                    conn.Open();

                    OfflineBanner.Visibility = Visibility.Collapsed;

                    return true;
                }
            }
            catch
            {
                OfflineBanner.Visibility = Visibility.Visible;

                return false;
            }
        }

        private void UpdatePendingOfflineCount()
        {
            using (var conn = new SqliteConnection(LocalDatabaseHelper.GetConnectionString()))
            {
                conn.Open();

                string sql = "SELECT COUNT(*) FROM LocalOrders WHERE IsSynced = 0";

                using (var cmd = new SqliteCommand(sql, conn))
                {
                    PendingOrderCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            OnPropertyChanged(nameof(PendingOrderCount));
        }

        private void TrySyncOfflineOrders()
        {
            if (!CheckServerConnection())
                return;

            using (var sqliteConn = new SqliteConnection(LocalDatabaseHelper.GetConnectionString()))
            {
                sqliteConn.Open();

                string getOrdersSql = "SELECT * FROM LocalOrders WHERE IsSynced = 0";

                using (var cmd = new SqliteCommand(getOrdersSql, sqliteConn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string localOrderId = reader["LocalOrderID"].ToString();

                        try
                        {
                            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString))
                            {
                                sqlConn.Open();

                                string insertSql = @"
                            INSERT INTO Orders (BranchID, UserID, TotalAmount, DiscountAmount, PaymentMethod, CreatedAt, OfflineID, IsSynced)
                            VALUES (@BranchID, @UserID, @TotalAmount, @DiscountAmount, @PaymentMethod, @CreatedAt, @OfflineID, 1);
                            SELECT SCOPE_IDENTITY();";

                                int newOrderId;

                                using (SqlCommand insertCmd = new SqlCommand(insertSql, sqlConn))
                                {
                                    // Kiểm tra DBNull an toàn cho từng trường
                                    insertCmd.Parameters.AddWithValue("@BranchID", reader["BranchID"] != DBNull.Value ? Convert.ToInt32(reader["BranchID"]) : 0);
                                    insertCmd.Parameters.AddWithValue("@UserID", reader["UserID"] != DBNull.Value ? Convert.ToInt32(reader["UserID"]) : 0);
                                    insertCmd.Parameters.AddWithValue("@TotalAmount", reader["TotalAmount"] != DBNull.Value ? Convert.ToDouble(reader["TotalAmount"]) : 0.0);
                                    insertCmd.Parameters.AddWithValue("@DiscountAmount", reader["DiscountAmount"] != DBNull.Value ? Convert.ToDouble(reader["DiscountAmount"]) : 0.0);
                                    insertCmd.Parameters.AddWithValue("@PaymentMethod", reader["PaymentMethod"]?.ToString() ?? "");
                                    insertCmd.Parameters.AddWithValue("@CreatedAt", reader["CreatedAt"]?.ToString() ?? DateTime.Now.ToString());
                                    insertCmd.Parameters.AddWithValue("@OfflineID", localOrderId);

                                    newOrderId = Convert.ToInt32(insertCmd.ExecuteScalar());
                                }

                                // Sync Chi tiết đơn hàng
                                string getDetailsSql = "SELECT ProductID, Quantity, UnitPrice FROM LocalOrderDetails WHERE LocalOrderID = @LocalOrderID";
                                using (SqliteCommand detailCmd = new SqliteCommand(getDetailsSql, sqliteConn))
                                {
                                    detailCmd.Parameters.AddWithValue("@LocalOrderID", localOrderId);
                                    using (SqliteDataReader detailReader = detailCmd.ExecuteReader())
                                    {
                                        while (detailReader.Read())
                                        {
                                            string insertDetailSql = @"
                                        INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice)
                                        VALUES (@OrderID, @ProductID, @Quantity, @UnitPrice)";

                                            using (SqlCommand insertDetailCmd = new SqlCommand(insertDetailSql, sqlConn))
                                            {
                                                insertDetailCmd.Parameters.AddWithValue("@OrderID", newOrderId);
                                                insertDetailCmd.Parameters.AddWithValue("@ProductID", detailReader["ProductID"] != DBNull.Value ? Convert.ToInt32(detailReader["ProductID"]) : 0);
                                                insertDetailCmd.Parameters.AddWithValue("@Quantity", detailReader["Quantity"] != DBNull.Value ? Convert.ToInt32(detailReader["Quantity"]) : 0);
                                                insertDetailCmd.Parameters.AddWithValue("@UnitPrice", detailReader["UnitPrice"] != DBNull.Value ? Convert.ToDouble(detailReader["UnitPrice"]) : 0.0);

                                                insertDetailCmd.ExecuteNonQuery();

                                                // Cập nhật kho
                                                string updateInventorySql = @"
                                            UPDATE i SET i.CurrentQuantity = i.CurrentQuantity - (r.Quantity * @SoldQuantity)
                                            FROM Inventory i
                                            INNER JOIN Recipes r ON i.IngredientID = r.IngredientID
                                            WHERE i.BranchID = @BranchID AND r.ProductID = @ProductID";

                                                using (SqlCommand updateInventoryCmd = new SqlCommand(updateInventorySql, sqlConn))
                                                {
                                                    updateInventoryCmd.Parameters.AddWithValue("@SoldQuantity", Convert.ToInt32(detailReader["Quantity"]));
                                                    updateInventoryCmd.Parameters.AddWithValue("@BranchID", Convert.ToInt32(reader["BranchID"]));
                                                    updateInventoryCmd.Parameters.AddWithValue("@ProductID", Convert.ToInt32(detailReader["ProductID"]));
                                                    updateInventoryCmd.ExecuteNonQuery();
                                                }
                                            }
                                        }
                                    }
                                }

                                // Đánh dấu đã đồng bộ
                                string updateSql = "UPDATE LocalOrders SET IsSynced = 1 WHERE LocalOrderID = @LocalOrderID";
                                using (var updateCmd = new SqliteCommand(updateSql, sqliteConn))
                                {
                                    updateCmd.Parameters.AddWithValue("@LocalOrderID", localOrderId);
                                    updateCmd.ExecuteNonQuery();
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log lỗi nếu cần
                            System.Diagnostics.Debug.WriteLine($"Lỗi sync đơn {localOrderId}: {ex.Message}");
                        }
                    }
                }
            }
        }
    }

    public class CategoryItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class ProductItem : INotifyPropertyChanged
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double Price { get; set; }
        private bool _isOutOfStock;
        public bool IsOutOfStock
        {
            get => _isOutOfStock;
            set { _isOutOfStock = value; OnPropertyChanged(); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CartItem : INotifyPropertyChanged
    {
        public string ProductName { get; set; } = string.Empty;
        public double Price { get; set; }

        private int _quantity;
        public int Quantity
        {
            get => _quantity;
            set { _quantity = value; OnPropertyChanged(); OnPropertyChanged(nameof(SubTotal)); }
        }
        public double SubTotal => Price * Quantity;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class CheckoutInfo
    {
        public System.Collections.Generic.List<CartItem> CartItems { get; set; } = new System.Collections.Generic.List<CartItem>();
        public string PaymentMethod { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
    }

    public class CheckoutResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public byte[] QrCodeImageData { get; set; } = Array.Empty<byte>();
    }

    public class CategoryBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7A16A")) : Brushes.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class CategoryForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class PaymentMethodBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EDF2F7"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class PaymentMethodForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#718096"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProductBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8FAFC")) : Brushes.White;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProductForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A0AEC0")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D3748"));
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class ProductCursorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Cursors.No : Cursors.Hand;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class StrikethroughConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool b && b) ? TextDecorations.Strikethrough : new TextDecorationCollection();
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value is bool && (bool)value) ? Visibility.Collapsed : Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => throw new NotImplementedException();
    }
}