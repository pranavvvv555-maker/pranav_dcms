using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DCMSApp.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Faculties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Designation = table.Column<string>(type: "TEXT", nullable: true),
                    Qualification = table.Column<string>(type: "TEXT", nullable: true),
                    ExperienceYears = table.Column<string>(type: "TEXT", nullable: true),
                    Domain = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    OfficialEmail = table.Column<string>(type: "TEXT", nullable: true),
                    PersonalEmail = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Organization = table.Column<string>(type: "TEXT", nullable: false),
                    PhotoUrl = table.Column<string>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Faculties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentPeriods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PeriodName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Semesters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    AcademicYear = table.Column<string>(type: "TEXT", nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Semesters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacultyRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    HourlyRateINR = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacultyRates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacultyRates_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PaymentPeriodId = table.Column<int>(type: "INTEGER", nullable: false),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAdjustments_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentAdjustments_PaymentPeriods_PaymentPeriodId",
                        column: x => x.PaymentPeriodId,
                        principalTable: "PaymentPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CourseName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CourseType = table.Column<string>(type: "TEXT", nullable: true),
                    Credits = table.Column<int>(type: "INTEGER", nullable: false),
                    LectureHours = table.Column<int>(type: "INTEGER", nullable: false),
                    TutorialHours = table.Column<int>(type: "INTEGER", nullable: false),
                    PracticalHours = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectHours = table.Column<int>(type: "INTEGER", nullable: false),
                    AssessmentScheme = table.Column<string>(type: "TEXT", nullable: true),
                    SemesterId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Students",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StudentCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SemesterId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnrollmentStatus = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Students", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Students_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourseFaculties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    Role = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseFaculties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseFaculties_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CourseFaculties_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimetableSlots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SemesterId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    SlotNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: true),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: true),
                    SlotType = table.Column<string>(type: "TEXT", nullable: false),
                    RoomNo = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimetableSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimetableSlots_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimetableSlots_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TimetableSlots_Semesters_SemesterId",
                        column: x => x.SemesterId,
                        principalTable: "Semesters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimetableSlotId = table.Column<int>(type: "INTEGER", nullable: true),
                    CourseId = table.Column<int>(type: "INTEGER", nullable: false),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActualStartTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    ActualEndTime = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    DurationHours = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    SessionType = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SubstituteFacultyId = table.Column<int>(type: "INTEGER", nullable: true),
                    CancellationReason = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LoggedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedBy = table.Column<string>(type: "TEXT", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsApproved = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Faculties_SubstituteFacultyId",
                        column: x => x.SubstituteFacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sessions_TimetableSlots_TimetableSlotId",
                        column: x => x.TimetableSlotId,
                        principalTable: "TimetableSlots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PaymentLineItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PaymentPeriodId = table.Column<int>(type: "INTEGER", nullable: false),
                    FacultyId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalHours = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Deductions = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    NetPayable = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_Faculties_FacultyId",
                        column: x => x.FacultyId,
                        principalTable: "Faculties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_PaymentPeriods_PaymentPeriodId",
                        column: x => x.PaymentPeriodId,
                        principalTable: "PaymentPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentLineItems_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseFaculties_CourseId",
                table: "CourseFaculties",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseFaculties_FacultyId",
                table: "CourseFaculties",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseCode",
                table: "Courses",
                column: "CourseCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Courses_SemesterId",
                table: "Courses",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Faculties_OfficialEmail",
                table: "Faculties",
                column: "OfficialEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacultyRates_FacultyId",
                table: "FacultyRates",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAdjustments_FacultyId",
                table: "PaymentAdjustments",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAdjustments_PaymentPeriodId",
                table: "PaymentAdjustments",
                column: "PaymentPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_FacultyId",
                table: "PaymentLineItems",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_PaymentPeriodId",
                table: "PaymentLineItems",
                column: "PaymentPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLineItems_SessionId",
                table: "PaymentLineItems",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_CourseId",
                table: "Sessions",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_FacultyId",
                table: "Sessions",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SubstituteFacultyId",
                table: "Sessions",
                column: "SubstituteFacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_TimetableSlotId",
                table: "Sessions",
                column: "TimetableSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_SemesterId",
                table: "Students",
                column: "SemesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Students_StudentCode",
                table: "Students",
                column: "StudentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSlots_CourseId",
                table: "TimetableSlots",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSlots_FacultyId",
                table: "TimetableSlots",
                column: "FacultyId");

            migrationBuilder.CreateIndex(
                name: "IX_TimetableSlots_SemesterId",
                table: "TimetableSlots",
                column: "SemesterId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CourseFaculties");

            migrationBuilder.DropTable(
                name: "FacultyRates");

            migrationBuilder.DropTable(
                name: "PaymentAdjustments");

            migrationBuilder.DropTable(
                name: "PaymentLineItems");

            migrationBuilder.DropTable(
                name: "Students");

            migrationBuilder.DropTable(
                name: "PaymentPeriods");

            migrationBuilder.DropTable(
                name: "Sessions");

            migrationBuilder.DropTable(
                name: "TimetableSlots");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Faculties");

            migrationBuilder.DropTable(
                name: "Semesters");
        }
    }
}
