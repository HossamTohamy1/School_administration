using MediatR;
using School_managment.Features.Timetables.DTOs;

namespace School_managment.Features.Timetables.Commands
{
    public class UpdateTimeTableCommand : IRequest<TimeTableDto>
    {
        public UpdateTimeTableDto TimeTable { get; set; }

        public UpdateTimeTableCommand(UpdateTimeTableDto timeTable)
        {
            TimeTable = timeTable;
        }
    }
    
}
