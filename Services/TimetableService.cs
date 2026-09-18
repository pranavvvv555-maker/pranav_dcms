using DCMSApp.Data;
using DCMSApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace DCMSApp.Services;

public class TimetableService(AppDbContext db)
{
    public async Task<List<TimetableSlot>> GetBySemesterAsync(int semesterId)
        => await db.TimetableSlots
            .Include(t => t.Course)
            .Include(t => t.Faculty)
            .Where(t => t.SemesterId == semesterId)
            .OrderBy(t => t.DayOfWeek)
            .ThenBy(t => t.SlotNumber)
            .ToListAsync();

    public async Task<TimetableSlot> CreateAsync(TimetableSlot slot)
    {
        db.TimetableSlots.Add(slot);
        await db.SaveChangesAsync();
        return slot;
    }

    public async Task UpdateAsync(TimetableSlot slot)
    {
        db.TimetableSlots.Update(slot);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var slot = await db.TimetableSlots.FindAsync(id);
        if (slot is not null)
        {
            db.TimetableSlots.Remove(slot);
            await db.SaveChangesAsync();
        }
    }
}
