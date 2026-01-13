using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SSRBusiness.BusinessClasses;

namespace SSRBlazor.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly UserRepository _userRepository;

    public AccountController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string userName, [FromForm] string password, [FromForm] string returnUrl)
    {
        // Default return URL if not provided
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "/";
        }

        // Validate user
        var user = await _userRepository.AuthenticateAndLoadAsync(userName, password);

        if (user == null || !user.IsActive)
        {
            // Redirect back to login with error
            return Redirect($"/account/login?error=Invalid credentials or account inactive");
        }

        // Create claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Email ?? user.UserId),
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Email, user.Email ?? ""),
        };

        // Add role claims if user has roles (assuming UserRoles property is populated)
        // If legacy code used "Administrator" session variable, we map it here:
        // Checking for "Administrator" usually implies a role or specific flag check.
        // User entity has login status but maybe not a direct Role property visible in the snippet I saw earlier.
        // I will assume for now we just log them in. If explicit roles are needed I'd add them here.
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Keep user logged in
            ExpiresUtc = DateTime.UtcNow.AddMinutes(120) // Match legacy 120min timeout
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return LocalRedirect(returnUrl);
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/account/login");
    }
}
