-- Seed Test Data for Earn & Learn (Allowance Tracker)
-- Run this script against the Azure SQL Server to create test accounts

-- Configuration
DECLARE @ParentEmail NVARCHAR(256) = 'demo@earnandlearn.app';
DECLARE @ParentPassword NVARCHAR(MAX) = 'Demo123!'; -- Note: This will need to be hashed by the API
DECLARE @FamilyName NVARCHAR(100) = 'Demo Family';

-- Generate consistent GUIDs for relationships
DECLARE @FamilyId UNIQUEIDENTIFIER = NEWID();
DECLARE @ParentUserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Child1UserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Child2UserId UNIQUEIDENTIFIER = NEWID();
DECLARE @Child1Id UNIQUEIDENTIFIER = NEWID();
DECLARE @Child2Id UNIQUEIDENTIFIER = NEWID();
DECLARE @SystemUserId UNIQUEIDENTIFIER = '00000000-0000-0000-0000-000000000001';

PRINT 'Creating test data...';
PRINT 'Family ID: ' + CAST(@FamilyId AS NVARCHAR(50));
PRINT 'Parent User ID: ' + CAST(@ParentUserId AS NVARCHAR(50));

-- Check if demo user already exists
IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = @ParentEmail)
BEGIN
    PRINT 'Demo user already exists. Skipping seed.';
    RETURN;
END

BEGIN TRANSACTION;

