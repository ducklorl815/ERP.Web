SeatClick();//座位點擊功能

// 將修改後的 傳回給資料庫
function ClickSeatSaveData(seat) {
    const status = seat.dataset.status;
    const rowcol = seat.dataset.rowcol.split('&');

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
        col: col
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
                handleStuffClick(seat);
            }
        });
    });
}


//新增障礙物 switch
new Vue({
    el: "#IsStuff",
    data: {
        checked: false
    },

    watch: {
        checked: function (val) {
            console.log(val)
            if (val == true) {
                $('#IsStuff').attr('value', true);
               
            } else if (val == false) {
                $('#IsStuff').prop('checked', false);
                $('#IsStuff').attr('value', false);
            }
        }
    }
});

// 點擊障礙物function
function handleStuffClick(seat) {
    // 移除原先的 class
    seat.className = 'seat'

    if (seat.dataset.status === 'IsStuff') {
        seat.dataset.status = 'unused';
        seat.classList.add('unused');
        seat.innerHTML = '';
    } else {
        seat.dataset.status = 'IsStuff';
        seat.classList.add('IsStuff');
    }
    ClickSeatSaveData(seat);
}