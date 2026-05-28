-- ==========================================
-- 0. KHỞI TẠO DATABASE
-- ==========================================
CREATE DATABASE PetCareDB;
GO
USE PetCareDB;
GO

-- ==========================================
-- 1. DICTIONARY TABLES (CHUẨN HÓA ENUM/STATUS)
-- ==========================================
CREATE TABLE Role (
    RoleID INT IDENTITY(1,1) PRIMARY KEY,
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

-- BẢNG MỚI: TÀI KHOẢN ĐĂNG NHẬP TRUNG TÂM
CREATE TABLE Account (
    AccountID INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL,
    RoleID INT NOT NULL,
    IsActive BIT DEFAULT 1,
    IsDeleted BIT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (RoleID) REFERENCES Role(RoleID) ON DELETE NO ACTION
);

CREATE TABLE ServiceCategory (
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE BookingStatus (
    StatusID INT PRIMARY KEY, 
    StatusName VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE DetailStatus (
    StatusID INT PRIMARY KEY,
    StatusName VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE InvoiceStatus (
    StatusID INT PRIMARY KEY,
    StatusName VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE PaymentMethod (
    MethodID INT PRIMARY KEY,
    MethodName VARCHAR(20) NOT NULL UNIQUE
);

CREATE TABLE PetSpecies (
    SpeciesID INT IDENTITY(1,1) PRIMARY KEY,
    SpeciesName NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE PetBreed (
    BreedID INT IDENTITY(1,1) PRIMARY KEY,
    SpeciesID INT NOT NULL,
    BreedName NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Breed_Species UNIQUE (BreedID, SpeciesID),
    FOREIGN KEY (SpeciesID) REFERENCES PetSpecies(SpeciesID) ON DELETE NO ACTION
);

-- Dữ liệu hạt giống (Seed Data)
INSERT INTO BookingStatus VALUES (1, 'Pending'), (2, 'Confirmed'), (3, 'Completed'), (4, 'Cancelled'), (5, 'Expired'), (6, 'In Progress');
INSERT INTO DetailStatus VALUES (1, 'Not Started'), (2, 'In Progress'), (3, 'Done'), (4, 'Cancelled');
INSERT INTO InvoiceStatus VALUES (1, 'Unpaid'), (2, 'Partial'), (3, 'Paid');
INSERT INTO PaymentMethod VALUES (1, 'Cash'), (2, 'Transfer'), (3, 'Card');
GO

-- ==========================================
-- 2. SCHEMA CHÍNH (CORE BUSINESS)
-- ==========================================
CREATE TABLE Customer (
    CustomerID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT NULL, -- Khách vãng lai không có tài khoản; thành viên được ràng buộc unique bằng filtered index.
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(15) NOT NULL UNIQUE,
    Email VARCHAR(100), 
    Address NVARCHAR(255),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2,
    IsDeleted BIT DEFAULT 0,
    FOREIGN KEY (AccountID) REFERENCES Account(AccountID) ON DELETE SET NULL
);

CREATE TABLE Pet (
    PetID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NOT NULL,
    Name NVARCHAR(50) NOT NULL,
    SpeciesID INT NOT NULL,
    BreedID INT NOT NULL,
    Weight DECIMAL(5,2) CHECK (Weight > 0 AND Weight <= 150),
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2,
    IsDeleted BIT DEFAULT 0,
    FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID) ON DELETE NO ACTION,
    FOREIGN KEY (SpeciesID) REFERENCES PetSpecies(SpeciesID) ON DELETE NO ACTION,
    CONSTRAINT FK_Pet_Breed_Species FOREIGN KEY (BreedID, SpeciesID) REFERENCES PetBreed(BreedID, SpeciesID) ON DELETE NO ACTION
);

CREATE TABLE Employee (
    EmployeeID INT IDENTITY(1,1) PRIMARY KEY,
    AccountID INT UNIQUE, -- LIÊN KẾT VỚI BẢNG TÀI KHOẢN
    RoleID INT NOT NULL, 
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(15) NOT NULL UNIQUE, 
    IsActive BIT DEFAULT 1,
    IsDeleted BIT DEFAULT 0,
    FOREIGN KEY (AccountID) REFERENCES Account(AccountID) ON DELETE SET NULL,
    FOREIGN KEY (RoleID) REFERENCES Role(RoleID) ON DELETE NO ACTION
);

CREATE TABLE ServiceCatalog (
    ServiceID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryID INT NOT NULL,
    ServiceName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    BasePrice DECIMAL(18,2) NOT NULL CHECK (BasePrice >= 0),
    EstimatedDuration INT CHECK (EstimatedDuration > 0),   
    
    -- CỘT MỚI: Quy định số lượng bé có thể phục vụ cùng lúc trong 1 khung giờ
    MaxCapacity INT NOT NULL DEFAULT 3, 
    
    IsActive BIT DEFAULT 1,
    IsDeleted BIT DEFAULT 0,
    FOREIGN KEY (CategoryID) REFERENCES ServiceCategory(CategoryID) ON DELETE NO ACTION
);

-- ==================== VẬT TƯ & KHUYẾN MÃI ====================
CREATE TABLE MedicalSupply (
    SupplyID INT IDENTITY(1,1) PRIMARY KEY,
    SupplyName NVARCHAR(100) NOT NULL,
    Unit NVARCHAR(20) NOT NULL,
    StockQuantity INT DEFAULT 0,
    MinStockLevel INT DEFAULT 5,
    ExpiryDate DATE NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2 NULL,
    IsDeleted BIT DEFAULT 0
);

CREATE TABLE ServiceMaterialQuota (
    ServiceID INT NOT NULL,
    SupplyID INT NOT NULL,
    QuantityUsed INT DEFAULT 1 CHECK (QuantityUsed > 0),
    PRIMARY KEY (ServiceID, SupplyID),
    FOREIGN KEY (ServiceID) REFERENCES ServiceCatalog(ServiceID) ON DELETE CASCADE,
    FOREIGN KEY (SupplyID) REFERENCES MedicalSupply(SupplyID) ON DELETE CASCADE
);

-- BẢNG MỚI: Sổ cái kho (Lịch sử giao dịch kho)
CREATE TABLE InventoryTransaction (
    TransactionID INT IDENTITY(1,1) PRIMARY KEY,
    SupplyID INT NOT NULL,
    TransactionType VARCHAR(20) NOT NULL 
        CHECK (TransactionType IN ('IMPORT', 'EXPORT_SERVICE', 'RETURN_SERVICE', 'ADJUSTMENT', 'EXPIRED')),
    QuantityChange INT NOT NULL, 
    ReferenceID INT NULL, 
    Note NVARCHAR(255),
    EmployeeID INT NULL, 
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (SupplyID) REFERENCES MedicalSupply(SupplyID) ON DELETE CASCADE,
    FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID) ON DELETE NO ACTION
);

CREATE TABLE Promotion (
    PromotionID INT IDENTITY(1,1) PRIMARY KEY,
    PromoCode VARCHAR(20) NOT NULL UNIQUE,
    DiscountType VARCHAR(15) CHECK (DiscountType IN ('Percentage', 'FixedAmount')),
    DiscountValue DECIMAL(18,2) NOT NULL CHECK (DiscountValue > 0),
    MaxDiscount DECIMAL(18,2) DEFAULT 0,
    MinOrderValue DECIMAL(18,2) DEFAULT 0,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    CONSTRAINT CHK_PromoDate CHECK (EndDate >= StartDate)
);

-- ==================== VẬN HÀNH & ĐÁNH GIÁ ====================
CREATE TABLE Booking (
    BookingID INT IDENTITY(1,1) PRIMARY KEY,
    BookingCode VARCHAR(30) NOT NULL UNIQUE, 
    CustomerID INT NOT NULL,
    BookingDate DATETIME2 NOT NULL,
    Notes NVARCHAR(MAX),
    StatusID INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2,
    IsDeleted BIT DEFAULT 0,
    FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID) ON DELETE NO ACTION,
    FOREIGN KEY (StatusID) REFERENCES BookingStatus(StatusID) ON DELETE NO ACTION
);

CREATE TABLE BookingDetail (
    BookingDetailID INT IDENTITY(1,1) PRIMARY KEY,
    BookingID INT NOT NULL,
    PetID INT NOT NULL,
    ServiceID INT NOT NULL,
    ActualPrice DECIMAL(18,2) NOT NULL CHECK (ActualPrice >= 0),
    StartTime DATETIME2,
    EndTime DATETIME2,
    StatusID INT NOT NULL DEFAULT 1,
    ModifiedAt DATETIME2,
    CONSTRAINT UQ_BookingDetail UNIQUE (BookingID, PetID, ServiceID),
    CONSTRAINT CHK_BookingTime CHECK (EndTime IS NULL OR StartTime IS NULL OR EndTime >= StartTime),
    FOREIGN KEY (BookingID) REFERENCES Booking(BookingID) ON DELETE NO ACTION,
    FOREIGN KEY (PetID) REFERENCES Pet(PetID) ON DELETE NO ACTION,
    FOREIGN KEY (ServiceID) REFERENCES ServiceCatalog(ServiceID) ON DELETE NO ACTION,
    FOREIGN KEY (StatusID) REFERENCES DetailStatus(StatusID) ON DELETE NO ACTION
);

CREATE TABLE BookingDetail_Employee (
    BookingDetailID INT NOT NULL,
    EmployeeID INT NOT NULL,
    AssignedAt DATETIME2 DEFAULT GETDATE(),
    PRIMARY KEY (BookingDetailID, EmployeeID),
    FOREIGN KEY (BookingDetailID) REFERENCES BookingDetail(BookingDetailID) ON DELETE CASCADE,
    FOREIGN KEY (EmployeeID) REFERENCES Employee(EmployeeID) ON DELETE NO ACTION
);

CREATE TABLE ServiceReview (
    ReviewID INT IDENTITY(1,1) PRIMARY KEY,
    BookingDetailID INT NOT NULL UNIQUE, 
    CustomerID INT NOT NULL,
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Content NVARCHAR(MAX),
    ReviewTags NVARCHAR(255),
    StoreReply NVARCHAR(MAX),
    IsVisible BIT DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (BookingDetailID) REFERENCES BookingDetail(BookingDetailID) ON DELETE CASCADE,
    FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID) ON DELETE NO ACTION
);

-- ==================== TÀI CHÍNH ====================
CREATE TABLE Invoice (
    InvoiceID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceCode VARCHAR(30) NOT NULL UNIQUE, 
    BookingID INT NOT NULL UNIQUE,
    PromotionID INT NULL,
    TotalAmount DECIMAL(18,2) DEFAULT 0 CHECK (TotalAmount >= 0),
    DiscountAmount DECIMAL(18,2) DEFAULT 0 CHECK (DiscountAmount >= 0),
    PaidAmount DECIMAL(18,2) DEFAULT 0 CHECK (PaidAmount >= 0),
    StatusID INT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    ModifiedAt DATETIME2,
    FOREIGN KEY (BookingID) REFERENCES Booking(BookingID) ON DELETE NO ACTION,
    FOREIGN KEY (StatusID) REFERENCES InvoiceStatus(StatusID) ON DELETE NO ACTION,
    FOREIGN KEY (PromotionID) REFERENCES Promotion(PromotionID) ON DELETE NO ACTION
);

CREATE TABLE Payment (
    PaymentID INT IDENTITY(1,1) PRIMARY KEY,
    InvoiceID INT NOT NULL,
    Amount DECIMAL(18,2) NOT NULL CHECK (Amount > 0),
    MethodID INT NOT NULL,
    PaymentDate DATETIME2 DEFAULT GETDATE(),
    Note NVARCHAR(255),
    FOREIGN KEY (InvoiceID) REFERENCES Invoice(InvoiceID) ON DELETE NO ACTION,
    FOREIGN KEY (MethodID) REFERENCES PaymentMethod(MethodID) ON DELETE NO ACTION
);
GO

-- ==========================================
-- 3. TẠO INDEXES CHUYÊN SÂU
-- ==========================================
CREATE UNIQUE NONCLUSTERED INDEX UX_Customer_Email ON Customer(Email) WHERE Email IS NOT NULL;
CREATE UNIQUE NONCLUSTERED INDEX UX_Customer_AccountID_NotNull ON Customer(AccountID) WHERE AccountID IS NOT NULL;
CREATE NONCLUSTERED INDEX IX_Pet_CustomerID ON Pet(CustomerID);
CREATE NONCLUSTERED INDEX IX_Booking_CustomerID ON Booking(CustomerID);
CREATE NONCLUSTERED INDEX IX_BookingDetail_BookingID ON BookingDetail(BookingID);
CREATE NONCLUSTERED INDEX IX_BookingDetail_PetID ON BookingDetail(PetID);
CREATE NONCLUSTERED INDEX IX_BookingDetail_ServiceID ON BookingDetail(ServiceID);
CREATE NONCLUSTERED INDEX IX_Payment_InvoiceID ON Payment(InvoiceID);
CREATE NONCLUSTERED INDEX IX_Payment_MethodID ON Payment(MethodID);
CREATE NONCLUSTERED INDEX IX_Booking_StatusID ON Booking(StatusID);
CREATE NONCLUSTERED INDEX IX_BookingDetail_StatusID ON BookingDetail(StatusID);
CREATE NONCLUSTERED INDEX IX_Invoice_StatusID ON Invoice(StatusID);
CREATE NONCLUSTERED INDEX IX_Invoice_PromotionID ON Invoice(PromotionID);
CREATE NONCLUSTERED INDEX IX_MedicalSupply_Expiry ON MedicalSupply(ExpiryDate);
CREATE NONCLUSTERED INDEX IX_InventoryTransaction_SupplyID ON InventoryTransaction(SupplyID);
GO

-- ==========================================
-- 4. FUNCTIONS 
-- ==========================================
CREATE FUNCTION fn_CalculateDiscountAmount (@TotalAmount DECIMAL(18,2), @PromotionID INT, @ReferenceDate DATETIME2)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @DiscountAmount DECIMAL(18,2) = 0;
    DECLARE @Type VARCHAR(15), @Value DECIMAL(18,2), @Max DECIMAL(18,2), @MinOrder DECIMAL(18,2);

    IF @PromotionID IS NOT NULL
    BEGIN
        SELECT @Type = DiscountType, @Value = DiscountValue, @Max = MaxDiscount, @MinOrder = MinOrderValue
        FROM Promotion 
        WHERE PromotionID = @PromotionID AND IsActive = 1 
          AND @ReferenceDate BETWEEN StartDate AND EndDate; 

        IF @Type IS NOT NULL AND @TotalAmount >= ISNULL(@MinOrder, 0)
        BEGIN
            IF @Type = 'FixedAmount' 
                SET @DiscountAmount = @Value;
            ELSE IF @Type = 'Percentage'
            BEGIN
                SET @DiscountAmount = @TotalAmount * (@Value / 100.0);
                IF @Max > 0 AND @DiscountAmount > @Max SET @DiscountAmount = @Max;
            END
        END
    END

    IF @DiscountAmount > @TotalAmount SET @DiscountAmount = @TotalAmount;
    RETURN ISNULL(@DiscountAmount, 0);
END;
GO

CREATE FUNCTION fn_GetInvoiceBalance (@InvoiceID INT)
RETURNS DECIMAL(18,2)
AS
BEGIN
    DECLARE @Balance DECIMAL(18,2);
    SELECT @Balance = CASE
        WHEN b.StatusID IN (4, 5) THEN 0
        ELSE i.TotalAmount - i.DiscountAmount - i.PaidAmount
    END
    FROM Invoice i
    INNER JOIN Booking b ON b.BookingID = i.BookingID
    WHERE i.InvoiceID = @InvoiceID;
    RETURN ISNULL(@Balance, 0);
END;
GO

-- ==========================================
-- 5. TRIGGERS
-- ==========================================
CREATE TRIGGER trg_ValidatePetOwnership
ON BookingDetail AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        INNER JOIN Booking b ON i.BookingID = b.BookingID
        INNER JOIN Pet p ON i.PetID = p.PetID
        WHERE b.CustomerID <> p.CustomerID
    )
    BEGIN
        RAISERROR('Lỗi Logic: Thú cưng không thuộc sở hữu của khách hàng đặt lịch!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TRIGGER trg_HandleStockOnStatusChange
ON BookingDetail AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    
    -- 1. XUẤT KHO: Trạng thái đổi TỪ khác 3 SANG 3 (Done)
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.BookingDetailID = d.BookingDetailID WHERE i.StatusID = 3 AND d.StatusID <> 3) 
    BEGIN
        UPDATE ms
        SET ms.StockQuantity = ms.StockQuantity - smq.QuantityUsed,
            ms.ModifiedAt = GETDATE()
        FROM MedicalSupply ms
        INNER JOIN ServiceMaterialQuota smq ON ms.SupplyID = smq.SupplyID
        INNER JOIN inserted i ON smq.ServiceID = i.ServiceID
        INNER JOIN deleted d ON i.BookingDetailID = d.BookingDetailID
        WHERE i.StatusID = 3 AND d.StatusID <> 3;

        INSERT INTO InventoryTransaction (SupplyID, TransactionType, QuantityChange, ReferenceID, Note)
        SELECT 
            smq.SupplyID, 
            'EXPORT_SERVICE', 
            -smq.QuantityUsed, 
            i.BookingDetailID, 
            N'Xuất kho tự động khi hoàn thành dịch vụ'
        FROM inserted i 
        JOIN deleted d ON i.BookingDetailID = d.BookingDetailID
        JOIN ServiceMaterialQuota smq ON i.ServiceID = smq.ServiceID
        WHERE i.StatusID = 3 AND d.StatusID <> 3;
    END

    -- 2. HOÀN KHO: Trạng thái đổi TỪ 3 (Done) SANG trạng thái khác
    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.BookingDetailID = d.BookingDetailID WHERE d.StatusID = 3 AND i.StatusID <> 3) 
    BEGIN
        UPDATE ms
        SET ms.StockQuantity = ms.StockQuantity + smq.QuantityUsed,
            ms.ModifiedAt = GETDATE()
        FROM MedicalSupply ms
        INNER JOIN ServiceMaterialQuota smq ON ms.SupplyID = smq.SupplyID
        INNER JOIN deleted d ON smq.ServiceID = d.ServiceID
        INNER JOIN inserted i ON d.BookingDetailID = i.BookingDetailID
        WHERE d.StatusID = 3 AND i.StatusID <> 3;

        INSERT INTO InventoryTransaction (SupplyID, TransactionType, QuantityChange, ReferenceID, Note)
        SELECT 
            smq.SupplyID, 
            'RETURN_SERVICE', 
            smq.QuantityUsed, 
            i.BookingDetailID, 
            N'Hoàn kho tự động do dịch vụ bị đổi trạng thái'
        FROM deleted d 
        JOIN inserted i ON d.BookingDetailID = i.BookingDetailID
        JOIN ServiceMaterialQuota smq ON d.ServiceID = smq.ServiceID
        WHERE d.StatusID = 3 AND i.StatusID <> 3;
    END
