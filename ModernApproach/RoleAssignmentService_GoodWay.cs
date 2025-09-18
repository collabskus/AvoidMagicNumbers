// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment with Practical DI
// ============================================

using System.ComponentModel;

namespace ModernApproach
{
    // ============================================
    // STRONG TYPES - Prevent primitive obsession
    // ============================================
    
    public readonly record struct UserId(string Value)
    {
        public static implicit operator string(UserId userId) => userId.Value;
        public override string ToString() => Value;
        
        public static UserId From(string value) => 
            string.IsNullOrWhiteSpace(value) 
                ? throw new ArgumentException("UserId cannot be null or empty", nameof(value))
                : new(value);
    }
    
    public readonly record struct DepartmentId(string Value) : IEquatable<DepartmentId>
    {
        public static implicit operator string(DepartmentId departmentId) => departmentId.Value;
        public override string ToString() => Value;
        
        public static DepartmentId From(string value) => 
            string.IsNullOrWhiteSpace(value) 
                ? throw new ArgumentException("DepartmentId cannot be null or empty", nameof(value))
                : new(value);
        
        public bool Equals(DepartmentId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        
        public override int GetHashCode() => 
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }
    
    public readonly record struct SupervisorId(string Value)
    {
        public static implicit operator string(SupervisorId supervisorId) => supervisorId.Value;
        public override string ToString() => Value;
    }
    
    public readonly record struct RoleId(Guid Value)
    {
        public static RoleId New() => new(Guid.NewGuid());
        public static implicit operator Guid(RoleId roleId) => roleId.Value;
        public override string ToString() => Value.ToString();
    }

    // ============================================
    // ENUMS - Document business meaning
    // ============================================
    
    public enum StandardRoleType
    {
        [Description("Department Manager - oversees department operations")]
        DepartmentManager = 6,
        
        [Description("Project Coordinator - manages project workflows")]
        ProjectCoordinator = 7
    }

    public enum WorkAssignmentRoleCode
    {
        ProjectManager = 101,
        GeneralAdministrator = 203,
        SpecialAdministrator = 202
    }

    public enum ResourceType
    {
        UserAccount = 25
    }

    // ============================================
    // RESULT TYPES - Simplified error handling
    // ============================================
    
    public sealed record RoleAssignmentResult
    {
        public IReadOnlyList<RoleAssignmentSuccess> Successes { get; init; } = [];
        public IReadOnlyList<RoleAssignmentFailure> Failures { get; init; } = [];
        
        public bool IsSuccess => Failures.Count == 0 && Successes.Count > 0;
        public bool HasFailures => Failures.Count > 0;
        
        public static RoleAssignmentResult Success(params RoleAssignmentSuccess[] successes) => 
            new() { Successes = successes };
        
        public static RoleAssignmentResult WithFailures(params RoleAssignmentFailure[] failures) => 
            new() { Failures = failures };
        
        public static RoleAssignmentResult Mixed(
            IEnumerable<RoleAssignmentSuccess> successes, 
            IEnumerable<RoleAssignmentFailure> failures) => 
            new() { 
                Successes = successes.ToArray(),
                Failures = failures.ToArray()
            };
    }

    public sealed record RoleAssignmentSuccess(RoleId RoleId, StandardRoleType RoleType, string Description);
    public sealed record RoleAssignmentFailure(StandardRoleType RoleType, string Error);

    // ============================================
    // DATA STRUCTURES - Immutable domain models
    // ============================================
    
    public sealed record RoleAssignment(
        StandardRoleType RoleType,
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode WorkRole
    )
    {
        public string Description => RoleType.GetDescription();
    }

    public sealed record CreateUserRoleCommand(
        RoleId RoleId,
        UserId UserId,
        DepartmentId DepartmentId,
        DateTime AssignedDate,
        StandardRoleType RoleType,
        SupervisorId SupervisorId
    );

    public sealed record WorkAssignmentCommand(
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode RoleCode,
        UserId UserId,
        ResourceType ResourceType,
        DateTime CreatedDate
    );

    // ============================================
    // CONFIGURATION - Externalized settings
    // ============================================
    
    public sealed record RoleAssignmentOptions
    {
        public static readonly RoleAssignmentOptions Default = new();
        
        public IReadOnlySet<DepartmentId> SpecialDepartments { get; init; } = 
            new HashSet<DepartmentId> { DepartmentId.From("SPECIAL-DEPT") };
        
        public bool ValidateExistingRoles { get; init; } = true;
        public bool AllowPartialFailures { get; init; } = true;
        public TimeSpan TransactionTimeout { get; init; } = TimeSpan.FromMinutes(5);
    }

    // ============================================
    // INTERFACES - Dependency abstractions
    // ============================================
    
    public interface IUserRoleRepository
    {
        Task CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default);
        Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default);
    }

    public interface IWorkAssignmentRepository
    {
        Task CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default);
    }

    public interface ISupervisorResolver
    {
        SupervisorId GetDepartmentManager(DepartmentId departmentId);
        SupervisorId GetProjectCoordinator(DepartmentId departmentId);
    }

    public interface IRoleAssignmentLogger
    {
        void LogRoleAssignmentStarting(UserId userId, DepartmentId departmentId, int roleCount);
        void LogRoleAssignmentCompleted(UserId userId, int successCount, int failureCount);
        void LogRoleAssigning(StandardRoleType roleType, UserId userId);
        void LogRoleAssigned(RoleId roleId, StandardRoleType roleType, UserId userId);
        void LogRoleAssignmentFailed(StandardRoleType roleType, UserId userId, string error);
        void LogWorkAssignmentCreated(SupervisorId supervisorId, WorkAssignmentRoleCode roleCode);
    }

    // ============================================
    // EXTENSION METHODS - Clean code helpers
    // ============================================
    
    public static class EnumExtensions
    {
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                               .Cast<DescriptionAttribute>()
                               .FirstOrDefault();
            return attribute?.Description ?? value.ToString();
        }
    }

    public static class RoleAssignmentExtensions
    {
        private const string ManagerPrefix = "manager-";
        private const string CoordinatorPrefix = "coordinator-";
        
        public static SupervisorId GetDepartmentManager(this DepartmentId departmentId) =>
            new($"{ManagerPrefix}{departmentId.Value}");
        
        public static SupervisorId GetProjectCoordinator(this DepartmentId departmentId) =>
            new($"{CoordinatorPrefix}{departmentId.Value}");
    }

    // ============================================
    // SERVICE - Main business logic with practical DI
    // ============================================
    
    public sealed class RoleAssignmentService_GoodWay
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IWorkAssignmentRepository _workAssignmentRepository;
        private readonly ISupervisorResolver _supervisorResolver;
        private readonly IRoleAssignmentLogger _logger;
        private readonly RoleAssignmentOptions _options;

        public RoleAssignmentService_GoodWay(
            IUserRoleRepository userRoleRepository,
            IWorkAssignmentRepository workAssignmentRepository,
            ISupervisorResolver supervisorResolver,
            IRoleAssignmentLogger logger,
            RoleAssignmentOptions? options = null)
        {
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _workAssignmentRepository = workAssignmentRepository ?? throw new ArgumentNullException(nameof(workAssignmentRepository));
            _supervisorResolver = supervisorResolver ?? throw new ArgumentNullException(nameof(supervisorResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options ?? RoleAssignmentOptions.Default;
        }

        public async Task<RoleAssignmentResult> AssignStandardRolesAsync(
            UserId userId, 
            DepartmentId departmentId, 
            DateTime assignedDate,
            CancellationToken cancellationToken = default)
        {
            var roleAssignments = CreateStandardRoleAssignments(departmentId);
            _logger.LogRoleAssignmentStarting(userId, departmentId, roleAssignments.Count);

            var existingRoles = _options.ValidateExistingRoles
                ? await _userRoleRepository.GetExistingRoleTypesAsync(userId, cancellationToken)
                : new HashSet<StandardRoleType>();

            var successes = new List<RoleAssignmentSuccess>();
            var failures = new List<RoleAssignmentFailure>();

            foreach (var assignment in roleAssignments)
            {
                if (existingRoles.Contains(assignment.RoleType))
                {
                    var error = $"User already has {assignment.RoleType.GetDescription()} role";
                    _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, error);
                    failures.Add(new RoleAssignmentFailure(assignment.RoleType, error));
                    continue;
                }

                try
                {
                    var roleId = await ProcessRoleAssignmentAsync(userId, departmentId, assignedDate, assignment, cancellationToken);
                    successes.Add(new RoleAssignmentSuccess(roleId, assignment.RoleType, assignment.Description));
                }
                catch (Exception ex)
                {
                    _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, ex.Message);
                    failures.Add(new RoleAssignmentFailure(assignment.RoleType, ex.Message));
                    
                    if (!_options.AllowPartialFailures)
                        break;
                }
            }

            _logger.LogRoleAssignmentCompleted(userId, successes.Count, failures.Count);
            return RoleAssignmentResult.Mixed(successes, failures);
        }

        // Synchronous convenience method
        public void AssignStandardRoles(UserId userId, DepartmentId departmentId, DateTime assignedDate)
        {
            var result = AssignStandardRolesAsync(userId, departmentId, assignedDate).GetAwaiter().GetResult();
            if (!result.IsSuccess && result.Failures.Any())
            {
                var errors = string.Join(", ", result.Failures.Select(f => f.Error));
                throw new InvalidOperationException($"Role assignment failed: {errors}");
            }
        }

        private IReadOnlyList<RoleAssignment> CreateStandardRoleAssignments(DepartmentId departmentId)
        {
            return
            [
                new RoleAssignment(
                    StandardRoleType.DepartmentManager,
                    _supervisorResolver.GetDepartmentManager(departmentId),
                    WorkAssignmentRoleCode.ProjectManager),
                
                new RoleAssignment(
                    StandardRoleType.ProjectCoordinator,
                    _supervisorResolver.GetProjectCoordinator(departmentId),
                    GetCoordinatorWorkRole(departmentId))
            ];
        }

        private WorkAssignmentRoleCode GetCoordinatorWorkRole(DepartmentId departmentId) =>
            _options.SpecialDepartments.Contains(departmentId)
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;

        private async Task<RoleId> ProcessRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            DateTime assignedDate, 
            RoleAssignment assignment,
            CancellationToken cancellationToken)
        {
            var roleId = RoleId.New();
            
            _logger.LogRoleAssigning(assignment.RoleType, userId);
            
            var userRoleCommand = new CreateUserRoleCommand(
                roleId, userId, departmentId, assignedDate, assignment.RoleType, assignment.SupervisorId);
            
            await _userRoleRepository.CreateUserRoleAsync(userRoleCommand, cancellationToken);
            
            var workCommand = new WorkAssignmentCommand(
                assignment.SupervisorId, assignment.WorkRole, userId, ResourceType.UserAccount, assignedDate);
            
            await _workAssignmentRepository.CreateWorkAssignmentAsync(workCommand, cancellationToken);
            
            _logger.LogRoleAssigned(roleId, assignment.RoleType, userId);
            _logger.LogWorkAssignmentCreated(assignment.SupervisorId, assignment.WorkRole);
            
            return roleId;
        }
    }

    // ============================================
    // DEFAULT IMPLEMENTATIONS - For demo/testing
    // ============================================
    
    public sealed class ConsoleRoleAssignmentLogger : IRoleAssignmentLogger
    {
        public void LogRoleAssignmentStarting(UserId userId, DepartmentId departmentId, int roleCount)
        {
            Console.WriteLine($"[INFO] Starting assignment of {roleCount} roles for user {userId} in department {departmentId}");
        }

        public void LogRoleAssignmentCompleted(UserId userId, int successCount, int failureCount)
        {
            Console.WriteLine($"[INFO] Completed role assignment for user {userId} - Success: {successCount}, Failed: {failureCount}");
        }

        public void LogRoleAssigning(StandardRoleType roleType, UserId userId)
        {
            Console.WriteLine($"[INFO] Assigning {roleType.GetDescription()} to user {userId}");
        }

        public void LogRoleAssigned(RoleId roleId, StandardRoleType roleType, UserId userId)
        {
            Console.WriteLine($"[DEBUG] Successfully created role assignment {roleId} ({roleType}) for user {userId}");
        }

        public void LogRoleAssignmentFailed(StandardRoleType roleType, UserId userId, string error)
        {
            Console.WriteLine($"[WARN] Failed to assign {roleType} to user {userId}: {error}");
        }

        public void LogWorkAssignmentCreated(SupervisorId supervisorId, WorkAssignmentRoleCode roleCode)
        {
            Console.WriteLine($"[REPO] Created work assignment - Supervisor: {supervisorId}, Role: {roleCode}");
        }
    }

    public sealed class DemoUserRoleRepository : IUserRoleRepository
    {
        public Task CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[REPO] Creating user role - RoleId: {command.RoleId}, UserId: {command.UserId}");
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<StandardRoleType>>(new HashSet<StandardRoleType>());
        }
    }

    public sealed class DemoWorkAssignmentRepository : IWorkAssignmentRepository
    {
        public Task CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default)
        {
            // Logging handled by the logger, not the repository
            return Task.CompletedTask;
        }
    }

    public sealed class DefaultSupervisorResolver : ISupervisorResolver
    {
        public SupervisorId GetDepartmentManager(DepartmentId departmentId) =>
            departmentId.GetDepartmentManager();

        public SupervisorId GetProjectCoordinator(DepartmentId departmentId) =>
            departmentId.GetProjectCoordinator();
    }

    // ============================================
    // DEMO FACTORY - Simplified creation
    // ============================================
    
    public static class DemoRoleAssignmentService
    {
        public static RoleAssignmentService_GoodWay Create()
        {
            return new RoleAssignmentService_GoodWay(
                new DemoUserRoleRepository(),
                new DemoWorkAssignmentRepository(),
                new DefaultSupervisorResolver(),
                new ConsoleRoleAssignmentLogger(),
                RoleAssignmentOptions.Default
            );
        }
    }
}
