# ?? Password Age Implementation - Complete Guide

**Feature:** Minimum and Maximum Password Age  
**Status:** ? Implementation Ready  
**Date:** February 2, 2025

---

## ?? **What Has Been Implemented**

### ? **Completed:**

1. **ApplicationUser.cs** - Added password age tracking fields
2. **PasswordAgeSettings.cs** - Configuration class for password age policies
3. **PasswordAgeService.cs** - Service to enforce password age rules
4. **Program.cs** - Registered password age service
5. **appsettings.json** - Added password age configuration
6. **ChangePassword.cshtml.cs** - Integrated password age checks
7. **ChangePassword.cshtml** - Added password expiry warnings

###  **Still Needed:**

1. **Database Migration** - Create migration for new fields
2. **Register.cshtml.cs** - Set initial password change date
3. **ResetPassword.cshtml.cs** - Set password change date after reset
4. **Login Middleware** - Block access if password expired
5. **Testing** - Verify all scenarios work

---

## ?? **Features Implemented**

### **1. Minimum Password Age** ?

**What it does:**
- Prevents users from changing password too frequently
- Default: 1 minute (for testing) - configurable to any duration
- Prevents bypassing password history

**Configuration:**
```json
"PasswordAgeSettings": {
  "MinimumPasswordAgeMinutes": 1,  // For testing
  "EnforceMinimumPasswordAge": true
}
```

**For production, set to:**
```json
"MinimumPasswordAgeMinutes": 1440// 24 hours
```

**How it works:**
- When user tries to change password, system checks last change date
- If less than minimum age has passed, shows error message
- Error message shows exact time remaining
- After minimum age passes, user can change password

---

### **2. Maximum Password Age** ?

**What it does:**
- Forces users to change password after 90 days
- Shows warnings when password is close to expiring
- Blocks access if password expired (pending middleware)

**Configuration:**
```json
"PasswordAgeSettings": {
  "MaximumPasswordAgeDays": 90,
  "PasswordExpiryWarningDays": 14,
  "EnforcePasswordExpiry": true
}
```

**How it works:**
- System tracks when password was last changed
- 14 days before expiry: Shows warning on Change Password page
- 7 days before expiry: Warning changes to yellow alert
- On expiry day: Warning changes to red alert
- After expiry: `MustChangePassword` flag is set (blocks access with middleware)

---

## ?? **Files Created/Modified**

### **New Files:**

1. **WebApp Core identity\Configuration\PasswordAgeSettings.cs**
   - Configuration class for password age policies
   - Defines min/max age settings
   - Enables/disables enforcement

2. **WebApp Core identity\Services\PasswordAgeService.cs**
   - Service to check password age
   - Validates minimum age before password change
   - Checks if password has expired
   - Updates password change dates
   - Shows expiry warnings

### **Modified Files:**

1. **WebApp Core identity\Models\ApplicationUser.cs**
   - Added `PasswordLastChangedDate` field
   - Added `MustChangePassword` flag
   - Added `PasswordExpiryWarningDate` field

2. **WebApp Core identity\Program.cs**
   - Registered `PasswordAgeSettings` configuration
   - Registered `IPasswordAgeService` in DI container

3. **WebApp Core identity\appsettings.json**
   - Added `PasswordAgeSettings` section
   - Configured min age (1 min for testing)
   - Configured max age (90 days)
   - Configured warning period (14 days)

4. **WebApp Core identity\Pages\ChangePassword.cshtml.cs**
   - Injected `IPasswordAgeService`
   - Check minimum age before allowing password change
   - Update password change date after successful change
   - Load expiry warnings on page load

5. **WebApp Core identity\Pages\ChangePassword.cshtml**
- Display password expiry warnings
   - Color-coded alerts (red/yellow/blue)
   - Shows days remaining

---

## ?? **Next Steps to Complete Implementation**

### **Step 1: Stop the App and Create Migration**

```bash
# Stop the running app (important!)
# In Visual Studio: Shift+F5

# Then run:
cd "WebApp Core identity"
dotnet ef migrations add AddPasswordAgeTracking
dotnet ef database update
```

