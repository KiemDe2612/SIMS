using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SIMS.Application.DTOs;
using SIMS.Application.Interfaces;
using SIMS.Domain.Constants;
using SIMS.Domain.Entities;

namespace SIMS.Web.Controllers;

[Authorize(Roles = "Administrator")]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly UserManager<AppUser> _userManager;

    public StudentsController(IStudentService studentService, UserManager<AppUser> userManager)
    {
        _studentService = studentService;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _studentService.GetAllStudentsAsync();
        // Since Generic Repo doesn't include AppUser, let's map the names here for UI
        foreach(var student in students)
        {
            // Note: In a real enterprise app, we'd use a raw SQL query or custom repo method with Include to avoid N+1 queries.
            // But this works for our small demo.
            var appUser = _userManager.Users.FirstOrDefault(u => u.Email == student.StudentId + AppConstants.DefaultEmailDomain);
            if (appUser != null)
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
            // 1. Create Identity User
            var email = $"{model.StudentId.ToLower()}{AppConstants.DefaultEmailDomain}";
            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = model.FirstName,
                LastName = model.LastName
            };

            var result = await _userManager.CreateAsync(newUser, $"{model.StudentId}{AppConstants.DefaultPasswordSuffix}");
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, "Student");

                // 2. Create Student Entity
                model.UserId = newUser.Id;
                await _studentService.CreateStudentAsync(model);

                TempData["SuccessMessage"] = "Student created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            foreach(var error in result.Errors)
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
        var appUser = _userManager.Users.FirstOrDefault(u => u.Email == student.StudentId + AppConstants.DefaultEmailDomain);
        
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
                var appUser = _userManager.Users.FirstOrDefault(u => u.Email == student.StudentId + AppConstants.DefaultEmailDomain);
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
        var student = await _studentService.GetStudentByIdAsync(id);
        if (student != null)
        {
            // Delete Identity user
            var appUser = _userManager.Users.FirstOrDefault(u => u.Email == student.StudentId + AppConstants.DefaultEmailDomain);
            if (appUser != null)
            {
                await _userManager.DeleteAsync(appUser);
            }

            // Delete Student
            await _studentService.DeleteStudentAsync(id);
            TempData["SuccessMessage"] = "Student deleted successfully!";
        }
        return RedirectToAction(nameof(Index));
    }
}
