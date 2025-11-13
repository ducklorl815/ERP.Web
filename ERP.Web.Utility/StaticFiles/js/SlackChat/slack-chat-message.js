/**
 * Slack Chat 訊息管理模組
 * 負責訊息的載入、渲染、發送和輪詢
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

    window.SlackChat.message = {
        /**
         * 載入訊息
         */
        async loadMessages(channelId, appendOnly = false) {
            if (!channelId) {
                console.warn('載入訊息：channelId 為空');
                return;
            }

            const windowElement = state.chatWindows.get(channelId);
            if (!windowElement) {
                console.warn('載入訊息：找不到對應的視窗', channelId);
                return;
            }

            if (!config.messagesUrl) {
                console.error('載入訊息：messagesUrl 未設定');
                const messageEmpty = windowElement.querySelector('.slack-message-empty');
                if (messageEmpty && !appendOnly) {
                    messageEmpty.textContent = '訊息 URL 未設定';
                }
                return;
            }

            const messageList = windowElement.querySelector('.slack-message-list');
            const messageEmpty = windowElement.querySelector('.slack-message-empty');
            if (!messageList) {
                console.warn('載入訊息：找不到訊息列表元素');
                return;
            }

            // 顯示載入中訊息（只在非追加模式時顯示）
            if (messageEmpty && !appendOnly) {
                messageEmpty.textContent = '載入中...';
            }

            try {
                // 如果是要追加新訊息，可以使用最後一次訊息時間戳記作為參數
                let url = `${config.messagesUrl}?channel=${encodeURIComponent(channelId)}&limit=20`;
                if (appendOnly && state.channelLastMessageTime.has(channelId)) {
                    const lastMessageTime = state.channelLastMessageTime.get(channelId);
                    // 可以添加 since 參數（如果 API 支援）
                    // url += `&since=${lastMessageTime}`;
                }
                
                console.log('載入訊息：', url, appendOnly ? '(追加模式)' : '(完整載入)');
                
                const response = await fetch(url, {
                    method: 'GET',
                    headers: { 
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    credentials: 'same-origin'
                });

                if (response.ok) {
                    const data = await response.json();
                    // 檢查回應格式
                    const messages = Array.isArray(data) ? data : (data?.messages || data?.Messages || []);
                    window.SlackChat.message.renderMessages(messages, messageList, messageEmpty, channelId, appendOnly);
                } else {
                    console.error('載入訊息失敗', response.status, response.statusText);
                    const errorText = await response.text().catch(() => '');
                    console.error('錯誤詳情：', errorText);
                    
                    if (messageEmpty && !appendOnly) {
                        if (response.status === 401) {
                            messageEmpty.textContent = '未授權，請重新登入';
                        } else if (response.status === 403) {
                            messageEmpty.textContent = '無權限存取此頻道';
                        } else if (response.status === 404) {
                            messageEmpty.textContent = '頻道不存在';
                        } else {
                            messageEmpty.textContent = `載入訊息失敗 (${response.status})`;
                        }
                    }
                }
            } catch (error) {
                console.error('載入訊息錯誤', error);
                if (messageEmpty && !appendOnly) {
                    if (error.message && error.message.includes('fetch')) {
                        messageEmpty.textContent = '無法連線到伺服器';
                    } else {
                        messageEmpty.textContent = '載入訊息時發生錯誤';
                    }
                }
            }
        },
        
        /**
         * 輪詢新訊息
         */
        startMessagePolling() {
            // 清除舊的輪詢計時器
            if (state.messagePollingTimer) {
                clearInterval(state.messagePollingTimer);
            }
            
            // 開始輪詢
            state.messagePollingTimer = setInterval(() => {
                // 只輪詢已開啟的聊天視窗
                for (const [channelId, windowElement] of state.chatWindows.entries()) {
                    // 跳過空字串的 channelId（新開的頻道）
                    if (channelId && channelId !== '') {
                        // 檢查視窗是否可見
                        if (windowElement && windowElement.classList.contains('active')) {
                            // 追加模式載入新訊息
                            window.SlackChat.message.loadMessages(channelId, true).catch(error => {
                                console.error('輪詢訊息錯誤', channelId, error);
                            });
                        }
                    }
                }
            }, config.messagePollingInterval);
            
            console.log('開始輪詢新訊息，間隔:', config.messagePollingInterval, '毫秒');
        },
        
        /**
         * 停止輪詢新訊息
         */
        stopMessagePolling() {
            if (state.messagePollingTimer) {
                clearInterval(state.messagePollingTimer);
                state.messagePollingTimer = null;
                console.log('停止輪詢新訊息');
            }
        },

        /**
         * 渲染訊息
         */
        renderMessages(messages, messageList, messageEmpty, channelId, appendOnly = false) {
            if (!messageList) return;

            // 如果只是追加新訊息，不要清空現有訊息
            if (!appendOnly) {
                messageList.innerHTML = '';
            }

            // 如果沒有訊息，顯示空訊息提示
            if (!messages || messages.length === 0) {
                if (!appendOnly && messageEmpty) {
                    messageEmpty.textContent = '目前沒有訊息';
                    messageList.appendChild(messageEmpty);
                }
                return;
            }

            // 追蹤最後一次訊息時間戳記
            let lastTimestamp = null;
            if (messages.length > 0) {
                // 找到最新的訊息時間戳記
                const timestamps = messages
                    .map(item => item.ts ? parseFloat(item.ts) : 0)
                    .filter(ts => ts > 0);
                if (timestamps.length > 0) {
                    lastTimestamp = Math.max(...timestamps);
                }
            }

            // 如果只是追加新訊息，只顯示新的訊息（比最後一次時間戳記新的訊息）
            let messagesToRender = messages;
            if (appendOnly && channelId && state.channelLastMessageTime.has(channelId)) {
                const lastMessageTime = state.channelLastMessageTime.get(channelId);
                messagesToRender = messages.filter(item => {
                    if (!item || !item.ts) return false;
                    const itemTime = parseFloat(item.ts);
                    return itemTime > lastMessageTime;
                });
                
                // 如果沒有新訊息，直接返回
                if (messagesToRender.length === 0) {
                    return;
                }
            }

            // 如果是追加模式，按時間順序排序（從舊到新）
            // 如果是完整載入，反轉訊息順序（從舊到新）
            const sortedMessages = appendOnly 
                ? messagesToRender.sort((a, b) => {
                    const timeA = a.ts ? parseFloat(a.ts) : 0;
                    const timeB = b.ts ? parseFloat(b.ts) : 0;
                    return timeA - timeB;
                })
                : [...messagesToRender].reverse();
                
            sortedMessages.forEach(item => {
                if (!item || item.subtype === 'channel_join') return;

                const wrapper = document.createElement('div');
                const userId = item.user || item.userId || item.UserId || '';
                const isMe = config.currentUserId && userId === config.currentUserId;
                wrapper.className = `slack-message-item${isMe ? ' me' : ''}`;

                // 如果訊息物件中有使用者資訊，儲存到對應表中
                if (userId) {
                    const realName = item.realName || item.RealName || item.real_name || item.real_name_normalized || '';
                    const displayName = item.displayName || item.DisplayName || item.display_name || item.display_name_normalized || '';
                    const userName = item.userName || item.UserName || item.username || item.user_name || '';
                    
                    // 優先使用 realName，其次使用 displayName，最後使用 userName
                    const nameToStore = realName || displayName || userName;
                    if (nameToStore && !state.userInfoMap.has(userId)) {
                        state.userInfoMap.set(userId, {
                            realName: realName || displayName || userName,
                            displayName: displayName || userName || realName
                        });
                    }
                }

                // 取得顯示名稱（優先使用 realName）
                let displayUserName = '未知使用者';
                if (isMe) {
                    displayUserName = '我';
                } else if (userId) {
                    const userInfo = state.userInfoMap.get(userId);
                    if (userInfo && userInfo.realName) {
                        displayUserName = userInfo.realName;
                    } else if (userInfo && userInfo.displayName) {
                        displayUserName = userInfo.displayName;
                    } else {
                        // 如果對應表中沒有，嘗試從訊息物件中取得
                        const realName = item.realName || item.RealName || item.real_name || item.real_name_normalized || '';
                        const displayName = item.displayName || item.DisplayName || item.display_name || item.display_name_normalized || '';
                        const userName = item.userName || item.UserName || item.username || item.user_name || '';
                        displayUserName = realName || displayName || userName || userId;
                    }
                }

                // 訊息元資料（使用者名稱和時間）
                const meta = document.createElement('div');
                meta.className = 'slack-message-meta';
                const date = item.ts ? utils.formatTimestamp(item.ts) : '';
                meta.innerHTML = `<span>${displayUserName}</span><span>${date}</span>`;

                // 訊息內容
                const text = document.createElement('div');
                text.className = 'slack-message-text';
                text.textContent = item.text || '';

                wrapper.appendChild(meta);
                wrapper.appendChild(text);
                
                // 追加到列表末尾
                messageList.appendChild(wrapper);
            });

            // 更新最後一次訊息時間戳記
            if (channelId) {
                if (lastTimestamp) {
                    const currentLastTime = state.channelLastMessageTime.get(channelId) || 0;
                    if (lastTimestamp > currentLastTime) {
                        state.channelLastMessageTime.set(channelId, lastTimestamp);
                        console.log('更新最後一次訊息時間戳記:', channelId, lastTimestamp);
                    }
                } else if (!appendOnly) {
                    // 如果是完整載入但沒有訊息，清除時間戳記
                    state.channelLastMessageTime.delete(channelId);
                }
            }

            // 自動滾動到底部
            messageList.scrollTop = messageList.scrollHeight;
        },

        /**
         * 發送訊息
         */
        async sendMessage(channelId, text) {
            if (!channelId || !text || !config.messageUrl) {
                console.warn('發送訊息：參數不完整', { channelId, text, messageUrl: config.messageUrl });
                return Promise.reject(new Error('參數不完整'));
            }

            const windowElement = state.chatWindows.get(channelId);
            if (!windowElement) {
                console.warn('發送訊息：找不到對應的視窗', channelId);
                return Promise.reject(new Error('找不到對應的視窗'));
            }

            // 顯示發送中狀態
            utils.showTemporaryStatusInWindow(windowElement, '發送中，請稍候...', 'info');

            try {
                console.log('發送訊息請求:', { channel: channelId, text, messageUrl: config.messageUrl });
                
                const response = await fetch(config.messageUrl, {
                    method: 'POST',
                    headers: { 
                        'Content-Type': 'application/json',
                        'Accept': 'application/json'
                    },
                    credentials: 'same-origin',
                    body: JSON.stringify({ channel: channelId, text })
                });

                if (!response.ok) {
                    const errorText = await response.text().catch(() => '');
                    console.error('發送訊息失敗', response.status, response.statusText, errorText);
                    let errorMessage = '發送失敗';
                    try {
                        const errorResult = JSON.parse(errorText);
                        errorMessage = errorResult.message || errorResult.error || errorMessage;
                    } catch (e) {
                        errorMessage = errorText || errorMessage;
                    }
                    utils.showTemporaryStatusInWindow(windowElement, errorMessage, 'danger', 3000);
                    return Promise.reject(new Error(errorMessage));
                }

                const result = await response.json().catch(() => ({}));
                
                console.log('發送訊息成功', result);
                utils.showTemporaryStatusInWindow(windowElement, '訊息已成功發送', 'success', 2000);
                
                // 重新載入訊息（延遲一點時間，確保訊息已經寫入）
                setTimeout(() => {
                    window.SlackChat.message.loadMessages(channelId);
                }, 500);
                
                return Promise.resolve(result);
            } catch (error) {
                console.error('發送訊息錯誤', error);
                utils.showTemporaryStatusInWindow(windowElement, '無法連線到 Slack API', 'danger', 3000);
                return Promise.reject(error);
            }
        }
    };
})();

