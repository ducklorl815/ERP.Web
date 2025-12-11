# OTP 功能整合分析報告

## 📋 目錄
1. [現有登入系統架構分析](#現有登入系統架構分析)
2. [OTP 功能需求分析](#otp-功能需求分析)
3. [技術方案建議](#技術方案建議)
4. [資料庫設計建議](#資料庫設計建議)
5. [整合點分析](#整合點分析)
6. [安全性考量](#安全性考量)
7. [實作建議](#實作建議)
8. [風險評估](#風險評估)

---

## 現有登入系統架構分析

### 1.1 現有登入流程

```
使用者輸入帳號密碼
    ↓
AccountController.Login (POST)
    ↓
AccountLoginService.LoginAsync
    ↓
驗證流程：
  1. 基本驗證（帳號密碼非空）
  2. 檢查登入失敗次數（24小時內超過5次鎖定）
  3. 驗證帳號密碼（AccountLoginRepo.ValidateUserAsync）
  4. 取得使用者資料（AccountLoginRepo.GetUserDataAsync）
  5. 檢查帳號是否啟用
  6. 清除失敗記錄
  7. 更新最後登入時間
    ↓
初始化 Session（InitializeUserSessionAsync）
    ↓
處理自動登入 Cookie（HandleAutoLoginCookie）
    ↓
導向目標頁面
```

### 1.2 現有架構組件

#### Controller 層
- **檔案位置**: `ERP.Web/Controllers/Account/AccountController.Login.cs`
- **主要方法**:
  - `Login()` - 顯示登入頁面
  - `Login(AccountLoginViewModel_param)` - 處理登入請求
  - `PerformAutoLoginAsync()` - 自動登入流程
  - `InitializeUserSessionAsync()` - 初始化 Session

#### Service 層
- **檔案位置**: `ERP.Web.Service/Service/Account/AccountLoginService.cs`
- **主要方法**:
  - `LoginAsync()` - 登入驗證邏輯
  - `GetUserDataByAccountAsync()` - 取得使用者資料

#### Repository 層
- **檔案位置**: `ERP.Web.Models/Respository/Account/AccountLoginRepo.cs`
- **主要方法**:
  - `ValidateUserAsync()` - 驗證帳號密碼
  - `GetUserDataAsync()` - 取得使用者資料
  - `UpdateLastLoginTimeAsync()` - 更新最後登入時間
  - `LogFailedLoginAsync()` - 記錄登入失敗
  - `GetFailedLoginCountAsync()` - 取得失敗次數
  - `ClearFailedLoginAsync()` - 清除失敗記錄

#### ViewModel 層
- **檔案位置**: `ERP.Web.Service/ViewModels/Account/AccountLoginViewModel.cs`
- **類別**:
  - `AccountLoginViewModel_param` - 登入請求參數
  - `AccountLoginViewModel_result` - 登入回應結果

#### View 層
- **檔案位置**: `ERP.Web/Views/Account/Login.cshtml`
- **表單欄位**:
  - Account（帳號）
  - Password（密碼）
  - AutoLogin（自動登入）

### 1.3 現有資料表結構

#### EmployeeMain（員工主檔）
- `Email` - 帳號（Email）
- `Password` - 密碼（SHA2_256 雜湊）
- `Salt` - 鹽值
- `IsActive` - 是否啟用
- `LastLoginTime` - 最後登入時間

#### EmployeeLoginFailedLog（登入失敗記錄）
- `Email` - 帳號
- `Reason` - 失敗原因
- `AttemptTime` - 嘗試時間
- `IPAddress` - IP 位址

---

## OTP 功能需求分析

### 2.1 OTP 類型選擇

#### 選項 1: TOTP (Time-based One-Time Password)
- **優點**:
  - 不需要網路連線（離線可用）
  - 使用 Google Authenticator、Microsoft Authenticator 等標準 App
  - 安全性高（基於時間同步）
  - 使用者體驗佳（不需要等待簡訊）
- **缺點**:
  - 需要使用者安裝 App
  - 時間同步問題（需處理時差）
- **適用場景**: 一般使用者、內部員工

#### 選項 2: SMS OTP
- **優點**:
  - 不需要額外 App
  - 使用者熟悉度高
  - 實作相對簡單
- **缺點**:
  - 需要簡訊服務（成本）
  - 可能被攔截（SIM 卡劫持風險）
  - 需要網路連線
- **適用場景**: 外部客戶、不常使用的帳號

#### 選項 3: Email OTP
- **優點**:
  - 不需要額外 App
  - 成本低
  - 實作簡單
- **缺點**:
  - 安全性較低（Email 可能被駭）
  - 需要網路連線
  - 可能被歸類為垃圾郵件
- **適用場景**: 備用驗證方式

#### 選項 4: 混合模式（推薦）
- **主要**: TOTP（Google Authenticator）
- **備用**: SMS OTP 或 Email OTP
- **優點**: 兼顧安全性和使用者體驗

### 2.2 OTP 觸發時機

#### 選項 1: 強制啟用（所有使用者）
- 每次登入都需要 OTP
- 安全性最高，但使用者體驗較差

#### 選項 2: 選擇性啟用（推薦）
- 使用者可選擇是否啟用 OTP
- 啟用後，每次登入都需要 OTP
- 平衡安全性和使用者體驗

#### 選項 3: 條件式觸發
- 新裝置登入時觸發
- IP 位址變更時觸發
- 異常登入行為時觸發
- 需要記錄裝置和 IP 資訊

### 2.3 OTP 驗證流程設計

```
使用者輸入帳號密碼
    ↓
驗證帳號密碼（現有流程）
    ↓
檢查是否啟用 OTP
    ↓
【如果啟用 OTP】
    ↓
顯示 OTP 輸入頁面
    ↓
使用者輸入 OTP
    ↓
驗證 OTP
    ↓
【驗證成功】
    ↓
初始化 Session（現有流程）
    ↓
導向目標頁面
```

---

## 技術方案建議

### 3.1 TOTP 實作方案

#### 套件選擇
- **推薦**: `Otp.NET` (NuGet Package)
  - 套件 ID: `Otp.NET`
  - 版本: 最新穩定版
  - 功能: 支援 TOTP 和 HOTP
  - 相容性: 與 Google Authenticator、Microsoft Authenticator 相容

#### 核心功能
```csharp
// 生成 Secret Key
var secretKey = KeyGeneration.GenerateRandomKey(20);

// 生成 QR Code URL
var qrCodeUrl = $"otpauth://totp/{issuer}:{account}?secret={secretKey}&issuer={issuer}";

// 驗證 OTP
var totp = new Totp(secretKey);
bool isValid = totp.VerifyTotp(otpCode, out timeStepMatched, new VerificationWindow(1, 1));
```

### 3.2 SMS OTP 實作方案

#### 服務商選擇
- **台灣地區推薦**:
  - Twilio（國際服務，支援多國）
  - 三竹資訊（台灣本地服務）
  - 簡訊王（台灣本地服務）

#### 實作方式
```csharp
// 生成 6 位數 OTP
var otpCode = new Random().Next(100000, 999999).ToString();

// 發送簡訊
await _smsService.SendSMSAsync(phoneNumber, $"您的驗證碼是: {otpCode}，有效期 5 分鐘");

// 儲存 OTP（含過期時間）
await _otpRepository.SaveOTPAsync(account, otpCode, DateTime.Now.AddMinutes(5));
```

### 3.3 Email OTP 實作方案

#### 使用現有 Email 服務
- 使用 ASP.NET Core 的 `IEmailSender` 介面
- 或使用第三方服務（SendGrid、Mailgun 等）

#### 實作方式
```csharp
// 生成 OTP
var otpCode = GenerateOTP();

// 發送 Email
await _emailService.SendEmailAsync(
    email, 
    "登入驗證碼", 
    $"您的驗證碼是: {otpCode}，有效期 5 分鐘"
);
```

---

## 資料庫設計建議

### 4.1 新增資料表

#### EmployeeOTPSetting（員工 OTP 設定）
```sql
CREATE TABLE erp.dbo.EmployeeOTPSetting
(
    ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EmployeeMainID UNIQUEIDENTIFIER NOT NULL,  -- 關聯到 EmployeeMain.ID
    Email NVARCHAR(255) NOT NULL,              -- 帳號（Email）
    
    -- OTP 設定
    IsOTPEnabled BIT NOT NULL DEFAULT 0,       -- 是否啟用 OTP
    OTPType NVARCHAR(20) NULL,                 -- OTP 類型：TOTP, SMS, Email
    SecretKey NVARCHAR(100) NULL,              -- TOTP Secret Key（加密儲存）
    
    -- 備用驗證方式
    BackupPhone NVARCHAR(20) NULL,             -- 備用手機號碼（SMS OTP）
    BackupEmail NVARCHAR(255) NULL,            -- 備用 Email（Email OTP）
    
    -- 系統欄位
    CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
    CreateUser NVARCHAR(100) NULL,
    ModifyDate DATETIME NULL,
    ModifyUser NVARCHAR(100) NULL,
    Enabled BIT NOT NULL DEFAULT 1,
    Deleted BIT NOT NULL DEFAULT 0,
    
    -- 索引
    CONSTRAINT FK_EmployeeOTPSetting_EmployeeMain 
        FOREIGN KEY (EmployeeMainID) REFERENCES erp.dbo.EmployeeMain(ID),
    INDEX IX_EmployeeOTPSetting_Email (Email),
    INDEX IX_EmployeeOTPSetting_EmployeeMainID (EmployeeMainID)
);
```

#### EmployeeOTPLog（OTP 驗證記錄）
```sql
CREATE TABLE erp.dbo.EmployeeOTPLog
(
    ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EmployeeMainID UNIQUEIDENTIFIER NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    
    -- OTP 資訊
    OTPType NVARCHAR(20) NOT NULL,            -- TOTP, SMS, Email
    OTPCode NVARCHAR(10) NULL,                -- OTP 碼（SMS/Email 用，驗證後可清除）
    SecretKey NVARCHAR(100) NULL,             -- TOTP Secret Key（僅記錄，不儲存完整）
    
    -- 驗證資訊
    IsVerified BIT NOT NULL DEFAULT 0,        -- 是否已驗證
    VerifiedTime DATETIME NULL,              -- 驗證時間
    ExpireTime DATETIME NOT NULL,            -- 過期時間
    
    -- 登入資訊
    IPAddress NVARCHAR(50) NULL,             -- IP 位址
    UserAgent NVARCHAR(500) NULL,            -- User Agent
    
    -- 系統欄位
    CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
    Enabled BIT NOT NULL DEFAULT 1,
    Deleted BIT NOT NULL DEFAULT 0,
    
    -- 索引
    INDEX IX_EmployeeOTPLog_Email (Email),
    INDEX IX_EmployeeOTPLog_EmployeeMainID (EmployeeMainID),
    INDEX IX_EmployeeOTPLog_CreateDate (CreateDate)
);
```

#### EmployeeOTPDevice（OTP 信任裝置）
```sql
CREATE TABLE erp.dbo.EmployeeOTPDevice
(
    ID UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    EmployeeMainID UNIQUEIDENTIFIER NOT NULL,
    Email NVARCHAR(255) NOT NULL,
    
    -- 裝置資訊
    DeviceName NVARCHAR(100) NULL,           -- 裝置名稱（使用者自訂）
    DeviceFingerprint NVARCHAR(200) NULL,     -- 裝置指紋（Browser Fingerprint）
    IPAddress NVARCHAR(50) NULL,             -- 註冊時的 IP
    UserAgent NVARCHAR(500) NULL,            -- User Agent
    
    -- 信任設定
    IsTrusted BIT NOT NULL DEFAULT 1,        -- 是否信任
    TrustUntil DATETIME NULL,                -- 信任到期時間（可選）
    
    -- 系統欄位
    CreateDate DATETIME NOT NULL DEFAULT GETDATE(),
    LastUsedDate DATETIME NULL,              -- 最後使用時間
    ModifyDate DATETIME NULL,
    Enabled BIT NOT NULL DEFAULT 1,
    Deleted BIT NOT NULL DEFAULT 0,
    
    -- 索引
    INDEX IX_EmployeeOTPDevice_Email (Email),
    INDEX IX_EmployeeOTPDevice_EmployeeMainID (EmployeeMainID)
);
```

### 4.2 資料表關聯

```
EmployeeMain (1) ──→ (N) EmployeeOTPSetting
EmployeeMain (1) ──→ (N) EmployeeOTPLog
EmployeeMain (1) ──→ (N) EmployeeOTPDevice
```

### 4.3 資料加密建議

- **SecretKey**: 使用 AES 加密儲存
- **OTPCode**: 使用雜湊值儲存（驗證後立即清除）
- **敏感資訊**: 考慮使用 SQL Server 的 Always Encrypted 功能

---

## 整合點分析

### 5.1 Controller 層整合點

#### 修改 `AccountController.Login.cs`

**新增方法**:
```csharp
/// <summary>
/// 顯示 OTP 驗證頁面
/// </summary>
[HttpGet]
public async Task<IActionResult> VerifyOTP(string account, string returnUrl)
{
    // 檢查是否啟用 OTP
    var otpSetting = await _otpService.GetOTPSettingAsync(account);
    if (otpSetting == null || !otpSetting.IsOTPEnabled)
    {
        // 未啟用 OTP，直接登入
        return RedirectToAction("Login");
    }
    
    // 顯示 OTP 輸入頁面
    ViewData["Account"] = account;
    ViewData["ReturnUrl"] = returnUrl;
    ViewData["OTPType"] = otpSetting.OTPType;
    return View();
}

/// <summary>
/// 驗證 OTP
/// </summary>
[HttpPost]
public async Task<IActionResult> VerifyOTP(OTPVerifyViewModel model)
{
    // 驗證 OTP
    var result = await _otpService.VerifyOTPAsync(model.Account, model.OTPCode);
    
    if (!result.IsSuccess)
    {
        ModelState.AddModelError("", result.ErrorMessage);
        return View();
    }
    
    // OTP 驗證成功，繼續登入流程
    var userData = await _accountLoginService.GetUserDataByAccountAsync(model.Account);
    await InitializeUserSessionAsync(userData.Account, userData.EmpName, userData.EmpID);
    
    return RedirectToTargetPage(model.ReturnUrl);
}
```

**修改現有方法**:
```csharp
[HttpPost]
public async Task<IActionResult> Login(AccountLoginViewModel_param model)
{
    // ... 現有的帳號密碼驗證 ...
    
    if (!result.IsSuccess)
    {
        // ... 現有的錯誤處理 ...
    }
    
    // 登入成功後，檢查是否啟用 OTP
    var otpSetting = await _otpService.GetOTPSettingAsync(result.Account);
    if (otpSetting != null && otpSetting.IsOTPEnabled)
    {
        // 需要 OTP 驗證，導向 OTP 驗證頁面
        // 將登入資訊暫存到 Session 或 TempData
        TempData["PendingLogin_Account"] = result.Account;
        TempData["PendingLogin_EmpID"] = result.EmpID;
        TempData["PendingLogin_EmpName"] = result.EmpName;
        TempData["PendingLogin_ReturnUrl"] = model.ReturnUrl;
        
        return RedirectToAction("VerifyOTP", new { account = result.Account, returnUrl = model.ReturnUrl });
    }
    
    // 未啟用 OTP，直接完成登入
    await InitializeUserSessionAsync(result.Account, result.EmpName, result.EmpID);
    HandleAutoLoginCookie(model.AutoLogin, result.Account);
    return RedirectToTargetPage(model.ReturnUrl);
}
```

### 5.2 Service 層整合點

#### 新增 `OTPService.cs`

**檔案位置**: `ERP.Web.Service/Service/Account/OTPService.cs`

**主要方法**:
```csharp
public class OTPService
{
    // 取得 OTP 設定
    Task<EmployeeOTPSetting?> GetOTPSettingAsync(string account);
    
    // 啟用/停用 OTP
    Task<bool> EnableOTPAsync(string account, string otpType);
    Task<bool> DisableOTPAsync(string account);
    
    // 生成 TOTP Secret Key 和 QR Code
    Task<OTPSetupResult> SetupTOTPAsync(string account);
    
    // 驗證 TOTP
    Task<OTPVerifyResult> VerifyTOTPAsync(string account, string otpCode);
    
    // 發送 SMS OTP
    Task<bool> SendSMSOTPAsync(string account, string phoneNumber);
    
    // 發送 Email OTP
    Task<bool> SendEmailOTPAsync(string account, string email);
    
    // 驗證 OTP（統一入口）
    Task<OTPVerifyResult> VerifyOTPAsync(string account, string otpCode);
    
    // 記錄 OTP 驗證
    Task<bool> LogOTPAttemptAsync(string account, string otpType, bool isSuccess, string ipAddress);
}
```

### 5.3 Repository 層整合點

#### 新增 `OTPRepository.cs`

**檔案位置**: `ERP.Web.Models/Respository/Account/OTPRepository.cs`

**主要方法**:
```csharp
public class OTPRepository
{
    // OTP 設定相關
    Task<EmployeeOTPSetting?> GetOTPSettingAsync(string account);
    Task<bool> SaveOTPSettingAsync(EmployeeOTPSetting setting);
    Task<bool> UpdateOTPSettingAsync(EmployeeOTPSetting setting);
    
    // OTP 記錄相關
    Task<bool> SaveOTPLogAsync(EmployeeOTPLog log);
    Task<EmployeeOTPLog?> GetPendingOTPAsync(string account);
    Task<bool> MarkOTPAsVerifiedAsync(Guid otpLogId);
    
    // 裝置信任相關
    Task<List<EmployeeOTPDevice>> GetTrustedDevicesAsync(string account);
    Task<bool> SaveTrustedDeviceAsync(EmployeeOTPDevice device);
    Task<bool> RemoveTrustedDeviceAsync(Guid deviceId);
}
```

### 5.4 ViewModel 層整合點

#### 新增 `OTPViewModel.cs`

**檔案位置**: `ERP.Web.Service/ViewModels/Account/OTPViewModel.cs`

**類別**:
```csharp
// OTP 驗證請求
public class OTPVerifyViewModel
{
    public string Account { get; set; }
    public string OTPCode { get; set; }
    public string ReturnUrl { get; set; }
}

// OTP 設定請求
public class OTPSetupViewModel
{
    public string Account { get; set; }
    public string OTPType { get; set; } // TOTP, SMS, Email
    public string? PhoneNumber { get; set; }
    public string? BackupEmail { get; set; }
}

// OTP 設定結果
public class OTPSetupResult
{
    public bool IsSuccess { get; set; }
    public string? SecretKey { get; set; }
    public string? QRCodeUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

// OTP 驗證結果
public class OTPVerifyResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### 5.5 View 層整合點

#### 新增 `VerifyOTP.cshtml`

**檔案位置**: `ERP.Web/Views/Account/VerifyOTP.cshtml`

**功能**:
- 顯示 OTP 輸入欄位
- 根據 OTP 類型顯示不同提示（TOTP/SMS/Email）
- 提供重新發送 OTP 功能（SMS/Email）
- 提供返回登入頁面功能

---

## 安全性考量

### 6.1 OTP 生成安全性

- **TOTP Secret Key**: 
  - 使用加密儲存（AES-256）
  - 不在日誌中記錄
  - 定期輪換（可選）

- **SMS/Email OTP**:
  - 使用隨機數生成（避免可預測）
  - 設定有效期（建議 5-10 分鐘）
  - 限制發送頻率（防止濫用）

### 6.2 OTP 驗證安全性

- **驗證次數限制**: 
  - 單一 OTP 最多驗證 3 次
  - 24 小時內最多發送 10 次 OTP

- **時間視窗**:
  - TOTP: 允許前後 1 個時間步長（30 秒）
  - SMS/Email OTP: 嚴格檢查過期時間

- **重放攻擊防護**:
  - 每個 OTP 只能使用一次
  - 驗證後立即標記為已使用

### 6.3 會話管理

- **暫存登入狀態**:
  - 使用 TempData 或加密的 Session
  - 設定過期時間（建議 10 分鐘）
  - OTP 驗證失敗後清除暫存狀態

### 6.4 日誌記錄

- **記錄內容**:
  - OTP 發送時間和方式
  - OTP 驗證嘗試（成功/失敗）
  - IP 位址和 User Agent
  - 異常行為（頻繁失敗、異常 IP）

- **敏感資訊**:
  - 不記錄完整的 OTP 碼
  - 不記錄 Secret Key
  - 僅記錄驗證結果

---

## 實作建議

### 7.1 實作階段規劃

#### 第一階段：基礎架構
1. 建立資料表（EmployeeOTPSetting, EmployeeOTPLog, EmployeeOTPDevice）
2. 建立 Repository 層（OTPRepository）
3. 建立 Service 層（OTPService）
4. 建立 ViewModel 層（OTPViewModel）

#### 第二階段：TOTP 實作
1. 整合 Otp.NET 套件
2. 實作 TOTP 生成和驗證
3. 實作 QR Code 生成
4. 建立 OTP 設定頁面

#### 第三階段：SMS/Email OTP 實作
1. 選擇並整合 SMS 服務商
2. 實作 Email 發送功能
3. 實作 OTP 發送和驗證邏輯

#### 第四階段：整合登入流程
1. 修改 AccountController.Login
2. 建立 VerifyOTP 頁面
3. 整合 OTP 驗證到登入流程

#### 第五階段：進階功能
1. 信任裝置功能
2. 條件式觸發 OTP
3. 管理介面（啟用/停用 OTP）

### 7.2 程式碼結構建議

```
ERP.Web/
├── Controllers/
│   └── Account/
│       ├── AccountController.Login.cs (修改)
│       └── AccountController.OTP.cs (新增)
│
ERP.Web.Service/
├── Service/
│   └── Account/
│       ├── AccountLoginService.cs (現有)
│       └── OTPService.cs (新增)
│
└── ViewModels/
    └── Account/
        ├── AccountLoginViewModel.cs (現有)
        └── OTPViewModel.cs (新增)
│
ERP.Web.Models/
└── Respository/
    └── Account/
        ├── AccountLoginRepo.cs (現有)
        └── OTPRepository.cs (新增)
│
ERP.Web/
└── Views/
    └── Account/
        ├── Login.cshtml (現有)
        ├── VerifyOTP.cshtml (新增)
        └── OTPSetup.cshtml (新增)
```

### 7.3 設定檔建議

#### appsettings.json
```json
{
  "OTP": {
    "Enabled": true,
    "DefaultType": "TOTP",
    "TOTP": {
      "Issuer": "ERP System",
      "TimeStep": 30,
      "Digits": 6
    },
    "SMS": {
      "Provider": "Twilio",
      "AccountSid": "",
      "AuthToken": "",
      "FromNumber": ""
    },
    "Email": {
      "FromAddress": "noreply@example.com",
      "FromName": "ERP System"
    },
    "Validation": {
      "MaxAttempts": 3,
      "ExpireMinutes": 5,
      "MaxSendsPerDay": 10
    }
  }
}
```

---

## 風險評估

### 8.1 技術風險

| 風險 | 影響 | 機率 | 緩解措施 |
|------|------|------|----------|
| TOTP 時間同步問題 | 中 | 低 | 允許前後 1 個時間步長 |
| SMS 服務商故障 | 高 | 低 | 提供 Email 備用方案 |
| Secret Key 洩露 | 高 | 低 | 加密儲存、定期輪換 |
| OTP 被暴力破解 | 中 | 中 | 限制驗證次數、記錄異常 |

### 8.2 使用者體驗風險

| 風險 | 影響 | 機率 | 緩解措施 |
|------|------|------|----------|
| 使用者忘記設定 OTP | 中 | 中 | 提供管理員重置功能 |
| 手機遺失無法登入 | 高 | 低 | 提供備用驗證方式 |
| 使用者不熟悉操作 | 中 | 中 | 提供詳細說明和教學 |

### 8.3 業務風險

| 風險 | 影響 | 機率 | 緩解措施 |
|------|------|------|----------|
| 增加登入時間 | 低 | 高 | 提供信任裝置功能 |
| 增加支援成本 | 中 | 中 | 提供自助重置功能 |
| 使用者抗拒使用 | 中 | 中 | 提供選擇性啟用 |

---

## 總結

### 建議實作方案

1. **OTP 類型**: 優先實作 TOTP（Google Authenticator），後續可擴充 SMS/Email
2. **啟用方式**: 選擇性啟用（使用者可自行決定）
3. **整合方式**: 在現有登入流程中插入 OTP 驗證步驟
4. **安全性**: 嚴格限制驗證次數、記錄所有嘗試、加密敏感資訊

### 預估工作量

- **基礎架構**: 2-3 天
- **TOTP 實作**: 3-4 天
- **SMS/Email OTP**: 2-3 天
- **整合測試**: 2-3 天
- **文件和使用者教學**: 1-2 天

**總計**: 約 10-15 個工作天

### 後續擴充建議

1. **信任裝置功能**: 減少重複驗證
2. **條件式觸發**: 新裝置或異常 IP 時才要求 OTP
3. **管理介面**: 讓管理員可以查看和重置使用者的 OTP 設定
4. **多因素認證**: 結合生物辨識、硬體 Token 等

---

**報告日期**: 2024年
**分析人員**: AI Assistant
**版本**: 1.0

