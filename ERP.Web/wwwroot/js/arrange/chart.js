

var doughnutData = {
    labels: ["App", "Software", "Laptop"],
    datasets: [{
        data: [300, 50, 100],
        backgroundColor: ["#a3e1d4", "#dedede", "#9CC3DA"]
    }]
};


var doughnutOptions = {
    responsive: false,
    legend: {
        display: false
    }
};


var ctx4 = document.getElementById("doughnutChart").getContext("2d");
new Chart(ctx4, { type: 'doughnut', data: doughnutData, options: doughnutOptions });

var doughnutData = {
    labels: ["App", "Software", "Laptop"],
    datasets: [{
        data: [70, 27, 85],
        backgroundColor: ["#a3e1d4", "#dedede", "#9CC3DA"]
    }]
};


var doughnutOptions = {
    responsive: false,
    legend: {
        display: false
    }
};


var ctx4 = document.getElementById("doughnutChart2").getContext("2d");
new Chart(ctx4, { type: 'doughnut', data: doughnutData, options: doughnutOptions });

//圖表
//訂單金額

// 從後台獲取資料
$.ajax({
    url: '/Charts/GetOrdersAmount', // 後端 API 路徑
    type: 'GET', // 請求方式
    dataType: 'json', // 返回的資料格式
    success: function (response) {
        console.log(response)
        if (response.isSuccess) {

            // 使用 data1 和 data2 初始化圖表或進行其他操作
            renderChart(response.amount, response.count);
        } else {
            console.error('獲取資料失敗：', response.message);
        }
    },
    error: function (xhr, status, error) {
        console.error('AJAX 請求失敗：', error);
    }
});
function renderChart(amount, count) {
    console.log(amount)
    console.log(count)

    $("#flot-dashboard-chart").length && $.plot($("#flot-dashboard-chart"), [
        amount, count
    ],
        {
            series: {
                lines: {
                    show: false,
                    fill: true
                },
                splines: {
                    show: true,
                    tension: 0.4,
                    lineWidth: 1,
                    fill: 0.4
                },
                points: {
                    radius: 0,
                    show: true
                },
                shadowSize: 2
            },
            grid: {
                hoverable: true,
                clickable: true,
                tickColor: "#d5d5d5",
                borderWidth: 1,
                color: '#d5d5d5'
            },
            colors: ["#1ab394", "#1C84C6"],
            xaxis: {
            },
            yaxis: {
                ticks: 4
            },
            tooltip: false
        }
    );
}




