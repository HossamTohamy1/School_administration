namespace School_managment.Features.Timetables.DTOs
{
    public class TeacherAvailabilityDto
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
        public bool IsAvailable { get; set; }
    }
}
