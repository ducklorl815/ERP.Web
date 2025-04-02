/// <reference path="../lib/sweetalert2/dist/sweetalert2.min.js" />
/// <reference path="../lib/sweetalert2/dist/sweetalert2.min.js" />
/* Date Extention Start */
Date.prototype.addDays = function (days) {
    this.setDate(this.getDate() + days);
    return this;
}
/* Date Extention End */

/* 取得 QueryString Start */
function GetQueryStringByKey(name, url) {
    if (!url) url = window.location.href;
    name = name.replace(/[\[\]]/g, '\\$&');
    var regex = new RegExp('[?&]' + name + '(=([^&#]*)|&|#|$)'),
        results = regex.exec(url);
    if (!results) return '';
    if (!results[2]) return '';
    return decodeURIComponent(results[2].replace(/\+/g, ' '));
}
/* 取得 QueryString End */

/* QueryString Start */
function QueryStringToObject(search) {

    var s1 = search ? search : window.location.search;
    var s2 = s1.charAt(0) == "?" ? s1.substring(1) : s1;
    var pairs = s2.split("&"),
        obj = {},
        pair,
        i;
    for (i in pairs) {
        if (pairs[i] === "") continue;

        pair = pairs[i].split("=");
        obj[decodeURIComponent(pair[0])] = decodeURIComponent(pair[1]);
    }
    return obj;
}
/* QueryString End */

/*  搜尋自動補字資料來源 START */
$.get('/StaticFiles/api/typehead_collection.json', function (data) {
    $("#top-search").typeahead({ source: data.feature });
}, 'json');
/* 搜尋自動補字資料來源 END */

/* 提醒視窗 START */
toastr.options = {
    "closeButton": true,
    "debug": false,
    "progressBar": false,
    "positionClass": "toast-top-center",
    "onclick": null,
    "showDuration": "400",
    "hideDuration": "1000",
    "timeOut": "3000",
    "extendedTimeOut": "1000",
    "showEasing": "swing",
    "hideEasing": "linear",
    "showMethod": "fadeIn",
    "hideMethod": "fadeOut"
}
/* 提醒視窗 END */

/* side menu sortable start */
var sideMenuTimeout;
function sideMenuStartTimeout($this, $item) {
    sideMenuTimeout = setTimeout(function () {
        $($item).removeClass("menu1 menu2 menu3");
        if ($this.hasClass("menu1btn"))
            $item.addClass("menu1");
        if ($this.hasClass("menu2btn"))
            $item.addClass("menu2");
        if ($this.hasClass("menu3btn"))
            $item.addClass("menu3");
        $this.find("a").trigger("click");
    }, 600);
}
function sideMenuStopTimeout() {
    clearTimeout(sideMenuTimeout);
}
function enableSideMenuSort() {
    $("#side-menu").sortable("enable");
    $("#side-menu").disableSelection();
}
function disableSideMenuSort() {
    $("#side-menu").sortable("disable")
    $("#side-menu").enableSelection();
}
$("#side-menu").sortable({
    items: ".sortable-container",
    //cancel: ".ui-state-disabled",
    activate: function (event, ui) {
        $(".menu3btn,.menu2btn,.menu1btn").css("z-index", "1001").mouseover(function () {
            $(this).css("opacity", "0.5");
            sideMenuStartTimeout($(this), $(ui.item));
        }).mouseout(function () {
            sideMenuStopTimeout();
            $(this).css("opacity", "1");
        });;
    },
    deactivate: function (event, ui) {
        $(".menu3btn,.menu2btn,.menu1btn").css("z-index", "auto").unbind("mouseover");
    },
    stop: function (event, ui) {
        var list = [];
        for (var i = 1; i <= 3; i++) {
            $("#side-menu li.menu" + i).each(function (ii, e) {
                var id = $(e).data("id");
                var pageNumber = i;
                //console.log(id);
                list.push({ ControllerMainID: id, PageNumber: pageNumber, Sort: ii + 1 });
            });
        }

        $.ajax({
            url: $("#NavERPDomainHid").val() + "Common/MenuSort",
            method: "post",
            xhrFields: { withCredentials: true },
            data: { List: list }
        }).done(function (data) {
            if (data.msg) {
                swarning(data.msg);
            }
        }).fail(function () {
            swerror("排序失敗");
        });
    }
});
disableSideMenuSort();
$("#sideMenuLock").click(function () {
    if ($(this).hasClass("fa-unlock")) {
        $(this).removeClass("fa-unlock").addClass("fa-lock");
        disableSideMenuSort();
    } else {
        $(this).removeClass("fa-lock").addClass("fa-unlock");
        Swal.fire({
            html: true,
            title: "成功解除鎖定",
            text: "請用滑鼠左鍵按下右側選單不放<br/>並拖曳至想放的位置或其他的Menu<br/><span style='color:red;'>(拖曳完成後「點選登出」重新登入才會生效)</span>",
            type: "success"
        });
        enableSideMenuSort();
    }
});
/* side menu sortable end */

