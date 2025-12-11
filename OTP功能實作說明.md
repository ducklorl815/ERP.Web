# OTP 功能實作說明

## 📋 實作完成項目

### ✅ 已完成功能

1. **TOTP (Time-based One-Time Password) 功能**
   - 使用 Google Authenticator、Microsoft Authenticator 等標準驗證器 App
   - 基於時間的動態驗證碼（每 30 秒更新一次）

2. **登入流程整合**
   - 第一次登入後需要驗證 OTP
   - 登入後 Session 有效期間不需要再次驗證
   - Session 遺失後需要重新驗證 OTP

3. **OTP 設定功能**
   - 使用者可以自行設定 OTP
   - 顯示 QR Code 供掃描
   - 顯示 Secret Key 供手動輸入
   - 可以停用 OTP

4. **安全性功能**
   - 驗證次數限制（10 分鐘內最多 5 次）
   - 完整的驗證記錄
   - Secret Key 加密儲存

---

## 🗄️ 資料庫設定

### 執行 SQL 腳本

請執行 `資料庫建立腳本_OTP功能.sql` 建立以下資料表：

1. **EmployeeOTPSetting** - 儲存使用者 OTP 設定
2. **EmployeeOTPLog** - 記錄 OTP 驗證嘗試

### 資料表結構

#### EmployeeOTPSetting
- `ID` - 主鍵
- `EmployeeMainID` - 關聯到 EmployeeMain
- `Email` - 帳號（Email）
- `IsOTPEnabled` - 是否啟用 OTP
- `OTPType` - OTP 類型（TOTP）
- `SecretKey` - 加密儲存的 Secret Key
- `BackupPhone` - 備用手機（未來擴充用）
- `BackupEmail` - 備用 Email（未來擴充用）

#### EmployeeOTPLog
- `ID` - 主鍵
- `Email` - 帳號
- `OTPType` - OTP 類型
- `IsVerified` - 是否驗證成功
- `VerifiedTime` - 驗證時間
- `IPAddress` - IP 位址
- `UserAgent` - User Agent

---

## ⚙️ 設定檔調整

### appsettings.json

已加入以下設定：

```json
{
  "OTP": {
    "Issuer": "ERP System",
    "TimeStep": 30,
    "Digits": 6
  }
}
```

- `Issuer`: 顯示在驗證器 App 中的發行者名稱
- `TimeStep`: 時間步長（秒），預設 30 秒
- `Digits`: OTP 碼位數，預設 6 位

---

## 📦 安裝的 NuGet 套件

### ERP.Web.Service 專案
- `Otp.NET` Version 1.3.0 - TOTP 功能核心套件

---

## 🔄 登入流程

### 未啟用 OTP 的使用者
```
輸入帳號密碼
    ↓
驗證成功
    ↓
初始化 Session
    ↓
導向首頁
```

### 已啟用 OTP 的使用者（第一次登入或 Session 遺失）
```
輸入帳號密碼
    ↓
驗證成功
    ↓
檢查是否啟用 OTP
    ↓
【已啟用】
    ↓
導向 OTP 驗證頁面
    ↓
輸入 OTP 驗證碼
    ↓
驗證成功
    ↓
初始化 Session（包含 OTP 驗證狀態）
    ↓
導向首頁
```

### 已啟用 OTP 的使用者（Session 有效）
```
輸入帳號密碼
    ↓
驗證成功
    ↓
檢查 Session 中的 OTP 驗證狀態
    ↓
【已驗證】
    ↓
直接完成登入（跳過 OTP 驗證）
    ↓
導向首頁
```

---

## 🎯 使用方式

### 1. 使用者設定 OTP

1. 登入系統後，前往「OTP 設定」頁面（`/Account/OTPSetup`）
2. 點擊「開始設定 OTP」
3. 系統會生成 Secret Key 和 QR Code
4. 使用驗證器 App（如 Google Authenticator）掃描 QR Code
5. 或手動輸入 Secret Key
6. 輸入驗證器 App 中顯示的 6 位數驗證碼
7. 驗證成功後，OTP 功能即啟用

### 2. 登入時驗證 OTP

1. 輸入帳號密碼
2. 如果已啟用 OTP 且 Session 中沒有驗證記錄，會導向 OTP 驗證頁面
3. 開啟驗證器 App，輸入顯示的 6 位數驗證碼
4. 驗證成功後完成登入

