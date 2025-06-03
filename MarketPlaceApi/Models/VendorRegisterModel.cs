using System.ComponentModel.DataAnnotations;

namespace MarketPlaceApi.Models
{
    public class VendorRegisterModel
    {
        [Required(ErrorMessage = "Store Name is required.")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Owner Name is required.")]
        public string OwnerName { get; set; }

        [Required(ErrorMessage = "Business Email is required.")]
        public string BusinessEmail { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required.")]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Phone Number is required.")]
        public string PhoneNumber { get; set; }
    }
}