using MarketPlaceApi.Models;
using MarketPlaceApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarketPlaceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VendorController : ControllerBase
    {
        private readonly VendorService _vendorService;

        public VendorController(VendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpPost("addproduct")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> AddProduct([FromBody] AddProductModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _vendorService.AddProduct(User, model);
        }

        [HttpGet("listproducts")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> ListProducts()
        {
            return await _vendorService.ListProducts(User);
        }

        [HttpPut("updateproduct/{productId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> UpdateProduct(int productId, [FromBody] UpdateProductModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _vendorService.UpdateProduct(User, productId, model);
        }

        [HttpDelete("deleteproduct/{productId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> DeleteProduct(int productId)
        {
            return await _vendorService.DeleteProduct(User, productId);
        }

        [HttpGet("orders")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> ViewOrders()
        {
            return await _vendorService.ViewOrders(User);
        }

        [HttpGet("orders/product/{productId}")]
        [Authorize(Roles = "Vendor")]
        public async Task<IActionResult> ViewOrdersForProduct(int productId)
        {
            return await _vendorService.ViewOrdersForProduct(User, productId);
        }
    }
}