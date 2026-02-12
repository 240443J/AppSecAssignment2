# Project Context for AI Agent

## Architecture
- **Framework:** ASP.NET Core 8.0 Razor Pages.
- **Auth Setup:** HYBRID.
  - We use `ASP.NET Core Identity` for User Management (UserManager, SignInManager).
  - We use a CUSTOM Cookie Scheme named "MyCookieAuth" for the actual login session (HttpContext.SignInAsync).
  - **Crucial:** Do NOT use `signInManager.PasswordSignInAsync` alone; we must manually create claims and sign in to "MyCookieAuth".

## Current Critical Issues
1. **Password Change:** The session breaks after changing the password because the SecurityStamp updates, but the "MyCookieAuth" cookie is not refreshed.
2. **2FA:** We need to intercept the login flow.
   - Step 1: Validate credentials -> Generate OTP -> Store in Session -> Redirect to 2FA Page.
   - Step 2: Verify OTP -> Create Claims -> Sign In to "MyCookieAuth".
3. **Email:** We are using Gmail SMTP. The code is correct, but we need to ensure "App Passwords" are used, not raw passwords.

## Assignment Requirements (from Practical Assignment 005)
- Must implement 2FA (Email OTP).
- Must have Account Lockout (3 attempts).
- Must have Password History (Cannot reuse last 2).
- Must have Session Timeout (1 minute sliding).