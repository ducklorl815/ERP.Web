/**
 * Slack Chat 頻道管理模組
 * 負責頻道搜尋、載入和管理
 */
(function () {
    'use strict';

    if (!window.SlackChat) {
        console.error('SlackChat 核心模組未載入');
        return;
    }

    const config = window.SlackChat.config;
    const state = window.SlackChat.state;

    window.SlackChat.channel = {
        /**
         * 載入頻道列表
         */
        async loadChannels(force = false) {
            // 如果不需要強制重新載入且已有快取，直接返回
            if (!force && state.channelCache.length > 0) {
                // 確保使用者資訊對應表已更新
                state.channelCache.forEach(channel => {
                    const userId = channel.userId || channel.UserId || '';
                    const realName = channel.realName || channel.RealName || channel.real_name || channel.real_name_normalized || '';
                    const displayName = channel.displayName || channel.DisplayName || channel.display_name || channel.display_name_normalized || '';
                    
                    if (userId && (realName || displayName) && !state.userInfoMap.has(userId)) {
                        state.userInfoMap.set(userId, {
                            realName: realName || displayName,
                            displayName: displayName || realName
                        });
                    }
                });
                return state.channelCache;
            }

            try {
                // 嘗試從 sessionStorage 載入
                const storedChannels = this.loadChannelsFromStorage();
                if (!force && storedChannels && storedChannels.length > 0) {
                    state.channelCache = storedChannels;
                    
                    // 將頻道中的使用者資訊儲存到對應表中（特別是私訊頻道）
                    storedChannels.forEach(channel => {
                        const userId = channel.userId || channel.UserId || '';
                        const realName = channel.realName || channel.RealName || channel.real_name || channel.real_name_normalized || '';
                        const displayName = channel.displayName || channel.DisplayName || channel.display_name || channel.display_name_normalized || '';
                        
                        if (userId && (realName || displayName)) {
                            if (!state.userInfoMap.has(userId)) {
                                state.userInfoMap.set(userId, {
                                    realName: realName || displayName,
                                    displayName: displayName || realName
                                });
                            }
                        }
                    });
                    
                    return state.channelCache;
                }

                // 從 API 載入頻道列表
                const url = config.channelsAllUrl || config.channelsUrl;
                if (!url) {
                    console.error('載入頻道列表：URL 未設定');
                    console.error('channelsAllUrl:', config.channelsAllUrl);
                    console.error('channelsUrl:', config.channelsUrl);
                    return [];
                }

                const requestUrl = `${url}?types=private_channel,im&limit=100`;
                console.log('載入頻道列表：', requestUrl);

                const response = await fetch(requestUrl, {
                    method: 'GET',
                    headers: { 
                        'Accept': 'application/json',
                        'Content-Type': 'application/json'
                    },
                    credentials: 'same-origin'
                });

                if (response.ok) {
                    const data = await response.json();
                    // 檢查回應格式，支援多種格式
                    const channels = Array.isArray(data) ? data : (data?.Channels || data?.channels || data?.data || []);
                    
                    if (Array.isArray(channels)) {
                        state.channelCache = channels;
                        const lastUpdated = data?.LastUpdated || data?.lastUpdated || data?.lastUpdated || new Date().toISOString();
                        this.saveChannelsToStorage(channels, lastUpdated);
                        
                        // 將頻道中的使用者資訊儲存到對應表中（特別是私訊頻道）
                        channels.forEach(channel => {
                            const userId = channel.userId || channel.UserId || '';
                            const realName = channel.realName || channel.RealName || channel.real_name || channel.real_name_normalized || '';
                            const displayName = channel.displayName || channel.DisplayName || channel.display_name || channel.display_name_normalized || '';
                            
                            if (userId && (realName || displayName)) {
                                if (!state.userInfoMap.has(userId)) {
                                    state.userInfoMap.set(userId, {
                                        realName: realName || displayName,
                                        displayName: displayName || realName
                                    });
                                }
                            }
                        });
                        
                        console.log('載入頻道列表成功，共', channels.length, '個頻道');
                        return channels;
                    } else {
                        console.warn('載入頻道列表：回應格式不正確', data);
                    }
                } else {
                    console.error('載入頻道列表失敗', response.status, response.statusText);
                    const errorText = await response.text().catch(() => '');
                    console.error('錯誤詳情：', errorText);
                    
                    if (response.status === 401) {
                        console.error('載入頻道列表：未授權，請重新登入');
                    } else if (response.status === 403) {
                        console.error('載入頻道列表：無權限');
                    } else if (response.status === 404) {
                        console.error('載入頻道列表：API 端點不存在');
                    }
                }
            } catch (error) {
                console.error('載入頻道列表錯誤', error);
                if (error.message && error.message.includes('fetch')) {
                    console.error('載入頻道列表：無法連線到伺服器');
                }
            }
            return [];
        },

        /**
         * 從 sessionStorage 載入頻道
         */
        loadChannelsFromStorage() {
            try {
                const stored = sessionStorage.getItem(config.channelsStorageKey);
                const lastUpdated = sessionStorage.getItem(config.channelsLastUpdatedKey);
                if (!stored || !lastUpdated) return null;

                // 檢查快取是否過期
                const lastUpdatedDate = new Date(lastUpdated);
                const now = new Date();
                if (now - lastUpdatedDate >= config.cacheExpirationMs) {
                    return null;
                }

                return JSON.parse(stored);
            } catch (error) {
                console.warn('載入頻道快取錯誤', error);
                return null;
            }
        },

        /**
         * 儲存頻道到 sessionStorage
         */
        saveChannelsToStorage(channels, lastUpdated) {
            try {
                sessionStorage.setItem(config.channelsStorageKey, JSON.stringify(channels));
                sessionStorage.setItem(config.channelsLastUpdatedKey, lastUpdated);
            } catch (error) {
                console.warn('儲存頻道快取錯誤', error);
            }
        },

        /**
         * 為使用者開啟頻道
         */
        async openChannelForUser(userId, displayName) {
            if (!config.openChannelUrl) return null;
            try {
                const response = await fetch(config.openChannelUrl, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ userId })
                });

                if (response.ok) {
                    return await response.json();
                }
            } catch (error) {
                console.error('開啟頻道錯誤', error);
            }
            return null;
        },

        /**
         * 設定視窗內的搜尋功能
         */
        setupSearchInWindow(windowElement) {
            const searchToggle = windowElement.querySelector('.slack-search-toggle');
            const searchWrapper = windowElement.querySelector('.slack-channel-search-wrapper');
            const searchInput = windowElement.querySelector('.slack-channel-search');
            const loadBtn = windowElement.querySelector('.slack-load-channels');
            const channelResults = windowElement.querySelector('.slack-channel-results');
            const channelId = windowElement.dataset.channelId;

            // 搜尋切換按鈕
            if (searchToggle && searchWrapper) {
                searchToggle.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    const isHidden = searchWrapper.style.display === 'none' || searchWrapper.style.display === '';
                    searchWrapper.style.display = isHidden ? 'block' : 'none';
                    if (isHidden && searchInput) {
                        setTimeout(() => {
                            searchInput.focus();
                            // 如果頻道快取為空，先載入頻道列表
                            if (state.channelCache.length === 0) {
                                window.SlackChat.channel.loadChannels().then(() => {
                                    if (state.channelCache.length > 0 && channelResults) {
                                        window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                                        channelResults.style.display = 'block';
                                    }
                                });
                            } else if (channelResults) {
                                window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                                channelResults.style.display = 'block';
                            }
                        }, 100);
                    } else if (!isHidden && channelResults) {
                        channelResults.style.display = 'none';
                    }
                });
            }

            // 搜尋輸入框
            if (searchInput) {
                searchInput.addEventListener('input', () => {
                    if (state.globalSearchTimer) {
                        clearTimeout(state.globalSearchTimer);
                    }
                    state.globalSearchTimer = setTimeout(() => {
                        const keyword = searchInput.value.trim();
                        if (keyword && channelResults) {
                            // 如果頻道快取為空，先載入頻道列表
                            if (state.channelCache.length === 0) {
                                window.SlackChat.channel.loadChannels().then(() => {
                                    window.SlackChat.channel.searchChannelsInWindow(keyword, channelResults, windowElement);
                                });
                            } else {
                                window.SlackChat.channel.searchChannelsInWindow(keyword, channelResults, windowElement);
                            }
                        } else if (channelResults) {
                            // 如果沒有關鍵字，顯示所有頻道
                            if (state.channelCache.length > 0) {
                                window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                                channelResults.style.display = 'block';
                            } else {
                                channelResults.style.display = 'none';
                            }
                        }
                    }, 300);
                });

                searchInput.addEventListener('focus', () => {
                    // 如果頻道快取為空，先載入頻道列表
                    if (state.channelCache.length === 0) {
                        window.SlackChat.channel.loadChannels().then(() => {
                            if (state.channelCache.length > 0 && channelResults) {
                                window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                                channelResults.style.display = 'block';
                            }
                        });
                    } else if (channelResults) {
                        window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                        channelResults.style.display = 'block';
                    }
                });

                // 按下 Enter 鍵時，選擇第一個結果
                searchInput.addEventListener('keydown', (e) => {
                    if (e.key === 'Enter' && channelResults && channelResults.style.display !== 'none') {
                        e.preventDefault();
                        const firstItem = channelResults.querySelector('.slack-channel-item');
                        if (firstItem) {
                            firstItem.click();
                        }
                    }
                });
            }

            // 載入頻道按鈕
            if (loadBtn) {
                loadBtn.addEventListener('click', (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    window.SlackChat.channel.loadChannels(true).then(() => {
                        if (channelResults && searchInput) {
                            const keyword = searchInput.value.trim();
                            if (keyword) {
                                window.SlackChat.channel.searchChannelsInWindow(keyword, channelResults, windowElement);
                            } else {
                                window.SlackChat.channel.renderChannelResultsInWindow(state.channelCache, channelResults, windowElement);
                            }
                            channelResults.style.display = 'block';
                        }
                    });
                });
            }
        },

        /**
         * 在視窗內搜尋頻道
         */
        searchChannelsInWindow(keyword, resultsContainer, windowElement) {
            if (!resultsContainer) return;

            // 過濾頻道列表
            const filtered = state.channelCache.filter(item => {
                if (!item) return false;
                const displayName = ((item.displayName || item.DisplayName || '').toString()).toLowerCase();
                const name = ((item.name || item.Name || '').toString()).toLowerCase();
                const id = ((item.id || item.Id || '').toString()).toLowerCase();
                const userId = ((item.userId || item.UserId || '').toString()).toLowerCase();
                const searchTerm = keyword.toLowerCase();
                return displayName.includes(searchTerm) || name.includes(searchTerm) || id.includes(searchTerm) || userId.includes(searchTerm);
            });

            this.renderChannelResultsInWindow(filtered, resultsContainer, windowElement);
        },

        /**
         * 在視窗內渲染頻道結果
         */
        renderChannelResultsInWindow(channels, container, windowElement) {
            if (!container) return;

            container.innerHTML = '';
            if (!channels || channels.length === 0) {
                const empty = document.createElement('div');
                empty.className = 'text-muted small text-center py-2';
                empty.textContent = '沒有找到相關頻道';
                container.appendChild(empty);
                container.style.display = 'block';
                return;
            }

            // 建立頻道項目按鈕
            channels.forEach(item => {
                if (!item) return;
                const channelId = item.id || item.Id || '';
                const displayName = item.displayName || item.DisplayName || item.name || item.Name || channelId;
                const realName = item.realName || item.RealName || item.real_name || item.real_name_normalized || '';
                const userId = item.userId || item.UserId || '';
                const isIm = item.isIm || item.IsIm || false;
                const isPrivate = item.isPrivate || item.IsPrivate || false;
                const labelSuffix = isIm ? '[私訊]' : (isPrivate ? '[私密]' : '');

                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'btn btn-light btn-sm slack-channel-item';
                button.textContent = displayName;
                
                if (labelSuffix) {
                    const suffix = document.createElement('small');
                    suffix.className = 'text-muted';
                    suffix.textContent = labelSuffix;
                    button.appendChild(document.createTextNode(' '));
                    button.appendChild(suffix);
                }

                button.addEventListener('click', () => {
                    window.SlackChat.channel.selectChannelInWindow(channelId, displayName, userId, realName, windowElement);
                    container.style.display = 'none';
                });

                container.appendChild(button);
            });

            container.style.display = 'block';
        },

        /**
         * 在視窗內選擇頻道
         */
        selectChannelInWindow(channelId, channelName, userId, realName, currentWindow) {
            // 收合搜尋欄
            const searchWrapper = currentWindow.querySelector('.slack-channel-search-wrapper');
            const channelResults = currentWindow.querySelector('.slack-channel-results');
            const searchInput = currentWindow.querySelector('.slack-channel-search');
            
            if (searchWrapper) {
                searchWrapper.style.display = 'none';
            }
            if (channelResults) {
                channelResults.style.display = 'none';
            }
            if (searchInput) {
                searchInput.value = '';
            }

            // 如果沒有頻道 ID 但有使用者 ID，需要開啟頻道
            if (!channelId && userId && config.openChannelUrl) {
                // 先取得舊的 channelId（可能是空的）
                const oldChannelId = currentWindow.dataset.channelId || '';
                
                window.SlackChat.channel.openChannelForUser(userId, channelName).then(result => {
                    if (result && result.channelId) {
                        // 更新視窗資料
                        const newChannelId = result.channelId;
                        currentWindow.dataset.channelId = newChannelId;
                        currentWindow.dataset.channelName = channelName;
                        if (userId) {
                            currentWindow.dataset.userId = userId;
                        }
                        
                        // 更新標題
                        const titleElement = currentWindow.querySelector('.slack-channel-title');
                        if (titleElement) {
                            titleElement.textContent = channelName;
                        }
                        
                        // 更新聊天視窗 Map（移除舊的，加入新的）
                        if (oldChannelId && state.chatWindows.has(oldChannelId)) {
                            state.chatWindows.delete(oldChannelId);
                            console.log('移除舊的 channelId:', oldChannelId);
                        }
                        // 如果舊的 channelId 是空的，也要移除
                        if ((!oldChannelId || oldChannelId === '') && state.chatWindows.has('')) {
                            state.chatWindows.delete('');
                            console.log('移除空 channelId');
                        }
                        state.chatWindows.set(newChannelId, currentWindow);
                        console.log('設定新的 channelId:', newChannelId);
                        
                        // 重新設定表單提交事件（使用新的 channelId）
                        window.SlackChat.window?.setupMessageForm(currentWindow, newChannelId);
                        
                        // 載入歷史訊息
                        window.SlackChat.message?.loadMessages(newChannelId);
                        
                        // 建立或更新小圖示（使用 realName）
                        window.SlackChat.icons?.createRecentChatIcon(newChannelId, channelName, userId, realName);
                        
                        // 更新小圖示狀態
                        window.SlackChat.icons?.updateRecentChatIconState(newChannelId, true);
                    }
                }).catch(error => {
                    console.error('開啟頻道失敗', error);
                    window.SlackChat.utils?.showTemporaryStatusInWindow(currentWindow, '開啟頻道失敗', 'danger', 3000);
                });
                return;
            }

            // 如果有頻道 ID，直接切換
            if (channelId) {
                // 取得舊的 channelId（可能是空的）
                const oldChannelId = currentWindow.dataset.channelId || '';
                
                // 更新視窗資料
                currentWindow.dataset.channelId = channelId;
                currentWindow.dataset.channelName = channelName;
                if (userId) {
                    currentWindow.dataset.userId = userId;
                }
                
                // 更新標題
                const titleElement = currentWindow.querySelector('.slack-channel-title');
                if (titleElement) {
                    titleElement.textContent = channelName;
                }
                
                // 更新聊天視窗 Map（如果 channelId 改變，移除舊的 key）
                if (oldChannelId && oldChannelId !== channelId && state.chatWindows.has(oldChannelId)) {
                    state.chatWindows.delete(oldChannelId);
                    console.log('移除舊的 channelId:', oldChannelId);
                }
                // 如果舊的 channelId 是空的，也要移除
                if ((!oldChannelId || oldChannelId === '') && state.chatWindows.has('')) {
                    state.chatWindows.delete('');
                    console.log('移除空 channelId');
                }
                
                // 加入新的 channelId
                state.chatWindows.set(channelId, currentWindow);
                console.log('設定新的 channelId:', channelId);
                
                // 重新設定表單提交事件（使用新的 channelId）
                window.SlackChat.window?.setupMessageForm(currentWindow, channelId);
                
                // 載入歷史訊息
                window.SlackChat.message?.loadMessages(channelId);
                
                // 建立或更新小圖示（使用 realName）
                window.SlackChat.icons?.createRecentChatIcon(channelId, channelName, userId, realName);
                
                // 更新小圖示狀態
                window.SlackChat.icons?.updateRecentChatIconState(channelId, true);
            }
        }
    };
})();

