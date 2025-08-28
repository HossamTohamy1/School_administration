namespace School_managment.Features.Timetables.DTOs
{
    public class ClassSubjectWithTeacherDto
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public int? TeacherId { get; set; }
        public string TeacherName { get; set; }
        public int HoursPerWeek { get; set; }
        public bool IsAssigned { get; set; }
        public string SubjectColor { get; set; } // إضافة الـ color property
    }
}
