$('#autoLoginCheckbox').click(function () {
    Swal.fire({
        title: "清除登入資訊並執行登出",
        text: "如需再次使用自動登入請於登入畫面設定",
        icon: "info",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#969696",
        confirmButtonText: "取消自動登入",
        cancelButtonText: "繼續使用"
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: "已清除登入資訊!",
                icon: "success",
                timer: 2000
            }).then(() => {
                window.location.href = '/Account/Logout/';
            });
        }
    });
});