import utils.DatabaseHelper;
import java.sql.Connection;

public class TestConnection {
    public static void main(String[] args) {
        try (Connection conn = DatabaseHelper.getConnection()) {
            System.out.println("Kết nối thành công DB!");
        } catch (Exception e) {
            System.out.println("Kết nối thất bại!");
            e.printStackTrace();
        }
    }
}