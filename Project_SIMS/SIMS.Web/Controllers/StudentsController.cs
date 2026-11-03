using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;

namespace SIMS.Web.Controllers;

[Authorize(Roles = AppRoles.Administrator)]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IExportService _exportService;
    private readonly IUserFactory _userFactory;

    public StudentsController(
        IStudentService studentService, 
        UserManager<AppUser> userManager, 
        IExportService exportService,
        IUserFactory userFactory)
    {
        _studentService = studentService;
        _userManager = userManager;
        _exportService = exportService;
        _userFactory = userFactory;
    }

    [HttpGet]
    public async Task<IActionResult> Export(string format = "csv")
    {
        var expFormat = format?.ToLower() switch
        {
            "excel" or "xlsx" => ExportFormat.Excel,
            "pdf" => ExportFormat.Pdf,
            _ => ExportFormat.Csv
        };

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "";
        var result = await _exportService.ExportStudentsAsync(userId, expFormat);
        return File(result.FileContents, result.ContentType, result.FileName);
    }

    public async Task<IActionResult> Index()
    {
        var students = (await _studentService.GetAllStudentsAsync()).ToList();
        var emails = students.Select(s => $"{s.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}").ToHashSet();

        // Tối ưu loại bỏ N+1 query: lấy toàn bộ AppUser trong 1 lần truy vấn SQL duy nhất
        var users = await _userManager.Users
            .Where(u => emails.Contains(u.Email!.ToLower()))
            .ToDictionaryAsync(u => u.Email!.ToLower());

        foreach (var student in students)
        {
            string email = $"{student.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}";
            if (users.TryGetValue(email, out var appUser))
            {
                student.FirstName = appUser.FirstName;
                student.LastName = appUser.LastName;
                student.Email = appUser.Email ?? "";
            }
        }
        return View(students);
    }

    public async Task<IActionResult> Create()
    {
        ViewBag.Departments = new SelectList(await _studentService.GetDepartmentsAsync(), "Id", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStudentDto model)
    {
        if (ModelState.IsValid)
        {
            // Delegate creation to UserFactory (Factory Method pattern)
            var userReq = new CreateUserRequest
            {
                Identifier = model.StudentId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Email = model.Email,
                Role = AppRoles.Student
            };

            var (result, newUser) = await _userFactory.CreateUserWithRoleAsync(userReq);
            if (result.Succeeded && newUser != null)
            {
                model.UserId = newUser.Id;
                await _studentService.CreateStudentAsync(model);

                TempData["SuccessMessage"] = "Student created successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }

        ViewBag.Departments = new SelectList(await _studentService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        return View(model);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student == null) return NotFound();

        // Get AppUser for name
        var appUser = _userManager.Users.FirstOrDefault(u => u.Email == $"{student.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}");
        
        var model = new UpdateStudentDto
        {
            Id = student.Id,
            FirstName = appUser?.FirstName ?? "",
            LastName = appUser?.LastName ?? "",
            DateOfBirth = student.DateOfBirth,
            Address = student.Address,
            DepartmentId = student.DepartmentId
        };

        ViewBag.Departments = new SelectList(await _studentService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateStudentDto model)
    {
        if (id != model.Id) return NotFound();

        if (ModelState.IsValid)
        {
            await _studentService.UpdateStudentAsync(model);

            // Update AppUser names
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student != null)
            {
                var appUser = _userManager.Users.FirstOrDefault(u => u.Email == $"{student.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}");
                if (appUser != null)
                {
                    appUser.FirstName = model.FirstName;
                    appUser.LastName = model.LastName;
                    await _userManager.UpdateAsync(appUser);
                }
            }

            TempData["SuccessMessage"] = "Student updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Departments = new SelectList(await _studentService.GetDepartmentsAsync(), "Id", "Name", model.DepartmentId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student != null)
            {
                // Soft delete student record (throws InvalidOperationException if enrolled)
                await _studentService.DeleteStudentAsync(id);

                // Deactivate Identity user
                var appUser = _userManager.Users.FirstOrDefault(u => u.Email == $"{student.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}");
                if (appUser != null)
                {
                    appUser.IsActive = false;
                    await _userManager.UpdateAsync(appUser);
                }

                TempData["SuccessMessage"] = "Sinh viên đã được xóa (Soft Delete) thành công!";
            }
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
