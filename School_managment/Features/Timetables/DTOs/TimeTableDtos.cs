namespace School_managment.Features.Timetables.DTOs
{
    public class TimeTableDtos
    {
        public bool AvoidDoubleBooking { get; set; }
        public bool RespectRestrictedPeriods { get; set; }
        public int MaxHoursPerDay { get; set; }
        public bool SpreadSubjectsEvenly { get; set; }
    }
}
