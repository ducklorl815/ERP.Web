/*不能為空白*/
jQuery.validator.addMethod("noSpace", function (value, element) {
    return value == "" || value.trim() != "";
}, "No space please and don't leave it empty");