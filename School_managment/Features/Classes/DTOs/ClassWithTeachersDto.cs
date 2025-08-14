using School_managment.Features.Subjects.DTOs;

namespace School_managment.Features.Classes.DTOs
{
    public class ClassWithTeachersDto
    {
        public int Id { get; set; }
        public string Grade { get; set; }
        public string Section { get; set; }
        public List<SubjectDto> Subjects { get; set; } = new List<SubjectDto>();
        public List<string> TeacherNames { get; set; } = new List<string>();


        public int TotalHours { get; set; }
    }
}
