using Microsoft.AspNetCore.Mvc;
using SELENE_STUDIO.Data;
using Microsoft.AspNetCore.Identity;
using SELENE_STUDIO.ViewModels;
using SELENE_STUDIO.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace SELENE_STUDIO.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<RegUser> _signInManager;
        private readonly UserManager<RegUser> _userManager;
        private readonly LogAppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ITokenRepository _tokenRepository;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<RegUser> signInManager, UserManager<RegUser> userManager, LogAppDbContext context, IEmailSender emailSender, ITokenRepository tokenRepository, ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
            _emailSender = emailSender;
            _tokenRepository = tokenRepository;
            _logger = logger;
        }
        //GET /<controller>/

        [RedirectAuthenticatedUser]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel logininfo)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(logininfo.Email);

                // Check if the user exists
                if (user != null)
                {
                    // Check if the user's email is confirmed
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        // Generate email confirmation token with a 1-minute expiration time
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                        // Store token securely in the database
                        await _tokenRepository.AddAsync(new Token { UserId = user.Id, TokenValue = token, ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(15) });

                        // Include token as a query parameter in the confirmation link
                        var confirmationLink = Url.Action("ConfirmEmail", "Account", new { token }, Request.Scheme);


                        // Send email confirmation link to the user's email address
                        // Send email confirmation link to the user's email address
                        await _emailSender.SendEmailAsync(
                            user.Email,
                            "Confirm your email",
                            $"<!DOCTYPE html>" +
                            $"<html lang=\"en\">" +
                            $"<head>" +
                            $"<meta charset=\"UTF-8\">" +
                            $"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                            $"<title>Email Confirmation</title>" +
                            $"<style>" +
                            $"  .button-container {{ text-align: center; }}" +
                            $"  .button {{ background-color: #007bff; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}" +
                            $"  .button:hover {{ background-color: #0056b3; }}" +
                            $"  .button:visited {{ color: #fff; }}" +
                            $"</style>" +
                            $"</head>" +
                            $"<body>" +
                            $"  <div>" +
                            $"    <h2>Email Confirmation</h2>" +
                            $"    <p>Dear {user.FirstName},</p>" +
                            $"    <p>Your email is yet to be confirmed. Please confirm your account by clicking the button below:</p>" +
                            $"    <div class=\"button-container\">" +
                            $"      <a href=\"{confirmationLink}\" class=\"button\">Confirm Email</a>" +
                            $"    </div>" +
                            $"    <p>This link will expire in 1 minute.</p>" +
                            $"    <p>If you did not request this, please disregard this email.</p>" +
                            $"    <p>Sincerely,<br>Selene Printing Studio</p>" +
                            $"  </div>" +
                            $"</body>" +
                            $"</html>"
                        );


                        return RedirectToAction("EmailConfirmationRequired");
                    }

                    var result = await _signInManager.PasswordSignInAsync(logininfo.Email, logininfo.Password, isPersistent: false, lockoutOnFailure: true);

                    if (result.Succeeded)
                    {
                        // Record session start time
                        user.SessionStartTime = DateTime.Now;
                        await _userManager.UpdateAsync(user);

                        if (await _userManager.IsInRoleAsync(user, "Admin"))
                        {
                            return RedirectToAction("Index", "Admin");
                        }
                        else
                        {
                            return RedirectToAction("Home", "User");
                        }
                    }
                    if (result.IsLockedOut)
                    {
                        var resetPasswordLink = Url.Action("ForgotPassword", "Account");
                        var encodedLink = HtmlEncoder.Default.Encode(resetPasswordLink);
                        var errorMessage = $"Your account is temporarily locked out for 5 minutes due to multiple failed login attempts. Please try again later. If you need to reset your password, you can <a href='{encodedLink}'>reset it here</a>.";
                        ModelState.AddModelError("", errorMessage);
                    }
                    else
                    {
                        ModelState.AddModelError("", "Invalid login attempt. Please try again.");
                    }

                }
                else
                {
                    ModelState.AddModelError("", "User not found");
                }
            }
            return View(logininfo);
        }






        /*
        if (result.Succeeded)
        {
            return RedirectToAction("Home", "User");
        }
        else
        {
            ModelState.AddModelError("", "Failed to Login");
        }
        return View(logininfo);
        */

        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                // Record session end time
                user.SessionEndTime = DateTime.Now;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [RedirectAuthenticatedUser]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }


        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel userEnteredData)
        {
            if (ModelState.IsValid)
            {
                // Check if the email already exists
                var existingUser = await _userManager.FindByEmailAsync(userEnteredData.Email);
                if (existingUser != null)
                {
                    // If the email already exists, add an error message to the model state
                    ModelState.AddModelError("EmailExists", "A user with this email already exists.");
                    return View(userEnteredData);
                }

                // If the email doesn't exist, proceed with user creation
                var newRegUser = new RegUser
                {
                    UserName = userEnteredData.Email,
                    FirstName = userEnteredData.FirstName,
                    LastName = userEnteredData.LastName,
                    Address = $"{userEnteredData.Address}, Postal Code: {userEnteredData.PostalCode}",
                    PhoneNumber = userEnteredData.Contact,
                    Email = userEnteredData.Email
                };

                var result = await _userManager.CreateAsync(newRegUser, userEnteredData.Password);

                if (result.Succeeded)
                {
                    // Generate email confirmation token with a 1-minute expiration time
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(newRegUser);

                    // Store token securely in the database
                    await _tokenRepository.AddAsync(new Token { UserId = newRegUser.Id, TokenValue = token, ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1) });

                    // Include token as a query parameter in the confirmation link
                    var confirmationLink = Url.Action("ConfirmEmail", "Account", new { token }, Request.Scheme);


                    // Send email confirmation link to the user's email address
                    // Send email confirmation link to the user's email address
                    await _emailSender.SendEmailAsync(
                        newRegUser.Email,
                        "Confirm your email",
                        $"<!DOCTYPE html>" +
                        $"<html lang=\"en\">" +
                        $"<head>" +
                        $"<meta charset=\"UTF-8\">" +
                        $"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                        $"<title>Email Confirmation</title>" +
                        $"<style>" +
                        $"  .button-container {{ text-align: center; }}" +
                        $"  .button {{ background-color: #007bff; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}" +
                        $"  .button:hover {{ background-color: #0056b3; }}" +
                        $"  .button:visited {{ color: #fff; }}" +
                        $"</style>" +
                        $"</head>" +
                        $"<body>" +
                        $"  <div>" +
                        $"    <h2>Email Confirmation</h2>" +
                        $"    <p>Dear {newRegUser.FirstName},</p>" +
                        $"    <p>Thank you for registering with our website. Please confirm your account by clicking the button below:</p>" +
                        $"    <div class=\"button-container\">" +
                        $"      <a href=\"{confirmationLink}\" class=\"button\">Confirm Email</a>" +
                        $"    </div>" +
                        $"    <p>This link will expire in 1 minute.</p>" +
                        $"    <p>If you did not request this, please disregard this email.</p>" +
                        $"    <p>Sincerely,<br>Selene Printing Studio</p>" +
                        $"  </div>" +
                        $"</body>" +
                        $"</html>"
                    );

                    return RedirectToAction("EmailConfirmationRequired");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(userEnteredData);
        }




        public IActionResult ConfirmEmail()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string token)
        {
            _logger.LogInformation("ConfirmEmail action called with token: {Token}", token);

            if (token == null)
            {
                _logger.LogWarning("Invalid token: token is null");
                TempData["ErrorMessage"] = "Invalid token.";
                return RedirectToAction("Login", "Account");
            }

            var tokenRecord = await _tokenRepository.GetTokenAsync(token);
            if (tokenRecord == null || tokenRecord.ExpirationTime < DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Invalid token or token has expired: TokenValue={TokenValue}, UserId={UserId}, ExpirationTime={ExpirationTime}", tokenRecord?.TokenValue, tokenRecord?.UserId, tokenRecord?.ExpirationTime);
                TempData["ErrorMessage"] = "Invalid token or token has expired.";
                return RedirectToAction("Login", "Account");
            }

            var user = await _userManager.FindByIdAsync(tokenRecord.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for token: TokenValue={TokenValue}, UserId={UserId}", tokenRecord?.TokenValue, tokenRecord?.UserId);
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Login", "Account");
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            _logger.LogInformation("Verification result: {Result}", result.Succeeded ? "Succeeded" : "Failed");
            if (result.Succeeded)
            {
                _logger.LogInformation("Email confirmed successfully for user: UserId={UserId}", user.Id);
                TempData["SuccessMessage"] = "Email successfully confirmed. You can now log in.";
                return RedirectToAction("Login", "Account");
            }
            else
            {
                _logger.LogError("Email confirmation failed for user: UserId={UserId}, Errors={Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                TempData["ErrorMessage"] = "Email confirmation failed.";
                return RedirectToAction("Login", "Account");
            }
        }




        [HttpGet]
        [AllowAnonymous]
        public IActionResult EmailConfirmationRequired()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Display a model error if the user does not exist or is not confirmed
                    ModelState.AddModelError(string.Empty, "User does not exist or is not confirmed.");
                    return View(model);
                }

                // Generate a unique token
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                // Store token securely in the database
                await _tokenRepository.AddAsync(new Token { UserId = user.Id, TokenValue = token, ExpirationTime = DateTimeOffset.UtcNow.AddMinutes(1) });

                // Include token as a query parameter in the password reset link
                var resetPasswordLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);


                // Send password reset link to the user's email address
                await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                           $"<!DOCTYPE html>" +
                           $"<html lang=\"en\">" +
                           $"<head>" +
                           $"<meta charset=\"UTF-8\">" +
                           $"<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">" +
                           $"<title>Reset Password</title>" +
                           $"<style>" +
                           $"  .button-container {{ text-align: center; }}" +
                           $"  .button {{ background-color: #007bff; color: #fff; padding: 10px 20px; text-decoration: none; border-radius: 5px; }}" +
                           $"  .button:hover {{ background-color: #0056b3; }}" +
                           $"  .button:visited {{ color: #fff; }}" +
                           $"</style>" +
                           $"</head>" +
                           $"<body>" +
                           $"  <div>" +
                           $"    <h2>Reset Password</h2>" +
                           $"    <p>Hello,</p>" +
                           $"    <p>You have requested to reset your password. Please click the button below to reset it:</p>" +
                           $"    <div class=\"button-container\">" +
                           $"      <a href=\"{resetPasswordLink}\" class=\"button\">Reset Password</a>" +
                           $"    </div>" +
                           $"    <p>This link will expire in 1 minute.</p>" +
                           $"    <p>If you did not request this, please disregard this email.</p>" +
                           $"    <p>Regards,<br>Selene Printing Studio</p>" +
                           $"  </div>" +
                           $"</body>" +
                           $"</html>"
                       );

                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }


        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPasswordAsync(string token)
        {
            if (token == null)
            {
                ModelState.AddModelError("", "Invalid password reset token.");
                return View();
            }

            // Retrieve the token from the database using the token identifier passed in the URL
            var tokenRecord = await _tokenRepository.GetTokenAsync(token);
            if (tokenRecord == null || tokenRecord.ExpirationTime < DateTimeOffset.UtcNow)
            {
                TempData["ErrorMessage"] = "Invalid token or token has expired.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            // Populate the ResetPasswordViewModel with the retrieved values
            var model = new ResetPasswordViewModel
            {
                Token = tokenRecord.TokenValue, // Assign the token value
                Email = tokenRecord.UserId, // Assign the user ID
                Expiration = tokenRecord.ExpirationTime.Ticks // Assign the expiration time
            };

            return View(model);
        }



        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            // Retrieve the token from the database using the token identifier passed in the URL
            var tokenRecord = await _tokenRepository.GetTokenAsync(model.Token);
            if (tokenRecord == null)
            {
                TempData["ErrorMessage"] = "Invalid token.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token }); // Return the same view with model data including the token in the URL
            }

            // Populate model with userId and token
            model.Email = tokenRecord.UserId;
            model.Token = tokenRecord.TokenValue;

            if (model.Password.Length < 8 || model.Password.Length > 64)
            {
                TempData["ErrorMessage"] = "Password must be between 8 and 64 characters.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token });
            }

            // Check if password and confirm password match
            if (model.Password != model.ConfirmPassword)
            {
                TempData["ErrorMessage"] = "The password and confirmation password do not match.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token });
            }

            if (!Regex.IsMatch(model.Password, @"^(?=.*[A-Z])(?=.*\d)(?=.*[^\w\s]).{8,64}$"))
            {
                TempData["ErrorMessage"] = "Password must have at least one capital letter, one special character, and a number.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token });
            }

            if (!ModelState.IsValid)
            {
                // Model state is invalid, return the same view with model data including the token in the URL
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token });
            }

            var user = await _userManager.FindByIdAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token }); // Return the same view with model data including the token in the URL
            }

            if (tokenRecord.ExpirationTime < DateTimeOffset.UtcNow)
            {
                TempData["ErrorMessage"] = "Token has expired.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token }); // Return the same view with model data including the token in the URL
            }

            string resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);


            // Check if the new password is the same as the current one
            var isSamePassword = await _userManager.CheckPasswordAsync(user, model.Password);
            if (isSamePassword)
            {
                TempData["ErrorMessage"] = "New password cannot be the same as the current one.";
                return RedirectToAction(nameof(ResetPassword), new { token = model.Token }); // Return the same view with model data including the token in the URL
            }

            if (await _userManager.IsLockedOutAsync(user))
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow);
            }


            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password reset successful. You can now log in with your new password.";
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            // Get more information about the failure
            var errors = result.Errors;
            if (errors.Any())
            {
                var errorMessage = errors.FirstOrDefault()?.Description;
                TempData["ErrorMessage"] = $"Password reset failed: {errorMessage} {model.Token} && {tokenRecord.TokenValue}";
            }
            else
            {
                TempData["ErrorMessage"] = "Password reset failed.";
            }

            return RedirectToAction(nameof(ResetPassword), new { token = model.Token }); // Return the same view with model data including the token in the URL
        }













        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

    }
}