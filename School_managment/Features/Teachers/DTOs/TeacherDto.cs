namespace School_managment.Features.Teachers.DTOs
{
    public class TeacherDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Subject { get; set; }
        public int WeeklyQuota { get; set; }
        public List<string> RestrictedPeriods { get; set; }
        public List<string> ClassNames { get; set; } = new List<string>();

    }
}
