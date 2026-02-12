# ?? Password Age Time Calculations Reference

**Quick reference for converting minutes to days for MaximumPasswordAgeDays**

---

## ?? **Common Time Conversions**

### **Formula:**
```
Days = Minutes ÷ 1440
(1440 = number of minutes in a day: 24 hours × 60 minutes)
```

---

## ?? **Quick Reference Table:**

| Time Period | Minutes | Days (Decimal) | Use For |
|-------------|---------|----------------|---------|
| **1 minute** | 1 | 0.000694 | Ultra-fast testing |
| **5 minutes** | 5 | 0.003472 | Quick testing |
| **10 minutes** | 10 | 0.006944 | Short testing |
| **15 minutes** | 15 | 0.010417 | Brief testing |
| **30 minutes** | 30 | **0.02083** | **Testing (Current)** |
| **1 hour** | 60 | 0.041667 | Extended testing |
| **2 hours** | 120 | 0.083333 | Long testing |
| **4 hours** | 240 | 0.166667 | Half-day testing |
| **8 hours** | 480 | 0.333333 | Work-day testing |
| **12 hours** | 720 | 0.5 | Half-day |
| **24 hours** | 1440 | 1 | 1 day |
| **3 days** | 4320 | 3 | Short term |
| **7 days** | 10080 | 7 | 1 week |
| **30 days** | 43200 | 30 | 1 month |
| **90 days** | 129600 | 90 | **Production (Recommended)** |

---

## ? **Current Settings:**

```json
{
  "PasswordAgeSettings": {
    "MinimumPasswordAgeMinutes": 1,       // 1 minute = 60 seconds
    "MaximumPasswordAgeDays": 0.02083,    // 30 minutes = 1800 seconds
    "PasswordExpiryWarningDays": 0,
    "EnforcePasswordExpiry": true,
    "EnforceMinimumPasswordAge": true
  }
}
```

### **What This Means:**

- **Minimum Age:** User must wait **1 minute** before changing password again
- **Maximum Age:** Password expires after **30 minutes**
- **Warning:** Shows immediately (0 days before expiry)

---

## ?? **Calculation Examples:**

### **30 Minutes (Current Setting):**
```
30 minutes ÷ 1440 minutes/day = 0.020833... days
Rounded: 0.02083
```

### **1 Hour:**
```
60 minutes ÷ 1440 minutes/day = 0.041667 days
```

### **90 Days (Production):**
```
90 days = 90 (already in days, no conversion needed)
```

---

## ?? **How to Change:**

### **To Change Maximum Password Age:**

1. **Calculate the days value:**
   ```
   Days = (Minutes you want) ÷ 1440
   ```

2. **Update appsettings.json:**
   ```json
   "MaximumPasswordAgeDays": 0.02083  // Your calculated value
   ```

3. **Restart the application**

---

## ?? **Examples:**

### **Change to 1 Hour:**
```json
"MaximumPasswordAgeDays": 0.041667  // 60 minutes ÷ 1440
```

### **Change to 2 Hours:**
```json
"MaximumPasswordAgeDays": 0.083333  // 120 minutes ÷ 1440
```

### **Change to 24 Hours (1 Day):**
```json
"MaximumPasswordAgeDays": 1  // 1440 minutes ÷ 1440
```

### **Change to 90 Days (Production):**
```json
"MaximumPasswordAgeDays": 90  // Standard production value
```

---

## ?? **Code Explanation:**

In `PasswordAgeService.cs`, the conversion happens here:

```csharp
// Configuration stores as DAYS (double)
var maxAge = TimeSpan.FromDays(_settings.MaximumPasswordAgeDays);

// Example with 30 minutes (0.02083 days):
// maxAge = TimeSpan.FromDays(0.02083)
// maxAge = 30 minutes exactly
```

The `TimeSpan.FromDays()` method handles the conversion automatically:
- Takes a `double` representing days
- Converts to exact TimeSpan with hours, minutes, seconds

---

## ?? **Verification:**

### **Check Your Settings Work:**

```csharp
// 30 minutes in different formats:
TimeSpan.FromDays(0.02083)     // = 00:30:00 (30 minutes)
TimeSpan.FromMinutes(30)       // = 00:30:00 (30 minutes)
TimeSpan.FromSeconds(1800)   // = 00:30:00 (30 minutes)

// All produce the same result!
```

### **Verify in Database:**

```sql
-- Check password age for a user
SELECT 
    Email,
    PasswordLastChangedDate,
    DATEDIFF(MINUTE, PasswordLastChangedDate, GETUTCDATE()) as MinutesOld,
    CASE 
     WHEN DATEDIFF(MINUTE, PasswordLastChangedDate, GETUTCDATE()) >= 30 
  THEN 'EXPIRED'
      ELSE 'Valid - Expires in ' + 
             CAST(30 - DATEDIFF(MINUTE, PasswordLastChangedDate, GETUTCDATE()) AS VARCHAR) + 
    ' minutes'
    END as Status
FROM AspNetUsers
WHERE Email = 'your@email.com';
```

---

## ?? **Pro Tips:**

1. **Testing:** Use shorter durations (1-30 minutes) for quick testing
2. **Development:** Use 1-4 hours for development testing
3. **Staging:** Use 24 hours to simulate production
4. **Production:** Use 90 days (industry standard)

5. **Minimum Age:** Usually set to prevent rapid changes
   - Testing: 1 minute
   - Production: 1 day (1440 minutes)

6. **Precision:** Use at least 5-6 decimal places for short durations
   - ? Good: 0.02083 (30 minutes)
- ? Poor: 0.02 (28.8 minutes - inaccurate)

---

## ?? **Common Conversions:**

### **Minutes to Days (Quick Formula):**
```
For X minutes:
Days = X ÷ 1440

Examples:
1 min   = 1 ÷ 1440    = 0.000694
5 min= 5 ÷ 1440    = 0.003472
15 min  = 15 ÷ 1440   = 0.010417
30 min  = 30 ÷ 1440   = 0.020833
60 min  = 60 ÷ 1440   = 0.041667
120 min = 120 ÷ 1440  = 0.083333
```

### **Days to Minutes (Reverse):**
```
For X days:
Minutes = X × 1440

Examples:
0.02083 days = 0.02083 × 1440 = 30 minutes
0.041667 days = 0.041667 × 1440 = 60 minutes
1 day = 1 × 1440 = 1440 minutes
```

---

## ? **Summary:**

**Current Configuration:**
- Minimum Age: **1 minute**
- Maximum Age: **30 minutes** (0.02083 days)
- Warning: **Always shown**

**To Apply Changes:**
1. ? Configuration updated in `appsettings.json`
2. ?? **Restart application required**
3. ? Test with new 30-minute expiry

**Formula to Remember:**
```
Days = Minutes ÷ 1440
Minutes = Days × 1440
```

---

**Status:** ? Updated to 30 minutes  
**Restart:** ?? Required  
**Testing:** Password now expires in 30 minutes
