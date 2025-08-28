using System.ComponentModel.DataAnnotations;

namespace School_managment.Features.Timetables.DTOs
{
    public class AddTimetableSlotForAddSlotsDto
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        public int TimetableId { get; set; }

        [Required]
        [Range(1, 8)]
        public int Period { get; set; }

        [Required]
        public DayOfWeek DayOfWeek { get; set; }

        public int? SubjectId { get; set; }

        public int? TeacherId { get; set; }
    }
}
