namespace School_managment.Features.Timetables.DTOs
{
    public class TimetableSlotDto
    {
        public int Id { get; set; }
        public int Period { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int? SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int? TeacherId { get; set; }
        public string TeacherName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
