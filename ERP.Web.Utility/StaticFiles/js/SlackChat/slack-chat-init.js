/**
 * Slack Chat 初始化模組
 * 在所有模組載入完成後執行初始化
 */
(function () {
    'use strict';

    // 等待所有模組載入完成後執行初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function() {
            if (window.SlackChat && window.SlackChat.init) {
                window.SlackChat.init();
            }
        });
    } else {
        // DOM 已經載入完成，直接執行初始化
        if (window.SlackChat && window.SlackChat.init) {
            window.SlackChat.init();
        }
    }
})();