END;
GO

/*
    Không tạo trigger tự cập nhật Invoice.TotalAmount hoặc DiscountAmount.
    Ứng dụng tính VAT và khuyến mãi tại InvoiceBusinessService; tránh việc đổi
    trạng thái BookingDetail âm thầm ghi đè tổng hóa đơn và làm mất VAT 10%.
*/

CREATE TRIGGER trg_StrictUpdateInvoicePayment
ON Payment AFTER INSERT, UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @AffectedInvoices TABLE (InvoiceID INT);
    
    INSERT INTO @AffectedInvoices (InvoiceID)
    SELECT DISTINCT InvoiceID FROM inserted
    UNION SELECT DISTINCT InvoiceID FROM deleted;

    UPDATE inv
    SET inv.PaidAmount = ISNULL(p.TotalPaid, 0),
        inv.ModifiedAt = GETDATE()
    FROM Invoice inv
    INNER JOIN @AffectedInvoices ai ON inv.InvoiceID = ai.InvoiceID
    LEFT JOIN (
        SELECT InvoiceID, SUM(Amount) AS TotalPaid
        FROM Payment
        WHERE InvoiceID IN (SELECT InvoiceID FROM @AffectedInvoices)
        GROUP BY InvoiceID
    ) p ON inv.InvoiceID = p.InvoiceID;

    IF EXISTS (
        SELECT 1 FROM Invoice i
        INNER JOIN @AffectedInvoices ai ON i.InvoiceID = ai.InvoiceID
        WHERE i.PaidAmount > (i.TotalAmount - i.DiscountAmount)
    )
    BEGIN
        RAISERROR('Lỗi Tài chính: Tổng số tiền thanh toán không được vượt quá số tiền cần thu!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    UPDATE Invoice
    SET StatusID = CASE 
                    WHEN PaidAmount = 0 THEN 1 
                    WHEN PaidAmount > 0 AND PaidAmount < (TotalAmount - DiscountAmount) THEN 2 
                    WHEN PaidAmount >= (TotalAmount - DiscountAmount) THEN 3 
                 END
    WHERE InvoiceID IN (SELECT InvoiceID FROM @AffectedInvoices);
END;
GO

-- ==========================================
-- 6. STORED PROCEDURES 
-- ==========================================
CREATE PROCEDURE sp_CreateBookingWithInvoice
    @CustomerID INT,
    @BookingDate DATETIME2,
    @Notes NVARCHAR(MAX),
    @NewBookingID INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; 

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Customer WHERE CustomerID = @CustomerID AND IsDeleted = 1)
        BEGIN
            RAISERROR('Khách hàng này đã bị xóa hoặc không tồn tại.', 16, 1);
            RETURN;
        END

        BEGIN TRANSACTION;

        INSERT INTO Booking (BookingCode, CustomerID, BookingDate, Notes, StatusID)
        VALUES ('TEMP', @CustomerID, @BookingDate, @Notes, 1); 

        SET @NewBookingID = SCOPE_IDENTITY();

        DECLARE @DatePrefix VARCHAR(8) = CONVERT(VARCHAR(8), GETDATE(), 112);
        DECLARE @FormattedID VARCHAR(10) = FORMAT(@NewBookingID, '00000');
        DECLARE @BookingCode VARCHAR(30) = 'BK-' + @DatePrefix + '-' + @FormattedID;
        
        UPDATE Booking SET BookingCode = @BookingCode WHERE BookingID = @NewBookingID;

        DECLARE @InvoiceCode VARCHAR(30) = 'INV-' + @DatePrefix + '-' + @FormattedID;
        
        INSERT INTO Invoice (InvoiceCode, BookingID, TotalAmount, DiscountAmount, PaidAmount, StatusID)
        VALUES (@InvoiceCode, @NewBookingID, 0, 0, 0, 1); 

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_AddBookingDetail
    @BookingID INT,
    @PetID INT,
    @ServiceID INT,
    @EmployeeID INT = NULL 
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; 

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Pet WHERE PetID = @PetID AND IsDeleted = 1)
        BEGIN
            RAISERROR('Thú cưng này đã bị xóa hoặc không tồn tại.', 16, 1);
            RETURN;
        END

        BEGIN TRANSACTION;

        DECLARE @ActualPrice DECIMAL(18,2);
        SELECT @ActualPrice = BasePrice FROM ServiceCatalog WHERE ServiceID = @ServiceID AND IsDeleted = 0;

        IF @ActualPrice IS NULL
        BEGIN
            RAISERROR('Dịch vụ không tồn tại hoặc đã bị ngừng cung cấp.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        DECLARE @NewDetailID INT;
        INSERT INTO BookingDetail (BookingID, PetID, ServiceID, ActualPrice, StatusID)
        VALUES (@BookingID, @PetID, @ServiceID, @ActualPrice, 1); 

        SET @NewDetailID = SCOPE_IDENTITY();

        /*
            Khi dùng trực tiếp stored procedure này, đồng bộ hóa đơn một cách
            tường minh và luôn bao gồm VAT 10%. Ứng dụng MVC tính tương đương
            trong InvoiceBusinessService và không phụ thuộc trigger.
        */
        UPDATE inv
        SET inv.TotalAmount = totals.Subtotal * CAST(1.10 AS DECIMAL(5,2)),
            inv.DiscountAmount = dbo.fn_CalculateDiscountAmount(
                totals.Subtotal * CAST(1.10 AS DECIMAL(5,2)),
                inv.PromotionID,
                inv.CreatedAt),
            inv.ModifiedAt = GETDATE()
        FROM Invoice inv
        CROSS APPLY (
            SELECT SUM(bd.ActualPrice) AS Subtotal
            FROM BookingDetail bd
            WHERE bd.BookingID = @BookingID AND bd.StatusID <> 4
        ) totals
        WHERE inv.BookingID = @BookingID;

        IF @EmployeeID IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM Employee WHERE EmployeeID = @EmployeeID AND (IsDeleted = 1 OR IsActive = 0))
            BEGIN
                RAISERROR('Nhân viên đã nghỉ việc hoặc không hoạt động.', 16, 1);
                ROLLBACK TRANSACTION;
                RETURN;
            END

            INSERT INTO BookingDetail_Employee (BookingDetailID, EmployeeID)
            VALUES (@NewDetailID, @EmployeeID);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_ProcessPayment
    @InvoiceID INT,
    @Amount DECIMAL(18,2),
    @MethodID INT, 
    @Note NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON; 

    BEGIN TRY
        BEGIN TRANSACTION;

        DECLARE @Balance DECIMAL(18,2), @BookingStatusID INT;
        SELECT @Balance = (inv.TotalAmount - inv.DiscountAmount - inv.PaidAmount),
               @BookingStatusID = b.StatusID
        FROM Invoice inv
        INNER JOIN Booking b ON b.BookingID = inv.BookingID
        WHERE inv.InvoiceID = @InvoiceID;
        
        IF @Balance IS NULL
        BEGIN
             RAISERROR('Hóa đơn không tồn tại.', 16, 1);
             ROLLBACK TRANSACTION;
             RETURN;
        END

        IF @Balance <= 0
        BEGIN
            RAISERROR('Hóa đơn này đã được thanh toán đủ.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @BookingStatusID IN (4, 5)
        BEGIN
            RAISERROR('Lịch đã hủy hoặc hết hạn không thể ghi nhận thanh toán mới.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF @Amount > @Balance
        BEGIN
            RAISERROR('Số tiền thanh toán vượt quá dư nợ hiện tại.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM PaymentMethod WHERE MethodID = @MethodID)
        BEGIN
            RAISERROR('Phương thức thanh toán không hợp lệ.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END

        INSERT INTO Payment (InvoiceID, Amount, MethodID, Note)
        VALUES (@InvoiceID, @Amount, @MethodID, @Note);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

CREATE PROCEDURE sp_ImportMedicalSupply
    @SupplyID INT,
    @ImportQuantity INT,
    @EmployeeID INT,
    @Note NVARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        IF @ImportQuantity <= 0
        BEGIN
            RAISERROR('Số lượng nhập phải lớn hơn 0.', 16, 1);
            RETURN;
        END

        BEGIN TRANSACTION;

        UPDATE MedicalSupply
        SET StockQuantity = StockQuantity + @ImportQuantity,
            ModifiedAt = GETDATE()
        WHERE SupplyID = @SupplyID;

        INSERT INTO InventoryTransaction (SupplyID, TransactionType, QuantityChange, EmployeeID, Note)
        VALUES (@SupplyID, 'IMPORT', @ImportQuantity, @EmployeeID, ISNULL(@Note, N'Nhập hàng mới vào kho'));

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END;
GO

-- =========================================================
-- SCRIPT INSERT DỮ LIỆU MẪU CHO PETCARE DB
-- =========================================================

-- 1. Chèn Danh mục dịch vụ
INSERT INTO ServiceCategory (CategoryName) VALUES 
(N'Spa & Cắt tỉa'), 
(N'Y tế & Khám chữa bệnh'), 
(N'Khách sạn thú cưng'), 
(N'Chăm sóc răng miệng');
GO

-- 2. Chèn Dịch vụ
INSERT INTO ServiceCatalog (CategoryID, ServiceName, Description, BasePrice, EstimatedDuration, MaxCapacity, IsActive, IsDeleted) VALUES
(1, N'Tắm gội & Sấy khô cơ bản', N'Làm sạch sâu, sấy tơi bồng bềnh và vệ sinh tai móng cơ bản cho bé.', 150000, 60, 3, 1, 0),
(1, N'Cắt tỉa tạo kiểu chuyên nghiệp', N'Cắt tỉa lông theo yêu cầu, tạo hình xinh xắn chuẩn idol cho thú cưng.', 300000, 90, 4, 1, 0),
(1, N'Spa VIP & Sục Microbubble', N'Phục hồi lông da, sục Microbubble thư giãn sâu chuẩn hoàng gia.', 500000, 90, 1, 1, 0),
(1, N'Nhuộm màu an toàn', N'Nhuộm lông highlight hoặc toàn thân bằng thuốc nhuộm hữu cơ an toàn tuyệt đối.', 450000, 120, 2, 1, 0),
(1, N'Massage thư giãn tinh dầu', N'Massage body bằng tinh dầu thiên nhiên chuyên dụng giúp bé giảm stress.', 250000, 60, 2, 1, 0),
(2, N'Khám sức khỏe tổng quát', N'Kiểm tra sức khỏe toàn diện bởi bác sĩ thú y có chứng chỉ quốc tế.', 200000, 45, 2, 1, 0),
(2, N'Tiêm phòng Vaccine dại', N'Tiêm phòng các bệnh cơ bản và bệnh dại bảo vệ sức khỏe cho chó mèo.', 250000, 30, 5, 1, 0),
(2, N'Siêu âm & Xét nghiệm máu', N'Chẩn đoán hình ảnh và xét nghiệm máu tổng quát giúp phát hiện sớm bệnh lý.', 600000, 60, 1, 1, 0),
(3, N'Lưu trú Khách sạn (Standard)', N'Phòng lưu trú riêng biệt, có máy lạnh và camera giám sát 24/7.', 150000, 30, 10, 1, 0),
(3, N'Lưu trú Khách sạn (Phòng VIP)', N'Phòng siêu rộng, chế độ ăn đặc biệt hạt hữu cơ và giờ vui chơi riêng.', 350000, 30, 3, 1, 0),
(4, N'Vệ sinh lấy cao răng siêu âm', N'Lấy cao răng bằng sóng siêu âm hiện đại, không gây đau đớn.', 400000, 60, 2, 1, 0),
(4, N'Điều trị viêm da đặc trị', N'Liệu trình tắm thảo dược kết hợp bôi thuốc trị nấm, ghẻ, viêm da cho thú cưng.', 350000, 60, 2, 1, 0);
GO

-- 3. Chèn Loài và Giống thú cưng
INSERT INTO PetSpecies (SpeciesName) VALUES (N'Chó'), (N'Mèo');
GO

INSERT INTO PetBreed (SpeciesID, BreedName) VALUES
(1, N'Poodle'), (1, N'Corgi'), (1, N'Golden Retriever'), (1, N'Pug'), (1, N'Phốc Sóc (Chihuahua)'),
(2, N'Mèo Anh lông ngắn (ALN)'), (2, N'Mèo Anh lông dài (ALD)'), (2, N'Mèo Ba Tư'), (2, N'Mèo Sphynx'), (2, N'Mèo Ta');
GO

-- 4. Chèn Vai trò và Tài Khoản
INSERT INTO Role (RoleName, Description) VALUES 
(N'Admin', N'Quản trị viên toàn quyền hệ thống'), 
(N'Bác sĩ thú y', N'Chịu trách nhiệm khám và điều trị'), 
(N'Groomer', N'Nhân viên Spa, tắm và cắt tỉa lông'),
(N'Khách hàng', N'Khách hàng sử dụng dịch vụ');
GO

INSERT INTO Account (Username, Password, RoleID) VALUES
('0901234567', '123456', 1), -- ID 1: Admin Danh
('0912345678', '123456', 2), -- ID 2: Bác sĩ Tài
('0923456789', '123456', 3), -- ID 3: Groomer Hà
('0987654321', '123456', 4), -- ID 4: Khách hàng Tùng
('0976543210', '123456', 4), -- ID 5: Khách hàng Mai
('0965432109', '123456', 4); -- ID 6: Khách hàng Phương
GO

-- 5. Chèn Nhân viên
INSERT INTO Employee (AccountID, RoleID, FullName, PhoneNumber, IsActive, IsDeleted) VALUES
(1, 1, N'Lê Đình Danh', '0901234567', 1, 0),
(2, 2, N'Nguyễn Văn Tài', '0912345678', 1, 0),
(3, 3, N'Trần Thu Hà', '0923456789', 1, 0);
GO

-- 6. Chèn Khách hàng
INSERT INTO Customer (AccountID, FullName, PhoneNumber, Email, Address, CreatedAt, IsDeleted) VALUES
(4, N'Phan Mạnh Tùng', '0987654321', 'tung.phan@email.com', N'Liên Chiểu, Đà Nẵng', GETDATE(), 0),
(5, N'Nguyễn Hoàng Mai', '0976543210', 'mai.nguyen@email.com', N'Hải Châu, Đà Nẵng', GETDATE(), 0),
(6, N'Lê Trần Phương', '0965432109', 'phuong.le@email.com', N'Sơn Trà, Đà Nẵng', GETDATE(), 0);
GO

-- 7. Chèn Thú cưng của Khách hàng
INSERT INTO Pet (CustomerID, Name, SpeciesID, BreedID, Weight, Notes, CreatedAt, IsDeleted) VALUES
(1, N'Bé Củ Cải', 1, 2, 8.5, N'Hơi nhát người lạ, cần nhẹ nhàng', GETDATE(), 0),
(1, N'Mimi', 2, 6, 4.2, N'Thích ăn pate cá ngừ', GETDATE(), 0),
(2, N'Đại Ngáo', 1, 3, 25.0, N'Rất thân thiện, hay quẫy đuôi', GETDATE(), 0),
(3, N'Hoàng Thượng', 2, 9, 3.5, N'Da nhạy cảm, chỉ dùng sữa tắm hữu cơ', GETDATE(), 0);
GO

-- 8. Chèn Kho Vật tư y tế / Sữa tắm (Medical Supply)
INSERT INTO MedicalSupply (SupplyName, Unit, StockQuantity, MinStockLevel, ExpiryDate, CreatedAt, IsDeleted) VALUES
(N'Sữa tắm nấm Fungicure', N'Chai', 50, 10, '2026-12-31', GETDATE(), 0),
(N'Sữa tắm siêu mượt lông', N'Chai', 100, 20, '2027-06-30', GETDATE(), 0),
(N'Vaccine dại Rabisin', N'Liều', 200, 50, '2025-12-31', GETDATE(), 0),
(N'Thuốc tẩy giun sán', N'Viên', 500, 100, '2026-05-15', GETDATE(), 0);
GO
CREATE TABLE ContactMessage (
    ContactMessageID INT IDENTITY(1,1) PRIMARY KEY,
    CustomerID INT NULL,
    FullName NVARCHAR(100) NOT NULL,
    PhoneNumber VARCHAR(20) NOT NULL,
    Email VARCHAR(100) NULL,
    Topic NVARCHAR(100) NULL,
    Message NVARCHAR(1000) NOT NULL,
    Status VARCHAR(20) NOT NULL DEFAULT 'New',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    RepliedAt DATETIME NULL,
    AdminNote NVARCHAR(1000) NULL,

    FOREIGN KEY (CustomerID) REFERENCES Customer(CustomerID)
);
GO

CREATE NONCLUSTERED INDEX IX_ContactMessage_CustomerID ON ContactMessage(CustomerID);
GO
