using School_managment.Features.Classes.Models;

namespace School_managment.Features.Timetables.Models
{
    public class TimeTable
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ScheduleId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool IsActive { get; set; }
        public string Constraints { get; set; } // JSON string

        public Class Class { get; set; }
        public ICollection<TimetableSlot> TimetableSlots { get; set; } = new List<TimetableSlot>();
    }
}
