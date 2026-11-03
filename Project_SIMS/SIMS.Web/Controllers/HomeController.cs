using Microsoft.AspNetCore.Mvc;
using SIMS.Domain.Entities;
using SIMS.Domain.Interfaces;

namespace SIMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _unitOfWork.Repository<Student>().GetAllAsync();
        var courses = await _unitOfWork.Repository<Course>().GetAllAsync();
        var faculties = await _unitOfWork.Repository<Faculty>().GetAllAsync();
        
        ViewBag.TotalStudents = students.Count();
        ViewBag.TotalCourses = courses.Count();
        ViewBag.TotalFaculties = faculties.Count();

        return View();
    }
}
