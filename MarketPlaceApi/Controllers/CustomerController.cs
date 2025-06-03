using MarketPlaceApi.Models;
using MarketPlaceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketPlaceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomerController : ControllerBase
    {
        private readonly CustomerService _customerService;

        public CustomerController(CustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            return await _customerService.GetProducts(User);
        }

        [HttpGet("products/{productId}")]
        public async Task<IActionResult> GetProductDetails(int productId)
        {
            return await _customerService.GetProductDetails(productId);
        }

        [HttpGet("products/search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string category, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice)
        {
            return await _customerService.SearchProducts(User, category, minPrice, maxPrice);
        }

        [HttpPost("cart")]
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] CartItemDto cartItemDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _customerService.AddToCart(User, cartItemDto);
        }

        [HttpGet("cart")]
        [Authorize]
        public async Task<IActionResult> ViewCart()
        {
            return await _customerService.ViewCart(User);
        }

        [HttpDelete("cart/{cartItemId}")]
        [Authorize]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            return await _customerService.RemoveFromCart(User, cartItemId);
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] CheckoutModel checkoutModel)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _customerService.Checkout(User, checkoutModel);
        }

        [HttpPost("savedproducts")]
        [Authorize]
        public async Task<IActionResult> SaveProduct([FromBody] SaveProductDto saveProductDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _customerService.SaveProduct(User, saveProductDto);
        }

        [HttpGet("savedproducts")]
        [Authorize]
        public async Task<IActionResult> ViewSavedProducts()
        {
            return await _customerService.ViewSavedProducts(User);
        }

        [HttpDelete("savedproducts/{productId}")]
        [Authorize]
        public async Task<IActionResult> RemoveSavedProduct(int productId)
        {
            return await _customerService.RemoveSavedProduct(User, productId);
        }

        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> ViewOrderHistory()
        {
            return await _customerService.ViewOrderHistory(User);
        }
    }
}