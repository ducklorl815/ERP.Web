# Microsoft.Data.SqlClient 遷移常見問題

## 📋 快速解決指南

### ⚠️ 問題：SSL 憑證信任錯誤

#### 錯誤訊息
```
Microsoft.Data.SqlClient.SqlException: 
'A connection was successfully established with the server, but then an error occurred 
during the login process. (provider: SSL Provider, error: 0 - 此憑證鏈結是由不受信任的授權單位發出的。)'
```

#### 🎯 快速解決方案

**步驟 1：** 找到連線字串設定檔（通常在 `appsettings.json` 或 `appsettings.Development.json`）

**步驟 2：** 在連線字串末尾添加 `;TrustServerCertificate=True`

**修改前：**
```json
{
  "ConnectionStrings": {
    "erp": "Data Source=SERVER;Initial Catalog=DB;User ID=user;Password=pwd"
  }
}
```

**修改後：**
```json
{
  "ConnectionStrings": {
    "erp": "Data Source=SERVER;Initial Catalog=DB;User ID=user;Password=pwd;TrustServerCertificate=True"
  }
}
```

**步驟 3：** 重新啟動應用程式

---

## 🔍 問題原因

### System.Data.SqlClient vs Microsoft.Data.SqlClient

| 項目 | System.Data.SqlClient (舊) | Microsoft.Data.SqlClient (新) |
|-----|---------------------------|------------------------------|
| SSL/TLS 加密 | 預設關閉 | **預設開啟** |
| 憑證驗證 | 不嚴格 | **嚴格驗證** |
| 自簽憑證 | 接受 | **拒絕（除非明確信任）** |
| 安全性 | 較低 | **較高** |

### 為什麼會出現這個錯誤？

1. **新套件更安全：** Microsoft.Data.SqlClient 預設啟用 SSL/TLS 加密
2. **憑證驗證：** 預設要求伺服器的 SSL 憑證必須由受信任的 CA 簽發
3. **開發環境問題：** 大多數開發環境使用自簽憑證或沒有正式憑證

---

## 💡 解決方案詳解

### 方案 1：TrustServerCertificate=True（推薦用於開發環境）

**優點：**
- ✅ 快速簡單
- ✅ 保持加密連線
- ✅ 適合開發和測試環境

**缺點：**
- ⚠️ 不驗證憑證真實性
- ⚠️ 可能受到中間人攻擊

**連線字串範例：**
```
Data Source=localhost;Initial Catalog=MyDB;User ID=sa;Password=pass123;TrustServerCertificate=True
```

**適用場景：**
- 本地開發環境
- 內部測試環境
- 使用自簽憑證的伺服器

---

### 方案 2：Encrypt=False（不推薦）

**優點：**
- ✅ 快速解決問題

**缺點：**
- ❌ 完全停用加密
- ❌ 嚴重降低安全性
- ❌ 資料以明文傳輸

**連線字串範例：**
```
Data Source=localhost;Initial Catalog=MyDB;User ID=sa;Password=pass123;Encrypt=False
```

**⚠️ 警告：** 除非絕對必要，否則不要使用此方案！

---

### 方案 3：安裝正式 SSL 憑證（推薦用於正式環境）

**步驟：**
1. 購買或申請正式的 SSL 憑證（從 DigiCert、Let's Encrypt 等）
2. 在 SQL Server 上安裝憑證
3. 配置 SQL Server 使用該憑證
4. 連線字串不需要 `TrustServerCertificate=True`

**優點：**
- ✅ 最高安全性
- ✅ 防止中間人攻擊
- ✅ 符合企業安全標準

**連線字串範例：**
```
Data Source=prod-server.company.com;Initial Catalog=MyDB;User ID=appuser;Password=SecurePass!
```

---

## 📊 環境建議

### 開發環境 (Development)
```json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;Database=DevDB;User Id=dev;Password=dev123;TrustServerCertificate=True"
  }
}
```
- ✅ 使用 `TrustServerCertificate=True`
- 設定在 `appsettings.Development.json`

### 測試環境 (Staging)
```json
{
  "ConnectionStrings": {
    "Default": "Server=test-server;Database=TestDB;User Id=testuser;Password=test123;TrustServerCertificate=True"
  }
}
```
- ⚠️ 可使用 `TrustServerCertificate=True`
- 或安裝測試用憑證

### 正式環境 (Production)
```json
{
  "ConnectionStrings": {
    "Default": "Server=prod-server.company.com;Database=ProdDB;User Id=appuser;Password=SecurePass123!;Encrypt=True"
  }
}
```
- ✅ 必須使用正式 SSL 憑證
- ❌ 不要使用 `TrustServerCertificate=True`
- ✅ 使用 `Encrypt=True` 或省略（預設為 True）

