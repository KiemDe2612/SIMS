using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SIMS.Domain.Entities;

namespace SIMS.Persistence.Data;

public static class DbInitializer
{
    public static async Task SeedUsersAndRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure database is created
        await context.Database.MigrateAsync();

        string[] roleNames = { "Administrator", "Faculty", "Student" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 1. Create Admin
        var adminEmail = "admin@sims.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new AppUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true, FirstName = "System", LastName = "Admin" };
            await userManager.CreateAsync(adminUser, "Admin@123");
            await userManager.AddToRoleAsync(adminUser, "Administrator");
        }

        // 2. Create Departments
        var depts = new List<Department>
        {
            new Department { Code = "SE", Name = "Software Engineering", Description = "Khoa Kỹ thuật Phần mềm" },
            new Department { Code = "BA", Name = "Business Administration", Description = "Khoa Quản trị Kinh doanh" },
            new Department { Code = "GD", Name = "Graphic Design", Description = "Khoa Thiết kế Đồ họa" }
        };

        foreach (var d in depts)
        {
            if (!await context.Departments.AnyAsync(x => x.Code == d.Code))
            {
                context.Departments.Add(d);
            }
        }
        await context.SaveChangesAsync();

        var seDept = await context.Departments.FirstAsync(d => d.Code == "SE");
        var baDept = await context.Departments.FirstAsync(d => d.Code == "BA");
        var gdDept = await context.Departments.FirstAsync(d => d.Code == "GD");

        // 3. Create Faculties (3 faculties)
        var facultiesData = new[]
        {
            new { Email = "faculty@sims.com", First = "John", Last = "Doe", FId = "F123", Dept = seDept.Id },
            new { Email = "faculty2@sims.com", First = "Alice", Last = "Wong", FId = "F124", Dept = baDept.Id },
            new { Email = "faculty3@sims.com", First = "Bob", Last = "Smith", FId = "F125", Dept = gdDept.Id }
        };

        foreach (var f in facultiesData)
        {
            var user = await userManager.FindByEmailAsync(f.Email);
            if (user == null)
            {
                user = new AppUser { UserName = f.Email, Email = f.Email, EmailConfirmed = true, FirstName = f.First, LastName = f.Last };
                await userManager.CreateAsync(user, "Faculty@123");
                await userManager.AddToRoleAsync(user, "Faculty");
            }
            if (!await context.Faculties.AnyAsync(x => x.UserId == user.Id))
            {
                context.Faculties.Add(new Faculty { UserId = user.Id, FacultyId = f.FId, Title = "Dr", HireDate = DateTime.UtcNow.AddYears(-5), DepartmentId = f.Dept });
            }
        }
        await context.SaveChangesAsync();

        // 4. Create Students (10 students)
        var studentsData = new[]
        {
            new { Email = "student@sims.com", First = "Jane", Last = "Smith", SId = "SE12345", Dept = seDept.Id },
            new { Email = "se11111@sims.com", First = "Nguyen", Last = "Van A", SId = "SE11111", Dept = seDept.Id },
            new { Email = "se22222@sims.com", First = "Tran", Last = "Thi B", SId = "SE22222", Dept = seDept.Id },
            new { Email = "se33333@sims.com", First = "Le", Last = "Hoang C", SId = "SE33333", Dept = seDept.Id },
            new { Email = "se44444@sims.com", First = "Pham", Last = "Minh D", SId = "SE44444", Dept = seDept.Id },
            new { Email = "ba55555@sims.com", First = "Vo", Last = "Thi E", SId = "BA55555", Dept = baDept.Id },
            new { Email = "ba66666@sims.com", First = "Dang", Last = "Khoa", SId = "BA66666", Dept = baDept.Id },
            new { Email = "ba77777@sims.com", First = "Bui", Last = "Ngoc", SId = "BA77777", Dept = baDept.Id },
            new { Email = "gd88888@sims.com", First = "Ho", Last = "Tuan", SId = "GD88888", Dept = gdDept.Id },
            new { Email = "gd99999@sims.com", First = "Do", Last = "Quyen", SId = "GD99999", Dept = gdDept.Id }
        };

