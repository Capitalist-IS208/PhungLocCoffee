package repository;

import utils.DatabaseHelper;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;

public class RevenueRepository {

    public double getTotalRevenue() {
        String sql = "SELECT ISNULL(SUM(TotalAmount), 0) AS TotalRevenue FROM Orders";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            if (rs.next()) {
                return rs.getDouble("TotalRevenue");
            }

        } catch (Exception e) {
            System.out.println("Lỗi lấy tổng doanh thu: " + e.getMessage());
        }

        return 0;
    }

    public int getTotalOrders() {
        String sql = "SELECT COUNT(*) AS TotalOrders FROM Orders";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            if (rs.next()) {
                return rs.getInt("TotalOrders");
            }

        } catch (Exception e) {
            System.out.println("Lỗi lấy tổng số hóa đơn: " + e.getMessage());
        }

        return 0;
    }
}