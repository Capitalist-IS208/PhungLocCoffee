package View;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import java.awt.*;

public class MainFrame extends JFrame {

    private final int userId;
    private final String username;
    private final String fullName;
    private final int branchId;

    public MainFrame(int userId, String username, String fullName, int branchId) {
        this.userId = userId;
        this.username = username;
        this.fullName = fullName;
        this.branchId = branchId;

        setTitle("Phụng Lộc Coffee - Trang chính");
        setSize(700, 420);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);

        JPanel root = new JPanel(new BorderLayout(15, 15));
        root.setBackground(UIStyle.BG);
        root.setBorder(new EmptyBorder(20, 20, 20, 20));

        JPanel header = UIStyle.createCardLayout();
        header.setLayout(new BorderLayout());
        header.setPreferredSize(new Dimension(660, 90));

        JLabel lblWelcome = new JLabel(
                "<html><div style='text-align:center;'>"
                        + "<b>Xin chào, " + fullName + "</b><br/>"
                        + "Tài khoản: " + username + " | Chi nhánh: " + branchId
                        + "</div></html>",
                SwingConstants.CENTER
        );
        lblWelcome.setFont(UIStyle.headingFont());
        lblWelcome.setForeground(UIStyle.PRIMARY_DARK);
        header.add(lblWelcome, BorderLayout.CENTER);

        JPanel menuPanel = new JPanel(new GridLayout(1, 3, 15, 15));
        menuPanel.setBackground(UIStyle.BG);

        JButton btnOrders = createMenuButton("Tạo hóa đơn", "Bán hàng và thanh toán");
        JButton btnRevenue = createMenuButton("Báo cáo doanh thu", "Xem thống kê doanh thu");
        JButton btnExit = createMenuButton("Thoát", "Đóng ứng dụng");

        btnOrders.addActionListener(e -> new OrderFrame(userId, branchId));
        btnRevenue.addActionListener(e -> new RevenueFrame());
        btnExit.addActionListener(e -> System.exit(0));

        menuPanel.add(btnOrders);
        menuPanel.add(btnRevenue);
        menuPanel.add(btnExit);

        root.add(header, BorderLayout.NORTH);
        root.add(menuPanel, BorderLayout.CENTER);

        setContentPane(root);
        setVisible(true);
    }

    private JButton createMenuButton(String title, String subtitle) {
        JButton btn = new JButton(
                "<html><div style='text-align:center;'>"
                        + "<div style='font-size:16px;font-weight:bold;'>" + title + "</div>"
                        + "<div style='font-size:11px;'>" + subtitle + "</div>"
                        + "</div></html>"
        );
        btn.setFont(UIStyle.buttonFont());
        btn.setBackground(UIStyle.PANEL);
        btn.setForeground(UIStyle.PRIMARY_DARK);
        btn.setFocusPainted(false);
        btn.setBorder(BorderFactory.createLineBorder(UIStyle.ACCENT, 2));
        return btn;
    }
}