/* upload file start */
$('.custom-file-input').on('change', function () {
    var fileName = $(this).val().split('\\').pop();
    $(this).next('.custom-file-label').addClass("selected").html(fileName);
});
/* upload file end */


/* custom block ui start */
//$.blockUI.defaults.message = "<div class=\"sk-spinner sk-spinner-three-bounce\"><div class=\"sk-bounce1\"></div><div class=\"sk-bounce2\"></div><div class=\"sk-bounce3\"></div></div>";
$.blockUI.defaults.message = "<div class=\"sk-spinner sk-spinner-fading-circle\"> <div class=\"sk-circle1 sk-circle\"></div> <div class=\"sk-circle2 sk-circle\"></div> <div class=\"sk-circle3 sk-circle\"></div> <div class=\"sk-circle4 sk-circle\"></div> <div class=\"sk-circle5 sk-circle\"></div> <div class=\"sk-circle6 sk-circle\"></div> <div class=\"sk-circle7 sk-circle\"></div> <div class=\"sk-circle8 sk-circle\"></div> <div class=\"sk-circle9 sk-circle\"></div> <div class=\"sk-circle10 sk-circle\"></div> <div class=\"sk-circle11 sk-circle\"></div> <div class=\"sk-circle12 sk-circle\"></div> </div>";
/* custom block ui end*/

/*搜尋條件 按鈕功能 start*/
$(".SearchClearBtn").click(function () {
    $form = $(this).closest('form');
    $form.find("select").each(function () { $(this).val(null).trigger('change'); });
    $form.find('input[type=text],input[type=number], textarea,input[name^=Help]').val('');
    $form.find('span[id^=Help]').text('');
    $form.find('input[type=checkbox],input[type=radio]').prop('checked', false);
});
$(".SearchShrinkBtn").click(function () {
    $('#accordion .panel-title a').trigger('click');
});
/*搜尋條件 按鈕功能 end*/

/* 作廢事件 swal start */
//$("#Abandon").click(function () {
//    var $this = $(this);
//    if ($this.prop("checked")) {
//        $this.prop("checked", false);
//        Swal.fire({
//            type: "warning",
//            html: true,
//            title: "確定要作廢嗎?",
//            text: "<form id='SwalAbandonForm' action='javascript:void(0)'><textarea id='SwalAbandonReasonTextArea' name='SwalAbandonReasonTextArea' style='width:100%;' row='3' placeholder='請輸入作廢原因*'></textarea></form>",
//            showCancelButton: true,
//            cancelButtonText: "取消",
//            confirmButtonColor: "#DD6B55",
//            confirmButtonText: "確定",
//            closeOnConfirm: false
//        }, function () {
//            $("#SwalAbandonForm").validate({
//                rules:
//                {
//                    SwalAbandonReasonTextArea: {
//                        required: true,
//                        maxlength: 200,
//                        noSpace: true
//                    }
//                }
//            });
//            if ($("#SwalAbandonForm").valid()) {
//                $("#AbandonReason").val($("#SwalAbandonReasonTextArea").val());
//                $this.prop("checked", true);
//                Swal.close();
//            } else {
//                $this.prop("checked", false);
//            }
//        });
//    } else {
//        $("#AbandonReason").val("");
//    }
//});
$("#Abandon").click(function () {
    var $this = $(this);
    if ($this.prop("checked")) {
        $this.prop("checked", false);
        Swal.fire({
            icon: "warning",
            html: true,
            title: "確定要作廢嗎?",
            html: "<form id='SwalAbandonForm'><textarea id='SwalAbandonReasonTextArea' style='width:100%;' rows='3' placeholder='請輸入作廢原因*'></textarea></form>",
            showCancelButton: true,
            cancelButtonText: "取消",
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "確定",
            showCloseButton: false,
            reverseButtons: true,
            preConfirm: () => {
                return new Promise((resolve) => {
                    if ($("#SwalAbandonForm").valid()) {
                        resolve();
                    } else {
                        reject("請輸入有效的作廢原因");
                    }
                });
            }
        }).then((result) => {
            if (result.isConfirmed) {
                $("#AbandonReason").val($("#SwalAbandonReasonTextArea").val());
                $this.prop("checked", true);
                Swal.close();
            } else {
                $this.prop("checked", false);
            }
        });

        $("#SwalAbandonForm").validate({
            rules: {
                SwalAbandonReasonTextArea: {
                    required: true,
                    maxlength: 200,
                    noSpace: true
                }
            }
        });
    } else {
        $("#AbandonReason").val("");
    }
});
/* 作廢事件 swal end */


