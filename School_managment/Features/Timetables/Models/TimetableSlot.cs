using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.Models;
using School_managment.Features.Teachers.Models;
using System.Xml;

namespace School_managment.Features.Timetables.Models
{
    public class TimetableSlot
    {
        public int Id { get; set; }
        public int ClassId { get; set; }   

        public int TimetableId { get; set; }
        public int Period { get; set; } // 1..8
        public DayOfWeek DayOfWeek { get; set; }
        public int? SubjectId { get; set; }
        public int? TeacherId { get; set; }
        public DateTime CreatedAt { get; set; }

        public Class Class { get; set; }
        public Subject Subject { get; set; }
        public Teacher Teacher { get; set; }
        public TimeTable Timetable { get; set; }
    }
}
