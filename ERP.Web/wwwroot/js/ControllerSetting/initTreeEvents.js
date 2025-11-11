/**
 * ========================================
 * 樹狀結構事件處理 JavaScript
 * ========================================
 * 
 * 功能說明：
 * 1. 樹狀節點的展開/收合功能
 * 2. Checkbox 的父子聯動（勾選父節點時子節點跟隨）
 * 3. 父節點的半勾狀態管理（部分子節點勾選時顯示半勾）
 * 4. 自動展開已勾選的節點
 * 5. 表單提交時的資料收集
 * 
 * 使用場景：
 * - Controller / Action 權限樹狀圖
 * - 模組權限選擇
 * - 群組權限設定
 * 
 * 作者：系統開發課
 * 更新日期：2024
 */

// ========================================
// 頁面載入時自動初始化
// ========================================

/**
 * 頁面載入完成後自動初始化樹狀結構事件
 * 確保 DOM 元素都已載入後才執行
 */
document.addEventListener("DOMContentLoaded", function () {
    initTreeEvents();
});

/**
 * 初始化樹狀結構的所有事件處理
 * 
 * 功能：
 * 1. 設定展開/收合按鈕的點擊事件
 * 2. 設定 Checkbox 的勾選事件
 * 3. 初始化樹狀結構的預設狀態
 * 4. 設定表單提交事件
 * 
 * 執行順序：
 * 1. 設定事件監聽器
 * 2. 初始化預設狀態
 * 3. 設定表單提交
 */
