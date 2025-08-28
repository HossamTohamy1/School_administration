using System.ComponentModel.DataAnnotations;

namespace School_managment.Features.Timetables.DTOs
{
    public class AddTimeTableDto
    {
        [Required]
        public int ClassId { get; set; }

        [Required]
        [MaxLength(100)]
        public string ScheduleId { get; set; }

        public string Constraints { get; set; } = "{}";

        public List<AddTimetableSlotDto> TimetableSlots { get; set; } = new();
    
    }
}
