namespace MarketPlaceApi.Models
{
    public class SavedProduct
    {
        public string CustomerPhoneNumber { get; set; }
        public int ProductId { get; set; }

        // Navigation Properties
        public Customer Customer { get; set; }
        public Product Product { get; set; }
    }
}