---

### **Step 2: Update Register.cshtml.cs**

Add this code after user creation (around line 170):

```csharp
var result = await userManager.CreateAsync(user, RModel.Password);
if (result.Succeeded)
{
    // Set initial password change date
    user.PasswordLastChangedDate = DateTime.UtcNow;
    await userManager.UpdateAsync(user);
    
    await userManager.AddToRoleAsync(user, "Admin");
    // ... rest of existing code
}
```

---

### **Step 3: Update ResetPassword.cshtml.cs**

Add this code after successful password reset (around line 104):

```csharp
if (result.Succeeded)
{
    // Update password last changed date
    user.PasswordLastChangedDate = DateTime.UtcNow;
    user.MustChangePassword = false; // Clear flag
    await _userManager.UpdateAsync(user);
    
    // ... rest of existing code
}
```

---

### **Step 4: Create Password Expiry Middleware** (Optional but Recommended)

Create `WebApp Core identity\Middleware\PasswordExpiryMiddleware.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using WebApp_Core_Identity.Model;
using WebApp_Core_Identity.Services;

namespace WebApp_Core_Identity.Middleware
{
    public class PasswordExpiryMiddleware
    {
   private readonly RequestDelegate _next;

        public PasswordExpiryMiddleware(RequestDelegate next)
  {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context,
      UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
 IPasswordAgeService passwordAgeService)
        {
   if (context.User.Identity?.IsAuthenticated == true)
     {
     var user = await userManager.GetUserAsync(context.User);
     
      if (user != null)
       {
          // Check if password has expired
  var (isExpired, _) = await passwordAgeService.IsPasswordExpiredAsync(user);
   
         if (isExpired || user.MustChangePassword)
   {
     // Allow access to change password page
       if (!context.Request.Path.StartsWithSegments("/ChangePassword") &&
    !context.Request.Path.StartsWithSegments("/Logout"))
          {
   context.Response.Redirect("/ChangePassword?expired=true");
    return;
         }
         }
            }
 }

      await _next(context);
   }
    }

    public static class PasswordExpiryMiddlewareExtensions
    {
        public static IApplicationBuilder UsePasswordExpiryCheck(this IApplicationBuilder builder)
        {
      return builder.UseMiddleware<PasswordExpiryMiddleware>();
        }
    }
}
```

Then in `Program.cs`, add after `app.UseAuthentication()`:

```csharp
app.UseAuthentication();
app.UsePasswordExpiryCheck(); // Add this line
app.UseAuthorization();
```

---

### **Step 5: Update ChangePassword OnGet to Show Expiry Message**

Already implemented! But if you set `?expired=true` query parameter, you can check for it:

```csharp
public async Task OnGetAsync(bool expired = false)
{
    if (expired)
    {
    StatusMessage = "Your password has expired. You must change it to continue.";
    }
    
    // ... existing code
}
```

---

## ?? **Testing Guide**

### **Test 1: Minimum Password Age**

```
1. Register or login
2. Go to Change Password
3. Change your password successfully
4. Try to change password again immediately
5. ? Expected: Error message "You must wait 1 more minute(s)"
6. Wait 1 minute
7. Try to change password again
8. ? Expected: Password changes successfully
```

---

### **Test 2: Maximum Password Age Warning**

```
1. Login to database
2. Run SQL:
   UPDATE AspNetUsers 
   SET PasswordLastChangedDate = DATEADD(DAY, -76, GETUTCDATE())
   WHERE Email = 'your@email.com'
   
3. Go to Change Password page
4. ? Expected: Warning message "Your password will expire in 14 days"

5. Run SQL:
   UPDATE AspNetUsers 
   SET PasswordLastChangedDate = DATEADD(DAY, -84, GETUTCDATE())
   
6. Refresh page
7. ? Expected: Yellow warning "Your password will expire in 6 days"

8. Run SQL:
   UPDATE AspNetUsers 
   SET PasswordLastChangedDate = DATEADD(DAY, -90, GETUTCDATE())
   
9. Refresh page
10. ? Expected: Red alert "Your password has expired!"
```

