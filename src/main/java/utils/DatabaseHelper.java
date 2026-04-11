package utils;

import java.sql.Connection;
import java.sql.DriverManager;
import java.sql.SQLException;

public class DatabaseHelper {
    private static final String URL = "jdbc:sqlserver://localhost:1433;databaseName=QUANLYCHUOICUAHANG;integratedSecurity=true;encrypt=true;trustServerCertificate=true;";
    
    public static Connection getConnection() throws SQLException {
        return DriverManager.getConnection(URL);
    }
}