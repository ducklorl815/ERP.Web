namespace ERP.Web.Service.ViewModels
{
    public class OrdersAmountViewModel
    {
        public bool IsSuccess { get; set; }
        public List<List<string>> OrderDate { get; set; }
        public List<List<int>> OrderCount { get; set; }
        public List<List<int>> TotalAmount { get; set; }
        public List<List<int>> pre_TotalAmount { get; set; }
        public List<List<int>> TopAmount { get; set; }
    }
}
