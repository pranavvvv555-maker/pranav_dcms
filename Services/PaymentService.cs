using ClosedXML.Excel;
using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace DCMSApp.Services;

public class PaymentService(AppDbContext db)
{
    public async Task<List<PaymentPeriod>> GetPeriodsAsync()
        => await db.PaymentPeriods.OrderByDescending(p => p.StartDate).ToListAsync();

    public async Task<PaymentPeriod> CreatePeriodAsync(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name) || endDate.Date < startDate.Date)
            throw new InvalidOperationException("A name and valid date range are required.");
        if (await db.PaymentPeriods.AnyAsync(p => p.StartDate <= endDate.Date && p.EndDate >= startDate.Date))
            throw new InvalidOperationException("Payment periods cannot overlap: a class may be billed only once.");
        var period = new PaymentPeriod
        {
            PeriodName = name,
            StartDate = startDate.Date,
            EndDate = endDate.Date,
            Status = PaymentPeriodStatus.Open
        };
        db.PaymentPeriods.Add(period);
        await db.SaveChangesAsync();
        return period;
    }

    public async Task CalculatePaymentsAsync(int periodId)
    {
        var period = await db.PaymentPeriods
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == periodId);
        if (period is null) return;

        if (period.Status is PaymentPeriodStatus.Approved or PaymentPeriodStatus.Paid || period.LineItems.Any(x => x.IsPaid))
            throw new InvalidOperationException("Approved or paid payroll is locked. Use an adjustment in an open period.");
        if (await db.PaymentPeriods.AnyAsync(p => p.Id != periodId && p.StartDate <= period.EndDate && p.EndDate >= period.StartDate))
            throw new InvalidOperationException("Resolve overlapping payroll periods before calculation.");
        // Remove only draft line items for recalculation
        db.PaymentLineItems.RemoveRange(period.LineItems);

        var payableFaculty = await db.Faculties
            .Where(f => f.IsActive)
            .OrderBy(f => f.FullName)
            .ToListAsync();

        foreach (var faculty in payableFaculty)
        {
            var conductedSessions = await db.Sessions
                .Where(s => s.FacultyId == faculty.Id
                    && s.Date >= period.StartDate
                    && s.Date <= period.EndDate
                    && s.Status == SessionStatus.Conducted
                    && s.IsApproved)
                .ToListAsync();

            var lectureCount = conductedSessions.Count(s => s.SessionType is "Lecture" or "Tutorial");
            var practicalCount = conductedSessions.Count - lectureCount;
            var totalHours = conductedSessions.Sum(s => s.DurationHours);

            var today = DateTime.Today;
            var rate = await db.FacultyRates
                .Where(r => r.FacultyId == faculty.Id
                    && r.EffectiveFrom <= today
                    && (r.EffectiveTo == null || r.EffectiveTo > today))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefaultAsync();

            var lectureRate = rate is null ? FacultyService.DefaultClassRateINR : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
            var practicalRate = rate is null ? FacultyService.DefaultClassRateINR : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;
            var historicalRates = await db.FacultyRates.Where(r => r.FacultyId == faculty.Id).ToListAsync();
            var gross = CalculatePay(conductedSessions, historicalRates);
            var effectiveLectureRate = lectureCount == 0 ? lectureRate : CalculatePay(conductedSessions.Where(s => s.SessionType is "Lecture" or "Tutorial"), historicalRates) / lectureCount;
            var effectivePracticalRate = practicalCount == 0 ? practicalRate : CalculatePay(conductedSessions.Where(s => s.SessionType is not ("Lecture" or "Tutorial")), historicalRates) / practicalCount;

            var adjustments = await db.PaymentAdjustments
                .Where(a => a.PaymentPeriodId == periodId && a.FacultyId == faculty.Id)
                .ToListAsync();

            var totalAdjustment = adjustments.Sum(a =>
                a.Type == "Deduction" ? -a.Amount : a.Amount);

            db.PaymentLineItems.Add(new PaymentLineItem
            {
                PaymentPeriodId = periodId,
                FacultyId = faculty.Id,
                LectureCount = lectureCount,
                PracticalCount = practicalCount,
                LectureRate = effectiveLectureRate,
                PracticalRate = effectivePracticalRate,
                TotalHours = totalHours,
                HourlyRate = effectiveLectureRate,
                GrossAmount = gross,
                Deductions = adjustments.Where(a => a.Type == "Deduction").Sum(a => a.Amount),
                NetPayable = gross + totalAdjustment,
            });
        }

        period.Status = PaymentPeriodStatus.Calculated;
        await db.SaveChangesAsync();
    }

    public async Task<List<PaymentLineItem>> GetLineItemsAsync(int periodId)
        => await db.PaymentLineItems
            .Include(p => p.Faculty)
            .Where(p => p.PaymentPeriodId == periodId)
            .OrderBy(p => p.Faculty.FullName)
            .ToListAsync();

    public async Task<List<MonthlyPaymentPreview>> GetMonthlyPreviewAsync(DateTime startDate, DateTime endDate)
    {
        var faculty = await db.Faculties
            .Where(item => item.IsActive)
            .Include(item => item.Rates)
            .OrderBy(item => item.FullName)
            .ToListAsync();

        var sessions = await db.Sessions
            .Where(item => item.Date >= startDate
                && item.Date <= endDate
                && item.Status == SessionStatus.Conducted
                && item.IsApproved)
            .ToListAsync();

        return faculty.Select(item =>
        {
            var classes = sessions.Where(session => session.FacultyId == item.Id).ToList();
            var lectures = classes.Count(session => session.SessionType is "Lecture" or "Tutorial");
            var practicals = classes.Count - lectures;
            var today = DateTime.Today;
            var rate = item.Rates
                .Where(rate => rate.EffectiveFrom <= today
                    && (rate.EffectiveTo is null || rate.EffectiveTo > today))
                .OrderByDescending(rate => rate.EffectiveFrom)
                .FirstOrDefault();
            var lectureRate = rate is null ? FacultyService.DefaultClassRateINR : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
            var practicalRate = rate is null ? FacultyService.DefaultClassRateINR : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;

            return new MonthlyPaymentPreview(
                item.Id,
                item.FullName,
                lectures,
                practicals,
                lectureRate,
                practicalRate,
                CalculatePay(classes, item.Rates));
        }).ToList();
    }

    public async Task<List<TimetablePaymentForecast>> GetTimetablePaymentForecastAsync(DateTime startDate, DateTime endDate)
    {
        // Forecast from the persisted timetable when available. This keeps
        // payment planning aligned with timetable edits instead of silently
        var allSlots = await db.TimetableSlots
            .Include(slot => slot.Faculty)
            .Where(slot => slot.FacultyId != null)
            .ToListAsync();
        var persisted = allSlots
            .Where(slot => slot.Faculty != null && slot.StartTime < slot.EndTime)
            .ToList();
        if (persisted.Count > 0)
        {
            var faculty = await db.Faculties.Where(f => f.IsActive).Include(f => f.Rates).ToListAsync();
            var byId = faculty.ToDictionary(f => f.Id);
            return persisted.GroupBy(slot => slot.FacultyId!.Value).Select(group =>
            {
                byId.TryGetValue(group.Key, out var person);
                var rates = person?.Rates ?? [];
                var lectureSlots = group.Count(slot => slot.SlotType is "Lecture" or "Tutorial");
                var practicalSlots = group.Count() - lectureSlots;
                var lectureRate = rates.Where(r => r.EffectiveFrom <= endDate.Date && (r.EffectiveTo == null || r.EffectiveTo > endDate.Date)).OrderByDescending(r => r.EffectiveFrom).Select(r => r.LectureRateINR > 0 ? r.LectureRateINR : r.HourlyRateINR).FirstOrDefault();
                var practicalRate = rates.Where(r => r.EffectiveFrom <= endDate.Date && (r.EffectiveTo == null || r.EffectiveTo > endDate.Date)).OrderByDescending(r => r.EffectiveFrom).Select(r => r.PracticalRateINR > 0 ? r.PracticalRateINR : r.HourlyRateINR).FirstOrDefault();
                var occurrences = group.Sum(slot => CountOccurrences(startDate, endDate, slot.DayOfWeek));
                var monthlyLectures = lectureSlots * occurrences;
                var monthlyPracticals = practicalSlots * occurrences;
                return new TimetablePaymentForecast(person?.FullName ?? $"Faculty {group.Key}", lectureSlots, practicalSlots, monthlyLectures, monthlyPracticals, lectureRate, practicalRate, monthlyLectures * lectureRate + monthlyPracticals * practicalRate, person != null);
            }).OrderByDescending(item => item.ExpectedAmount).ThenBy(item => item.FacultyName).ToList();
        }
        var activeFaculty = await db.Faculties
            .Where(item => item.IsActive)
            .Include(item => item.Rates)
            .ToListAsync();

        var facultyByName = activeFaculty.ToDictionary(item => item.FullName, StringComparer.OrdinalIgnoreCase);
        var forecasts = TimetablePaymentPlan
            .GroupBy(item => item.FacultyName)
            .Select(group =>
            {
                facultyByName.TryGetValue(group.Key, out var faculty);
                var today = DateTime.Today;
                var rate = faculty?.Rates
                    .Where(item => item.EffectiveFrom <= today
                        && (item.EffectiveTo is null || item.EffectiveTo > today))
                    .OrderByDescending(item => item.EffectiveFrom)
                    .FirstOrDefault();
                var lectureRate = rate is null ? FacultyService.DefaultClassRateINR : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
                var practicalRate = rate is null ? FacultyService.DefaultClassRateINR : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;
                var weeklyLectures = group.Where(item => item.ClassType == "Lecture").Sum(item => item.UnitsPerDay);
                var weeklyPracticals = group.Where(item => item.ClassType == "Practical").Sum(item => item.UnitsPerDay);
                var monthlyLectures = group
                    .Where(item => item.ClassType == "Lecture")
                    .Sum(item => item.UnitsPerDay * CountOccurrences(startDate, endDate, item.Day));
                var monthlyPracticals = group
                    .Where(item => item.ClassType == "Practical")
                    .Sum(item => item.UnitsPerDay * CountOccurrences(startDate, endDate, item.Day));

                return new TimetablePaymentForecast(
                    group.Key,
                    weeklyLectures,
                    weeklyPracticals,
                    monthlyLectures,
                    monthlyPracticals,
                    lectureRate,
                    practicalRate,
                    (monthlyLectures * lectureRate) + (monthlyPracticals * practicalRate),
                    rate is not null);
            })
            .OrderByDescending(item => item.ExpectedAmount)
            .ThenBy(item => item.FacultyName)
            .ToList();

        return forecasts;
    }

    public async Task<List<FacultyPaymentOverview>> GetFacultyPaymentOverviewAsync(
        DateTime weekStart,
        DateTime weekEnd,
        DateTime monthStart,
        DateTime monthEnd)
    {
        var firstDate = weekStart < monthStart ? weekStart : monthStart;
        var lastDate = weekEnd > monthEnd ? weekEnd : monthEnd;

        var faculty = await db.Faculties
            .Where(item => item.IsActive)
            .Include(item => item.Rates)
            .OrderBy(item => item.FullName)
            .ToListAsync();

        var conductedSessions = await db.Sessions
            .Where(item => item.Date >= firstDate
                && item.Date <= lastDate
                && item.Status == SessionStatus.Conducted
                && item.IsApproved)
            .Include(item => item.Course)
            .ToListAsync();

        var today = DateTime.Today;

        return faculty.Select(item =>
        {
            var facultySessions = conductedSessions
                .Where(session => session.FacultyId == item.Id)
                .ToList();
            var weeklySessions = facultySessions
                .Where(session => session.Date >= weekStart && session.Date <= weekEnd)
                .ToList();
            var monthlySessions = facultySessions
                .Where(session => session.Date >= monthStart && session.Date <= monthEnd)
                .ToList();

            var currentRate = item.Rates
                .Where(r => r.EffectiveFrom <= today && (r.EffectiveTo == null || r.EffectiveTo > today))
                .OrderByDescending(r => r.EffectiveFrom)
                .FirstOrDefault();
            var lectureRate = currentRate is null
                ? FacultyService.DefaultClassRateINR
                : currentRate.LectureRateINR > 0 ? currentRate.LectureRateINR : currentRate.HourlyRateINR;
            var practicalRate = currentRate is null
                ? FacultyService.DefaultClassRateINR
                : currentRate.PracticalRateINR > 0 ? currentRate.PracticalRateINR : currentRate.HourlyRateINR;

            var weeklyItems = weeklySessions
                .OrderBy(s => s.Date)
                .ThenBy(s => s.ActualStartTime)
                .Select(s => new FacultyConductedSessionItem(
                    s.Id,
                    s.Date,
                    s.Course?.CourseCode ?? "",
                    s.Course?.CourseName ?? "Subject",
                    s.SessionType,
                    s.ActualStartTime,
                    s.ActualEndTime,
                    s.DurationHours,
                    GetSessionSingleRate(s, item.Rates)))
                .ToList();

            var monthlyItems = monthlySessions
                .OrderBy(s => s.Date)
                .ThenBy(s => s.ActualStartTime)
                .Select(s => new FacultyConductedSessionItem(
                    s.Id,
                    s.Date,
                    s.Course?.CourseCode ?? "",
                    s.Course?.CourseName ?? "Subject",
                    s.SessionType,
                    s.ActualStartTime,
                    s.ActualEndTime,
                    s.DurationHours,
                    GetSessionSingleRate(s, item.Rates)))
                .ToList();

            return new FacultyPaymentOverview(
                item.Id,
                item.FullName,
                item.Organization,
                item.Role,
                item.Designation,
                lectureRate,
                practicalRate,
                CountLectures(weeklySessions),
                CountPracticals(weeklySessions),
                CalculatePay(weeklySessions, item.Rates),
                CountLectures(monthlySessions),
                CountPracticals(monthlySessions),
                CalculatePay(monthlySessions, item.Rates),
                weeklyItems,
                monthlyItems);
        }).ToList();
    }

    public byte[] ExportPaymentOverviewExcel(
        IReadOnlyList<FacultyPaymentOverview> data,
        DateTime weekendStart,
        DateTime weekendEnd,
        DateTime monthStart,
        DateTime monthEnd)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Payment Overview");

        // Title Block
        ws.Cell(1, 1).Value = "MIT-WPU x NIRVAA SOLUTIONS";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#102A43");

        ws.Cell(2, 1).Value = "Faculty Remuneration Ledger & Teaching Activity Register";
        ws.Cell(2, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Style.Font.FontSize = 12;
        ws.Cell(2, 1).Style.Font.FontColor = XLColor.FromHtml("#164E80");

        var periodText = (monthStart.Day == 1 && monthEnd.Day == DateTime.DaysInMonth(monthStart.Year, monthStart.Month) && monthStart.Month == monthEnd.Month)
            ? $"Reporting Month: {monthStart:MMMM yyyy}"
            : $"Reporting Period: {monthStart:dd MMM yyyy} - {monthEnd:dd MMM yyyy}";
        ws.Cell(3, 1).Value = $"Teaching Weekend: {weekendStart:dd MMM yyyy} - {weekendEnd:dd MMM yyyy}   |   {periodText}   |   Exported: {DateTime.Now:dd MMM yyyy, hh:mm tt}";
        ws.Cell(3, 1).Style.Font.FontSize = 9;
        ws.Cell(3, 1).Style.Font.FontColor = XLColor.FromHtml("#60758B");

        var headers = new[]
        {
            "#", "Professor Name", "Organization", "Role", "Designation",
            "Lecture Fee", "Practical Fee",
            "Weekend Lectures", "Weekend Practicals", "Weekend Total (INR)",
            "Month Lectures", "Month Practicals", "Month Total (INR)"
        };

        var headerRow = 5;
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(headerRow, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 10;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#102A43");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
        ws.Row(headerRow).Height = 26;

        int row = headerRow + 1;
        int index = 1;
        foreach (var item in data)
        {
            ws.Cell(row, 1).Value = index++;
            ws.Cell(row, 2).Value = item.FacultyName;
            ws.Cell(row, 3).Value = item.Organization;
            ws.Cell(row, 4).Value = item.Role;
            ws.Cell(row, 5).Value = item.Designation ?? "-";
            ws.Cell(row, 6).Value = item.LectureRate;
            ws.Cell(row, 7).Value = item.PracticalRate;
            ws.Cell(row, 8).Value = item.WeeklyLectureCount;
            ws.Cell(row, 9).Value = item.WeeklyPracticalCount;
            ws.Cell(row, 10).Value = item.WeeklyPayableAmount;
            ws.Cell(row, 11).Value = item.MonthlyLectureCount;
            ws.Cell(row, 12).Value = item.MonthlyPracticalCount;
            ws.Cell(row, 13).Value = item.MonthlyPayableAmount;

            ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 6).Style.NumberFormat.Format = "₹#,##0";
            ws.Cell(row, 7).Style.NumberFormat.Format = "₹#,##0";
            ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 10).Style.NumberFormat.Format = "₹#,##0";
            ws.Cell(row, 10).Style.Font.Bold = true;
            ws.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(row, 13).Style.NumberFormat.Format = "₹#,##0";
            ws.Cell(row, 13).Style.Font.Bold = true;

            if (row % 2 == 1)
            {
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#F8FAFC");
            }
            row++;
        }

        // Summary row
        ws.Cell(row, 2).Value = "TOTAL SUMMARY";
        ws.Cell(row, 2).Style.Font.Bold = true;
        ws.Cell(row, 8).FormulaA1 = $"SUM(H6:H{row - 1})";
        ws.Cell(row, 9).FormulaA1 = $"SUM(I6:I{row - 1})";
        ws.Cell(row, 10).FormulaA1 = $"SUM(J6:J{row - 1})";
        ws.Cell(row, 11).FormulaA1 = $"SUM(K6:K{row - 1})";
        ws.Cell(row, 12).FormulaA1 = $"SUM(L6:L{row - 1})";
        ws.Cell(row, 13).FormulaA1 = $"SUM(M6:M{row - 1})";

        ws.Range(row, 1, row, headers.Length).Style.Font.Bold = true;
        ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = XLColor.FromHtml("#E2E8F0");
        ws.Cell(row, 8).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 9).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 10).Style.NumberFormat.Format = "₹#,##0";
        ws.Cell(row, 11).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 12).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ws.Cell(row, 13).Style.NumberFormat.Format = "₹#,##0";
        ws.Row(row).Height = 24;

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public byte[] GenerateMonthlyPaymentPdf(
        IReadOnlyList<FacultyPaymentOverview> data,
        DateTime monthStart,
        DateTime monthEnd)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var totalLectures = data.Sum(x => x.MonthlyLectureCount);
        var totalPracticals = data.Sum(x => x.MonthlyPracticalCount);
        var totalAmount = data.Sum(x => x.MonthlyPayableAmount);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9.5f).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("MIT WORLD PEACE UNIVERSITY, PUNE").FontSize(15).Bold().FontColor("#102A43");
                            c.Item().Text("School of Computer Science & Technology  ·  NIRVAA Partnership").FontSize(9.5f).SemiBold().FontColor("#1E5CA8");
                            c.Item().Text("FACULTY DISBURSEMENT STATEMENT & CONDUCTION REGISTER").FontSize(10.5f).Bold().FontColor("#C28800");
                        });
                        row.ConstantItem(150).AlignRight().Column(c =>
                        {
                            var periodTitle = (monthStart.Day == 1 && monthEnd.Day == DateTime.DaysInMonth(monthStart.Year, monthStart.Month) && monthStart.Month == monthEnd.Month)
                                ? monthStart.ToString("MMMM yyyy")
                                : $"{monthStart:dd MMM} – {monthEnd:dd MMM yyyy}";
                            c.Item().Text($"Period: {periodTitle}").FontSize(10.5f).Bold().FontColor("#102A43");
                            c.Item().Text($"Dates: {monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}").FontSize(7.5f).FontColor("#60758B");
                            c.Item().Text($"Generated: {DateTime.Now:dd MMM yyyy, hh:mm tt}").FontSize(7.5f).Italic().FontColor("#60758B");
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#164E80");
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    // Executive KPI Stat Boxes
                    col.Item().PaddingBottom(10).Row(r =>
                    {
                        r.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(7).Column(c =>
                        {
                            c.Item().Text("TOTAL PROFESSORS").FontSize(7.5f).Bold().FontColor("#64748B");
                            c.Item().Text($"{data.Count} Faculty").FontSize(13).Bold().FontColor("#102A43");
                        });
                        r.ConstantItem(8);
                        r.RelativeItem().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").Padding(7).Column(c =>
                        {
                            c.Item().Text("TOTAL LECTURES").FontSize(7.5f).Bold().FontColor("#B45309");
                            c.Item().Text($"{totalLectures} Lectures").FontSize(13).Bold().FontColor("#92400E");
                        });
                        r.ConstantItem(8);
                        r.RelativeItem().Border(1).BorderColor("#BFDBFE").Background("#EFF6FF").Padding(7).Column(c =>
                        {
                            c.Item().Text("TOTAL PRACTICALS").FontSize(7.5f).Bold().FontColor("#1D4ED8");
                            c.Item().Text($"{totalPracticals} Practicals").FontSize(13).Bold().FontColor("#1E40AF");
                        });
                        r.ConstantItem(8);
                        r.RelativeItem().Border(1).BorderColor("#A7F3D0").Background("#ECFDF5").Padding(7).Column(c =>
                        {
                            c.Item().Text("TOTAL DISBURSEMENT").FontSize(7.5f).Bold().FontColor("#047857");
                            c.Item().Text($"INR {totalAmount:N0}").FontSize(13).Bold().FontColor("#065F46");
                        });
                    });

                    // Ledger Table
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(24);
                            columns.RelativeColumn(3.2f);
                            columns.RelativeColumn(2.3f);
                            columns.ConstantColumn(58);
                            columns.ConstantColumn(58);
                            columns.ConstantColumn(80);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#102A43").Padding(5).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(8.5f);
                            header.Cell().Background("#102A43").Padding(5).Text("Professor Name").FontColor(Colors.White).Bold().FontSize(8.5f);
                            header.Cell().Background("#102A43").Padding(5).Text("Organization / Role").FontColor(Colors.White).Bold().FontSize(8.5f);
                            header.Cell().Background("#102A43").Padding(5).AlignCenter().Text("Lectures").FontColor(Colors.White).Bold().FontSize(8.5f);
                            header.Cell().Background("#102A43").Padding(5).AlignCenter().Text("Practicals").FontColor(Colors.White).Bold().FontSize(8.5f);
                            header.Cell().Background("#102A43").Padding(5).AlignRight().Text("Payable (INR)").FontColor(Colors.White).Bold().FontSize(8.5f);
                        });

                        int idx = 1;
                        foreach (var row in data)
                        {
                            var bg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";
                            table.Cell().Background(bg).Padding(4.5f).AlignCenter().Text(idx.ToString()).FontSize(8f);
                            table.Cell().Background(bg).Padding(4.5f).Text(row.FacultyName).Bold().FontSize(8f);
                            table.Cell().Background(bg).Padding(4.5f).Text($"{row.Organization} · {row.Role}").FontSize(7.5f).FontColor("#475569");
                            table.Cell().Background(bg).Padding(4.5f).AlignCenter().Text(row.MonthlyLectureCount.ToString()).FontSize(8f);
                            table.Cell().Background(bg).Padding(4.5f).AlignCenter().Text(row.MonthlyPracticalCount.ToString()).FontSize(8f);
                            table.Cell().Background(bg).Padding(4.5f).AlignRight().Text($"INR {row.MonthlyPayableAmount:N0}").Bold().FontSize(8f).FontColor("#047857");
                            idx++;
                        }

                        // Total Row
                        table.Cell().ColumnSpan(3).Background("#E2E8F0").Padding(5).Text("GRAND TOTAL").Bold().FontSize(8.5f);
                        table.Cell().Background("#E2E8F0").Padding(5).AlignCenter().Text(totalLectures.ToString()).Bold().FontSize(8.5f);
                        table.Cell().Background("#E2E8F0").Padding(5).AlignCenter().Text(totalPracticals.ToString()).Bold().FontSize(8.5f);
                        table.Cell().Background("#E2E8F0").Padding(5).AlignRight().Text($"INR {totalAmount:N0}").Bold().FontSize(9f).FontColor("#102A43");
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(20).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).Text("Prepared By: DCMS Coordinator").FontSize(7.5f).FontColor("#64748B");
                        });
                        r.ConstantItem(35);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).Text("Verified By: Program Head").FontSize(7.5f).FontColor("#64748B");
                        });
                        r.ConstantItem(35);
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).Text("Approved By: Dean / Director").FontSize(7.5f).FontColor("#64748B");
                        });
                    });
                    col.Item().PaddingTop(8).Row(r =>
                    {
                        r.RelativeItem().Text("DCMS Degree Class Management System — Confidential").FontSize(7f).FontColor("#94A3B8");
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateFacultyPaymentReceiptPdf(
        FacultyPaymentOverview faculty,
        DateTime monthStart,
        DateTime monthEnd,
        string? webRootPath = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? nirvaaLogo = null;
        byte[]? mitLogo = null;

        try
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var nirvaaPath = Path.Combine(webRootPath, "images", "nirvaa.jpg");
                if (File.Exists(nirvaaPath)) nirvaaLogo = File.ReadAllBytes(nirvaaPath);

                var mitPath = Path.Combine(webRootPath, "images", "mit-wpu.jpg");
                if (File.Exists(mitPath)) mitLogo = File.ReadAllBytes(mitPath);
            }
        }
        catch
        {
            // Fallback gracefully without logos if disk read fails
        }

        var amountInWords = NumberToWordsINR(faculty.MonthlyPayableAmount);
        var voucherNo = $"NIRVAA/MIT-WPU/VOUCH/{monthStart:yyyyMM}/{faculty.FacultyId:D3}";

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Arial"));

                // Header with Dual Logos
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // NIRVAA Logo & Identity
                        row.RelativeItem(2).Row(r =>
                        {
                            r.Spacing(6);
                            if (nirvaaLogo is not null)
                            {
                                r.AutoItem().Height(38).Image(nirvaaLogo).FitArea();
                            }
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("NIRVAA SOLUTIONS").FontSize(11).Bold().FontColor("#102A43");
                                c.Item().Text("Corporate & EduTech Partner").FontSize(7.5f).FontColor("#1E5CA8");
                                c.Item().Text("Pune, Maharashtra, India").FontSize(7f).FontColor("#64748B");
                            });
                        });

                        // Center Collaboration Tag
                        row.AutoItem().PaddingHorizontal(6).AlignCenter().Column(c =>
                        {
                            c.Item().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").PaddingVertical(3).PaddingHorizontal(6).Text("ACADEMIC PARTNERSHIP").FontSize(6.5f).Bold().FontColor("#92400E");
                        });

                        // MIT-WPU Logo & Identity
                        row.RelativeItem(2).AlignRight().Row(r =>
                        {
                            r.Spacing(6);
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text("MIT WORLD PEACE UNIVERSITY").FontSize(10.5f).Bold().FontColor("#102A43");
                                c.Item().Text("School of Computer Science & Tech").FontSize(7.5f).FontColor("#B45309");
                                c.Item().Text("Kothrud, Pune - 411038").FontSize(7f).FontColor("#64748B");
                            });
                            if (mitLogo is not null)
                            {
                                r.AutoItem().Height(38).Image(mitLogo).FitArea();
                            }
                        });
                    });

                    col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#164E80");
                    col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor("#E2E8F0");

                    // Title & Voucher Metadata Banner
                    col.Item().PaddingTop(6).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FACULTY REMUNERATION VOUCHER").FontSize(12).Bold().FontColor("#102A43");
                            c.Item().Text("Official Statement of Teaching Remuneration & Class Conduction").FontSize(7.5f).FontColor("#64748B");
                        });
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text($"VOUCHER NO: {voucherNo}").FontSize(8f).Bold().FontColor("#164E80");
                            c.Item().Text($"DATE OF ISSUE: {DateTime.Now:dd MMMM yyyy}").FontSize(7.5f).FontColor("#334155");
                            c.Item().Text($"BILLING PERIOD: {monthStart:dd MMMM yyyy} – {monthEnd:dd MMMM yyyy}").FontSize(7.5f).Bold().FontColor("#047857");
                            c.Item().Text($"PAYMENT MODE: CASH").FontSize(7.5f).Bold().FontColor("#B45309");
                        });
                    });
                });

                // Content Body
                page.Content().PaddingVertical(8).Column(col =>
                {
                    // 1. Faculty Profile Card
                    col.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("FACULTY DETAILS").FontSize(7f).Bold().FontColor("#64748B");
                            c.Item().Text(faculty.FacultyName).FontSize(12).Bold().FontColor("#102A43");
                            c.Item().PaddingTop(2).Text($"Affiliation: {faculty.Organization}  |  Role: {faculty.Role}").FontSize(8f).FontColor("#334155");
                            if (!string.IsNullOrWhiteSpace(faculty.Designation))
                                c.Item().Text($"Designation: {faculty.Designation}").FontSize(7.5f).FontColor("#64748B");
                        });

                        r.ConstantItem(175).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                        {
                            c.Item().Text("CONFIGURED REMUNERATION RATES").FontSize(7f).Bold().FontColor("#64748B");
                            c.Item().PaddingTop(2).Row(rateRow =>
                            {
                                rateRow.RelativeItem().Text($"Theory Lecture:").FontSize(7.5f).FontColor("#475569");
                                rateRow.AutoItem().Text($"INR {faculty.LectureRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                            });
                            c.Item().Row(rateRow =>
                            {
                                rateRow.RelativeItem().Text($"Practical Lab:").FontSize(7.5f).FontColor("#475569");
                                rateRow.AutoItem().Text($"INR {faculty.PracticalRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                            });
                            c.Item().Row(rateRow =>
                            {
                                rateRow.RelativeItem().Text($"Disbursement Mode:").FontSize(7.5f).FontColor("#475569");
                                rateRow.AutoItem().Text($"Cash").Bold().FontSize(8f).FontColor("#B45309");
                            });
                        });
                    });

                    // 2. Section Heading: Conduction Audit
                    col.Item().PaddingTop(8).PaddingBottom(3).Row(r =>
                    {
                        r.RelativeItem().Text("ITEMIZED TEACHING CONDUCTION AUDIT").FontSize(8.5f).Bold().FontColor("#102A43");
                        var auditPeriod = (monthStart.Day == 1 && monthEnd.Day == DateTime.DaysInMonth(monthStart.Year, monthStart.Month) && monthStart.Month == monthEnd.Month)
                            ? monthStart.ToString("MMMM yyyy")
                            : $"{monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}";
                        r.RelativeItem().AlignRight().Text($"{faculty.MonthlySessions.Count} Sessions Conducted ({auditPeriod})").FontSize(7.5f).FontColor("#64748B");
                    });

                    // 3. Itemized Table of Sessions
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(22);
                            columns.ConstantColumn(72);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(68);
                            columns.ConstantColumn(105);
                            columns.ConstantColumn(70);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(7.5f);
                            header.Cell().Background("#102A43").Padding(4).Text("Date").FontColor(Colors.White).Bold().FontSize(7.5f);
                            header.Cell().Background("#102A43").Padding(4).Text("Subject / Module").FontColor(Colors.White).Bold().FontSize(7.5f);
                            header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Session Type").FontColor(Colors.White).Bold().FontSize(7.5f);
                            header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Timing / Duration").FontColor(Colors.White).Bold().FontSize(7.5f);
                            header.Cell().Background("#102A43").Padding(4).AlignRight().Text("Fee (INR)").FontColor(Colors.White).Bold().FontSize(7.5f);
                        });

                        if (faculty.MonthlySessions.Any())
                        {
                            int idx = 1;
                            foreach (var s in faculty.MonthlySessions)
                            {
                                var bg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";
                                table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                                table.Cell().Background(bg).Padding(3.5f).Text(s.Date.ToString("ddd, dd MMM")).FontSize(7.5f).Bold();
                                table.Cell().Background(bg).Padding(3.5f).Column(c =>
                                {
                                    c.Item().Text(s.CourseName).FontSize(7.5f);
                                    c.Item().Text(s.CourseCode).FontSize(6.5f).FontColor("#64748B");
                                });
                                table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(s.SessionType).FontSize(7.5f);
                                table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text($"{FormatTimePdf(s.StartTime)}–{FormatTimePdf(s.EndTime)} ({s.DurationHours:0.#}h)").FontSize(7f);
                                table.Cell().Background(bg).Padding(3.5f).AlignRight().Text($"INR {s.Amount:N0}").Bold().FontSize(7.5f).FontColor("#102A43");
                                idx++;
                            }
                        }
                        else
                        {
                            table.Cell().ColumnSpan(6).Background("#F8FAFC").Padding(7).AlignCenter().Text("No conducted sessions recorded for this billing cycle.").FontSize(7.5f).Italic().FontColor("#64748B");
                        }
                    });

                    // 4, 5, 6. Remuneration Summary, Certification & Signatures (Guaranteed to stay together cleanly)
                    col.Item().ShowEntire().Column(botCol =>
                    {
                        // Summary Matrix Box
                        botCol.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Background("#FFFFFF").Padding(7).Row(r =>
                        {
                            r.RelativeItem(2).Column(c =>
                            {
                                c.Item().Text("COMPENSATION SUMMARY MATRIX").FontSize(8f).Bold().FontColor("#102A43");
                                c.Item().PaddingTop(2).Text($"• Theory Lectures: {faculty.MonthlyLectureCount} conducted × INR {faculty.LectureRate:N0} = INR {(faculty.MonthlyLectureCount * faculty.LectureRate):N0}").FontSize(7.5f).FontColor("#334155");
                                c.Item().Text($"• Practical Labs:   {faculty.MonthlyPracticalCount} conducted × INR {faculty.PracticalRate:N0} = INR {(faculty.MonthlyPracticalCount * faculty.PracticalRate):N0}").FontSize(7.5f).FontColor("#334155");
                                c.Item().PaddingTop(3).Text($"Amount in Words: {amountInWords}").FontSize(7.5f).Bold().FontColor("#164E80");
                            });

                            r.ConstantItem(165).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                            {
                                c.Item().Text("NET REMUNERATION PAYABLE").FontSize(7f).Bold().FontColor("#047857");
                                c.Item().Text($"INR {faculty.MonthlyPayableAmount:N0}").FontSize(15).Bold().FontColor("#047857");
                                c.Item().PaddingTop(2).Border(1).BorderColor("#FDE68A").Background("#FFFBEB").Padding(2.5f).AlignCenter().Text("REMUNERATION STATEMENT · NOT A RECEIPT").FontSize(6.5f).Bold().FontColor("#92400E");
                            });
                        });

                        // Verification Certificate Note
                        botCol.Item().PaddingTop(6).Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(5).Row(r =>
                        {
                            r.RelativeItem().Text("CERTIFICATION: This system-generated voucher certifies that the above sessions were duly conducted by the faculty member and approved under academic guidelines. This statement shows calculated remuneration and does not certify payment or settlement.").FontSize(6.5f).FontColor("#64748B");
                        });

                        // Signatures inside Content Flow
                        botCol.Item().PaddingTop(18).Row(r =>
                        {
                            // NIRVAA Signature
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(24);
                                c.Item().LineHorizontal(1).LineColor("#94A3B8");
                                c.Item().PaddingTop(2).Text("Authorized Signatory").FontSize(8f).Bold().FontColor("#102A43");
                                c.Item().Text("NIRVAA SOLUTIONS").FontSize(7f).FontColor("#64748B");
                            });

                            r.ConstantItem(80);

                            // Faculty Receiver
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().PaddingBottom(24);
                                c.Item().LineHorizontal(1).LineColor("#94A3B8");
                                c.Item().PaddingTop(2).Text("Faculty Signature").FontSize(8f).Bold().FontColor("#102A43");
                                c.Item().Text("Faculty review acknowledgement (not proof of payment)").FontSize(7f).FontColor("#64748B");
                            });
                        });
                    });
                });

                // Footer (Confidentiality & Page numbering)
                page.Footer().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("DCMS — Degree Class Management System  |  Confidential Financial Record").FontSize(6.5f).FontColor("#94A3B8");
                    r.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateAllFacultyReceiptsPdf(
        IReadOnlyList<FacultyPaymentOverview> faculties,
        DateTime monthStart,
        DateTime monthEnd,
        string? webRootPath = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? nirvaaLogo = null;
        byte[]? mitLogo = null;

        try
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var nirvaaPath = Path.Combine(webRootPath, "images", "nirvaa.jpg");
                if (File.Exists(nirvaaPath)) nirvaaLogo = File.ReadAllBytes(nirvaaPath);

                var mitPath = Path.Combine(webRootPath, "images", "mit-wpu.jpg");
                if (File.Exists(mitPath)) mitLogo = File.ReadAllBytes(mitPath);
            }
        }
        catch
        {
            // Fallback gracefully without logos if disk read fails
        }

        var doc = Document.Create(container =>
        {
            foreach (var faculty in faculties)
            {
                var amountInWords = NumberToWordsINR(faculty.MonthlyPayableAmount);
                var voucherNo = $"NIRVAA/MIT-WPU/VOUCH/{monthStart:yyyyMM}/{faculty.FacultyId:D3}";

                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem(2).Row(r =>
                            {
                                r.Spacing(6);
                                if (nirvaaLogo is not null)
                                {
                                    r.AutoItem().Height(38).Image(nirvaaLogo).FitArea();
                                }
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("NIRVAA SOLUTIONS").FontSize(11).Bold().FontColor("#102A43");
                                    c.Item().Text("Corporate & EduTech Partner").FontSize(7.5f).FontColor("#1E5CA8");
                                    c.Item().Text("Pune, Maharashtra, India").FontSize(7f).FontColor("#64748B");
                                });
                            });

                            row.AutoItem().PaddingHorizontal(6).AlignCenter().Column(c =>
                            {
                                c.Item().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").PaddingVertical(3).PaddingHorizontal(6).Text("ACADEMIC PARTNERSHIP").FontSize(6.5f).Bold().FontColor("#92400E");
                            });

                            row.RelativeItem(2).AlignRight().Row(r =>
                            {
                                r.Spacing(6);
                                r.RelativeItem().AlignRight().Column(c =>
                                {
                                    c.Item().Text("MIT WORLD PEACE UNIVERSITY").FontSize(10.5f).Bold().FontColor("#102A43");
                                    c.Item().Text("School of Computer Science & Tech").FontSize(7.5f).FontColor("#B45309");
                                    c.Item().Text("Kothrud, Pune - 411038").FontSize(7f).FontColor("#64748B");
                                });
                                if (mitLogo is not null)
                                {
                                    r.AutoItem().Height(38).Image(mitLogo).FitArea();
                                }
                            });
                        });

                        col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#164E80");
                        col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor("#E2E8F0");

                        col.Item().PaddingTop(6).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FACULTY REMUNERATION VOUCHER").FontSize(12).Bold().FontColor("#102A43");
                                c.Item().Text("Official Statement of Teaching Remuneration & Class Conduction").FontSize(7.5f).FontColor("#64748B");
                            });
                            r.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"VOUCHER NO: {voucherNo}").FontSize(8f).Bold().FontColor("#164E80");
                                c.Item().Text($"DATE OF ISSUE: {DateTime.Now:dd MMMM yyyy}").FontSize(7.5f).FontColor("#334155");
                                c.Item().Text($"BILLING PERIOD: {monthStart:dd MMMM yyyy} – {monthEnd:dd MMMM yyyy}").FontSize(7.5f).Bold().FontColor("#047857");
                                c.Item().Text($"PAYMENT MODE: CASH").FontSize(7.5f).Bold().FontColor("#B45309");
                            });
                        });
                    });

                    page.Content().PaddingVertical(8).Column(col =>
                    {
                        col.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Row(r =>
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text("FACULTY DETAILS").FontSize(7f).Bold().FontColor("#64748B");
                                c.Item().Text(faculty.FacultyName).FontSize(12).Bold().FontColor("#102A43");
                                c.Item().PaddingTop(2).Text($"Affiliation: {faculty.Organization}  |  Role: {faculty.Role}").FontSize(8f).FontColor("#334155");
                                if (!string.IsNullOrWhiteSpace(faculty.Designation))
                                    c.Item().Text($"Designation: {faculty.Designation}").FontSize(7.5f).FontColor("#64748B");
                            });

                            r.ConstantItem(175).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                            {
                                c.Item().Text("CONFIGURED REMUNERATION RATES").FontSize(7f).Bold().FontColor("#64748B");
                                c.Item().PaddingTop(2).Row(rateRow =>
                                {
                                    rateRow.RelativeItem().Text($"Theory Lecture:").FontSize(7.5f).FontColor("#475569");
                                    rateRow.AutoItem().Text($"INR {faculty.LectureRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                                });
                                c.Item().Row(rateRow =>
                                {
                                    rateRow.RelativeItem().Text($"Practical Lab:").FontSize(7.5f).FontColor("#475569");
                                    rateRow.AutoItem().Text($"INR {faculty.PracticalRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                                });
                                c.Item().Row(rateRow =>
                                {
                                    rateRow.RelativeItem().Text($"Disbursement Mode:").FontSize(7.5f).FontColor("#475569");
                                    rateRow.AutoItem().Text($"Cash").Bold().FontSize(8f).FontColor("#B45309");
                                });
                            });
                        });

                        col.Item().PaddingTop(8).PaddingBottom(3).Row(r =>
                        {
                            r.RelativeItem().Text("ITEMIZED TEACHING CONDUCTION AUDIT").FontSize(8.5f).Bold().FontColor("#102A43");
                            var auditPeriod = (monthStart.Day == 1 && monthEnd.Day == DateTime.DaysInMonth(monthStart.Year, monthStart.Month) && monthStart.Month == monthEnd.Month)
                                ? monthStart.ToString("MMMM yyyy")
                                : $"{monthStart:dd MMM yyyy} – {monthEnd:dd MMM yyyy}";
                            r.RelativeItem().AlignRight().Text($"{faculty.MonthlySessions.Count} Sessions ({auditPeriod})").FontSize(7.5f).FontColor("#64748B");
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(22);
                                columns.ConstantColumn(72);
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(68);
                                columns.ConstantColumn(105);
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(7.5f);
                                header.Cell().Background("#102A43").Padding(4).Text("Date").FontColor(Colors.White).Bold().FontSize(7.5f);
                                header.Cell().Background("#102A43").Padding(4).Text("Subject / Module").FontColor(Colors.White).Bold().FontSize(7.5f);
                                header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Session Type").FontColor(Colors.White).Bold().FontSize(7.5f);
                                header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Timing / Duration").FontColor(Colors.White).Bold().FontSize(7.5f);
                                header.Cell().Background("#102A43").Padding(4).AlignRight().Text("Fee (INR)").FontColor(Colors.White).Bold().FontSize(7.5f);
                            });

                            if (faculty.MonthlySessions.Any())
                            {
                                int idx = 1;
                                foreach (var s in faculty.MonthlySessions)
                                {
                                    var bg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";
                                    table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                                    table.Cell().Background(bg).Padding(3.5f).Text(s.Date.ToString("ddd, dd MMM")).FontSize(7.5f).Bold();
                                    table.Cell().Background(bg).Padding(3.5f).Column(c =>
                                    {
                                        c.Item().Text(s.CourseName).FontSize(7.5f);
                                        c.Item().Text(s.CourseCode).FontSize(6.5f).FontColor("#64748B");
                                    });
                                    table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(s.SessionType).FontSize(7.5f);
                                    table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text($"{FormatTimePdf(s.StartTime)}–{FormatTimePdf(s.EndTime)} ({s.DurationHours:0.#}h)").FontSize(7f);
                                    table.Cell().Background(bg).Padding(3.5f).AlignRight().Text($"INR {s.Amount:N0}").Bold().FontSize(7.5f).FontColor("#102A43");
                                    idx++;
                                }
                            }
                            else
                            {
                                table.Cell().ColumnSpan(6).Background("#F8FAFC").Padding(7).AlignCenter().Text("No conducted sessions recorded for this billing cycle.").FontSize(7.5f).Italic().FontColor("#64748B");
                            }
                        });

                        // 4, 5, 6. Remuneration Summary, Certification & Signatures (Guaranteed to stay together cleanly)
                        col.Item().ShowEntire().Column(botCol =>
                        {
                            // Summary Matrix Box
                            botCol.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Background("#FFFFFF").Padding(7).Row(r =>
                            {
                                r.RelativeItem(2).Column(c =>
                                {
                                    c.Item().Text("COMPENSATION SUMMARY MATRIX").FontSize(8f).Bold().FontColor("#102A43");
                                    c.Item().PaddingTop(2).Text($"• Theory Lectures: {faculty.MonthlyLectureCount} conducted × INR {faculty.LectureRate:N0} = INR {(faculty.MonthlyLectureCount * faculty.LectureRate):N0}").FontSize(7.5f).FontColor("#334155");
                                    c.Item().Text($"• Practical Labs:   {faculty.MonthlyPracticalCount} conducted × INR {faculty.PracticalRate:N0} = INR {(faculty.MonthlyPracticalCount * faculty.PracticalRate):N0}").FontSize(7.5f).FontColor("#334155");
                                    c.Item().PaddingTop(3).Text($"Amount in Words: {amountInWords}").FontSize(7.5f).Bold().FontColor("#164E80");
                                });

                                r.ConstantItem(165).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                                {
                                    c.Item().Text("NET REMUNERATION PAYABLE").FontSize(7f).Bold().FontColor("#047857");
                                    c.Item().Text($"INR {faculty.MonthlyPayableAmount:N0}").FontSize(15).Bold().FontColor("#047857");
                                    c.Item().PaddingTop(2).Border(1).BorderColor("#FDE68A").Background("#FFFBEB").Padding(2.5f).AlignCenter().Text("REMUNERATION STATEMENT · NOT A RECEIPT").FontSize(6.5f).Bold().FontColor("#92400E");
                                });
                            });

                            // Verification Certificate Note
                            botCol.Item().PaddingTop(6).Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(5).Row(r =>
                            {
                                r.RelativeItem().Text("CERTIFICATION: This system-generated voucher certifies that the above sessions were duly conducted by the faculty member and approved under academic guidelines. This statement shows calculated remuneration and does not certify payment or settlement.").FontSize(6.5f).FontColor("#64748B");
                            });

                            // Signatures inside Content Flow
                            botCol.Item().PaddingTop(18).Row(r =>
                            {
                                // NIRVAA Signature
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().PaddingBottom(24);
                                    c.Item().LineHorizontal(1).LineColor("#94A3B8");
                                    c.Item().PaddingTop(2).Text("Authorized Signatory").FontSize(8f).Bold().FontColor("#102A43");
                                    c.Item().Text("NIRVAA SOLUTIONS").FontSize(7f).FontColor("#64748B");
                                });

                                r.ConstantItem(80);

                                // Faculty Receiver
                                r.RelativeItem().Column(c =>
                                {
                                    c.Item().PaddingBottom(24);
                                    c.Item().LineHorizontal(1).LineColor("#94A3B8");
                                    c.Item().PaddingTop(2).Text("Faculty Signature").FontSize(8f).Bold().FontColor("#102A43");
                                    c.Item().Text("Faculty review acknowledgement (not proof of payment)").FontSize(7f).FontColor("#64748B");
                                });
                            });
                        });
                    });

                    page.Footer().PaddingTop(4).Row(r =>
                    {
                        r.RelativeItem().Text("DCMS — Degree Class Management System  |  Confidential Financial Record").FontSize(6.5f).FontColor("#94A3B8");
                        r.RelativeItem().AlignRight().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                    });
                });
            }
        });

        return doc.GeneratePdf();
    }

    public async Task<ExecutiveSanctionData> GetExecutiveSanctionDataAsync(
        DateTime startDate,
        DateTime endDate,
        string? customRefNo = null,
        DateTime? memoDate = null,
        string? customCycleTitle = null,
        string? customNarrative = null,
        string? customStatus = null)
    {
        var sDate = startDate.Date;
        var eDate = endDate.Date;
        if (eDate < sDate) (sDate, eDate) = (eDate, sDate);

        var faculties = await db.Faculties
            .Where(f => f.IsActive)
            .Include(f => f.Rates)
            .ToListAsync();

        var conductedSessions = (await db.Sessions
            .Where(s => s.Date >= sDate && s.Date <= eDate && s.Status == SessionStatus.Conducted && s.IsApproved)
            .Include(s => s.Course)
            .Include(s => s.Faculty)
            .OrderBy(s => s.Date)
            .ToListAsync())
            .OrderBy(s => s.Date).ThenBy(s => s.ActualStartTime).ToList();
        if (!conductedSessions.Any())
        {
            var scheduled = (await db.Sessions
                .Where(s => s.Date >= sDate && s.Date <= eDate && s.Status != SessionStatus.Cancelled)
                .Include(s => s.Course)
                .Include(s => s.Faculty)
                .OrderBy(s => s.Date)
                .ToListAsync())
                .OrderBy(s => s.Date).ThenBy(s => s.ActualStartTime).ToList();
            if (scheduled.Any())
                conductedSessions = scheduled;
        }

        var facultyGroups = conductedSessions.GroupBy(s => s.FacultyId).ToList();
        var items = new List<ExecutiveSanctionFacultyItem>();
        int sr = 1;

        var facultyListWithSessions = facultyGroups
            .Select(group =>
            {
                var fac = faculties.FirstOrDefault(f => f.Id == group.Key) ?? group.First().Faculty;
                var hours = group.Sum(s => s.DurationHours);
                var rates = fac.Rates;
                var currentRate = rates
                    .Where(r => r.EffectiveFrom <= eDate && (r.EffectiveTo == null || r.EffectiveTo > eDate))
                    .OrderByDescending(r => r.EffectiveFrom)
                    .FirstOrDefault();
                var rateVal = currentRate is null
                    ? FacultyService.DefaultClassRateINR
                    : currentRate.LectureRateINR > 0 ? currentRate.LectureRateINR : currentRate.HourlyRateINR;
                var payable = hours * rateVal;
                return new { Faculty = fac, Sessions = group.ToList(), Hours = hours, Rate = rateVal, Payable = payable };
            })
            .OrderByDescending(x => x.Faculty.Organization == "NIRVAA")
            .ThenByDescending(x => x.Payable)
            .ToList();

        foreach (var entry in facultyListWithSessions)
        {
            var fac = entry.Faculty;
            var sList = entry.Sessions;
            var voucherNo = $"VOUCH-{fac.Id:D3}";

            string orgRole;
            if (fac.Organization == "NIRVAA")
            {
                orgRole = string.IsNullOrWhiteSpace(fac.Designation) || fac.Designation.Contains("Teacher")
                    ? "NIRVAA / Faculty"
                    : (fac.Designation.Contains("Director") || fac.Designation.Contains("Head") || fac.Designation.Contains("Manager") || fac.Designation.Contains("Architect")
                        ? "NIRVAA / Industry Expert"
                        : $"NIRVAA / {fac.Designation}");
            }
            else
            {
                orgRole = $"MIT-WPU / {fac.Designation ?? "Professor"}";
            }

            var distinctCourses = sList.Select(s => s.Course).Where(c => c != null).DistinctBy(c => c.Id).ToList();
            var subjectName = distinctCourses.Count switch
            {
                1 => $"{ShortenCourseName(distinctCourses[0].CourseName)} ({distinctCourses[0].CourseCode})",
                > 1 => string.Join(", ", distinctCourses.Select(c => $"{ShortenCourseName(c.CourseName)} ({c.CourseCode})")),
                _ => "Academic Sessions"
            };

            var scheduleText = FormatScheduleSummary(sList);

            items.Add(new ExecutiveSanctionFacultyItem(
                sr++,
                voucherNo,
                fac.Id,
                fac.FullName,
                fac.Organization,
                orgRole,
                subjectName,
                scheduleText,
                entry.Hours,
                entry.Rate,
                entry.Payable
            ));
        }

        var totalHours = items.Sum(x => x.Hours);
        var totalAmount = items.Sum(x => x.PayableAmount);
        var amountInWords = NumberToWordsINR(totalAmount);

        var cycle = customCycleTitle ?? (sDate.Day == 1 && eDate.Day == DateTime.DaysInMonth(sDate.Year, sDate.Month)
            ? $"{sDate:MMMM yyyy} ({sDate:dd}–{eDate:dd MMM})"
            : $"{sDate:dd MMM yyyy} – {eDate:dd MMM yyyy}");

        var refNo = customRefNo ?? $"NIRVAA/MIT-WPU/REM/{sDate:yyyyMM}/01";
        var mDate = memoDate ?? (DateTime.Today > eDate ? (sDate.Month == 8 && sDate.Year == 2026 ? new DateTime(2026, 9, 9) : eDate.AddDays(7)) : DateTime.Today);

        var minVouch = items.Any() ? items.Min(x => x.VoucherNo) : "VOUCH-001";
        var maxVouch = items.Any() ? items.Max(x => x.VoucherNo) : "VOUCH-001";
        var enclosures = $"(1) Signed DCMS Vouchers {minVouch} to {maxVouch} | (2) Student Attendance Logs | (3) Weekly Conduction Audit";

        var subjectLine = $"APPROVAL & DISBURSEMENT SANCTION FOR {cycle.ToUpper()} FACULTY REMUNERATION (TOTAL: INR {totalAmount:N0}/-)";

        var narrative = customNarrative;
        if (string.IsNullOrWhiteSpace(narrative))
        {
            var highlights = items
                .Where(x => x.Organization == "MIT-WPU" || x.FacultyName.Contains("Gutte") || x.FacultyName.Contains("Hambarde") || x.FacultyName.Contains("Birajdar"))
                .Select(x => $"{x.FacultyName} conducted {x.Hours:0.0} hours (INR {x.PayableAmount:N0}/-) in {x.SubjectAndCode}")
                .ToList();

            var highlightSentence = highlights.Count switch
            {
                1 => $" Specifically, {highlights[0]}.",
                2 => $" Specifically, {highlights[0]}, while {highlights[1]}.",
                3 => $" Specifically, {highlights[0]} and {highlights[1]}, while {highlights[2]}.",
                > 3 => $" Specifically, {string.Join(", ", highlights.Take(highlights.Count - 1))}, while {highlights.Last()}.",
                _ => items.Any() ? $" Specifically, {items.First().FacultyName} conducted {items.First().Hours:0.0} hours (INR {items.First().PayableAmount:N0}/-) in {items.First().SubjectAndCode}." : ""
            };

            var deferralSentence = sDate.Month == 8 && sDate.Year == 2026
                ? "September lectures (including 05 Sept) have been strictly excluded and deferred to the September cycle."
                : "Sessions falling outside this cycle date window have been strictly excluded and deferred to the subsequent billing cycle.";

            narrative = $"Submitted for executive approval is the audited faculty remuneration schedule and individual signed payment vouchers from the Department & Course Management System (DCMS) for sessions conducted strictly during the {cycle} under the MIT-WPU × NIRVAA M.Tech in DCSE program. All {totalHours:0.#} teaching hours have been verified against academic timetables, biometric LMS logs, and faculty reports at the approved rate of INR 1,200/hr. {deferralSentence}{highlightSentence}";
        }

        var approvalMatrix = new List<ExecutiveSignatory>
        {
            new(1, "1. TECHNICAL REVIEW", "Mr. Siddu Patil", "Head – IT & Analytics", "Curriculum & Session Conduction Verification", "Approved & Cleared", null, null),
            new(2, "2. FINANCE CLEARANCE", "Mr. Yadnesh Bauskar", "Director - Strategy & Finance", "Budget Sanction & Accounts Clearance", "Approved & Cleared", null, null),
            new(3, "3. ACADEMIC ENDORSEMENT", "Mr. Anup Goel", "Director – Academics & Training", "Academic Program & Faculty Conduction Sign-off", "Approved & Cleared", null, null),
            new(4, "4. FINAL SANCTION", "Dr. Jagdish Shinde", "Managing Director", "Executive Sanction & Banking Release", "Approved & Cleared", null, null),
        };

        return new ExecutiveSanctionData(
            refNo,
            mDate,
            cycle,
            sDate,
            eDate,
            customStatus ?? "Audited & Recommended",
            "Head of Human Resources & Accounts, NIRVAA Solutions Pvt. Ltd.",
            "Academic Operations & DCMS",
            subjectLine,
            narrative,
            items,
            totalHours,
            totalAmount,
            amountInWords,
            "Cash / Bank Transfer",
            enclosures,
            approvalMatrix
        );
    }

    public byte[] GenerateExecutiveSanctionCoverPagePdf(
        ExecutiveSanctionData data,
        string? webRootPath = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? nirvaaLogo = null;
        byte[]? mitLogo = null;

        try
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var nirvaaPath = Path.Combine(webRootPath, "images", "nirvaa.jpg");
                if (File.Exists(nirvaaPath)) nirvaaLogo = File.ReadAllBytes(nirvaaPath);

                var mitPath = Path.Combine(webRootPath, "images", "mit-wpu.jpg");
                if (File.Exists(mitPath)) mitLogo = File.ReadAllBytes(mitPath);
            }
        }
        catch
        {
        }

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                RenderExecutiveCoverPageContent(page, data, nirvaaLogo, mitLogo);
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateExecutiveSanctionDossierPdf(
        ExecutiveSanctionData data,
        string? webRootPath = null,
        IReadOnlyList<FacultyPaymentOverview>? faculties = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        byte[]? nirvaaLogo = null;
        byte[]? mitLogo = null;

        try
        {
            if (!string.IsNullOrEmpty(webRootPath))
            {
                var nirvaaPath = Path.Combine(webRootPath, "images", "nirvaa.jpg");
                if (File.Exists(nirvaaPath)) nirvaaLogo = File.ReadAllBytes(nirvaaPath);

                var mitPath = Path.Combine(webRootPath, "images", "mit-wpu.jpg");
                if (File.Exists(mitPath)) mitLogo = File.ReadAllBytes(mitPath);
            }
        }
        catch
        {
        }

        var doc = Document.Create(container =>
        {
            // Page 1: Executive Cover Page
            container.Page(page =>
            {
                RenderExecutiveCoverPageContent(page, data, nirvaaLogo, mitLogo);
            });

            // Subsequent Pages: Individual signed faculty vouchers
            var relevantFaculties = new List<FacultyPaymentOverview>();
            if (faculties != null && faculties.Any())
            {
                relevantFaculties = faculties
                    .Where(f => data.FacultyItems.Any(item => item.FacultyId == f.FacultyId))
                    .ToList();
                if (!relevantFaculties.Any())
                    relevantFaculties = faculties.Where(f => f.MonthlyPayableAmount > 0).ToList();
            }

            if (!relevantFaculties.Any())
            {
                // Synthesize from ExecutiveSanctionFacultyItems so vouchers are always rendered
                foreach (var item in data.FacultyItems)
                {
                    relevantFaculties.Add(new FacultyPaymentOverview(
                        item.FacultyId,
                        item.FacultyName,
                        item.Organization,
                        "Visiting Faculty",
                        item.DesignationOrRole,
                        item.Rate,
                        item.Rate,
                        0,
                        0,
                        0,
                        (int)item.Hours,
                        0,
                        item.PayableAmount,
                        new List<FacultyConductedSessionItem>(),
                        new List<FacultyConductedSessionItem>
                        {
                            new FacultyConductedSessionItem(
                                item.FacultyId,
                                data.StartDate,
                                item.SubjectAndCode,
                                item.SubjectAndCode,
                                "Lecture",
                                new TimeSpan(9, 0, 0),
                                new TimeSpan(9 + (int)Math.Max(1, Math.Min(12, (long)item.Hours)), 0, 0),
                                item.Hours,
                                item.PayableAmount)
                        }
                    ));
                }
            }

            foreach (var faculty in relevantFaculties)
            {
                RenderIndividualReceiptPage(container, faculty, data.StartDate, data.EndDate, nirvaaLogo, mitLogo);
            }
        });

        return doc.GeneratePdf();
    }

    public byte[] GenerateExecutiveSanctionDossierPdf(
        ExecutiveSanctionData data,
        IReadOnlyList<FacultyPaymentOverview> faculties,
        string? webRootPath = null)
        => GenerateExecutiveSanctionDossierPdf(data, webRootPath, faculties);

    private static void RenderExecutiveCoverPageContent(
        PageDescriptor page,
        ExecutiveSanctionData data,
        byte[]? nirvaaLogo,
        byte[]? mitLogo)
    {
        page.Size(PageSizes.A4);
        page.Margin(20);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(x => x.FontSize(8f).FontFamily("Arial"));

        page.Header().Column(headerCol =>
        {
            headerCol.Item().Row(row =>
            {
                row.ConstantItem(120).Column(c =>
                {
                    if (nirvaaLogo is not null)
                        c.Item().Height(36).Image(nirvaaLogo).FitArea();
                    else
                        c.Item().Text("NIRVAA").Bold().FontSize(12).FontColor("#102A43");
                    c.Item().PaddingTop(1).Text("We are just a call away").Italic().FontSize(6.5f).FontColor("#64748B");
                });

                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().AlignCenter().Text("NIRVAA SOLUTIONS PRIVATE LIMITED").FontSize(11.5f).Bold().FontColor("#102A43");
                    c.Item().AlignCenter().Text("Centre of Excellence in Digital Infrastructure & Cloud Engineering").FontSize(7.5f).FontColor("#164E80");
                    c.Item().AlignCenter().Text("In Academic Collaboration with MIT World Peace University (MIT-WPU), Pune").FontSize(7.5f).SemiBold().Italic().FontColor("#0284C7");
                });

                row.ConstantItem(120).AlignRight().Column(c =>
                {
                    if (mitLogo is not null)
                        c.Item().AlignRight().Height(36).Image(mitLogo).FitArea();
                    else
                        c.Item().AlignRight().Text("MIT-WPU").Bold().FontSize(12).FontColor("#102A43");
                });
            });

            headerCol.Item().PaddingTop(4).LineHorizontal(1.2f).LineColor("#164E80");
        });

        page.Content().PaddingTop(3).Column(col =>
        {
            col.Item().Border(1).BorderColor("#94A3B8").Background("#F8FAFC").Padding(4).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Text("MEMORANDUM FOR EXECUTIVE SANCTION").Bold().FontSize(8.5f).FontColor("#102A43");
                    r.AutoItem().Text($"DATE: {data.MemoDate:dd MMMM yyyy}").Bold().FontSize(8f).FontColor("#102A43");
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text(text =>
                    {
                        text.Span("REF NO: ").Bold().FontSize(7.5f).FontColor("#334155");
                        text.Span($"{data.ReferenceNumber}  |  ").FontSize(7.5f).FontColor("#334155");
                        text.Span("CYCLE: ").Bold().FontSize(7.5f).FontColor("#334155");
                        text.Span(data.CycleTitle).FontSize(7.5f).FontColor("#334155");
                    });
                    r.AutoItem().Text(text =>
                    {
                        text.Span("STATUS: ").Bold().FontSize(7.5f).FontColor("#15803D");
                        text.Span(data.Status).Bold().FontSize(7.5f).FontColor("#15803D");
                    });
                });
                c.Item().PaddingTop(2).Row(r =>
                {
                    r.RelativeItem().Text($"TO: {data.ToRecipient}").FontSize(7.5f).FontColor("#475569");
                    r.AutoItem().Text($"FROM: {data.FromSender}").FontSize(7.5f).FontColor("#475569");
                });
            });

            col.Item().PaddingTop(4).Border(1).BorderColor("#0284C7").Background("#E0F2FE").PaddingVertical(3.5f).PaddingHorizontal(6).Text(
                $"SUBJECT: {data.SubjectLine.ToUpper()}"
            ).FontSize(8f).Bold().FontColor("#0369A1");

            col.Item().PaddingTop(4).Column(c =>
            {
                c.Item().Text("Respected Authorities,").Bold().FontSize(7.5f).FontColor("#1E293B");
                c.Item().PaddingTop(1).Text(data.ExecutiveNarrative).FontSize(7.2f).FontColor("#334155").LineHeight(1.22f);
            });

            col.Item().PaddingTop(5).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(22);
                    columns.ConstantColumn(64);
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(3.2f);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(52);
                    columns.ConstantColumn(66);
                });

                table.Header(header =>
                {
                    header.Cell().Background("#102A43").Padding(3.5f).AlignCenter().Text("Sr.").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).AlignCenter().Text("Voucher").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).Text("Faculty Member").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).Text("Subject & Conduction Schedule").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).AlignCenter().Text("Hours").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).AlignRight().Text("Rate (INR)").FontColor(Colors.White).Bold().FontSize(7.5f);
                    header.Cell().Background("#102A43").Padding(3.5f).AlignRight().Text("Payable (INR)").FontColor(Colors.White).Bold().FontSize(7.5f);
                });

                if (data.FacultyItems.Any())
                {
                    foreach (var item in data.FacultyItems)
                    {
                        var bg = item.SrNo % 2 == 1 ? "#FFFFFF" : "#F8FAFC";
                        table.Cell().Background(bg).Padding(3f).AlignCenter().Text(item.SrNo.ToString()).FontSize(7f);
                        table.Cell().Background(bg).Padding(3f).AlignCenter().Text(item.VoucherNo).Bold().FontSize(7f).FontColor("#102A43");
                        table.Cell().Background(bg).Padding(3f).Column(fc =>
                        {
                            fc.Item().Text(item.FacultyName).Bold().FontSize(7.5f).FontColor("#0F172A");
                            fc.Item().Text(item.DesignationOrRole).FontSize(6.5f).FontColor("#64748B");
                        });
                        table.Cell().Background(bg).Padding(3f).Column(sc =>
                        {
                            sc.Item().Text(item.SubjectAndCode).FontSize(7.2f).FontColor("#0F172A");
                            sc.Item().Text(item.ConductionSchedule).FontSize(6.5f).FontColor("#0284C7");
                        });
                        table.Cell().Background(bg).Padding(3f).AlignCenter().Text($"{item.Hours:0.#}h").FontSize(7f);
                        table.Cell().Background(bg).Padding(3f).AlignRight().Text($"{item.Rate:N0}").FontSize(7f);
                        table.Cell().Background(bg).Padding(3f).AlignRight().Text($"{item.PayableAmount:N0}").Bold().FontSize(7.5f).FontColor("#0F172A");
                    }
                }
                else
                {
                    table.Cell().ColumnSpan(7).Background("#F8FAFC").Padding(8).AlignCenter().Text("No conducted teaching sessions recorded for this date period.").FontSize(7.5f).Italic().FontColor("#64748B");
                }

                table.Cell().ColumnSpan(4).Background("#E2E8F0").Padding(3.5f).Text($"TOTAL CONSOLIDATED REMUNERATION ({data.CycleTitle.ToUpper()})").Bold().FontSize(7.5f).FontColor("#102A43");
                table.Cell().Background("#E2E8F0").Padding(3.5f).AlignCenter().Text($"{data.TotalHours:0.#}h").Bold().FontSize(7.5f).FontColor("#102A43");
                table.Cell().Background("#E2E8F0").Padding(3.5f).AlignCenter().Text("-").FontSize(7.5f);
                table.Cell().Background("#E2E8F0").Padding(3.5f).AlignRight().Text($"INR {data.TotalAmount:N0}").Bold().FontSize(8f).FontColor("#102A43");
            });

            col.Item().PaddingTop(4).Column(fc =>
            {
                fc.Item().Text(text =>
                {
                    text.Span("Amount in Words: ").Bold().FontSize(7.2f).FontColor("#1E293B");
                    text.Span($"{data.AmountInWords}  |  ").FontSize(7.2f).FontColor("#1E293B");
                    text.Span("Mode: ").Bold().FontSize(7.2f).FontColor("#1E293B");
                    text.Span(data.PaymentMode).FontSize(7.2f).FontColor("#1E293B");
                });
                fc.Item().PaddingTop(1).Text(text =>
                {
                    text.Span("Enclosures: ").Bold().FontSize(6.8f).FontColor("#475569");
                    text.Span(data.EnclosuresText).FontSize(6.8f).FontColor("#475569");
                });
            });

            col.Item().PaddingTop(5).Column(matrixCol =>
            {
                matrixCol.Item().Text("EXECUTIVE REVIEW & SIGNATURE APPROVAL MATRIX:").Bold().FontSize(7.2f).FontColor("#102A43");
                matrixCol.Item().PaddingTop(3).Row(r1 =>
                {
                    r1.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(4).Column(b => RenderSignatoryBox(b, data.ApprovalMatrix[0]));
                    r1.ConstantItem(8);
                    r1.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(4).Column(b => RenderSignatoryBox(b, data.ApprovalMatrix[1]));
                });
                matrixCol.Item().PaddingTop(4).Row(r2 =>
                {
                    r2.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(4).Column(b => RenderSignatoryBox(b, data.ApprovalMatrix[2]));
                    r2.ConstantItem(8);
                    r2.RelativeItem().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(4).Column(b => RenderSignatoryBox(b, data.ApprovalMatrix[3]));
                });
            });
        });
    }

    private static void RenderSignatoryBox(ColumnDescriptor b, ExecutiveSignatory signatory)
    {
        b.Item().Text(signatory.StepTitle).Bold().FontSize(7.2f).FontColor("#0284C7");
        b.Item().PaddingTop(1).Text(text =>
        {
            text.Span(signatory.Name).Bold().FontSize(7f).FontColor("#0F172A");
            text.Span(" | ").FontSize(6.5f).FontColor("#94A3B8");
            text.Span(signatory.Designation).FontSize(6.8f).FontColor("#475569");
        });
        b.Item().PaddingTop(1).Text($"Scope: {signatory.Scope}").FontSize(6.3f).FontColor("#64748B");
        b.Item().PaddingTop(2).Row(r =>
        {
            r.RelativeItem().Text($"Status: [ ] {signatory.StatusText}").FontSize(6.3f).FontColor("#334155");
            r.AutoItem().Text("Date: _________________").FontSize(6.3f).FontColor("#64748B");
        });
        b.Item().PaddingTop(3).Text("Signature: _________________________________________________").FontSize(6.3f).FontColor("#64748B");
    }

    private static void RenderIndividualReceiptPage(
        IDocumentContainer container,
        FacultyPaymentOverview faculty,
        DateTime monthStart,
        DateTime monthEnd,
        byte[]? nirvaaLogo,
        byte[]? mitLogo)
    {
        var amountInWords = NumberToWordsINR(faculty.MonthlyPayableAmount);
        var voucherNo = $"NIRVAA/MIT-WPU/VOUCH/{monthStart:yyyyMM}/{faculty.FacultyId:D3}";

        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(28);
            page.PageColor(Colors.White);
            page.DefaultTextStyle(x => x.FontSize(9f).FontFamily("Arial"));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem(2).Row(r =>
                    {
                        r.Spacing(6);
                        if (nirvaaLogo is not null)
                            r.AutoItem().Height(38).Image(nirvaaLogo).FitArea();
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("NIRVAA SOLUTIONS").FontSize(11).Bold().FontColor("#102A43");
                            c.Item().Text("Corporate & EduTech Partner").FontSize(7.5f).FontColor("#1E5CA8");
                            c.Item().Text("Pune, Maharashtra, India").FontSize(7f).FontColor("#64748B");
                        });
                    });

                    row.AutoItem().PaddingHorizontal(6).AlignCenter().Column(c =>
                    {
                        c.Item().Border(1).BorderColor("#FDE68A").Background("#FFFBEB").PaddingVertical(3).PaddingHorizontal(6).Text("ACADEMIC PARTNERSHIP").FontSize(6.5f).Bold().FontColor("#92400E");
                    });

                    row.RelativeItem(2).AlignRight().Row(r =>
                    {
                        r.Spacing(6);
                        r.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("MIT WORLD PEACE UNIVERSITY").FontSize(10.5f).Bold().FontColor("#102A43");
                            c.Item().Text("School of Computer Science & Tech").FontSize(7.5f).FontColor("#B45309");
                            c.Item().Text("Kothrud, Pune - 411038").FontSize(7f).FontColor("#64748B");
                        });
                        if (mitLogo is not null)
                            r.AutoItem().Height(38).Image(mitLogo).FitArea();
                    });
                });

                col.Item().PaddingTop(6).LineHorizontal(1.5f).LineColor("#164E80");
                col.Item().PaddingTop(2).LineHorizontal(0.5f).LineColor("#E2E8F0");

                col.Item().PaddingTop(6).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("FACULTY REMUNERATION VOUCHER").FontSize(12).Bold().FontColor("#102A43");
                        c.Item().Text("Official Statement of Teaching Remuneration & Class Conduction").FontSize(7.5f).FontColor("#64748B");
                    });
                    r.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text($"VOUCHER NO: {voucherNo}").FontSize(8f).Bold().FontColor("#164E80");
                        c.Item().Text($"DATE OF ISSUE: {DateTime.Now:dd MMMM yyyy}").FontSize(7.5f).FontColor("#334155");
                        c.Item().Text($"BILLING PERIOD: {monthStart:dd MMMM yyyy} – {monthEnd:dd MMMM yyyy}").FontSize(7.5f).Bold().FontColor("#047857");
                        c.Item().Text($"PAYMENT MODE: CASH").FontSize(7.5f).Bold().FontColor("#B45309");
                    });
                });
            });

            page.Content().PaddingVertical(8).Column(col =>
            {
                col.Item().Border(1).BorderColor("#CBD5E1").Background("#F8FAFC").Padding(8).Row(r =>
                {
                    r.RelativeItem().Column(c =>
                    {
                        c.Item().Text("FACULTY DETAILS").FontSize(7f).Bold().FontColor("#64748B");
                        c.Item().Text(faculty.FacultyName).FontSize(12).Bold().FontColor("#102A43");
                        c.Item().PaddingTop(2).Text($"Affiliation: {faculty.Organization}  |  Role: {faculty.Role}").FontSize(8f).FontColor("#334155");
                        if (!string.IsNullOrWhiteSpace(faculty.Designation))
                            c.Item().Text($"Designation: {faculty.Designation}").FontSize(7.5f).FontColor("#64748B");
                    });

                    r.ConstantItem(175).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                    {
                        c.Item().Text("CONFIGURED REMUNERATION RATES").FontSize(7f).Bold().FontColor("#64748B");
                        c.Item().PaddingTop(2).Row(rateRow =>
                        {
                            rateRow.RelativeItem().Text($"Theory Lecture:").FontSize(7.5f).FontColor("#475569");
                            rateRow.AutoItem().Text($"INR {faculty.LectureRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                        });
                        c.Item().Row(rateRow =>
                        {
                            rateRow.RelativeItem().Text($"Practical Lab:").FontSize(7.5f).FontColor("#475569");
                            rateRow.AutoItem().Text($"INR {faculty.PracticalRate:N0}").Bold().FontSize(8f).FontColor("#164E80");
                        });
                        c.Item().Row(rateRow =>
                        {
                            rateRow.RelativeItem().Text($"Disbursement Mode:").FontSize(7.5f).FontColor("#475569");
                            rateRow.AutoItem().Text($"Cash").Bold().FontSize(8f).FontColor("#B45309");
                        });
                    });
                });

                col.Item().PaddingTop(8).PaddingBottom(3).Row(r =>
                {
                    r.RelativeItem().Text("ITEMIZED TEACHING CONDUCTION AUDIT").FontSize(8.5f).Bold().FontColor("#102A43");
                    r.RelativeItem().AlignRight().Text($"{faculty.MonthlySessions.Count} Sessions").FontSize(7.5f).FontColor("#64748B");
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(22);
                        columns.ConstantColumn(72);
                        columns.RelativeColumn(3);
                        columns.ConstantColumn(68);
                        columns.ConstantColumn(105);
                        columns.ConstantColumn(70);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("#").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#102A43").Padding(4).Text("Date").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#102A43").Padding(4).Text("Subject / Module").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Session Type").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#102A43").Padding(4).AlignCenter().Text("Timing / Duration").FontColor(Colors.White).Bold().FontSize(7.5f);
                        header.Cell().Background("#102A43").Padding(4).AlignRight().Text("Fee (INR)").FontColor(Colors.White).Bold().FontSize(7.5f);
                    });

                    if (faculty.MonthlySessions.Any())
                    {
                        int idx = 1;
                        foreach (var s in faculty.MonthlySessions)
                        {
                            var bg = idx % 2 == 1 ? "#FFFFFF" : "#F8FAFC";
                            table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(idx.ToString()).FontSize(7.5f);
                            table.Cell().Background(bg).Padding(3.5f).Text(s.Date.ToString("ddd, dd MMM")).FontSize(7.5f).Bold();
                            table.Cell().Background(bg).Padding(3.5f).Column(c =>
                            {
                                c.Item().Text(s.CourseName).FontSize(7.5f);
                                c.Item().Text(s.CourseCode).FontSize(6.5f).FontColor("#64748B");
                            });
                            table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text(s.SessionType).FontSize(7.5f);
                            table.Cell().Background(bg).Padding(3.5f).AlignCenter().Text($"{FormatTimePdf(s.StartTime)}–{FormatTimePdf(s.EndTime)} ({s.DurationHours:0.#}h)").FontSize(7f);
                            table.Cell().Background(bg).Padding(3.5f).AlignRight().Text($"INR {s.Amount:N0}").Bold().FontSize(7.5f).FontColor("#102A43");
                            idx++;
                        }
                    }
                    else
                    {
                        table.Cell().ColumnSpan(6).Background("#F8FAFC").Padding(7).AlignCenter().Text("No conducted sessions recorded for this billing cycle.").FontSize(7.5f).Italic().FontColor("#64748B");
                    }
                });

                col.Item().ShowEntire().Column(botCol =>
                {
                    botCol.Item().PaddingTop(8).Border(1).BorderColor("#CBD5E1").Background("#FFFFFF").Padding(7).Row(r =>
                    {
                        r.RelativeItem(2).Column(c =>
                        {
                            c.Item().Text("COMPENSATION SUMMARY MATRIX").FontSize(8f).Bold().FontColor("#102A43");
                            c.Item().PaddingTop(2).Text($"• Theory Lectures: {faculty.MonthlyLectureCount} conducted × INR {faculty.LectureRate:N0} = INR {(faculty.MonthlyLectureCount * faculty.LectureRate):N0}").FontSize(7.5f).FontColor("#334155");
                            c.Item().Text($"• Practical Labs:   {faculty.MonthlyPracticalCount} conducted × INR {faculty.PracticalRate:N0} = INR {(faculty.MonthlyPracticalCount * faculty.PracticalRate):N0}").FontSize(7.5f).FontColor("#334155");
                            c.Item().PaddingTop(3).Text($"Amount in Words: {amountInWords}").FontSize(7.5f).Bold().FontColor("#164E80");
                        });

                        r.ConstantItem(165).BorderLeft(1).BorderColor("#E2E8F0").PaddingLeft(8).Column(c =>
                        {
                            c.Item().Text("NET REMUNERATION PAYABLE").FontSize(7f).Bold().FontColor("#047857");
                            c.Item().Text($"INR {faculty.MonthlyPayableAmount:N0}").FontSize(15).Bold().FontColor("#047857");
                            c.Item().PaddingTop(2).Border(1).BorderColor("#FDE68A").Background("#FFFBEB").Padding(2.5f).AlignCenter().Text("REMUNERATION STATEMENT · NOT A RECEIPT").FontSize(6.5f).Bold().FontColor("#92400E");
                        });
                    });

                    botCol.Item().PaddingTop(6).Border(1).BorderColor("#E2E8F0").Background("#F8FAFC").Padding(5).Row(r =>
                    {
                        r.RelativeItem().Text("CERTIFICATION: This system-generated voucher certifies that the above sessions were duly conducted by the faculty member and approved under academic guidelines. This statement shows calculated remuneration and does not certify payment or settlement.").FontSize(6.5f).FontColor("#64748B");
                    });

                    botCol.Item().PaddingTop(18).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(24);
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).Text("Authorized Signatory").FontSize(8f).Bold().FontColor("#102A43");
                            c.Item().Text("NIRVAA SOLUTIONS").FontSize(7f).FontColor("#64748B");
                        });

                        r.ConstantItem(80);

                        r.RelativeItem().Column(c =>
                        {
                            c.Item().PaddingBottom(24);
                            c.Item().LineHorizontal(1).LineColor("#94A3B8");
                            c.Item().PaddingTop(2).Text("Faculty Signature").FontSize(8f).Bold().FontColor("#102A43");
                            c.Item().Text("Faculty review acknowledgement (not proof of payment)").FontSize(7f).FontColor("#64748B");
                        });
                    });
                });
            });

            page.Footer().PaddingTop(4).Row(r =>
            {
                r.RelativeItem().Text("DCMS — Degree Class Management System  |  Confidential Financial Record").FontSize(6.5f).FontColor("#94A3B8");
                r.RelativeItem().AlignRight().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });
    }

    private static string ShortenCourseName(string name)
    {
        if (name.Contains("Cloud Computing", StringComparison.OrdinalIgnoreCase)) return "Adv. Cloud Computing";
        if (name.Contains("Project Lab", StringComparison.OrdinalIgnoreCase)) return "Project Lab-I";
        if (name.Contains("Mind", StringComparison.OrdinalIgnoreCase)) return "Mind & Consciousness";
        if (name.Contains("Research Methodology", StringComparison.OrdinalIgnoreCase)) return "Research Methodology";
        if (name.Contains("Mathematics", StringComparison.OrdinalIgnoreCase)) return "Advanced Mathematics";
        if (name.Contains("Data Centre and Cloud", StringComparison.OrdinalIgnoreCase)) return "Data Centre & Cloud";
        return name.Length > 28 ? name[..28].Trim() + "..." : name;
    }

    private static string FormatScheduleSummary(List<Session> sessions)
    {
        var days = sessions.Select(s => s.Date.DayOfWeek).Distinct().OrderBy(d => d).ToList();
        var lecCount = sessions.Count(s => s.SessionType is "Lecture" or "Tutorial");
        var pracCount = sessions.Count - lecCount;
        var lecHours = sessions.Where(s => s.SessionType is "Lecture" or "Tutorial").Sum(s => s.DurationHours);
        var pracHours = sessions.Where(s => s.SessionType is not ("Lecture" or "Tutorial")).Sum(s => s.DurationHours);

        if (days.Count == 1)
        {
            var dayName = days[0].ToString();
            if (lecHours > 0 && pracHours > 0)
                return $"{dayName} ({lecHours:0.#}h Theory + {pracHours:0.#}h Lab)";
            if (pracHours > 0)
                return $"{dayName} Practical Lab";
            return $"{dayName} Lectures";
        }

        if (days.Contains(DayOfWeek.Saturday) && days.Contains(DayOfWeek.Sunday) && days.Count == 2)
        {
            if (lecHours > 0 && pracHours == 0) return "Saturday & Sunday Lectures";
            if (pracHours > 0 && lecHours == 0) return "Weekend Practical Labs";
            return "Saturday & Sunday Sessions";
        }

        if (days.Contains(DayOfWeek.Friday))
        {
            if (sessions.Any(s => s.SessionType == "Tutorial")) return "Friday & Weekend Lectures / Tut";
            return "Friday & Weekend Sessions";
        }

        return $"{string.Join(" & ", days.Select(d => d.ToString()))} Classes";
    }

    private static string FormatTimePdf(TimeSpan time) => DateTime.Today.Add(time).ToString("h:mm tt");

    public static string NumberToWordsINR(decimal number)
    {
        long n = (long)Math.Floor(number);
        if (n == 0) return "Zero Rupees Only";

        var words = ConvertNumberToWordsINR(n);
        return $"{words} Rupees Only";
    }

    private static string ConvertNumberToWordsINR(long number)
    {
        if (number == 0) return "Zero";
        if (number < 0) return "Minus " + ConvertNumberToWordsINR(Math.Abs(number));

        string words = "";

        if ((number / 10000000) > 0)
        {
            words += ConvertNumberToWordsINR(number / 10000000) + " Crore ";
            number %= 10000000;
        }

        if ((number / 100000) > 0)
        {
            words += ConvertNumberToWordsINR(number / 100000) + " Lakh ";
            number %= 100000;
        }

        if ((number / 1000) > 0)
        {
            words += ConvertNumberToWordsINR(number / 1000) + " Thousand ";
            number %= 1000;
        }

        if ((number / 100) > 0)
        {
            words += ConvertNumberToWordsINR(number / 100) + " Hundred ";
            number %= 100;
        }

        if (number > 0)
        {
            if (words != "") words += "and ";

            var unitsMap = new[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen" };
            var tensMap = new[] { "Zero", "Ten", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety" };

            if (number < 20)
                words += unitsMap[number];
            else
            {
                words += tensMap[number / 10];
                if ((number % 10) > 0)
                    words += " " + unitsMap[number % 10];
            }
        }

        return words.Trim();
    }

    private static decimal GetSessionSingleRate(Session session, IEnumerable<FacultyRate> rates)
    {
        var rate = rates
            .Where(item => item.EffectiveFrom <= session.Date.Date
                && (item.EffectiveTo == null || item.EffectiveTo > session.Date.Date))
            .OrderByDescending(item => item.EffectiveFrom)
            .FirstOrDefault();
        var lectureRate = rate is null
            ? FacultyService.DefaultClassRateINR
            : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
        var practicalRate = rate is null
            ? FacultyService.DefaultClassRateINR
            : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;

        return session.SessionType is "Lecture" or "Tutorial" ? lectureRate : practicalRate;
    }

    public async Task<List<SessionPaymentAmount>> GetSessionPaymentAmountsAsync(DateTime date)
    {
        var sessions = await db.Sessions
            .Where(item => item.Date.Date == date.Date && item.Status != SessionStatus.Cancelled)
            .Include(item => item.Faculty)
                .ThenInclude(faculty => faculty.Rates)
            .ToListAsync();

        return sessions.Select(session =>
        {
            var rate = session.Faculty.Rates
                .Where(item => item.EffectiveFrom <= session.Date.Date
                    && (item.EffectiveTo == null || item.EffectiveTo > session.Date.Date))
                .OrderByDescending(item => item.EffectiveFrom)
                .FirstOrDefault();
            var lectureRate = rate is null
                ? FacultyService.DefaultClassRateINR
                : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
            var practicalRate = rate is null
                ? FacultyService.DefaultClassRateINR
                : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;
            var amount = session.SessionType is "Lecture" or "Tutorial" ? lectureRate : practicalRate;

            return new SessionPaymentAmount(
                session.Id,
                session.FacultyId,
                session.Faculty.FullName,
                session.SessionType,
                amount);
        }).ToList();
    }

    public async Task ApprovePeriodAsync(int periodId)
    {
        var period = await db.PaymentPeriods.FindAsync(periodId);
        if (period is not null)
        {
            if (period.Status != PaymentPeriodStatus.Calculated) throw new InvalidOperationException("Only calculated payroll can be approved.");
            period.Status = PaymentPeriodStatus.Approved;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkAsPaidAsync(int periodId)
    {
        var period = await db.PaymentPeriods.FindAsync(periodId);
        if (period is not null)
        {
            if (period.Status != PaymentPeriodStatus.Approved) throw new InvalidOperationException("Approve payroll before recording payment.");
            var paidAt = DateTime.UtcNow;
            var payableLineItems = await db.PaymentLineItems
                .Where(item => item.PaymentPeriodId == periodId && item.NetPayable > 0 && !item.IsPaid)
                .ToListAsync();

            foreach (var lineItem in payableLineItems)
            {
                var paymentPeriod = await db.PaymentPeriods.FindAsync(lineItem.PaymentPeriodId);
        if (paymentPeriod?.Status != PaymentPeriodStatus.Approved) throw new InvalidOperationException("Approve payroll before recording payment.");
        lineItem.IsPaid = true;
                lineItem.PaidAt = paidAt;
            }

            period.Status = PaymentPeriodStatus.Paid;
            await db.SaveChangesAsync();
        }
    }

    public async Task MarkLineItemAsPaidAsync(int lineItemId)
    {
        var lineItem = await db.PaymentLineItems.FindAsync(lineItemId);
        if (lineItem is null || lineItem.NetPayable <= 0 || lineItem.IsPaid) return;

        var paymentPeriod = await db.PaymentPeriods.FindAsync(lineItem.PaymentPeriodId);
        if (paymentPeriod?.Status != PaymentPeriodStatus.Approved) throw new InvalidOperationException("Approve payroll before recording payment.");
        lineItem.IsPaid = true;
        lineItem.PaidAt = DateTime.UtcNow;

        var hasOutstandingPayments = await db.PaymentLineItems.AnyAsync(item =>
            item.Id != lineItem.Id && item.PaymentPeriodId == lineItem.PaymentPeriodId &&
            item.NetPayable > 0 &&
            !item.IsPaid);

        if (!hasOutstandingPayments)
        {
            var period = await db.PaymentPeriods.FindAsync(lineItem.PaymentPeriodId);
            if (period is not null)
                period.Status = PaymentPeriodStatus.Paid;
        }

        await db.SaveChangesAsync();
    }

    public async Task AddAdjustmentAsync(PaymentAdjustment adjustment)
    {
        var period = await db.PaymentPeriods.FindAsync(adjustment.PaymentPeriodId);
        if (period is null || period.Status is PaymentPeriodStatus.Approved or PaymentPeriodStatus.Paid) throw new InvalidOperationException("Adjustments require an open or calculated period.");
        if (adjustment.Amount <= 0) throw new InvalidOperationException("Adjustment amount must be positive.");
        period.Status = PaymentPeriodStatus.Open;
        db.PaymentAdjustments.Add(adjustment);
        await db.SaveChangesAsync();
    }

    public async Task<List<PaymentAdjustment>> GetAdjustmentsAsync(int periodId)
        => await db.PaymentAdjustments
            .Include(a => a.Faculty)
            .Where(a => a.PaymentPeriodId == periodId)
            .ToListAsync();

    private static int CountOccurrences(DateTime startDate, DateTime endDate, DayOfWeek day)
    {
        var start = startDate.Date;
        var end = endDate.Date;
        if (end < start) return 0;

        var daysUntilFirst = ((int)day - (int)start.DayOfWeek + 7) % 7;
        var firstDate = start.AddDays(daysUntilFirst);
        return firstDate > end ? 0 : 1 + (end - firstDate).Days / 7;
    }

    private static int CountLectures(IEnumerable<Session> sessions) =>
        sessions.Count(session => session.SessionType is "Lecture" or "Tutorial");

    private static int CountPracticals(IEnumerable<Session> sessions) =>
        sessions.Count() - CountLectures(sessions);

    internal static decimal CalculatePay(IEnumerable<Session> sessions, IEnumerable<FacultyRate> rates) =>
        sessions.Sum(session =>
        {
            var rate = rates
                .Where(item => item.EffectiveFrom <= session.Date.Date
                    && (item.EffectiveTo == null || item.EffectiveTo > session.Date.Date))
                .OrderByDescending(item => item.EffectiveFrom)
                .FirstOrDefault();
            var lectureRate = rate is null
                ? FacultyService.DefaultClassRateINR
                : rate.LectureRateINR > 0 ? rate.LectureRateINR : rate.HourlyRateINR;
            var practicalRate = rate is null
                ? FacultyService.DefaultClassRateINR
                : rate.PracticalRateINR > 0 ? rate.PracticalRateINR : rate.HourlyRateINR;

            return session.SessionType is "Lecture" or "Tutorial" ? lectureRate : practicalRate;
        });

    private sealed record TimetablePlanItem(DayOfWeek Day, string FacultyName, string ClassType, int UnitsPerDay);

    // This mirrors the Friday–Sunday schedule shown on the Timetable page.
    // A practical block, however long, is one practical payment unit; theory/tutorial slots are paid per lecture.
    private static readonly IReadOnlyList<TimetablePlanItem> TimetablePaymentPlan = new List<TimetablePlanItem>
    {
        new(DayOfWeek.Friday, "Dr. Ganesh Birajdar", "Lecture", 3),
        new(DayOfWeek.Friday, "Dr. Vitthal Gutte", "Lecture", 1),
        new(DayOfWeek.Friday, "Dr. M. D. Hambarde", "Lecture", 1),
        new(DayOfWeek.Friday, "Dr. Vivekanand M Bankolli", "Lecture", 3),
        new(DayOfWeek.Friday, "Mr. Siddu Patil", "Lecture", 3),
        new(DayOfWeek.Friday, "Mr. Abhishek Joshi", "Practical", 1),

        new(DayOfWeek.Saturday, "Mr. Yatharth Verma", "Practical", 1),
        new(DayOfWeek.Saturday, "Dr. Jagdish Shinde", "Practical", 1),
        new(DayOfWeek.Saturday, "Mr. Shashidhar Ramesh", "Lecture", 1),
        new(DayOfWeek.Saturday, "Dr. Jagdish Shinde", "Lecture", 1),
        new(DayOfWeek.Saturday, "Dr. Vitthal Gutte", "Lecture", 1),
        new(DayOfWeek.Saturday, "Dr. M. D. Hambarde", "Lecture", 1),
        new(DayOfWeek.Saturday, "Dr. Ganesh Birajdar", "Lecture", 1),
        new(DayOfWeek.Saturday, "Mr. Subodh B Patil", "Practical", 1),
        new(DayOfWeek.Saturday, "Mr. Abhishek Joshi", "Practical", 1),

        new(DayOfWeek.Sunday, "Mr. Yatharth Verma", "Practical", 1),
        new(DayOfWeek.Sunday, "Dr. Jagdish Shinde", "Practical", 1),
        new(DayOfWeek.Sunday, "Mr. Shashidhar Ramesh", "Lecture", 1),
        new(DayOfWeek.Sunday, "Dr. Jagdish Shinde", "Lecture", 1),
        new(DayOfWeek.Sunday, "Dr. Vitthal Gutte", "Lecture", 2),
        new(DayOfWeek.Sunday, "Dr. M. D. Hambarde", "Lecture", 2),
        new(DayOfWeek.Sunday, "Mr. Vivekanand P Navadagi", "Lecture", 3),
        new(DayOfWeek.Sunday, "Mr. Vivekanand P Navadagi", "Practical", 1)
    };
}

public sealed record MonthlyPaymentPreview(
    int FacultyId,
    string FacultyName,
    int LectureCount,
    int PracticalCount,
    decimal LectureRate,
    decimal PracticalRate,
    decimal PayableAmount);

public sealed record TimetablePaymentForecast(
    string FacultyName,
    int WeeklyLectures,
    int WeeklyPracticals,
    int MonthlyLectures,
    int MonthlyPracticals,
    decimal LectureRate,
    decimal PracticalRate,
    decimal ExpectedAmount,
    bool HasRate);

public sealed record FacultyPaymentOverview(
    int FacultyId,
    string FacultyName,
    string Organization,
    string Role,
    string? Designation,
    decimal LectureRate,
    decimal PracticalRate,
    int WeeklyLectureCount,
    int WeeklyPracticalCount,
    decimal WeeklyPayableAmount,
    int MonthlyLectureCount,
    int MonthlyPracticalCount,
    decimal MonthlyPayableAmount,
    List<FacultyConductedSessionItem> WeeklySessions,
    List<FacultyConductedSessionItem> MonthlySessions);

public sealed record FacultyConductedSessionItem(
    int SessionId,
    DateTime Date,
    string CourseCode,
    string CourseName,
    string SessionType,
    TimeSpan StartTime,
    TimeSpan EndTime,
    decimal DurationHours,
    decimal Amount);

public sealed record SessionPaymentAmount(
    int SessionId,
    int FacultyId,
    string FacultyName,
    string SessionType,
    decimal Amount);

public sealed record ExecutiveSanctionData(
    string ReferenceNumber,
    DateTime MemoDate,
    string CycleTitle,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    string ToRecipient,
    string FromSender,
    string SubjectLine,
    string ExecutiveNarrative,
    List<ExecutiveSanctionFacultyItem> FacultyItems,
    decimal TotalHours,
    decimal TotalAmount,
    string AmountInWords,
    string PaymentMode,
    string EnclosuresText,
    List<ExecutiveSignatory> ApprovalMatrix);

public sealed record ExecutiveSanctionFacultyItem(
    int SrNo,
    string VoucherNo,
    int FacultyId,
    string FacultyName,
    string Organization,
    string DesignationOrRole,
    string SubjectAndCode,
    string ConductionSchedule,
    decimal Hours,
    decimal Rate,
    decimal PayableAmount);

public sealed record ExecutiveSignatory(
    int StepNumber,
    string StepTitle,
    string Name,
    string Designation,
    string Scope,
    string StatusText,
    string? ApprovedDate,
    string? SignaturePlaceholder);
