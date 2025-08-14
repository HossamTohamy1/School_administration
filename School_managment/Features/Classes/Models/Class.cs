using School_managment.Common.Models;
using School_managment.Features.Subjects.Models;
using School_managment.Features.Teachers.Models;

namespace School_managment.Features.Classes.Models
{
    public class Class : BaseEntity
    {
        public string Grade { get; set; }
        public string Section { get; set; }


        public List<ClassSubject> ClassSubjects { get; set; } = new();


        public int TotalHours { get; set; }

        public List<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();
    }
}
