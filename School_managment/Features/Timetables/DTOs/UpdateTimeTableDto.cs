using System.ComponentModel.DataAnnotations;

namespace School_managment.Features.Timetables.DTOs
{
    public class UpdateTimeTableDto
    {
        [Required]
        public int Id { get; set; }

        [Required]
        public int ClassId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ScheduleId { get; set; }

        public string Constraints { get; set; } = "{}";

        public bool IsActive { get; set; }

        public List<UpdateTimetableSlotDto> TimetableSlots { get; set; } = new();
    }
}
