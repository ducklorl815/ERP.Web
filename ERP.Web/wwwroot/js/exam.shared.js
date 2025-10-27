(function (window, document) {
    "use strict";

    // 選取題目 ID 的共用儲存陣列（提供相容的全域引用）
    var selectedWordIDs = [];
    window.selectedWordIDs = selectedWordIDs;

    // 防抖函式（debounce）：限制高頻事件觸發次數
    function debounce(fn, wait) {
        var timerId = null;
        return function () {
            var context = this;
            var args = arguments;
            clearTimeout(timerId);
            timerId = setTimeout(function () {
                fn.apply(context, args);
            }, wait);
        };
    }

    // 取得 Cookie 值
    function getCookieValue(name) {
        var arr = document.cookie.split('; ');
        for (var i = 0; i < arr.length; i++) {
            var row = arr[i];
            if (row.indexOf(name + '=') === 0) {
                try {
                    return decodeURIComponent(row.split('=')[1]);
                } catch (e) {
                    return null;
                }
            }
        }
        return null;
    }

    // 設定 Cookie 值（path 預設為根目錄）
    function setCookie(name, value) {
        document.cookie = name + '=' + encodeURIComponent(value) + '; path=/';
    }

    // 從 Cookie 載入指定 Kid 的已選取題目 ID 清單
    function loadSelectedIdsFromCookie(kidId) {
        if (!kidId) {
            selectedWordIDs.length = 0;
            return;
        }
        var cookieName = 'SelectedWordIDs_' + kidId;
        var raw = getCookieValue(cookieName) || '[]';
        var parsed;
        try {
            parsed = JSON.parse(raw);
        } catch (e) {
            parsed = [];
        }
        // 重置原陣列內容以保留引用
        selectedWordIDs.length = 0;
        if (Array.isArray(parsed)) {
            for (var i = 0; i < parsed.length; i++) {
                selectedWordIDs.push(parsed[i]);
            }
        }
    }

    // 依目前 selectedWordIDs 勾選 _ReTest 清單中的 checkbox（支援指定容器）
    function syncSelectedReTest(root) {
        var container = root || document;
        var nodeList = container.querySelectorAll('input[type="checkbox"][name^="selectedQuestions["]');
        for (var i = 0; i < nodeList.length; i++) {
            var checkbox = nodeList[i];
            var wordId = checkbox.value;
            checkbox.checked = selectedWordIDs.indexOf(wordId) !== -1;
        }
    }

    // 導出共用物件
    window.ExamShared = {
        debounce: debounce,
        getCookieValue: getCookieValue,
        setCookie: setCookie,
        loadSelectedIdsFromCookie: loadSelectedIdsFromCookie,
        syncSelectedReTest: syncSelectedReTest,
        selectedWordIDs: selectedWordIDs
    };
})(window, document);


