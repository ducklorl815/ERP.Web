﻿namespace ERP.Web.Models.Models
{
    public class OrdersAmountMainModel
    {
        public string OrderID { get; set; }
        public int TopAmount { get; set; }
        public int TotalAmount { get; set; }
        public int OrderCount { get; set; }
        public DateTime OrderDate { get; set; }
        public Guid SalerID { get; set; }
    }
    public class ChartOrderMainModel
    {
        public string OrderID { get; set; }
        public int OrderAmount { get; set; }
        public Guid SalerID { get; set; }
        public DateTime OrderDate { get; set; }
        public Guid ModifyUser { get; set; }
        public DateTime ModifyDate { get; set; }
    }
    public class ChartsIndexMainModel
    {
        public string OrderID { get; set; }
        public int OrderAmount { get; set; }
        public DateTime OrderDate { get; set; }
    }
}
