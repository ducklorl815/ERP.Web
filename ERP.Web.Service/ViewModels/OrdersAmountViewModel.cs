namespace ERP.Web.Service.ViewModels
{
    public class OrdersAmountViewModel
    {
        public bool IsSuccess { get; set; }
        public List<List<int>> Amount { get; set; }
        public List<List<int>> Count { get; set; }
    }
}