/* swal start */
/*sweetaler舊版*/
//function swconfirm(msg, callback) {
//    Swal.fire({
//        type: "warning"
//        , title: msg
//        , showCancelButton: true
//        , cancelButtonText: "取消"
//        , confirmButtonText: "確定"
//    }, callback);
//}
/*sweetalert2版本*/
function swconfirm(msg, callback) {
    Swal.fire({
        icon: "warning",
        title: msg,
        showCancelButton: true,
        cancelButtonText: "取消",
        confirmButtonText: "確定"
    }).then((result) => {
        if (!result.isConfirmed)
            return;

        callback();
    });
}
/*sweetaler舊版*/
//function swerror(msg) {
//    swal({ type: "error", title: msg });
//}
/*sweetalert2版本*/
function swerror(msg) {
    Swal.fire({ icon: "error", title: msg });
}
/*sweetaler舊版*/
//function swarning(msg) {
//    swal({ type: "warning", title: msg });
//}
/*sweetalert2版本*/
function swarning(msg) {
    Swal.fire({ icon: "warning", title: msg });
}

/*sweetaler舊版*/
//function swDelList(url, callback) {
//    $("body").on("click", ".delBtn", function (e) {
//        var $this = $(this);
//        Swal.fire({
//            type: "warning",
//            html: true,
//            title: "確定要刪除嗎?",
//            text: "<form id='SwalDelForm' action='javascript:void(0)'><textarea id='SwalDelReasonTextArea' name='SwalDelReasonTextArea' style='width:100%;' row='3' placeholder='請輸入刪除原因*'></textarea></form>",
//            showCancelButton: true,
//            cancelButtonText: "取消",
//            confirmButtonColor: "#DD6B55",
//            confirmButtonText: "確定",
//            closeOnConfirm: false
//        }, function () {
//            $("#SwalDelForm").validate({
//                rules: {
//                    SwalDelReasonTextArea: {
//                        required: true,
//                        maxlength: 200,
//                        noSpace: true
//                    }
//                }
//            });
//            if ($("#SwalDelForm").valid()) {
//                $.blockUI();
//                $.ajax({
//                    method: "post",
//                    url: url,
//                    data: { reason: $("#SwalDelReasonTextArea").val(), id: $this.data("id"), __RequestVerificationToken: $("input[name=__RequestVerificationToken]").val() }
//                }).done(function (msg) {
//                    if (msg) {
//                        Swal.fire({ title: msg, type: "error" });
//                    } else {
//                        var currentPage = $(".page-block ul li.active a").data("page");
//                        var page = $("table tbody tr").length <= 1 ? +currentPage - 1 : currentPage;
//                        ListDataAjax(page, callback);
//                        swal.close();
//                    }
//                }).fail(function () {
//                    $.unblockUI();
//                    swal({ title: "系統發生錯誤", type: "error" });
//                }).always(function () {
//                    $.unblockUI();
//                })

//            }
//        });
//    });
//}

/*sweetalert2版本*/
function swDelList(url, callback) {
    $("body").on("click", ".delBtn", function (e) {
        var $this = $(this);
        Swal.fire({
            icon: "warning",
            title: "確定要刪除嗎?",
            html: "<form id='SwalDelForm' action='javascript:void(0)'>" +
                "<textarea id='SwalDelReasonTextArea' name='SwalDelReasonTextArea' " +
                "style='width:100%;' rows='3' placeholder='請輸入刪除原因*'></textarea></form>",
            showCancelButton: true,
            cancelButtonText: "取消",
            confirmButtonColor: "#DD6B55",
            confirmButtonText: "確定",
            preConfirm: () => {
               
                const reason = $("#SwalDelReasonTextArea").val();
                if (!reason || reason.trim() === "" || reason.length > 200) {
                    Swal.showValidationMessage("請輸入刪除原因 (1-200 字)");
                    return false;
                }
                return true;
            }
        }).then((result) => {
            if (result.isConfirmed) {
       
                $.blockUI();
                $.ajax({
                    method: "post",
                    url: url,
                    data: {
                        reason: $("#SwalDelReasonTextArea").val(),
                        id: $this.data("id"),
                        __RequestVerificationToken: $("input[name=__RequestVerificationToken]").val()
                    }
                }).done(function (msg) {
                    if (msg) {
                        Swal.fire({ title: msg, icon: "error" });
                    } else {
                        var currentPage = $(".page-block ul li.active a").data("page");
                        var page = $("table tbody tr").length <= 1 ? +currentPage - 1 : currentPage;
                        ListDataAjax(page, callback);
                        Swal.close();
                    }
                }).fail(function () {
                    $.unblockUI();
                    Swal.fire({ title: "系統發生錯誤", icon: "error" });
                }).always(function () {
                    $.unblockUI();
                });
            }
        });
    });
}


/*sweetaler舊版*/
//function swsuccess(msg) {
//    swal({ title: msg, type: "success" });
//}
/*sweetalert2版本*/
function swsuccess(msg) {
    Swal.fire({ title: msg, icon: "success" });
}
/* swal end */

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

/* 密碼過期提醒 start */
if ($("#PwExpireMsg").val()) {
    swarning($("#PwExpireMsg").val());
}
/* 密碼過期提醒 end */




