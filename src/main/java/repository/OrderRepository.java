package repository;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;
import utils.DatabaseHelper;

public class OrderRepository {

    // 1. Hàm đồng bộ: Nhận dữ liệu hóa đơn Offline từ máy POS và đẩy lên Server
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

    // 2. Hàm lấy dữ liệu: Phục vụ cho màn hình Dashboard của Ban Giám Đốc
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
}