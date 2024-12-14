namespace ERP.Web.Models.Models
{
    public class OrdersAmountMainModel
    {
        public Guid ID { get; set; }
        public DateTime OrderDate { get; set; }
        public int Amount { get; set; }
        public int Count { get; set; }

    }
}