---

## 🛠️ 故障排除

### 問題 1：添加 TrustServerCertificate=True 後仍然失敗

**可能原因：**
1. 連線字串格式錯誤（檢查分號）
2. 快取問題（重新啟動應用程式）
3. 使用了錯誤的連線字串

**解決步驟：**
```bash
# 1. 停止應用程式
# 2. 清除建置
dotnet clean

# 3. 重新建置
dotnet build

# 4. 重新啟動
dotnet run
```

---

### 問題 2：正式環境無法使用 TrustServerCertificate=True

**解決方案：**
在 SQL Server 上安裝正式 SSL 憑證

**SQL Server 配置步驟：**
1. 開啟 SQL Server Configuration Manager
2. 展開 SQL Server Network Configuration
3. 右鍵點擊 Protocols for [Instance] → Properties
4. Certificate 頁籤選擇已安裝的憑證
5. Flags 頁籤設定 ForceEncryption = Yes
6. 重新啟動 SQL Server 服務

---

### 問題 3：連線字串存放在多個地方

**檢查清單：**
- [ ] `appsettings.json`
- [ ] `appsettings.Development.json`
- [ ] `appsettings.Production.json`
- [ ] `web.config`（舊專案）
- [ ] 環境變數
- [ ] Azure App Configuration
- [ ] Key Vault

**解決方案：** 在所有連線字串配置中統一添加參數

---

## 📝 連線字串參數完整說明

### 加密相關參數

| 參數 | 值 | 說明 | 建議 |
|-----|---|------|------|
| `Encrypt` | True/False | 是否啟用 SSL/TLS 加密 | 建議 True |
| `TrustServerCertificate` | True/False | 是否信任伺服器憑證 | 開發環境 True，正式環境 False |
| `Certificate` | 路徑 | 客戶端憑證路徑 | 依需求 |

### 完整連線字串範例

```csharp
// 最小配置（開發環境）
"Server=localhost;Database=MyDB;User Id=sa;Password=pass;TrustServerCertificate=True"

// 完整配置（正式環境）
"Server=prod.company.com;Database=ProdDB;User Id=appuser;Password=SecurePass!;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Application Name=MyApp"

// Windows 整合驗證
"Server=localhost;Database=MyDB;Integrated Security=True;TrustServerCertificate=True"

// 連線池配置
"Server=localhost;Database=MyDB;User Id=sa;Password=pass;TrustServerCertificate=True;Pooling=True;Min Pool Size=5;Max Pool Size=100"
```

---

## ✅ 檢查清單

更新 Microsoft.Data.SqlClient 時，請確認：

- [ ] 已將所有 `using System.Data.SqlClient;` 改為 `using Microsoft.Data.SqlClient;`
- [ ] 已更新 .csproj 檔案中的套件參考
- [ ] 已在連線字串中添加 `TrustServerCertificate=True`（開發環境）
- [ ] 已測試所有資料庫連線功能
- [ ] 已更新所有環境的連線字串配置
- [ ] 已通知團隊成員相關變更
- [ ] 已更新部署文件
- [ ] 正式環境已安裝正式 SSL 憑證（如適用）

---

## 🔗 相關資源

### 官方文件
- [Microsoft.Data.SqlClient 官方文件](https://docs.microsoft.com/en-us/sql/connect/ado-net/introduction-microsoft-data-sqlclient-namespace)
- [連線字串語法](https://docs.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection.connectionstring)
- [SQL Server 加密配置](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/enable-encrypted-connections-to-the-database-engine)

### 安全性資訊
- [System.Data.SqlClient 安全性弱點](https://github.com/advisories?query=System.Data.SqlClient)
- [TLS/SSL 最佳實踐](https://cheatsheetseries.owasp.org/cheatsheets/Transport_Layer_Protection_Cheat_Sheet.html)

---

## 📞 取得協助

如果問題仍未解決：

1. **檢查錯誤日誌：** 查看完整的例外訊息和堆疊追蹤
2. **驗證連線字串：** 使用 SQL Server Management Studio 測試連線
3. **檢查防火牆：** 確保 SQL Server 端口（預設 1433）開放
4. **聯絡團隊：** 將錯誤訊息和配置提供給開發團隊負責人

---

**文件版本：** 1.0  
**最後更新：** 2025年10月28日  
**適用於：** Microsoft.Data.SqlClient 5.1.x 及更高版本