        foreach (var s in studentsData)
        {
            var user = await userManager.FindByEmailAsync(s.Email);
            if (user == null)
            {
                user = new AppUser { UserName = s.Email, Email = s.Email, EmailConfirmed = true, FirstName = s.First, LastName = s.Last };
                await userManager.CreateAsync(user, "Student@123"); // Note: Using standard password for demo
                await userManager.AddToRoleAsync(user, "Student");
            }
            if (!await context.Students.AnyAsync(x => x.UserId == user.Id))
            {
                context.Students.Add(new Student { UserId = user.Id, StudentId = s.SId, DateOfBirth = new DateTime(2002, 1, 1), Address = "Vietnam", EnrollmentDate = DateTime.UtcNow.AddYears(-1), DepartmentId = s.Dept });
            }
        }
        await context.SaveChangesAsync();

        // 5. Create Courses (5 courses)
        var f1 = await context.Faculties.FirstOrDefaultAsync(f => f.FacultyId == "F123");
        var f2 = await context.Faculties.FirstOrDefaultAsync(f => f.FacultyId == "F124");
        var f3 = await context.Faculties.FirstOrDefaultAsync(f => f.FacultyId == "F125");

        var coursesData = new[]
        {
            new { Code = "PRN211", Title = "Basic Cross-Platform App with .NET", Credits = 3, Dept = seDept.Id, Fac = f1?.Id },
            new { Code = "PRJ301", Title = "Java Web Application", Credits = 3, Dept = seDept.Id, Fac = f1?.Id },
            new { Code = "MGT101", Title = "Introduction to Management", Credits = 3, Dept = baDept.Id, Fac = f2?.Id },
            new { Code = "MKT201", Title = "Marketing Principles", Credits = 3, Dept = baDept.Id, Fac = f2?.Id },
            new { Code = "DSG101", Title = "Color Theory", Credits = 3, Dept = gdDept.Id, Fac = f3?.Id }
        };

        foreach (var c in coursesData)
        {
            if (!await context.Courses.AnyAsync(x => x.Code == c.Code))
            {
                context.Courses.Add(new Course { Code = c.Code, Title = c.Title, Description = "Sample description", Credits = c.Credits, DepartmentId = c.Dept, AssignedFacultyId = c.Fac });
            }
        }
        await context.SaveChangesAsync();

        // 6. Create Semesters
        if (!await context.Semesters.AnyAsync(s => s.Name == "Fall 2026"))
        {
            context.Semesters.Add(new Semester { Name = "Fall 2026", StartDate = new DateTime(2026, 9, 1), EndDate = new DateTime(2026, 12, 31), IsActive = true });
        }
        if (!await context.Semesters.AnyAsync(s => s.Name == "Spring 2026"))
        {
            context.Semesters.Add(new Semester { Name = "Spring 2026", StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 5, 31), IsActive = false });
        }
        await context.SaveChangesAsync();

        // 7. Enroll Students in Courses
        var semesterFall = await context.Semesters.FirstAsync(s => s.Name == "Fall 2026");
        var semesterSpring = await context.Semesters.FirstAsync(s => s.Name == "Spring 2026");
        var allStudents = await context.Students.ToListAsync();
        var prn211 = await context.Courses.FirstAsync(c => c.Code == "PRN211");
        var prj301 = await context.Courses.FirstAsync(c => c.Code == "PRJ301");

        // Enroll SE students in PRN211 and PRJ301
        var seStudents = allStudents.Where(s => s.DepartmentId == seDept.Id).ToList();
        foreach (var s in seStudents)
        {
            if (!await context.Enrollments.AnyAsync(e => e.StudentId == s.Id && e.CourseId == prn211.Id))
                context.Enrollments.Add(new Enrollment { StudentId = s.Id, CourseId = prn211.Id, SemesterId = semesterFall.Id, EnrollmentDate = DateTime.UtcNow });
            
            // Give them some grades in Spring
            if (!await context.Enrollments.AnyAsync(e => e.StudentId == s.Id && e.CourseId == prj301.Id))
                context.Enrollments.Add(new Enrollment { StudentId = s.Id, CourseId = prj301.Id, SemesterId = semesterSpring.Id, EnrollmentDate = DateTime.UtcNow.AddMonths(-6), Grade = new Random().Next(50, 100) / 10.0 });
        }
        await context.SaveChangesAsync();
    }
}
