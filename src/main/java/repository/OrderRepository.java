package repository;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import utils.DatabaseHelper;

public class OrderRepository {

    public boolean syncOfflineOrder(int branchId, int userId, double totalAmount, String paymentMethod, String offlineId) {
        String sql = "INSERT INTO Orders (BranchID, UserID, TotalAmount, PaymentMethod, OfflineID, IsSynced) " +
                "VALUES (?, ?, ?, ?, ?, 1)";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setInt(1, branchId);
            ps.setInt(2, userId);
            ps.setDouble(3, totalAmount);
            ps.setString(4, paymentMethod);
            ps.setString(5, offlineId);

            int rowsAffected = ps.executeUpdate();
            return rowsAffected > 0;

        } catch (SQLException e) {
            System.out.println("Lỗi đồng bộ hóa đơn: " + e.getMessage());
            return false;
        }
    }

    public void printSyncedOrders() {
        String sql = "SELECT OrderID, TotalAmount, PaymentMethod, OfflineID FROM Orders WHERE IsSynced = 1";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            System.out.println("--- DANH SÁCH HÓA ĐƠN ĐÃ ĐỒNG BỘ TRÊN SERVER ---");
            while (rs.next()) {
                System.out.println("Mã DB: " + rs.getInt("OrderID") +
                        " | Mã Offline: " + rs.getString("OfflineID") +
                        " | Tổng tiền: " + rs.getDouble("TotalAmount") + " VND" +
                        " | Thanh toán: " + rs.getString("PaymentMethod"));
            }
        } catch (SQLException e) {
            System.out.println("Lỗi lấy danh sách hóa đơn: " + e.getMessage());
        }
    }

    public int addOrderAndReturnId(int branchId, int userId, double totalAmount, String paymentMethod, String offlineId) {
        String sql = "INSERT INTO Orders (BranchID, UserID, TotalAmount, PaymentMethod, OfflineID, IsSynced) VALUES (?, ?, ?, ?, ?, 1)";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql, PreparedStatement.RETURN_GENERATED_KEYS)) {

            ps.setInt(1, branchId);
            ps.setInt(2, userId);
            ps.setDouble(3, totalAmount);
            ps.setString(4, paymentMethod);
            ps.setString(5, offlineId);

            int rowsAffected = ps.executeUpdate();

            if (rowsAffected > 0) {
                try (ResultSet rs = ps.getGeneratedKeys()) {
                    if (rs.next()) {
                        return rs.getInt(1);
                    }
                }
            }

        } catch (SQLException e) {
            System.out.println("Lỗi thêm hóa đơn: " + e.getMessage());
        }

        return -1;
    }

    public boolean addOrderDetail(int orderId, int productId, int quantity, double unitPrice) {
        String sql = "INSERT INTO OrderDetails (OrderID, ProductID, Quantity, UnitPrice) VALUES (?, ?, ?, ?)";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setInt(1, orderId);
            ps.setInt(2, productId);
            ps.setInt(3, quantity);
            ps.setDouble(4, unitPrice);

            int rowsAffected = ps.executeUpdate();
            return rowsAffected > 0;

        } catch (SQLException e) {
            System.out.println("Lỗi thêm chi tiết hóa đơn: " + e.getMessage());
            return false;
        }
    }

    public boolean checkStock(int branchId, int productId, int productQuantity) {
        String recipeSql = "SELECT IngredientID, Quantity FROM Recipes WHERE ProductID = ?";
        String inventorySql = "SELECT CurrentQuantity FROM Inventory WHERE BranchID = ? AND IngredientID = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement recipePs = conn.prepareStatement(recipeSql);
             PreparedStatement inventoryPs = conn.prepareStatement(inventorySql)) {

            recipePs.setInt(1, productId);

            try (ResultSet rs = recipePs.executeQuery()) {
                while (rs.next()) {
                    int ingredientId = rs.getInt("IngredientID");
                    double recipeQty = rs.getDouble("Quantity");

                    double need = recipeQty * productQuantity;

                    inventoryPs.setInt(1, branchId);
                    inventoryPs.setInt(2, ingredientId);

                    try (ResultSet invRs = inventoryPs.executeQuery()) {
                        if (invRs.next()) {
                            double current = invRs.getDouble("CurrentQuantity");

                            if (current < need) {
                                return false;
                            }
                        } else {
                            return false;
                        }
                    }
                }
            }

            return true;

        } catch (SQLException e) {
            System.out.println("Lỗi check tồn kho: " + e.getMessage());
            return false;
        }
    }

    public boolean checkStockForCart(int branchId, java.util.List<View.CartItem> cartItems) {
        for (View.CartItem item : cartItems) {
            boolean ok = checkStock(branchId, item.getProductId(), item.getQuantity());
            if (!ok) {
                return false;
            }
        }
        return true;
    }

    public boolean deductIngredients(int branchId, int productId, int productQuantity) {
        String recipeSql = "SELECT IngredientID, Quantity FROM Recipes WHERE ProductID = ?";
        String updateSql = "UPDATE Inventory SET CurrentQuantity = CurrentQuantity - ? WHERE BranchID = ? AND IngredientID = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement recipePs = conn.prepareStatement(recipeSql);
             PreparedStatement updatePs = conn.prepareStatement(updateSql)) {

            recipePs.setInt(1, productId);

            try (ResultSet rs = recipePs.executeQuery()) {
                while (rs.next()) {
                    int ingredientId = rs.getInt("IngredientID");
                    double recipeQty = rs.getDouble("Quantity");

                    double amount = recipeQty * productQuantity;

                    updatePs.setDouble(1, amount);
                    updatePs.setInt(2, branchId);
                    updatePs.setInt(3, ingredientId);

                    updatePs.executeUpdate();
                }
            }

            return true;

        } catch (SQLException e) {
            System.out.println("Lỗi trừ nguyên liệu: " + e.getMessage());
            return false;
        }
    }
}