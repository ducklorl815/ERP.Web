/**
 * ========================================
 * ControllerSetting 共用 JavaScript 函數庫
 * ========================================
 * 
 * 功能說明：
 * 1. 模組管理：新增、刪除、選擇模組
 * 2. 表單驗證：群組名稱、模組選擇驗證
 * 3. 資料傳遞：與後端 API 溝通
 * 4. 頁面初始化：根據模式初始化不同功能
 * 
 * 使用方式：
 * - 新增模式：initControllerSetting(groupList, false)
 * - 編輯模式：initControllerSetting(groupList, true)
 * 
 * 作者：系統開發課
 * 更新日期：2024
 */

// ========================================
// 全域變數區域
// ========================================

/**
 * 群組清單資料
 * 從後端 Controller 傳入的 AccessGroupList 資料
 * 格式：[{ID: 1, GroupName: "群組名稱", GroupDesc: "群組描述"}, ...]
 */
let groupList = [];

// ========================================
// 模組管理相關函數
// ========================================

/**
 * 更新模組描述和表單欄位
 * 
 * 功能：
 * 1. 當使用者選擇模組時，顯示該模組的描述
 * 2. 編輯模式時，同時填入群組名稱和描述到表單欄位
 * 3. 選擇「請選擇模組」時，清空所有相關欄位
 * 4. 自動發送請求更新樹狀圖（新增模式）
 * 
 * @param {HTMLSelectElement} selectEl - 被選擇的 select 下拉選單元素
 * @param {boolean} isEditMode - 是否為編輯模式
 *                                  true: 編輯模式，會填入表單欄位
 *                                  false: 新增模式，只顯示描述
 * 
 * 使用場景：
 * - 新增模式：使用者選擇模組時顯示描述
 * - 編輯模式：使用者選擇模組時顯示描述並填入表單
 */
function updateModuleDesc(selectEl, isEditMode = false) {
    // 取得選擇的模組 ID
    const selectedId = selectEl.value;
    
    // 找到對應的模組行容器
    const row = selectEl.closest(".module-row");
    const descEl = row.querySelector(".module-desc");
    
    // 取得表單欄位元素
    const inputName = document.getElementById("groupName");
    const inputDesc = document.getElementById("groupDesc");
    
    // 如果沒有選擇模組（選擇了「請選擇模組」）
    if (!selectedId) {
        // 清空所有相關欄位
        descEl.textContent = "";
        if (inputName) inputName.value = "";
        if (inputDesc) inputDesc.value = "";
        
        // 編輯模式時不發送請求，避免清空樹狀圖
        if (isEditMode) return;
        
        // 新增模式時發送請求更新樹狀圖（清空樹狀圖）
        saveModules();
        return;
    }

    // 從群組清單中找到對應的群組資料
    const selectedGroup = groupList.find(g => g.ID.toString() === selectedId);

    if (selectedGroup) {
        // 顯示模組描述在模組行下方
        descEl.textContent = selectedGroup.GroupDesc || "";

        // 編輯模式時，同時填入表單欄位供使用者修改
        if (isEditMode && inputName && inputDesc) {
            inputName.value = selectedGroup.GroupName || "";
            inputDesc.value = selectedGroup.GroupDesc || "";
        }
        
        // 發送請求更新樹狀圖，顯示該模組的權限樹
        saveModules();
    } else {
        // 找不到對應群組時清空欄位
        descEl.textContent = "";
        if (inputName) inputName.value = "";
        if (inputDesc) inputDesc.value = "";
    }
}

/**
 * 儲存模組並更新樹狀圖
 * 
 * 功能：
 * 1. 收集所有已選擇的模組 ID
 * 2. 發送 AJAX 請求到後端
 * 3. 更新頁面上的樹狀圖
 * 4. 重新初始化樹狀圖事件
 * 
 * 觸發時機：
 * - 選擇模組時
 * - 新增模組行時
 * - 移除模組行時
 * 
 * 防呆機制：
 * - 沒有選擇模組時不發送請求
 * - 請求失敗時顯示錯誤訊息
 */
function saveModules() {
    const container = document.getElementById("moduleContainer");
    if (!container) return;
    
    // 收集所有已選擇的模組 ID
    const moduleIds = [...container.querySelectorAll('select')]
        .map(s => s.value)                    // 取得每個 select 的值
        .filter(v => v !== "");               // 過濾掉空值

    // 防呆：如果沒有選擇任何模組，不發送請求
    if (moduleIds.length === 0) {
        console.log("沒有選擇模組，不發送請求");
        return;
    }

    // 將模組 ID 陣列轉換成 Query String 格式
    // 例如：ModuleIDs=1,2,3
    const query = encodeURIComponent(moduleIds.join(','));
    
    // 發送 AJAX 請求到後端
    $.ajax({
        url: `/ControllerSetting/TreeView?ModuleIDs=${query}`,
        type: 'GET',
        success: function (result) {
            // 成功時更新樹狀圖 HTML
            $('#controllerTree').html(result);
            // 重新初始化樹狀圖事件（因為 HTML 內容改變了）
            initTreeEvents();
        },
        error: function (err) {
            // 失敗時顯示錯誤訊息
            console.error("送出失敗：", err);
            alert("送出失敗！");
        }
    });
}

