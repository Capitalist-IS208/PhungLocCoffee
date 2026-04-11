package com.mycompany.capitalist_phungloccoffee;

import repository.OrderRepository;
import java.util.UUID; // Dùng để tạo mã Offline ngẫu nhiên

public class Capitalist_PhungLocCoffee {

    public static void main(String[] args) {
        System.out.println("Bắt đầu test đồng bộ dữ liệu...");
        
        OrderRepository orderRepo = new OrderRepository();
        
        // Giả lập máy POS rớt mạng, tạo ra 1 mã hóa đơn Offline ngẫu nhiên
        String fakeOfflineID = UUID.randomUUID().toString();
        
        // Khi có mạng lại, gọi hàm đồng bộ đẩy lên Server (BranchID = 1, UserID = 1, Tiền = 55000)
        boolean isSuccess = orderRepo.syncOfflineOrder(1, 1, 55000, "Tiền mặt", fakeOfflineID);
        
        if (isSuccess) {
            System.out.println("Đồng bộ thành công hóa đơn Offline lên Server!");
            // Gọi hàm in ra để kiểm tra
            orderRepo.printSyncedOrders();
        } else {
            System.out.println("Đồng bộ thất bại!");
        }
    }
}