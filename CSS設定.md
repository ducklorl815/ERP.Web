# ERP.Web CSS 設定文件

## 📋 文件說明
本文件記錄 ERP.Web 專案的 CSS 樣式設定、動畫效果、響應式設計等相關資訊。

---

## 📚 目錄
- [登入頁面設計](#登入頁面設計)
- [顏色配置](#顏色配置)
- [動畫效果](#動畫效果)
- [響應式設計](#響應式設計)

---

# 登入頁面設計

## 整體布局

### 左右分割結構（桌面版）
```
┌───────────────────────────────┬──────────────────┐
│ 左側 - 白色背景              │ 右側 - 深色背景   │
├───────────────────────────────┼──────────────────┤
│                               │  FlashWolf 招牌  │
│  FlashWolf ERP 系統           │  (霓虹燈效果)    │
│  (深灰色標題)                 │──────────────────│
│                               │                  │
│  企業資源規劃整合平台         │  歡迎回來        │
│  (藍色副標題)                 │  (淡藍白色)      │
│                               │                  │
│  系統說明...                  │  [帳號輸入框]    │
│  (深灰色內文)                 │  (半透明深色)    │
│                               │                  │
│  ⚡ 智慧化資料分析            │  [密碼輸入框]    │
│  ⚡ 多平台支援                │  (半透明深色)    │
│  ⚡ 高度安全性                │                  │
│  ⚡ 客製化模組                │  □ 記住我        │
│  ⚡ 專業支援                  │                  │
│  (中灰色列表)                 │  [登入按鈕]      │
│                               │  (藍色漸層)      │
│  © 2025 FlashWolf Tech       │                  │
│  (淺灰色)                     │  © 2025          │
│                               │  (灰藍色)        │
└───────────────────────────────┴──────────────────┘
```

### 手機版布局（< 992px）
```
┌─────────────────┐
│   FlashWolf     │
│   招牌區        │
│  (40vh 高度)    │
├─────────────────┤
│                 │
│   登入表單      │
│   (自適應)      │
│                 │
└─────────────────┘
```

## CSS 類別結構

### 左側說明區
```css
.description-section {
    background: #ffffff;  /* 白色背景 */
    padding: 60px;
}

.description-title {
    color: #1a1a2e;  /* 深灰藍色 */
    font-size: 2.5rem;
    font-weight: 700;
}

.description-subtitle {
    color: #00d4ff;  /* 青藍色 */
    font-size: 1.5rem;
    margin-bottom: 30px;
}

.description-text {
    color: #555;  /* 深灰色 */
    font-size: 1.1rem;
    line-height: 1.8;
}

.feature-list {
    list-style: none;
    padding: 0;
}

.feature-item {
    color: #666;  /* 中灰色 */
    font-size: 1.1rem;
    margin: 15px 0;
}

.feature-item::before {
    content: '⚡';
    color: #00d4ff;  /* 閃電符號 */
    margin-right: 12px;
}
```

### 右側登入區
```css
.login-section {
    background: linear-gradient(180deg, #1a1a2e 0%, #0a0a0a 100%);
    width: 480px;
    display: flex;
    flex-direction: column;
}

.brand-header {
    background: linear-gradient(180deg, 
        rgba(26, 26, 46, 0.8) 0%, 
        rgba(26, 26, 46, 0.4) 100%);
    border-bottom: 2px solid rgba(0, 212, 255, 0.2);
    padding: 30px 50px 20px;
}

.login-title {
    color: #e0f7ff;  /* 淡藍白色 */
    font-size: 1.8rem;
    text-shadow: 0 0 15px rgba(0, 212, 255, 0.5);
}

.login-subtitle {
    color: #80d4ff;  /* 淺藍色 */
    font-size: 0.95rem;
}
```

## 輸入框樣式

### 基本樣式
```css
.form-control {
    background-color: rgba(26, 26, 46, 0.5);  /* 半透明深色 */
    border: 1px solid rgba(0, 212, 255, 0.3);
    color: #e0f7ff;
    padding: 12px 15px;
    border-radius: 8px;
}

.form-control::placeholder {
    color: rgba(128, 212, 255, 0.5);
}
```

### 焦點樣式
```css
.form-control:focus {
    background-color: rgba(26, 26, 46, 0.7);
    border-color: #00d4ff;
    box-shadow: 0 0 0 0.2rem rgba(0, 212, 255, 0.25);
    outline: none;
}
```

### 標籤樣式
```css
.form-label {
    color: #b3d9f0;  /* 中淺藍色 */
    font-size: 0.95rem;
    font-weight: 500;
    margin-bottom: 8px;
}
```

## 按鈕樣式

### 登入按鈕
```css
.btn-login {
    background: linear-gradient(135deg, #00d4ff 0%, #0099ff 100%);
    color: white;
    font-size: 1rem;
    font-weight: 600;
    padding: 14px 40px;
    border: none;
    border-radius: 8px;
    cursor: pointer;
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
    box-shadow: 0 4px 15px rgba(0, 212, 255, 0.4);
}

.btn-login:hover {
    transform: translateY(-3px) scale(1.01);
    box-shadow: 0 10px 30px rgba(0, 212, 255, 0.6);
}

.btn-login:active {
    transform: translateY(-1px) scale(0.99);
    transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}
```

---

# 顏色配置

## 左側說明區（白色）

| 元素 | 顏色代碼 | 說明 |
|------|---------|------|
| 背景 | `#ffffff` | 純白色 |
| 主標題 | `#1a1a2e` | 深灰藍色 |
| 副標題 | `#00d4ff` | 青藍色（品牌色） |
| 內文 | `#555` | 深灰色 |
| 功能列表 | `#666` | 中灰色 |
| 閃電符號 | `#00d4ff` | 青藍色 |

## 右側登入區（深色）

| 元素 | 顏色代碼 | 說明 |
|------|---------|------|
| 背景 | `#1a1a2e → #0a0a0a` | 深色漸層 |
| 標題 | `#e0f7ff` | 淡藍白色 |
| 副標題 | `#80d4ff` | 淺藍色 |
| 標籤 | `#b3d9f0` | 中淺藍色 |
| 輸入框文字 | `#e0f7ff` | 淡藍白色 |
| Placeholder | `rgba(128, 212, 255, 0.5)` | 半透明藍色 |
| 按鈕 | `#00d4ff → #0099ff` | 藍色漸層 |
| 錯誤訊息 | `#dc3545` | 紅色 |

## FlashWolf 霓虹燈配色

| 層次 | 顏色代碼 | 用途 |
|------|---------|------|
| 燈管本體 | `#e0f7ff` | 霓虹燈管本身 |
| 描邊 | `#00d4ff` | 燈管邊框 |
| 內層光 | `#b3e5ff, #80d4ff` | 強烈內光 |
| 主發光 | `#00d4ff` | 主要霓虹藍 |
| 外層光 | `#0099ff, #0066ff` | 擴散光暈 |

---

# 動畫效果

## 霓虹燈閃爍動畫

### 文字版本
```css
.brand-name-text {
    color: #e0f7ff;
    -webkit-text-stroke: 2px #00d4ff;
    text-shadow: 
        0 0 5px #b3e5ff,
        0 0 10px #80d4ff,
        0 0 20px #00d4ff,
        0 0 40px #00d4ff,
        0 0 60px #0099ff,
        0 0 80px #0099ff,
        0 0 120px #0066ff,
        0 0 150px #0066ff,
        0 0 200px rgba(0, 102, 255, 0.5);
    
    animation: neonFlicker 10s cubic-bezier(0.4, 0, 0.2, 1) infinite;
}

@keyframes neonFlicker {
    0%, 18%, 22%, 25%, 53%, 57%, 100% {
        color: #e0f7ff;
        text-shadow: /* 完整發光效果 */;
    }
    
    20%, 24%, 55% {
        color: #a0c8e0;
        text-shadow: /* 減弱的發光效果 */;
    }
}
```

### 圖片版本
```css
.brand-logo img {
    filter: drop-shadow(0 0 25px rgba(0, 212, 255, 0.9))
            drop-shadow(0 0 50px rgba(0, 212, 255, 0.7))
            drop-shadow(0 0 80px rgba(0, 153, 255, 0.5));
    
    animation: neonImageGlow 7s cubic-bezier(0.4, 0, 0.2, 1) infinite;
}

@keyframes neonImageGlow {
    0%, 19%, 21%, 23%, 25%, 54%, 56%, 100% {
        filter: drop-shadow(...);
        opacity: 1;
    }
    20%, 24%, 55% {
        filter: drop-shadow(...);
        opacity: 0.85;
    }
}
```

## 迷濛動畫效果

### 完整時間軸（12秒循環）
```
0%────30%────40%────50%────60%────70%────80%────85%────90%───91%─92%───95%───100%
│      │      │      │      │      │      │      │      │     │   │     │      │
清晰   保持   微模   模糊   深模   最模   開始   繼續   最暗   閃爍 暗   恢復   清晰
明亮   明亮   糊感   漸深   糊感   糊感   暗淡   暗淡   狀態         復亮   明亮
```

### 關鍵幀設定
```css
@keyframes neonDreamGlow {
    /* 清晰明亮期 (0%-30%) */
    0%, 30% {
        filter: blur(0px);
        opacity: 1;
        /* 完整光暈 */
    }
    
    /* 開始模糊 (40%) */
    40% {
        filter: blur(0.5px);
        opacity: 0.95;
        color: #d0ecff;
    }
    
    /* 模糊加深 (50%) */
    50% {
        filter: blur(1px);
        opacity: 0.9;
        color: #c0e4ff;
    }
    
    /* 迷濛深化 (60%-70%) */
    60% {
        filter: blur(1.5px);
        opacity: 0.8;
        color: #b0dcff;
    }
    
    70% {
        filter: blur(2px);  /* 最模糊 */
        opacity: 0.7;
        color: #a0d4ff;
    }
    
    /* 慢慢暗淡 (80%-85%) */
    80% {
        filter: blur(1.5px);
        opacity: 0.6;
        color: #90c8e8;
    }
    
    85% {
        filter: blur(1px);
        opacity: 0.5;
        color: #80bcdc;
    }
    
    /* 最暗狀態 (90%) */
    90% {
        filter: blur(0.5px);
        opacity: 0.4;  /* 最暗 */
        color: #70b0d0;
    }
    
    /* 快速閃爍 (91%) */
    91% {
        filter: blur(0px);
        opacity: 1;  /* 瞬間全亮 */
        color: #e0f7ff;
    }
    
    /* 稍暗 (92%) */
    92% {
        filter: blur(0.3px);
        opacity: 0.7;
        color: #a0d4ff;
    }
    
    /* 恢復明亮 (95%-100%) */
    95%, 100% {
        filter: blur(0px);
        opacity: 1;
        color: #e0f7ff;
    }
}
```

## 絲滑動畫優化

### Cubic-Bezier 緩動函數
```css
/* Material Design 標準緩動 */
cubic-bezier(0.4, 0, 0.2, 1)
```

**特點：**
- 開始時緩慢加速
- 中間快速移動
- 結束時快速減速
- 創造出「絲滑」的感覺

### 應用範例

#### 輸入框過渡
```css
.form-control {
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}
```

#### 按鈕交互
```css
.btn-login {
    transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.btn-login:active {
    transition: all 0.15s cubic-bezier(0.4, 0, 0.2, 1);
}
```

#### 背景動畫
```css
@keyframes backgroundMove {
    0% { transform: translate(0, 0); }
    50% { transform: translate(40px, 40px); }
    100% { transform: translate(0, 0); }
}

.description-section::before {
    animation: backgroundMove 30s ease-in-out infinite;
}
```

## 背景動畫

### 動態點陣背景
```css
.login-section::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-image: 
        radial-gradient(circle, rgba(0, 212, 255, 0.15) 1px, transparent 1px);
    background-size: 40px 40px;
    animation: backgroundMove 25s linear infinite;
    opacity: 0.3;
}

@keyframes backgroundMove {
    0% { transform: translate(0, 0); }
    100% { transform: translate(40px, 40px); }
}
```

### 中央光暈效果
```css
.login-section::after {
    content: '';
    position: absolute;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    width: 800px;
    height: 800px;
    background: radial-gradient(
        circle, 
        rgba(0, 212, 255, 0.15) 0%, 
        transparent 70%
    );
    pointer-events: none;
}
```

## 頁面載入動畫

### 表單淡入
```css
.login-box {
    animation: fadeInRight 1s cubic-bezier(0.4, 0, 0.2, 1);
}

@keyframes fadeInRight {
    from {
        opacity: 0;
        transform: translateX(50px) translateY(10px);
    }
    to {
        opacity: 1;
        transform: translateX(0) translateY(0);
    }
}
```

### 表單元素依序淡入
```css
.form-group {
    animation: fadeInUp 0.6s cubic-bezier(0.4, 0, 0.2, 1) backwards;
}

.form-group:nth-child(1) { animation-delay: 0.1s; }
.form-group:nth-child(2) { animation-delay: 0.2s; }
.form-group:nth-child(3) { animation-delay: 0.3s; }

@keyframes fadeInUp {
    from {
        opacity: 0;
        transform: translateY(20px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
```

## 裝飾效果

### 發光線條
```css
.decorative-line {
    width: 100%;
    height: 2px;
    background: linear-gradient(
        90deg,
        transparent 0%,
        rgba(0, 212, 255, 0.5) 50%,
        transparent 100%
    );
    animation: lineGlow 3s ease-in-out infinite;
}

@keyframes lineGlow {
    0%, 100% {
        opacity: 0.3;
        box-shadow: 0 0 10px rgba(0, 212, 255, 0.3);
    }
    50% {
        opacity: 0.8;
        box-shadow: 0 0 20px rgba(0, 212, 255, 0.8);
    }
}
```

### 狼爪圖示脈動
```css
@keyframes pulse {
    0%, 100% { 
        transform: scale(1);
        filter: drop-shadow(0 0 8px rgba(0, 212, 255, 0.6));
    }
    50% { 
        transform: scale(1.2);
        filter: drop-shadow(0 0 18px rgba(0, 212, 255, 1));
    }
}

.brand-subtitle i {
    transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.brand-subtitle i:hover {
    transform: scale(1.3) rotate(12deg);
}
```

---

# 響應式設計

## 斷點設定

### 桌面版 (> 992px)
```css
@media (min-width: 993px) {
    .login-container {
        display: flex;
        flex-direction: row;
        align-items: stretch;
    }
    
    .description-section {
        flex: 1;
        min-width: 0;
    }
    
    .login-section {
        width: 480px;
        flex-shrink: 0;
    }
}
```

### 平板/手機版 (< 992px)
```css
@media (max-width: 992px) {
    .login-container {
        display: flex;
        flex-direction: column;
    }
    
    .description-section {
        min-height: 40vh;
        padding: 40px 20px;
    }
    
    .description-title {
        font-size: 2rem;
    }
    
    .description-subtitle {
        font-size: 1.2rem;
    }
    
    .login-section {
        width: 100%;
        min-height: 60vh;
    }
    
    .brand-header {
        padding: 20px 30px 15px;
    }
    
    .brand-name-text {
        font-size: 48px;
    }
}
```

## 效能優化

### GPU 加速
```css
/* 使用 transform 和 opacity 觸發 GPU 加速 */
.btn-login {
    transform: translateZ(0);
    will-change: transform, opacity;
}
```

### 避免效能問題
```css
/* ✅ 好的做法 */
transform: translateY(-3px);
opacity: 0.5;

/* ❌ 不好的做法 */
top: -3px;  /* 會觸發重排 */
visibility: hidden;  /* 效能較差 */
```

---

## 📝 更新記錄

| 日期 | 內容 | 負責人 |
|------|------|--------|
| 2025-10-31 | 整合所有 CSS 設定文件 | Cursor AI |
| 2025-10-31 | 新增顏色對調說明 | Cursor AI |
| 2025-10-31 | 新增迷濛動畫效果 | Cursor AI |
| 2025-10-31 | 新增絲滑動畫優化 | Cursor AI |

---

**最後更新：** 2025年10月31日  
**版本：** 1.0  
**維護者：** ERP 開發團隊

