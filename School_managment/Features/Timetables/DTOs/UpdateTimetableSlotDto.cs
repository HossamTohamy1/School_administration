using System.ComponentModel.DataAnnotations;

namespace School_managment.Features.Timetables.DTOs
{
    public class UpdateTimetableSlotDto
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 8)]
        public int Period { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public int? SubjectId { get; set; }

        public int? TeacherId { get; set; }
    }
}
