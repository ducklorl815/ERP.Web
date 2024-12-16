

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
        if (response.isSuccess) {
            // 使用 data1 和 data2 初始化圖表或進行其他操作
            renderChart(response.totalAmount, response.topAmount, response.orderCount, response.orderDate);
        } else {
            console.error('獲取資料失敗：', response.message);
        }
    },
    error: function (xhr, status, error) {
        console.error('AJAX 請求失敗：', error);
    }
});
function renderChart(totalAmount, topAmount, orderCount, orderDate) {

    // 轉換 orderDate 只保留日期部分
    var formattedDates = orderDate.map(function (date) {
        return date[1]; // 取得日期部分，去掉索引
    });

    $("#flot-dashboard-chart").length && $.plot($("#flot-dashboard-chart"), [
        { label: "總銷售", data: totalAmount, },
        { label: "最高金額", data: topAmount },
        { label: "訂單筆數", data: orderCount }
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
            legend: {
                show: false // 隱藏圖表上的 label
            },
            colors: ["#1ab394", "#1C84C6", "#F39C12"], // 顏色配置
            xaxis: {
                ticks: totalAmount.map(function (value, index) {
                    return [index, formattedDates[index]]; // 将索引与日期配对
                }),
                tickLength: 10,
                rotateTicks: 45,
                axisLabel: "日期" // X 軸標籤
            },
            yaxis: {
                ticks: 5,
                axisLabel: "金額（單位：元）" // Y 軸標籤
            },
            tooltip: true,
            tooltipOpts: {
                content: function (label, xval, yval) {
                    return label + ": " + yval; // 顯示提示框
                }
            }
        });
}

