namespace School_managment.Features.Timetables.DTOs
{
    public class TimeTableDto
    {
        public int Id { get; set; }
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public string ScheduleId { get; set; }
        public DateTime GeneratedAt { get; set; }
        public bool IsActive { get; set; }
        public string Constraints { get; set; }

        public List<TimetableSlotDto> TimetableSlots { get; set; } = new();
    }
}