// ========================================
// 新增模組相關函數 (僅用於 TreeView 新增模式)
// ========================================

/**
 * 點擊「加入模組」按鈕的處理函數
 * 
 * 功能：
 * 1. 檢查上一行是否已選擇模組
 * 2. 如果未選擇，提示使用者先選擇
 * 3. 如果已選擇，禁用上一行並新增下一行
 * 
 * 防呆機制：
 * - 必須先選擇上一行的模組才能新增下一行
 * - 避免新增空白的模組行
 */
function onAddModuleClick() {
    const container = document.getElementById("moduleContainer");
    if (!container) return;

    // 找到所有模組行
    const lastRows = container.querySelectorAll(".module-row");
    
    // 取得最後一行的 select 元素
    const lastSelect = lastRows.length > 0
        ? lastRows[lastRows.length - 1].querySelector("select")
        : null;

    // 防呆：如果最後一行沒有選擇模組，不允許新增
    if (lastSelect && !lastSelect.value) {
        alert("請先選擇現有模組！");
        return;
    }

    // 禁用最後一行的 select，避免重複選擇
    if (lastSelect) lastSelect.disabled = true;
    
    // 新增下一行
    addModuleRow();
}

/**
 * 新增模組行到頁面上
 * 
 * 功能：
 * 1. 動態建立新的模組行 HTML
 * 2. 過濾掉已選擇的模組，只顯示可選的模組
 * 3. 插入到「加入模組」按鈕之前
 * 4. 更新按鈕顯示狀態
 * 
 * 動態 HTML 結構：
 * - 模組選擇下拉選單
 * - 刪除按鈕
 * - 模組描述顯示區域
 */
function addModuleRow() {
    const container = document.getElementById("moduleContainer");
    if (!container) return;

    // 取得所有已選擇的模組 ID
    const selectedIds = [...container.querySelectorAll("select")]
        .map(s => s.value)
        .filter(v => v !== "");

    // 過濾出尚未選擇的模組
    const available = groupList.filter(g => !selectedIds.includes(g.ID.toString()));

    // 先發送請求更新樹狀圖
    saveModules();
    
    // 如果沒有可選的模組，不允許新增
    if (available.length === 0) {
        alert("所有模組都已加入，無法再新增！");
        return;
    }

    // 建立新的模組行 HTML
    const div = document.createElement("div");
    div.className = "module-row";
    div.innerHTML = `
        <div class="d-flex align-items-center gap-2">
            <select name="ModuleIDs" class="form-select form-select-sm module-select flex-grow-1" onchange="updateModuleDesc(this, false)">
                <option value="">請選擇模組</option>
                ${available.map(g => `<option value="${g.ID}">${g.GroupName}</option>`).join("")}
            </select>
            <span class="del-btn" style="cursor:pointer; display:inline;" onclick="removeModuleRow(this)">
                <i class="fa-solid fa-xmark"></i>
            </span>
        </div>
        <div class="module-desc text-secondary small mt-1 ms-1"></div>
    `;

    // 插入到「加入模組」按鈕之前
    const addBtnContainer = container.querySelector(".add-btn").closest(".d-flex");
    container.insertBefore(div, addBtnContainer);

    // 更新按鈕顯示狀態
    updateAddButtonState();
}

/**
 * 移除模組行
 * 
 * 功能：
 * 1. 刪除指定的模組行
 * 2. 更新「加入模組」按鈕的顯示狀態
 * 3. 重新計算可選的模組清單
 * 
 * @param {HTMLElement} el - 被點擊的刪除按鈕元素
 */
function removeModuleRow(el) {
    // 找到並刪除該模組行
    el.closest(".module-row").remove();
    
    // 更新按鈕狀態
    updateAddButtonState();
}

/**
 * 更新「加入模組」按鈕的顯示狀態
 * 
 * 功能：
 * 1. 計算還有多少模組可以選擇
 * 2. 如果沒有可選模組，隱藏按鈕
 * 3. 如果還有可選模組，顯示按鈕
 * 
 * 觸發時機：
 * - 新增模組行後
 * - 移除模組行後
 * - 選擇模組後
 */
