using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class CourseService(AppDbContext db)
{
    public async Task<List<Course>> GetAllAsync()
        => await db.Courses.Include(c => c.Semester).OrderBy(c => c.CourseCode).ToListAsync();

    public async Task<List<Course>> GetBySemesterAsync(int semesterId)
        => await db.Courses.Where(c => c.SemesterId == semesterId).OrderBy(c => c.CourseCode).ToListAsync();

    public async Task<Course?> GetByIdAsync(int id)
        => await db.Courses.Include(c => c.CourseFaculties).ThenInclude(cf => cf.Faculty)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Semester>> GetSemestersAsync()
        => await db.Semesters.OrderBy(s => s.Name).ToListAsync();

    public async Task<Semester?> GetActiveSemesterAsync()
        => await db.Semesters.FirstOrDefaultAsync(s => s.IsActive);

    public async Task<Course> CreateAsync(Course course)
    {
        db.Courses.Add(course);
        await db.SaveChangesAsync();
        return course;
    }

    public async Task UpdateAsync(Course course)
    {
        db.Courses.Update(course);
        await db.SaveChangesAsync();
    }
}
