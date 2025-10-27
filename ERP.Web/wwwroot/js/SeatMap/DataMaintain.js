SeatClick();//座位點擊功能

// 將修改後的 傳回給資料庫
function ClickSeatSaveData(seat) {
    const status = seat.dataset.status;
    const rowcol = seat.dataset.rowcol.split('&');
    const sign = seat.dataset.sign;

    var compName = $('#selectComp').val();

    // 確保 border 有預設值（避免 null 或 undefined）
    const border = seat.dataset.border || '';

    const row = parseInt(rowcol[0].split('_')[1]);
    const col = parseInt(rowcol[1].split('_')[1]);

    const seatData = {
        border: border,
        status: status,
        row: row,
        col: col,
        sign: sign
    };

    const JosnString = JSON.stringify(seatData);
    
    // Debug: 顯示完整的座位資訊
    console.log('=== 座位存檔資訊 ===');
    console.log('座位編號:', sign);
    console.log('行 (Row):', row);
    console.log('列 (Col):', col);
    console.log('狀態:', status);
    console.log('邊框:', border || '(無)');
    console.log('JSON:', JosnString);
    $.ajax({
        url: '/SeatMap/SaveSeatMap',
        type: 'POST',
        data: {
            JosnString: JosnString
        },
        success: function (response) {
            console.log('✅ 儲存成功:', response);
            if (response.success) {
                console.log('訊息:', response.message);
            }
        },
        error: function (xhr, status, error) {
            console.error('❌ 儲存失敗');
            console.error('狀態碼:', xhr.status);
            console.error('錯誤訊息:', error);
            console.error('回應內容:', xhr.responseText);
            
            // 如果有後端返回的錯誤訊息，顯示它
            try {
                const errorResponse = JSON.parse(xhr.responseText);
                if (errorResponse.message) {
                    console.error('後端錯誤:', errorResponse.message);
                    alert('儲存失敗：' + errorResponse.message);
                }
            } catch (e) {
                console.error('無法解析錯誤回應');
            }
        }
    });
}

//單點點擊div效果
function SeatClick() {
    const seats = document.querySelectorAll('.seat');
    console.log('SeatClick() 綁定座位數量:', seats.length);
    
    seats.forEach(seat => {
        seat.addEventListener('click', event => {
            console.log('座位被點擊:', seat.dataset.sign);
            console.log('IsStuff 狀態:', $('#IsStuff').is(':checked'));
            console.log('IsUsing 狀態:', $('#IsUsing').is(':checked'));
            console.log('IsRoad 狀態:', $('#IsRoad').is(':checked'));
            
            // 製作方塊 function
            if ($('#IsStuff').is(':checked')) {
                console.log('執行新增障礙物');
                handleStuffClick(seat,"IsStuff");
            }
            if ($('#IsUsing').is(':checked')) {
                console.log('執行新增座位');
                handleStuffClick(seat, "IsUsing");
            }
            if ($('#IsRoad').is(':checked')) {
                console.log('執行新增走道');
                handleStuffClick(seat, "IsRoad");
            }
        });
    });
}

// 拖移按鈕設定樣式
new Vue({
    el: '#app',
    data: {
        IsStuff: false,
        IsUsing: false,
        IsRoad: false
    },
    watch: {
        IsStuff(val) {
            if (val) {
                this.IsUsing = false
                this.IsRoad = false
            }
        },
        IsUsing(val) {
            if (val) {
                this.IsStuff = false
                this.IsRoad = false
            }
        },
        IsRoad(val) {
            if (val) {
                this.IsStuff = false
                this.IsUsing = false
            }
        }
    }
})

// 點擊障礙物function //todo
function handleStuffClick(seat, type) {
    console.log(`handleStuffClick 被呼叫 - 類型: ${type}, 座位: ${seat.dataset.sign}`);
    
    // 移除原先的 class（保留 'seat' 基礎 class）
    seat.className = 'seat';

    if (type === 'IsStuff') {
        console.log('設定為障礙物');
        seat.dataset.status = 'IsStuff';
        seat.classList.add('IsStuff');
        // 清空內容，只顯示座位編號
        seat.innerHTML = `<div style="text-align: center;"><span>${seat.dataset.sign}</span></div>`;
    } 
    else if (type === 'IsRoad') {
        console.log('設定為走道');
        seat.dataset.status = 'IsRoad';
        seat.classList.add('IsRoad');
        // 清空內容，只顯示座位編號
        seat.innerHTML = `<div style="text-align: center;"><span>${seat.dataset.sign}</span></div>`;
    }
    else if (type === 'IsUsing') {
        console.log('設定為使用中座位');
        seat.dataset.status = 'IsUsing';
        seat.classList.add('IsUsing');
        // 顯示椅子 icon
        seat.innerHTML = `
            <div style="text-align: center;">
                <div class="seat IsUsing">
                    <div class="chair-icon"></div>
                    <span class="SeatSign">${seat.dataset.sign}</span>
                </div>
            </div>`;
    }
    
    console.log('更新後的座位:', seat);
    ClickSeatSaveData(seat);
}