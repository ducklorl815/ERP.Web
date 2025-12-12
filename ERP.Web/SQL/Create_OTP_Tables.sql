-- =============================================
-- OTP 相關資料表建立腳本
-- 資料庫：erp
-- 建立日期：2025-01-XX
-- 說明：用於 Google Authenticator TOTP 雙因素驗證
-- =============================================

USE erp;
GO

-- =============================================
-- 1. EmployeeOTPSetting - OTP 設定資料表
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeOTPSetting]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeOTPSetting]
    (
        [ID]                UNIQUEIDENTIFIER    NOT NULL,
        [EmployeeMainID]    UNIQUEIDENTIFIER    NOT NULL,
        [Email]             NVARCHAR(255)      NOT NULL,
        [IsOTPEnabled]      BIT                 NOT NULL    DEFAULT 0,
        [OTPType]           NVARCHAR(50)       NULL        DEFAULT 'TOTP',
        [SecretKey]         NVARCHAR(MAX)       NULL,       -- 加密後的 Secret Key
        [BackupPhone]       NVARCHAR(50)        NULL,
        [BackupEmail]       NVARCHAR(255)       NULL,
        [CreateDate]        DATETIME            NOT NULL    DEFAULT GETDATE(),
        [CreateUser]        NVARCHAR(100)       NULL,
        [ModifyDate]        DATETIME            NULL,
        [ModifyUser]        NVARCHAR(100)       NULL,
        [Enabled]           BIT                 NOT NULL    DEFAULT 1,
        [Deleted]           BIT                 NOT NULL    DEFAULT 0,
        
        CONSTRAINT [PK_EmployeeOTPSetting] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
    
    -- 建立索引
    CREATE NONCLUSTERED INDEX [IX_EmployeeOTPSetting_Email] 
        ON [dbo].[EmployeeOTPSetting] ([Email] ASC)
        WHERE [Deleted] = 0 AND [Enabled] = 1;
    
    CREATE NONCLUSTERED INDEX [IX_EmployeeOTPSetting_EmployeeMainID] 
        ON [dbo].[EmployeeOTPSetting] ([EmployeeMainID] ASC)
        WHERE [Deleted] = 0;
    
    -- 新增註解
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'員工 OTP 雙因素驗證設定資料表', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'主鍵 ID', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'ID';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'員工主檔 ID（關聯到 EmployeeMain）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'EmployeeMainID';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'員工 Email（帳號）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'Email';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'是否已啟用 OTP（0=未啟用, 1=已啟用）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'IsOTPEnabled';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'OTP 類型（TOTP=時間型, SMS=簡訊, Email=郵件）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'OTPType';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'加密後的 Secret Key（Base64 編碼）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'SecretKey';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'備用手機號碼', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'BackupPhone';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'備用 Email', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPSetting',
        @level2type = N'COLUMN', @level2name = N'BackupEmail';
    
    PRINT '資料表 EmployeeOTPSetting 建立成功';
END
ELSE
BEGIN
    PRINT '資料表 EmployeeOTPSetting 已存在';
END
GO

-- =============================================
-- 2. EmployeeOTPLog - OTP 驗證記錄資料表
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[EmployeeOTPLog]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[EmployeeOTPLog]
    (
        [ID]            UNIQUEIDENTIFIER    NOT NULL    DEFAULT NEWID(),
        [Email]         NVARCHAR(255)       NOT NULL,
        [OTPType]       NVARCHAR(50)        NOT NULL    DEFAULT 'TOTP',
        [IsVerified]    BIT                 NOT NULL    DEFAULT 0,
        [VerifiedTime]  DATETIME            NULL,
        [IPAddress]     NVARCHAR(50)        NOT NULL    DEFAULT 'Unknown',
        [UserAgent]     NVARCHAR(500)       NULL,
        [CreateDate]    DATETIME            NOT NULL    DEFAULT GETDATE(),
        [Enabled]       BIT                 NOT NULL    DEFAULT 1,
        [Deleted]       BIT                 NOT NULL    DEFAULT 0,
        
        CONSTRAINT [PK_EmployeeOTPLog] PRIMARY KEY CLUSTERED ([ID] ASC)
    );
    
    -- 建立索引
    CREATE NONCLUSTERED INDEX [IX_EmployeeOTPLog_Email_CreateDate] 
        ON [dbo].[EmployeeOTPLog] ([Email] ASC, [CreateDate] DESC)
        WHERE [Deleted] = 0 AND [Enabled] = 1;
    
    CREATE NONCLUSTERED INDEX [IX_EmployeeOTPLog_CreateDate] 
        ON [dbo].[EmployeeOTPLog] ([CreateDate] DESC)
        WHERE [Deleted] = 0;
    
    -- 新增註解
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'員工 OTP 驗證記錄資料表（用於記錄驗證嘗試和安全性稽核）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'主鍵 ID', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'ID';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'員工 Email（帳號）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'Email';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'OTP 類型（TOTP=時間型, SMS=簡訊, Email=郵件）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'OTPType';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'是否驗證成功（0=失敗, 1=成功）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'IsVerified';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'驗證成功時間（僅在 IsVerified=1 時有值）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'VerifiedTime';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'驗證時的 IP 位址', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'IPAddress';
    
    EXEC sys.sp_addextendedproperty 
        @name = N'MS_Description', 
        @value = N'驗證時的 User Agent（瀏覽器資訊）', 
        @level0type = N'SCHEMA', @level0name = N'dbo', 
        @level1type = N'TABLE', @level1name = N'EmployeeOTPLog',
        @level2type = N'COLUMN', @level2name = N'UserAgent';
    
    PRINT '資料表 EmployeeOTPLog 建立成功';
END
ELSE
BEGIN
    PRINT '資料表 EmployeeOTPLog 已存在';
END
GO

-- =============================================
-- 欄位型態說明
-- =============================================
/*
EmployeeOTPSetting 資料表欄位型態：

ID              UNIQUEIDENTIFIER    - GUID 主鍵
EmployeeMainID  UNIQUEIDENTIFIER    - 員工主檔 ID（關聯到 EmployeeMain）
Email           NVARCHAR(255)      - 員工 Email（帳號）
IsOTPEnabled    BIT                 - 是否已啟用 OTP（0/1）
OTPType         NVARCHAR(50)       - OTP 類型（TOTP/SMS/Email）
SecretKey       NVARCHAR(MAX)       - 加密後的 Secret Key（Base64）
BackupPhone     NVARCHAR(50)       - 備用手機號碼（可選）
BackupEmail     NVARCHAR(255)      - 備用 Email（可選）
CreateDate      DATETIME            - 建立時間
CreateUser      NVARCHAR(100)       - 建立者
ModifyDate      DATETIME            - 修改時間（可選）
ModifyUser      NVARCHAR(100)       - 修改者
Enabled         BIT                 - 是否啟用（0/1）
Deleted         BIT                 - 是否刪除（0/1，軟刪除）

EmployeeOTPLog 資料表欄位型態：

ID              UNIQUEIDENTIFIER    - GUID 主鍵
Email           NVARCHAR(255)      - 員工 Email（帳號）
OTPType         NVARCHAR(50)       - OTP 類型（TOTP/SMS/Email）
IsVerified      BIT                 - 是否驗證成功（0/1）
VerifiedTime    DATETIME            - 驗證成功時間（可選）
IPAddress       NVARCHAR(50)       - 驗證時的 IP 位址
UserAgent       NVARCHAR(500)      - 驗證時的 User Agent（可選）
CreateDate      DATETIME            - 建立時間
Enabled         BIT                 - 是否啟用（0/1）
Deleted         BIT                 - 是否刪除（0/1，軟刪除）
*/
GO

PRINT '所有 OTP 相關資料表建立完成！';
GO

