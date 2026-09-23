using DCMSApp.Data.Entities;
using DCMSApp.Services;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Data;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext db)
    {
        if (await db.Faculties.AnyAsync())
        {
            // Existing installations keep any customised fees. Professors who
            // have not yet had a fee configured receive the standard amount.
            await EnsureDefaultClassRatesAsync(db);
            await EnsureAssessmentsAsync(db);
            await EnsureMockInterviewsAsync(db);
            await EnsureAugust2026SessionsAsync(db);
            await EnsureInductionStudentsAsync(db);
            return;
        }

        // --- Semester ---
        var sem1 = new Semester
        {
            Name = "Semester I",
            AcademicYear = "2026-27",
            StartDate = new DateTime(2026, 8, 21),
            EndDate = new DateTime(2027, 1, 31),
            IsActive = true
        };
        db.Semesters.Add(sem1);
        await db.SaveChangesAsync();

        // --- Authentic Faculty Profiles (NIRVAA Solution Pvt Ltd) ---
        var faculties = new List<Faculty>
        {
            new() { FullName = "Dr. Jagdish Shinde", Designation = "Managing Director", Qualification = "Ph.D", ExperienceYears = "25+", Domain = "Data Centre", Role = "Teaching", OfficialEmail = "js@nirvaa.com", Phone = "8454948568", Organization = "NIRVAA" },
            new() { FullName = "Mr. Anup Goel", Designation = "Director", Qualification = "PG(Diploma), B.E", ExperienceYears = "20+", Domain = "Electrical and Data Centre Technology", Role = "Teaching", OfficialEmail = "anup.g@nirvaa.com", Phone = "9112250505", Organization = "NIRVAA" },
            new() { FullName = "Dr. Vivekanand M Bankolli", Designation = "Asst. General Manager", Qualification = "Ph.D", ExperienceYears = "30+", Domain = "Data Centre", Role = "Teaching", OfficialEmail = "vmb@nirvaa.com", Phone = "", Organization = "NIRVAA" },
            new() { FullName = "Mr. Siddu Patil", Designation = "IT Head", Qualification = "Ph.D(Pursuing), M.Tech", ExperienceYears = "15+", Domain = "Data Centre and Data Engineering", Role = "Teaching", OfficialEmail = "siddu.patil@nirvaa.com", Phone = "7843092957", Organization = "NIRVAA" },
            new() { FullName = "Mr. Vivekanand P Navadagi", Designation = "Cloud Architect", Qualification = "Ph.D(Pursuing), M.Tech", ExperienceYears = "15+", Domain = "Cloud Computing and Data Engineering", Role = "Teaching", OfficialEmail = "vivekanand.navadagi@nirvaa.com", PersonalEmail = "vnavadagi1989@gmail.com", Phone = "8073814481", Organization = "NIRVAA" },
            new() { FullName = "Mr. Subodh B Patil", Designation = "Project Manager", Qualification = "Ph.D(Pursuing), M.Tech", ExperienceYears = "15+", Domain = "Cloud Computing and Data Engineering", Role = "Teaching", OfficialEmail = "subodh.patil@nirvaa.com", PersonalEmail = "sbp771@gmail.com", Phone = "7722074289", Organization = "NIRVAA" },
            new() { FullName = "Mr. Rahul P Suryavanshi", Designation = "Technical Manager", Qualification = "M.Tech", ExperienceYears = "15+", Domain = "Data Analytics and Data Engineering", Role = "Teaching", OfficialEmail = "rahul.suryavanshi@nirvaa.com", PersonalEmail = "rahulps2202@gmail.com", Phone = "9881267919", Organization = "NIRVAA" },
            new() { FullName = "Mr. Vaman B Chavan", Designation = "Senior Technical Lead", Qualification = "M.Tech", ExperienceYears = "14+", Domain = "Data Analytics and Data Engineering", Role = "Teaching", OfficialEmail = "vaman.chavan@nirvaa.com", PersonalEmail = "vamanchavan15@gmail.com", Phone = "9960111647", Organization = "NIRVAA" },
            new() { FullName = "Mr. Abhishek Joshi", Designation = "IBMS Manager", Qualification = "B.E", ExperienceYears = "13+", Domain = "Data Centre Technology", Role = "Teaching", OfficialEmail = "abhishek.joshi@nirvaa.com", Phone = "9833989377", Organization = "NIRVAA" },
            new() { FullName = "Mr. Pranav Biradar", Designation = "Trainee Engineer", Qualification = "B.E", ExperienceYears = "1+", Domain = "Cloud, Full Stack", Role = "Lab Assistant", OfficialEmail = "pranav.biradar@nirvaa.com", PersonalEmail = "pranavvvv555@gmail.com", Phone = "8180003121", Organization = "NIRVAA" },
            new() { FullName = "Mr. Yatharth Verma", Designation = "Yoga Teacher", Qualification = "B.Tech", ExperienceYears = "20+", Domain = "Yoga Teacher", Role = "Teaching", OfficialEmail = "yatharth.verma@nirvaa.com", PersonalEmail = "yatharth.v1506@gmail.com", Phone = "8237531174", Organization = "NIRVAA" },
            new() { FullName = "Mr. Shashidhar Ramesh", Designation = "Yoga Teacher", Qualification = "B.E, PGDBA", ExperienceYears = "23+", Domain = "Yoga Teacher", Role = "Teaching", OfficialEmail = "shashidhar.ramesh@nirvaa.com", PersonalEmail = "volunteershashi@gmail.com", Phone = "7798414000", Organization = "NIRVAA" },
        };

        // --- Authentic MIT-WPU Internal Faculty ---
        faculties.AddRange(new[]
        {
            new Faculty { FullName = "Dr. Ganesh Birajdar", Designation = "Professor", Qualification = "Ph.D", Domain = "Mathematics", Role = "Teaching", Organization = "MIT-WPU" },
            new Faculty { FullName = "Dr. M. D. Hambarde", Designation = "PG Program Coordinator", Qualification = "Ph.D", Domain = "Computer Science", Role = "Teaching", Organization = "MIT-WPU" },
            new Faculty { FullName = "Dr. Vitthal Gutte", Designation = "Professor", Qualification = "Ph.D", Domain = "Computer Science", Role = "Teaching", Organization = "MIT-WPU" },
        });

        db.Faculties.AddRange(faculties);
        await db.SaveChangesAsync();

        // --- Authentic Curriculum Courses (Sem-I) ---
        var courses = new List<Course>
        {
            new() { CourseCode = "MEC50010", CourseName = "Advanced Mathematics", CourseType = "PM", Credits = 4, LectureHours = 3, TutorialHours = 1, PracticalHours = 0, ProjectHours = 0, AssessmentScheme = "TT1", SemesterId = sem1.Id },
            new() { CourseCode = "MEC51050", CourseName = "Research Methodology for Engineers", CourseType = "PM", Credits = 4, LectureHours = 3, TutorialHours = 1, PracticalHours = 0, ProjectHours = 0, AssessmentScheme = "TT1", SemesterId = sem1.Id },
            new() { CourseCode = "CDS40010", CourseName = "Advance Data Centre and Cloud Infrastructure Engineering", CourseType = "PM", Credits = 4, LectureHours = 3, TutorialHours = 0, PracticalHours = 2, ProjectHours = 0, AssessmentScheme = "TL3", SemesterId = sem1.Id },
            new() { CourseCode = "CDS40020", CourseName = "Advanced Cloud Computing & Data Storage System", CourseType = "PM", Credits = 4, LectureHours = 3, TutorialHours = 0, PracticalHours = 2, ProjectHours = 0, AssessmentScheme = "TL3", SemesterId = sem1.Id },
            new() { CourseCode = "CDS40030", CourseName = "Project Lab-I", CourseType = "PR", Credits = 2, LectureHours = 0, TutorialHours = 0, PracticalHours = 0, ProjectHours = 4, AssessmentScheme = "PJ", SemesterId = sem1.Id },
            new() { CourseCode = "PCE10040", CourseName = "Scientific Studies of Mind, Matter, Spirit and Consciousness", CourseType = "UC", Credits = 2, LectureHours = 2, TutorialHours = 0, PracticalHours = 0, ProjectHours = 0, AssessmentScheme = "UP", SemesterId = sem1.Id },
            new() { CourseCode = "YOG10030", CourseName = "Yoga", CourseType = "UC", Credits = 1, LectureHours = 0, TutorialHours = 0, PracticalHours = 2, ProjectHours = 0, AssessmentScheme = "-", SemesterId = sem1.Id },
        };
        db.Courses.AddRange(courses);
        await db.SaveChangesAsync();

        // --- Authentic Student Cohort Roll List (26 Induction Students) ---
        await EnsureInductionStudentsAsync(db);

        // Classes and payments intentionally start clean; faculty start with the standard fee.
        await EnsureDefaultClassRatesAsync(db);
        await EnsureAssessmentsAsync(db);
        await EnsureMockInterviewsAsync(db);
        await EnsureAugust2026SessionsAsync(db);
    }

    private static async Task EnsureAssessmentsAsync(AppDbContext db)
    {
        // 1. Ensure SQLite tables exist
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""Assessments"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Assessments"" PRIMARY KEY AUTOINCREMENT,
                ""CourseId"" INTEGER NOT NULL,
                ""Title"" TEXT NOT NULL,
                ""Type"" TEXT NOT NULL,
                ""MaxMarks"" TEXT NOT NULL,
                ""WeightagePercent"" TEXT NOT NULL,
                ""AssessmentDate"" TEXT NOT NULL,
                ""SemesterId"" INTEGER NOT NULL,
                ""Description"" TEXT NULL,
                ""Status"" TEXT NOT NULL,
                CONSTRAINT ""FK_Assessments_Courses_CourseId"" FOREIGN KEY (""CourseId"") REFERENCES ""Courses"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_Assessments_Semesters_SemesterId"" FOREIGN KEY (""SemesterId"") REFERENCES ""Semesters"" (""Id"") ON DELETE RESTRICT
            );

            CREATE TABLE IF NOT EXISTS ""StudentAssessmentMarks"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_StudentAssessmentMarks"" PRIMARY KEY AUTOINCREMENT,
                ""AssessmentId"" INTEGER NOT NULL,
                ""StudentId"" INTEGER NOT NULL,
                ""MarksObtained"" TEXT NULL,
                ""IsAbsent"" INTEGER NOT NULL,
                ""Remarks"" TEXT NULL,
                ""GradedAt"" TEXT NULL,
                CONSTRAINT ""FK_StudentAssessmentMarks_Assessments_AssessmentId"" FOREIGN KEY (""AssessmentId"") REFERENCES ""Assessments"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_StudentAssessmentMarks_Students_StudentId"" FOREIGN KEY (""StudentId"") REFERENCES ""Students"" (""Id"") ON DELETE CASCADE
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_StudentAssessmentMarks_AssessmentId_StudentId"" ON ""StudentAssessmentMarks"" (""AssessmentId"", ""StudentId"");
        ");

        // 2. Check if assessments are already seeded
        if (await db.Assessments.AnyAsync()) return;

        var sem = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive) ?? await db.Semesters.FirstOrDefaultAsync();
        if (sem == null) return;

        var courses = await db.Courses.Where(c => c.SemesterId == sem.Id).ToListAsync();
        if (!courses.Any()) return;

        var students = await db.Students.Where(s => s.SemesterId == sem.Id).OrderBy(s => s.StudentCode).ToListAsync();
        if (!students.Any()) return;

        var c1 = courses.FirstOrDefault(c => c.CourseCode.Contains("5001")) ?? courses[0];
        var c2 = courses.Count > 1 ? (courses.FirstOrDefault(c => c.CourseCode.Contains("5002")) ?? courses[1]) : c1;
        var c3 = courses.Count > 2 ? (courses.FirstOrDefault(c => c.CourseCode.Contains("5003")) ?? courses[2]) : c1;

        var a1 = new Assessment
        {
            CourseId = c1.Id,
            SemesterId = sem.Id,
            Title = "Unit Test 1: Data Centre Operations & Reliability",
            Type = "Unit Test",
            MaxMarks = 25,
            WeightagePercent = 20,
            AssessmentDate = DateTime.Today.AddDays(-14),
            Status = "Evaluated",
            Description = "Covers Uptime tiers, power redundancy, and cooling architectures."
        };

        var a2 = new Assessment
        {
            CourseId = c1.Id,
            SemesterId = sem.Id,
            Title = "Lab Practical 1: DCIM Monitoring & Power Telemetry",
            Type = "Lab Practical",
            MaxMarks = 25,
            WeightagePercent = 20,
            AssessmentDate = DateTime.Today.AddDays(-7),
            Status = "Evaluated",
            Description = "Hands-on configuration of DCIM sensors, PDU telemetry, and alarm thresholds."
        };

        var a3 = new Assessment
        {
            CourseId = c2.Id,
            SemesterId = sem.Id,
            Title = "Assignment 1: Cloud & Virtualization Architecture",
            Type = "Assignment",
            MaxMarks = 20,
            WeightagePercent = 15,
            AssessmentDate = DateTime.Today.AddDays(-3),
            Status = "Evaluated",
            Description = "Comparative architectural review of VMware vs OpenStack virtualization layers."
        };

        var a4 = new Assessment
        {
            CourseId = c3.Id,
            SemesterId = sem.Id,
            Title = "Unit Test 1: Spine-Leaf Network Topologies",
            Type = "Unit Test",
            MaxMarks = 25,
            WeightagePercent = 20,
            AssessmentDate = DateTime.Today.AddDays(5),
            Status = "Scheduled",
            Description = "BGP EVPN, Spine-Leaf switching, and top-of-rack cabling design."
        };

        db.Assessments.AddRange(a1, a2, a3, a4);
        await db.SaveChangesAsync();

        // Seed realistic marks for evaluated assessments (a1, a2, a3)
        int[] scoresA1 = { 23, 21, 24, 19, 22, 25, 18, 20, 22, 24, 21, 17, 23, 22, 20, 25, 19, 22 };
        int[] scoresA2 = { 22, 24, 25, 20, 23, 24, 19, 21, 23, 25, 20, 18, 22, 24, 21, 24, 20, 23 };
        int[] scoresA3 = { 18, 19, 20, 16, 17, 20, 15, 17, 18, 19, 17, 15, 18, 19, 16, 20, 16, 18 };

        for (int i = 0; i < students.Count; i++)
        {
            var student = students[i];
            db.StudentAssessmentMarks.Add(new StudentAssessmentMark
            {
                AssessmentId = a1.Id,
                StudentId = student.Id,
                MarksObtained = scoresA1[i % scoresA1.Length],
                IsAbsent = false,
                Remarks = scoresA1[i % scoresA1.Length] >= 24 ? "Outstanding understanding" : "Good conceptual clarity"
            });

            db.StudentAssessmentMarks.Add(new StudentAssessmentMark
            {
                AssessmentId = a2.Id,
                StudentId = student.Id,
                MarksObtained = scoresA2[i % scoresA2.Length],
                IsAbsent = false,
                Remarks = "Lab setup verified successfully"
            });

            db.StudentAssessmentMarks.Add(new StudentAssessmentMark
            {
                AssessmentId = a3.Id,
                StudentId = student.Id,
                MarksObtained = scoresA3[i % scoresA3.Length],
                IsAbsent = false,
                Remarks = "Submitted on time"
            });

            // a4 is scheduled (not evaluated yet)
            db.StudentAssessmentMarks.Add(new StudentAssessmentMark
            {
                AssessmentId = a4.Id,
                StudentId = student.Id,
                MarksObtained = null,
                IsAbsent = false
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureDefaultClassRatesAsync(AppDbContext db)
    {
        var today = DateTime.Today;
        var facultyWithoutCurrentRate = await db.Faculties
            .Where(faculty => !db.FacultyRates.Any(rate => rate.FacultyId == faculty.Id
                && rate.EffectiveFrom <= today
                && (rate.EffectiveTo == null || rate.EffectiveTo > today)))
            .Select(faculty => faculty.Id)
            .ToListAsync();

        if (facultyWithoutCurrentRate.Count == 0) return;
        db.FacultyRates.AddRange(facultyWithoutCurrentRate.Select(facultyId => new FacultyRate
        {
            FacultyId = facultyId,
            HourlyRateINR = FacultyService.DefaultClassRateINR,
            LectureRateINR = FacultyService.DefaultClassRateINR,
            PracticalRateINR = FacultyService.DefaultClassRateINR,
            EffectiveFrom = today,
            Notes = "Standard class fee"
        }));

        await db.SaveChangesAsync();
    }

    private static async Task EnsureMockInterviewsAsync(AppDbContext db)
    {
        // Safely migrate table if schema changed
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT ConfidenceScore FROM MockInterviewEvaluations LIMIT 1;");
        }
        catch
        {
            await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS MockInterviewEvaluations; DROP TABLE IF EXISTS MockInterviewDrives;");
        }

        // Safely ensure CourseId and SubjectName exist on MockInterviewDrives and MockInterviewEvaluations
        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT CourseId, SubjectName FROM MockInterviewDrives LIMIT 1;");
        }
        catch
        {
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE MockInterviewDrives ADD COLUMN CourseId INTEGER NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE MockInterviewDrives ADD COLUMN SubjectName TEXT NULL;"); } catch {}
        }

        try
        {
            await db.Database.ExecuteSqlRawAsync("SELECT CourseId, SubjectName FROM MockInterviewEvaluations LIMIT 1;");
        }
        catch
        {
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE MockInterviewEvaluations ADD COLUMN CourseId INTEGER NULL;"); } catch {}
            try { await db.Database.ExecuteSqlRawAsync("ALTER TABLE MockInterviewEvaluations ADD COLUMN SubjectName TEXT NULL;"); } catch {}
        }

        // 1. Ensure SQLite tables exist with exact 10-point scale factors and subject/course linkage
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS ""MockInterviewDrives"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MockInterviewDrives"" PRIMARY KEY AUTOINCREMENT,
                ""SemesterId"" INTEGER NOT NULL,
                ""Title"" TEXT NOT NULL,
                ""DriveDate"" TEXT NOT NULL,
                ""Status"" TEXT NOT NULL,
                ""PanelNotes"" TEXT NULL,
                ""CourseId"" INTEGER NULL,
                ""SubjectName"" TEXT NULL,
                CONSTRAINT ""FK_MockInterviewDrives_Semesters_SemesterId"" FOREIGN KEY (""SemesterId"") REFERENCES ""Semesters"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_MockInterviewDrives_Courses_CourseId"" FOREIGN KEY (""CourseId"") REFERENCES ""Courses"" (""Id"") ON DELETE SET NULL
            );

            CREATE TABLE IF NOT EXISTS ""MockInterviewEvaluations"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_MockInterviewEvaluations"" PRIMARY KEY AUTOINCREMENT,
                ""DriveId"" INTEGER NOT NULL,
                ""StudentId"" INTEGER NOT NULL,
                ""InterviewerFacultyId"" INTEGER NULL,
                ""CourseId"" INTEGER NULL,
                ""SubjectName"" TEXT NULL,
                ""StudentGroup"" TEXT NOT NULL,
                ""ConfidenceScore"" TEXT NULL,
                ""CommunicationScore"" TEXT NULL,
                ""TechnicalScore"" TEXT NULL,
                ""MarksOutOf10"" TEXT NULL,
                ""IsAbsent"" INTEGER NOT NULL,
                ""Feedback"" TEXT NULL,
                ""Status"" TEXT NOT NULL,
                ""InterviewedAt"" TEXT NULL,
                CONSTRAINT ""FK_MockInterviewEvaluations_MockInterviewDrives_DriveId"" FOREIGN KEY (""DriveId"") REFERENCES ""MockInterviewDrives"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_MockInterviewEvaluations_Students_StudentId"" FOREIGN KEY (""StudentId"") REFERENCES ""Students"" (""Id"") ON DELETE CASCADE,
                CONSTRAINT ""FK_MockInterviewEvaluations_Faculties_InterviewerFacultyId"" FOREIGN KEY (""InterviewerFacultyId"") REFERENCES ""Faculties"" (""Id"") ON DELETE SET NULL,
                CONSTRAINT ""FK_MockInterviewEvaluations_Courses_CourseId"" FOREIGN KEY (""CourseId"") REFERENCES ""Courses"" (""Id"") ON DELETE SET NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS ""IX_MockInterviewEvaluations_DriveId_StudentId"" ON ""MockInterviewEvaluations"" (""DriveId"", ""StudentId"");
        ");

        var sem = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive) ?? await db.Semesters.FirstOrDefaultAsync();
        if (sem == null) return;

        // Ensure authentic students from mock feedback sheet exist in db
        var mockSheetData = new (string group, string name, decimal? conf, decimal? comm, decimal? tech, decimal? marks, bool absent, string feedback)[]
        {
            ("Group A", "Omprakash Todgire", 9m, 8m, 8m, 8m, false, "Theoretical knowledge is good. Out of 4 SQL queries, 2 are correct, while the other 2 are partially correct and partially incorrect. Needs to strengthen complex joins."),
            ("Group A", "Ajay Deshmukh", 6m, 5m, 5m, 5.5m, false, "Needs to work on communication skills. Out of 4 SQL queries, 2 were correct and 2 were partially correct. Needs to strengthen theoretical fundamentals."),
            ("Group A", "Vivek", 3m, 3m, 2m, 3m, false, "Not prepared for the mock interview and needs to take it more seriously. Unable to answer basic theoretical questions and write proper syntax."),
            ("Group A", "Abhay Deshmukh", 6m, 5m, 4m, 4m, false, "Not prepared for the mock interview. It seemed like someone was helping with the answers during the mock. 2 queries were correct, but lacks clarity."),
            ("Group A", "Ayesha", 10m, 10m, 10m, 10m, false, "Overall mock interview was outstanding. Great explanation and good communication. All 5 SQL queries were answered correctly."),
            ("Group A", "Varsha", 9m, 9m, 9m, 9m, false, "Theoretical concepts are clear, and the explanations are good. Good at writing SQL queries. Out of 7–8 queries, most were answered correctly."),

            ("Group B", "Akash", 9m, 8m, 8m, 8.5m, false, "must work on stammering , and 2 queries were answered correctly and 1 query was partially correct"),
            ("Group B", "Dhanashri Patil", null, null, null, null, true, "absent"),
            ("Group B", "Gayatri Patil", 8m, 7m, 6m, 6m, false, "Needs to work on practical questions , and also on voice modulation and stammering, 3 queries were wrong and 2 were partially correct"),
            ("Group B", "Sanjana Gidwani", 10m, 10m, 10m, 10m, false, "Excellent mock , and theory questions were almost all correctly answered , if she practice more difficult theory questions she will be on top."),
            ("Group B", "Smitali", 8m, 8m, 8.5m, 8.5m, false, "Theoretical concepts are clear, and the explanations are good. Out of 5–6 SQL queries, the answers were given correctly, but some were partially correct.")
        };

        // Match or create students
        var existingStudents = await db.Students.Where(s => s.SemesterId == sem.Id).ToListAsync();
        int nextRollNum = 1272262301;

        foreach (var item in mockSheetData)
        {
            if (!existingStudents.Any(s => s.FullName.Equals(item.name, StringComparison.OrdinalIgnoreCase)))
            {
                var newStudent = new Student
                {
                    FullName = item.name,
                    StudentCode = (nextRollNum++).ToString(),
                    SemesterId = sem.Id,
                    EnrollmentStatus = "Active"
                };
                db.Students.Add(newStudent);
                existingStudents.Add(newStudent);
            }
        }
        await db.SaveChangesAsync();

        // 2. Check if drives already seeded
        var defaultCourse1 = await db.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CDS40010") ?? await db.Courses.FirstOrDefaultAsync();
        var defaultCourse2 = await db.Courses.FirstOrDefaultAsync(c => c.CourseCode == "CDS40020") ?? defaultCourse1;

        if (await db.MockInterviewDrives.AnyAsync())
        {
            var unassignedDrives = await db.MockInterviewDrives.Where(d => d.CourseId == null).ToListAsync();
            if (unassignedDrives.Any())
            {
                foreach (var d in unassignedDrives)
                {
                    var c = (d.Title.Contains("12-09") || d.Title.Contains("SQL") || d.PanelNotes != null && d.PanelNotes.Contains("SQL"))
                        ? defaultCourse1
                        : defaultCourse2;
                    d.CourseId = c?.Id;
                    d.SubjectName = c != null ? $"{c.CourseCode} — {c.CourseName}" : "Advance Data Centre and Cloud Infrastructure Engineering";
                }
                await db.SaveChangesAsync();
            }
            return;
        }

        var interviewer = await db.Faculties.FirstOrDefaultAsync(f => f.FullName.Contains("Siddu") || f.Organization == "NIRVAA")
                       ?? await db.Faculties.FirstOrDefaultAsync();

        var drive1 = new MockInterviewDrive
        {
            SemesterId = sem.Id,
            Title = "Mock Feedback — 12-09-2026",
            DriveDate = new DateTime(2026, 9, 12),
            Status = "Completed",
            CourseId = defaultCourse1?.Id,
            SubjectName = defaultCourse1 != null ? $"{defaultCourse1.CourseCode} — {defaultCourse1.CourseName}" : "Advance Data Centre and Cloud Infrastructure Engineering",
            PanelNotes = "Month-end corporate interview drive evaluating Confidence, Communication, and Technical SQL & DC knowledge."
        };

        var drive2 = new MockInterviewDrive
        {
            SemesterId = sem.Id,
            Title = "Mock Feedback — 29-08-2026",
            DriveDate = new DateTime(2026, 8, 29),
            Status = "Completed",
            CourseId = defaultCourse2?.Id,
            SubjectName = defaultCourse2 != null ? $"{defaultCourse2.CourseCode} — {defaultCourse2.CourseName}" : "Advanced Cloud Computing & Data Storage System",
            PanelNotes = "Foundational mock interview drive assessing initial candidate preparedness."
        };

        db.MockInterviewDrives.AddRange(drive1, drive2);
        await db.SaveChangesAsync();

        // Seed drive 1 with exact sheet rows
        foreach (var item in mockSheetData)
        {
            var student = existingStudents.First(s => s.FullName.Equals(item.name, StringComparison.OrdinalIgnoreCase));
            db.MockInterviewEvaluations.Add(new MockInterviewEvaluation
            {
                DriveId = drive1.Id,
                StudentId = student.Id,
                InterviewerFacultyId = interviewer?.Id,
                StudentGroup = item.group,
                ConfidenceScore = item.conf,
                CommunicationScore = item.comm,
                TechnicalScore = item.tech,
                MarksOutOf10 = item.marks,
                IsAbsent = item.absent,
                Feedback = item.feedback,
                Status = item.absent ? "Absent" : "Completed",
                InterviewedAt = new DateTime(2026, 9, 12, 10, 30, 0)
            });
        }

        // Also seed drive 2 baseline
        foreach (var item in mockSheetData)
        {
            var student = existingStudents.First(s => s.FullName.Equals(item.name, StringComparison.OrdinalIgnoreCase));
            var baselineMarks = item.marks.HasValue ? Math.Max(item.marks.Value - 1m, 3m) : (decimal?)null;
            db.MockInterviewEvaluations.Add(new MockInterviewEvaluation
            {
                DriveId = drive2.Id,
                StudentId = student.Id,
                InterviewerFacultyId = interviewer?.Id,
                StudentGroup = item.group,
                ConfidenceScore = item.conf.HasValue ? Math.Max(item.conf.Value - 1m, 3m) : null,
                CommunicationScore = item.comm.HasValue ? Math.Max(item.comm.Value - 1m, 3m) : null,
                TechnicalScore = item.tech.HasValue ? Math.Max(item.tech.Value - 1m, 3m) : null,
                MarksOutOf10 = baselineMarks,
                IsAbsent = item.absent,
                Feedback = item.absent ? "absent" : "Initial round review. Good potential, needs more mock practice.",
                Status = item.absent ? "Absent" : "Completed",
                InterviewedAt = new DateTime(2026, 8, 29, 11, 0, 0)
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task EnsureAugust2026SessionsAsync(AppDbContext db)
    {
        var augStart = new DateTime(2026, 8, 1);
        var augEnd = new DateTime(2026, 8, 31);

        var existingAugSessions = await db.Sessions
            .Where(s => s.Date >= augStart && s.Date <= augEnd)
            .ToListAsync();

        if (existingAugSessions.Count == 16 && existingAugSessions.Sum(s => s.DurationHours) == 38.0m)
        {
            return;
        }

        if (existingAugSessions.Any())
        {
            db.Sessions.RemoveRange(existingAugSessions);
            await db.SaveChangesAsync();
        }

        var courses = await db.Courses.ToListAsync();
        var faculties = await db.Faculties.ToListAsync();

        var navadagi = faculties.FirstOrDefault(f => f.FullName.Contains("Navadagi"));
        var subodh = faculties.FirstOrDefault(f => f.FullName.Contains("Subodh"));
        var shashidhar = faculties.FirstOrDefault(f => f.FullName.Contains("Shashidhar"));
        var gutte = faculties.FirstOrDefault(f => f.FullName.Contains("Gutte"));
        var hambarde = faculties.FirstOrDefault(f => f.FullName.Contains("Hambarde"));
        var birajdar = faculties.FirstOrDefault(f => f.FullName.Contains("Birajdar"));

        var cds40020 = courses.FirstOrDefault(c => c.CourseCode == "CDS40020");
        var cds40030 = courses.FirstOrDefault(c => c.CourseCode == "CDS40030");
        var pce10040 = courses.FirstOrDefault(c => c.CourseCode == "PCE10040");
        var mec51050 = courses.FirstOrDefault(c => c.CourseCode == "MEC51050");
        var mec50010 = courses.FirstOrDefault(c => c.CourseCode == "MEC50010");

        var sessionsToSeed = new List<Session>();

        // 1. Mr. Vivekanand P. Navadagi - 10h (6h Theory + 4h Lab) in CDS40020
        if (navadagi != null && cds40020 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 23),
                FacultyId = navadagi.Id,
                CourseId = cds40020.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(12, 0, 0),
                DurationHours = 3.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 23),
                FacultyId = navadagi.Id,
                CourseId = cds40020.Id,
                ActualStartTime = new TimeSpan(13, 30, 0),
                ActualEndTime = new TimeSpan(15, 30, 0),
                DurationHours = 2.0m,
                SessionType = "Practical",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 30),
                FacultyId = navadagi.Id,
                CourseId = cds40020.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(12, 0, 0),
                DurationHours = 3.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 30),
                FacultyId = navadagi.Id,
                CourseId = cds40020.Id,
                ActualStartTime = new TimeSpan(13, 30, 0),
                ActualEndTime = new TimeSpan(15, 30, 0),
                DurationHours = 2.0m,
                SessionType = "Practical",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        // 2. Mr. Subodh B. Patil - 8h (Saturday Practical Lab) in CDS40030
        if (subodh != null && cds40030 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 22),
                FacultyId = subodh.Id,
                CourseId = cds40030.Id,
                ActualStartTime = new TimeSpan(13, 30, 0),
                ActualEndTime = new TimeSpan(17, 30, 0),
                DurationHours = 4.0m,
                SessionType = "Practical",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 29),
                FacultyId = subodh.Id,
                CourseId = cds40030.Id,
                ActualStartTime = new TimeSpan(13, 30, 0),
                ActualEndTime = new TimeSpan(17, 30, 0),
                DurationHours = 4.0m,
                SessionType = "Practical",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        // 3. Mr. Shashidhar Ramesh - 8h (Saturday & Sunday Lectures) in PCE10040
        if (shashidhar != null && pce10040 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 22),
                FacultyId = shashidhar.Id,
                CourseId = pce10040.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(11, 0, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 23),
                FacultyId = shashidhar.Id,
                CourseId = pce10040.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(11, 0, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 29),
                FacultyId = shashidhar.Id,
                CourseId = pce10040.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(11, 0, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 30),
                FacultyId = shashidhar.Id,
                CourseId = pce10040.Id,
                ActualStartTime = new TimeSpan(9, 0, 0),
                ActualEndTime = new TimeSpan(11, 0, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        // 4. Dr. Vitthal Gutte - 5h (Friday & Weekend Sessions) in MEC51050
        if (gutte != null && mec51050 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 21),
                FacultyId = gutte.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(10, 45, 0),
                ActualEndTime = new TimeSpan(11, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Tutorial",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 22),
                FacultyId = gutte.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(10, 45, 0),
                ActualEndTime = new TimeSpan(11, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 23),
                FacultyId = gutte.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(10, 45, 0),
                ActualEndTime = new TimeSpan(11, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 28),
                FacultyId = gutte.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(10, 45, 0),
                ActualEndTime = new TimeSpan(11, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Tutorial",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 29),
                FacultyId = gutte.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(10, 45, 0),
                ActualEndTime = new TimeSpan(11, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        // 5. Dr. M. D. Hambarde - 2h (Saturday Lectures) in MEC51050
        if (hambarde != null && mec51050 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 22),
                FacultyId = hambarde.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(11, 45, 0),
                ActualEndTime = new TimeSpan(12, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 29),
                FacultyId = hambarde.Id,
                CourseId = mec51050.Id,
                ActualStartTime = new TimeSpan(11, 45, 0),
                ActualEndTime = new TimeSpan(12, 45, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        // 6. Dr. Ganesh Birajdar - 5h (Friday & Weekend Lectures / Tut) in MEC50010
        if (birajdar != null && mec50010 != null)
        {
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 21),
                FacultyId = birajdar.Id,
                CourseId = mec50010.Id,
                ActualStartTime = new TimeSpan(8, 30, 0),
                ActualEndTime = new TimeSpan(10, 30, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 22),
                FacultyId = birajdar.Id,
                CourseId = mec50010.Id,
                ActualStartTime = new TimeSpan(11, 0, 0),
                ActualEndTime = new TimeSpan(12, 0, 0),
                DurationHours = 1.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
            sessionsToSeed.Add(new Session
            {
                Date = new DateTime(2026, 8, 28),
                FacultyId = birajdar.Id,
                CourseId = mec50010.Id,
                ActualStartTime = new TimeSpan(8, 30, 0),
                ActualEndTime = new TimeSpan(10, 30, 0),
                DurationHours = 2.0m,
                SessionType = "Lecture",
                Status = SessionStatus.Conducted,
                IsApproved = true,
                ApprovedBy = "Academic Operations & DCMS",
                LoggedBy = "Administrator"
            });
        }

        if (sessionsToSeed.Count > 0)
        {
            db.Sessions.AddRange(sessionsToSeed);
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureInductionStudentsAsync(AppDbContext db)
    {
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Students\" ADD COLUMN \"Email\" TEXT NULL;");
        }
        catch { /* Column already exists */ }
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Students\" ADD COLUMN \"MobileNumber\" TEXT NULL;");
        }
        catch { /* Column already exists */ }
        try
        {
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE \"Students\" ADD COLUMN \"Gender\" TEXT NULL;");
        }
        catch { /* Column already exists */ }

        var sem = await db.Semesters.FirstOrDefaultAsync(s => s.IsActive) ?? await db.Semesters.FirstOrDefaultAsync();
        if (sem == null) return;

        var inductionList = new (string code, string name, string mobile, string email, string gender)[]
        {
            ("1272262167", "Bhagwat Yogendra Dada", "9322508069", "yogendrabhagwat009@gmail.com", "M"),
            ("1272262312", "Bhagwat Kshitija Santosh", "8010403002", "kshitija8675@gmail.com", "F"),
            ("1272262069", "Chaudhari Kashish Kailash", "8155990879", "kashishchaudhari805@gmail.com", "F"),
            ("1272261959", "Deore Asmita Shashikant", "9922494964", "asmitadeore25@gmail.com", "F"),
            ("1272262093", "Dhawalekar Vaishnavi Sachchidanand", "7058911357", "vaishnavidhawalekar@gmail.com", "F"),
            ("1272262313", "Harpale Prathamesh Vikas", "8454063599", "prathameshharpale21@gmail.com", "M"),
            ("1272262250", "Khomane Aditya Navnath", "8623055396", "adityakhomane810@gmail.com", "M"),
            ("1272262214", "Kolhe Sanket Suresh", "8623847277", "sanketkolhe5621@gmail.com", "M"),
            ("1272262140", "Lature Tejas Rajshekhar", "9960267097", "tejaslature996@gmail.com", "M"),
            ("1272262017", "Mankar Mayuresh Dnyaneshwar", "8483846230", "mayureshmankar414@gmail.com", "M"),
            ("1272262067", "Manore Aditya Jayprakash", "8010896329", "manoreaditya123@gmail.com", "M"),
            ("1272262082", "Nikam Divya Dipak", "9307466637", "divya20.nikam@gmail.com", "F"),
            ("1272262189", "Patil Akshay Vinod", "8767166547", "avp1042005@gmail.com", "M"),
            ("1272262241", "Patrike Harshada Siddheshwar", "7972315647", "harshadapatrike2002@gmail.com", "F"),
            ("1272262022", "Pisal Piyush Satish", "8530743755", "piyushpisal2310@gmail.com", "M"),
            ("1272261957", "Rajebhosale Poonam Shivaji", "7841813537", "poonamrb0811@gmail.com", "F"),
            ("1272261958", "S Monish Kumar", "9952032404", "monishkumar1095@gmail.com", "M"),
            ("1272262079", "Tambe Abhinay Balasaheb", "7219326275", "abtambe2003@gmail.com", "M"),
            ("1272262220", "Varsale Saurabh", "9022216455", "saurabhvarsale30@gmail.com", "M"),
            ("1272262314", "Wankhede Unmesh", "9923141189", "theunmesh27@gmail.com", "M"),
            ("1272261987", "Wankhede Shrey", "6260063313", "shreywankhede@gmail.com", "M"),
            ("1272262315", "Gale Gauri Pravin", "7020144604", "gaurigale6@gmail.com", "F"),
            ("1272262316", "Nakade Kartik Vilas", "9022250768", "kartiknakade007@gmail.com", "M"),
            ("1272262317", "Patel Shreya Bhadreshkumar", "7990365829", "shreya.patel0806@gmail.com", "F"),
            ("1272262318", "Sonone Ayush Sanjay", "9860141619", "ayushsonone72@gmail.com", "M"),
            ("1272262319", "Sailee Thakkar", "8686791777", "saileethakkar@gmail.com", "F"),
        };

        var existing = await db.Students.ToListAsync();
        bool changed = false;

        foreach (var item in inductionList)
        {
            var match = existing.FirstOrDefault(s => s.FullName.Equals(item.name, StringComparison.OrdinalIgnoreCase) || s.StudentCode == item.code);
            if (match != null)
            {
                if (string.IsNullOrEmpty(match.Email) || string.IsNullOrEmpty(match.MobileNumber) || string.IsNullOrEmpty(match.Gender))
                {
                    match.Email = item.email;
                    match.MobileNumber = item.mobile;
                    match.Gender = item.gender;
                    match.EnrollmentStatus = "Active";
                    changed = true;
                }
            }
            else
            {
                var newStudent = new Student
                {
                    StudentCode = item.code,
                    FullName = item.name,
                    SemesterId = sem.Id,
                    EnrollmentStatus = "Active",
                    Email = item.email,
                    MobileNumber = item.mobile,
                    Gender = item.gender
                };
                db.Students.Add(newStudent);
                existing.Add(newStudent);
                changed = true;
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
