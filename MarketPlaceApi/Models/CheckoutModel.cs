namespace MarketPlaceApi.Models
{
    public class CheckoutModel
    {
        public string Address { get; set; }
        public string PhoneNumber { get; set; } // رقم التلفون للتوصيل
        public string? Comment { get; set; } // كومنت اختياري
    }
}