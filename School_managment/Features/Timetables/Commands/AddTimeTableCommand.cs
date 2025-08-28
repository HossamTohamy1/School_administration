using MediatR;
using School_managment.Features.Timetables.DTOs;

namespace School_managment.Features.Timetables.Commands
{
    public class AddTimeTableCommand : IRequest<TimeTableDto>
    {
        public AddTimeTableDto TimeTable { get; set; }
        public int ClassId { get; set; }
        public string Name { get; set; }
        public List<AddTimetableSlotDto> Slots { get; set; }

        public AddTimeTableCommand(AddTimeTableDto timeTable)
        {
            TimeTable = timeTable;
        }
 
    }
}
