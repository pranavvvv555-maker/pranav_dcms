using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class FacultyService(AppDbContext db)
{
    public const decimal DefaultClassRateINR = 1200m;

    public async Task<List<Faculty>> GetAllAsync()
        => await db.Faculties.OrderBy(f => f.FullName).ToListAsync();

    public async Task<Faculty?> GetByIdAsync(int id)
        => await db.Faculties.Include(f => f.Rates).FirstOrDefaultAsync(f => f.Id == id);

    public async Task<Faculty> CreateAsync(Faculty faculty)
    {
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        // Every new professor starts with the standard rate. It can be changed
        // at any time from Faculty & rates without affecting older records.
        await SetClassRatesAsync(
            faculty.Id,
            DefaultClassRateINR,
            DefaultClassRateINR,
            DateTime.Today,
            "Standard class fee");

        return faculty;
    }

    public async Task UpdateAsync(Faculty faculty)
    {
        db.Faculties.Update(faculty);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var faculty = await db.Faculties.FindAsync(id);
        if (faculty is not null)
        {
            db.Faculties.Remove(faculty);
            await db.SaveChangesAsync();
        }
    }

    public async Task SetClassRatesAsync(int facultyId, decimal lectureRate, decimal practicalRate,
        DateTime effectiveFrom, string? notes = null)
    {
        var effectiveDate = effectiveFrom.Date;
        var rateForThatDate = await db.FacultyRates
            .FirstOrDefaultAsync(rate => rate.FacultyId == facultyId && rate.EffectiveFrom == effectiveDate);

        if (rateForThatDate is not null)
        {
            rateForThatDate.HourlyRateINR = lectureRate;
            rateForThatDate.LectureRateINR = lectureRate;
            rateForThatDate.PracticalRateINR = practicalRate;
            rateForThatDate.Notes = notes;
            await db.SaveChangesAsync();
            return;
        }

        // Keep a rate history without overlapping periods. This also lets an
        // administrator schedule a different fee for a future date.
        var precedingRate = await db.FacultyRates
            .Where(rate => rate.FacultyId == facultyId
                && rate.EffectiveFrom < effectiveDate
                && (rate.EffectiveTo == null || rate.EffectiveTo > effectiveDate))
            .OrderByDescending(rate => rate.EffectiveFrom)
            .FirstOrDefaultAsync();
        var followingRate = await db.FacultyRates
            .Where(rate => rate.FacultyId == facultyId && rate.EffectiveFrom > effectiveDate)
            .OrderBy(rate => rate.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (precedingRate is not null)
            precedingRate.EffectiveTo = effectiveDate;

        db.FacultyRates.Add(new FacultyRate
        {
            FacultyId = facultyId,
            // Keeps historical data readable for older deployments.
            HourlyRateINR = lectureRate,
            LectureRateINR = lectureRate,
            PracticalRateINR = practicalRate,
            EffectiveFrom = effectiveDate,
            EffectiveTo = followingRate?.EffectiveFrom,
            Notes = notes
        });

        await db.SaveChangesAsync();
    }

    public async Task<FacultyRate?> GetCurrentClassRatesAsync(int facultyId)
    {
        var today = DateTime.Today;
        return await db.FacultyRates
            .Where(r => r.FacultyId == facultyId
                && r.EffectiveFrom <= today
                && (r.EffectiveTo == null || r.EffectiveTo > today))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    public async Task<List<FacultyRate>> GetRateHistoryAsync(int facultyId)
        => await db.FacultyRates
            .Where(r => r.FacultyId == facultyId)
            .OrderByDescending(r => r.EffectiveFrom)
            .ToListAsync();
}
