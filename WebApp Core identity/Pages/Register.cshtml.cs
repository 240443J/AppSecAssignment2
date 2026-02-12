using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WebApp_Core_identity.ViewModels;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;
using WebApp_Core_Identity.Helpers;
using Newtonsoft.Json;

namespace WebApp_Core_identity.Pages
{
    [ValidateAntiForgeryToken]
    public class RegisterModel : PageModel
    {
private UserManager<ApplicationUser> userManager { get; }
        private SignInManager<ApplicationUser> signInManager { get; }
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly EncryptionService encryptionService;
        private readonly IWebHostEnvironment environment;
        private readonly ILogger<RegisterModel> logger;
        private readonly AuditService auditService;
  private readonly IConfiguration _configuration;

     [BindProperty]
        public Register RModel { get; set; } = new Register();

        public RegisterModel(
      UserManager<ApplicationUser> userManager,
 SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole> roleManager,
  EncryptionService encryptionService,
    IWebHostEnvironment environment,
  ILogger<RegisterModel> logger,
AuditService auditService,
   IConfiguration configuration)
  {
  this.userManager = userManager;
     this.signInManager = signInManager;
  this.roleManager = roleManager;
 this.encryptionService = encryptionService;
  this.environment = environment;
     this.logger = logger;
    this.auditService = auditService;
 this._configuration = configuration;
}

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync(string? captchaToken)
     {
         if (ModelState.IsValid)
    {
            try
      {
  // === SECURITY: Google reCAPTCHA v3 verification ===
      if (!string.IsNullOrEmpty(captchaToken))
    {
       var client = new HttpClient();
     var secretKey = "6LcgHEcsAAAAAHG-99vFR-5dKrz_YBM06Dv15xpG";
         var response = await client.PostAsync(
        $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={captchaToken}",
    null);
  var jsonString = await response.Content.ReadAsStringAsync();
        dynamic? captchaResult = JsonConvert.DeserializeObject(jsonString);

        if (captchaResult?.success != "true" || captchaResult?.score < 0.5)
   {
            // Bot detected - reject registration
        ModelState.AddModelError("", "Registration failed. Please try again.");
      var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
   var score = captchaResult?.score?.ToString() ?? "unknown";
  logger.LogWarning($"reCAPTCHA failed for registration attempt from IP: {ipAddress}, Score: {score}");

 // Log security event
    await auditService.LogSecurityEventAsync(
  "Anonymous",
   "Bot Registration Attempt",
 $"reCAPTCHA score: {score}",
  ipAddress,
    HttpContext.Request.Headers["User-Agent"].ToString());

        return Page();
 }

   var passIpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
  var passScore = captchaResult?.score?.ToString() ?? "unknown";
logger.LogInformation($"reCAPTCHA passed for registration from IP: {passIpAddress}, Score: {passScore}");
     }

   // === SECURITY: Additional validation checks ===
     
       // Check for SQL injection patterns
   if (InputValidationHelper.ContainsSqlInjectionPatterns(RModel.Email) ||
 InputValidationHelper.ContainsSqlInjectionPatterns(RModel.FullName))
  {
   ModelState.AddModelError("", "Invalid input detected. Please remove special SQL characters.");
  var sqlInjectionIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
       logger.LogWarning("Potential SQL injection attempt detected in registration from IP: {IP}", sqlInjectionIp);
      
   // Log security event
       await auditService.LogSecurityEventAsync(
    "Anonymous",
    "SQL Injection Attempt",
   $"Potential SQL injection in registration",
      HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
     HttpContext.Request.Headers["User-Agent"].ToString());
    
 return Page();
    }

    // Validate email format
        if (!InputValidationHelper.IsValidEmail(RModel.Email))
  {
      ModelState.AddModelError("RModel.Email", "Invalid email format");
       return Page();
   }

 // Validate phone number
        if (!InputValidationHelper.IsValidPhoneNumber(RModel.MobileNo))
     {
   ModelState.AddModelError("RModel.MobileNo", "Invalid phone number format");
 return Page();
       }

   // Validate credit card with Luhn algorithm
   if (!InputValidationHelper.IsValidCreditCard(RModel.CreditCardNo))
      {
   ModelState.AddModelError("RModel.CreditCardNo", "Invalid credit card number");
       return Page();
 }

    // Validate photo file
     if (RModel.Photo != null)
{
        // Check file extension
     if (!InputValidationHelper.IsValidFileExtension(RModel.Photo.FileName, new[] { ".jpg", ".jpeg" }))
  {
 ModelState.AddModelError("RModel.Photo", "Only .JPG files are allowed");
 return Page();
   }

 // Check file size (max 5MB)
   if (!InputValidationHelper.IsValidFileSize(RModel.Photo.Length, 5 * 1024 * 1024))
    {
      ModelState.AddModelError("RModel.Photo", "File size must not exceed 5MB");
       return Page();
 }

       // Verify actual JPG file signature
  using (var stream = RModel.Photo.OpenReadStream())
  {
    if (!InputValidationHelper.IsValidJpgFile(stream))
        {
    ModelState.AddModelError("RModel.Photo", "File is not a valid JPG image");
  return Page();
   }
   }
      }

    // Check for duplicate email
    var existingUser = await userManager.FindByEmailAsync(RModel.Email);
        if (existingUser != null)
  {
 ModelState.AddModelError("RModel.Email", "This email address is already registered");
  logger.LogWarning("Duplicate email registration attempt: {Email}", RModel.Email);
    return Page();
 }

  // Save photo with sanitized filename
  string? photoPath = null;
   if (RModel.Photo != null)
  {
      string uploadsFolder = Path.Combine(environment.WebRootPath, "uploads");
    Directory.CreateDirectory(uploadsFolder);

     // Sanitize filename
     string sanitizedFileName = InputValidationHelper.SanitizeFileName(RModel.Photo.FileName);
     string uniqueFileName = Guid.NewGuid().ToString() + "_" + sanitizedFileName;
      string filePath = Path.Combine(uploadsFolder, uniqueFileName);

  using (var fileStream = new FileStream(filePath, FileMode.Create))
     {
  await RModel.Photo.CopyToAsync(fileStream);
       }

     photoPath = "/uploads/" + uniqueFileName;
        logger.LogInformation("Photo uploaded successfully: {FileName}", uniqueFileName);
     }

  // Encrypt credit card number
      string encryptedCreditCard = encryptionService.Encrypt(RModel.CreditCardNo);

   // Sanitize text inputs
  var user = new ApplicationUser()
     {
        UserName = RModel.Email,
    Email = RModel.Email,
 FullName = InputValidationHelper.SanitizeInput(RModel.FullName),
   CreditCardNo = encryptedCreditCard,
       Gender = RModel.Gender,
    MobileNo = RModel.MobileNo,
DeliveryAddress = InputValidationHelper.SanitizeInput(RModel.DeliveryAddress),
  PhotoPath = photoPath,
      AboutMe = InputValidationHelper.SanitizeInput(RModel.AboutMe)
   };

   // Create roles if they don't exist
   await EnsureRolesExist();

    var result = await userManager.CreateAsync(user, RModel.Password);
 if (result.Succeeded)
  {
 // === SET INITIAL PASSWORD CHANGE DATE ===
        user.PasswordLastChangedDate = DateTime.UtcNow;
    await userManager.UpdateAsync(user);

 await userManager.AddToRoleAsync(user, "Admin");
  await userManager.AddToRoleAsync(user, "HR");

var regIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
     logger.LogInformation("New user registered successfully: {Email} from IP: {IP}", 
   user.Email, regIp);

// === SAVE INITIAL PASSWORD TO HISTORY ===
   var passwordHasher = new PasswordHasher<ApplicationUser>();
  var initialPasswordHash = passwordHasher.HashPassword(user, RModel.Password);
   
     using (var context = new AuthDbContext(_configuration))
 {
 context.PasswordHistories.Add(new PasswordHistory
      {
UserId = user.Id,
    PasswordHash = user.PasswordHash!,
  CreatedDate = DateTime.UtcNow
    });
 await context.SaveChangesAsync();
   }

     // === AUDIT LOG: Registration ===
await auditService.LogRegistrationAsync(
   user.Id,
 user.Email!,
  HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
HttpContext.Request.Headers["User-Agent"].ToString());

  return RedirectToPage("/Login");
  }

   foreach (var error in result.Errors)
        {
 ModelState.AddModelError("", error.Description);
  logger.LogWarning("User registration failed for {Email}: {Error}", RModel.Email, error.Description);
    }
      }
        catch (Exception ex)
     {
      ModelState.AddModelError("", "An error occurred during registration. Please try again.");
     logger.LogError(ex, "Registration error for {Email}", RModel.Email);
 }
  }

return Page();
        }

   private async Task EnsureRolesExist()
   {
    var roleNames = new[] { "Admin", "HR" };
 foreach (var roleName in roleNames)
       {
     if (!await roleManager.RoleExistsAsync(roleName))
   {
         await roleManager.CreateAsync(new IdentityRole(roleName));
   logger.LogInformation("Created role: {RoleName}", roleName);
}
}
   }
    }
}