---

### **Test 3: Password Expiry Enforcement** (With Middleware)

```
1. Set user password to 91 days old (SQL above)
2. Login
3. Try to access any page (e.g., /Index)
4. ? Expected: Redirected to /ChangePassword?expired=true
5. Change password
6. ? Expected: Can now access all pages normally
```

---

## ?? **Configuration Options**

### **For Development/Testing:**
```json
{
  "PasswordAgeSettings": {
    "MinimumPasswordAgeMinutes": 1, // 1 minute for quick testing
    "MaximumPasswordAgeDays": 7,          // 7 days for testing
    "PasswordExpiryWarningDays": 2,       // Warn 2 days before
    "EnforcePasswordExpiry": true,
    "EnforceMinimumPasswordAge": true
  }
}
```

### **For Production:**
```json
{
  "PasswordAgeSettings": {
    "MinimumPasswordAgeMinutes": 1440,    // 24 hours
    "MaximumPasswordAgeDays": 90,         // 90 days (industry standard)
    "PasswordExpiryWarningDays": 14,    // 2 weeks warning
    "EnforcePasswordExpiry": true,
    "EnforceMinimumPasswordAge": true
  }
}
```

### **To Disable Features:**
```json
{
  "PasswordAgeSettings": {
    "EnforcePasswordExpiry": false,        // Disable max age
    "EnforceMinimumPasswordAge": false  // Disable min age
  }
}
```

---

## ?? **Database Schema Changes**

### **New Columns in AspNetUsers Table:**

| Column Name | Type | Nullable | Description |
|-------------|------|----------|-------------|
| `PasswordLastChangedDate` | datetime2 | Yes | When password was last changed |
| `MustChangePassword` | bit | No | Flag to force password change |
| `PasswordExpiryWarningDate` | datetime2 | Yes | When warning was last shown |

---

## ?? **Troubleshooting**

### **Problem: Migration fails**
```
Solution:
1. Stop the app completely (Shift+F5)
2. Close any database connections
3. Run: dotnet ef database update
4. If still fails, restart SQL Server LocalDB
```

### **Problem: Minimum age not working**
```
Check:
1. Did you restart the app after code changes?
2. Is EnforceMinimumPasswordAge = true in appsettings.json?
3. Is PasswordLastChangedDate set in database?
4. Check logs for any errors
```

### **Problem: Warning not showing**
```
Check:
1. Is user.PasswordLastChangedDate set in database?
2. Is it old enough to trigger warning?
3. Did you inject IPasswordAgeService in controller?
4. Check browser console for errors
```

---

## ? **Implementation Checklist**

```
Setup:
? Stop running app
? Create migration: dotnet ef migrations add AddPasswordAgeTracking
? Update database: dotnet ef database update
? Restart app

Code Updates:
? Update Register.cshtml.cs (set initial date)
? Update ResetPassword.cshtml.cs (set date after reset)
? Create PasswordExpiryMiddleware (optional)
? Register middleware in Program.cs (optional)

Testing:
? Test minimum age (change password twice quickly)
? Test max age warning (set old date in DB)
? Test expiry enforcement (with middleware)
? Test configuration changes (enable/disable)

Documentation:
? Update README.md with new features
? Document configuration options
? Add testing instructions
```

---

## ?? **Summary**

**What You Have Now:**

? **Minimum Password Age** - Fully implemented and configured  
? **Maximum Password Age** - Warning system implemented  
? **Password Expiry Enforcement** - Middleware ready to implement  
? **Configurable Settings** - Easy to adjust via appsettings.json  
? **User-Friendly Messages** - Clear warnings and error messages

**What's Left:**

1. Create and run migration
2. Update Register and ResetPassword pages
3. Optionally add middleware for enforcement
4. Test all scenarios

**Time to Complete:** ~15-30 minutes

---

**You're almost done! Just need to create the migration and update a couple of pages.** ??

**Next step:** Stop the app and run the migration commands!
