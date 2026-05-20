using Microsoft.Data.Sqlite;
using System;
using System.IO;

namespace PhungLocCoffee_POS
{
    public static class LocalDatabaseHelper
    {
        private static readonly string DbPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "local_pos.db");

        private static readonly string ConnectionString =
            $"Data Source={DbPath}";

        public static string GetConnectionString()
        {
            return ConnectionString;
        }

        public static void InitializeDatabase()
        {
            using (SqliteConnection conn = new SqliteConnection(ConnectionString))
            {
                conn.Open();

                string createOrdersTable = @"
                CREATE TABLE IF NOT EXISTS LocalOrders
                (
                    LocalOrderID TEXT PRIMARY KEY,
                    BranchID INTEGER NOT NULL,
                    UserID INTEGER NOT NULL,
                    TotalAmount REAL NOT NULL,
                    PaymentMethod TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    IsSynced INTEGER NOT NULL DEFAULT 0
                );";

                using (SqliteCommand cmd = new SqliteCommand(createOrdersTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string createOrderDetailsTable = @"
                CREATE TABLE IF NOT EXISTS LocalOrderDetails
                (
                    DetailID INTEGER PRIMARY KEY AUTOINCREMENT,
                    LocalOrderID TEXT NOT NULL,
                    ProductID INTEGER NOT NULL,
                    ProductName TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    UnitPrice REAL NOT NULL,
                    FOREIGN KEY(LocalOrderID) REFERENCES LocalOrders(LocalOrderID)
                );";

                using (SqliteCommand cmd = new SqliteCommand(createOrderDetailsTable, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string createLocalCategories = @"
                CREATE TABLE IF NOT EXISTS LocalCategories
                (
                    CategoryID INTEGER PRIMARY KEY,
                    CategoryName TEXT NOT NULL
                );";

                using (SqliteCommand cmd = new SqliteCommand(createLocalCategories, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                string createLocalProducts = @"
                CREATE TABLE IF NOT EXISTS LocalProducts
                (
                    ProductID INTEGER PRIMARY KEY,
                    ProductName TEXT NOT NULL,
                    CategoryID INTEGER,
                    Price REAL NOT NULL,
                    IsActive INTEGER NOT NULL
                );";

                using (SqliteCommand cmd = new SqliteCommand(createLocalProducts, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}