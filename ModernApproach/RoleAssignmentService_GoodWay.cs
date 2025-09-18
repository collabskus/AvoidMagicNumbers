// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment with Enhanced Patterns
// ============================================

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ModernApproach
{
    // ============================================
    // STRONG TYPES - Prevent primitive obsession with validation
    // ============================================
    
    public readonly record struct UserId(string Value)
    {
        public static implicit operator string(UserId userId) => userId.Value;
        public override string ToString() => Value;
        
        public static UserId From(string value) => 
            string.IsNullOrWhiteSpace(value) 
                ? throw new ArgumentException("UserId cannot be null or empty", nameof(value))
                : new(value);
        
        public static bool TryParse(string? value, out UserId userId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                userId = default;
                return false;
            }
            userId = new(value);
            return true;
        }
    }
    
    public readonly record struct DepartmentId(string Value) : IEquatable<DepartmentId>
    {
        public static implicit operator string(DepartmentId departmentId) => departmentId.Value;
        public override string ToString() => Value;
        
        public static DepartmentId From(string value) => 
            string.IsNullOrWhiteSpace(value) 
                ? throw new ArgumentException("DepartmentId cannot be null or empty", nameof(value))
                : new(value.ToUpperInvariant());
        
        public bool Equals(DepartmentId other) =>
            string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        
        public override int GetHashCode() => 
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
            
        public static bool TryParse(string? value, out DepartmentId departmentId)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                departmentId = default;
                return false;
            }
            departmentId = new(value.ToUpperInvariant());
            return true;
        }
    }
    
    public readonly record struct SupervisorId(string Value)
    {
        public static implicit operator string(SupervisorId supervisorId) => supervisorId.Value;
        public override string ToString() => Value;
        
        public static SupervisorId From(string value) => 
            string.IsNullOrWhiteSpace(value) 
                ? throw new ArgumentException("SupervisorId cannot be null or empty", nameof(value))
                : new(value);
    }
    
    public readonly record struct RoleId(Guid Value)
    {
        public static RoleId New() => new(Guid.NewGuid());
        public static implicit operator Guid(RoleId roleId) => roleId.Value;
        public override string ToString() => Value.ToString();
        
        public static RoleId From(Guid value) => 
            value == Guid.Empty 
                ? throw new ArgumentException("RoleId cannot be empty", nameof(value))
                : new(value);
    }

    // ============================================
    // ENUMS - Document business meaning with metadata
    // ============================================
    
    public enum StandardRoleType
    {
        [Description("Department Manager - oversees department operations")]
        [RoleMetadata(Priority = 1, RequiresApproval = true)]
        DepartmentManager = 6,
        
        [Description("Project Coordinator - manages project workflows")]
        [RoleMetadata(Priority = 2, RequiresApproval = false)]
        ProjectCoordinator = 7
    }

    public enum WorkAssignmentRoleCode
    {
        [Description("Project Manager")]
        ProjectManager = 101,
        
        [Description("General Administrator")]
        GeneralAdministrator = 203,
        
        [Description("Special Administrator")]
        SpecialAdministrator = 202
    }

    public enum ResourceType
    {
        [Description("User Account Resource")]
        UserAccount = 25
    }

    // ============================================
    // METADATA ATTRIBUTES
    // ============================================
    
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class RoleMetadataAttribute : Attribute
    {
        public int Priority { get; init; }
        public bool RequiresApproval { get; init; }
    }

    // ============================================
    // RESULT TYPES - Railway-oriented programming
    // ============================================
    
    public abstract record Result<TSuccess, TFailure>
    {
        private Result() { }
        
        public sealed record Success(TSuccess Value) : Result<TSuccess, TFailure>;
        public sealed record Failure(TFailure Error) : Result<TSuccess, TFailure>;
        
        public bool IsSuccess => this is Success;
        public bool IsFailure => this is Failure;
        
        public TResult Match<TResult>(
            Func<TSuccess, TResult> success,
            Func<TFailure, TResult> failure) => this switch
        {
            Success s => success(s.Value),
            Failure f => failure(f.Error),
            _ => throw new InvalidOperationException()
        };
        
        public static implicit operator Result<TSuccess, TFailure>(TSuccess value) => 
            new Success(value);
        public static implicit operator Result<TSuccess, TFailure>(TFailure error) => 
            new Failure(error);
    }
    
    public sealed record RoleAssignmentResult
    {
        public IReadOnlyList<RoleAssignmentSuccess> Successes { get; init; } = [];
        public IReadOnlyList<RoleAssignmentFailure> Failures { get; init; } = [];
        
        public bool IsSuccess => Failures.Count == 0 && Successes.Count > 0;
        public bool IsPartialSuccess => Successes.Count > 0 && Failures.Count > 0;
        public bool IsFailure => Successes.Count == 0 && Failures.Count > 0;
        
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

    public sealed record RoleAssignmentSuccess(
        RoleId RoleId, 
        StandardRoleType RoleType, 
        string Description,
        TimeSpan Duration);
        
    public sealed record RoleAssignmentFailure(
        StandardRoleType RoleType, 
        string Error,
        FailureReason Reason);

    public enum FailureReason
    {
        AlreadyExists,
        ValidationFailed,
        RepositoryError,
        Unauthorized,
        TransientFailure,
        Unknown
    }

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
        public int Priority => RoleType.GetMetadata()?.Priority ?? 99;
        public bool RequiresApproval => RoleType.GetMetadata()?.RequiresApproval ?? false;
    }

    public sealed record CreateUserRoleCommand(
        RoleId RoleId,
        UserId UserId,
        DepartmentId DepartmentId,
        DateTime AssignedDate,
        StandardRoleType RoleType,
        SupervisorId SupervisorId,
        string? AssignedBy = null
    )
    {
        public CreateUserRoleCommand Validate()
        {
            if (AssignedDate > DateTime.UtcNow.AddDays(1))
                throw new ArgumentException("AssignedDate cannot be more than 1 day in the future");
            if (AssignedDate < DateTime.UtcNow.AddYears(-10))
                throw new ArgumentException("AssignedDate cannot be more than 10 years in the past");
            return this;
        }
    }

    public sealed record WorkAssignmentCommand(
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode RoleCode,
        UserId UserId,
        ResourceType ResourceType,
        DateTime CreatedDate
    );

    // ============================================
    // CONFIGURATION - Externalized settings with validation
    // ============================================
    
    public sealed record RoleAssignmentOptions
    {
        public static readonly RoleAssignmentOptions Default = new();
        
        public IReadOnlySet<DepartmentId> SpecialDepartments { get; init; } = 
            new HashSet<DepartmentId> { DepartmentId.From("SPECIAL-DEPT") };
        
        public bool ValidateExistingRoles { get; init; } = true;
        public bool AllowPartialFailures { get; init; } = true;
        public TimeSpan TransactionTimeout { get; init; } = TimeSpan.FromMinutes(5);
        public int MaxRetryAttempts { get; init; } = 3;
        public TimeSpan RetryDelay { get; init; } = TimeSpan.FromSeconds(1);
        public bool EnableTelemetry { get; init; } = true;
        public bool RequireApprovalCheck { get; init; } = false;
        
        public RoleAssignmentOptions Validate()
        {
            if (TransactionTimeout <= TimeSpan.Zero)
                throw new ArgumentException("TransactionTimeout must be positive");
            if (MaxRetryAttempts < 0)
                throw new ArgumentException("MaxRetryAttempts cannot be negative");
            if (RetryDelay < TimeSpan.Zero)
                throw new ArgumentException("RetryDelay cannot be negative");
            return this;
        }
    }

    // ============================================
    // INTERFACES - Dependency abstractions
    // ============================================
    
    public interface IUserRoleRepository
    {
        Task CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default);
        Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(RoleId roleId, CancellationToken cancellationToken = default);
    }

    public interface IWorkAssignmentRepository
    {
        Task CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default);
    }

    public interface ISupervisorResolver
    {
        SupervisorId GetDepartmentManager(DepartmentId departmentId);
        SupervisorId GetProjectCoordinator(DepartmentId departmentId);
        Task<bool> ValidateSupervisorAsync(SupervisorId supervisorId, CancellationToken cancellationToken = default);
    }

    public interface IRoleAssignmentLogger
    {
        void LogRoleAssignmentStarting(UserId userId, DepartmentId departmentId, int roleCount);
        void LogRoleAssignmentCompleted(UserId userId, int successCount, int failureCount, TimeSpan duration);
        void LogRoleAssigning(StandardRoleType roleType, UserId userId);
        void LogRoleAssigned(RoleId roleId, StandardRoleType roleType, UserId userId, TimeSpan duration);
        void LogRoleAssignmentFailed(StandardRoleType roleType, UserId userId, string error, FailureReason reason);
        void LogWorkAssignmentCreated(SupervisorId supervisorId, WorkAssignmentRoleCode roleCode);
        void LogRetryAttempt(int attempt, StandardRoleType roleType);
    }

    public interface ITelemetryCollector
    {
        void RecordRoleAssignment(StandardRoleType roleType, bool success, TimeSpan duration);
        void RecordWorkAssignment(WorkAssignmentRoleCode roleCode, TimeSpan duration);
        void RecordRetry(StandardRoleType roleType, int attempt);
    }

    public interface IUnitOfWork : IDisposable
    {
        Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }

    public interface IRoleAssignmentFactory
    {
        IReadOnlyList<RoleAssignment> CreateStandardRoleAssignments(DepartmentId departmentId);
        IReadOnlyList<RoleAssignment> CreateCustomRoleAssignments(DepartmentId departmentId, IEnumerable<StandardRoleType> roleTypes);
    }

    // ============================================
    // EXTENSION METHODS - Clean code helpers
    // ============================================
    
    public static class EnumExtensions
    {
        private static readonly Dictionary<Enum, string> DescriptionCache = new();
        private static readonly Dictionary<Enum, RoleMetadataAttribute?> MetadataCache = new();
        
        public static string GetDescription(this Enum value)
        {
            if (DescriptionCache.TryGetValue(value, out var cached))
                return cached;
                
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                               .Cast<DescriptionAttribute>()
                               .FirstOrDefault();
            var description = attribute?.Description ?? value.ToString();
            DescriptionCache[value] = description;
            return description;
        }
        
        public static RoleMetadataAttribute? GetMetadata(this StandardRoleType value)
        {
            if (MetadataCache.TryGetValue(value, out var cached))
                return cached;
                
            var field = value.GetType().GetField(value.ToString());
            var metadata = field?.GetCustomAttributes(typeof(RoleMetadataAttribute), false)
                               .Cast<RoleMetadataAttribute>()
                               .FirstOrDefault();
            MetadataCache[value] = metadata;
            return metadata;
        }
    }

    public static class RoleAssignmentExtensions
    {
        private const string ManagerPrefix = "manager-";
        private const string CoordinatorPrefix = "coordinator-";
        
        public static SupervisorId GetDepartmentManager(this DepartmentId departmentId) =>
            SupervisorId.From($"{ManagerPrefix}{departmentId.Value}");
        
        public static SupervisorId GetProjectCoordinator(this DepartmentId departmentId) =>
            SupervisorId.From($"{CoordinatorPrefix}{departmentId.Value}");
    }

    // ============================================
    // FACTORIES - Creation patterns
    // ============================================
    
    public sealed class StandardRoleAssignmentFactory : IRoleAssignmentFactory
    {
        private readonly ISupervisorResolver _supervisorResolver;
        private readonly RoleAssignmentOptions _options;
        
        public StandardRoleAssignmentFactory(
            ISupervisorResolver supervisorResolver,
            RoleAssignmentOptions options)
        {
            _supervisorResolver = supervisorResolver ?? throw new ArgumentNullException(nameof(supervisorResolver));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }
        
        public IReadOnlyList<RoleAssignment> CreateStandardRoleAssignments(DepartmentId departmentId)
        {
            var assignments = new List<RoleAssignment>
            {
                new RoleAssignment(
                    StandardRoleType.DepartmentManager,
                    _supervisorResolver.GetDepartmentManager(departmentId),
                    WorkAssignmentRoleCode.ProjectManager),
                
                new RoleAssignment(
                    StandardRoleType.ProjectCoordinator,
                    _supervisorResolver.GetProjectCoordinator(departmentId),
                    GetCoordinatorWorkRole(departmentId))
            };
            
            return assignments.OrderBy(a => a.Priority).ToList();
        }
        
        public IReadOnlyList<RoleAssignment> CreateCustomRoleAssignments(
            DepartmentId departmentId, 
            IEnumerable<StandardRoleType> roleTypes)
        {
            var assignments = new List<RoleAssignment>();
            
            foreach (var roleType in roleTypes)
            {
                var supervisorId = roleType switch
                {
                    StandardRoleType.DepartmentManager => _supervisorResolver.GetDepartmentManager(departmentId),
                    StandardRoleType.ProjectCoordinator => _supervisorResolver.GetProjectCoordinator(departmentId),
                    _ => throw new NotSupportedException($"Role type {roleType} is not supported")
                };
                
                var workRole = roleType switch
                {
                    StandardRoleType.DepartmentManager => WorkAssignmentRoleCode.ProjectManager,
                    StandardRoleType.ProjectCoordinator => GetCoordinatorWorkRole(departmentId),
                    _ => WorkAssignmentRoleCode.GeneralAdministrator
                };
                
                assignments.Add(new RoleAssignment(roleType, supervisorId, workRole));
            }
            
            return assignments.OrderBy(a => a.Priority).ToList();
        }
        
        private WorkAssignmentRoleCode GetCoordinatorWorkRole(DepartmentId departmentId) =>
            _options.SpecialDepartments.Contains(departmentId)
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;
    }

    // ============================================
    // SERVICE - Main business logic with enhanced patterns
    // ============================================
    
    public sealed class RoleAssignmentService_GoodWay
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IWorkAssignmentRepository _workAssignmentRepository;
        private readonly ISupervisorResolver _supervisorResolver;
        private readonly IRoleAssignmentLogger _logger;
        private readonly ITelemetryCollector? _telemetry;
        private readonly IUnitOfWork? _unitOfWork;
        private readonly IRoleAssignmentFactory _roleAssignmentFactory;
        private readonly RoleAssignmentOptions _options;

        public RoleAssignmentService_GoodWay(
            IUserRoleRepository userRoleRepository,
            IWorkAssignmentRepository workAssignmentRepository,
            ISupervisorResolver supervisorResolver,
            IRoleAssignmentLogger logger,
            IRoleAssignmentFactory? roleAssignmentFactory = null,
            ITelemetryCollector? telemetry = null,
            IUnitOfWork? unitOfWork = null,
            RoleAssignmentOptions? options = null)
        {
            _userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            _workAssignmentRepository = workAssignmentRepository ?? throw new ArgumentNullException(nameof(workAssignmentRepository));
            _supervisorResolver = supervisorResolver ?? throw new ArgumentNullException(nameof(supervisorResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = (options ?? RoleAssignmentOptions.Default).Validate();
            _telemetry = telemetry;
            _unitOfWork = unitOfWork;
            _roleAssignmentFactory = roleAssignmentFactory ?? new StandardRoleAssignmentFactory(_supervisorResolver, _options);
        }

        public async Task<RoleAssignmentResult> AssignStandardRolesAsync(
            UserId userId, 
            DepartmentId departmentId, 
            DateTime assignedDate,
            string? assignedBy = null,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var roleAssignments = _roleAssignmentFactory.CreateStandardRoleAssignments(departmentId);
            _logger.LogRoleAssignmentStarting(userId, departmentId, roleAssignments.Count);

            var existingRoles = _options.ValidateExistingRoles
                ? await _userRoleRepository.GetExistingRoleTypesAsync(userId, cancellationToken)
                : new HashSet<StandardRoleType>();

            var successes = new List<RoleAssignmentSuccess>();
            var failures = new List<RoleAssignmentFailure>();

            // Use unit of work if available
            if (_unitOfWork != null)
            {
                try
                {
                    await _unitOfWork.ExecuteAsync(async () =>
                    {
                        await ProcessRoleAssignmentsAsync(
                            userId, departmentId, assignedDate, assignedBy,
                            roleAssignments, existingRoles, successes, failures,
                            cancellationToken);
                        return true;
                    }, cancellationToken);
                    
                    if (failures.Any() && !_options.AllowPartialFailures)
                    {
                        await _unitOfWork.RollbackAsync(cancellationToken);
                    }
                    else
                    {
                        await _unitOfWork.CommitAsync(cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackAsync(cancellationToken);
                    throw new InvalidOperationException("Failed to assign roles within transaction", ex);
                }
            }
            else
            {
                await ProcessRoleAssignmentsAsync(
                    userId, departmentId, assignedDate, assignedBy,
                    roleAssignments, existingRoles, successes, failures,
                    cancellationToken);
            }

            stopwatch.Stop();
            _logger.LogRoleAssignmentCompleted(userId, successes.Count, failures.Count, stopwatch.Elapsed);
            
            return RoleAssignmentResult.Mixed(successes, failures);
        }

        // Synchronous convenience method
        public void AssignStandardRoles(UserId userId, DepartmentId departmentId, DateTime assignedDate)
        {
            var result = AssignStandardRolesAsync(userId, departmentId, assignedDate).GetAwaiter().GetResult();
            if (!result.IsSuccess && result.Failures.Any())
            {
                var errors = string.Join(", ", result.Failures.Select(f => $"{f.RoleType}: {f.Error}"));
                throw new InvalidOperationException($"Role assignment failed: {errors}");
            }
        }

        private async Task ProcessRoleAssignmentsAsync(
            UserId userId,
            DepartmentId departmentId,
            DateTime assignedDate,
            string? assignedBy,
            IReadOnlyList<RoleAssignment> roleAssignments,
            IReadOnlySet<StandardRoleType> existingRoles,
            List<RoleAssignmentSuccess> successes,
            List<RoleAssignmentFailure> failures,
            CancellationToken cancellationToken)
        {
            foreach (var assignment in roleAssignments)
            {
                if (existingRoles.Contains(assignment.RoleType))
                {
                    var error = $"User already has {assignment.RoleType.GetDescription()} role";
                    _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, error, FailureReason.AlreadyExists);
                    failures.Add(new RoleAssignmentFailure(assignment.RoleType, error, FailureReason.AlreadyExists));
                    continue;
                }

                // Check if approval is required
                if (_options.RequireApprovalCheck && assignment.RequiresApproval && string.IsNullOrEmpty(assignedBy))
                {
                    var error = $"Role {assignment.RoleType} requires approval but no approver specified";
                    _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, error, FailureReason.Unauthorized);
                    failures.Add(new RoleAssignmentFailure(assignment.RoleType, error, FailureReason.Unauthorized));
                    continue;
                }

                var assignmentResult = await ProcessRoleAssignmentWithRetryAsync(
                    userId, departmentId, assignedDate, assignedBy, assignment, cancellationToken);
                
                assignmentResult.Match(
                    success => successes.Add(success),
                    failure => failures.Add(failure));

                if (failures.Any() && !_options.AllowPartialFailures)
                    break;
            }
        }

        private async Task<Result<RoleAssignmentSuccess, RoleAssignmentFailure>> ProcessRoleAssignmentWithRetryAsync(
            UserId userId,
            DepartmentId departmentId,
            DateTime assignedDate,
            string? assignedBy,
            RoleAssignment assignment,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            
            for (int attempt = 0; attempt <= _options.MaxRetryAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    _logger.LogRetryAttempt(attempt, assignment.RoleType);
                    _telemetry?.RecordRetry(assignment.RoleType, attempt);
                    await Task.Delay(_options.RetryDelay * attempt, cancellationToken);
                }

                try
                {
                    var roleId = await ProcessRoleAssignmentAsync(
                        userId, departmentId, assignedDate, assignedBy, assignment, cancellationToken);
                    
                    stopwatch.Stop();
                    var success = new RoleAssignmentSuccess(
                        roleId, assignment.RoleType, assignment.Description, stopwatch.Elapsed);
                    
                    _telemetry?.RecordRoleAssignment(assignment.RoleType, true, stopwatch.Elapsed);
                    return success;
                }
                catch (Exception ex) when (attempt < _options.MaxRetryAttempts && IsTransientException(ex))
                {
                    // Retry on transient failures
                    continue;
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var reason = DetermineFailureReason(ex);
                    _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, ex.Message, reason);
                    _telemetry?.RecordRoleAssignment(assignment.RoleType, false, stopwatch.Elapsed);
                    
                    return new RoleAssignmentFailure(assignment.RoleType, ex.Message, reason);
                }
            }
            
            stopwatch.Stop();
            var timeoutError = $"Failed after {_options.MaxRetryAttempts} retry attempts";
            _logger.LogRoleAssignmentFailed(assignment.RoleType, userId, timeoutError, FailureReason.TransientFailure);
            return new RoleAssignmentFailure(assignment.RoleType, timeoutError, FailureReason.TransientFailure);
        }

        private async Task<RoleId> ProcessRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            DateTime assignedDate,
            string? assignedBy,
            RoleAssignment assignment,
            CancellationToken cancellationToken)
        {
            var roleStopwatch = Stopwatch.StartNew();
            var roleId = RoleId.New();
            
            _logger.LogRoleAssigning(assignment.RoleType, userId);
            
            // Validate supervisor if needed
            if (!await _supervisorResolver.ValidateSupervisorAsync(assignment.SupervisorId, cancellationToken))
            {
                throw new InvalidOperationException($"Invalid supervisor: {assignment.SupervisorId}");
            }
            
            var userRoleCommand = new CreateUserRoleCommand(
                roleId, userId, departmentId, assignedDate, 
                assignment.RoleType, assignment.SupervisorId, assignedBy)
                .Validate();
            
            await _userRoleRepository.CreateUserRoleAsync(userRoleCommand, cancellationToken);
            
            var workStopwatch = Stopwatch.StartNew();
            var workCommand = new WorkAssignmentCommand(
                assignment.SupervisorId, assignment.WorkRole, userId, 
                ResourceType.UserAccount, assignedDate);
            
            await _workAssignmentRepository.CreateWorkAssignmentAsync(workCommand, cancellationToken);
            workStopwatch.Stop();
            
            _logger.LogRoleAssigned(roleId, assignment.RoleType, userId, roleStopwatch.Elapsed);
            _logger.LogWorkAssignmentCreated(assignment.SupervisorId, assignment.WorkRole);
            
            _telemetry?.RecordWorkAssignment(assignment.WorkRole, workStopwatch.Elapsed);
            
            return roleId;
        }

        private static bool IsTransientException(Exception ex) =>
            ex is TaskCanceledException or TimeoutException ||
            (ex.InnerException is TaskCanceledException or TimeoutException);
        
        private static FailureReason DetermineFailureReason(Exception ex) => ex switch
        {
            ArgumentException => FailureReason.ValidationFailed,
            UnauthorizedAccessException => FailureReason.Unauthorized,
            TaskCanceledException or TimeoutException => FailureReason.TransientFailure,
            InvalidOperationException => FailureReason.ValidationFailed,
            _ => FailureReason.Unknown
        };
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

        public void LogRoleAssignmentCompleted(UserId userId, int successCount, int failureCount, TimeSpan duration)
        {
            Console.WriteLine($"[INFO] Completed role assignment for user {userId} - Success: {successCount}, Failed: {failureCount}, Duration: {duration.TotalMilliseconds:F2}ms");
        }

        public void LogRoleAssigning(StandardRoleType roleType, UserId userId)
        {
            Console.WriteLine($"[INFO] Assigning {roleType.GetDescription()} to user {userId}");
        }

        public void LogRoleAssigned(RoleId roleId, StandardRoleType roleType, UserId userId, TimeSpan duration)
        {
            Console.WriteLine($"[DEBUG] Successfully created role assignment {roleId} ({roleType}) for user {userId} in {duration.TotalMilliseconds:F2}ms");
        }

        public void LogRoleAssignmentFailed(StandardRoleType roleType, UserId userId, string error, FailureReason reason)
        {
            Console.WriteLine($"[WARN] Failed to assign {roleType} to user {userId}: {error} (Reason: {reason})");
        }

        public void LogWorkAssignmentCreated(SupervisorId supervisorId, WorkAssignmentRoleCode roleCode)
        {
            Console.WriteLine($"[REPO] Created work assignment - Supervisor: {supervisorId}, Role: {roleCode}");
        }
        
        public void LogRetryAttempt(int attempt, StandardRoleType roleType)
        {
            Console.WriteLine($"[RETRY] Attempt {attempt} for role {roleType}");
        }
    }

    public sealed class NoOpTelemetryCollector : ITelemetryCollector
    {
        public void RecordRoleAssignment(StandardRoleType roleType, bool success, TimeSpan duration) { }
        public void RecordWorkAssignment(WorkAssignmentRoleCode roleCode, TimeSpan duration) { }
        public void RecordRetry(StandardRoleType roleType, int attempt) { }
    }

    public sealed class DemoUserRoleRepository : IUserRoleRepository
    {
        private readonly HashSet<RoleId> _existingRoles = new();
        
        public Task CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[REPO] Creating user role - RoleId: {command.RoleId}, UserId: {command.UserId}");
            _existingRoles.Add(command.RoleId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<StandardRoleType>>(new HashSet<StandardRoleType>());
        }
        
        public Task<bool> ExistsAsync(RoleId roleId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_existingRoles.Contains(roleId));
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
            
        public Task<bool> ValidateSupervisorAsync(SupervisorId supervisorId, CancellationToken cancellationToken = default)
        {
            // In production, this would check if supervisor exists and is active
            return Task.FromResult(true);
        }
    }

    public sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation();
        
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    // ============================================
    // DEMO FACTORY - Simplified creation with DI container simulation
    // ============================================
    
    public static class DemoRoleAssignmentService
    {
        public static RoleAssignmentService_GoodWay Create(RoleAssignmentOptions? options = null)
        {
            var supervisorResolver = new DefaultSupervisorResolver();
            var finalOptions = options ?? RoleAssignmentOptions.Default;
            
            return new RoleAssignmentService_GoodWay(
                new DemoUserRoleRepository(),
                new DemoWorkAssignmentRepository(),
                supervisorResolver,
                new ConsoleRoleAssignmentLogger(),
                new StandardRoleAssignmentFactory(supervisorResolver, finalOptions),
                new NoOpTelemetryCollector(),
                new NoOpUnitOfWork(),
                finalOptions
            );
        }
        
        public static RoleAssignmentService_GoodWay CreateWithCustomOptions()
        {
            var options = new RoleAssignmentOptions
            {
                ValidateExistingRoles = true,
                AllowPartialFailures = false,
                TransactionTimeout = TimeSpan.FromMinutes(2),
                MaxRetryAttempts = 2,
                RetryDelay = TimeSpan.FromMilliseconds(500),
                EnableTelemetry = true,
                RequireApprovalCheck = true,
                SpecialDepartments = new HashSet<DepartmentId> 
                { 
                    DepartmentId.From("SPECIAL-DEPT"), 
                    DepartmentId.From("VIP-DEPT") 
                }
            };
            
            return Create(options);
        }
    }
}