function updateAddButtonState() {
    const container = document.getElementById("moduleContainer");
    if (!container) return;
    
    // 取得所有已選擇的模組 ID
    const selects = container.querySelectorAll("select");
    const selectedIds = [...selects].map(s => s.value).filter(v => v !== "");
    
    // 計算可選的模組數量
    const available = groupList.filter(g => !selectedIds.includes(g.ID.toString()));

    // 更新按鈕顯示狀態
    const addBtn = container.querySelector(".add-btn");
    if (addBtn) {
        addBtn.style.display = available.length === 0 ? "none" : "inline";
    }
}

// ========================================
// 表單驗證相關函數
// ========================================

/**
 * 驗證群組表單
 * 
 * 功能：
 * 1. 檢查群組名稱是否已填寫
 * 2. 編輯模式時檢查是否已選擇模組
 * 3. 驗證失敗時顯示錯誤訊息並聚焦到對應欄位
 * 
 * @param {boolean} isEditMode - 是否為編輯模式
 * @returns {boolean} 驗證是否通過
 *                    true: 驗證通過，可以提交
 *                    false: 驗證失敗，不能提交
 * 
 * 驗證規則：
 * - 群組名稱：必填
 * - 編輯模式：必須選擇至少一個模組
 */
function validateGroupForm(isEditMode = false) {
    const inputName = document.getElementById("groupName");
    const inputDesc = document.getElementById("groupDesc");
    
    // 檢查群組名稱是否已填寫
    if (!inputName || !inputName.value.trim()) {
        alert("請輸入群組名稱！");
        if (inputName) inputName.focus();
        return false;
    }

    // 編輯模式時，額外檢查是否已選擇模組
    if (isEditMode) {
        const container = document.getElementById("moduleContainer");
        const modules = [...container.querySelectorAll('.module-row')]
            .map(row => {
                const select = row.querySelector('select');
                return select ? select.value : "";
            })
            .filter(id => id !== "");

        if (modules.length === 0) {
            alert("請先選擇要修改的模組！");
            return false;
        }
    }

    return true;
}

// ========================================
// 頁面初始化函數
// ========================================

/**
 * 初始化 ControllerSetting 頁面
 * 
 * 功能：
 * 1. 設定全域群組清單資料
 * 2. 初始化樹狀結構事件
 * 3. 根據模式執行不同的初始化邏輯
 * 
 * @param {Array} accessGroupList - 從後端傳入的群組清單資料
 * @param {boolean} isEditMode - 是否為編輯模式
 *                                  true: 編輯模式，會初始化預設選擇
 *                                  false: 新增模式，只初始化基本功能
 * 
 * 使用方式：
 * - 新增頁面：initControllerSetting(groupList, false)
 * - 編輯頁面：initControllerSetting(groupList, true)
 */
function initControllerSetting(accessGroupList, isEditMode = false) {
    // 設定全域群組清單
    groupList = accessGroupList || [];
    
    // 初始化樹狀結構事件（展開/收合、勾選等）
    initTreeEvents();
    
    // 編輯模式時，初始化預設選擇的模組
    if (isEditMode) {
        initEditMode();
    }
}

/**
 * 初始化編輯模式
 * 
 * 功能：
 * 1. 從隱藏欄位取得預設選擇的模組 ID
 * 2. 設定下拉選單的預設值
 * 3. 觸發 change 事件，載入對應的模組資料
 * 
 * 資料來源：
 * - 從 View 中的 data-current-node-id 屬性取得
 * - 格式：逗號分隔的模組 ID 字串，如 "1,2,3"
 */
function initEditMode() {
    // 從隱藏欄位取得預設選擇的模組 ID
    const selectedIds = document.querySelector('[data-current-node-id]')?.dataset.currentNodeId?.split(',').filter(id => id) || [];
    
    const container = document.getElementById("moduleContainer");
    const selects = container?.querySelectorAll("select");

    // 如果有預設選擇的模組，設定為選中狀態
    if (selectedIds.length > 0 && selects && selects[0]) {
        selects[0].value = selectedIds[0];
        // 觸發 change 事件，載入模組資料並更新表單
        selects[0].dispatchEvent(new Event("change"));
    }
}

// ========================================
// 全域函數別名 (向後相容)
// ========================================

/**
 * 為了保持向後相容性，提供舊的函數名稱別名
 * 這樣可以避免修改現有的 HTML 中的 onclick 事件
 */

// updateDesc 別名，預設為編輯模式
window.updateDesc = function(selectEl) {
    updateModuleDesc(selectEl, true);
};

// onAddBtnClick 別名
window.onAddBtnClick = onAddModuleClick;

// removeRow 別名
window.removeRow = removeModuleRow;