using Microsoft.Extensions.Logging;
using SIMS.Application.Interfaces;

namespace SIMS.Application.Services.Observers;

public class NotificationEnrollmentObserver : IEnrollmentObserver
{
    private readonly ILogger<NotificationEnrollmentObserver>? _logger;

    public NotificationEnrollmentObserver(ILogger<NotificationEnrollmentObserver>? logger = null)
    {
        _logger = logger;
    }

    public Task OnEnrollmentCreatedAsync(EnrollmentCreatedEvent eventData)
    {
        // Gửi thông báo xác nhận đăng ký môn học cho sinh viên
        _logger?.LogInformation(
            "[NotificationObserver] Sinh viên {StudentCode} đã đăng ký thành công môn học {CourseCode} vào {Date}.",
            eventData.StudentCode,
            eventData.CourseCode,
            eventData.EnrollmentDate);

        return Task.CompletedTask;
    }
}

