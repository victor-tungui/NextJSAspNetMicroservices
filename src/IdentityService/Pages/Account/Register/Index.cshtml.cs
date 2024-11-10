using System.Security.Claims;
using IdentityModel;
using IdentityService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityService.Pages.Account.Register;

[SecurityHeaders]
[AllowAnonymous]
public class Index(UserManager<ApplicationUser> userManager) : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager = userManager;

    [BindProperty]
    public RegisterViewModel Input { get; set; }

    [BindProperty]
    public bool RegisterSuccess { get; set; }

    public IActionResult OnGet(string returnUrl)
    {
        Input = new RegisterViewModel 
        {
            ReturnUrl = returnUrl
        };

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        if (Input.Button != "register")
        {
            return Redirect("~/");
        }

        if (ModelState.IsValid)
        {
            ApplicationUser user = new() 
            {
                UserName = Input.Username,
                Email = Input.Email,
                EmailConfirmed = true
            };

            IdentityResult idResult = await _userManager.CreateAsync(user, Input.Password);

            if (idResult.Succeeded)
            {
                await _userManager.AddClaimsAsync(user,
                    [new Claim(JwtClaimTypes.Name, Input.FullName)]
                );

                RegisterSuccess = true;
            }
        }

        return Page();
    }
}
