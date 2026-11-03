using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using System.Security.Claims;

namespace SIMS.Web.Controllers;

[Authorize(Roles = $"{AppRoles.Faculty},{AppRoles.Administrator}")]
public class FacultyController : Controller
{
    private readonly IFacultyService _facultyService;
    private readonly IExportService _exportService;

    public FacultyController(IFacultyService facultyService, IExportService exportService)
    {
        _facultyService = facultyService;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> ExportGrades(Guid id, string format = "csv")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        try
        {
            var result = await _exportService.ExportFacultyCourseGradesAsync(userId, id, expFormat);
            return File(result.FileContents, result.ContentType, result.FileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var courses = await _facultyService.GetAssignedCoursesAsync(userId);
        return View(courses);
    }

    public async Task<IActionResult> CourseGrades(Guid id)
    {
        var course = await _facultyService.GetCourseByIdAsync(id);
        if (course == null) return NotFound();

        var grades = await _facultyService.GetCourseGradesAsync(id);
        
        ViewBag.CourseTitle = course.Title;
        ViewBag.CourseCode = course.Code;
        ViewBag.CourseId = course.Id;

        return View(grades);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveGrades(Guid courseId, List<CourseGradeUpdateDto> updates)
    {
        if (ModelState.IsValid)
        {
            await _facultyService.UpdateGradesAsync(updates);
            TempData["SuccessMessage"] = "Grades saved successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to save grades. Please check your inputs.";
        }

        return RedirectToAction(nameof(CourseGrades), new { id = courseId });
    }
}
