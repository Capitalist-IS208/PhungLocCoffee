import repository.UserRepository;

public class TestLogin {
    public static void main(String[] args) {
        UserRepository repo = new UserRepository();

        boolean ok = repo.login("admin", "123");

        if (ok) {
            System.out.println("Đăng nhập thành công");
            System.out.println("Tên: " + repo.getFullName("admin"));
        } else {
            System.out.println("Đăng nhập thất bại");
        }
    }
}