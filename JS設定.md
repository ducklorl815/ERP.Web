# ERP.Web JavaScript 設定文件

## 📋 文件說明
本文件記錄 ERP.Web 專案的 JavaScript 相關設定、動態效果、交互邏輯等資訊。

---

## 📚 目錄
- [登入頁面 JavaScript](#登入頁面-javascript)
- [錯誤訊息處理](#錯誤訊息處理)
- [動態 Controller 支援](#動態-controller-支援)
- [考試系統 AJAX](#考試系統-ajax)

---

# 登入頁面 JavaScript

## 錯誤訊息顯示

### 中文編碼問題解決方案

#### 問題
ASP.NET Core 預設會對 `@` 輸出的內容進行 HTML 編碼，導致中文顯示為 HTML 實體：
```
&#x5E33;&#x865F;&#x6216;&#x5BC6;&#x78BC;&#x932F;&#x8AA4;
```

#### 解決方案
使用 JSON 序列化 + jQuery `.text()` 方法：

**HTML 部分：**
```html
<!-- 空的錯誤訊息容器，由 JavaScript 填充 -->
<div class="error-message" id="errorMessage"></div>
```

**JavaScript 部分：**
```javascript
// 使用 JSON 序列化安全傳遞中文
var errorMsg = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(TempData["ErrorMessage"]?.ToString() ?? ""));
if (errorMsg && errorMsg.trim() !== '') {
    $('#errorMessage').text(errorMsg).show();
}
```

### 為什麼這樣可以解決問題？

#### 1. JSON 序列化處理
```javascript
System.Text.Json.JsonSerializer.Serialize("帳號或密碼錯誤")
// 輸出：「"帳號或密碼錯誤"」（JSON 字串格式）
```

#### 2. Html.Raw 讓 JSON 不被再次編碼
```csharp
@Html.Raw(JsonSerializer.Serialize(...))
// 確保輸出到 JavaScript 時保持為正確的 JSON 格式
```

#### 3. jQuery .text() 安全顯示
```javascript
$('#errorMessage').text(errorMsg)  // 安全地顯示純文字
```

### 安全性對比

#### ❌ 不安全的做法
```javascript
// 方案 A: 直接使用 Html.Raw（有 XSS 風險）
var errorMsg = '@Html.Raw(TempData["ErrorMessage"])';

// 方案 B: 使用 .html()（有 XSS 風險）
$('#errorMessage').html(errorMsg);
```

#### ✅ 安全的做法（已採用）
```javascript
// JSON 序列化 + .text()
var errorMsg = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(...));
$('#errorMessage').text(errorMsg);  // .text() 會自動跳脫 HTML
```

**優點：**
- ✅ 正確顯示中文
- ✅ 防止 XSS 攻擊
- ✅ 自動處理特殊字符

## 表單驗證

### 基本驗證邏輯
```javascript
$(document).ready(function() {
    $('#loginForm').on('submit', function(e) {
        var account = $('#Account').val().trim();
        var password = $('#Password').val().trim();
        
        // 清除之前的錯誤訊息
        $('#errorMessage').hide();
        
        // 基本驗證
        if (account === '') {
            showError('請輸入帳號');
            e.preventDefault();
            return false;
        }
        
        if (password === '') {
            showError('請輸入密碼');
            e.preventDefault();
            return false;
        }
        
        return true;
    });
});

function showError(message) {
    $('#errorMessage').text(message).show();
}
```

## 自動登入處理

### Cookie 檢查邏輯
```javascript
// 檢查是否有自動登入 Cookie
function checkAutoLogin() {
    var autoLoginAccount = getCookie('AutoLogin_Account');
    if (autoLoginAccount) {
        // 顯示自動登入提示
        $('#autoLoginInfo').text('將自動以 ' + autoLoginAccount + ' 登入').show();
    }
}

// 取得 Cookie 的通用函數
function getCookie(name) {
    var value = "; " + document.cookie;
    var parts = value.split("; " + name + "=");
    if (parts.length === 2) {
        return parts.pop().split(";").shift();
    }
    return null;
}
```

---

# 錯誤訊息處理

## jQuery .text() vs .html()

### 差異說明

| 方法 | 行為 | 安全性 | 使用場景 |
|------|------|--------|----------|
| `.text()` | 設定純文字，自動跳脫 HTML | ⭐⭐⭐⭐⭐ | 顯示純文字內容 |
| `.html()` | 設定 HTML，解析標籤 | ⭐⭐ | 顯示 HTML 內容（需確保安全） |

### 範例對比

#### 使用 .text()（推薦）
```javascript
var userInput = '<script>alert("XSS")</script>';
$('#message').text(userInput);
// 顯示：<script>alert("XSS")</script>（純文字，不執行）
```

#### 使用 .html()（危險）
```javascript
var userInput = '<script>alert("XSS")</script>';
$('#message').html(userInput);
// 執行：腳本會被執行！（XSS 攻擊）
```

## ASP.NET Core 自動 HTML 編碼

### 為什麼會自動編碼？
- ASP.NET Core 預設會對 `@` 輸出的內容進行 HTML 編碼
- 目的：防止 XSS（跨站腳本攻擊）
- 副作用：中文字符被編碼成 `&#x????;` 格式

### 何時使用 @Html.Raw()

**僅在以下情況使用：**
1. ✅ 輸出可信任的內容（如 JSON）
2. ✅ 配合其他安全機制（如 JSON 序列化）
3. ❌ 不直接輸出使用者輸入的內容

---

# 動態 Controller 支援

## ViewContext 動態取得 Controller 名稱

### 使用場景
在共用的 View 中，根據當前的 Controller 動態生成 URL。

### 實作方式

#### 在 View 中取得當前 Controller
```cshtml
@{
    var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
}
```

#### 動態生成 URL
```cshtml
<!-- ExamEnglish 和 ExamMath 共用相同的 View -->
<a href="@Url.Action("GenerateQuestions", ViewContext.RouteData.Values["controller"]?.ToString())">
    產生題目
</a>
```

#### JavaScript 中使用
```javascript
// 取得當前 Controller 名稱
var currentController = '@ViewContext.RouteData.Values["controller"]';

// 動態生成 AJAX URL
var ajaxUrl = '/' + currentController + '/UpdateExamWord';

$.ajax({
    url: ajaxUrl,
    type: 'POST',
    data: formData,
    success: function(response) {
        // 處理回應
    }
});
```

### 實際範例

#### 考試頁面
```cshtml
<!-- Test.cshtml -->
@{
    var controller = ViewContext.RouteData.Values["controller"]?.ToString();
}

<script>
    function updateExamWord(wordId, isCorrect) {
        var url = '@Url.Action("UpdateExamWord", ViewContext.RouteData.Values["controller"]?.ToString())';
        
        $.ajax({
            url: url,
            type: 'POST',
            data: {
                wordId: wordId,
                isCorrect: isCorrect
            },
            success: function(response) {
                if (response.success) {
                    console.log('更新成功');
                }
            }
        });
    }
</script>
```

---

# 考試系統 AJAX

## 更新考試單字狀態

### UpdateExamWord AJAX
```javascript
function updateExamWord(wordId, isCorrect) {
    var controller = '@ViewContext.RouteData.Values["controller"]';
    var url = '/' + controller + '/UpdateExamWord';
    
    $.ajax({
        url: url,
        type: 'POST',
        data: {
            wordId: wordId,
            isCorrect: isCorrect
        },
        dataType: 'json',
        success: function(response) {
            if (response.success) {
                // 更新成功，顯示提示
                showNotification('更新成功', 'success');
                
                // 更新 UI
                updateWordStatus(wordId, isCorrect);
            } else {
                showNotification('更新失敗：' + response.message, 'error');
            }
        },
        error: function(xhr, status, error) {
            showNotification('網路錯誤：' + error, 'error');
        }
    });
}

function updateWordStatus(wordId, isCorrect) {
    var $word = $('#word-' + wordId);
    
    if (isCorrect) {
        $word.addClass('correct').removeClass('incorrect');
    } else {
        $word.addClass('incorrect').removeClass('correct');
    }
}
```

## 取得考試日期清單

### GetTestDates AJAX
```javascript
function loadTestDates(kidId) {
    var controller = '@ViewContext.RouteData.Values["controller"]';
    var url = '/' + controller + '/GetTestDates';
    
    $.ajax({
        url: url,
        type: 'GET',
        data: { kidID: kidId },
        dataType: 'json',
        success: function(response) {
            if (response.success) {
                // 更新日期下拉選單
                updateDateDropdown(response.data);
            }
        }
    });
}

function updateDateDropdown(dates) {
    var $select = $('#testDateSelect');
    $select.empty();
    
    // 加入預設選項
    $select.append('<option value="">請選擇考試日期</option>');
    
    // 加入日期選項
    $.each(dates, function(index, date) {
        $select.append('<option value="' + date + '">' + date + '</option>');
    });
}
```

## 產生心算題目

### GenerateQuestions AJAX（Math 專屬）
```javascript
function generateQuestions(level, kidId) {
    var url = '@Url.Action("GenerateQuestions", "ExamMath")';
    
    $.ajax({
        url: url,
        type: 'POST',
        data: {
            level: level,
            kidID: kidId
        },
        dataType: 'json',
        beforeSend: function() {
            // 顯示載入動畫
            showLoading();
        },
        success: function(response) {
            if (response.success) {
                // 顯示題目
                displayQuestions(response.questions);
            } else {
                showNotification('產生題目失敗：' + response.message, 'error');
            }
        },
        error: function(xhr, status, error) {
            showNotification('網路錯誤：' + error, 'error');
        },
        complete: function() {
            // 隱藏載入動畫
            hideLoading();
        }
    });
}

function displayQuestions(questions) {
    var $container = $('#questionsContainer');
    $container.empty();
    
    $.each(questions, function(index, question) {
        var html = '<div class="question-item">' +
                   '<p>' + (index + 1) + '. ' + question.text + '</p>' +
                   '<input type="text" class="answer-input" data-id="' + question.id + '">' +
                   '</div>';
        $container.append(html);
    });
}
```

## 通用 AJAX 設定

### 全域 AJAX 錯誤處理
```javascript
$(document).ajaxError(function(event, xhr, settings, thrownError) {
    console.error('AJAX 錯誤:', {
        url: settings.url,
        status: xhr.status,
        error: thrownError
    });
    
    if (xhr.status === 401) {
        // 未授權，導向登入頁
        window.location.href = '/Account/Login';
    } else if (xhr.status === 403) {
        // 無權限
        showNotification('您沒有執行此操作的權限', 'error');
    } else if (xhr.status === 500) {
        // 伺服器錯誤
        showNotification('伺服器錯誤，請稍後再試', 'error');
    }
});
```

### 載入動畫
```javascript
var loadingCount = 0;

function showLoading() {
    loadingCount++;
    if (loadingCount === 1) {
        $('#loadingOverlay').fadeIn(200);
    }
}

function hideLoading() {
    loadingCount--;
    if (loadingCount === 0) {
        $('#loadingOverlay').fadeOut(200);
    }
}
```

### 通知訊息
```javascript
function showNotification(message, type) {
    type = type || 'info';  // info, success, warning, error
    
    var $notification = $('<div class="notification notification-' + type + '">' +
                         '<span class="notification-message">' + message + '</span>' +
                         '<button class="notification-close">&times;</button>' +
                         '</div>');
    
    $('#notificationContainer').append($notification);
    
    // 動畫顯示
    $notification.fadeIn(300);
    
    // 3秒後自動關閉
    setTimeout(function() {
        $notification.fadeOut(300, function() {
            $(this).remove();
        });
    }, 3000);
    
    // 點擊關閉按鈕
    $notification.find('.notification-close').on('click', function() {
        $notification.fadeOut(300, function() {
            $(this).remove();
        });
    });
}
```

---

# 表單處理

## 防止重複提交

### 方法 1：禁用按鈕
```javascript
$('#loginForm').on('submit', function(e) {
    var $submitBtn = $(this).find('button[type="submit"]');
    
    // 禁用按鈕
    $submitBtn.prop('disabled', true);
    
    // 顯示載入文字
    var originalText = $submitBtn.text();
    $submitBtn.text('登入中...');
    
    // 3秒後重新啟用（防止卡住）
    setTimeout(function() {
        $submitBtn.prop('disabled', false);
        $submitBtn.text(originalText);
    }, 3000);
});
```

### 方法 2：表單鎖定
```javascript
var formLocked = false;

$('#loginForm').on('submit', function(e) {
    if (formLocked) {
        e.preventDefault();
        return false;
    }
    
    formLocked = true;
    
    // 3秒後解鎖
    setTimeout(function() {
        formLocked = false;
    }, 3000);
});
```

## 表單驗證

### 即時驗證
```javascript
$('#Account').on('blur', function() {
    var value = $(this).val().trim();
    var $feedback = $(this).siblings('.feedback');
    
    if (value === '') {
        $(this).addClass('is-invalid');
        $feedback.text('帳號不能為空').show();
    } else {
        $(this).removeClass('is-invalid').addClass('is-valid');
        $feedback.hide();
    }
});

$('#Password').on('blur', function() {
    var value = $(this).val().trim();
    var $feedback = $(this).siblings('.feedback');
    
    if (value === '') {
        $(this).addClass('is-invalid');
        $feedback.text('密碼不能為空').show();
    } else if (value.length < 6) {
        $(this).addClass('is-invalid');
        $feedback.text('密碼長度至少 6 個字元').show();
    } else {
        $(this).removeClass('is-invalid').addClass('is-valid');
        $feedback.hide();
    }
});
```

---

# 瀏覽器相容性

## 使用的 JavaScript 功能

### ES6 功能
```javascript
// 箭頭函數
setTimeout(() => {
    console.log('Hello');
}, 1000);

// let/const
let count = 0;
const MAX_COUNT = 10;

// 模板字串
var message = `當前數量：${count}`;

// 解構賦值
var {success, message} = response;
```

### 相容性注意
- **Chrome:** 51+
- **Firefox:** 54+
- **Safari:** 10+
- **Edge:** 14+

如需支援更舊的瀏覽器，請使用 Babel 轉譯。

---

## 📝 更新記錄

| 日期 | 內容 | 負責人 |
|------|------|--------|
| 2025-10-31 | 整合所有 JavaScript 設定文件 | Cursor AI |
| 2025-10-31 | 新增錯誤訊息處理說明 | Cursor AI |
| 2025-10-31 | 新增動態 Controller 支援 | Cursor AI |
| 2025-10-31 | 新增考試系統 AJAX 範例 | Cursor AI |

---

**最後更新：** 2025年10月31日  
**版本：** 1.0  
**維護者：** ERP 開發團隊

