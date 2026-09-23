using Microsoft.EntityFrameworkCore;
using DCMSApp.Data.Entities;

namespace DCMSApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Faculty> Faculties => Set<Faculty>();
    public DbSet<FacultyRate> FacultyRates => Set<FacultyRate>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<CourseFaculty> CourseFaculties => Set<CourseFaculty>();
    public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<PaymentPeriod> PaymentPeriods => Set<PaymentPeriod>();
    public DbSet<PaymentLineItem> PaymentLineItems => Set<PaymentLineItem>();
    public DbSet<PaymentAdjustment> PaymentAdjustments => Set<PaymentAdjustment>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<SessionAttendance> SessionAttendances => Set<SessionAttendance>();
    public DbSet<Assessment> Assessments => Set<Assessment>();
    public DbSet<StudentAssessmentMark> StudentAssessmentMarks => Set<StudentAssessmentMark>();
    public DbSet<MockInterviewDrive> MockInterviewDrives => Set<MockInterviewDrive>();
    public DbSet<MockInterviewEvaluation> MockInterviewEvaluations => Set<MockInterviewEvaluation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Faculty
        modelBuilder.Entity<Faculty>(e =>
        {
            e.HasIndex(f => f.OfficialEmail).IsUnique();
            e.Property(f => f.FullName).HasMaxLength(200).IsRequired();
        });

        // FacultyRate
        modelBuilder.Entity<FacultyRate>(e =>
        {
            e.Property(r => r.HourlyRateINR).HasPrecision(10, 2);
            e.Property(r => r.LectureRateINR).HasPrecision(10, 2);
            e.Property(r => r.PracticalRateINR).HasPrecision(10, 2);
            e.HasOne(r => r.Faculty)
             .WithMany(f => f.Rates)
             .HasForeignKey(r => r.FacultyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Semester
        modelBuilder.Entity<Semester>(e =>
        {
            e.Property(s => s.Name).HasMaxLength(50).IsRequired();
        });

        // Course
        modelBuilder.Entity<Course>(e =>
        {
            e.HasIndex(c => c.CourseCode).IsUnique();
            e.Property(c => c.CourseCode).HasMaxLength(20).IsRequired();
            e.Property(c => c.CourseName).HasMaxLength(200).IsRequired();
            e.HasOne(c => c.Semester)
             .WithMany(s => s.Courses)
             .HasForeignKey(c => c.SemesterId);
        });

        // CourseFaculty
        modelBuilder.Entity<CourseFaculty>(e =>
        {
            e.HasOne(cf => cf.Course)
             .WithMany(c => c.CourseFaculties)
             .HasForeignKey(cf => cf.CourseId);
            e.HasOne(cf => cf.Faculty)
             .WithMany(f => f.CourseFaculties)
             .HasForeignKey(cf => cf.FacultyId);
        });

        // TimetableSlot
        modelBuilder.Entity<TimetableSlot>(e =>
        {
            e.HasOne(t => t.Semester)
             .WithMany(s => s.TimetableSlots)
             .HasForeignKey(t => t.SemesterId);
            e.HasOne(t => t.Course)
             .WithMany(c => c.TimetableSlots)
             .HasForeignKey(t => t.CourseId);
            e.HasOne(t => t.Faculty)
             .WithMany(f => f.TimetableSlots)
             .HasForeignKey(t => t.FacultyId);
        });

        // Session
        modelBuilder.Entity<Session>(e =>
        {
            e.Property(s => s.DurationHours).HasPrecision(5, 2);
            e.HasOne(s => s.TimetableSlot)
             .WithMany(t => t.Sessions)
             .HasForeignKey(s => s.TimetableSlotId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(s => s.Course)
             .WithMany(c => c.Sessions)
             .HasForeignKey(s => s.CourseId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.Faculty)
             .WithMany(f => f.Sessions)
             .HasForeignKey(s => s.FacultyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.SubstituteFaculty)
             .WithMany()
             .HasForeignKey(s => s.SubstituteFacultyId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // PaymentPeriod
        modelBuilder.Entity<PaymentPeriod>(e =>
        {
            e.Property(p => p.PeriodName).HasMaxLength(50).IsRequired();
        });

        // PaymentLineItem
        modelBuilder.Entity<PaymentLineItem>(e =>
        {
            e.Property(p => p.LectureRate).HasPrecision(10, 2);
            e.Property(p => p.PracticalRate).HasPrecision(10, 2);
            e.Property(p => p.TotalHours).HasPrecision(8, 2);
            e.Property(p => p.HourlyRate).HasPrecision(10, 2);
            e.Property(p => p.GrossAmount).HasPrecision(12, 2);
            e.Property(p => p.Deductions).HasPrecision(12, 2);
            e.Property(p => p.NetPayable).HasPrecision(12, 2);
            e.HasOne(p => p.PaymentPeriod)
             .WithMany(pp => pp.LineItems)
             .HasForeignKey(p => p.PaymentPeriodId);
            e.HasOne(p => p.Faculty)
             .WithMany(f => f.PaymentLineItems)
             .HasForeignKey(p => p.FacultyId);
        });

        // PaymentAdjustment
        modelBuilder.Entity<PaymentAdjustment>(e =>
        {
            e.Property(a => a.Amount).HasPrecision(12, 2);
            e.HasOne(a => a.PaymentPeriod)
             .WithMany(pp => pp.Adjustments)
             .HasForeignKey(a => a.PaymentPeriodId);
            e.HasOne(a => a.Faculty)
             .WithMany()
             .HasForeignKey(a => a.FacultyId);
        });

        // Student
        modelBuilder.Entity<Student>(e =>
        {
            e.HasIndex(s => s.StudentCode).IsUnique();
            e.Property(s => s.StudentCode).HasMaxLength(20).IsRequired();
            e.Property(s => s.FullName).HasMaxLength(200).IsRequired();
            e.Property(s => s.Email).HasMaxLength(200);
            e.Property(s => s.MobileNumber).HasMaxLength(20);
            e.Property(s => s.Gender).HasMaxLength(10);
            e.HasOne(s => s.Semester)
             .WithMany()
             .HasForeignKey(s => s.SemesterId);
        });

        // Student attendance register
        modelBuilder.Entity<SessionAttendance>(e =>
        {
            e.HasIndex(a => new { a.SessionId, a.StudentId }).IsUnique();
            e.HasOne(a => a.Session)
             .WithMany(s => s.AttendanceRecords)
             .HasForeignKey(a => a.SessionId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Student)
             .WithMany(s => s.AttendanceRecords)
             .HasForeignKey(a => a.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Assessment
        modelBuilder.Entity<Assessment>(e =>
        {
            e.Property(a => a.Title).HasMaxLength(200).IsRequired();
            e.Property(a => a.Type).HasMaxLength(50).IsRequired();
            e.Property(a => a.MaxMarks).HasPrecision(5, 2);
            e.Property(a => a.WeightagePercent).HasPrecision(5, 2);
            e.Property(a => a.Status).HasMaxLength(50).IsRequired();
            e.HasOne(a => a.Course)
             .WithMany()
             .HasForeignKey(a => a.CourseId);
            e.HasOne(a => a.Semester)
             .WithMany()
             .HasForeignKey(a => a.SemesterId);
        });

        // StudentAssessmentMark
        modelBuilder.Entity<StudentAssessmentMark>(e =>
        {
            e.HasIndex(m => new { m.AssessmentId, m.StudentId }).IsUnique();
            e.Property(m => m.MarksObtained).HasPrecision(5, 2);
            e.HasOne(m => m.Assessment)
             .WithMany(a => a.Marks)
             .HasForeignKey(m => m.AssessmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Student)
             .WithMany()
             .HasForeignKey(m => m.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // MockInterviewDrive
        modelBuilder.Entity<MockInterviewDrive>(e =>
        {
            e.Property(d => d.Title).HasMaxLength(200).IsRequired();
            e.Property(d => d.Status).HasMaxLength(50).IsRequired();
            e.Property(d => d.SubjectName).HasMaxLength(200);
            e.HasOne(d => d.Semester)
             .WithMany()
             .HasForeignKey(d => d.SemesterId);
            e.HasOne(d => d.Course)
             .WithMany()
             .HasForeignKey(d => d.CourseId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // MockInterviewEvaluation
        modelBuilder.Entity<MockInterviewEvaluation>(e =>
        {
            e.HasIndex(m => new { m.DriveId, m.StudentId }).IsUnique();
            e.Property(m => m.ConfidenceScore).HasPrecision(4, 2);
            e.Property(m => m.CommunicationScore).HasPrecision(4, 2);
            e.Property(m => m.TechnicalScore).HasPrecision(4, 2);
            e.Property(m => m.MarksOutOf10).HasPrecision(4, 2);
            e.Property(m => m.StudentGroup).HasMaxLength(50).IsRequired();
            e.Property(m => m.Status).HasMaxLength(50).IsRequired();
            e.Property(m => m.SubjectName).HasMaxLength(200);
            e.HasOne(m => m.Drive)
             .WithMany(d => d.Evaluations)
             .HasForeignKey(m => m.DriveId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Student)
             .WithMany()
             .HasForeignKey(m => m.StudentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(m => m.Interviewer)
             .WithMany()
             .HasForeignKey(m => m.InterviewerFacultyId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(m => m.Course)
             .WithMany()
             .HasForeignKey(m => m.CourseId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
