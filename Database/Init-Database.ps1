# Script khởi tạo Database cho Phụng Lộc Coffee
# Yêu cầu: Đã cài đặt SQL Server và có công cụ sqlcmd (thường đi kèm SSMS) hoặc Invoke-Sqlcmd

$Server = "localhost" # Thay đổi nếu Server của bạn khác
$Database = "QUANLYCHUOICUAHANG"

$Files = @(
    "PhungLocCoffee_FullDB.sql",
    "Bosung.sql",
    "DemoData_Enhanced.sql"
)

Write-Host "--- Bắt đầu khởi tạo cơ sở dữ liệu Phụng Lộc Coffee ---" -ForegroundColor Cyan

foreach ($File in $Files) {
    $FilePath = Join-Path $PSScriptRoot $File
    Write-Host "Đang thực thi: $File..." -ForegroundColor Yellow
    
    if (Get-Command sqlcmd -ErrorAction SilentlyContinue) {
        sqlcmd -S $Server -i $FilePath
    } elseif (Get-Module -ListAvailable SqlServer) {
        Invoke-Sqlcmd -ServerInstance $Server -InputFile $FilePath
    } else {
        Write-Host "LỖI: Không tìm thấy 'sqlcmd' hoặc module 'SqlServer'. Vui lòng cài đặt hoặc chạy file SQL thủ công trong SSMS." -ForegroundColor Red
        exit
    }
}

Write-Host "--- Hoàn tất khởi tạo dữ liệu! ---" -ForegroundColor Green
