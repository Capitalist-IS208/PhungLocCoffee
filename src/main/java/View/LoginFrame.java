package View;

import repository.UserRepository;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import java.awt.*;

public class LoginFrame extends JFrame {

    private JTextField txtUsername;
    private JPasswordField txtPassword;
    private JButton btnLogin;

    public LoginFrame() {
        setTitle("Phụng Lộc Coffee - Đăng nhập");
        setSize(700, 500);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(EXIT_ON_CLOSE);
        setResizable(false);

        JPanel background = new JPanel(new GridBagLayout());
        background.setBackground(new Color(244, 241, 238));

        JPanel card = new JPanel();
        card.setPreferredSize(new Dimension(500, 360));
        card.setBackground(Color.WHITE);
        card.setLayout(new BoxLayout(card, BoxLayout.Y_AXIS));
        card.setBorder(new EmptyBorder(30, 30, 30, 30));

        JLabel lblTitle = new JLabel("PHỤNG LỘC COFFEE");
        lblTitle.setFont(new Font("Segoe UI", Font.BOLD, 26));
        lblTitle.setForeground(new Color(90, 55, 35));
        lblTitle.setAlignmentX(Component.CENTER_ALIGNMENT);

        JLabel lblSubtitle = new JLabel("Đăng nhập hệ thống bán hàng");
        lblSubtitle.setFont(new Font("Segoe UI", Font.PLAIN, 16));
        lblSubtitle.setAlignmentX(Component.CENTER_ALIGNMENT);

        JLabel lblUsername = new JLabel("Tên đăng nhập");
        lblUsername.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        lblUsername.setAlignmentX(Component.CENTER_ALIGNMENT);

        txtUsername = new JTextField();
        txtUsername.setMaximumSize(new Dimension(300, 35));
        txtUsername.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        txtUsername.setAlignmentX(Component.CENTER_ALIGNMENT);

        JLabel lblPassword = new JLabel("Mật khẩu");
        lblPassword.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        lblPassword.setAlignmentX(Component.CENTER_ALIGNMENT);

        txtPassword = new JPasswordField();
        txtPassword.setMaximumSize(new Dimension(300, 35));
        txtPassword.setFont(new Font("Segoe UI", Font.PLAIN, 14));
        txtPassword.setAlignmentX(Component.CENTER_ALIGNMENT);

        btnLogin = new JButton("Đăng nhập");
        btnLogin.setFont(new Font("Segoe UI", Font.BOLD, 16));
        btnLogin.setForeground(Color.WHITE);
        btnLogin.setBackground(new Color(133, 92, 66));
        btnLogin.setFocusPainted(false);
        btnLogin.setBorderPainted(false);
        btnLogin.setMaximumSize(new Dimension(200, 45));
        btnLogin.setAlignmentX(Component.CENTER_ALIGNMENT);

        btnLogin.addActionListener(e -> login());

        card.add(lblTitle);
        card.add(Box.createVerticalStrut(10));
        card.add(lblSubtitle);
        card.add(Box.createVerticalStrut(25));
        card.add(lblUsername);
        card.add(Box.createVerticalStrut(5));
        card.add(txtUsername);
        card.add(Box.createVerticalStrut(15));
        card.add(lblPassword);
        card.add(Box.createVerticalStrut(5));
        card.add(txtPassword);
        card.add(Box.createVerticalStrut(25));
        card.add(btnLogin);

        background.add(card);
        setContentPane(background);

        setVisible(true);
    }

    private void login() {
        String username = txtUsername.getText().trim();
        String password = new String(txtPassword.getPassword());

        if (username.isEmpty() || password.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Vui lòng nhập đầy đủ thông tin");
            return;
        }

        UserRepository repo = new UserRepository();
        boolean ok = repo.login(username, password);

        if (ok) {
            int userId = repo.getUserId(username);
            String fullName = repo.getFullName(username);
            int branchId = repo.getBranchIdByUsername(username);

            JOptionPane.showMessageDialog(this, "Đăng nhập thành công");

            new MainFrame(userId, username, fullName, branchId);
            dispose();
        } else {
            JOptionPane.showMessageDialog(this, "Sai tài khoản hoặc mật khẩu");
        }
    }
}