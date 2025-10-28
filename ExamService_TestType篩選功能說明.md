# ExamService TestType 篩選功能說明

## 📅 修改日期
2025年10月28日

---

## 📋 修改概述

在 `ExamService.cs` 中加入 TestType 篩選邏輯，確保：
- **ExamEnglishController** 只顯示英文課程清單
- **ExamMathController** 只顯示數學課程清單

---

## 🎯 問題說明

### 修改前的問題

在 `GetNewTestAsync` 和 `GetReTestAsync` 方法中，課程清單（ClassNameList）會同時包含英文和數學的所有課程，沒有根據 TestType 進行篩選。

**結果：**
- ExamEnglishController 會看到數學課程
- ExamMathController 會看到英文課程
- 使用者體驗混亂

---

## ✅ 解決方案

### 修改位置

在兩個方法中加入 TestType 篩選邏輯：

1. **GetReTestAsync** 方法（第 68-71 行）
2. **PublicTaskAsync** 方法（第 455-459 行）

---

## 📝 修改內容

### 1. GetReTestAsync 方法

**修改位置：** `ERP.Web.Service/Service/ExamService.cs` 第 39-122 行

**加入的程式碼：**
```csharp
// 根據 TestType 篩選課程清單
if (!string.IsNullOrEmpty(param.TestType))
{
    ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
}
```

**插入位置：** 在取得 `ExamList` 之後，排序邏輯之前

**完整流程：**
```csharp
// 第 65 行：取得所有課程清單
var ExamList = await _examRepo.GetExamListAsync();

// 第 67-71 行：根據 TestType 篩選（新增）
if (!string.IsNullOrEmpty(param.TestType))
{
    ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
}

// 第 73+ 行：進行排序和處理
```

---

### 2. PublicTaskAsync 方法

**修改位置：** `ERP.Web.Service/Service/ExamService.cs` 第 448-508 行

**加入的程式碼：**
```csharp
// 根據 TestType 篩選課程清單
if (!string.IsNullOrEmpty(param.TestType))
{
    ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
}
```

**插入位置：** 在取得 `ExamList` 之後，排序邏輯之前

**完整流程：**
```csharp
// 第 453 行：取得所有課程清單
List<ExamListModel> ExamList = await _examRepo.GetExamListAsync();

// 第 455-459 行：根據 TestType 篩選（新增）
if (!string.IsNullOrEmpty(param.TestType))
{
    ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
}

// 第 461+ 行：進行排序和處理
```

---

## 🔍 篩選邏輯說明

### 條件檢查
```csharp
if (!string.IsNullOrEmpty(param.TestType))
```
- 確保 `param.TestType` 不是 null 或空字串
- 如果沒有指定 TestType，則不進行篩選（保持原有行為）

### LINQ 查詢
```csharp
ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
```
- 使用 `Where` 篩選符合條件的課程
- 使用 `Equals` 方法進行比對
- `StringComparison.OrdinalIgnoreCase` - 不區分大小寫
- `.ToList()` - 立即執行查詢，產生新的清單

---

## 📊 實際運作流程

### ExamEnglishController 呼叫流程

```
1. ExamEnglishController.NewTest()
   ↓
   設定 param.TestType = "English"
   ↓
2. ExamService.GetNewTestAsync(param)
   ↓
   呼叫 PublicTaskAsync(result, param)
   ↓
3. PublicTaskAsync()
   ↓
   取得所有課程：ExamList = await _examRepo.GetExamListAsync()
   ↓
   篩選英文課程：ExamList = ExamList.Where(x => x.TestType == "English")
   ↓
   排序和處理
   ↓
4. 返回結果（只包含英文課程）
```

### ExamMathController 呼叫流程

```
1. ExamMathController.NewTest()
   ↓
   設定 param.TestType = "Math"
   ↓
2. ExamService.GetNewTestAsync(param)
   ↓
   呼叫 PublicTaskAsync(result, param)
   ↓
3. PublicTaskAsync()
   ↓
   取得所有課程：ExamList = await _examRepo.GetExamListAsync()
   ↓
   篩選數學課程：ExamList = ExamList.Where(x => x.TestType == "Math")
   ↓
   排序和處理
   ↓
4. 返回結果（只包含數學課程）
```

---

## 🧪 測試建議

### 測試案例 1：英文課程清單

**測試步驟：**
1. 登入系統
2. 進入 `/ExamEnglish/NewTest`
3. 選擇「選擇考卷」下拉選單

**預期結果：**
- ✅ 只顯示英文課程（如 "English Unit 1", "English Homework" 等）
- ❌ 不顯示數學課程（如 "MentalMath10" 等）

---

### 測試案例 2：數學課程清單

**測試步驟：**
1. 登入系統
2. 進入 `/ExamMath/NewTest`
3. 選擇「選擇考卷」下拉選單

**預期結果：**
- ✅ 只顯示數學課程（如 "MentalMath10", "MentalMath9" 等）
- ❌ 不顯示英文課程

---

### 測試案例 3：複習測驗頁面

**測試步驟：**
1. 進入 `/ExamEnglish/ReTest`
2. 選擇「選擇考卷」下拉選單

**預期結果：**
- ✅ 只顯示英文課程

---

## 💡 技術細節

