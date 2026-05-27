USE [QUANLYCHUOICUAHANG];
GO

SET XACT_ABORT ON; -- Tự động rollback nếu có lỗi
BEGIN TRANSACTION;


-- Thêm user Kế toán nếu chưa tồn tại (kiểm tra qua Username)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Users] WHERE [Username] = N'accountant')
BEGIN
    INSERT INTO [dbo].[Users] (
        [Username], 
        [Password], 
        [FullName], 
        [BranchID], 
        [RoleID], 
        [IsActive], 
        [PasswordHash], 
        [Status]
    )
    VALUES (
        N'accountant',   -- Username
        N'123',          -- Password (mã hóa thực tế nên dùng hash, nhưng giữ như mẫu)
        N'Trần Kế Toán', -- FullName
        1,               -- BranchID (1: Quận 1, bạn có thể đổi sang chi nhánh khác)
        4,               -- RoleID (Kế toán)
        1,               -- IsActive (1: hoạt động)
        NULL,            -- PasswordHash (nếu dùng hash thì thay bằng giá trị)
        NULL             -- Status
    );
    PRINT 'Đã thêm user Kế toán.';
END
ELSE
BEGIN
    PRINT 'User account đã tồn tại.';
END



-- 2. Sinh dữ liệu giả cho Inventory (Branches 2-8)
DECLARE @BranchID INT = 2;
DECLARE @IngredientID INT;
DECLARE @Qty DECIMAL(18,4);
WHILE @BranchID <= 8
BEGIN
    SET @IngredientID = 1;
    WHILE @IngredientID <= 20
    BEGIN
        SET @Qty = 100 + (ABS(CHECKSUM(NEWID())) % 1901);
        IF NOT EXISTS (SELECT 1 FROM [dbo].[Inventory] WHERE [BranchID]=@BranchID AND [IngredientID]=@IngredientID)
            INSERT INTO [dbo].[Inventory] ([BranchID], [IngredientID], [CurrentQuantity]) VALUES (@BranchID, @IngredientID, @Qty);
        SET @IngredientID = @IngredientID + 1;
    END
    SET @BranchID = @BranchID + 1;
END

-- 3. Sinh dữ liệu InventoryTransactions & Details
SET @BranchID = 2;
DECLARE @UserID INT;
DECLARE @TransID UNIQUEIDENTIFIER;
DECLARE @i INT;
WHILE @BranchID <= 8
BEGIN
    SELECT TOP 1 @UserID = [UserID] FROM [dbo].[Users] WHERE [BranchID] = @BranchID;
    IF @UserID IS NOT NULL
    BEGIN
        SET @i = 1;
        WHILE @i <= 10
        BEGIN
            SET @TransID = NEWID();
            INSERT INTO [dbo].[InventoryTransactions] ([TransactionID], [BranchID], [UserID], [Note], [TotalAmount], [CreatedAt], [TransactionType], [IsSynced])
            VALUES (@TransID, @BranchID, @UserID, N'Nhập kho mẫu chi nhánh ' + CAST(@BranchID AS NVARCHAR), 0, DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 90), GETDATE()), N'Nhập kho', 1);
            INSERT INTO [dbo].[InventoryTransactionDetails] ([DetailID], [TransactionID], [IngredientID], [Quantity], [UnitPrice])
            VALUES (NEWID(), @TransID, 1, 10, 50000), (NEWID(), @TransID, 2, 5, 120000);
            SET @i = @i + 1;
        END
    END
    SET @BranchID = @BranchID + 1;
END

-- 4. Sinh dữ liệu InventoryAudit & Details
SET @BranchID = 2;
DECLARE @AuditID UNIQUEIDENTIFIER;
WHILE @BranchID <= 8
BEGIN
    SELECT TOP 1 @UserID = [UserID] FROM [dbo].[Users] WHERE [BranchID] = @BranchID;
    IF @UserID IS NOT NULL
    BEGIN
        SET @i = 1;
        WHILE @i <= 5
        BEGIN
            SET @AuditID = NEWID();
            INSERT INTO [dbo].[InventoryAudit] ([AuditID], [BranchID], [UserID], [AuditDate], [Notes], [IsSynced])
            VALUES (@AuditID, @BranchID, @UserID, DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 60), GETDATE()), N'Kiểm kê định kỳ mẫu', 1);
            INSERT INTO [dbo].[InventoryAuditDetail] ([DetailID], [AuditID], [IngredientID], [SystemQuantity], [ActualQuantity])
            VALUES (NEWID(), @AuditID, 3, 100, 98), (NEWID(), @AuditID, 4, 50, 50), (NEWID(), @AuditID, 5, 200, 205);
            SET @i = @i + 1;
        END
    END
    SET @BranchID = @BranchID + 1;
END

-- 5. Sinh dữ liệu StockTransfer & Details
SET @BranchID = 2;
DECLARE @TransferID UNIQUEIDENTIFIER;
DECLARE @ToBranchID INT;
WHILE @BranchID <= 8
BEGIN
    SELECT TOP 1 @UserID = [UserID] FROM [dbo].[Users] WHERE [BranchID] = @BranchID;
    IF @UserID IS NOT NULL
    BEGIN
        SET @i = 1;
        WHILE @i <= 5
        BEGIN
            SET @TransferID = NEWID();
            SET @ToBranchID = (@BranchID % 8) + 1;
            INSERT INTO [dbo].[StockTransfer] ([TransferID], [FromBranchID], [ToBranchID], [CreatedBy], [Status], [Note], [CreatedAt], [IsSynced])
            VALUES (@TransferID, @BranchID, @ToBranchID, @UserID, N'Hoàn thành', N'Chuyển hàng mẫu', DATEADD(DAY, -(ABS(CHECKSUM(NEWID())) % 30), GETDATE()), 1);
            INSERT INTO [dbo].[StockTransferDetail] ([DetailID], [TransferID], [IngredientID], [Quantity])
            VALUES (NEWID(), @TransferID, 10, 5), (NEWID(), @TransferID, 11, 2);
            SET @i = @i + 1;
        END
    END
    SET @BranchID = @BranchID + 1;
END

COMMIT TRANSACTION;