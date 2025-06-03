using MarketPlaceApi.Services;
using Microsoft.AspNetCore.Mvc;
using MarketPlaceApi.Models;

namespace MarketPlaceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _adminService;

        public AdminController(AdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("vendors")]
        public async Task<IActionResult> GetAllVendors()
        {
            return await _adminService.GetAllVendors();
        }

        [HttpGet("products/pending")]
        public async Task<IActionResult> GetPendingProducts()
        {
            return await _adminService.GetPendingProducts();
        }

        [HttpPut("vendors/{phoneNumber}/approve")]
        public async Task<IActionResult> ApproveVendor(string phoneNumber)
        {
            return await _adminService.ApproveVendor(phoneNumber);
        }

        [HttpPut("vendors/{phoneNumber}/disapprove")]
        public async Task<IActionResult> DisapproveVendor(string phoneNumber)
        {
            return await _adminService.DisapproveVendor(phoneNumber);
        }

        [HttpPut("vendors/{phoneNumber}/enable")]
        public async Task<IActionResult> EnableVendor(string phoneNumber)
        {
            return await _adminService.EnableVendor(phoneNumber);
        }

        [HttpPut("vendors/{phoneNumber}/disable")]
        public async Task<IActionResult> DisableVendor(string phoneNumber)
        {
            return await _adminService.DisableVendor(phoneNumber);
        }

        [HttpPut("vendors/{phoneNumber}/auto-approve-products")]
        public async Task<IActionResult> SetAutoApproveProducts(string phoneNumber, [FromBody] AutoApproveModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _adminService.SetAutoApproveProducts(phoneNumber, model);
        }

        [HttpPut("vendors/auto-approve-all")]
        public async Task<IActionResult> SetAutoApproveAllVendors([FromBody] AutoApproveModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _adminService.SetAutoApproveAllVendors(model);
        }

        [HttpPut("products/{productId}/accept")]
        public async Task<IActionResult> AcceptProduct(int productId)
        {
            return await _adminService.AcceptProduct(productId);
        }

        [HttpPut("products/{productId}/reject")]
        public async Task<IActionResult> RejectProduct(int productId)
        {
            return await _adminService.RejectProduct(productId);
        }
    }
}