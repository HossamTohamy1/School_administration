using School_managment.Common.Models;
using School_managment.Features.Classes.Models;
using School_managment.Features.Users.Models;

namespace School_managment.Features.Teachers.Models
{
    public class Teacher : User
    {
        public string Subject { get; set; }
        public int WeeklyQuota { get; set; }
        public List<string> RestrictedPeriods { get; set; } = new List<string>();
        public List<ClassTeacher> ClassTeachers { get; set; } = new List<ClassTeacher>();

    }

}

