using School_managment.Features.Subjects.DTOs;
using School_managment.Features.Teachers.DTOs;

namespace School_managment.Features.Classes.DTOs
{
    public class ClassDto
    {
        public int Id { get; set; }
        public string Grade { get; set; }
        public string Section { get; set; }
        public List<SimpleSubjectDto> Subjects { get; set; } = new List<SimpleSubjectDto>();
        public List<string> TeacherNames { get; set; } = new List<string>();
        public int TotalHours { get; set; }
    }
}
