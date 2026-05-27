using PhungLocCoffee_POS.Models;
using PhungLocCoffee_POS.Views;
using PhungLocCoffee_POS.Helpers;


namespace PhungLocCoffee_POS.Models
{
    public class UserSession
    {
        public int UserID { get; set; }
        public int BranchID { get; set; }
        public int RoleID { get; set; }

        public string Username { get; set; } = "";
        public string FullName { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string RoleName { get; set; } = "";

        public bool IsAdmin => RoleName == "Admin";
        public bool IsManager => RoleName == "Manager";
        public bool IsStaff => RoleName == "Staff";
        public bool IsAccountant => RoleName == "Kế toán" || RoleName == "Accountant";
        public bool IsInventoryKeeper => RoleName == "Inventory Keeper";
    }
}

