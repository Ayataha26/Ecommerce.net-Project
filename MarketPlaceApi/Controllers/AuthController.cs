using MarketPlaceApi.Models;
using MarketPlaceApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace MarketPlaceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register/customer")]
        public async Task<IActionResult> RegisterCustomer([FromBody] CustomerRegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _authService.RegisterCustomer(model);
        }

        [HttpPost("register/vendor")]
        public async Task<IActionResult> RegisterVendor([FromBody] VendorRegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _authService.RegisterVendor(model);
        }

        [HttpPost("login/customer")]
        public async Task<IActionResult> LoginCustomer([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _authService.LoginCustomer(model);
        }

        [HttpPost("login/vendor")]
        public async Task<IActionResult> LoginVendor([FromBody] LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            return await _authService.LoginVendor(model);
        }
    }
}