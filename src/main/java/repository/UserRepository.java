package repository;

import utils.DatabaseHelper;

import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.sql.SQLException;

public class UserRepository {

    public boolean login(String username, String password) {
        String sql = "SELECT * FROM Users WHERE Username = ? AND Password = ? AND IsActive = 1";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, username);
            ps.setString(2, password);

            try (ResultSet rs = ps.executeQuery()) {
                return rs.next();
            }
        } catch (SQLException e) {
            System.out.println("Lỗi đăng nhập: " + e.getMessage());
            return false;
        }
    }

    public String getFullName(String username) {
        String sql = "SELECT FullName FROM Users WHERE Username = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, username);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    return rs.getString("FullName");
                }
            }
        } catch (SQLException e) {
            System.out.println("Lỗi lấy tên người dùng: " + e.getMessage());
        }
        return null;
    }

    public int getUserId(String username) {
        String sql = "SELECT UserID FROM Users WHERE Username = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, username);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    return rs.getInt("UserID");
                }
            }
        } catch (SQLException e) {
            System.out.println("Lỗi lấy UserID: " + e.getMessage());
        }
        return -1;
    }

    public int getBranchIdByUsername(String username) {
        String sql = "SELECT BranchID FROM Users WHERE Username = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, username);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    return rs.getInt("BranchID");
                }
            }
        } catch (SQLException e) {
            System.out.println("Lỗi lấy BranchID: " + e.getMessage());
        }

        return -1;
    }
}