using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;
using System.Security.Claims;

namespace SIMS.Web.Controllers;

[Authorize(Roles = AppRoles.Student)]
public class StudentPortalController : Controller
{
    private readonly IStudentPortalService _studentPortalService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IExportService _exportService;

    public StudentPortalController(IStudentPortalService studentPortalService, UserManager<AppUser> userManager, IExportService exportService)
    {
        _studentPortalService = studentPortalService;
        _userManager = userManager;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> ExportMyGrades(string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            "pdf" => ExportFormat.Pdf,
            _ => ExportFormat.Csv
        };

        try
        {
            var result = await _exportService.ExportStudentGradesAsync(GetUserId(), expFormat);
            return File(result.FileContents, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return Forbid();
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportMyEnrollments(string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        try
        {
            var result = await _exportService.ExportStudentEnrollmentsAsync(GetUserId(), expFormat);
            return File(result.FileContents, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return Forbid();
        }
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public async Task<IActionResult> Profile()
    {
        var profile = await _studentPortalService.GetMyProfileAsync(GetUserId());
        if (profile == null) return NotFound("Student profile not found.");

        var appUser = await _userManager.FindByIdAsync(GetUserId());
        if (appUser != null)
        {
            profile.FullName = $"{appUser.FirstName} {appUser.LastName}";
            profile.Email = appUser.Email ?? "";
        }

        return View(profile);
    }

    public async Task<IActionResult> Registration()
    {
        var courses = await _studentPortalService.GetAvailableCoursesAsync(GetUserId());
        return View(courses);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(Guid courseId)
    {
        var success = await _studentPortalService.EnrollInCourseAsync(GetUserId(), courseId);
        if (success) TempData["SuccessMessage"] = "Đăng ký học phần thành công!";
        else TempData["ErrorMessage"] = "Đăng ký thất bại. Bạn có thể đã đăng ký môn này rồi.";
        
        return RedirectToAction(nameof(Registration));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Drop(Guid courseId)
    {
        var success = await _studentPortalService.DropCourseAsync(GetUserId(), courseId);
        if (success) TempData["SuccessMessage"] = "Hủy đăng ký học phần thành công!";
        else TempData["ErrorMessage"] = "Hủy thất bại. Không tìm thấy thông tin đăng ký.";
        
        return RedirectToAction(nameof(Registration));
    }

    public async Task<IActionResult> MyGrades()
    {
        var grades = await _studentPortalService.GetMyGradesAsync(GetUserId());
        return View(grades);
    }
}
