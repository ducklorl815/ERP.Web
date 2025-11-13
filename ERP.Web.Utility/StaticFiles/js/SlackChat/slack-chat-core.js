/**
 * Slack Chat 核心模組
 * 負責初始化、配置管理和共用變數
 */
(function () {
    'use strict';

    // 全域 SlackChat 物件，用於模組間共享
    window.SlackChat = window.SlackChat || {};

    // 取得聊天視窗容器元素
    const container = document.getElementById('slack-chat-container');
    const newChatBtn = document.getElementById('slack-new-chat-btn');
    const template = document.getElementById('slack-chat-window-template');
    const recentChatIconsContainer = document.getElementById('slack-recent-chat-icons');
    
    if (!container || !template) {
        return;
    }

    // 從 data 屬性取得設定值
    const hasToken = container.dataset.hasToken === 'true';
    const authSuccess = container.dataset.authSuccess === 'true';
    const authorizeUrl = container.dataset.authorizeUrl;
    const messageUrl = container.dataset.messageUrl;
    const messagesUrl = container.dataset.messagesUrl;
    const channelsUrl = container.dataset.channelsUrl;
    const channelsAllUrl = container.dataset.channelsAllUrl;
    const channelsRefreshUrl = container.dataset.channelsRefreshUrl;
    const openChannelUrl = container.dataset.openChannelUrl;
    const defaultChannel = container.dataset.defaultChannel || '';
    const currentUserId = container.dataset.userId || '';
    const channelsStorageKey = 'erp.slack.channels';
    const channelsLastUpdatedKey = 'erp.slack.channelsLastUpdated';
    const cacheExpirationMs = 60 * 60 * 1000; // 1 小時（3600000 毫秒）

    // 聊天視窗管理（共用變數）
    const chatWindows = new Map(); // channelId -> windowElement
    const recentChatIcons = new Map(); // channelId -> iconElement
    const channelLastMessageTime = new Map(); // channelId -> lastMessageTimestamp
    const userInfoMap = new Map(); // userId -> { realName, displayName }
    let channelCache = [];
    let globalSearchTimer = null;
    let messagePollingTimer = null;
    const messagePollingInterval = 30000; // 30 秒輪詢一次

    // 匯出共用變數和配置到全域物件
    window.SlackChat.config = {
        container,
        newChatBtn,
        template,
        recentChatIconsContainer,
        hasToken,
        authSuccess,
        authorizeUrl,
        messageUrl,
        messagesUrl,
        channelsUrl,
        channelsAllUrl,
        channelsRefreshUrl,
        openChannelUrl,
        defaultChannel,
        currentUserId,
        channelsStorageKey,
        channelsLastUpdatedKey,
        cacheExpirationMs,
        messagePollingInterval
    };

    window.SlackChat.state = {
        chatWindows,
        recentChatIcons,
        channelLastMessageTime,
        userInfoMap,
        channelCache,
        globalSearchTimer,
        messagePollingTimer
    };

    // 工具函數：格式化時間戳記
    window.SlackChat.utils = {
        formatTimestamp(ts) {
            if (!ts) return '';
            const unix = parseFloat(ts);
            if (Number.isNaN(unix)) return ts;
            const date = new Date(unix * 1000);
            return new Intl.DateTimeFormat('zh-TW', {
                month: '2-digit',
                day: '2-digit',
                hour: '2-digit',
                minute: '2-digit'
            }).format(date);
        },

        showTemporaryStatusInWindow(windowElement, message, type, duration = 2000) {
            if (!windowElement) return;

            const statusElement = windowElement.querySelector('.slack-status-message');
            const statusText = windowElement.querySelector('.slack-status-text');
            
            if (statusElement && statusText) {
                statusText.textContent = message;
                statusElement.style.display = 'block';
                statusElement.hidden = false;
                statusElement.classList.remove('alert-info', 'alert-success', 'alert-danger', 'alert-warning');
                statusElement.classList.add(`alert-${type}`);
                
                // 設定自動隱藏
                setTimeout(() => {
                    statusElement.style.display = 'none';
                    statusElement.hidden = true;
                }, duration);
            }
        }
    };

    // 觸發聊天視窗按鈕事件（即使沒有 Token 也要顯示）
    const chatTrigger = document.getElementById('small-chat');
    const openSmallChatLink = chatTrigger?.querySelector('.open-small-chat');
    if (openSmallChatLink) {
        openSmallChatLink.addEventListener('click', (e) => {
            e.preventDefault();
            if (!hasToken) {
                // 如果沒有 Token，導向授權頁面
                if (authorizeUrl) {
                    window.location.href = authorizeUrl;
                }
                return;
            }
            // 如果有 Token，建立預設聊天視窗
            if (defaultChannel) {
                window.SlackChat.window?.createChatWindow(defaultChannel, 'Slack 即時通訊', '');
            } else {
                // 如果沒有預設頻道，建立空視窗讓使用者選擇
                window.SlackChat.window?.createChatWindow('', 'Slack 即時通訊', '');
            }
        });
    }

    // 新增聊天視窗按鈕事件（即使沒有 Token 也要顯示）
    if (newChatBtn) {
        newChatBtn.addEventListener('click', () => {
            if (!hasToken) {
                // 如果沒有 Token，導向授權頁面
                if (authorizeUrl) {
                    window.location.href = authorizeUrl;
                }
                return;
            }
            // 建立一個空的聊天視窗，等待選擇頻道
            window.SlackChat.window?.createChatWindow('', 'Slack 即時通訊', '');
        });
    }

    // 點擊外部區域關閉頻道列表
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.slack-channel-results') && 
            !e.target.closest('.slack-channel-search') &&
            !e.target.closest('.slack-load-channels')) {
            document.querySelectorAll('.slack-channel-results').forEach(el => {
                el.style.display = 'none';
            });
        }
    });

    // 如果沒有 Token，只初始化觸發按鈕，不初始化其他功能
    if (!hasToken) {
        return;
    }

    // 初始化：載入頻道列表（等待其他模組載入後執行）
    // 這個會在 slack-chat-init.js 中執行
    window.SlackChat.init = function() {
        window.SlackChat.channel?.loadChannels().then(() => {
            // 啟動輪詢機制
            window.SlackChat.message?.startMessagePolling();
        }).catch(error => {
            console.error('初始化失敗', error);
            // 即使失敗也啟動輪詢機制
            window.SlackChat.message?.startMessagePolling();
        });
    };

    // 頁面卸載時停止輪詢
    window.addEventListener('beforeunload', () => {
        window.SlackChat.message?.stopMessagePolling();
    });
})();

