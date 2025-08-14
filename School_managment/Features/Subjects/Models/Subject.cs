using School_managment.Common.Models;

namespace School_managment.Features.Subjects.Models
{
    public class Subject : BaseEntity
    {
        public string Name { get; set; }
        public int HoursPerWeek { get; set; }
        public List<ClassSubject> ClassSubjects { get; set; } = new();

    }
}
