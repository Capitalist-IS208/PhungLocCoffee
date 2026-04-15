package repository;

import utils.DatabaseHelper;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.util.ArrayList;
import java.util.List;

public class ProductRepository {

    public List<String> getProductNames() {
        List<String> list = new ArrayList<>();
        String sql = "SELECT ProductName FROM Products WHERE IsActive = 1";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            while (rs.next()) {
                list.add(rs.getString("ProductName"));
            }

        } catch (Exception e) {
            System.out.println("Lỗi lấy tên sản phẩm: " + e.getMessage());
        }

        return list;
    }

    public double getPriceByName(String productName) {
        String sql = "SELECT Price FROM Products WHERE ProductName = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, productName);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    return rs.getDouble("Price");
                }
            }

        } catch (Exception e) {
            System.out.println("Lỗi lấy giá sản phẩm: " + e.getMessage());
        }

        return 0;
    }

    public int getProductIdByName(String productName) {
        String sql = "SELECT ProductID FROM Products WHERE ProductName = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, productName);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    return rs.getInt("ProductID");
                }
            }

        } catch (Exception e) {
            System.out.println("Lỗi lấy ProductID: " + e.getMessage());
        }

        return -1;
    }
}