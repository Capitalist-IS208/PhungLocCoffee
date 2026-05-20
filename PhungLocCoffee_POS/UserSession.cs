namespace PhungLocCoffee_POS
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
        public bool IsInventoryKeeper => RoleName == "Inventory Keeper";
    }
}