namespace YamyProject.Core.Consts
{
    public static class Errors
    {
        public const string MaxLength = "Length cannot be more than {1} characters";
        public const string Duplicated = "Another Record With The same {0} is already exists!";
        public const string DuplicatedBook = "Book with the same title is already exists with the same author!";
        public const string NotAllowedExtension = "Only .png, .jpg, .jpeg files are allowed!";
        public const string MaxSize = "File cannot be more that 2 MB!";
        public const string NotAllowFutureDates = "Date cannot be in the future!";
        public const string InvalidRange = "{0} should be between {1} and  {2}!";
        public const string InvalidDate = "Invalid date format!";
        public const string InvalidEmail = "The email format is invalid!";
        public const string InvalidPhoneNumber = "The phone number format is invalid!";
        public const string MaxMinLength = "The {0} must be at least {2} and at max {1} characters long.";
        public const string ConfirmPasswordsNotMatch = "The password and confirmation password do not match.";
        public const string PasswordComplexity = "The password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
        public const string InvalidUserName = "The username format is invalid!";
        public const string RequiredFilde = "Required Filde!";
    }
}
