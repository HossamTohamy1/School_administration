using School_managment.Common.Models;

namespace School_managment.Features.Subjects.DTOs
{
    public class SubjectDto
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public int HoursPerWeek { get; set; }
        public List<ClassSubject> ClassSubjects { get; set; } = new();

    }
}
