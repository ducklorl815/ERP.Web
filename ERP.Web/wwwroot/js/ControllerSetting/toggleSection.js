/**
 * ========================================
 * 區塊收合/展開功能 JavaScript
 * ========================================
 * 
 * 功能說明：
 * 1. 提供卡片區塊的收合/展開功能
 * 2. 切換箭頭圖示方向
 * 3. 設定預設的收合狀態
 * 
 * 使用場景：
 * - 新增群組模組設定區塊
 * - 修改模組設定區塊
 * - 其他需要收合/展開的卡片區塊
 * 
 * 作者：系統開發課
 * 更新日期：2024
 */

// ========================================
// 收合/展開功能
// ========================================

/**
 * 切換區塊的收合/展開狀態
 * 
 * 功能：
 * 1. 切換區塊的顯示/隱藏狀態
 * 2. 更新箭頭圖示方向
 * 3. 提供平滑的使用者體驗
 * 
 * @param {string} sectionId - 要收合/展開的區塊 ID
 * 
 * 使用方式：
 * - HTML: onclick="toggleSection('createSection')"
 * - JavaScript: toggleSection('createSection')
 * 
 * 圖示狀態：
 * - 展開時：fa-chevron-down (向下箭頭)
 * - 收合時：fa-chevron-right (向右箭頭)
 */
function toggleSection(sectionId) {
    // 取得要收合的區塊元素
    const section = document.getElementById(sectionId);
    
    // 取得區塊標題元素（通常是前一個兄弟元素）
    const header = section.previousElementSibling;
    
    // 取得箭頭圖示元素
    const icon = header.querySelector(".toggle-icon");

    // 檢查當前狀態並切換
    if (section.style.display === "none") {
        // 當前是收合狀態 → 展開
        section.style.display = "block";
        icon.classList.replace("fa-chevron-right", "fa-chevron-down");
    } else {
        // 當前是展開狀態 → 收合
        section.style.display = "none";
        icon.classList.replace("fa-chevron-down", "fa-chevron-right");
    }
}

// ========================================
// 頁面載入時的初始化
// ========================================

/**
 * 頁面載入完成後設定預設的收合狀態
 * 
 * 功能：
 * 1. 設定所有卡片區塊為收合狀態
 * 2. 設定所有箭頭圖示為向右箭頭
 * 3. 提供一致的初始使用者體驗
 * 
 * 執行時機：
 * - DOM 載入完成後自動執行
 * - 確保所有元素都已存在
 * 
 * 預設行為：
 * - 所有 .card-body 區塊預設收合
 * - 所有 .toggle-icon 圖示預設向右
 */
document.addEventListener("DOMContentLoaded", () => {
    // 設定所有卡片區塊為收合狀態
    document.querySelectorAll(".card-body").forEach(el => {
        el.style.display = "inline"; // 或 "block"，根據 CSS 設定調整
    });
    
    // 設定所有箭頭圖示為向右箭頭（收合狀態）
    document.querySelectorAll(".toggle-icon").forEach(icon => {
        icon.classList.remove("fa-chevron-down");
        icon.classList.add("fa-chevron-right");
    });
});