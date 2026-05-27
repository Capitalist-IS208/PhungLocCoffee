USE [QUANLYCHUOICUAHANG];
GO

SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- 1. Cập nhật tên Chi nhánh chuyên nghiệp
UPDATE [dbo].[Branches] SET [BranchName] = N'Phụng Lộc Quận 1', [Address] = N'123 Lê Lợi, Quận 1', [Status] = N'Đang hoạt động' WHERE [BranchID] = 1;
UPDATE [dbo].[Branches] SET [BranchName] = N'Phụng Lộc Bình Thạnh', [Address] = N'45 Phan Đăng Lưu, Bình Thạnh', [Status] = N'Đang hoạt động' WHERE [BranchID] = 2;
UPDATE [dbo].[Branches] SET [BranchName] = N'Phụng Lộc Quận 3', [Address] = N'89 Cao Thắng, Quận 3', [Status] = N'Đang hoạt động' WHERE [BranchID] = 3;
UPDATE [dbo].[Branches] SET [BranchName] = N'Phụng Lộc Quận 10', [Address] = N'12 Thành Thái, Quận 10', [Status] = N'Đang hoạt động' WHERE [BranchID] = 4;
UPDATE [dbo].[Branches] SET [BranchName] = N'Phụng Lộc Tân Bình', [Address] = N'77 Cộng Hòa, Tân Bình', [Status] = N'Đang hoạt động' WHERE [BranchID] = 5;

-- 2. Sắp xếp/Cập nhật 12 nguyên liệu đầu tiên theo nhóm
-- Nhóm Cà phê
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Cà phê Robusta (Hạt)', [MinStockLevel] = 20 WHERE [IngredientID] = 1;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Cà phê Arabica (Hạt)', [MinStockLevel] = 15 WHERE [IngredientID] = 2;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Cà phê Espresso Blend', [MinStockLevel] = 10 WHERE [IngredientID] = 3;
-- Nhóm Sữa
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Sữa tươi thanh trùng', [MinStockLevel] = 30 WHERE [IngredientID] = 4;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Sữa đặc có đường', [MinStockLevel] = 20 WHERE [IngredientID] = 5;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Sữa hạt (Hạnh nhân/Yến mạch)', [MinStockLevel] = 10 WHERE [IngredientID] = 6;
-- Nhóm Đường/Đá
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Đường cát trắng', [MinStockLevel] = 25 WHERE [IngredientID] = 7;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Đường nâu/Đường phèn', [MinStockLevel] = 15 WHERE [IngredientID] = 8;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Đá viên tinh khiết', [MinStockLevel] = 50 WHERE [IngredientID] = 9;
-- Nhóm Topping/Khác
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Trân châu đen/trắng', [MinStockLevel] = 12 WHERE [IngredientID] = 10;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Kem tươi (Whipping cream)', [MinStockLevel] = 8 WHERE [IngredientID] = 11;
UPDATE [dbo].[Ingredients] SET [IngredientName] = N'Syrup các loại (Vani/Caramel)', [MinStockLevel] = 5 WHERE [IngredientID] = 12;

-- 3. Tạo cảnh báo tồn kho (5-7 mặt hàng tồn dưới ngưỡng tại Chi nhánh 1)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 5 WHERE [BranchID] = 1 AND [IngredientID] = 1; -- Robusta (Ngưỡng 20)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 10 WHERE [BranchID] = 1 AND [IngredientID] = 4; -- Sữa tươi (Ngưỡng 30)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 2 WHERE [BranchID] = 1 AND [IngredientID] = 12; -- Syrup (Ngưỡng 5)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 3 WHERE [BranchID] = 1 AND [IngredientID] = 11; -- Kem tươi (Ngưỡng 8)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 0 WHERE [BranchID] = 1 AND [IngredientID] = 10; -- Trân châu (Hết hàng)
UPDATE [dbo].[Inventory] SET [CurrentQuantity] = 40 WHERE [BranchID] = 1 AND [IngredientID] = 9; -- Đá viên (Ngưỡng 50)

-- 4. Sinh dữ liệu đơn hàng (Orders) với xu hướng tăng trưởng
-- Xóa bớt dữ liệu cũ nếu quá nhiều để tránh rối (Tùy chọn)
-- DELETE FROM [dbo].[Orders] WHERE [BranchID] IN (1,2,3,4,5);

DECLARE @MonthOffset INT = 0;
DECLARE @BranchIdx INT;
DECLARE @OrderCount INT;
DECLARE @TotalAmount DECIMAL(18,2);
DECLARE @OrderDate DATETIME;

WHILE @MonthOffset < 6 -- Sinh dữ liệu trong 6 tháng qua
BEGIN
    SET @BranchIdx = 1;
    WHILE @BranchIdx <= 5
    BEGIN
        -- Doanh thu tăng dần mỗi tháng (10-20% growth)
        SET @OrderCount = 20 + (@MonthOffset * 10) + (ABS(CHECKSUM(NEWID())) % 20);
        
        DECLARE @i INT = 0;
        WHILE @i < @OrderCount
        BEGIN
            SET @OrderDate = DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 28), DATEADD(MONTH, -@MonthOffset, GETDATE()));
            SET @TotalAmount = 45000 + (ABS(CHECKSUM(NEWID())) % 200000);
            
            INSERT INTO [dbo].[Orders] ([OrderID], [BranchID], [UserID], [CustomerName], [TotalAmount], [OrderDate], [OrderStatus], [IsSynced])
            VALUES (NEWID(), @BranchIdx, 1, N'Khách vãng lai', @TotalAmount, @OrderDate, N'Hoàn thành', 1);
            
            SET @i = @i + 1;
        END
        SET @BranchIdx = @BranchIdx + 1;
    END
    SET @MonthOffset = @MonthOffset + 1;
END

COMMIT TRANSACTION;
PRINT 'Dữ liệu demo chuyên nghiệp đã được nạp thành công!';
