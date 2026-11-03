using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;

namespace SIMS.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class CoursesController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IExportService _exportService;

    public CoursesController(ICourseService courseService, IExportService exportService)
    {
        _courseService = courseService;
        _exportService = exportService;
    }

    [HttpGet]
    public async Task<IActionResult> Export(string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await _exportService.ExportCoursesAsync(userId, expFormat);
        return File(result.FileContents, result.ContentType, result.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCourseGrades(Guid id, string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await _exportService.ExportCourseGradesAsync(userId, id, expFormat);
        return File(result.FileContents, result.ContentType, result.FileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportEnrollments(Guid? semesterId, string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            _ => ExportFormat.Csv
        };

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await _exportService.ExportEnrollmentsBySemesterAsync(userId, semesterId, expFormat);
        return File(result.FileContents, result.ContentType, result.FileName);
    }

    public async Task<IActionResult> Index()
    {
        var courses = await _courseService.GetAllCoursesAsync();
        return View(courses);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = new SelectList(await _courseService.GetDepartmentsAsync(), "Id", "Name");
        // In a real app we'd map Faculty names properly, but for demo we just show IDs or titles
        var faculties = await _courseService.GetFacultiesAsync();
        ViewBag.Faculties = new SelectList(faculties, "Id", "FacultyId");
        
        var courses = await _courseService.GetAllCoursesAsync();
        ViewBag.Prerequisites = new SelectList(courses, "Id", "Title");

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCourseDto model)
    {
        if (ModelState.IsValid)
        {
            await _courseService.CreateCourseAsync(model);
            TempData["SuccessMessage"] = "Course created successfully!";
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.Departments = new SelectList(await _courseService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        ViewBag.Faculties = new SelectList(await _courseService.GetFacultiesAsync(), "Id", "FacultyId", model.AssignedFacultyId);
        ViewBag.Prerequisites = new SelectList(await _courseService.GetAllCoursesAsync(), "Id", "Title", model.PrerequisiteCourseId);
        return View(model);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var course = await _courseService.GetCourseByIdAsync(id);
        if (course == null) return NotFound();

        var model = new UpdateCourseDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            Credits = course.Credits,
            DepartmentId = course.DepartmentId,
            AssignedFacultyId = course.AssignedFacultyId,
            PrerequisiteCourseId = course.PrerequisiteCourseId
        };

        ViewBag.Departments = new SelectList(await _courseService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        ViewBag.Faculties = new SelectList(await _courseService.GetFacultiesAsync(), "Id", "FacultyId", model.AssignedFacultyId);
        ViewBag.Prerequisites = new SelectList(await _courseService.GetAllCoursesAsync(), "Id", "Title", model.PrerequisiteCourseId);
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateCourseDto model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _courseService.UpdateCourseAsync(model);
            TempData["SuccessMessage"] = "Course updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Departments = new SelectList(await _courseService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        ViewBag.Faculties = new SelectList(await _courseService.GetFacultiesAsync(), "Id", "FacultyId", model.AssignedFacultyId);
        ViewBag.Prerequisites = new SelectList(await _courseService.GetAllCoursesAsync(), "Id", "Title", model.PrerequisiteCourseId);
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _courseService.DeleteCourseAsync(id);
            TempData["SuccessMessage"] = "Môn học đã được xóa (Soft Delete) thành công!";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
