namespace ERP.Web.Service.ViewModels.Account
{
    public class AccountLoginViewModel_result : AccountLoginViewModel_param
    {

        /// <summary>員工ID</summary>
        public string EmpID { get; set; }

        /// <summary>員工名稱</summary>
        public string EmpName { get; set; }

        public bool IsSuccess { get; set; }

        public string Msg { get; set; }
    }

    public class AccountLoginViewModel_param
    {
        /// <summary>帳號</summary>
        public string Account { get; set; }

        /// <summary>密碼</summary>
        public string Password { get; set; }

        /// <summary></summary>
        public string ReturnUrl { get; set; }

        /// <summary>自動登入</summary>
        public bool AutoLogin { get; set; }
    }

   
}