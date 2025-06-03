namespace MarketPlaceApi.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }

        // Navigation Properties
        public Customer Customer { get; set; }
        public Product Product { get; set; }
    }
}