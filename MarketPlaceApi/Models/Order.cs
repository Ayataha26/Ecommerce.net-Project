namespace MarketPlaceApi.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; } // رقم التلفون للتوصيل
        public string? Comment { get; set; } // كومنت اختياري
        public DateTime OrderDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string Status { get; set; } = "Active";
        public List<OrderItem> OrderItems { get; set; }

        // Navigation Property
        public Customer Customer { get; set; }
    }
}