namespace School_managment.Features.Teachers.DTOs
{
    public class AddTeacherDto
    {
        public string Name { get; set; }
        public string Subject { get; set; }
        public int WeeklyQuota { get; set; }
        public List<string> RestrictedPeriods { get; set; } = new List<string>();
        public List<int> SubjectIds { get; set; }

        public List<int> ClassIds { get; set; } = new List<int>();
    }
}
