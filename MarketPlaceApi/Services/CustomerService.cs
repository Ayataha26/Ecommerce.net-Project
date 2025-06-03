using MarketPlaceApi.Models;
using MarketPlaceApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MarketPlaceApi.Services
{
    public class CustomerService
    {
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IGenericRepository<CartItem> _cartItemRepository;
        private readonly IGenericRepository<Order> _orderRepository;
        private readonly IGenericRepository<SavedProduct> _savedProductRepository;

        public CustomerService(
            IGenericRepository<Customer> customerRepository,
            IGenericRepository<Product> productRepository,
            IGenericRepository<CartItem> cartItemRepository,
            IGenericRepository<Order> orderRepository,
            IGenericRepository<SavedProduct> savedProductRepository)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
            _cartItemRepository = cartItemRepository;
            _orderRepository = orderRepository;
            _savedProductRepository = savedProductRepository;
        }

        public async Task<IActionResult> GetProducts(ClaimsPrincipal user)
        {
            string customerPhoneNumber = null;
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(customerEmail))
            {
                var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
                customerPhoneNumber = customers.FirstOrDefault()?.PhoneNumber;
            }

            var products = await _productRepository.FindWithIncludeAsync(
                p => p.IsApproved && !p.IsDeleted,
                p => p.Vendor
            );

            if (!products.Any())
                return new NotFoundObjectResult(new { Message = "No approved products found." });

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
                StoreName = p.Vendor != null ? p.Vendor.StoreName : "Unknown", // StoreName من الـ Vendor
                VendorPhoneNumber = p.Vendor != null ? p.Vendor.PhoneNumber : p.VendorPhoneNumber
            });

            return new OkObjectResult(result);
        }


        public async Task<IActionResult> GetProductDetails(int productId)
        {
            var product = await _productRepository.FindWithIncludeAsync(
                p => p.Id == productId && p.IsApproved && !p.IsDeleted,
                p => p.Vendor
            );

            var productSingle = product.FirstOrDefault();
            if (productSingle == null)
                return new NotFoundObjectResult(new { Message = "Product not found, not approved, or deleted." });

            // زيادة عدد المشاهدات
            productSingle.NumberOfViewers += 1;
            await _productRepository.UpdateAsync(productSingle);

            var result = new
            {
                productSingle.Id,
                productSingle.Title,
                productSingle.Description,
                productSingle.Price,
                productSingle.Category,
                productSingle.NumberOfAvailableUnits,
                productSingle.NumberOfViewers,
                Images = productSingle.Images, // إضافة الصور
                StoreName = productSingle.Vendor != null ? productSingle.Vendor.StoreName : "Unknown"
            };

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> SearchProducts(ClaimsPrincipal user, string category, decimal? minPrice, decimal? maxPrice)
        {
            string customerPhoneNumber = null;
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (!string.IsNullOrEmpty(customerEmail))
            {
                var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
                customerPhoneNumber = customers.FirstOrDefault()?.PhoneNumber;
            }

            var products = await _productRepository.FindWithIncludeAsync(
                p => p.IsApproved && !p.IsDeleted &&
                     (string.IsNullOrEmpty(category) || p.Category == category) &&
                     (!minPrice.HasValue || p.Price >= minPrice.Value) &&
                     (!maxPrice.HasValue || p.Price <= maxPrice.Value),
                p => p.Vendor
            );

            if (!products.Any())
                return new NotFoundObjectResult(new { Message = "No products found matching the criteria." });


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
                StoreName = p.Vendor != null ? p.Vendor.StoreName : "Unknown", // StoreName من الـ Vendor
                VendorPhoneNumber = p.Vendor != null ? p.Vendor.PhoneNumber : p.VendorPhoneNumber
            });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> AddToCart(ClaimsPrincipal user, CartItemDto cartItemDto)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var product = await _productRepository.GetByIdAsync(cartItemDto.ProductId);
            if (product == null || !product.IsApproved || product.IsDeleted)
                return new NotFoundObjectResult(new { Message = "Product not found, not approved, or deleted." });

            if (product.NumberOfAvailableUnits < cartItemDto.Quantity)
                return new BadRequestObjectResult(new { Message = "Not enough units available." });

            if (cartItemDto.Quantity <= 0)
                return new BadRequestObjectResult(new { Message = "Quantity must be greater than zero." });

            var existingCartItems = await _cartItemRepository.FindAsync(ci => ci.CustomerPhoneNumber == customerPhoneNumber && ci.ProductId == cartItemDto.ProductId);
            var existingCartItem = existingCartItems.FirstOrDefault();

            if (existingCartItem != null)
            {
                existingCartItem.Quantity += cartItemDto.Quantity;
                await _cartItemRepository.UpdateAsync(existingCartItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    ProductId = cartItemDto.ProductId,
                    Quantity = cartItemDto.Quantity,
                    CustomerPhoneNumber = customerPhoneNumber
                };
                await _cartItemRepository.AddAsync(cartItem);
            }

            return new OkObjectResult(new
            {
                Message = "Product added to cart successfully.",
                ProductId = cartItemDto.ProductId,
                ProductTitle = product.Title
            });
        }

        public async Task<IActionResult> ViewCart(ClaimsPrincipal user)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var cartItems = await _cartItemRepository.FindWithIncludeAsync(
                ci => ci.CustomerPhoneNumber == customerPhoneNumber,
                ci => ci.Product);

            var result = cartItems.Select(ci => new
            {
                CartItemId = ci.Id,
                ProductId = ci.ProductId,
                ProductName = ci.Product != null ? ci.Product.Title : "Unknown Product",
                Quantity = ci.Quantity,
                Price = ci.Product != null ? ci.Product.Price : 0,
                TotalPrice = ci.Product != null ? ci.Quantity * ci.Product.Price : 0,
                Category = ci.Product != null ? ci.Product.Category : "Unknown",
                Images = ci.Product != null ? ci.Product.Images : null,
                IsDeleted = ci.Product != null && ci.Product.IsDeleted
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "Cart is empty." });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> RemoveFromCart(ClaimsPrincipal user, int cartItemId)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var cartItems = await _cartItemRepository.FindAsync(ci => ci.Id == cartItemId && ci.CustomerPhoneNumber == customerPhoneNumber);
            var cartItem = cartItems.FirstOrDefault();
            if (cartItem == null)
                return new NotFoundObjectResult(new { Message = "Cart item not found or you are not authorized to remove it." });

            await _cartItemRepository.DeleteAsync(cartItem);

            return new OkObjectResult(new { Message = "Cart item removed successfully." });
        }

        public async Task<IActionResult> Checkout(ClaimsPrincipal user, CheckoutModel checkoutModel)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            if (checkoutModel == null || string.IsNullOrWhiteSpace(checkoutModel.Address) || string.IsNullOrWhiteSpace(checkoutModel.PhoneNumber))
                return new BadRequestObjectResult(new { Message = "Address and Phone Number are required." });

            // التحقق من صيغة رقم التلفون
            var phoneRegex = new Regex(@"^\+?\d{10,15}$");
            if (!phoneRegex.IsMatch(checkoutModel.PhoneNumber))
                return new BadRequestObjectResult(new { Message = "Invalid phone number format. It should be 10-15 digits, optionally starting with +." });

            // كل الـ cart items
            var cartItems = await _cartItemRepository.FindWithIncludeAsync(
                ci => ci.CustomerPhoneNumber == customerPhoneNumber,
                ci => ci.Product
            );

            if (!cartItems.Any())
                return new BadRequestObjectResult(new { Message = "Cart is empty." });

            foreach (var item in cartItems)
            {
                if (item.Product == null || !item.Product.IsApproved || item.Product.IsDeleted)
                    return new BadRequestObjectResult(new { Message = $"Product {item.ProductId} is not available, not approved, or deleted." });
                if (item.Product.NumberOfAvailableUnits < item.Quantity)
                    return new BadRequestObjectResult(new { Message = $"Not enough units available for product: {item.Product.Title}." });
            }

            var order = new Order
            {
                CustomerPhoneNumber = customerPhoneNumber,
                Address = checkoutModel.Address,
                PhoneNumber = checkoutModel.PhoneNumber, // رقم التلفون الجديد
                Comment = checkoutModel.Comment, // الكومنت
                OrderDate = DateTime.UtcNow,
                TotalPrice = cartItems.Sum(ci => ci.Quantity * ci.Product.Price),
                Status = "Active",
                OrderItems = cartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    Price = ci.Product.Price
                }).ToList()
            };

            foreach (var item in cartItems)
            {
                item.Product.NumberOfAvailableUnits -= item.Quantity;
                await _productRepository.UpdateAsync(item.Product);
            }

            await _orderRepository.AddAsync(order);
            foreach (var item in cartItems)
            {
                await _cartItemRepository.DeleteAsync(item);
            }

            return new OkObjectResult(new { Message = "Checkout completed successfully.", OrderId = order.Id });
        }
        public async Task<IActionResult> SaveProduct(ClaimsPrincipal user, SaveProductDto saveProductDto)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var product = await _productRepository.GetByIdAsync(saveProductDto.ProductId);
            if (product == null || !product.IsApproved || product.IsDeleted)
                return new NotFoundObjectResult(new { Message = "Product not found, not approved, or deleted." });

            var existingSavedProducts = await _savedProductRepository.FindAsync(sp => sp.CustomerPhoneNumber == customerPhoneNumber && sp.ProductId == saveProductDto.ProductId);
            var existingSavedProduct = existingSavedProducts.FirstOrDefault();
            if (existingSavedProduct != null)
                return new BadRequestObjectResult(new { Message = "Product already saved." });

            var savedProduct = new SavedProduct
            {
                ProductId = saveProductDto.ProductId,
                CustomerPhoneNumber = customerPhoneNumber
            };

            await _savedProductRepository.AddAsync(savedProduct);

            return new OkObjectResult(new { Message = "Product saved successfully." });
        }

        public async Task<IActionResult> ViewSavedProducts(ClaimsPrincipal user)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var savedProducts = await _savedProductRepository.FindWithIncludeAsync(
                sp => sp.CustomerPhoneNumber == customerPhoneNumber,
                sp => sp.Product);

            var result = savedProducts.Select(sp => new
            {
                ProductId = sp.ProductId,
                ProductName = sp.Product != null ? (sp.Product.Title ?? "Unknown Product") : "Unknown Product",
                Price = sp.Product != null ? sp.Product.Price : 0,
                Category = sp.Product != null ? (sp.Product.Category ?? "Unknown") : "Unknown",
                Images = sp.Product != null ? (sp.Product.Images ?? null) : null,
                NumberOfViewers = sp.Product != null ? sp.Product.NumberOfViewers : 0,
                IsDeleted = sp.Product != null && sp.Product.IsDeleted
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "No saved products found." });

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> RemoveSavedProduct(ClaimsPrincipal user, int productId)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var savedProducts = await _savedProductRepository.FindAsync(sp => sp.ProductId == productId && sp.CustomerPhoneNumber == customerPhoneNumber);
            var savedProduct = savedProducts.FirstOrDefault();
            if (savedProduct == null)
                return new NotFoundObjectResult(new { Message = "Saved product not found or you are not authorized to remove it." });

            await _savedProductRepository.DeleteAsync(savedProduct);

            return new OkObjectResult(new { Message = "Saved product removed successfully." });
        }

        public async Task<IActionResult> ViewOrderHistory(ClaimsPrincipal user)
        {
            var customerEmail = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(customerEmail))
                return new UnauthorizedObjectResult(new { Message = "Customer not authenticated." });

            var customers = await _customerRepository.FindAsync(c => c.Email == customerEmail);
            var customer = customers.FirstOrDefault();
            if (customer == null)
                return new UnauthorizedObjectResult(new { Message = "Customer not found." });

            var customerPhoneNumber = customer.PhoneNumber;

            var orders = await _orderRepository.FindWithNestedIncludeAsync(
                o => o.CustomerPhoneNumber == customerPhoneNumber,
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
                Address = o.Address,
                PhoneNumber = o.PhoneNumber, // رقم التلفون بتاع الـ checkout
                Comment = o.Comment, // الكومنت
                Items = o.OrderItems.Select(oi => new
                {
                    ProductId = oi.ProductId,
                    ProductName = oi.Product != null ? oi.Product.Title : "Unknown Product",
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    TotalPrice = oi.Quantity * oi.Price,
                    Category = oi.Product != null ? oi.Product.Category : "Unknown",
                    Images = oi.Product != null ? oi.Product.Images : null
                })
            }).ToList();

            if (!result.Any())
                return new NotFoundObjectResult(new { Message = "No orders found." });

            return new OkObjectResult(result);
        }
    }
}