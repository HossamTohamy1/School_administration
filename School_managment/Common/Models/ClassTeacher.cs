using School_managment.Features.Classes.Models;
using School_managment.Features.Teachers.Models;

namespace School_managment.Common.Models
{
    public class ClassTeacher : BaseEntity
    {
        public int ClassId { get; set; }
        public Class Class { get; set; }
        public string NameClass { get; set; }

        public int TeacherId { get; set; }
        public Teacher Teacher { get; set; }

        public string NameTeacher { get; set; }
    }
}
