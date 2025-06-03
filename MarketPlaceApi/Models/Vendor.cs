using System.ComponentModel.DataAnnotations;

namespace MarketPlaceApi.Models
{
    public class Vendor
    {
        public string StoreName { get; set; }
        public string OwnerName { get; set; }
        public string BusinessEmail { get; set; }
        public string PasswordHash { get; set; }
        public string ConfirmPasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public bool IsApproved { get; set; } = false;
        public bool IsPending { get; set; } = true; 
        public bool AutoApproveProducts { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public List<Product> Products { get; set; }
    }
}