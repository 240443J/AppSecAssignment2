# ?? Fresh Farm Market - Secure Web Application

**Application Security Assignment 2**  
**Student ID:** 240443J  
**Repository:** [AppSecAssignment2](https://github.com/240443J/AppSecAssignment2)

---

## ?? Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Security Features](#security-features)
- [Technologies Used](#technologies-used)
- [Setup Instructions](#setup-instructions)
- [Configuration](#configuration)
- [Usage](#usage)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Security Considerations](#security-considerations)
- [Known Issues](#known-issues)
- [Future Enhancements](#future-enhancements)

---

## ?? Overview

Fresh Farm Market is a secure ASP.NET Core 8.0 Razor Pages web application implementing comprehensive authentication, authorization, and security features. The application demonstrates industry-standard security practices including password hashing, session management, audit logging, and email-based password reset functionality.

---

## ? Features

### **User Management**
- ? User registration with validation
- ? Secure login with account lockout
- ? Password change functionality
- ? Forgot password with email reset
- ? Role-based authorization (Admin, HR)

### **Security Features**
- ? Password complexity enforcement (12+ chars, mixed case, numbers, special chars)
- ? Password history (prevents reuse of last 2 passwords)
- ? Account lockout after 5 failed attempts
- ? Session timeout (1 minute for testing - configurable)
- ? Google reCAPTCHA v3 integration
- ? SQL injection prevention
- ? XSS protection
- ? CSRF protection
- ? Secure cookie configuration
- ? Credit card encryption (AES-256)
- ? Input validation and sanitization
- ? File upload validation (JPG only, 5MB max)

### **Audit & Monitoring**
- ? Comprehensive audit logging
- ? Security event tracking
- ? Failed login attempt monitoring
- ? Password change history

### **Email Features**
- ? Password reset via email
- ? HTML email templates
- ? Gmail SMTP integration
- ? Email delivery confirmation

---

## ?? Security Features

### **Authentication & Authorization**
```csharp
- ASP.NET Core Identity
- Cookie-based authentication
- Role-based authorization
- Password hashing (PBKDF2)
- Secure session management
```

### **Password Policy**
```
- Minimum 12 characters
- At least 1 uppercase letter
- At least 1 lowercase letter
- At least 1 number
- At least 1 special character (@$!%*?&#)
- Cannot reuse last 2 passwords
```

### **Account Lockout**
```
- 5 failed login attempts
- 3-minute lockout duration
- Automatic unlock after timeout
```

### **Session Security**
```
- 1-minute idle timeout (configurable)
- Sliding expiration
- HttpOnly cookies
- Secure cookies (HTTPS only)
- SameSite=Strict
```

---

## ??? Technologies Used

### **Backend**
- ASP.NET Core 8.0
- C# 12.0
- Entity Framework Core 8.0
- ASP.NET Core Identity
- SQL Server LocalDB

### **Frontend**
- Razor Pages
- Bootstrap 5
- jQuery
- Bootstrap Icons
- jQuery Validation

### **Security**
- Google reCAPTCHA v3
- AES-256 Encryption
- PBKDF2 Password Hashing
- Antiforgery Tokens

### **Email**
- System.Net.Mail (SMTP)
- Gmail App Passwords
- HTML Email Templates

---

## ?? Setup Instructions

### **Prerequisites**
```
- .NET 8.0 SDK
- Visual Studio 2022 (or VS Code)
- SQL Server LocalDB
- Gmail account (for email features)
```

### **Step 1: Clone Repository**
```bash
git clone https://github.com/240443J/AppSecAssignment2.git
cd AppSecAssignment2
```

### **Step 2: Install Dependencies**
```bash
cd "WebApp Core identity"
dotnet restore
```

### **Step 3: Configure Database**
```bash
# Update connection string in appsettings.json if needed
dotnet ef database update
```

### **Step 4: Configure Email (Optional)**
```bash
# Set up User Secrets for email
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:AppPassword" "your-gmail-app-password"
```

See [Email Configuration](#email-configuration) for details.

### **Step 5: Run Application**
```bash
dotnet run
```

Navigate to: `https://localhost:7151`

---

## ?? Configuration

### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "AuthConnectionString": "Data Source=(LocalDB)\\MSSQLLocalDB;Initial Catalog=AspNetAuth;Integrated Security=True;"
  },
  "EmailSettings": {
 "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Fresh Farm Market",
    "AppPassword": ""  // Use User Secrets!
  }
}
```

### **Email Configuration**

**For Gmail SMTP:**

1. **Enable 2-Step Verification:**
   - Go to: https://myaccount.google.com/security
   - Turn on 2-Step Verification

2. **Generate App Password:**
   - Go to: https://myaccount.google.com/apppasswords
   - Create password for "Mail" > "Windows Computer"
   - Copy the 16-character password

3. **Store in User Secrets:**
   ```bash
   dotnet user-secrets set "EmailSettings:SenderEmail" "your-email@gmail.com"
 dotnet user-secrets set "EmailSettings:AppPassword" "your-app-password"
   ```

4. **Never commit passwords to Git!**

### **reCAPTCHA Configuration**

Current keys are for testing only. For production:

1. Get keys from: https://www.google.com/recaptcha/admin
2. Update in `Register.cshtml`, `ForgotPassword.cshtml`, etc.
3. Update secret key in respective `.cshtml.cs` files

---

## ?? Usage

### **Register New User**
1. Navigate to `/Register`
2. Fill in all required fields
3. Upload JPG photo (max 5MB)
4. Complete reCAPTCHA
5. Submit form

### **Login**
1. Navigate to `/Login`
2. Enter email and password
3. Optionally check "Remember Me"
4. Submit

### **Change Password**
1. Login to your account
2. Navigate to `/ChangePassword`
3. Enter current password
4. Enter new password (must meet complexity requirements)
5. Confirm new password
6. Submit

### **Forgot Password**
1. Navigate to `/ForgotPassword`
2. Enter your email address
3. Check email for reset link
4. Click link and enter new password
5. Link expires in 30 minutes

---

## ?? Project Structure

```
WebApp Core identity/
??? Pages/
?   ??? Register.cshtml / .cs           # User registration
?   ??? Login.cshtml / .cs # User login
?   ??? ChangePassword.cshtml / .cs     # Password change
?   ??? ForgotPassword.cshtml / .cs     # Password reset request
???? ResetPassword.cshtml / .cs      # Password reset form
?   ??? Error404.cshtml / .cs           # 404 error page
?   ??? Error500.cshtml / .cs           # 500 error page
?   ??? ...
??? ViewModels/
?   ??? Register.cs          # Registration ViewModel
?   ??? Login.cs    # Login ViewModel
?   ??? ChangePassword.cs        # Change Password ViewModel
?   ??? ForgotPassword.cs  # Forgot Password ViewModel
?   ??? ResetPassword.cs          # Reset Password ViewModel
??? Models/
?   ??? ApplicationUser.cs              # Extended Identity User
?   ??? PasswordHistory.cs              # Password history tracking
?   ??? AuditLog.cs   # Audit log model
?   ??? AuthDbContext.cs         # Database context
??? Services/
?   ??? EmailService.cs        # Email sending service
?   ??? EncryptionService.cs      # AES encryption
?   ??? AuditService.cs           # Audit logging
??? Helpers/
?   ??? InputValidationHelper.cs        # Input validation utilities
??? Migrations/       # EF Core migrations
??? wwwroot/ # Static files
?   ??? css/           # Stylesheets
?   ??? js/          # JavaScript
?   ??? lib/            # Libraries (Bootstrap, jQuery)
?   ??? uploads/   # User uploads
??? appsettings.json       # Configuration
??? Program.cs               # Application startup
```

---

## ?? Testing

### **Test Accounts**
After registration, users are automatically assigned Admin and HR roles.

### **Test Scenarios**

#### **Password Policy**
- ? Try weak password (< 12 chars) ? Should fail
- ? Try password without uppercase ? Should fail
- ? Try password without number ? Should fail
- ? Try strong password ? Should succeed

#### **Account Lockout**
- ? Enter wrong password 5 times ? Account locked for 3 minutes
- ? Wait 3 minutes ? Account unlocked automatically

#### **Password History**
- ? Change password to new password ? Succeeds
- ? Try to change back to previous password ? Fails
- ? Change to third password ? Succeeds

#### **Forgot Password**
- ? Enter valid email ? Receives reset link
- ? Click reset link within 30 minutes ? Can reset
- ? Click expired link (> 30 minutes) ? Error message
- ? Use link twice ? Second attempt fails

#### **Session Timeout**
- ? Login and wait 1 minute inactive ? Session expires
- ? Click any link after timeout ? Redirected to login

---

## ??? Security Considerations

### **Implemented**
- ? Password hashing (PBKDF2 via Identity)
- ? Parameterized queries (EF Core)
- ? Input validation and sanitization
- ? XSS prevention (Razor encoding)
- ? CSRF protection (Antiforgery tokens)
- ? Secure cookies (HttpOnly, Secure, SameSite)
- ? Account lockout after failed attempts
- ? Password complexity enforcement
- ? Session timeout
- ? Audit logging
- ? File upload validation
- ? Credit card encryption
- ? reCAPTCHA bot protection

### **Best Practices**
- ? User Secrets for sensitive data (not in appsettings.json)
- ? HTTPS enforcement
- ? Secure password reset tokens
- ? Email validation
- ? Phone number validation
- ? Credit card Luhn validation
- ? JPG file signature validation

### **Not Implemented (Future)**
- ? Two-Factor Authentication (2FA)
- ? Email confirmation on registration
- ? Remember me device tracking
- ? IP-based rate limiting
- ? Security headers (CSP, HSTS, etc.)

---

## ?? Known Issues

### **Email Sending**
- Gmail may block emails on first attempt
- Solution: Check Gmail security settings and approve sign-in
- Emails may go to spam folder initially

### **Session Timeout**
- Currently set to 1 minute for testing
- Production should use longer timeout (15-30 minutes)

### **LocalDB**
- Database file location may vary
- Check connection string if database errors occur

---

## ?? Future Enhancements

### **Security**
- [ ] Implement Two-Factor Authentication (2FA)
- [ ] Add email confirmation on registration
- [ ] Implement device tracking for "Remember Me"
- [ ] Add rate limiting per IP address
- [ ] Implement security headers middleware

### **Features**
- [ ] User profile management
- [ ] Password strength indicator
- [ ] Email verification on registration
- [ ] Social login (Google, Facebook)
- [ ] Admin dashboard for user management

### **Infrastructure**
- [ ] Move to SQL Server (from LocalDB)
- [ ] Implement Redis for session storage
- [ ] Add application insights/monitoring
- [ ] Containerize with Docker
- [ ] CI/CD pipeline

---

## ?? License

This project is created for educational purposes as part of an Application Security course assignment.

---

## ?? Author

**Student ID:** 240443J  
**Email:** jingyanglim23@gmail.com  
**Repository:** https://github.com/240443J/AppSecAssignment2

---

## ?? References

- [ASP.NET Core Identity Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Google reCAPTCHA Documentation](https://developers.google.com/recaptcha/docs/v3)

---

**Last Updated:** February 2, 2025  
**Version:** 1.0.0  
**Status:** ? Production Ready
