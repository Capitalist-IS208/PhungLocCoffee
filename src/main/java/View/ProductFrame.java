package View;

import utils.DatabaseHelper;

import javax.swing.*;
import javax.swing.table.DefaultTableModel;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;

public class ProductFrame extends JFrame {
    private final JTable table;
    private final DefaultTableModel model;

    public ProductFrame() {
        setTitle("Danh sách sản phẩm");
        setSize(500, 300);
        setLocationRelativeTo(null);

        model = new DefaultTableModel(new String[]{"ID", "Tên sản phẩm", "Giá"}, 0);
        table = new JTable(model);

        add(new JScrollPane(table));
        loadProducts();

        setVisible(true);
    }

    private void loadProducts() {
        String sql = "SELECT ProductID, ProductName, Price FROM Products WHERE IsActive = 1";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            model.setRowCount(0);

            while (rs.next()) {
                model.addRow(new Object[]{
                        rs.getInt("ProductID"),
                        rs.getString("ProductName"),
                        rs.getDouble("Price")
                });
            }
        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Lỗi load sản phẩm: " + e.getMessage());
        }
    }
}