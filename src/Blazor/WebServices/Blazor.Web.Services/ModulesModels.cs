//using ConnectX.Domain.Entities;

//namespace Blazor.Web.Services
//{
//    public enum StudentUpcomingEventType
//    {
//        Meeting = 0,
//        Assignment = 1,
//        Exam = 2
//    }

//    public class StudentUpcomingEventModel
//    {
//        public Guid Id { get; init; }
//        public Guid ExternalId { get; init; }
//        public StudentUpcomingEventType EventType { get; init; }
//        public DateTimeOffset StartsAt { get; init; }
//        public DateTimeOffset? EndsAt { get; init; }
//        public string Title { get; init; } = string.Empty;
//        public string? Description { get; init; }
//        public Guid? ModuleId { get; init; }
//        public string? ModuleCode { get; init; }
//        public string? ModuleName { get; init; }
//        public Guid? ModuleOfferingId { get; init; }
//        public Guid? MeetingId { get; init; }
//        public string? MeetingTitle { get; init; }
//        public Guid? TeacherId { get; init; }
//        public string? TeacherName { get; init; }
//        public string? TeacherEmail { get; init; }
//        public string? LocationName { get; init; }
//        public string? LocationAddress { get; init; }
//        public string? OnlineUrl { get; init; }
//        public MeetingFormat? MeetingFormat { get; init; }
//        public AssessmentType? AssessmentType { get; init; }
//        public AssignmentType? AssignmentType { get; init; }
//        public AssignmentStatus? AssignmentStatus { get; init; }
//        public AttendanceStatus? AttendanceStatus { get; init; }
//        public bool IsCancelled { get; init; }
//        public bool IsRequired { get; init; }
//        public decimal? MaximumScore { get; init; }
//        public decimal? WeightPercentage { get; init; }
//        public string? ResultGrade { get; init; }
//        public decimal? ResultScore { get; init; }
//        public bool? ResultPassed { get; init; }
//        public int RelatedItemCount { get; init; }
//    }

//    public class StudentMeetingDetailModel
//    {
//        public StudentUpcomingEventModel Summary { get; init; } = new();
//        public string? PreparationInstructions { get; init; }
//        public string? CancellationReason { get; init; }
//        public IReadOnlyList<LearningObjectiveModel> LearningObjectives { get; init; } = [];
//        public IReadOnlyList<LearningActivityModel> LearningActivities { get; init; } = [];
//        public IReadOnlyList<AssignmentSummaryModel> PreparationAssignments { get; init; } = [];
//        public IReadOnlyList<ResourceSummaryModel> Resources { get; init; } = [];
//        public IReadOnlyList<LessonContentSummaryModel> LessonContents { get; init; } = [];
//        public IReadOnlyList<StudyTipSummaryModel> StudyTips { get; init; } = [];
//        public IReadOnlyList<LocationDirectionModel> Directions { get; init; } = [];
//    }

//    public record LearningObjectiveModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        string Description,
//        string? BloomLevel,
//        bool IsAssessed);

//    public record LearningActivityModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        string? Description,
//        LearningActivityType ActivityType,
//        int? DurationMinutes,
//        string? Instructions,
//        bool IsRequired);

//    public record AssignmentSummaryModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        AssignmentType AssignmentType,
//        AssignmentStatus Status,
//        DateTimeOffset? DueAt,
//        decimal? MaximumScore,
//        decimal? WeightPercentage);

//    public record ResourceSummaryModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        string? Description,
//        ResourceType ResourceType,
//        string? Url,
//        string? FileName,
//        bool IsRequired,
//        string? CitationText);

//    public record LessonContentSummaryModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        string? Summary,
//        int? EstimatedStudyMinutes,
//        bool IsRequired);

//    public record StudyTipSummaryModel(
//        Guid Id,
//        Guid ExternalId,
//        string Title,
//        string Body,
//        StudyTipCategory Category,
//        bool IsHighlighted);

//    public record LocationDirectionModel(
//        TravelMode TravelMode,
//        string Title,
//        string Instructions,
//        string? PublicTransportStop,
//        string? ParkingInstructions,
//        string? ExternalNavigationUrl);
//}

