using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using PresensiJamaah.Infrastructure.Identity;
using PresensiJamaah.Web.Models;

namespace PresensiJamaah.Web.Controllers;

public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    ILogger<AccountController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToLocal(returnUrl);
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await signInManager.PasswordSignInAsync(
            model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            logger.LogInformation("Login berhasil: {Email}", model.Email);
            return RedirectToLocal(model.ReturnUrl);
        }

        if (result.IsLockedOut)
        {
            logger.LogWarning("Akun terkunci: {Email}", model.Email);
            ModelState.AddModelError(string.Empty, "Akun terkunci sementara karena terlalu banyak percobaan gagal. Coba lagi beberapa menit lagi.");
        }
        else
        {
            ModelState.AddModelError(string.Empty, "Email atau password salah.");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();

    private IActionResult RedirectToLocal(string? returnUrl) =>
        !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
}