### 為什麼使用 StringComparison.OrdinalIgnoreCase？

```csharp
x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)
```

**原因：**
1. **不區分大小寫：** "English" 和 "english" 都會被視為相同
2. **避免文化差異：** 使用 Ordinal 比對，不受當前文化影響
3. **效能較好：** Ordinal 比對比 CurrentCulture 快

**替代方案比較：**
```csharp
// 方案 1：使用 == 運算子（區分大小寫）
x.TestType == param.TestType  // ❌ "English" != "english"

// 方案 2：使用 ToLower()（不建議）
x.TestType.ToLower() == param.TestType.ToLower()  // ⚠️ 效能較差，會產生新字串

// 方案 3：使用 Equals + OrdinalIgnoreCase（推薦）
x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)  // ✅ 最佳方案
```

---

### 為什麼在這裡篩選而不是在 Repository？

**優點：**
1. **彈性高：** Service 層可以根據業務邏輯決定是否篩選
2. **向後相容：** Repository 層保持原有功能，不影響其他使用者
3. **易於測試：** 篩選邏輯在 Service 層，容易單獨測試
4. **可重複使用：** Repository 提供完整資料，Service 層可靈活運用

**缺點：**
- ⚠️ 從資料庫撈取更多資料（但課程數量不多，影響有限）

**如果課程數量很大（>1000）：**
- 可以考慮在 Repository 層加入 TestType 參數
- 直接在 SQL 查詢中篩選，減少資料傳輸量

---

## 📊 資料流程圖

```
Controller 層
    ↓ (設定 TestType)
ExamService
    ↓ (呼叫 Repository)
ExamRespo.GetExamListAsync()
    ↓ (從資料庫取得所有課程)
SQL Server - Lession Table
    ↓ (返回所有課程)
ExamService
    ↓ (根據 TestType 篩選)
    ↓ (排序處理)
    ↓ (返回結果)
Controller 層
    ↓ (傳遞給 View)
View 層（顯示課程選單）
```

---

## 🔧 維護建議

### 1. 如果要新增其他考試類型

**步驟：**
1. 建立新的 Controller（如 `ExamListeningController`）
2. 設定 `TestType = "Listening"`
3. Service 層的篩選邏輯會自動生效
4. 在資料庫 `Lession` 表中新增 TestType = "Listening" 的課程

**範例：**
```csharp
public class ExamListeningController : Controller
{
    private const string TestType = "Listening";
    
    public async Task<IActionResult> NewTest(ExamSearchListViewModel_param param)
    {
        param.TestType = TestType;  // 設定為 Listening
        var result = await _examService.GetNewTestAsync(param);
        return View("~/Views/Exam/NewTest.cshtml", result);
    }
}
```

---

### 2. 如果要改用 Repository 層篩選

**修改 Repository：**
```csharp
public async Task<List<ExamListModel>> GetExamListAsync(string testType = null)
{
    var sql = @"
        SELECT ClassName, LessionSort, TestType, CreateDate
        FROM KidsWorld.dbo.Lession
        WHERE Enabled = 1 AND Deleted = 0
    ";
    
    // 如果有指定 TestType，加入篩選條件
    if (!string.IsNullOrEmpty(testType))
    {
        sql += " AND TestType = @TestType";
        sqlparam.Add("TestType", testType);
    }
    
    sql += " ORDER BY LessionSort DESC";
    
    // ... 執行查詢
}
```

**修改 Service：**
```csharp
// 直接傳入 TestType 參數
List<ExamListModel> ExamList = await _examRepo.GetExamListAsync(param.TestType);

// 不需要在這裡篩選了
// if (!string.IsNullOrEmpty(param.TestType)) { ... }  // 移除這段
```

---

## ✅ 編譯狀態

**建置結果：** ✅ 成功

```
Build succeeded.
    20 Warning(s)  (都是現有的警告)
    0 Error(s)
Time Elapsed 00:00:03.89
```

**說明：**
- 所有警告都是現有的 null reference warnings
- 沒有因本次修改而產生的新錯誤
- 專案可以正常編譯執行

---

## 📝 修改檔案清單

### 修改的檔案
1. ✅ `ERP.Web.Service/Service/ExamService.cs`
   - `GetReTestAsync` 方法（第 67-71 行）- 加入 TestType 篩選
   - `PublicTaskAsync` 方法（第 455-459 行）- 加入 TestType 篩選

### 保持不變的檔案
- `ExamEnglishController.cs` - 已經設定 TestType = "English"
- `ExamMathController.cs` - 已經設定 TestType = "Math"
- `ExamRespo.cs` - Repository 層不變
- 所有 View 檔案 - 不需修改

---

## 🎯 效果總結

### 修改前
```
ExamEnglishController → Service → 所有課程（English + Math）
ExamMathController    → Service → 所有課程（English + Math）
```

### 修改後
```
ExamEnglishController → Service → 只有英文課程（English）
ExamMathController    → Service → 只有數學課程（Math）
```

---

## 📞 技術支援

如有任何問題或需要進一步協助，請參考：
- `.cursorrules` - 專案開發規範
- `ExamController整理報告.md` - Controller 整理說明

---

**文件版本：** 1.0  
**最後更新：** 2025年10月28日  
**作者：** Cursor AI Assistant

