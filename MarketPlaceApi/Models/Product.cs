using System.ComponentModel.DataAnnotations;
using System.Numerics;

namespace MarketPlaceApi.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string VendorPhoneNumber { get; set; }
        public string Category { get; set; }
        public string Images { get; set; }
        public int NumberOfAvailableUnits { get; set; }
        public int NumberOfViewers { get; set; }
        public bool IsPending { get; set; } = true; // Default to Pending
        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        public Vendor Vendor { get; set; }

        // Navigation Properties
        public List<CartItem> CartItems { get; set; }
        public List<OrderItem> OrderItems { get; set; }
        public List<SavedProduct> SavedProducts { get; set; }
    }
}