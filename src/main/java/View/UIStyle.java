package View;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import java.awt.*;

public class UIStyle {
    public static final Color BG = new Color(248, 244, 240);
    public static final Color PANEL = new Color(255, 255, 255);
    public static final Color PRIMARY = new Color(111, 78, 55);
    public static final Color PRIMARY_DARK = new Color(78, 52, 46);
    public static final Color ACCENT = new Color(212, 167, 106);
    public static final Color TEXT = new Color(45, 45, 45);

    public static Font titleFont() {
        return new Font("Segoe UI", Font.BOLD, 24);
    }

    public static Font headingFont() {
        return new Font("Segoe UI", Font.BOLD, 18);
    }

    public static Font normalFont() {
        return new Font("Segoe UI", Font.PLAIN, 15);
    }

    public static Font buttonFont() {
        return new Font("Segoe UI", Font.BOLD, 14);
    }

    public static JButton createButton(String text) {
        JButton btn = new JButton(text);
        btn.setFont(buttonFont());
        btn.setBackground(PRIMARY);
        btn.setForeground(Color.WHITE);
        btn.setFocusPainted(false);
        btn.setBorder(new EmptyBorder(10, 18, 10, 18));
        return btn;
    }

    public static JPanel createCardLayout() {
        JPanel panel = new JPanel();
        panel.setBackground(PANEL);
        panel.setBorder(new EmptyBorder(20, 20, 20, 20));
        return panel;
    }

    public static JLabel createTitle(String text) {
        JLabel lbl = new JLabel(text, SwingConstants.CENTER);
        lbl.setFont(titleFont());
        lbl.setForeground(PRIMARY_DARK);
        return lbl;
    }

    public static JLabel createLabel(String text) {
        JLabel lbl = new JLabel(text);
        lbl.setFont(normalFont());
        lbl.setForeground(TEXT);
        return lbl;
    }

    public static JTextField createTextField() {
        JTextField txt = new JTextField();
        txt.setFont(normalFont());
        txt.setPreferredSize(new Dimension(220, 36));
        return txt;
    }

    public static JPasswordField createPasswordField() {
        JPasswordField txt = new JPasswordField();
        txt.setFont(normalFont());
        txt.setPreferredSize(new Dimension(220, 36));
        return txt;
    }
}