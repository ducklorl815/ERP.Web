-- ========================================
-- OTP 功能資料表建立腳本
-- ========================================
-- 建立時間：2024年
-- 說明：用於實作 TOTP (Time-based One-Time Password) 功能
-- ========================================

USE [erp]
GO

-- ========================================
-- 1. EmployeeOTPSetting（員工 OTP 設定表）
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeOTPSetting]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeOTPSetting]
    (
        [ID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [EmployeeMainID] UNIQUEIDENTIFIER NOT NULL,
        [Email] NVARCHAR(255) NOT NULL,
        
        -- OTP 設定
        [IsOTPEnabled] BIT NOT NULL DEFAULT 0,
        [OTPType] NVARCHAR(20) NULL,              -- TOTP, SMS, Email
        [SecretKey] NVARCHAR(500) NULL,            -- 加密儲存的 Secret Key
        
        -- 備用驗證方式（未來擴充用）
        [BackupPhone] NVARCHAR(20) NULL,
        [BackupEmail] NVARCHAR(255) NULL,
        
        -- 系統欄位
        [CreateDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [CreateUser] NVARCHAR(100) NULL,
        [ModifyDate] DATETIME NULL,
        [ModifyUser] NVARCHAR(100) NULL,
        [Enabled] BIT NOT NULL DEFAULT 1,
        [Deleted] BIT NOT NULL DEFAULT 0
    )
    
    -- 建立索引
    CREATE INDEX [IX_EmployeeOTPSetting_Email] ON [dbo].[EmployeeOTPSetting]([Email])
    CREATE INDEX [IX_EmployeeOTPSetting_EmployeeMainID] ON [dbo].[EmployeeOTPSetting]([EmployeeMainID])
    CREATE INDEX [IX_EmployeeOTPSetting_IsOTPEnabled] ON [dbo].[EmployeeOTPSetting]([IsOTPEnabled])
    
    PRINT 'EmployeeOTPSetting 資料表建立完成'
END
ELSE
BEGIN
    PRINT 'EmployeeOTPSetting 資料表已存在'
END
GO

-- ========================================
-- 2. EmployeeOTPLog（OTP 驗證記錄表）
-- ========================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeOTPLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeOTPLog]
    (
        [ID] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
        [Email] NVARCHAR(255) NOT NULL,
        
        -- OTP 資訊
        [OTPType] NVARCHAR(20) NOT NULL,          -- TOTP, SMS, Email
        
        -- 驗證資訊
        [IsVerified] BIT NOT NULL DEFAULT 0,
        [VerifiedTime] DATETIME NULL,
        
        -- 登入資訊
        [IPAddress] NVARCHAR(50) NULL,
        [UserAgent] NVARCHAR(500) NULL,
        
        -- 系統欄位
        [CreateDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [Enabled] BIT NOT NULL DEFAULT 1,
        [Deleted] BIT NOT NULL DEFAULT 0
    )
    
    -- 建立索引
    CREATE INDEX [IX_EmployeeOTPLog_Email] ON [dbo].[EmployeeOTPLog]([Email])
    CREATE INDEX [IX_EmployeeOTPLog_CreateDate] ON [dbo].[EmployeeOTPLog]([CreateDate])
    CREATE INDEX [IX_EmployeeOTPLog_IsVerified] ON [dbo].[EmployeeOTPLog]([IsVerified])
    
    PRINT 'EmployeeOTPLog 資料表建立完成'
END
ELSE
BEGIN
    PRINT 'EmployeeOTPLog 資料表已存在'
END
GO

-- ========================================
-- 3. 建立外鍵約束（可選，如果需要）
-- ========================================
-- 注意：如果 EmployeeMain 表的 ID 欄位類型是 UNIQUEIDENTIFIER，可以建立外鍵
-- 如果類型不同，則不建立外鍵，僅在應用層面維護關聯性

-- IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeMain]') AND type in (N'U'))
-- BEGIN
--     IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_EmployeeOTPSetting_EmployeeMain')
--     BEGIN
--         ALTER TABLE [dbo].[EmployeeOTPSetting]
--         ADD CONSTRAINT [FK_EmployeeOTPSetting_EmployeeMain]
--         FOREIGN KEY ([EmployeeMainID]) REFERENCES [dbo].[EmployeeMain]([ID])
--         
--         PRINT '外鍵約束建立完成'
--     END
-- END
-- GO

PRINT '========================================'
PRINT 'OTP 功能資料表建立完成'
PRINT '========================================'

