using System.ComponentModel.DataAnnotations;

namespace MarketPlaceApi.Models
{
    public class Customer
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string ConfirmPasswordHash { get; set; }
        public string PhoneNumber { get; set; }

        // أضفت الـ Navigation Properties لأنها ضرورية للعلاقات في Entity Framework
        public List<CartItem> CartItems { get; set; }
        public List<Order> Orders { get; set; }
        public List<SavedProduct> SavedProducts { get; set; }
    }
}