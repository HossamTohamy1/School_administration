using School_managment.Features.Classes.Models;
using School_managment.Features.Subjects.Models;
using School_managment.Features.Teachers.Models;

namespace School_managment.Common.Models
{
    public class ClassSubject : BaseEntity
    {
        public int ClassId { get; set; }
        public Class Class { get; set; }

        public int SubjectId { get; set; }
        public Subject Subject { get; set; }
        public Teacher Teacher { get; set; }
        public int? TeacherId { get; set; }

    }
}
