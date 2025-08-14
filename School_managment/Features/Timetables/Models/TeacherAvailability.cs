using School_managment.Features.Teachers.Models;

namespace School_managment.Features.Timetables.Models
{
    public class TeacherAvailability
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public DayOfWeek DayOfWeek { get; set; }
        public int Period { get; set; }
        public bool IsAvailable { get; set; }

        public Teacher Teacher { get; set; }

    }
}
