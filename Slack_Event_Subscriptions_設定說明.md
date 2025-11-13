# Slack Event Subscriptions 設定說明

## 問題診斷

如果測試端點 `/slack/test/notification` 可以正常推送，但實際 Slack 訊息沒有推送，通常是 Slack App 的 Event Subscriptions 設定問題。

## 需要檢查的設定

### 1. Event Subscriptions 是否啟用

1. 前往 [Slack API 網站](https://api.slack.com/apps)
2. 選擇您的 App
3. 在左側選單選擇 **"Event Subscriptions"**
4. 確認 **"Enable Events"** 已開啟（Toggle 為 ON）

### 2. Request URL 設定

在 Event Subscriptions 頁面中：

- **Request URL** 應該設定為：
  ```
  https://localhost:44372/slack/events
  ```
  或
  ```
  https://YOUR_DOMAIN/slack/events
  ```

- 如果使用本地開發環境（localhost），需要使用 **ngrok** 或類似工具將本地服務暴露到公網：
  ```
  ngrok http 44372
  ```
  然後將 ngrok 提供的 HTTPS URL 設定到 Request URL

- Slack 會自動驗證 URL，應該顯示 **"Verified"** 綠色標記

### 3. Subscribe to bot events（Bot 事件訂閱）

在 Event Subscriptions 頁面下方，找到 **"Subscribe to bot events"** 區塊：

**必須訂閱的事件：**
- ✅ `message.channels` - 頻道訊息（如果需要頻道訊息通知）
- ✅ `message.groups` - 私人群組訊息
- ✅ `message.im` - 直接訊息（私訊）**← 目前系統主要處理這個**
- ✅ `message.mpim` - 多人直接訊息

**注意：** 目前系統只處理直接訊息（`message.im`），如果您需要頻道訊息通知，需要：
1. 訂閱 `message.channels` 事件
2. 修改後端程式碼以支援頻道訊息

### 4. Subscribe to events on behalf of users（使用者事件訂閱）

如果需要接收使用者相關的事件，可以在 **"Subscribe to events on behalf of users"** 區塊中訂閱。

### 5. Signing Secret 設定

確認 **"Signing Secret"** 已正確設定在 `appsettings.json` 中：

```json
{
  "Slack": {
    "SigningSecret": "您的_Signing_Secret"
  }
}
```

Signing Secret 可以在 App 的 **"Basic Information"** → **"App Credentials"** 中找到。

## 測試步驟

### 步驟 1：驗證 URL 是否正確

1. 在 Slack App 設定中，點擊 **"Save Changes"**
2. Slack 會自動發送驗證請求到您的 URL
3. 檢查後端日誌，應該看到：
   ```
   Slack URL 驗證成功，回應 challenge
   ```
4. 如果驗證失敗，檢查：
   - URL 是否可訪問（使用 ngrok 如果是本地開發）
   - Signing Secret 是否正確
   - 後端是否正確回應 challenge

### 步驟 2：測試事件接收

1. 在 Slack 中發送一條**直接訊息（DM）**給 Bot 或另一個使用者
2. 檢查後端日誌，應該看到：
   ```
   收到 Slack 事件：Type=message Subtype=(無) Channel=D... User=U... Text=...
   訊息類型判斷：IsDirectMessage=True Channel=D... ChannelType=im
   已透過 SignalR 推送 Slack 訊息通知給特定使用者：...
   ```
3. 如果沒有看到日誌，檢查：
   - Event Subscriptions 是否已啟用
   - 是否訂閱了 `message.im` 事件
   - Bot 是否已加入相關的 Workspace

### 步驟 3：檢查前端是否收到

1. 開啟瀏覽器開發者工具（F12）
2. 在 Console 中應該看到：
   ```
   🔔 SignalR 收到訊息事件：...
   ✅ 已顯示 Slack 訊息通知
   ```
3. 應該會顯示 Toastr 通知

## 常見問題

### Q1: 為什麼頻道訊息沒有推送？

**A:** 目前系統只處理直接訊息（DM）。如果需要頻道訊息通知：
1. 在 Event Subscriptions 中訂閱 `message.channels` 事件
2. 修改後端程式碼，移除 `isDirectMessage` 的檢查

### Q2: 本地開發環境如何接收 Slack 事件？

**A:** 使用 ngrok 將本地服務暴露到公網：
```bash
ngrok http 44372
```
然後將 ngrok 提供的 HTTPS URL 設定到 Slack App 的 Request URL。

### Q3: 如何確認 Slack 是否有發送事件？

**A:** 檢查後端日誌：
- 如果看到 "收到 Slack 事件"，表示 Slack 有發送事件
- 如果沒有看到任何日誌，可能是 Event Subscriptions 未正確設定

### Q4: 簽章驗證失敗怎麼辦？

**A:** 檢查：
1. Signing Secret 是否正確設定
2. `appsettings.json` 中的 `SigningSecret` 是否與 Slack App 中的一致
3. 後端日誌中的錯誤訊息

## 後端日誌範例

### 成功接收事件的日誌：
```
收到 Slack 事件：Type=message Subtype=(無) Channel=D01234567 User=U088LJ3SXN1 Text=測試訊息
訊息類型判斷：IsDirectMessage=True Channel=D01234567 ChannelType=im
查詢頻道資訊：Channel=D01234567 IsIm=True User=U088LJ3SXN1 Sender=U088LJ3SXN1 Recipient=U088LJ3SXN1
已透過 SignalR 推送 Slack 訊息通知給特定使用者：Channel=D01234567 Sender=U088LJ3SXN1 Recipient=U088LJ3SXN1
```

### 跳過處理的日誌（頻道訊息）：
```
收到 Slack 事件：Type=message Subtype=(無) Channel=C01234567 User=U088LJ3SXN1 Text=頻道訊息
訊息類型判斷：IsDirectMessage=False Channel=C01234567 ChannelType=(無)
跳過處理的訊息事件：Type=message Subtype=(無) Channel=C01234567 IsDirectMessage=False
```

## 下一步

1. 檢查 Slack App 的 Event Subscriptions 設定
2. 確認已訂閱 `message.im` 事件
3. 測試發送直接訊息
4. 檢查後端日誌確認是否有收到事件
5. 如果沒有收到事件，檢查 URL 驗證是否成功

