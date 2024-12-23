namespace ERP.Web.Models.Models
{
    public class SeatMapMainModel
    {
        public Guid ID { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public string Border { get; set; }
        public string BoxNumber { get; set; }
        public string Location { get; set; }
        public string Colorcode { get; set; }
        public string Status { get; set; }
        public string TicketID { get; set; }
        public DateTime ModifyDate { get; set; }
        public Guid ModifyUser { get; set; }
        public bool Enabled { get; set; }
        public bool Deleted { get; set; }
    }
}
