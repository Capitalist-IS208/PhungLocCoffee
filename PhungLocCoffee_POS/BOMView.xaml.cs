using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace PhungLocCoffee_POS
{
    public partial class BOMView : UserControl, INotifyPropertyChanged
    {
        private string _connectionString;
        private ProductModel _selectedProduct;

        public ObservableCollection<ProductModel> ProductsList { get; set; } = new ObservableCollection<ProductModel>();
        public ObservableCollection<IngredientModel> IngredientsList { get; set; } = new ObservableCollection<IngredientModel>();
        public ObservableCollection<RecipeDetailModel> RecipeDetails { get; set; } = new ObservableCollection<RecipeDetailModel>();

        public bool IsProductSelected => _selectedProduct != null;

        public BOMView()
        {
            InitializeComponent();
            _connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
            DataContext = this;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProducts();
            LoadIngredients();
        }

        private void LoadProducts(string keyword = "")
        {
            ProductsList.Clear();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    string query = @"
                SELECT ProductID, ProductName
                FROM Products
                WHERE IsActive = 1
                AND (@Keyword = '' OR ProductName LIKE N'%' + @Keyword + N'%')
                ORDER BY ProductName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@Keyword", keyword);

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                ProductsList.Add(new ProductModel
                                {
                                    ProductID = reader.GetInt32(0),
                                    ProductName = reader.GetString(1)
                                });
                            }
                        }
                    }
                }

                dgProducts.ItemsSource = ProductsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách món: " + ex.Message);
            }
        }
        private void txtSearchProduct_TextChanged(object sender, TextChangedEventArgs e)
        {
            LoadProducts(txtSearchProduct.Text.Trim());
        }
        private void LoadIngredients()
        {
            IngredientsList.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // Đã sửa: JOIN bảng Ingredients với bảng Units để lấy UnitName (Kg, Lít, Hộp,...)
                    string query = @"
                        SELECT i.IngredientID, i.IngredientName, u.UnitName 
                        FROM Ingredients i
                        LEFT JOIN Units u ON i.UnitID = u.UnitID
                        ORDER BY i.IngredientName";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            IngredientsList.Add(new IngredientModel
                            {
                                IngredientID = reader.GetInt32(0),
                                IngredientName = reader.GetString(1),
                                // Tránh lỗi null nếu có nguyên liệu chưa gán đơn vị
                                Unit = reader.IsDBNull(2) ? "" : reader.GetString(2)
                            });
                        }
                    }
                }
                cbIngredients.ItemsSource = IngredientsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải danh sách nguyên liệu: " + ex.Message);
            }
        }

        private void dgProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgProducts.SelectedItem is ProductModel selected)
            {
                _selectedProduct = selected;
                txtRecipeHeader.Text = $"CÔNG THỨC PHA CHẾ: {selected.ProductName.ToUpper()}";
                OnPropertyChanged(nameof(IsProductSelected));
                LoadRecipeDetails(selected.ProductID);
            }
        }

        private void LoadRecipeDetails(int productId)
        {
            RecipeDetails.Clear();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // Đã sửa: JOIN cả 3 bảng Recipes, Ingredients, và Units
                    string query = @"
                        SELECT r.RecipeID, r.IngredientID, i.IngredientName, r.Quantity, u.UnitName 
                        FROM Recipes r
                        JOIN Ingredients i ON r.IngredientID = i.IngredientID
                        LEFT JOIN Units u ON i.UnitID = u.UnitID
                        WHERE r.ProductID = @ProductID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", productId);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                RecipeDetails.Add(new RecipeDetailModel
                                {
                                    RecipeID = reader.GetInt32(0),
                                    IngredientID = reader.GetInt32(1),
                                    IngredientName = reader.GetString(2),
                                    Quantity = reader.GetDecimal(3),
                                    Unit = reader.IsDBNull(4) ? "" : reader.GetString(4)
                                });
                            }
                        }
                    }
                }
                dgRecipeDetails.ItemsSource = RecipeDetails;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải chi tiết công thức: " + ex.Message);
            }
        }

        private void btnAddIngredient_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedProduct == null) return;
            if (cbIngredients.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn nguyên liệu!");
                return;
            }
            if (!decimal.TryParse(txtQuantity.Text, out decimal quantity) || quantity <= 0)
            {
                MessageBox.Show("Vui lòng nhập định lượng hợp lệ (> 0)!");
                return;
            }

            int ingredientId = (int)cbIngredients.SelectedValue;

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    // UPSERT logic: Cập nhật nếu trùng nguyên liệu, Thêm mới nếu chưa có
                    string query = @"
                        IF EXISTS (SELECT 1 FROM Recipes WHERE ProductID = @ProductID AND IngredientID = @IngredientID)
                            UPDATE Recipes SET Quantity = @Quantity WHERE ProductID = @ProductID AND IngredientID = @IngredientID
                        ELSE
                            INSERT INTO Recipes (ProductID, IngredientID, Quantity) VALUES (@ProductID, @IngredientID, @Quantity)";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ProductID", _selectedProduct.ProductID);
                        cmd.Parameters.AddWithValue("@IngredientID", ingredientId);
                        cmd.Parameters.AddWithValue("@Quantity", quantity);
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageBox.Show("Cập nhật công thức thành công!");
                txtQuantity.Clear();
                LoadRecipeDetails(_selectedProduct.ProductID); // Tải lại bảng chi tiết
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi lưu công thức: " + ex.Message);
            }
        }

        private void btnDeleteIngredient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is RecipeDetailModel detail)
            {
                var result = MessageBox.Show($"Bạn có chắc muốn xóa '{detail.IngredientName}' khỏi công thức này?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (SqlConnection conn = new SqlConnection(_connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM Recipes WHERE RecipeID = @RecipeID";
                            using (SqlCommand cmd = new SqlCommand(query, conn))
                            {
                                cmd.Parameters.AddWithValue("@RecipeID", detail.RecipeID);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        LoadRecipeDetails(_selectedProduct.ProductID);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Lỗi khi xóa: " + ex.Message);
                    }
                }
            }
        }

        // --- INotifyPropertyChanged Implementation ---
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    // --- Data Models ---
    public class ProductModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
    }

    public class IngredientModel
    {
        public int IngredientID { get; set; }
        public string IngredientName { get; set; }
        public string Unit { get; set; }
    }

    public class RecipeDetailModel
    {
        public int RecipeID { get; set; }
        public int IngredientID { get; set; }
        public string IngredientName { get; set; }
        public decimal Quantity { get; set; }
        public string Unit { get; set; }
    }
}