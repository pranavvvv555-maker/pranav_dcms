using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class StudentService(AppDbContext db)
{
    public async Task<List<Student>> GetAllAsync()
        => await db.Students
            .Include(s => s.Semester)
            .OrderBy(s => s.Id)
            .ToListAsync();

    public async Task<Student?> GetByIdAsync(int id)
        => await db.Students
            .Include(s => s.Semester)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<List<Semester>> GetSemestersAsync()
        => await db.Semesters.OrderBy(s => s.Name).ToListAsync();

    public async Task<Semester?> GetActiveSemesterAsync()
        => await db.Semesters.FirstOrDefaultAsync(s => s.IsActive)
           ?? await db.Semesters.FirstOrDefaultAsync();

    public async Task<string> GenerateNextStudentCodeAsync()
    {
        var existingCodes = await db.Students
            .Select(s => s.StudentCode)
            .ToListAsync();

        long maxCode = 1272262319;
        foreach (var code in existingCodes)
        {
            if (long.TryParse(code, out var num) && num > maxCode)
            {
                maxCode = num;
            }
        }
        return (maxCode + 1).ToString();
    }

    public async Task<bool> IsStudentCodeTakenAsync(string studentCode, int excludeId = 0)
    {
        if (string.IsNullOrWhiteSpace(studentCode)) return false;
        var trimmed = studentCode.Trim();
        return await db.Students
            .AnyAsync(s => s.StudentCode.ToLower() == trimmed.ToLower() && s.Id != excludeId);
    }

    public async Task<Student> CreateAsync(Student student)
    {
        student.StudentCode = student.StudentCode.Trim();
        student.FullName = student.FullName.Trim();
        student.Email = string.IsNullOrWhiteSpace(student.Email) ? null : student.Email.Trim();
        student.MobileNumber = string.IsNullOrWhiteSpace(student.MobileNumber) ? null : student.MobileNumber.Trim();
        student.Gender = string.IsNullOrWhiteSpace(student.Gender) ? null : student.Gender.Trim();
        student.EnrollmentStatus = string.IsNullOrWhiteSpace(student.EnrollmentStatus) ? "Active" : student.EnrollmentStatus.Trim();

        db.Students.Add(student);
        await db.SaveChangesAsync();
        return student;
    }

    public async Task UpdateAsync(Student student)
    {
        var existing = await db.Students.FindAsync(student.Id)
            ?? throw new InvalidOperationException("Student not found.");

        existing.FullName = student.FullName.Trim();
        existing.StudentCode = student.StudentCode.Trim();
        existing.MobileNumber = string.IsNullOrWhiteSpace(student.MobileNumber) ? null : student.MobileNumber.Trim();
        existing.Email = string.IsNullOrWhiteSpace(student.Email) ? null : student.Email.Trim();
        existing.Gender = string.IsNullOrWhiteSpace(student.Gender) ? null : student.Gender.Trim();
        existing.EnrollmentStatus = string.IsNullOrWhiteSpace(student.EnrollmentStatus) ? "Active" : student.EnrollmentStatus.Trim();
        existing.SemesterId = student.SemesterId;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var existing = await db.Students.FindAsync(id);
        if (existing != null)
        {
            db.Students.Remove(existing);
            await db.SaveChangesAsync();
        }
    }
}
