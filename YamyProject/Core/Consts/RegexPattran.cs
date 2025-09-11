namespace YamyProject.Core.Consts
{
    public static class RegexPattran
    {
        public const string EmailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        public const string UrlPattern = @"^(http|https)://[^\s/$.?#].[^\s]*$";
        public const string PhoneNumberPattern = @"^\+?[1-9]\d{10,16}$"; // E.164 format
        public const string PasswordComplexityPattern = @"^(?=.*[0-9])(?=.*[a-z])(?=.*[A-Z])(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,100}$";
        public const string UserName = @"^[a-zA-Z0-9_._@+]*$";
    }
}