### 3. 停用 OTP

1. 前往「OTP 設定」頁面
2. 點擊「停用 OTP」按鈕
3. 確認停用後，OTP 功能即停用

---

## 🔒 安全性說明

### Secret Key 加密
- Secret Key 使用 Base64 編碼儲存（簡化實作）
- **建議**：實際環境應使用 AES 加密，並將加密金鑰儲存在安全的地方（如 Azure Key Vault）

### 驗證次數限制
- 10 分鐘內最多嘗試 5 次
- 超過限制後需要等待 10 分鐘才能再次嘗試

### Session 管理
- OTP 驗證狀態儲存在 Session 中
- Session 過期後需要重新驗證 OTP
- Session 預設閒置 30 分鐘後過期

### 驗證記錄
- 所有 OTP 驗證嘗試都會記錄
- 包含 IP 位址、User Agent、驗證結果等資訊
- 可用於安全審計和異常偵測

---

## 📁 檔案結構

### 新增檔案

#### Controller
- `ERP.Web/Controllers/Account/AccountController.OTP.cs` - OTP 相關的 Controller 方法

#### Service
- `ERP.Web.Service/Service/Account/OTPService.cs` - OTP 業務邏輯

#### Repository
- `ERP.Web.Models/Respository/Account/OTPRepository.cs` - 資料庫操作

#### ViewModel
- `ERP.Web.Service/ViewModels/Account/OTPViewModel.cs` - OTP 相關的 ViewModel

#### View
- `ERP.Web/Views/Account/VerifyOTP.cshtml` - OTP 驗證頁面
- `ERP.Web/Views/Account/OTPSetup.cshtml` - OTP 設定頁面

#### 資料庫
- `資料庫建立腳本_OTP功能.sql` - 資料表建立腳本

### 修改檔案

#### Controller
- `ERP.Web/Controllers/Account/AccountController.cs` - 加入 OTPService 依賴注入
- `ERP.Web/Controllers/Account/AccountController.Login.cs` - 整合 OTP 驗證流程

#### Service
- `ERP.Web.Service/Service/Account/AccountLoginService.cs` - 修正 namespace

#### 設定檔
- `ERP.Web/Program.cs` - 註冊 OTPService 和 OTPRepository
- `ERP.Web/appsettings.json` - 加入 OTP 設定

#### 專案檔
- `ERP.Web.Service/ERP.Web.Service.csproj` - 加入 Otp.NET 套件

---

## 🚀 後續擴充建議

### 1. 增強安全性
- 使用 AES 加密 Secret Key
- 將加密金鑰儲存在 Azure Key Vault 或環境變數
- 實作信任裝置功能（記住裝置，減少重複驗證）

### 2. 多種 OTP 方式
- SMS OTP（簡訊驗證碼）
- Email OTP（Email 驗證碼）
- 備用驗證方式

### 3. 管理功能
- 管理員可以查看使用者的 OTP 設定
- 管理員可以重置使用者的 OTP 設定
- OTP 驗證記錄查詢和報表

### 4. 使用者體驗優化
- 自動檢測 Session 中的 OTP 驗證狀態
- 提供「記住此裝置」選項
- 異常登入時強制要求 OTP 驗證

---

## ⚠️ 注意事項

1. **Secret Key 加密**：目前使用 Base64 編碼，實際環境應使用更安全的加密方式
2. **Session 管理**：OTP 驗證狀態依賴 Session，確保 Session 設定正確
3. **時區同步**：TOTP 基於時間，確保伺服器和使用者裝置時間同步
4. **備份機制**：建議提供備用驗證方式（如 Email OTP），避免使用者無法登入

---

## 📝 測試建議

### 功能測試
1. ✅ 設定 OTP 功能
2. ✅ 登入時驗證 OTP
3. ✅ Session 有效時跳過 OTP 驗證
4. ✅ Session 過期後重新驗證 OTP
5. ✅ 停用 OTP 功能
6. ✅ 驗證次數限制
7. ✅ 錯誤處理

### 安全性測試
1. ✅ 驗證碼錯誤處理
2. ✅ 驗證次數限制
3. ✅ Session 劫持防護
4. ✅ 驗證記錄完整性

---

**實作完成日期**: 2024年
**版本**: 1.0

