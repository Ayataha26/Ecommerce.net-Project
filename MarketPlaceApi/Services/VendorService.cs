using MarketPlaceApi.Models;
using MarketPlaceApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MarketPlaceApi.Services
{
    public class VendorService
    {
        private readonly IGenericRepository<Vendor> _vendorRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<Order> _orderRepository;

        public VendorService(
            IGenericRepository<Vendor> vendorRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<Order> orderRepository)
        {
            _vendorRepository = vendorRepository;
            _productRepository = productRepository;
            _orderRepository = orderRepository;
        }

        public async Task<IActionResult> AddProduct(ClaimsPrincipal user, AddProductModel model)
        {
            if (model == null)
                return new BadRequestObjectResult(new { Message = "Product data is null." });

            if (string.IsNullOrWhiteSpace(model.Title))
                return new BadRequestObjectResult(new { Message = "Title is required." });

            if (model.Price <= 0)
                return new BadRequestObjectResult(new { Message = "Price must be greater than zero." });

            if (model.NumberOfAvailableUnits < 0)
                return new BadRequestObjectResult(new { Message = "Number of available units cannot be negative." });

            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new NotFoundObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            if (!vendor.IsApproved)
                return new BadRequestObjectResult(new { Message = "Vendor is not approved." });

            if (!vendor.IsEnabled)
                return new BadRequestObjectResult(new { Message = "Vendor is disabled." });

            var product = new Product
            {
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                Category = model.Category,
                Images = model.Images,
                NumberOfAvailableUnits = model.NumberOfAvailableUnits,
                VendorPhoneNumber = vendor.PhoneNumber,
                IsPending = !vendor.AutoApproveProducts,
                IsApproved = vendor.AutoApproveProducts,
                IsRejected = false,
                IsDeleted = false,
                NumberOfViewers = 0
            };

            await _productRepository.AddAsync(product);

            if (product.IsApproved)
                return new OkObjectResult(new { Message = "Product added successfully and approved automatically.", ProductId = product.Id });

            return new OkObjectResult(new { Message = "Product added successfully and is awaiting approval.", ProductId = product.Id });
        }
        public async Task<IActionResult> ListProducts(ClaimsPrincipal user)
        {
            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new UnauthorizedObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            var products = await _productRepository.FindWithIncludeAsync(
                p => p.VendorPhoneNumber == vendor.PhoneNumber,
                p => p.Vendor
            );

            var result = products.Select(p => new
            {
                p.Id,
                p.Title,
                p.Description,
                p.Price,
                p.Category,
                p.Images,
                p.NumberOfAvailableUnits,
                p.NumberOfViewers,
                p.IsPending,
                p.IsApproved,
                p.IsRejected,
                p.IsDeleted,
                StoreName = p.Vendor != null ? p.Vendor.StoreName : "Unknown", // StoreName من الـ Vendor
                p.VendorPhoneNumber,
                OwnerName = p.Vendor != null ? p.Vendor.OwnerName : "Unknown"
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "No products found for this vendor." });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> UpdateProduct(ClaimsPrincipal user, int productId, UpdateProductModel model)
        {
            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new UnauthorizedObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            var products = await _productRepository.FindAsync(p => p.Id == productId && p.VendorPhoneNumber == vendor.PhoneNumber);
            var product = products.FirstOrDefault();
            if (product == null)
                return new NotFoundObjectResult(new { Message = "Product not found or you are not authorized to update it." });

            if (product.IsDeleted)
                return new BadRequestObjectResult(new { Message = "Cannot update a deleted product." });

            if (product.IsRejected)
                return new BadRequestObjectResult(new { Message = "This product is rejected, it cannot be updated." });

            if (model.Price.HasValue && model.Price.Value <= 0)
                return new BadRequestObjectResult(new { Message = "Price must be greater than zero." });

            if (model.NumberOfAvailableUnits.HasValue && model.NumberOfAvailableUnits.Value < 0)
                return new BadRequestObjectResult(new { Message = "Number of available units cannot be negative." });

            if (model.Title != null && string.IsNullOrWhiteSpace(model.Title))
                return new BadRequestObjectResult(new { Message = "Title cannot be empty." });

            product.Title = model.Title ?? product.Title;
            product.Description = model.Description ?? product.Description;
            product.Price = model.Price ?? product.Price;
            product.Category = model.Category ?? product.Category;
            product.Images = model.Images ?? product.Images;
            product.NumberOfAvailableUnits = model.NumberOfAvailableUnits ?? product.NumberOfAvailableUnits;

            await _productRepository.UpdateAsync(product);

            return new OkObjectResult(new { Message = "Product updated successfully." });
        }

        public async Task<IActionResult> DeleteProduct(ClaimsPrincipal user, int productId)
        {
            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new UnauthorizedObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            var products = await _productRepository.FindAsync(p => p.Id == productId && p.VendorPhoneNumber == vendor.PhoneNumber);
            var product = products.FirstOrDefault();
            if (product == null)
                return new NotFoundObjectResult(new { Message = "Product not found or you are not authorized to delete it." });

            if (product.IsDeleted)
                return new BadRequestObjectResult(new { Message = "Product is already deleted." });

            product.IsDeleted = true;
            await _productRepository.UpdateAsync(product);

            return new OkObjectResult(new { Message = "Product deleted successfully." });
        }

        public async Task<IActionResult> ViewOrders(ClaimsPrincipal user)
        {
            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new UnauthorizedObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            var orders = await _orderRepository.FindWithNestedIncludeAsync(
                o => o.OrderItems.Any(oi => oi.Product.VendorPhoneNumber == vendor.PhoneNumber),
                new IncludeExpression<Order>
                {
                    Include = o => o.OrderItems,
                    ThenIncludes = new List<Expression<Func<object, object>>> { oi => (oi as OrderItem).Product }
                }
            );

            var result = orders.Select(o => new
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                TotalPrice = o.TotalPrice,
                Status = o.Status,
                CustomerPhoneNumber = o.CustomerPhoneNumber,
                Address = o.Address,
                Items = o.OrderItems
                    .Where(oi => oi.Product.VendorPhoneNumber == vendor.PhoneNumber)
                    .Select(oi => new
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Title,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        TotalPrice = oi.Quantity * oi.Price
                    })
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "No orders found for this vendor." });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> ViewOrdersForProduct(ClaimsPrincipal user, int productId)
        {
            var vendorEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(vendorEmail))
                return new UnauthorizedObjectResult(new { Message = "Vendor not authenticated." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == vendorEmail);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null)
                return new UnauthorizedObjectResult(new { Message = "Vendor not found." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            // نتأكد إن المنتج ده بتاع البائع ده
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.VendorPhoneNumber != vendor.PhoneNumber)
                return new NotFoundObjectResult(new { Message = "Product not found or does not belong to this vendor." });

            // نجيب الطلبات اللي فيها المنتج ده باستخدام الدالة الجديدة
            var orders = await _orderRepository.FindWithNestedIncludeAsync(
                o => o.OrderItems.Any(oi => oi.ProductId == productId),
                new IncludeExpression<Order>
                {
                    Include = o => o.OrderItems,
                    ThenIncludes = new List<Expression<Func<object, object>>> { oi => (oi as OrderItem).Product }
                },
                new IncludeExpression<Order>
                {
                    Include = o => o.Customer
                }
            );

            var result = orders.Select(o => new
            {
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                CustomerName = o.Customer?.FullName ?? "Unknown",
                CustomerPhoneNumber = o.CustomerPhoneNumber,
                Items = o.OrderItems
                    .Where(oi => oi.ProductId == productId)
                    .Select(oi => new
                    {
                        ProductId = oi.ProductId,
                        ProductName = oi.Product.Title,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        TotalPrice = oi.Quantity * oi.Price
                    })
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "No orders found for this product." });

            return new OkObjectResult(result);
        }
    }
}