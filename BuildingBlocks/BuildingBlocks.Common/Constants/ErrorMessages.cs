namespace BuildingBlocks.Common.Constants;

public static class ErrorMessages
{
    public static class Auth
    {
        public const string InvalidCredentials = "Invalid email or password.";
        public const string EmailNotConfirmed = "Email is not confirmed.";
        public const string AccountLocked = "Account is locked. Please try again later.";
        public const string InvalidToken = "Invalid or expired token.";
        public const string RefreshTokenExpired = "Refresh token has expired.";
        public const string RefreshTokenRevoked = "Refresh token has been revoked.";
        public const string UserNotFound = "User not found.";
        public const string EmailAlreadyExists = "Email is already registered.";
        public const string UsernameAlreadyExists = "Username is already taken.";
    }

    public static class Validation
    {
        public const string RequiredField = "This field is required.";
        public const string InvalidEmail = "Invalid email format.";
        public const string InvalidPhoneNumber = "Invalid phone number format.";
        public const string PasswordTooWeak = "Password must contain at least 8 characters, including uppercase, lowercase, number, and special character.";
        public const string PasswordMismatch = "Passwords do not match.";
        public const string InvalidUsername = "Username can only contain letters, numbers, and underscores.";
    }

    public static class General
    {
        public const string NotFound = "Resource not found.";
        public const string Unauthorized = "Unauthorized access.";
        public const string Forbidden = "Access forbidden.";
        public const string BadRequest = "Bad request.";
        public const string InternalServerError = "An unexpected error occurred.";
        public const string Conflict = "Resource conflict occurred.";
    }
}
