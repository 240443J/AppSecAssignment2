# ?? Change Password - Quick Test Guide

**Time Required:** 5 minutes  
**Difficulty:** Easy  
**Status:** ? Ready to Test

---

## ?? Quick Test (2 Minutes)

### **Prerequisites:**
1. App is running (`dotnet run`)
2. You have a registered account
3. You're logged in

### **Test Steps:**

```bash
# 1. Navigate to Change Password
URL: https://localhost:5001/ChangePassword

# 2. Fill in the form:
Current Password: [your current password]
New Password: "NewSecure@Pass123"
Confirm Password: "NewSecure@Pass123"

# 3. Click "Change Password"

# Expected Result:
? Success message appears
? Page reloads with success banner
? You remain logged in
```

---

## ? What Should Happen

### **Visual Feedback:**
1. **Before Submit:**
   - Password strength indicator shows "Strong" in green
   - All fields filled with valid data

2. **After Submit:**
   - ? Green success message: *"Your password has been changed successfully."*
   - ? Form clears (or page reloads)
   - ? You stay logged in (don't need to re-login)

3. **In the Database:**
   ```sql
   -- Check AuditLogs table
   SELECT TOP 1 * FROM AuditLogs 
   WHERE Action = 'PasswordChange' 
   ORDER BY Timestamp DESC
   ```

---

## ?? Comprehensive Tests (5 Minutes)

### **Test 1: Successful Change** ?
```
Step 1: Go to /ChangePassword
Step 2: Enter current password
Step 3: Enter new strong password: "MyNewP@ss2025"
Step 4: Confirm password: "MyNewP@ss2025"
Step 5: Submit

Expected: ? Success message + stay logged in
```

### **Test 2: Wrong Current Password** ?
```
Step 1: Go to /ChangePassword
Step 2: Enter WRONG current password
Step 3: Enter new password
Step 4: Submit

Expected: ? Error: "Current password is incorrect"
```

### **Test 3: Passwords Don't Match** ?
```
Step 1: Go to /ChangePassword
Step 2: Enter current password
Step 3: New Password: "MyNewP@ss2025"
Step 4: Confirm Password: "DifferentP@ss2025"
Step 5: Submit

Expected: ? Error: "New password and confirmation do not match"
```

### **Test 4: Password Too Weak** ?
```
Step 1: Go to /ChangePassword
Step 2: Enter current password
Step 3: New Password: "weak"
Step 4: Submit

Expected: ? Error: "Password must be at least 12 characters"
```

### **Test 5: Password History Check** ?
```
Step 1: Change password to "FirstP@ssword123"
Step 2: Change password to "SecondP@ssword123"
Step 3: Try to change back to "FirstP@ssword123"

Expected: ? Error: "You cannot reuse your last 2 passwords"
```

### **Test 6: Password Visibility Toggle** ???
```
Step 1: Go to /ChangePassword
Step 2: Type in current password field
Step 3: Click the eye icon
Expected: ? Password becomes visible

Step 4: Click eye icon again
Expected: ? Password becomes hidden
```

### **Test 7: Real-time Strength Indicator** ??
```
Step 1: Focus on "New Password" field
Step 2: Type: "weak"
Expected: ?? Red badge: "Weak - Need: at least 12 characters..."

Step 3: Type: "WeakPassword"
Expected: ?? Yellow badge: "Moderate - Missing: ..."

Step 4: Type: "StrongP@ssw0rd123"
Expected: ?? Green badge: "Strong Password"
```

---

## ?? Debugging Checklist

If something doesn't work:

### **Check Browser Console (F12)**
```javascript
// Look for JavaScript errors
// Should see no errors in Console tab
```

### **Check Visual Studio Output**
```
Look for lines like:
"User {Email} changed password successfully"

Or errors like:
"Error changing password for user {UserId}"
```

### **Check Database**
```sql
-- Verify audit log entry
SELECT TOP 5 * FROM AuditLogs ORDER BY Timestamp DESC

-- Verify password history
SELECT * FROM PasswordHistories 
WHERE UserId = (SELECT Id FROM AspNetUsers WHERE Email = 'your-email')
ORDER BY CreatedDate DESC
```

### **Common Issues:**

| Issue | Cause | Solution |
|-------|-------|----------|
| Form doesn't submit | JavaScript error | Check browser console |
| "User not found" | Not logged in | Login first |
| "Database error" | DB connection | Check connection string |
| Redirect to login | Session expired | Login again |

---

## ?? Visual Verification

### **What You Should See:**

1. **Navigation Bar (When Logged In):**
   ```
   [Fresh Farm Market]  Home | Privacy | Change Password | [Logout]
   ```

2. **Change Password Page:**
   ```
   ???????????????????????????????????????
   ? ?? Change Password                   ?
   ???????????????????????????????????????
   ? [? Success Message] (if just changed)?
   ?               ?
   ? Current Password: [••••••••] ???     ?
   ?         ?
   ? New Password: [••••••••] ???          ?
   ? [?? Strong Password]         ?
   ? Min 12 characters with uppercase... ?
   ??
   ? Confirm New Password: [••••••••]    ?
   ?         ?
   ? ?? Password Requirements:    ?
   ? • At least 12 characters       ?
   ? • One uppercase letter           ?
   ? • One lowercase letter     ?
 ? • One number         ?
   ? • One special character   ?
   ? • Cannot reuse last 2 passwords     ?
   ?     ?
   ? [? Change Password]           ?
   ? [?? Back to Profile]         ?
   ???????????????????????????????????????
   ```

3. **Success State:**
   ```
   ???????????????????????????????????????
? ? Your password has been changed   ?
   ?    successfully.    ?
   ???????????????????????????????????????
   ```

4. **Error State:**
   ```
   ???????????????????????????????????????
   ? ? Current password is incorrect     ?
   ???????????????????????????????????????
   ```

---

## ?? Test Completion Checklist

Mark each as you test:

- [ ] Page loads correctly at /ChangePassword
- [ ] Navigation link works
- [ ] Can see all three password fields
- [ ] Eye icons toggle password visibility
- [ ] Password strength indicator updates in real-time
- [ ] Can successfully change password
- [ ] Success message appears
- [ ] Error shown for wrong current password
- [ ] Error shown for mismatched passwords
- [ ] Error shown for weak password
- [ ] Password history check works (can't reuse last 2)
- [ ] Audit log entry created
- [ ] Password history entry created
- [ ] Still logged in after password change
- [ ] Can login with NEW password
- [ ] Can NOT login with OLD password

---

## ?? Test Results

### **? PASS Criteria:**
- All visual elements present
- Form validation works
- Password changes successfully
- Error messages display correctly
- Audit log updated
- Password history tracked

### **? FAIL Indicators:**
- 404 error on page load
- Form doesn't submit
- No success message
- Still can login with old password
- Database not updated

---

## ?? Manual Testing Script

**Copy and paste this to track your testing:**

```
TEST SESSION: Change Password Feature
Date: ___________
Tester: ___________

1. Navigation Test
   [ ] Change Password link visible in nav bar
   [ ] Link navigates to correct page

2. Form Display Test
   [ ] All three fields visible
   [ ] Eye icons present
   [ ] Submit button present
   [ ] Requirements info box visible

3. Password Change Test
   [ ] Can type in all fields
   [ ] Password visibility toggle works
   [ ] Strength indicator updates
   [ ] Form submits successfully
   [ ] Success message appears

4. Validation Tests
   [ ] Wrong current password ? error
   [ ] Mismatched passwords ? error
   [ ] Weak password ? error
   [ ] Password history check ? error

5. Database Tests
   [ ] AuditLogs entry created
   [ ] PasswordHistories entry created
   [ ] Password actually changed

6. Security Tests
   [ ] Can login with NEW password
   [ ] CANNOT login with OLD password
   [ ] Still logged in after change

OVERALL RESULT: [ ] PASS  [ ] FAIL
NOTES:
_______________________________________
```

---

## ?? Success Criteria

Your feature is working if:

1. ? You can access /ChangePassword when logged in
2. ? You can change your password successfully
3. ? Old password no longer works
4. ? New password works for login
5. ? Password history prevents reuse
6. ? Audit log shows the change
7. ? You remain logged in after changing

---

## ?? Quick Fixes

### **If the page doesn't load:**
```bash
# Restart the app
Ctrl+C  # Stop app
dotnet run  # Restart
```

### **If form doesn't submit:**
```javascript
// Check browser console (F12)
// Look for JavaScript errors
// Make sure jQuery and Bootstrap are loaded
```

### **If password doesn't change:**
```csharp
// Check Visual Studio Output window
// Look for error messages
// Verify database connection
```

---

**Testing Time:** 5-10 minutes  
**Expected Result:** All tests pass ?  
**Your feature is:** Production-Ready ??

**Happy Testing!** ??
