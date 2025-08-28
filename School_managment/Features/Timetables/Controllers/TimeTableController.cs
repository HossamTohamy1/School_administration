using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using School_managment.Features.Timetables.Commands;
using School_managment.Features.Timetables.DTOs;
using School_managment.Features.Timetables.Models;
using School_managment.Features.Timetables.Orchestrators;
using School_managment.Features.Timetables.Queries;
using School_managment.Infrastructure;

namespace School_managment.Features.Timetables.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TimeTableController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ITimeTableOrchestrator _orchestrator;
        private readonly SchoolDbContext _context;

        public TimeTableController(IMediator mediator, ITimeTableOrchestrator orchestrator, SchoolDbContext context)
        {
            _mediator = mediator;
            _orchestrator = orchestrator;
            _context = context;
        }

        #region Basic CRUD Operations

        /// <summary>
        /// Get all timetables with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<TimeTableDto>>> GetAll(
            [FromQuery] int? classId = null,
            [FromQuery] bool? isActive = null)
        {
            var query = new GetAllTimeTableQuery(classId, isActive);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get a specific timetable by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TimeTableDto>> GetById(int id)
        {
            try
            {
                var query = new GetByIdTimeTableQuery(id);
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
        }

        /// <summary>
        /// Create a new timetable
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TimeTableDto>> Create([FromBody] AddTimeTableDto timeTable)
        {
            try
            {
                var command = new AddTimeTableCommand(timeTable);
                var result = await _mediator.Send(command);
                return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Update an existing timetable
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TimeTableDto>> Update(int id, [FromBody] UpdateTimeTableDto timeTable)
        {
            if (id != timeTable.Id)
                return BadRequest("ID mismatch between route and body.");

            try
            {
                var command = new UpdateTimeTableCommand(timeTable);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Delete a timetable
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var command = new DeleteTimeTableCommand(id);
            var result = await _mediator.Send(command);

            if (!result)
                return NotFound($"TimeTable with ID {id} not found.");

            return NoContent();
        }

        #endregion

        #region Enhanced Smart Timetable Generation

        /// <summary>
        /// Legacy smart timetable generation (kept for backward compatibility)
        /// </summary>
        [HttpPost("generate/{classId}")]
        public async Task<ActionResult<TimeTableDto>> GenerateSmartTimetable(
            int classId,
            [FromBody] SmartTimetableRequest request)
        {
            try
            {
                var result = await _orchestrator.GenerateSmartTimetableAsync(classId, request);

                if (!result.Success)
                {
                    return BadRequest(result.ErrorMessage);
                }

                return CreatedAtAction(nameof(GetById), new { id = result.TimetableDto.Id }, result.TimetableDto);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Enhanced smart timetable generation with comprehensive conflict prevention
        /// </summary>
        [HttpPost("generate-smart/{classId}")]
        public async Task<ActionResult<SmartTimetableGenerationResult>> GenerateEnhancedSmartTimetable(
            int classId,
            [FromBody] SmartTimetableRequest request)
        {
            try
            {
                // Get conflict prevention suggestions first
                var suggestions = await _orchestrator.GetConflictPreventionSuggestionsAsync(classId);

                // Check for critical issues that should prevent generation
                var criticalIssues = suggestions.Where(s => s.Priority == "High").ToList();
                if (criticalIssues.Any())
                {
                    return BadRequest(new
                    {
                        message = "Critical issues found that may affect timetable generation",
                        suggestions = criticalIssues,
                        canProceed = false
                    });
                }

                var result = await _orchestrator.GenerateSmartTimetableAsync(classId, request);

                if (!result.Success)
                {
                    return BadRequest(new
                    {
                        message = result.ErrorMessage,
                        suggestions = suggestions.Where(s => s.Priority == "Medium").ToList()
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating smart timetable: {ex.Message}");
            }
        }

        #endregion

        #region Smart Assignment and Availability

        /// <summary>
        /// Get available teachers for a specific time slot with detailed availability info
        /// </summary>
        [HttpGet("available-teachers/{classId}")]
        public async Task<ActionResult<List<AvailableTeacherDto>>> GetAvailableTeachersForSlot(
            int classId,
            [FromQuery] DayOfWeek dayOfWeek,
            [FromQuery] int period)
        {
            try
            {
                if (period < 1 || period > 8)
                    return BadRequest("Period must be between 1 and 8");

                var availableTeachers = await _orchestrator.GetAvailableTeachersForSlotAsync(classId, dayOfWeek, period);
                return Ok(availableTeachers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting available teachers: {ex.Message}");
            }
        }

        /// <summary>
        /// Assign teacher to slot with comprehensive validation
        /// </summary>
        [HttpPost("assign-teacher-to-slot")]
        public async Task<ActionResult<SlotAssignmentResult>> AssignTeacherToSlot([FromBody] SlotAssignmentRequest request)
        {
            try
            {
                // Validate request
                if (request.TeacherId <= 0 || request.ClassId <= 0 || request.SubjectId <= 0)
                    return BadRequest("Invalid teacher, class, or subject ID");

                if (request.Period < 1 || request.Period > 8)
                    return BadRequest("Period must be between 1 and 8");

                var result = await _orchestrator.AssignTeacherToSlotAsync(request);

                if (!result.Success)
                {
                    if (result.ConflictDetails != null)
                    {
                        return Conflict(result); // Return 409 Conflict with details
                    }
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error assigning teacher to slot: {ex.Message}");
            }
        }

        /// <summary>
        /// Legacy assign teacher method (kept for backward compatibility)
        /// </summary>
        [HttpPost("assign-teacher")]
        public async Task<IActionResult> AssignTeacherToSlot([FromBody] AssignTeacherDto dto)
        {
            // Convert legacy DTO to new request format
            var request = new SlotAssignmentRequest
            {
                TimetableId = 0, // Will need to be handled differently
                ClassId = dto.ClassId,
                TeacherId = dto.TeacherId,
                SubjectId = dto.SubjectId,
                DayOfWeek = dto.DayOfWeek,
                Period = dto.Period
            };

            var result = await AssignTeacherToSlot(request);
            return result.Result ?? Ok(new { message = "Teacher assigned successfully" });
        }

        #endregion

        #region Conflict Prevention and Validation

        /// <summary>
        /// Get conflict prevention suggestions for a class before timetable generation
        /// </summary>
        [HttpGet("class/{classId}/conflict-prevention-suggestions")]
        public async Task<ActionResult<List<ConflictPreventionSuggestion>>> GetConflictPreventionSuggestions(int classId)
        {
            try
            {
                var suggestions = await _orchestrator.GetConflictPreventionSuggestionsAsync(classId);
                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting suggestions: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate class readiness for timetable generation
        /// </summary>
        [HttpGet("class/{classId}/generation-readiness")]
        public async Task<ActionResult<ClassGenerationReadiness>> CheckGenerationReadiness(int classId)
        {
            try
            {
                var suggestions = await _orchestrator.GetConflictPreventionSuggestionsAsync(classId);

                // Get class assignment status
                var assignmentStatus = await GetClassAssignmentStatusInternal(classId);

                var readiness = new ClassGenerationReadiness
                {
                    ClassId = classId,
                    IsReady = !suggestions.Any(s => s.Priority == "High"),
                    ReadinessScore = CalculateReadinessScore(suggestions, assignmentStatus),
                    CriticalIssues = suggestions.Where(s => s.Priority == "High").ToList(),
                    Warnings = suggestions.Where(s => s.Priority == "Medium").ToList(),
                    AssignmentStatus = assignmentStatus,
                    Recommendations = GenerateReadinessRecommendations(suggestions, assignmentStatus)
                };

                return Ok(readiness);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking generation readiness: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate a timetable for conflicts
        /// </summary>
        [HttpGet("{id}/validate")]
        public async Task<ActionResult<List<ConflictDto>>> ValidateTimetable(int id)
        {
            try
            {
                var result = await _orchestrator.ValidateTimetableAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
        }

        /// <summary>
        /// Check teacher conflict - Enhanced with comprehensive checks
        /// </summary>
        [HttpPost("check-teacher-conflict")]
        public async Task<ActionResult<TeacherConflictResult>> CheckTeacherConflict([FromBody] CheckTeacherConflictDto dto)
        {
            try
            {
                // Validate input data
                if (dto.TeacherId <= 0)
                    return BadRequest("Invalid teacher ID");

                if (dto.ClassId <= 0)
                    return BadRequest("Invalid class ID");

                if (dto.Period < 1 || dto.Period > 8)
                    return BadRequest("Period must be between 1 and 8");

                var result = await _orchestrator.CheckTeacherConflictAsync(
                    dto.TeacherId,
                    dto.DayOfWeek,
                    dto.Period,
                    dto.ClassId
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking teacher conflict: {ex.Message}");
            }
        }

        /// <summary>
        /// Bulk check teacher conflicts for multiple slots
        /// </summary>
        [HttpPost("bulk-check-conflicts")]
        public async Task<ActionResult<BulkConflictCheckResult>> BulkCheckTeacherConflicts(
            [FromBody] BulkConflictCheckRequest request)
        {
            try
            {
                var result = new BulkConflictCheckResult
                {
                    SlotResults = new List<SlotConflictResult>()
                };

                foreach (var slot in request.SlotsToCheck)
                {
                    var conflictResult = await _orchestrator.CheckTeacherConflictAsync(
                        slot.TeacherId, slot.DayOfWeek, slot.Period, slot.ClassId);

                    result.SlotResults.Add(new SlotConflictResult
                    {
                        SlotRequest = slot,
                        ConflictResult = conflictResult
                    });
                }

                result.TotalChecked = result.SlotResults.Count;
                result.AvailableSlots = result.SlotResults.Count(r => r.ConflictResult.IsAvailable);
                result.ConflictingSlots = result.SlotResults.Count(r => !r.ConflictResult.IsAvailable);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error performing bulk conflict check: {ex.Message}");
            }
        }

        #endregion

        #region Conflict Resolution

        /// <summary>
        /// Resolve conflicts automatically - Enhanced
        /// </summary>
        [HttpPost("{id}/resolve-conflicts")]
        public async Task<ActionResult<ConflictResolutionResult>> ResolveConflictsAutomatically(int id)
        {
            try
            {
                var timetable = await _mediator.Send(new GetByIdTimeTableQuery(id));
                if (timetable == null)
                    return NotFound($"Timetable with ID {id} not found");

                var conflicts = await _orchestrator.ValidateTimetableAsync(id);
                if (!conflicts.Any())
                    return Ok(new ConflictResolutionResult
                    {
                        Success = true,
                        Message = "No conflicts found to resolve",
                        ResolvedCount = 0,
                        UnresolvedCount = 0
                    });

                var teacherConflicts = conflicts.Where(c =>
                    c.Type == "TeacherDoubleBooking" ||
                    c.Type == "TeacherCrossClassDoubleBooking"
                ).ToList();

                if (!teacherConflicts.Any())
                    return Ok(new ConflictResolutionResult
                    {
                        Success = true,
                        Message = "No teacher conflicts found to resolve",
                        ResolvedCount = 0,
                        UnresolvedCount = 0
                    });

                var resolutionResult = await _orchestrator.ResolveTeacherConflictsAsync(id, teacherConflicts);
                return Ok(resolutionResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error resolving conflicts: {ex.Message}");
            }
        }

        #endregion

        #region Statistics and Quality Analysis

        /// <summary>
        /// Get timetable statistics
        /// </summary>
        [HttpGet("{id}/statistics")]
        public async Task<ActionResult<TimetableStatisticsDto>> GetStatistics(int id)
        {
            try
            {
                var result = await _orchestrator.GetTimetableStatisticsAsync(id);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
        }

        /// <summary>
        /// Get comprehensive timetable quality report
        /// </summary>
        [HttpGet("{id}/quality-report")]
        public async Task<ActionResult<TimetableQualityReport>> GetTimetableQualityReport(int id)
        {
            try
            {
                var timetable = await _mediator.Send(new GetByIdTimeTableQuery(id));
                if (timetable == null)
                    return NotFound($"Timetable with ID {id} not found");

                var statistics = await _orchestrator.GetTimetableStatisticsAsync(id);
                var conflicts = await _orchestrator.ValidateTimetableAsync(id);

                var qualityReport = new TimetableQualityReport
                {
                    TimetableId = id,
                    OverallQualityScore = CalculateQualityScore(statistics, conflicts),
                    Statistics = statistics,
                    ConflictSummary = new ConflictSummary
                    {
                        TotalConflicts = conflicts.Count,
                        CriticalConflicts = conflicts.Count(c => c.Type.Contains("DoubleBooking")),
                        MinorConflicts = conflicts.Count(c => !c.Type.Contains("DoubleBooking")),
                        ConflictsByType = conflicts.GroupBy(c => c.Type).ToDictionary(g => g.Key, g => g.Count())
                    },
                    QualityMetrics = CalculateQualityMetrics(statistics, conflicts),
                    ImprovementSuggestions = GenerateImprovementSuggestions(statistics, conflicts)
                };

                return Ok(qualityReport);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating quality report: {ex.Message}");
            }
        }

        /// <summary>
        /// Get advanced statistics with conflict analysis
        /// </summary>
        [HttpGet("{id}/advanced-statistics")]
        public async Task<ActionResult<AdvancedTimetableStatistics>> GetAdvancedStatistics(int id)
        {
            try
            {
                var timetable = await _mediator.Send(new GetByIdTimeTableQuery(id));
                if (timetable == null)
                    return NotFound($"Timetable with ID {id} not found");

                var statistics = await _orchestrator.GetTimetableStatisticsAsync(id);
                var conflicts = await _orchestrator.ValidateTimetableAsync(id);

                var advancedStats = new AdvancedTimetableStatistics
                {
                    BasicStatistics = statistics,
                    ConflictAnalysis = new ConflictAnalysis
                    {
                        TotalConflicts = conflicts.Count,
                        TeacherConflicts = conflicts.Count(c => c.Type.Contains("Teacher")),
                        RestrictedPeriodConflicts = conflicts.Count(c => c.Type == "RestrictedPeriod"),
                        InvalidAssignmentConflicts = conflicts.Count(c => c.Type == "InvalidTeacherSubjectAssignment"),
                        ConflictsByDay = conflicts.GroupBy(c => c.DayOfWeek)
                            .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                        ConflictsByPeriod = conflicts.GroupBy(c => c.Period)
                            .ToDictionary(g => g.Key.ToString(), g => g.Count())
                    },
                    UtilizationAnalysis = new UtilizationAnalysis
                    {
                        TotalPossibleSlots = 5 * 8, // 5 days * 8 periods
                        UsedSlots = statistics.FilledSlots,
                        UtilizationPercentage = Math.Round((double)statistics.FilledSlots / (5 * 8) * 100, 2)
                    }
                };

                return Ok(advancedStats);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting advanced statistics: {ex.Message}");
            }
        }

        #endregion

        #region Optimization and Suggestions

        /// <summary>
        /// Get optimization suggestions for existing timetable
        /// </summary>
        [HttpGet("{id}/optimization-suggestions")]
        public async Task<ActionResult<List<OptimizationSuggestion>>> GetOptimizationSuggestions(int id)
        {
            try
            {
                var timetable = await _mediator.Send(new GetByIdTimeTableQuery(id));
                if (timetable == null)
                    return NotFound($"Timetable with ID {id} not found");

                var statistics = await _orchestrator.GetTimetableStatisticsAsync(id);
                var conflicts = await _orchestrator.ValidateTimetableAsync(id);

                var suggestions = new List<OptimizationSuggestion>();

                // Check for uneven daily distribution
                var dailyDistribution = statistics.DailyDistribution;
                if (dailyDistribution.Values.Any())
                {
                    var maxDaily = dailyDistribution.Values.Max();
                    var minDaily = dailyDistribution.Values.Min();

                    if (maxDaily - minDaily > 2)
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Type = "UnbalancedDailyDistribution",
                            Priority = "Medium",
                            Description = $"Daily class distribution is uneven (Max: {maxDaily}, Min: {minDaily})",
                            Recommendation = "Consider redistributing classes across days for better balance",
                            Impact = "Better student and teacher workload distribution"
                        });
                    }
                }

                // Check for teacher workload imbalance
                var teacherWorkload = statistics.TeacherWorkload;
                if (teacherWorkload.Values.Any())
                {
                    var maxWorkload = teacherWorkload.Values.Max();
                    var minWorkload = teacherWorkload.Values.Min();

                    if (maxWorkload - minWorkload > 3)
                    {
                        suggestions.Add(new OptimizationSuggestion
                        {
                            Type = "ImbalancedTeacherWorkload",
                            Priority = "Low",
                            Description = $"Teacher workload varies significantly (Max: {maxWorkload}, Min: {minWorkload})",
                            Recommendation = "Review teacher assignments to balance workload",
                            Impact = "Improved teacher satisfaction and reduced burnout risk"
                        });
                    }
                }

                // Check for conflicts that can be easily resolved
                var easyToResolveConflicts = conflicts.Where(c =>
                    c.Type == "TeacherDoubleBooking" ||
                    c.Type == "RestrictedPeriod").Count();

                if (easyToResolveConflicts > 0)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Type = "ResolvableConflicts",
                        Priority = "High",
                        Description = $"{easyToResolveConflicts} conflicts can be automatically resolved",
                        Recommendation = "Use the auto-resolve feature to fix these conflicts",
                        Impact = "Immediate conflict reduction and improved timetable quality"
                    });
                }

                // Check for empty slots that could be utilized
                if (statistics.EmptySlots > 5)
                {
                    suggestions.Add(new OptimizationSuggestion
                    {
                        Type = "UnderutilizedSlots",
                        Priority = "Low",
                        Description = $"{statistics.EmptySlots} empty slots could be better utilized",
                        Recommendation = "Consider adding study halls, extra-curricular activities, or additional subject hours",
                        Impact = "Better utilization of available time slots"
                    });
                }

                return Ok(suggestions);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating optimization suggestions: {ex.Message}");
            }
        }

        #endregion

        #region Slot Management

        /// <summary>
        /// Swap two time slots in a timetable
        /// </summary>
        [HttpPut("{id}/swap-slots")]
        public async Task<ActionResult> SwapSlots(int id, [FromBody] SwapSlotsRequest request)
        {
            try
            {
                var result = await _orchestrator.SwapTimetableSlotsAsync(id, request);
                if (!result)
                    return BadRequest("Unable to swap slots. Please check the slot positions.");

                return Ok(new { message = "Slots swapped successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
        }

        #endregion

        #region Status Management

        /// <summary>
        /// Activate/Deactivate a timetable
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<ActionResult<TimeTableDto>> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var timetable = await _mediator.Send(new GetByIdTimeTableQuery(id));

                var updateDto = new UpdateTimeTableDto
                {
                    Id = timetable.Id,
                    ClassId = timetable.ClassId,
                    ScheduleId = timetable.ScheduleId,
                    IsActive = request.IsActive,
                    Constraints = timetable.Constraints,
                    TimetableSlots = timetable.TimetableSlots.Select(ts => new UpdateTimetableSlotDto
                    {
                        Id = ts.Id,
                        Period = ts.Period,
                        DayOfWeek = ts.DayOfWeek,
                        SubjectId = ts.SubjectId,
                        TeacherId = ts.TeacherId
                    }).ToList()
                };

                var command = new UpdateTimeTableCommand(updateDto);
                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"TimeTable with ID {id} not found.");
            }
        }

        #endregion

        #region Class-Specific Operations

        /// <summary>
        /// Get timetables for a specific class
        /// </summary>
        [HttpGet("class/{classId}")]
        public async Task<ActionResult<List<TimeTableDto>>> GetByClass(int classId)
        {
            var query = new GetAllTimeTableQuery(classId);
            var result = await _mediator.Send(query);
            return Ok(result);
        }

        /// <summary>
        /// Get active timetable for a specific class
        /// </summary>
        [HttpGet("class/{classId}/active")]
        public async Task<ActionResult<TimeTableDto>> GetActiveByClass(int classId)
        {
            var query = new GetAllTimeTableQuery(classId, true);
            var result = await _mediator.Send(query);
            var activeTimetable = result.FirstOrDefault();

            if (activeTimetable == null)
                return NotFound($"No active timetable found for class with ID {classId}.");

            return Ok(activeTimetable);
        }

        /// <summary>
        /// Get assigned teachers and subjects for a specific class
        /// </summary>
        [HttpGet("class/{classId}/assigned-subjects")]
        public async Task<ActionResult<List<ClassSubjectWithTeacherDto>>> GetAssignedSubjectsForClass(int classId)
        {
            try
            {
                // First check if class exists
                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
                if (!classExists)
                {
                    return NotFound($"Class with ID {classId} not found.");
                }

                var classSubjects = await _context.ClassTeachers
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => new ClassSubjectWithTeacherDto
                    {
                        SubjectId = cs.SubjectId,
                        SubjectName = cs.Subject.Name,
                        TeacherId = cs.TeacherId,
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : "No Teacher Assigned",
                        HoursPerWeek = cs.Subject.HoursPerWeek,
                        IsAssigned = cs.TeacherId != null,
                        SubjectColor = GenerateSubjectColor(cs.Subject.Name)
                    })
                    .OrderBy(cs => cs.SubjectName)
                    .ToListAsync();

                return Ok(classSubjects);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving class subjects: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all assigned teachers for a specific class
        /// </summary>
        [HttpGet("class/{classId}/assigned-teachers")]
        public async Task<ActionResult<List<AssignedTeacherDto>>> GetAssignedTeachersForClass(int classId)
        {
            try
            {
                // Check if class exists
                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
                if (!classExists)
                {
                    return NotFound($"Class with ID {classId} not found.");
                }

                var assignedTeachers = await _context.ClassTeachers
                    .Include(ct => ct.Teacher)
                    .Include(ct => ct.Subject)
                    .Where(ct => ct.ClassId == classId && ct.TeacherId != null)
                    .GroupBy(ct => ct.TeacherId)
                    .Select(g => new AssignedTeacherDto
                    {
                        Id = g.Key, // TeacherId
                        Name = g.First().Teacher.Name,
                        Subject = g.First().Subject.Name, // First subject they teach
                        SubjectNames = g.Select(ct => ct.Subject.Name).ToList(),
                        SubjectCount = g.Count()
                    })
                    .ToListAsync();

                return Ok(assignedTeachers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving assigned teachers: {ex.Message}");
            }
        }

        /// <summary>
        /// Get class assignment status
        /// </summary>
        [HttpGet("class/{classId}/assignment-status")]
        public async Task<ActionResult<ClassAssignmentStatusDto>> GetClassAssignmentStatus(int classId)
        {
            try
            {
                var status = await GetClassAssignmentStatusInternal(classId);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking assignment status: {ex.Message}");
            }
        }

        /// <summary>
        /// Get subjects with teachers for a class
        /// </summary>
        [HttpGet("class/{classId}/subjects-with-teachers")]
        public async Task<ActionResult<List<ClassSubjectWithTeacherDto>>> GetSubjectsWithTeachersForClass(int classId)
        {
            try
            {
                // Check if class exists
                var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
                if (!classExists)
                {
                    return NotFound($"Class with ID {classId} not found.");
                }

                var classSubjectsWithTeachers = await _context.ClassTeachers
                    .Include(cs => cs.Subject)
                    .Include(cs => cs.Teacher)
                    .Where(cs => cs.ClassId == classId)
                    .Select(cs => new
                    {
                        cs.SubjectId,
                        SubjectName = cs.Subject.Name,
                        cs.TeacherId,
                        TeacherName = cs.Teacher != null ? cs.Teacher.Name : "No Teacher Assigned",
                        cs.Subject.HoursPerWeek,
                        IsAssigned = cs.TeacherId != null
                    })
                    .OrderBy(cs => cs.SubjectName)
                    .ToListAsync();

                var result = classSubjectsWithTeachers.Select(cs => new ClassSubjectWithTeacherDto
                {
                    SubjectId = cs.SubjectId,
                    SubjectName = cs.SubjectName,
                    TeacherId = cs.TeacherId,
                    TeacherName = cs.TeacherName,
                    HoursPerWeek = cs.HoursPerWeek,
                    IsAssigned = cs.IsAssigned,
                    SubjectColor = GenerateSubjectColor(cs.SubjectName)
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error retrieving subjects for class: {ex.Message}");
            }
        }

        #endregion

        #region Teacher Availability

        /// <summary>
        /// Get all available slots for a teacher across all days
        /// </summary>
        [HttpGet("teacher/{teacherId}/available-slots")]
        public async Task<ActionResult<TeacherAvailabilityMatrix>> GetTeacherAvailabilityMatrix(
            int teacherId,
            [FromQuery] int? excludeClassId = null)
        {
            try
            {
                var teacher = await _context.Teachers.FindAsync(teacherId);
                if (teacher == null)
                    return NotFound($"Teacher with ID {teacherId} not found");

                var matrix = new TeacherAvailabilityMatrix
                {
                    TeacherId = teacherId,
                    TeacherName = teacher.Name,
                    AvailabilityGrid = new Dictionary<string, Dictionary<int, SlotAvailabilityInfo>>()
                };

                var daysOfWeek = new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday };

                foreach (var day in daysOfWeek)
                {
                    matrix.AvailabilityGrid[day.ToString()] = new Dictionary<int, SlotAvailabilityInfo>();

                    for (int period = 1; period <= 8; period++)
                    {
                        var conflictResult = await _orchestrator.CheckTeacherConflictAsync(
                            teacherId, day, period, excludeClassId ?? 0);

                        matrix.AvailabilityGrid[day.ToString()][period] = new SlotAvailabilityInfo
                        {
                            IsAvailable = conflictResult.IsAvailable,
                            ConflictReason = conflictResult.ConflictMessage,
                            ConflictingClassName = conflictResult.ConflictingClassName,
                            ConflictingSubject = conflictResult.ConflictingSubjectName
                        };
                    }
                }

                return Ok(matrix);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error getting teacher availability matrix: {ex.Message}");
            }
        }

        [HttpDelete("slots/{slotId}")]
        public async Task<IActionResult> DeleteSlot(int slotId)
        {
            var slot = await _context.TimetableSlots.FindAsync(slotId);
            if (slot == null)
                return NotFound();

            _context.TimetableSlots.Remove(slot);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpPost("add-slot")]
        public async Task<IActionResult> AddSlot([FromBody] AddTimetableSlotForAddSlotsDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var slot = new TimetableSlot
            {
                ClassId = dto.ClassId,
                TimetableId = dto.TimetableId,
                Period = dto.Period,
                DayOfWeek = dto.DayOfWeek,
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId
            };

            _context.TimetableSlots.Add(slot);
            await _context.SaveChangesAsync();

            return Ok(slot);
        }




        #endregion

        #region Helper Methods

        /// <summary>
        /// Internal method to get class assignment status
        /// </summary>
        private async Task<ClassAssignmentStatusDto> GetClassAssignmentStatusInternal(int classId)
        {
            var classExists = await _context.Classes.AnyAsync(c => c.Id == classId);
            if (!classExists)
            {
                throw new KeyNotFoundException($"Class with ID {classId} not found.");
            }

            var classSubjects = await _context.ClassTeachers
                .Include(cs => cs.Subject)
                .Include(cs => cs.Teacher)
                .Where(cs => cs.ClassId == classId)
                .ToListAsync();

            var totalSubjects = classSubjects.Count;
            var assignedSubjects = classSubjects.Count(cs => cs.TeacherId != null);
            var unassignedSubjects = classSubjects.Where(cs => cs.TeacherId == null)
                .Select(cs => cs.Subject?.Name ?? "Unknown Subject")
                .ToList();

            return new ClassAssignmentStatusDto
            {
                ClassId = classId,
                TotalSubjects = totalSubjects,
                AssignedSubjects = assignedSubjects,
                UnassignedSubjects = unassignedSubjects,
                IsFullyAssigned = assignedSubjects == totalSubjects,
                CompletionPercentage = totalSubjects > 0 ? (double)assignedSubjects / totalSubjects * 100 : 0
            };
        }

        /// <summary>
        /// Calculate readiness score based on suggestions and assignment status
        /// </summary>
        private double CalculateReadinessScore(
            List<ConflictPreventionSuggestion> suggestions,
            ClassAssignmentStatusDto assignmentStatus)
        {
            double score = 100.0;

            // Deduct points for critical issues
            score -= suggestions.Count(s => s.Priority == "High") * 25;

            // Deduct points for medium issues
            score -= suggestions.Count(s => s.Priority == "Medium") * 10;

            // Factor in assignment completion
            score *= (assignmentStatus.CompletionPercentage / 100.0);

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Generate readiness recommendations
        /// </summary>
        private List<string> GenerateReadinessRecommendations(
            List<ConflictPreventionSuggestion> suggestions,
            ClassAssignmentStatusDto assignmentStatus)
        {
            var recommendations = new List<string>();

            if (!assignmentStatus.IsFullyAssigned)
            {
                recommendations.Add($"Assign teachers to {assignmentStatus.UnassignedSubjects.Count} unassigned subjects");
            }

            recommendations.AddRange(suggestions.Where(s => s.Priority == "High").Select(s => s.Recommendation));

            if (!recommendations.Any())
            {
                recommendations.Add("Class is ready for timetable generation");
            }

            return recommendations;
        }

        /// <summary>
        /// Calculate quality score for timetable
        /// </summary>
        private double CalculateQualityScore(TimetableStatisticsDto statistics, List<ConflictDto> conflicts)
        {
            double score = 100.0;

            // Deduct for conflicts
            score -= conflicts.Count(c => c.Type.Contains("DoubleBooking")) * 20; // Critical conflicts
            score -= conflicts.Count(c => !c.Type.Contains("DoubleBooking")) * 5; // Minor conflicts

            // Bonus for good utilization
            if (statistics.TotalSlots > 0)
            {
                double utilizationRate = (double)statistics.FilledSlots / statistics.TotalSlots;
                if (utilizationRate > 0.8) score += 5;
                if (utilizationRate < 0.5) score -= 10;
            }

            return Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Calculate quality metrics
        /// </summary>
        private QualityMetrics CalculateQualityMetrics(TimetableStatisticsDto statistics, List<ConflictDto> conflicts)
        {
            return new QualityMetrics
            {
                ConflictRate = statistics.TotalSlots > 0 ? (double)conflicts.Count / statistics.TotalSlots * 100 : 0,
                UtilizationRate = statistics.TotalSlots > 0 ? (double)statistics.FilledSlots / statistics.TotalSlots * 100 : 0,
                BalanceScore = CalculateBalanceScore(statistics),
                OverallRating = CalculateOverallRating(statistics, conflicts)
            };
        }

        /// <summary>
        /// Calculate balance score
        /// </summary>
        private double CalculateBalanceScore(TimetableStatisticsDto statistics)
        {
            double score = 100.0;

            // Check daily distribution balance
            if (statistics.DailyDistribution.Values.Any())
            {
                var max = statistics.DailyDistribution.Values.Max();
                var min = statistics.DailyDistribution.Values.Min();
                var variance = max - min;
                score -= variance * 5; // Deduct 5 points per period of variance
            }

            return Math.Max(0, score);
        }

        /// <summary>
        /// Calculate overall rating
        /// </summary>
        private string CalculateOverallRating(TimetableStatisticsDto statistics, List<ConflictDto> conflicts)
        {
            var qualityScore = CalculateQualityScore(statistics, conflicts);

            return qualityScore switch
            {
                >= 90 => "Excellent",
                >= 80 => "Good",
                >= 70 => "Fair",
                >= 60 => "Poor",
                _ => "Needs Improvement"
            };
        }

        /// <summary>
        /// Generate improvement suggestions
        /// </summary>
        private List<string> GenerateImprovementSuggestions(TimetableStatisticsDto statistics, List<ConflictDto> conflicts)
        {
            var suggestions = new List<string>();

            if (conflicts.Any(c => c.Type.Contains("DoubleBooking")))
            {
                suggestions.Add("Resolve teacher double-booking conflicts for better schedule integrity");
            }

            if (statistics.EmptySlots > 10)
            {
                suggestions.Add("Consider adding more subjects or activities to utilize empty slots");
            }

            var dailyVariance = 0;
            if (statistics.DailyDistribution.Values.Any())
            {
                dailyVariance = statistics.DailyDistribution.Values.Max() - statistics.DailyDistribution.Values.Min();
            }

            if (dailyVariance > 2)
            {
                suggestions.Add("Redistribute classes across days for better daily balance");
            }

            return suggestions;
        }

        /// <summary>
        /// Generate consistent colors for subjects
        /// </summary>
        private string GenerateSubjectColor(string subjectName)
        {
            var colors = new[]
            {
                "#FF6B6B", "#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD",
                "#FF9F43", "#6C5CE7", "#FD79A8", "#00B894", "#0984E3", "#00CEC9",
                "#E84393", "#FDCB6E", "#A29BFE", "#55A3FF"
            };

            int hash = 0;
            foreach (char c in subjectName)
            {
                hash = (hash * 31 + c) % int.MaxValue;
            }

            return colors[Math.Abs(hash) % colors.Length];
        }
     

        #endregion

        #region DTOs and Supporting Classes

        public class UpdateStatusRequest
        {
            public bool IsActive { get; set; }
        }

        public class ClassSubjectWithTeacherDto
        {
            public int SubjectId { get; set; }
            public string SubjectName { get; set; } = string.Empty;
            public int? TeacherId { get; set; }
            public string TeacherName { get; set; } = string.Empty;
            public int HoursPerWeek { get; set; }
            public bool IsAssigned { get; set; }
            public string SubjectColor { get; set; } = string.Empty;
        }

        public class AssignedTeacherDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Subject { get; set; } = string.Empty;
            public List<string> SubjectNames { get; set; } = new();
            public int SubjectCount { get; set; }
        }

        public class ClassAssignmentStatusDto
        {
            public int ClassId { get; set; }
            public int TotalSubjects { get; set; }
            public int AssignedSubjects { get; set; }
            public List<string> UnassignedSubjects { get; set; } = new();
            public bool IsFullyAssigned { get; set; }
            public double CompletionPercentage { get; set; }
        }

        public class AssignTeacherDto
        {
            public int ClassId { get; set; }
            public int SubjectId { get; set; }
            public int TeacherId { get; set; }
            public string SubjectName { get; set; } = string.Empty;
            public string TeacherName { get; set; } = string.Empty;
            public int Period { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
        }

        public class CheckTeacherConflictDto
        {
            public int TeacherId { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
            public int Period { get; set; }
            public int ClassId { get; set; }
        }

        public class AdvancedTimetableStatistics
        {
            public TimetableStatisticsDto BasicStatistics { get; set; } = new();
            public ConflictAnalysis ConflictAnalysis { get; set; } = new();
            public UtilizationAnalysis UtilizationAnalysis { get; set; } = new();
        }

        public class ConflictAnalysis
        {
            public int TotalConflicts { get; set; }
            public int TeacherConflicts { get; set; }
            public int RestrictedPeriodConflicts { get; set; }
            public int InvalidAssignmentConflicts { get; set; }
            public Dictionary<string, int> ConflictsByDay { get; set; } = new();
            public Dictionary<string, int> ConflictsByPeriod { get; set; } = new();
        }

        public class UtilizationAnalysis
        {
            public int TotalPossibleSlots { get; set; }
            public int UsedSlots { get; set; }
            public double UtilizationPercentage { get; set; }
        }

        public class ClassGenerationReadiness
        {
            public int ClassId { get; set; }
            public bool IsReady { get; set; }
            public double ReadinessScore { get; set; }
            public List<ConflictPreventionSuggestion> CriticalIssues { get; set; } = new();
            public List<ConflictPreventionSuggestion> Warnings { get; set; } = new();
            public ClassAssignmentStatusDto AssignmentStatus { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
        }

        public class TimetableQualityReport
        {
            public int TimetableId { get; set; }
            public double OverallQualityScore { get; set; }
            public TimetableStatisticsDto Statistics { get; set; } = new();
            public ConflictSummary ConflictSummary { get; set; } = new();
            public QualityMetrics QualityMetrics { get; set; } = new();
            public List<string> ImprovementSuggestions { get; set; } = new();
        }

        public class ConflictSummary
        {
            public int TotalConflicts { get; set; }
            public int CriticalConflicts { get; set; }
            public int MinorConflicts { get; set; }
            public Dictionary<string, int> ConflictsByType { get; set; } = new();
        }

        public class QualityMetrics
        {
            public double ConflictRate { get; set; }
            public double UtilizationRate { get; set; }
            public double BalanceScore { get; set; }
            public string OverallRating { get; set; } = string.Empty;
        }

        public class TeacherAvailabilityMatrix
        {
            public int TeacherId { get; set; }
            public string TeacherName { get; set; } = string.Empty;
            public Dictionary<string, Dictionary<int, SlotAvailabilityInfo>> AvailabilityGrid { get; set; } = new();
        }

        public class SlotAvailabilityInfo
        {
            public bool IsAvailable { get; set; }
            public string? ConflictReason { get; set; }
            public string? ConflictingClassName { get; set; }
            public string? ConflictingSubject { get; set; }
        }

        public class BulkConflictCheckRequest
        {
            public List<SlotConflictCheckDto> SlotsToCheck { get; set; } = new();
        }

        public class SlotConflictCheckDto
        {
            public int TeacherId { get; set; }
            public int ClassId { get; set; }
            public DayOfWeek DayOfWeek { get; set; }
            public int Period { get; set; }
        }

        public class BulkConflictCheckResult
        {
            public List<SlotConflictResult> SlotResults { get; set; } = new();
            public int TotalChecked { get; set; }
            public int AvailableSlots { get; set; }
            public int ConflictingSlots { get; set; }
        }

        public class SlotConflictResult
        {
            public SlotConflictCheckDto SlotRequest { get; set; } = new();
            public TeacherConflictResult ConflictResult { get; set; } = new();
        }

        public class OptimizationSuggestion
        {
            public string Type { get; set; } = string.Empty;
            public string Priority { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string Recommendation { get; set; } = string.Empty;
            public string Impact { get; set; } = string.Empty;
        }

        #endregion
    }
}