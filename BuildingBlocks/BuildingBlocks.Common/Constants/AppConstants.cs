namespace BuildingBlocks.Common.Constants;

public static class AppConstants
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string User = "User";
        public const string Moderator = "Moderator";
    }

    public static class Claims
    {
        public const string UserId = "uid";
        public const string Email = "email";
        public const string Role = "role";
        public const string UserName = "username";
    }

    public static class Pagination
    {
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 100;
    }

    public static class Validation
    {
        public const int MinPasswordLength = 8;
        public const int MaxPasswordLength = 128;
        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 50;
        public const int MaxEmailLength = 256;
        public const int MaxNameLength = 100;
    }

    public static class Token
    {
        public const int AccessTokenExpirationMinutes = 15;
        public const int RefreshTokenExpirationDays = 7;
    }
}