function initTreeEvents() {
    // 取得樹狀結構容器
    const tree = document.getElementById("controllerTree");
    if (!tree) return;

    // ========================================
    // 展開/收合功能設定
    // ========================================

    /**
     * 採用事件委派處理展開/收合，確保動態載入內容也能生效
     */
    // 移除舊的監聽器（避免重複綁定）
    const oldHandler = tree._toggleHandler;
    if (oldHandler) {
        tree.removeEventListener("click", oldHandler);
    }

    // 定義新的處理器
    const toggleHandler = function (e) {
        // 忽略勾選框點擊
        if (e.target.closest('.tree-checkbox')) return;

        // 允許點擊箭頭、圖示或節點名稱進行展開/收合
        const handle = e.target.closest('.toggle-icon, .node-icon, .node-name');
        if (!handle || !tree.contains(handle)) return;
        
        console.log('Toggle clicked:', handle.className, e.target);
        e.stopPropagation();

        const li = handle.closest('.tree-node');
        if (!li) return;

        // 僅對有子清單的節點可收合
        const hasChildren = !!li.querySelector(':scope > ul.tree-list');
        if (!hasChildren) return;

        // 切換展開/收合狀態
        const willExpand = li.classList.contains('collapsed');
        li.classList.toggle('collapsed');
        li.classList.toggle('expanded');


    };

    // 綁定新的監聽器
    tree.addEventListener("click", toggleHandler);
    tree._toggleHandler = toggleHandler;

    /**
     * 初始化所有節點為收合狀態
     * 
     * 功能：
     * - 預設所有有子節點的節點都收合
     * - 提供更好的使用者體驗，避免一次顯示太多內容
     */
    tree.querySelectorAll(".tree-node").forEach(li => {
        // 僅對有子清單者設定預設為收合
        const hasChildren = !!li.querySelector(":scope > ul.tree-list");
        if (hasChildren && !li.classList.contains("expanded")) {
            li.classList.add("collapsed");
            li.classList.remove("expanded");
        }
    });

    // ========================================
    // Checkbox 父子聯動功能
    // ========================================

    /**
     * 設定所有 Checkbox 的勾選事件
     * 
     * 功能：
     * 1. 勾選父節點時，所有子節點自動勾選
     * 2. 勾選父節點時，自動展開整棵樹
     * 3. 更新父節點的半勾狀態
     * 
     * 聯動邏輯：
     * - 父節點勾選 → 所有子節點勾選
     * - 父節點取消勾選 → 所有子節點取消勾選
     * - 子節點狀態改變 → 更新父節點的半勾狀態
     */
    tree.querySelectorAll(".tree-checkbox").forEach(chk => {
        chk.addEventListener("change", e => {
            e.stopPropagation(); // 阻止事件冒泡
            const li = chk.closest(".tree-node");

            // 子節點跟隨父節點勾選狀態
            li.querySelectorAll("ul .tree-checkbox").forEach(c => { 
                c.checked = chk.checked; 
                c.indeterminate = false; 
            });

            // 勾選父節點時自動展開整棵樹
            if (chk.checked) expandTree(li);

            // 更新父節點的半勾狀態
            updateParentState(li);
        });
    });

    // ========================================
    // 輔助函數：展開樹狀結構
    // ========================================

    /**
     * 遞迴展開樹狀結構
     * 
     * 功能：
     * 1. 展開指定的節點
     * 2. 展開該節點的所有子節點
     * 3. 遞迴向上展開所有父節點
     * 
     * @param {HTMLElement} li - 要展開的節點元素
     * 
     * 使用場景：
     * - 勾選父節點時自動展開整棵樹
     * - 確保使用者能看到所有被勾選的節點
     */
    function expandTree(li) {
        if (!li) return;
        
        // 展開當前節點
        li.classList.remove("collapsed"); 
        li.classList.add("expanded");
        
        // 展開所有子節點
        li.querySelectorAll("ul li.tree-node").forEach(c => { 
            c.classList.remove("collapsed"); 
            c.classList.add("expanded"); 
        });
        
        // 遞迴向上展開父節點
        const parentLi = li.parentElement.closest(".tree-node");
        if (parentLi) expandTree(parentLi);
    }

    // ========================================
    // 輔助函數：更新父節點狀態
    // ========================================

    /**
     * 遞迴更新父節點的半勾狀態
     * 
     * 功能：
     * 1. 檢查直接子節點的勾選狀態
     * 2. 根據子節點狀態設定父節點狀態：
     *    - 全部勾選 → 父節點勾選
     *    - 全部未勾選 → 父節點未勾選
     *    - 部分勾選 → 父節點半勾
     * 3. 遞迴向上更新所有父節點
     * 
     * @param {HTMLElement} li - 當前節點元素
     * 
     * 狀態說明：
     * - checked = true: 完全勾選
     * - checked = false, indeterminate = true: 半勾選
     * - checked = false, indeterminate = false: 未勾選
     */
    function updateParentState(li) {
        console.log(li)
        const parentLi = li.parentElement.closest(".tree-node");
        if (!parentLi) return;

        const parentChk = parentLi.querySelector(".node-content .tree-checkbox");

        // 取得父節點的直接子節點 Checkbox
        const siblingCheckboxes = parentLi.querySelectorAll(':scope > ul > .tree-node > .node-content > .tree-checkbox');

        // 計算子節點的勾選狀態
        const allChecked = Array.from(siblingCheckboxes).every(c => c.checked);
        const anyChecked = Array.from(siblingCheckboxes).some(c => c.checked || c.indeterminate);
        const noneChecked = Array.from(siblingCheckboxes).every(c => !c.checked && !c.indeterminate);

        // 根據子節點狀態設定父節點狀態
        if (allChecked) {
            // 全部子節點都勾選 → 父節點完全勾選
            parentChk.checked = true;
            parentChk.indeterminate = false;
        } else if (noneChecked) {
            // 全部子節點都未勾選 → 父節點未勾選
            parentChk.checked = false;
            parentChk.indeterminate = false;
        } else if (anyChecked) {
            // 部分子節點勾選 → 父節點半勾選
            parentChk.checked = false;
            parentChk.indeterminate = true;
        }

        // 遞迴向上更新所有父節點
        updateParentState(parentLi);
    }

    // ========================================
    // 初始化函數
    // ========================================

    /**
     * 初始化父節點的半勾狀態
     * 
     * 功能：
     * 1. 從底層節點開始，逐層向上更新父節點狀態
     * 2. 確保所有父節點的半勾狀態正確
     * 3. 處理頁面載入時已勾選的節點
     * 
     * 執行順序：
     * - 反向排序，先處理底層節點
     * - 確保子節點狀態正確後再更新父節點
     */
    function initParentCheckState() {
        const allNodes = tree.querySelectorAll(".tree-node");
        
        // 反向排序，從底層開始處理
        Array.from(allNodes).reverse().forEach(li => {
            updateParentState(li);
        });
    }

    /**
     * 自動展開已勾選的節點
     * 
     * 功能：
     * 1. 找到所有已勾選的節點
     * 2. 從該節點向上展開所有父節點
     * 3. 確保使用者能看到所有已勾選的節點
     * 
     * 使用場景：
     * - 編輯模式時顯示已設定的權限
     * - 頁面載入時顯示預設勾選的項目
     */
    function expandCheckedNodes() {
        const checkedNodes = tree.querySelectorAll(".tree-checkbox:checked");

        checkedNodes.forEach(chk => {
            let li = chk.closest(".tree-node");
            
            // 從當前節點向上展開所有父節點
            while (li) {
                li.classList.remove("collapsed");
                li.classList.add("expanded");

                // 向上找父節點
                li = li.parentElement.closest(".tree-node");
            }
        });
    }

    // ========================================
    // 執行初始化
    // ========================================

    // 初始化父節點的半勾狀態
    initParentCheckState();
    
    // 展開已勾選的節點
    expandCheckedNodes();

    // ========================================
    // 表單提交功能
    // ========================================

    /**
     * 設定表單提交按鈕事件
     * 
     * 功能：
     * 1. 驗證表單資料
     * 2. 收集模組資料
     * 3. 收集樹狀結構勾選資料
     * 4. 發送 AJAX 請求到後端
     * 5. 處理成功/失敗回應
     * 
     * 資料收集：
     * - 群組名稱和描述
     * - 選擇的模組清單
     * - 勾選的權限節點
     */
    const submitBtn = document.getElementById("submitGroup");
    if (submitBtn) {
        // 解除舊的事件監聽器，避免重複綁定
        submitBtn.replaceWith(submitBtn.cloneNode(true));

        const newSubmitBtn = document.getElementById("submitGroup");
        newSubmitBtn.addEventListener("click", () => {
            // 使用共用的表單驗證函數
            if (!validateGroupForm(true)) return;

            // 收集模組資料
            const container = document.getElementById("moduleContainer");
            const modules = [...container.querySelectorAll('.module-row')]
                .map(row => {
                    const select = row.querySelector('select');
                    const descEl = row.querySelector('.module-desc');
                    return {
                        id: select.value || "",
                        name: select.options[select.selectedIndex]?.text || "",
                        desc: descEl?.textContent.trim() || ""
                    };
                })
                .filter(m => m.id !== "");

            // 收集表單資料
            const inputName = document.getElementById("groupName");
            const inputDesc = document.getElementById("groupDesc");
            
            // 收集樹狀結構勾選資料
            const selectedTree = getCheckedTree(document.querySelector("#controllerTree > ul"));

            // 組裝提交資料
            const data = {
                ID: modules[0].id,
                groupName: inputName.value.trim(),
                groupDesc: inputDesc.value.trim(),
                selectedModules: modules,
                selectedNodes: selectedTree
            };

            console.log("送出資料：", data);

            // 發送 AJAX 請求
            $.ajax({
                url: '/ControllerSetting/UpdateAccessGroup',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(data),
                success: function (res) {
                    alert("送出成功");
                    window.location.reload();
                },
                error: function (err) {
                    console.error(err);
                    alert("送出失敗");
                }
            });
        });
    }

    // ========================================
    // 輔助函數：收集勾選的樹狀節點
    // ========================================

    /**
     * 遞迴收集勾選的樹狀節點資料
     * 
     * 功能：
     * 1. 遍歷樹狀結構的所有節點
     * 2. 收集已勾選的節點資料
     * 3. 保持樹狀結構的層級關係
     * 4. 回傳結構化的資料供後端處理
     * 
     * @param {HTMLElement} ul - 樹狀結構的根節點
     * @returns {Array} 勾選節點的陣列，包含 ID、顯示名稱和子節點
     * 
     * 回傳格式：
     * [
     *   {
     *     ID: "節點ID",
     *     DisplayName: "節點顯示名稱",
     *     children: [子節點陣列] 或 null
     *   }
     * ]
     */
    function getCheckedTree(ul) {
        if (!ul) return [];
        const result = [];

        // 遍歷直接子節點
        ul.querySelectorAll(":scope > li.tree-node").forEach(li => {
            const chk = li.querySelector(":scope > .node-content > .tree-checkbox");
            if (!chk) return;

            // 遞迴處理子節點
            const childUl = li.querySelector(":scope > ul.tree-list");
            const children = getCheckedTree(childUl);

            // 取得節點顯示文字（支援多種可能的 class 名稱）
            let textEl = li.querySelector(".node-name")
                || li.querySelector(".node-text")
                || li.querySelector(".node-text-group");

            let text = "";
            if (textEl) {
                // 如果選到的是群組元素，嘗試取裡面的 .node-name
                const innerName = (textEl.querySelector && textEl.querySelector(".node-name"))
                    ? textEl.querySelector(".node-name").innerText
                    : textEl.innerText;
                text = innerName ? innerName.trim() : "";
            }

            // 如果節點已勾選或有子節點被勾選，加入結果
            if (chk.checked || (children && children.length > 0)) {
                result.push({
                    ID: li.dataset.id,
                    DisplayName: text,
                    children: children.length ? children : null
                });
            }
        });

        return result;
    }
}