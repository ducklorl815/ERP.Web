/**
 * Slack Chat 圖示管理模組
 * 負責近期對話視窗小圖示的管理
 */
(function () {
    'use strict';

    if (!window.SlackChat) {
        console.error('SlackChat 核心模組未載入');
        return;
    }

    const config = window.SlackChat.config;
    const state = window.SlackChat.state;

    window.SlackChat.icons = {
        /**
         * 建立近期對話視窗小圖示
         */
        createRecentChatIcon(channelId, displayName, userId, realName) {
            if (!config.recentChatIconsContainer) return;
            
            // 如果圖示已存在，更新圖示內容（特別是 realName）並移到最前面
            const existingIcon = state.recentChatIcons.get(channelId);
            if (existingIcon) {
                // 更新圖示內容（使用 realName）
                const nameToUse = realName || displayName || '';
                const firstLetter = nameToUse.charAt(0).toUpperCase();
                
                // 先移除關閉按鈕（稍後會重新添加）
                const closeBtn = existingIcon.querySelector('.slack-icon-close-btn');
                if (closeBtn) {
                    closeBtn.remove();
                }
                
                // 清除現有內容
                existingIcon.innerHTML = '';
                
                // 如果有名稱，顯示第一個字母；否則使用預設圖示
                if (nameToUse && nameToUse.length > 0) {
                    // 顯示第一個字母作為圖示
                    const letterSpan = document.createElement('span');
                    letterSpan.textContent = firstLetter;
                    letterSpan.style.fontSize = '18px';
                    letterSpan.style.fontWeight = 'bold';
                    existingIcon.appendChild(letterSpan);
                } else {
                    // 使用預設圖示
                    const icon = document.createElement('i');
                    icon.className = 'fa fa-comment';
                    existingIcon.appendChild(icon);
                }
                
                // 重新添加關閉按鈕
                this.addCloseButtonToIcon(existingIcon, channelId);
                
                // 更新 title 和 data 屬性（優先使用 realName）
                existingIcon.title = realName || displayName || '開啟聊天視窗';
                if (realName) {
                    existingIcon.dataset.realName = realName;
                }
                
                // 將現有圖示移到最前面（因為容器使用 flex-direction: row-reverse，所以使用 appendChild 會讓它顯示在最左邊）
                if (existingIcon.parentNode === config.recentChatIconsContainer) {
                    const lastChild = config.recentChatIconsContainer.lastChild;
                    if (lastChild !== existingIcon) {
                        config.recentChatIconsContainer.removeChild(existingIcon);
                        config.recentChatIconsContainer.appendChild(existingIcon);
                    }
                }
                
                return existingIcon;
            }

            // 建立小圖示按鈕
            const iconBtn = document.createElement('div');
            iconBtn.className = 'slack-recent-chat-icon';
            iconBtn.title = realName || displayName || '開啟聊天視窗';
            iconBtn.dataset.channelId = channelId;
            iconBtn.dataset.channelName = displayName || '';
            // 儲存 realName 到 data 屬性，以便後續使用
            if (realName) {
                iconBtn.dataset.realName = realName;
            }
            if (userId) {
                iconBtn.dataset.userId = userId;
            }

            // 建立圖示（優先使用 realName 的第一個字母，如果沒有則使用 displayName）
            const nameToUse = realName || displayName || '';
            const firstLetter = nameToUse.charAt(0).toUpperCase();
            // 如果有名稱，顯示第一個字母；否則使用預設圖示
            if (nameToUse && nameToUse.length > 0) {
                // 顯示第一個字母作為圖示
                const letterSpan = document.createElement('span');
                letterSpan.textContent = firstLetter;
                letterSpan.style.fontSize = '18px';
                letterSpan.style.fontWeight = 'bold';
                iconBtn.appendChild(letterSpan);
            } else {
                // 使用預設圖示
                const icon = document.createElement('i');
                icon.className = 'fa fa-comment';
                iconBtn.appendChild(icon);
            }

            // 添加關閉按鈕
            this.addCloseButtonToIcon(iconBtn, channelId);

            // 點擊事件：開啟對應的聊天視窗（但點擊關閉按鈕時不觸發）
            iconBtn.addEventListener('click', (e) => {
                // 如果點擊的是關閉按鈕，不觸發開啟視窗
                if (e.target.closest('.slack-icon-close-btn')) {
                    return;
                }
                if (channelId) {
                    // 從圖示的 data 屬性中取得 realName（如果有的話）
                    const iconRealName = iconBtn.dataset.realName || realName;
                    const iconDisplayName = iconBtn.dataset.channelName || displayName;
                    
                    // 如果視窗已存在，直接顯示
                    if (state.chatWindows.has(channelId)) {
                        const existingWindow = state.chatWindows.get(channelId);
                        existingWindow.classList.add('active');
                        // 確保標題使用 RealName
                        const existingRealName = existingWindow.dataset.realName || iconRealName;
                        if (existingRealName) {
                            const titleElement = existingWindow.querySelector('.slack-channel-title');
                            if (titleElement) {
                                titleElement.textContent = existingRealName;
                            }
                            existingWindow.dataset.realName = existingRealName;
                        }
                    } else {
                        // 建立新的聊天視窗（傳遞 realName）
                        window.SlackChat.window?.createChatWindow(channelId, iconDisplayName, userId, iconRealName);
                    }
                }
            });

            // 加入容器（因為容器使用 flex-direction: row-reverse，所以使用 appendChild 會讓新圖示顯示在最左邊）
            config.recentChatIconsContainer.appendChild(iconBtn);
            state.recentChatIcons.set(channelId, iconBtn);

            return iconBtn;
        },

        /**
         * 為小圖示添加關閉按鈕
         */
        addCloseButtonToIcon(iconElement, channelId) {
            if (!iconElement || !channelId) return;
            
            // 檢查是否已經有關閉按鈕
            let closeBtn = iconElement.querySelector('.slack-icon-close-btn');
            if (closeBtn) {
                return; // 已經有關閉按鈕，不需要重複添加
            }
            
            // 建立關閉按鈕
            closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'slack-icon-close-btn';
            closeBtn.title = '關閉聊天視窗';
            closeBtn.innerHTML = '×';
            closeBtn.setAttribute('aria-label', '關閉聊天視窗');
            
            // 點擊關閉按鈕時關閉對應的聊天視窗並移除小圖示
            closeBtn.addEventListener('click', (e) => {
                e.preventDefault();
                e.stopPropagation(); // 阻止事件冒泡，避免觸發圖示的點擊事件
                
                if (channelId) {
                    // 關閉聊天視窗
                    window.SlackChat.window?.closeChatWindow(channelId);
                    // 移除小圖示
                    window.SlackChat.icons.removeRecentChatIcon(channelId);
                }
            });
            
            // 將關閉按鈕添加到圖示中
            iconElement.appendChild(closeBtn);
        },

        /**
         * 移除近期對話視窗小圖示
         */
        removeRecentChatIcon(channelId) {
            const iconBtn = state.recentChatIcons.get(channelId);
            if (iconBtn && iconBtn.parentNode) {
                iconBtn.parentNode.removeChild(iconBtn);
                state.recentChatIcons.delete(channelId);
            }
        },

        /**
         * 更新近期對話視窗小圖示狀態（當視窗開啟時）
         */
        updateRecentChatIconState(channelId, isActive) {
            const iconBtn = state.recentChatIcons.get(channelId);
            if (iconBtn) {
                if (isActive) {
                    iconBtn.style.background = '#18a689';
                    iconBtn.style.boxShadow = '0 4px 12px rgba(26, 179, 148, 0.4)';
                    // 當視窗開啟時，清除未讀訊息標記
                    this.updateUnreadCount(channelId, 0);
                } else {
                    iconBtn.style.background = '#1ab394';
                    iconBtn.style.boxShadow = '0 3px 10px rgba(0, 0, 0, 0.15)';
                }
            }
        },

        /**
         * 更新未讀訊息數量
         * @param {string} channelId - 頻道 ID
         * @param {number} count - 未讀訊息數量（0 表示清除標記）
         */
        updateUnreadCount(channelId, count) {
            const iconBtn = state.recentChatIcons.get(channelId);
            if (!iconBtn) return;

            // 查找或建立未讀訊息標記
            let badge = iconBtn.querySelector('.badge');
            
            if (count > 0) {
                // 需要顯示未讀訊息標記
                if (!badge) {
                    // 建立新的標記
                    badge = document.createElement('span');
                    badge.className = 'badge';
                    iconBtn.appendChild(badge);
                }
                // 更新數量（如果超過 99，顯示 99+）
                badge.textContent = count > 99 ? '99+' : count.toString();
                badge.style.display = 'flex';
            } else {
                // 清除未讀訊息標記
                if (badge) {
                    badge.style.display = 'none';
                }
            }
        },

        /**
         * 增加未讀訊息數量（用於收到新訊息時）
         * @param {string} channelId - 頻道 ID
         * @param {number} increment - 要增加的数量（預設為 1）
         */
        incrementUnreadCount(channelId, increment = 1) {
            const iconBtn = state.recentChatIcons.get(channelId);
            if (!iconBtn) return;

            // 取得目前的未讀數量
            const badge = iconBtn.querySelector('.badge');
            let currentCount = 0;
            
            if (badge && badge.style.display !== 'none') {
                const badgeText = badge.textContent || '';
                // 如果顯示 "99+"，則使用 99 作為基數
                if (badgeText === '99+') {
                    currentCount = 99;
                } else {
                    currentCount = parseInt(badgeText) || 0;
                }
            }
            
            // 更新未讀數量
            this.updateUnreadCount(channelId, currentCount + increment);
        }
    };
})();

