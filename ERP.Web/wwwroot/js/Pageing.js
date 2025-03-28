
/*分頁 control start*/
function ListDataAjax(page, callback) {
    var obj = $.extend({
        url: "",
        data: {},
        method: "post",
        wrapperContent: ".ibox-list-content",
        blockUI: false,
        footable: false
    }, callback());
    if (!page || page < 1)
        page = 1;
    var data = { Page: page, PageSize: $(".page-block input[name='PageSize']").val() };
    $.extend(data, obj.data);
    if (obj.blockUI) {
        $.blockUI();
    }
    $.ajax({
        url: obj.url,
        data: data,
        method: obj.method
    }).done(function (data) {
        $(obj.wrapperContent).html(data);
        if (obj.footable) {
            $('.footable').footable({ paginate: false });
        }
        if (obj.blockUI) {
            $.unblockUI();
        }
    }).fail(function () {
        if (obj.blockUI) {
            $.unblockUI();
        }
        swerror("系統發生錯誤");
    })
}
function PageTurning(callback) {
    $("body").on("click", ".page-block ul li:not(.active,.disabled) a", function () {
        ListDataAjax($(this).data("page"), callback);
    });
    $("body").on("focusout", ".page-block input[name='PageSize']", function () {
        $(".page-block input[name='PageSize']").val($(this).val());
        ListDataAjax(1, callback);
    });
}
/*分頁 control end*/