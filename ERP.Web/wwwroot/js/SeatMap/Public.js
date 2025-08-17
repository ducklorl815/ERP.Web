//畫面預設載入
loadSeatStatus();

function loadSeatStatus() {
    const seats = document.querySelectorAll('.seat');
    seats.forEach(seat => {
        const status = seat.dataset.status;
        //方塊屬性
        if (status === 'IsUsing') {
            seat.classList.add('IsUsing');
        }
        if (status === 'IsStuff') {
            seat.classList.add('IsStuff');
        }
        if (status === 'unused') {
            seat.classList.add('unused');
        }
        if (status === 'IsRoad') {
            seat.classList.add('IsRoad');
        }

        const border = seat.dataset.border;
        //邊線處理
        if (border.includes('TopBorder')) {
            seat.classList.add('TopBorder');
        }

        if (border.includes('RightBorder')) {
            seat.classList.add('RightBorder');
        }

        if (border.includes('DownBorder')) {
            seat.classList.add('DownBorder');
        }

        if (border.includes('LeftBorder')) {
            seat.classList.add('LeftBorder');
        }

        if (border.includes('TopBorder') && border.includes('RightBorder')) {
            seat.classList.add('RightTopRadius');
        }

        if (border.includes('TopBorder') && border.includes('LeftBorder')) {
            seat.classList.add('LeftTopRadius');
        }

        if (border.includes('DownBorder') && border.includes('RightBorder')) {
            seat.classList.add('RightDownRadius');
        }

        if (border.includes('DownBorder') && border.includes('LeftBorder')) {
            seat.classList.add('LeftDownRadius');
        }

        if (border.includes('Rotation')) {
            seat.classList.add('Rotation');
        }

    });
}