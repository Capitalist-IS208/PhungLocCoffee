package View;

import repository.RevenueRepository;
import utils.DatabaseHelper;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import javax.swing.table.DefaultTableModel;
import java.awt.*;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.NumberFormat;
import java.util.Locale;

public class RevenueFrame extends JFrame {

    private JLabel lblTotalOrders;
    private JLabel lblTotalRevenue;

    private JTextField txtDate;
    private JButton btnFilter;
    private JButton btnReload;

    private JTable table;
    private DefaultTableModel model;

    private final RevenueRepository revenueRepo = new RevenueRepository();

    public RevenueFrame() {
        setTitle("Phụng Lộc Coffee - Báo cáo doanh thu");
        setSize(920, 560);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);

        JPanel root = new JPanel(new BorderLayout(12, 12));
        root.setBackground(UIStyle.BG);
        root.setBorder(new EmptyBorder(15, 15, 15, 15));

        JPanel topCard = UIStyle.createCardLayout();
        topCard.setLayout(new BorderLayout(10, 10));

        JLabel lblTitle = UIStyle.createTitle("BÁO CÁO DOANH THU");
        topCard.add(lblTitle, BorderLayout.NORTH);

        JPanel summaryPanel = new JPanel(new GridLayout(1, 2, 10, 10));
        summaryPanel.setBackground(UIStyle.PANEL);

        lblTotalOrders = new JLabel();
        lblTotalOrders.setFont(UIStyle.headingFont());
        lblTotalRevenue = new JLabel();
        lblTotalRevenue.setFont(UIStyle.headingFont());

        summaryPanel.add(lblTotalOrders);
        summaryPanel.add(lblTotalRevenue);

        JPanel filterPanel = new JPanel(new FlowLayout());
        filterPanel.setBackground(UIStyle.PANEL);

        filterPanel.add(UIStyle.createLabel("Ngày (yyyy-MM-dd):"));
        txtDate = UIStyle.createTextField();
        txtDate.setPreferredSize(new Dimension(150, 34));

        btnFilter = UIStyle.createButton("Lọc");
        btnReload = UIStyle.createButton("Tải lại");

        filterPanel.add(txtDate);
        filterPanel.add(btnFilter);
        filterPanel.add(btnReload);

        topCard.add(summaryPanel, BorderLayout.CENTER);
        topCard.add(filterPanel, BorderLayout.SOUTH);

        model = new DefaultTableModel(
                new String[]{"Ngày", "Số hóa đơn", "Doanh thu"}, 0
        );
        table = new JTable(model);
        table.setRowHeight(28);
        table.setFont(UIStyle.normalFont());
        table.getTableHeader().setFont(UIStyle.buttonFont());
        table.getTableHeader().setBackground(UIStyle.PRIMARY);
        table.getTableHeader().setForeground(Color.WHITE);

        root.add(topCard, BorderLayout.NORTH);
        root.add(new JScrollPane(table), BorderLayout.CENTER);

        setContentPane(root);

        loadSummary();
        loadRevenueByDay();

        btnFilter.addActionListener(e -> filterByDate());
        btnReload.addActionListener(e -> reloadAll());

        setVisible(true);
    }

    private void loadSummary() {
        int totalOrders = revenueRepo.getTotalOrders();
        double totalRevenue = revenueRepo.getTotalRevenue();

        lblTotalOrders.setText("Tổng số hóa đơn: " + totalOrders);
        lblTotalRevenue.setText("Tổng doanh thu: " + formatMoney(totalRevenue));
    }

    private void loadRevenueByDay() {
        model.setRowCount(0);

        String sql = "SELECT CAST(CreatedAt AS DATE) AS OrderDate, COUNT(*) AS TotalOrders, SUM(TotalAmount) AS Revenue " +
                "FROM Orders GROUP BY CAST(CreatedAt AS DATE) ORDER BY OrderDate DESC";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            while (rs.next()) {
                model.addRow(new Object[]{
                        rs.getDate("OrderDate"),
                        rs.getInt("TotalOrders"),
                        formatMoney(rs.getDouble("Revenue"))
                });
            }

        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Lỗi load doanh thu theo ngày: " + e.getMessage());
        }
    }

    private void filterByDate() {
        String date = txtDate.getText().trim();

        if (date.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Vui lòng nhập ngày theo định dạng yyyy-MM-dd");
            return;
        }

        model.setRowCount(0);

        String sql = "SELECT CAST(CreatedAt AS DATE) AS OrderDate, COUNT(*) AS TotalOrders, SUM(TotalAmount) AS Revenue " +
                "FROM Orders WHERE CAST(CreatedAt AS DATE) = ? GROUP BY CAST(CreatedAt AS DATE)";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setString(1, date);

            try (ResultSet rs = ps.executeQuery()) {
                if (rs.next()) {
                    model.addRow(new Object[]{
                            rs.getDate("OrderDate"),
                            rs.getInt("TotalOrders"),
                            formatMoney(rs.getDouble("Revenue"))
                    });
                } else {
                    JOptionPane.showMessageDialog(this, "Không có dữ liệu cho ngày này");
                }
            }

        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Lỗi lọc theo ngày: " + e.getMessage());
        }
    }

    private void reloadAll() {
        txtDate.setText("");
        loadSummary();
        loadRevenueByDay();
    }

    private String formatMoney(double amount) {
        NumberFormat formatter = NumberFormat.getInstance(new Locale("vi", "VN"));
        return formatter.format(amount) + " VND";
    }
}