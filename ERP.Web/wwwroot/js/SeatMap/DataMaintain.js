SeatClick();//座位點擊功能

// 將修改後的 傳回給資料庫
function ClickSeatSaveData(seat) {
    const status = seat.dataset.status;
    const rowcol = seat.dataset.rowcol.split('&');
    const sign = seat.dataset.sign;

    var compName = $('#selectComp').val();

    if (seat.dataset.border == null) {
        seat.dataset.border = '';
    }
    const border = seat.dataset.border;

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

    console.log('JosnString:', JosnString);
    $.ajax({
        url: '/SeatMap/SaveSeatMap',
        type: 'POST',
        data: {
            JosnString: JosnString
        },
        success: function (compname) {
            console.log('success: ' + compname);
        },
        error: function (xhr, status, error) {
            console.error('failed: ' + error);
        }
    });
}

//單點點擊div效果
function SeatClick() {
    const seats = document.querySelectorAll('.seat');
    seats.forEach(seat => {
        seat.addEventListener('click', event => {
            // 製作方塊 function
            if ($('#IsStuff').is(':checked')) {
                handleStuffClick(seat,"IsStuff");
            }
            if ($('#IsUsing').is(':checked')) {
                handleStuffClick(seat, "IsUsing");
            }
            if ($('#IsRoad').is(':checked')) {
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
function handleStuffClick(seat,type) {
 
    // 移除原先的 class
    seat.className = 'seat'

    if (type === 'IsStuff') {
        seat.dataset.status = 'IsStuff';
        seat.classList.add('IsStuff');
        //seat.innerHTML = '';
    } 
    if (type === 'IsRoad') {
        seat.dataset.status = 'IsRoad';
        seat.classList.add('IsRoad');
        //seat.innerHTML = '';
    }
    if (type === 'IsUsing') {
        seat.dataset.status = 'IsUsing';
        seat.classList.add('IsUsing');
        //seat.innerHTML = '';
    }
    console.log(seat)
    ClickSeatSaveData(seat);
}