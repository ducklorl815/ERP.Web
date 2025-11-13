/**
 * Slack Chat 視窗管理模組
 * 負責聊天視窗的建立、關閉和表單管理
 */
(function () {
    'use strict';

    if (!window.SlackChat) {
        console.error('SlackChat 核心模組未載入');
        return;
    }

    const config = window.SlackChat.config;
    const state = window.SlackChat.state;
    const utils = window.SlackChat.utils;

    window.SlackChat.window = {
        /**
         * 建立新聊天視窗
         */
        createChatWindow(channelId, channelName, userId) {
            // 如果視窗已存在，則直接顯示
            if (state.chatWindows.has(channelId)) {
                const existingWindow = state.chatWindows.get(channelId);
                existingWindow.classList.add('active');
                // 更新對應的小圖示狀態為活躍
                if (channelId) {
                    window.SlackChat.icons?.updateRecentChatIconState(channelId, true);
                }
                return existingWindow;
            }

            // 從模板建立新視窗
            const windowElement = config.template.content.cloneNode(true).querySelector('.slack-chat-box');
            windowElement.dataset.channelId = channelId || '';
            windowElement.dataset.channelName = channelName || '';
            if (userId) {
                windowElement.dataset.userId = userId;
            }

            // 設定頻道標題
            const titleElement = windowElement.querySelector('.slack-channel-title');
            if (titleElement) {
                titleElement.textContent = channelName || 'Slack 即時通訊';
            }

            // 如果沒有頻道 ID（新開的頻道），自動顯示搜尋功能
            const searchToggle = windowElement.querySelector('.slack-search-toggle');
            const searchWrapper = windowElement.querySelector('.slack-channel-search-wrapper');
            if (!channelId || channelId === '') {
                // 新開的頻道，顯示搜尋按鈕和搜尋欄
                if (searchToggle) {
                    searchToggle.style.display = 'block';
                }
                if (searchWrapper) {
                    searchWrapper.style.display = 'block';
                }
                // 自動聚焦到搜尋輸入框並載入頻道列表
                const searchInput = windowElement.querySelector('.slack-channel-search');
                const channelResults = windowElement.querySelector('.slack-channel-results');
                if (searchInput) {
                    // 先載入頻道列表
                    window.SlackChat.channel?.loadChannels().then(() => {
                        setTimeout(() => {
                            searchInput.focus();
                            // 如果已經有頻道快取，顯示頻道列表
                            if (state.channelCache.length > 0 && channelResults) {
                                window.SlackChat.channel?.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                                channelResults.style.display = 'block';
                            } else if (channelResults) {
                                // 如果沒有頻道，顯示提示訊息
                                channelResults.innerHTML = '<div class="text-muted small text-center py-2">載入頻道中...</div>';
                                channelResults.style.display = 'block';
                            }
                        }, 100);
                    }).catch(error => {
                        console.error('載入頻道列表失敗', error);
                        if (channelResults) {
                            channelResults.innerHTML = '<div class="text-muted small text-center py-2">載入頻道失敗，請重試</div>';
                            channelResults.style.display = 'block';
                        }
                    });
                }
            } else {
                // 已有頻道的視窗，搜尋按鈕保持顯示（可以點擊來顯示搜尋欄）
                if (searchToggle) {
                    searchToggle.style.display = 'block';
                }
            }

            // 設定關閉按鈕
            const closeBtn = windowElement.querySelector('.slack-close-btn');
            if (closeBtn) {
                closeBtn.addEventListener('click', () => {
                    // 取得目前的 channelId（可能是空的）
                    const currentChannelId = windowElement.dataset.channelId || channelId || '';
                    window.SlackChat.window.closeChatWindow(currentChannelId);
                });
            }

            // 設定搜尋功能
            window.SlackChat.channel?.setupSearchInWindow(windowElement);

            // 加入容器
            config.container.appendChild(windowElement);
            windowElement.classList.add('active');
            
            // 如果有頻道 ID，設定表單並載入訊息
            if (channelId) {
                // 使用 setupMessageForm 設定表單（確保使用正確的 channelId）
                window.SlackChat.window.setupMessageForm(windowElement, channelId);
                state.chatWindows.set(channelId, windowElement);
                // 載入歷史訊息
                window.SlackChat.message?.loadMessages(channelId);
                // 更新對應的小圖示狀態為活躍
                window.SlackChat.icons?.updateRecentChatIconState(channelId, true);
            } else {
                // 沒有頻道 ID（新開的頻道），設定表單但不啟用（等待選擇頻道）
                const form = windowElement.querySelector('.slack-chat-form');
                const messageInput = windowElement.querySelector('.slack-message-input');
                if (form && messageInput) {
                    // 禁用輸入框，直到選擇頻道
                    messageInput.disabled = true;
                    messageInput.placeholder = '請先選擇頻道...';
                }
                // 使用空字串作為 key（新開的頻道）
                state.chatWindows.set('', windowElement);
            }

            return windowElement;
        },

        /**
         * 關閉聊天視窗
         */
        closeChatWindow(channelId) {
            // 如果 channelId 為空，嘗試找到所有空字串的視窗
            if (!channelId || channelId === '') {
                // 查找所有空字串的視窗並關閉
                for (const [key, windowElement] of state.chatWindows.entries()) {
                    if (!key || key === '') {
                        windowElement.remove();
                        state.chatWindows.delete(key);
                        return;
                    }
                }
                return;
            }
            
            const windowElement = state.chatWindows.get(channelId);
            if (windowElement) {
                windowElement.remove();
                state.chatWindows.delete(channelId);
                // 更新對應的小圖示狀態為非活躍（但不要刪除小圖示）
                window.SlackChat.icons?.updateRecentChatIconState(channelId, false);
            } else {
                // 如果找不到，嘗試查找所有視窗，看看是否有匹配的
                console.warn('關閉聊天視窗：找不到對應的視窗', channelId);
            }
        },

        /**
         * 設定訊息表單（用於更新 channelId）
         */
        setupMessageForm(windowElement, channelId) {
            if (!windowElement) return;
            
            if (!channelId || channelId === '') {
                console.warn('setupMessageForm: channelId 為空，無法設定表單');
                return;
            }
            
            const form = windowElement.querySelector('.slack-chat-form');
            const messageInput = windowElement.querySelector('.slack-message-input');
            
            if (!form || !messageInput) {
                console.warn('setupMessageForm: 找不到表單或輸入框');
                return;
            }
            
            // 移除舊的事件監聽器（通過移除並重新添加）
            const newForm = form.cloneNode(true);
            form.parentNode.replaceChild(newForm, form);
            
            const newMessageInput = newForm.querySelector('.slack-message-input');
            
            if (!newMessageInput) {
                console.warn('setupMessageForm: 找不到新的輸入框');
                return;
            }
            
            // 啟用輸入框並設定 placeholder
            newMessageInput.disabled = false;
            newMessageInput.placeholder = '請輸入要發送的訊息（按 Enter 發送，Shift+Enter 換行）';
            
            // 使用閉包確保 channelId 正確
            const currentChannelId = channelId;
            
            // 重新設定表單提交
            newForm.addEventListener('submit', (e) => {
                e.preventDefault();
                const text = newMessageInput.value.trim();
                if (text && currentChannelId) {
                    console.log('發送訊息到頻道:', currentChannelId, '內容:', text);
                    window.SlackChat.message?.sendMessage(currentChannelId, text).then(() => {
                        newMessageInput.value = '';
                    }).catch(error => {
                        console.error('發送訊息失敗', error);
                    });
                } else {
                    console.warn('無法發送訊息：文字為空或 channelId 無效', { text, currentChannelId });
                }
            });
            
            // Enter 發送，Shift+Enter 換行
            newMessageInput.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' && !e.shiftKey) {
                    e.preventDefault();
                    newForm.dispatchEvent(new Event('submit'));
                }
            });
            
            console.log('表單設定完成，channelId:', currentChannelId);
        }
    };
})();

