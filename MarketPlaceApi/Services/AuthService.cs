using MarketPlaceApi.Hubs;
using MarketPlaceApi.Models;
using MarketPlaceApi.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using BCrypt.Net;

namespace MarketPlaceApi.Services
{
    public class AuthService
    {
        private readonly IGenericRepository<Customer> _customerRepository;
        private readonly IGenericRepository<Vendor> _vendorRepository;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AuthService(
            IGenericRepository<Customer> customerRepository,
            IGenericRepository<Vendor> vendorRepository,
            IConfiguration configuration,
            IHubContext<NotificationHub> hubContext)
        {
            _customerRepository = customerRepository;
            _vendorRepository = vendorRepository;
            _configuration = configuration;
            _hubContext = hubContext;
        }

        public async Task<IActionResult> RegisterCustomer(CustomerRegisterModel model)
        {
            if (model == null)
                return new BadRequestObjectResult(new { Message = "Request body is empty or invalid." });

            if (string.IsNullOrWhiteSpace(model.FullName) || model.FullName.Length < 3 || model.FullName.Length > 100)
                return new BadRequestObjectResult(new { Message = "FullName must be between 3 and 100 characters." });

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
            if (!passwordRegex.IsMatch(model.Password))
                return new BadRequestObjectResult(new { Message = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, and one number." });

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(model.Email))
                return new BadRequestObjectResult(new { Message = "Invalid email format." });

            if (!model.Email.EndsWith("@marketplace.com"))
                return new BadRequestObjectResult(new { Message = "Email must end with @marketplace.com." });

            var existingCustomerByEmail = await _customerRepository.FindAsync(c => c.Email == model.Email);
            if (existingCustomerByEmail.Any())
                return new BadRequestObjectResult(new { Message = "Email already exists." });

            var phoneRegex = new Regex(@"^\+?\d{10,15}$");
            if (!phoneRegex.IsMatch(model.PhoneNumber))
                return new BadRequestObjectResult(new { Message = "Invalid phone number format. It should be 10-15 digits, optionally starting with +." });

            var existingCustomerByPhone = await _customerRepository.FindAsync(c => c.PhoneNumber == model.PhoneNumber);
            if (existingCustomerByPhone.Any())
                return new BadRequestObjectResult(new { Message = "Phone number already exists." });

            var customer = new Customer
            {
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName,
                Email = model.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                ConfirmPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.ConfirmPassword)
            };

            await _customerRepository.AddAsync(customer);

            return new OkObjectResult(new { Message = "Customer registered successfully." });
        }

        public async Task<IActionResult> RegisterVendor(VendorRegisterModel model)
        {
            if (model == null)
                return new BadRequestObjectResult(new { Message = "Request body is empty or invalid." });

            if (string.IsNullOrWhiteSpace(model.StoreName) || model.StoreName.Length < 3 || model.StoreName.Length > 100)
                return new BadRequestObjectResult(new { Message = "StoreName must be between 3 and 100 characters." });

            if (string.IsNullOrWhiteSpace(model.OwnerName) || model.OwnerName.Length < 3 || model.OwnerName.Length > 100)
                return new BadRequestObjectResult(new { Message = "OwnerName must be between 3 and 100 characters." });

            var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
            if (!passwordRegex.IsMatch(model.Password))
                return new BadRequestObjectResult(new { Message = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, and one number." });

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(model.BusinessEmail))
                return new BadRequestObjectResult(new { Message = "Invalid business email format." });

            if (!model.BusinessEmail.EndsWith("@marketplace.com"))
                return new BadRequestObjectResult(new { Message = "Business email must end with @marketplace.com." });

            var existingVendorByEmail = await _vendorRepository.FindAsync(v => v.BusinessEmail == model.BusinessEmail);
            if (existingVendorByEmail.Any())
                return new BadRequestObjectResult(new { Message = "Business email already exists." });

            var phoneRegex = new Regex(@"^\+?\d{10,15}$");
            if (!phoneRegex.IsMatch(model.PhoneNumber))
                return new BadRequestObjectResult(new { Message = "Invalid phone number format. It should be 10-15 digits, optionally starting with +." });

            var existingVendorByPhone = await _vendorRepository.FindAsync(v => v.PhoneNumber == model.PhoneNumber);
            if (existingVendorByPhone.Any())
                return new BadRequestObjectResult(new { Message = "Phone number already exists." });

            var vendor = new Vendor
            {
                PhoneNumber = model.PhoneNumber,
                StoreName = model.StoreName,
                OwnerName = model.OwnerName,
                BusinessEmail = model.BusinessEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                ConfirmPasswordHash = BCrypt.Net.BCrypt.HashPassword(model.ConfirmPassword),
                IsApproved = false,
                IsPending = true, // جديد
                AutoApproveProducts = false,
                IsEnabled = true
            };

            await _vendorRepository.AddAsync(vendor);

            if (_hubContext != null)
            {
                await _hubContext.Clients.Group("Admins").SendAsync("ReceiveNotification", "Admin", $"New Vendor registered: {model.StoreName}");
            }

            return new OkObjectResult(new { Message = "Vendor registered successfully." });
        }

        public async Task<IActionResult> LoginCustomer(LoginModel model)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(model.Email))
                return new BadRequestObjectResult(new { Message = "Invalid email format." });

            if (!model.Email.EndsWith("@marketplace.com"))
                return new BadRequestObjectResult(new { Message = "Email must end with @marketplace.com." });

            var customers = await _customerRepository.FindAsync(c => c.Email == model.Email);
            var customer = customers.FirstOrDefault();
            if (customer == null || !BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash))
                return new UnauthorizedObjectResult(new { Message = "Invalid email or password." });

            var token = GenerateJwtToken(customer.PhoneNumber, customer.Email, "Customer");
            return new OkObjectResult(new
            {
                AccessToken = token,
                Role = "Customer",
                Email = customer.Email
            });
        }

        public async Task<IActionResult> LoginVendor(LoginModel model)
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(model.Email))
                return new BadRequestObjectResult(new { Message = "Invalid email format." });

            if (!model.Email.EndsWith("@marketplace.com"))
                return new BadRequestObjectResult(new { Message = "Email must end with @marketplace.com." });

            var vendors = await _vendorRepository.FindAsync(v => v.BusinessEmail == model.Email);
            var vendor = vendors.FirstOrDefault();
            if (vendor == null || !BCrypt.Net.BCrypt.Verify(model.Password, vendor.PasswordHash))
                return new UnauthorizedObjectResult(new { Message = "Invalid email or password." });

            if (vendor.IsPending)
                return new BadRequestObjectResult(new { Message = "Vendor account is pending approval." });

            if (!vendor.IsApproved)
                return new UnauthorizedObjectResult(new { Message = "Vendor is not approved." });

            if (!vendor.IsEnabled)
                return new BadRequestObjectResult(new { Message = "Vendor is disabled." });

            var token = GenerateJwtToken(vendor.PhoneNumber, vendor.BusinessEmail, "Vendor");
            return new OkObjectResult(new
            {
                AccessToken = token,
                Role = "Vendor",
                Email = vendor.BusinessEmail
            });
        }

        private string GenerateJwtToken(string phoneNumber, string email, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, phoneNumber),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("Role", role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}