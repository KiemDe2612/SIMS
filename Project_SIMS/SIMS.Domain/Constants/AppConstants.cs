namespace SIMS.Domain.Constants;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Faculty = "Faculty";
    public const string Student = "Student";
}

public static class AppConstants
{
    public const string DefaultEmailDomain = "@sims.com";
    public const string DefaultPasswordSuffix = "@123";
    public const int MaxCreditsPerSemester = 24; // Giới hạn tín chỉ tối đa mỗi học kỳ
}

