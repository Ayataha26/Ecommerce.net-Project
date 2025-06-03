using MarketPlaceApi.Hubs;
using MarketPlaceApi.Models;
using MarketPlaceApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace MarketPlaceApi.Services
{
    public class AdminService
    {
        private readonly IGenericRepository<Vendor> _vendorRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminService(
            IGenericRepository<Vendor> vendorRepository,
            IGenericRepository<Product> productRepository,
            IHubContext<NotificationHub> hubContext)
        {
            _vendorRepository = vendorRepository;
            _productRepository = productRepository;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> GetAllVendors()
        {
            var vendors = await _vendorRepository.FindAsync(v => true);
            if (!vendors.Any())
                return new NotFoundObjectResult(new { Message = "No vendors found." });

            var result = vendors.Select(v => new
            {
                v.OwnerName,
                v.PhoneNumber,
                v.IsApproved,
                v.IsPending,
                v.IsEnabled,
                v.AutoApproveProducts
            });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetPendingProducts()
        {
            var products = await _productRepository.FindWithIncludeAsync(
                p => p.IsPending && !p.IsDeleted,
                p => p.Vendor
            );
            if (!products.Any())
                return new NotFoundObjectResult(new { Message = "No pending products found." });

            var result = products.Select(p => new
            {
                p.Id,
                p.Title,
                p.Category,
                p.Price,
                OwnerName = p.Vendor != null ? p.Vendor.OwnerName : "Unknown"
            });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> ApproveVendor(string phoneNumber)
        {
            var vendor = await _vendorRepository.GetByIdAsync(phoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsApproved)
                return new BadRequestObjectResult(new { Message = "Vendor is already approved." });

            vendor.IsApproved = true;
            vendor.IsPending = false;
            await _vendorRepository.UpdateAsync(vendor);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", "Your account has been approved.");

            return new OkObjectResult(new { Message = "Vendor approved successfully." });
        }

        public async Task<IActionResult> DisapproveVendor(string phoneNumber)
        {
            var vendor = await _vendorRepository.GetByIdAsync(phoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (!vendor.IsApproved && !vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor is already disapproved." });

            vendor.IsApproved = false;
            vendor.IsPending = false;
            await _vendorRepository.UpdateAsync(vendor);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", "Your account has been disapproved.");

            return new OkObjectResult(new { Message = "Vendor disapproved successfully." });
        }

        public async Task<IActionResult> EnableVendor(string phoneNumber)
        {
            var vendor = await _vendorRepository.GetByIdAsync(phoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsEnabled)
                return new BadRequestObjectResult(new { Message = "Vendor is already enabled." });

            vendor.IsEnabled = true;
            await _vendorRepository.UpdateAsync(vendor);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", "Your account has been enabled.");

            return new OkObjectResult(new { Message = "Vendor enabled successfully." });
        }

        public async Task<IActionResult> DisableVendor(string phoneNumber)
        {
            var vendor = await _vendorRepository.GetByIdAsync(phoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (!vendor.IsEnabled)
                return new BadRequestObjectResult(new { Message = "Vendor is already disabled." });

            vendor.IsEnabled = false;
            await _vendorRepository.UpdateAsync(vendor);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", "Your account has been disabled.");

            return new OkObjectResult(new { Message = "Vendor disabled successfully." });
        }

        public async Task<IActionResult> SetAutoApproveProducts(string phoneNumber, AutoApproveModel model)
        {
            if (model == null)
                return new BadRequestObjectResult(new { Message = "Invalid request body." });

            var vendor = await _vendorRepository.GetByIdAsync(phoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Cannot set auto-approve for a pending vendor." });

            if (vendor.AutoApproveProducts == model.AutoApprove)
                return new BadRequestObjectResult(new { Message = $"Auto-approve products is already set to {model.AutoApprove}." });

            vendor.AutoApproveProducts = model.AutoApprove;
            await _vendorRepository.UpdateAsync(vendor);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync(
                "ReceiveNotification",
                "Vendor",
                $"Auto-approve products has been set to {model.AutoApprove}."
            );

            return new OkObjectResult(new { Message = "Auto-approve products setting updated successfully." });
        }

        public async Task<IActionResult> SetAutoApproveAllVendors(AutoApproveModel model)
        {
            if (model == null)
                return new BadRequestObjectResult(new { Message = "Invalid request body." });

            var vendors = await _vendorRepository.FindAsync(v => !v.IsPending);
            if (!vendors.Any())
                return new NotFoundObjectResult(new { Message = "No non-pending vendors found." });

            foreach (var vendor in vendors)
            {
                vendor.AutoApproveProducts = model.AutoApprove;
                await _vendorRepository.UpdateAsync(vendor);

                await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync(
                    "ReceiveNotification",
                    "Vendor",
                    $"Auto-approve products for all vendors has been set to {model.AutoApprove}."
                );
            }

            return new OkObjectResult(new { Message = "Auto-approve setting updated for all non-pending vendors successfully." });
        }

        public async Task<IActionResult> AcceptProduct(int productId)
        {
            var product = await _productRepository.FindAsync(p => p.Id == productId && !p.IsDeleted);
            var productSingle = product.FirstOrDefault();
            if (productSingle == null)
                return new NotFoundObjectResult(new { Message = "Product not found or has been deleted." });

            if (productSingle.IsApproved)
                return new BadRequestObjectResult(new { Message = "Product is already accepted." });

            var vendor = await _vendorRepository.GetByIdAsync(productSingle.VendorPhoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            productSingle.IsApproved = true;
            productSingle.IsPending = false;
            productSingle.IsRejected = false;
            await _productRepository.UpdateAsync(productSingle);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", $"Your product '{productSingle.Title}' has been accepted.");

            return new OkObjectResult(new { Message = "Product accepted successfully." });
        }

        public async Task<IActionResult> RejectProduct(int productId)
        {
            var product = await _productRepository.FindAsync(p => p.Id == productId && !p.IsDeleted);
            var productSingle = product.FirstOrDefault();
            if (productSingle == null)
                return new NotFoundObjectResult(new { Message = "Product not found or has been deleted." });

            if (productSingle.IsRejected)
                return new BadRequestObjectResult(new { Message = "Product is already rejected." });

            var vendor = await _vendorRepository.GetByIdAsync(productSingle.VendorPhoneNumber);
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            productSingle.IsRejected = true;
            productSingle.IsPending = false;
            productSingle.IsApproved = false;
            await _productRepository.UpdateAsync(productSingle);

            await _hubContext.Clients.Group(vendor.BusinessEmail).SendAsync("ReceiveNotification", "Vendor", $"Your product '{productSingle.Title}' has been rejected.");

            return new OkObjectResult(new { Message = "Product rejected successfully." });
        }
    }
}