using System.Text.RegularExpressions;
using System.Web;

namespace WebApp_Core_Identity.Helpers
{
    /// <summary>
    /// Input validation and sanitization helper for XSS prevention
    /// </summary>
    public static class InputValidationHelper
    {
     // Dangerous HTML/Script patterns
        private static readonly string[] DangerousPatterns = new[]
        {
         @"<script[^>]*>.*?</script>",
      @"javascript:",
      @"on\w+\s*=",
            @"<iframe",
         @"<embed",
 @"<object",
    @"eval\(",
@"expression\(",
 @"vbscript:",
   @"data:text/html"
      };

        /// <summary>
  /// Sanitize input to prevent XSS attacks
    /// </summary>
        public static string SanitizeInput(string? input)
 {
            if (string.IsNullOrWhiteSpace(input))
      return string.Empty;

    // HTML encode the input
            string sanitized = HttpUtility.HtmlEncode(input);

            // Check for dangerous patterns
         foreach (var pattern in DangerousPatterns)
            {
            if (Regex.IsMatch(sanitized, pattern, RegexOptions.IgnoreCase))
    {
       throw new InvalidOperationException("Potentially dangerous input detected");
   }
            }

    return sanitized;
        }

        /// <summary>
        /// Validate email format
        /// </summary>
 public static bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
 return false;

   try
       {
    var pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
     return Regex.IsMatch(email, pattern);
      }
 catch
            {
     return false;
   }
        }

        /// <summary>
 /// Validate phone number (Singapore format: 8 digits starting with 6/8/9)
     /// </summary>
        public static bool IsValidPhoneNumber(string? phone)
 {
            if (string.IsNullOrWhiteSpace(phone))
      return false;

    var pattern = @"^[689]\d{7}$";
return Regex.IsMatch(phone, pattern);
        }

     /// <summary>
   /// Validate credit card number (16 digits)
        /// </summary>
        public static bool IsValidCreditCard(string? cardNumber)
        {
       if (string.IsNullOrWhiteSpace(cardNumber))
      return false;

            // Remove spaces
      cardNumber = cardNumber.Replace(" ", "");

 // Check if 16 digits
      if (!Regex.IsMatch(cardNumber, @"^\d{16}$"))
      return false;

        // Luhn algorithm for credit card validation
       return IsValidLuhn(cardNumber);
    }

        private static bool IsValidLuhn(string cardNumber)
  {
            int sum = 0;
    bool alternate = false;

for (int i = cardNumber.Length - 1; i >= 0; i--)
   {
           int digit = int.Parse(cardNumber[i].ToString());

             if (alternate)
          {
             digit *= 2;
  if (digit > 9)
  digit -= 9;
     }

  sum += digit;
   alternate = !alternate;
       }

   return sum % 10 == 0;
        }

     /// <summary>
      /// Validate file extension
        /// </summary>
        public static bool IsValidFileExtension(string fileName, string[] allowedExtensions)
 {
            if (string.IsNullOrWhiteSpace(fileName))
    return false;

            var extension = Path.GetExtension(fileName).ToLower();
return allowedExtensions.Contains(extension);
        }

        /// <summary>
        /// Validate file size
        /// </summary>
        public static bool IsValidFileSize(long fileSize, long maxSizeInBytes)
     {
         return fileSize > 0 && fileSize <= maxSizeInBytes;
        }

        /// <summary>
        /// Check if file is actually a JPG by reading file signature (magic numbers)
     /// </summary>
      public static bool IsValidJpgFile(Stream fileStream)
   {
            try
   {
              // JPG magic numbers: FF D8 FF
             byte[] jpgSignature = new byte[] { 0xFF, 0xD8, 0xFF };
        byte[] fileHeader = new byte[3];

    fileStream.Position = 0;
                fileStream.Read(fileHeader, 0, 3);
            fileStream.Position = 0;

     return fileHeader.SequenceEqual(jpgSignature);
     }
            catch
  {
      return false;
            }
        }

        /// <summary>
        /// Sanitize filename to prevent directory traversal
        /// </summary>
        public static string SanitizeFileName(string fileName)
        {
            // Remove path characters
            string sanitized = Path.GetFileName(fileName);

  // Remove invalid characters
   var invalidChars = Path.GetInvalidFileNameChars();
   sanitized = string.Join("_", sanitized.Split(invalidChars));

       return sanitized;
}

        /// <summary>
        /// Validate against SQL injection patterns
        /// </summary>
public static bool ContainsSqlInjectionPatterns(string input)
    {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var sqlPatterns = new[]
            {
         @"(\s|^)(union|select|insert|update|delete|drop|create|alter|exec|execute)(\s|$)",
                @"--",
     @";",
                @"'.*or.*'.*=.*'",
    @"1.*=.*1",
     @"xp_"
 };

       foreach (var pattern in sqlPatterns)
     {
        if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
     return true;
         }

     return false;
        }
    }
}