BEGIN TRY
    -- 1. Create the Family first (without OwnerId to avoid circular dependency)
    INSERT INTO Families (Id, Name, CreatedAt, OwnerId)
    VALUES (@FamilyId, @FamilyName, GETUTCDATE(), @ParentUserId);
    PRINT 'Created family: ' + @FamilyName;

    -- 2. Create Parent User
    -- Note: Password hash is for 'Demo123!' using ASP.NET Core Identity default hasher
    -- You may need to use the API to register instead for proper password hashing
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        FirstName, LastName, Role, FamilyId
    )
    VALUES (
        @ParentUserId,
        @ParentEmail,
        UPPER(@ParentEmail),
        @ParentEmail,
        UPPER(@ParentEmail),
        1, -- EmailConfirmed
        'AQAAAAIAAYagAAAAEFakeHashForDemoAccountPleaseRegisterViaAPI==', -- Placeholder - register via API
        NEWID(), -- SecurityStamp
        NEWID(), -- ConcurrencyStamp
        0, -- PhoneNumberConfirmed
        0, -- TwoFactorEnabled
        1, -- LockoutEnabled
        0, -- AccessFailedCount
        'Demo',
        'Parent',
        0, -- Parent role
        @FamilyId
    );
    PRINT 'Created parent user: ' + @ParentEmail;

    -- 3. Create Child 1 User (Emma, age 10)
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        FirstName, LastName, Role, FamilyId
    )
    VALUES (
        @Child1UserId,
        'emma.demo@earnandlearn.app',
        UPPER('emma.demo@earnandlearn.app'),
        'emma.demo@earnandlearn.app',
        UPPER('emma.demo@earnandlearn.app'),
        1,
        'AQAAAAIAAYagAAAAEFakeHashForDemoAccountPleaseRegisterViaAPI==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0,
        'Emma',
        'Demo',
        1, -- Child role
        @FamilyId
    );
    PRINT 'Created child user: Emma';

    -- 4. Create Child 2 User (Jack, age 8)
    INSERT INTO AspNetUsers (
        Id, UserName, NormalizedUserName, Email, NormalizedEmail,
        EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp,
        PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount,
        FirstName, LastName, Role, FamilyId
    )
    VALUES (
        @Child2UserId,
        'jack.demo@earnandlearn.app',
        UPPER('jack.demo@earnandlearn.app'),
        'jack.demo@earnandlearn.app',
        UPPER('jack.demo@earnandlearn.app'),
        1,
        'AQAAAAIAAYagAAAAEFakeHashForDemoAccountPleaseRegisterViaAPI==',
        NEWID(),
        NEWID(),
        0, 0, 1, 0,
        'Jack',
        'Demo',
        1, -- Child role
        @FamilyId
    );
    PRINT 'Created child user: Jack';

    -- 5. Create Child 1 Profile (Emma - $15/week allowance)
    INSERT INTO Children (
        Id, UserId, FamilyId, WeeklyAllowance, CurrentBalance,
        LastAllowanceDate, AllowanceDay, AllowancePaused,
        SavingsAccountEnabled, SavingsBalance, SavingsTransferType,
        SavingsTransferAmount, SavingsTransferPercentage, SavingsBalanceVisibleToChild,
        AllowDebt, CreatedAt
    )
    VALUES (
        @Child1Id,
        @Child1UserId,
        @FamilyId,
        15.00, -- Weekly allowance
        47.50, -- Current balance (will be updated by transactions)
        DATEADD(DAY, -3, GETUTCDATE()), -- Last allowance 3 days ago
        6, -- Saturday (DayOfWeek.Saturday)
        0, -- Not paused
        1, -- Savings enabled
        25.00, -- Savings balance
        2, -- Percentage transfer type
        0, -- Fixed amount (not used)
        20, -- 20% to savings
        1, -- Savings visible to child
        0, -- No debt allowed
        DATEADD(MONTH, -3, GETUTCDATE()) -- Created 3 months ago
    );
    PRINT 'Created child profile: Emma ($15/week, savings enabled)';

    -- 6. Create Child 2 Profile (Jack - $10/week allowance)
    INSERT INTO Children (
        Id, UserId, FamilyId, WeeklyAllowance, CurrentBalance,
        LastAllowanceDate, AllowanceDay, AllowancePaused,
        SavingsAccountEnabled, SavingsBalance, SavingsTransferType,
        SavingsTransferAmount, SavingsTransferPercentage, SavingsBalanceVisibleToChild,
        AllowDebt, CreatedAt
    )
    VALUES (
        @Child2Id,
        @Child2UserId,
        @FamilyId,
        10.00, -- Weekly allowance
        23.75, -- Current balance
        DATEADD(DAY, -3, GETUTCDATE()), -- Last allowance 3 days ago
        6, -- Saturday
        0, -- Not paused
        0, -- Savings not enabled
        0, -- No savings balance
        0, -- None
        0, 0, 1, 0,
        DATEADD(MONTH, -3, GETUTCDATE())
    );
    PRINT 'Created child profile: Jack ($10/week)';

    -- 7. Create Transactions for Emma (past 3 months of activity)
    DECLARE @TxDate DATETIME;
    DECLARE @Balance DECIMAL(10,2) = 0;

    -- Week 1 (12 weeks ago)
    SET @TxDate = DATEADD(WEEK, -12, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    -- Spending in week 1
    SET @Balance = @Balance - 5.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 5.99, 1, 14, 'Ice cream at the mall', @Balance, @ParentUserId, DATEADD(DAY, 2, @TxDate));

    -- Week 2
    SET @TxDate = DATEADD(WEEK, -11, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 12.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 12.99, 1, 12, 'Harry Potter book', @Balance, @ParentUserId, DATEADD(DAY, 3, @TxDate));

    -- Week 3
    SET @TxDate = DATEADD(WEEK, -10, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance + 20.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 20.00, 0, 3, 'Birthday money from Grandma', @Balance, @ParentUserId, DATEADD(DAY, 1, @TxDate));

    SET @Balance = @Balance - 8.50;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 8.50, 1, 10, 'LEGO minifigures', @Balance, @ParentUserId, DATEADD(DAY, 4, @TxDate));

    -- Week 4
    SET @TxDate = DATEADD(WEEK, -9, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    -- Week 5
    SET @TxDate = DATEADD(WEEK, -8, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 4.25;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 4.25, 1, 15, 'Candy from the store', @Balance, @ParentUserId, DATEADD(DAY, 2, @TxDate));

    SET @Balance = @Balance + 5.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 5.00, 0, 2, 'Helped wash the car', @Balance, @ParentUserId, DATEADD(DAY, 5, @TxDate));

    -- Week 6
    SET @TxDate = DATEADD(WEEK, -7, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 19.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 19.99, 1, 11, 'Minecraft game', @Balance, @ParentUserId, DATEADD(DAY, 3, @TxDate));

    -- Week 7
    SET @TxDate = DATEADD(WEEK, -6, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 6.50;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 6.50, 1, 17, 'Movie snacks', @Balance, @ParentUserId, DATEADD(DAY, 6, @TxDate));

    -- Week 8
    SET @TxDate = DATEADD(WEEK, -5, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 3.75;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 3.75, 1, 14, 'Smoothie', @Balance, @ParentUserId, DATEADD(DAY, 4, @TxDate));

    -- Week 9
    SET @TxDate = DATEADD(WEEK, -4, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 10.00, 0, 2, 'Mowed the lawn', @Balance, @ParentUserId, DATEADD(DAY, 2, @TxDate));

    SET @Balance = @Balance - 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 1, 19, 'Art supplies', @Balance, @ParentUserId, DATEADD(DAY, 5, @TxDate));

    -- Week 10
    SET @TxDate = DATEADD(WEEK, -3, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 7.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 7.99, 1, 12, 'Comic book', @Balance, @ParentUserId, DATEADD(DAY, 3, @TxDate));

    -- Week 11
    SET @TxDate = DATEADD(WEEK, -2, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 11.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 11.00, 1, 10, 'Stuffed animal', @Balance, @ParentUserId, DATEADD(DAY, 4, @TxDate));

    -- Week 12 (last week)
    SET @TxDate = DATEADD(WEEK, -1, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 5.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 5.00, 1, 31, 'Donated to animal shelter', @Balance, @ParentUserId, DATEADD(DAY, 2, @TxDate));

    -- This week's allowance
    SET @TxDate = DATEADD(DAY, -3, GETUTCDATE());
    SET @Balance = @Balance + 15.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 15.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    -- Recent spending
    SET @Balance = @Balance - 4.50;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child1Id, 4.50, 1, 14, 'Hot chocolate', @Balance, @ParentUserId, DATEADD(DAY, -1, GETUTCDATE()));

    -- Update Emma's balance to match transactions
    UPDATE Children SET CurrentBalance = @Balance WHERE Id = @Child1Id;
    PRINT 'Emma final balance: $' + CAST(@Balance AS NVARCHAR(20));

    -- 8. Create Transactions for Jack (simpler history)
    SET @Balance = 0;

    -- Week 1 (8 weeks ago)
    SET @TxDate = DATEADD(WEEK, -8, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 7.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 7.99, 1, 10, 'Hot Wheels cars', @Balance, @ParentUserId, DATEADD(DAY, 3, @TxDate));

    -- Week 2
    SET @TxDate = DATEADD(WEEK, -7, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 3.50;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 3.50, 1, 15, 'Candy', @Balance, @ParentUserId, DATEADD(DAY, 2, @TxDate));

    -- Week 3
    SET @TxDate = DATEADD(WEEK, -6, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance + 5.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 5.00, 0, 2, 'Cleaned his room', @Balance, @ParentUserId, DATEADD(DAY, 4, @TxDate));

    SET @Balance = @Balance - 9.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 9.99, 1, 11, 'Roblox Robux', @Balance, @ParentUserId, DATEADD(DAY, 5, @TxDate));

    -- Week 4
    SET @TxDate = DATEADD(WEEK, -5, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    -- Week 5
    SET @TxDate = DATEADD(WEEK, -4, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 6.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 6.00, 1, 18, 'Basketball cards', @Balance, @ParentUserId, DATEADD(DAY, 3, @TxDate));

    -- Week 6
    SET @TxDate = DATEADD(WEEK, -3, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 3, 'Tooth fairy', @Balance, @ParentUserId, DATEADD(DAY, 1, @TxDate));

    SET @Balance = @Balance - 8.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 8.00, 1, 14, 'Pizza with friends', @Balance, @ParentUserId, DATEADD(DAY, 5, @TxDate));

    -- Week 7
    SET @TxDate = DATEADD(WEEK, -2, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 4.99;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 4.99, 1, 10, 'Nerf darts', @Balance, @ParentUserId, DATEADD(DAY, 4, @TxDate));

    -- Week 8 (last week)
    SET @TxDate = DATEADD(WEEK, -1, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    -- This week's allowance
    SET @TxDate = DATEADD(DAY, -3, GETUTCDATE());
    SET @Balance = @Balance + 10.00;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 10.00, 0, 1, 'Weekly allowance', @Balance, @SystemUserId, @TxDate);

    SET @Balance = @Balance - 3.25;
    INSERT INTO Transactions (Id, ChildId, Amount, Type, Category, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES (NEWID(), @Child2Id, 3.25, 1, 15, 'Gummy bears', @Balance, @ParentUserId, DATEADD(DAY, -2, GETUTCDATE()));

    -- Update Jack's balance
    UPDATE Children SET CurrentBalance = @Balance WHERE Id = @Child2Id;
    PRINT 'Jack final balance: $' + CAST(@Balance AS NVARCHAR(20));

    -- 9. Create some wish list items
    INSERT INTO WishListItems (Id, ChildId, Name, Price, Notes, IsPurchased, CreatedAt)
    VALUES
        (NEWID(), @Child1Id, 'Nintendo Switch Game', 59.99, 'Pokemon Scarlet', 0, DATEADD(WEEK, -2, GETUTCDATE())),
        (NEWID(), @Child1Id, 'Art Set', 24.99, 'The one with 100 colors', 0, DATEADD(WEEK, -1, GETUTCDATE())),
        (NEWID(), @Child1Id, 'Headphones', 29.99, NULL, 0, DATEADD(DAY, -3, GETUTCDATE())),
        (NEWID(), @Child2Id, 'LEGO Star Wars Set', 49.99, 'Millennium Falcon', 0, DATEADD(WEEK, -3, GETUTCDATE())),
        (NEWID(), @Child2Id, 'Soccer Ball', 19.99, NULL, 0, DATEADD(WEEK, -1, GETUTCDATE()));

    PRINT 'Created wish list items';

    -- 10. Create some savings transactions for Emma
    INSERT INTO SavingsTransactions (Id, ChildId, Amount, Type, Description, BalanceAfter, CreatedById, CreatedAt)
    VALUES
        (NEWID(), @Child1Id, 10.00, 0, 'Initial savings deposit', 10.00, @ParentUserId, DATEADD(MONTH, -2, GETUTCDATE())),
        (NEWID(), @Child1Id, 5.00, 0, 'Birthday money to savings', 15.00, @ParentUserId, DATEADD(MONTH, -1, GETUTCDATE())),
        (NEWID(), @Child1Id, 10.00, 0, 'Saved from allowance', 25.00, @ParentUserId, DATEADD(WEEK, -2, GETUTCDATE()));

    PRINT 'Created savings transactions for Emma';

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '========================================';
    PRINT 'Test data created successfully!';
    PRINT '========================================';
    PRINT '';
    PRINT 'Demo Account Credentials:';
    PRINT '  Email: demo@earnandlearn.app';
    PRINT '  Password: (Register via API - password hash is placeholder)';
    PRINT '';
    PRINT 'To create the account properly:';
    PRINT '1. Use the /api/v1/auth/register endpoint';
    PRINT '2. Or run the C# seed script below';
    PRINT '';
    PRINT 'Children:';
    PRINT '  Emma (10 years old) - $15/week, savings enabled';
    PRINT '  Jack (8 years old) - $10/week';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error occurred: ' + ERROR_MESSAGE();
    THROW;
END CATCH
