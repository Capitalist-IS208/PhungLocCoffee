package View;

import repository.OrderRepository;
import repository.ProductRepository;
import utils.DatabaseHelper;

import javax.swing.*;
import javax.swing.border.EmptyBorder;
import javax.swing.table.DefaultTableModel;
import java.awt.*;
import java.sql.Connection;
import java.sql.PreparedStatement;
import java.sql.ResultSet;
import java.text.NumberFormat;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public class OrderFrame extends JFrame {

    private final int userId;
    private final int branchId;

    private JTextField txtQuantity;
    private JTextField txtTotalAmount;

    private JComboBox<String> cbProducts;
    private JComboBox<String> cbPaymentMethod;

    private JButton btnAddToCart;
    private JButton btnRemoveFromCart;
    private JButton btnCheckout;

    private JTable tableOrders;
    private JTable tableCart;
    private JTable tableOrderDetails;

    private DefaultTableModel ordersModel;
    private DefaultTableModel cartModel;
    private DefaultTableModel detailsModel;

    private final ProductRepository productRepo = new ProductRepository();
    private final OrderRepository orderRepo = new OrderRepository();

    private final List<CartItem> cartItems = new ArrayList<>();

    public OrderFrame(int userId, int branchId) {
        this.userId = userId;
        this.branchId = branchId;

        setTitle("Phụng Lộc Coffee - Bán hàng");
        setSize(1100, 760);
        setLocationRelativeTo(null);
        setDefaultCloseOperation(DISPOSE_ON_CLOSE);

        JPanel root = new JPanel(new BorderLayout(12, 12));
        root.setBackground(UIStyle.BG);
        root.setBorder(new EmptyBorder(15, 15, 15, 15));

        JPanel topCard = UIStyle.createCardLayout();
        topCard.setLayout(new BorderLayout(10, 10));

        JLabel lblTitle = new JLabel("TẠO HÓA ĐƠN", SwingConstants.CENTER);
        lblTitle.setFont(UIStyle.titleFont());
        lblTitle.setForeground(UIStyle.PRIMARY_DARK);
        topCard.add(lblTitle, BorderLayout.NORTH);

        JPanel infoPanel = new JPanel(new GridLayout(3, 4, 12, 12));
        infoPanel.setBackground(UIStyle.PANEL);

        txtQuantity = UIStyle.createTextField();
        txtTotalAmount = UIStyle.createTextField();
        txtTotalAmount.setEditable(false);

        cbProducts = new JComboBox<>();
        cbProducts.setFont(UIStyle.normalFont());

        cbPaymentMethod = new JComboBox<>(new String[]{"Tiền mặt", "Chuyển khoản", "Thẻ"});
        cbPaymentMethod.setFont(UIStyle.normalFont());

        infoPanel.add(UIStyle.createLabel("Chi nhánh"));
        infoPanel.add(new JLabel(String.valueOf(branchId)));

        infoPanel.add(UIStyle.createLabel("Nhân viên"));
        infoPanel.add(new JLabel(String.valueOf(userId)));

        infoPanel.add(UIStyle.createLabel("Sản phẩm"));
        infoPanel.add(cbProducts);

        infoPanel.add(UIStyle.createLabel("Số lượng"));
        infoPanel.add(txtQuantity);
        infoPanel.add(UIStyle.createLabel("Phương thức thanh toán"));
        infoPanel.add(cbPaymentMethod);

        infoPanel.add(UIStyle.createLabel("Tổng tiền"));
        infoPanel.add(txtTotalAmount);

        topCard.add(infoPanel, BorderLayout.CENTER);

btnAddToCart = UIStyle.createButton("Thêm vào giỏ");
btnRemoveFromCart = UIStyle.createButton("Xóa khỏi giỏ");
btnCheckout = UIStyle.createButton("Thanh toán");

JPanel actionPanel = new JPanel(new FlowLayout(FlowLayout.CENTER, 12, 8));
        actionPanel.setBackground(UIStyle.PANEL);
        actionPanel.add(btnAddToCart);
        actionPanel.add(btnRemoveFromCart);
        actionPanel.add(btnCheckout);

        topCard.add(actionPanel, BorderLayout.SOUTH);

cartModel = new DefaultTableModel(
                new String[]{"Mã SP", "Tên sản phẩm", "Số lượng", "Đơn giá", "Thành tiền"}, 0
        );
tableCart = new JTable(cartModel);
styleTable(tableCart);

ordersModel = new DefaultTableModel(
                new String[]{"OrderID", "Chi nhánh", "Nhân viên", "Tổng tiền", "Thanh toán", "Ngày tạo"}, 0
        );
tableOrders = new JTable(ordersModel);
styleTable(tableOrders);

detailsModel = new DefaultTableModel(
                new String[]{"Tên sản phẩm", "Số lượng", "Đơn giá", "Thành tiền"}, 0
        );
tableOrderDetails = new JTable(detailsModel);
styleTable(tableOrderDetails);

JPanel centerPanel = new JPanel(new GridLayout(1, 2, 12, 12));
        centerPanel.setBackground(UIStyle.BG);

JPanel leftCard = UIStyle.createCardLayout();
        leftCard.setLayout(new BorderLayout(8, 8));
        leftCard.add(createSectionTitle("Giỏ hàng"), BorderLayout.NORTH);
        leftCard.add(new JScrollPane(tableCart), BorderLayout.CENTER);

JPanel rightPanel = new JPanel(new GridLayout(2, 1, 12, 12));
        rightPanel.setBackground(UIStyle.BG);

JPanel ordersCard = UIStyle.createCardLayout();
        ordersCard.setLayout(new BorderLayout(8, 8));
        ordersCard.add(createSectionTitle("Danh sách hóa đơn"), BorderLayout.NORTH);
        ordersCard.add(new JScrollPane(tableOrders), BorderLayout.CENTER);

JPanel detailsCard = UIStyle.createCardLayout();
        detailsCard.setLayout(new BorderLayout(8, 8));
        detailsCard.add(createSectionTitle("Chi tiết hóa đơn"), BorderLayout.NORTH);
        detailsCard.add(new JScrollPane(tableOrderDetails), BorderLayout.CENTER);

        rightPanel.add(ordersCard);
        rightPanel.add(detailsCard);

        centerPanel.add(leftCard);
        centerPanel.add(rightPanel);

        root.add(topCard, BorderLayout.NORTH);
        root.add(centerPanel, BorderLayout.CENTER);

setContentPane(root);

loadProducts();
loadOrders();

        btnAddToCart.addActionListener(e -> addToCart());
        btnRemoveFromCart.addActionListener(e -> removeFromCart());
        btnCheckout.addActionListener(e -> checkout());

        tableOrders.getSelectionModel().addListSelectionListener(e -> {
        if (!e.getValueIsAdjusting()) {
loadOrderDetails();
            }
                    });

setVisible(true);
    }

private JLabel createSectionTitle(String text) {
    JLabel lbl = new JLabel(text);
    lbl.setFont(UIStyle.headingFont());
    lbl.setForeground(UIStyle.PRIMARY_DARK);
    return lbl;
}

private void styleTable(JTable table) {
    table.setRowHeight(26);
    table.setFont(UIStyle.normalFont());
    table.getTableHeader().setFont(UIStyle.buttonFont());
    table.getTableHeader().setBackground(UIStyle.PRIMARY);
    table.getTableHeader().setForeground(Color.WHITE);
    table.setSelectionBackground(new Color(230, 216, 201));
}

private void loadProducts() {
    cbProducts.removeAllItems();
    List<String> products = productRepo.getProductNames();
    for (String p : products) {
        cbProducts.addItem(p);
    }
}

private void addToCart() {
    try {
        String productName = (String) cbProducts.getSelectedItem();
        String quantityText = txtQuantity.getText().trim();

        if (productName == null || quantityText.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Vui lòng chọn sản phẩm và nhập số lượng");
            return;
        }

        int quantity = Integer.parseInt(quantityText);

        if (quantity <= 0) {
            JOptionPane.showMessageDialog(this, "Số lượng phải lớn hơn 0");
            return;
        }

        int productId = productRepo.getProductIdByName(productName);
        double unitPrice = productRepo.getPriceByName(productName);

        boolean found = false;

        for (int i = 0; i < cartItems.size(); i++) {
            CartItem item = cartItems.get(i);

            if (item.getProductId() == productId) {
                int newQuantity = item.getQuantity() + quantity;
                CartItem newItem = new CartItem(productId, productName, newQuantity, unitPrice);

                cartItems.set(i, newItem);
                cartModel.setValueAt(newQuantity, i, 2);
                cartModel.setValueAt(formatMoney(newItem.getUnitPrice()), i, 3);
                cartModel.setValueAt(formatMoney(newItem.getSubTotal()), i, 4);

                found = true;
                break;
            }
        }

        if (!found) {
            CartItem item = new CartItem(productId, productName, quantity, unitPrice);
            cartItems.add(item);

            cartModel.addRow(new Object[]{
                    item.getProductId(),
                    item.getProductName(),
                    item.getQuantity(),
                    formatMoney(item.getUnitPrice()),
                    formatMoney(item.getSubTotal())
            });
        }

        updateTotalAmount();
        txtQuantity.setText("");

    } catch (NumberFormatException e) {
        JOptionPane.showMessageDialog(this, "Số lượng phải là số");
    } catch (Exception e) {
        JOptionPane.showMessageDialog(this, "Lỗi thêm vào giỏ: " + e.getMessage());
    }
}

private void removeFromCart() {
    int selectedRow = tableCart.getSelectedRow();

    if (selectedRow == -1) {
        JOptionPane.showMessageDialog(this, "Vui lòng chọn sản phẩm cần xóa");
        return;
    }

    cartItems.remove(selectedRow);
    cartModel.removeRow(selectedRow);
    updateTotalAmount();
}

private void updateTotalAmount() {
    double total = 0;
    for (CartItem item : cartItems) {
        total += item.getSubTotal();
    }
    txtTotalAmount.setText(formatMoney(total));
}

private void checkout() {
    try {
        String payment = (String) cbPaymentMethod.getSelectedItem();

        if (cartItems.isEmpty()) {
            JOptionPane.showMessageDialog(this, "Giỏ hàng đang trống");
            return;
        }

        boolean stockOk = orderRepo.checkStockForCart(branchId, cartItems);
        if (!stockOk) {
            JOptionPane.showMessageDialog(this, "Không đủ nguyên liệu để thực hiện hóa đơn");
            return;
        }

        double total = 0;
        for (CartItem item : cartItems) {
            total += item.getSubTotal();
        }

        String offlineId = java.util.UUID.randomUUID().toString();

        int orderId = orderRepo.addOrderAndReturnId(branchId, userId, total, payment, offlineId);

        if (orderId <= 0) {
            JOptionPane.showMessageDialog(this, "Tạo hóa đơn thất bại");
            return;
        }

        boolean allOk = true;

        for (CartItem item : cartItems) {
            boolean detailOk = orderRepo.addOrderDetail(
                    orderId,
                    item.getProductId(),
                    item.getQuantity(),
                    item.getUnitPrice()
            );

            boolean deductOk = orderRepo.deductIngredients(
                    branchId,
                    item.getProductId(),
                    item.getQuantity()
            );

            if (!detailOk || !deductOk) {
                allOk = false;
            }
        }

        if (allOk) {
            JOptionPane.showMessageDialog(this, "Thanh toán thành công");
        } else {
            JOptionPane.showMessageDialog(this, "Có lỗi khi lưu hoặc trừ kho");
        }

        clearCart();
        loadOrders();
        detailsModel.setRowCount(0);

    } catch (Exception e) {
        JOptionPane.showMessageDialog(this, "Lỗi thanh toán: " + e.getMessage());
    }
}

    private void clearCart() {
        cartItems.clear();
        cartModel.setRowCount(0);
        txtTotalAmount.setText("");
        txtQuantity.setText("");
    }

    private void loadOrders() {
        ordersModel.setRowCount(0);

        String sql = "SELECT OrderID, BranchID, UserID, TotalAmount, PaymentMethod, CreatedAt FROM Orders ORDER BY OrderID DESC";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql);
             ResultSet rs = ps.executeQuery()) {

            while (rs.next()) {
                ordersModel.addRow(new Object[]{
                        rs.getInt("OrderID"),
                        rs.getInt("BranchID"),
                        rs.getInt("UserID"),
                        formatMoney(rs.getDouble("TotalAmount")),
                        rs.getString("PaymentMethod"),
                        rs.getTimestamp("CreatedAt")
                });
            }

        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Lỗi load hóa đơn: " + e.getMessage());
        }
    }

    private void loadOrderDetails() {
        int selectedRow = tableOrders.getSelectedRow();
        if (selectedRow == -1) return;

        int orderId = Integer.parseInt(ordersModel.getValueAt(selectedRow, 0).toString());
        detailsModel.setRowCount(0);

        String sql = "SELECT p.ProductName, od.Quantity, od.UnitPrice " +
                "FROM OrderDetails od " +
                "JOIN Products p ON od.ProductID = p.ProductID " +
                "WHERE od.OrderID = ?";

        try (Connection conn = DatabaseHelper.getConnection();
             PreparedStatement ps = conn.prepareStatement(sql)) {

            ps.setInt(1, orderId);

            ResultSet rs = ps.executeQuery();

            while (rs.next()) {
                String productName = rs.getString("ProductName");
                int quantity = rs.getInt("Quantity");
                double unitPrice = rs.getDouble("UnitPrice");
                double subTotal = quantity * unitPrice;

                detailsModel.addRow(new Object[]{
                        productName,
                        quantity,
                        formatMoney(unitPrice),
                        formatMoney(subTotal)
                });
            }

        } catch (Exception e) {
            JOptionPane.showMessageDialog(this, "Lỗi load chi tiết hóa đơn: " + e.getMessage());
        }
    }

    private String formatMoney(double amount) {
        NumberFormat formatter = NumberFormat.getInstance(new Locale("vi", "VN"));
        return formatter.format(amount) + " VND";
    }
}
