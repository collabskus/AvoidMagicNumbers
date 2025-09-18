// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment with DI
// ============================================

namespace ModernApproach
{
    // ============================================
    // STRONG TYPES - Prevent primitive obsession
    // ============================================
    
    public record UserId(string Value)
    {
        public static implicit operator string(UserId userId) => userId.Value;
        public override string ToString() => Value;
    }
    
    public sealed record DepartmentId(string Value) : IEquatable<DepartmentId>
    {
        public static implicit operator string(DepartmentId departmentId) => departmentId.Value;
        public override string ToString() => Value;
        
        // Improved equality for case-insensitive comparison
        public bool Equals(DepartmentId? other) =>
            other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        
        public override int GetHashCode() => 
            StringComparer.OrdinalIgnoreCase.GetHashCode(Value);
    }
    
    public record SupervisorId(string Value)
    {
        public static implicit operator string(SupervisorId supervisorId) => supervisorId.Value;
        public override string ToString() => Value;
    }
    
    public record RoleId(Guid Value)
    {
        public static RoleId New() => new(Guid.NewGuid());
        public static implicit operator Guid(RoleId roleId) => roleId.Value;
        public override string ToString() => Value.ToString();
    }
    
    public record AssignmentId(Guid Value)
    {
        public static AssignmentId New() => new(Guid.NewGuid());
        public static implicit operator Guid(AssignmentId assignmentId) => assignmentId.Value;
        public override string ToString() => Value.ToString();
    }

    // ============================================
    // ENUMS - Document business meaning
    // ============================================
    
    public enum StandardRoleType
    {
        DepartmentManager = 6,    // Maintaining original DB values for compatibility
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
    // CONSTANTS - Centralized configuration
    // ============================================
    
    public static class RoleAssignmentConstants
    {
        public const string ManagerPrefix = "manager-";
        public const string CoordinatorPrefix = "coordinator-";
        
        public static class Descriptions
        {
            public const string DepartmentManager = "Department Manager - oversees department operations";
            public const string ProjectCoordinator = "Project Coordinator - manages project workflows";
        }
        
        public static class ValidationMessages
        {
            public const string UserAlreadyHasRole = "User already has role {0}";
            public const string SupervisorNotFound = "Supervisor {0} not found";
            public const string InvalidUserId = "User ID cannot be null or empty";
            public const string InvalidDepartmentId = "Department ID cannot be null or empty";
        }
    }

    // ============================================
    // RESULT TYPES - Better error handling
    // ============================================
    
    public sealed record Result<T>
    {
        private Result(T? value, string? error)
        {
            Value = value;
            Error = error;
            IsSuccess = error is null;
        }
        
        public T? Value { get; }
        public string? Error { get; }
        public bool IsSuccess { get; }
        
        public static Result<T> Success(T value) => new(value, null);
        public static Result<T> Failure(string error) => new(default, error);
    }

    public sealed record RoleAssignmentSuccess(RoleId RoleId, StandardRoleType RoleType);
    public sealed record RoleAssignmentFailure(StandardRoleType RoleType, string Error);
    
    public sealed record RoleAssignmentResult
    {
        public IReadOnlyList<RoleAssignmentSuccess> Successes { get; init; } = Array.Empty<RoleAssignmentSuccess>();
        public IReadOnlyList<RoleAssignmentFailure> Failures { get; init; } = Array.Empty<RoleAssignmentFailure>();
        
        public bool IsFullySuccessful => Failures.Count == 0 && Successes.Count > 0;
        public bool IsPartiallySuccessful => Successes.Count > 0 && Failures.Count > 0;
        public bool IsCompleteFailure => Successes.Count == 0 && Failures.Count > 0;
        
        public static RoleAssignmentResult Success(params RoleAssignmentSuccess[] successes) => 
            new() { Successes = successes.ToList().AsReadOnly() };
        
        public static RoleAssignmentResult Failure(params RoleAssignmentFailure[] failures) => 
            new() { Failures = failures.ToList().AsReadOnly() };
            
        public static RoleAssignmentResult Mixed(
            IEnumerable<RoleAssignmentSuccess> successes, 
            IEnumerable<RoleAssignmentFailure> failures) => 
            new() { 
                Successes = successes.ToList().AsReadOnly(),
                Failures = failures.ToList().AsReadOnly()
            };
    }

    public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
    {
        public static ValidationResult Success() => new(true, Array.Empty<string>());
        public static ValidationResult Failure(params string[] errors) => 
            new(false, errors.ToList().AsReadOnly());
    }

    // ============================================
    // DATA STRUCTURES - Immutable domain models
    // ============================================
    
    public record RoleAssignment(
        StandardRoleType RoleType,
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode WorkRole,
        string Description
    );

    public record CreateUserRoleCommand(
        RoleId RoleId,
        AssignmentId AssignmentId,
        UserId UserId,
        DepartmentId DepartmentId,
        DateTime AssignedDate,
        StandardRoleType RoleType,
        SupervisorId SupervisorId
    );

    public record WorkAssignmentCommand(
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode RoleCode,
        UserId UserId,
        ResourceType ResourceType,
        DateTime CreatedDate
    );

    // ============================================
    // CONFIGURATION - Externalized settings
    // ============================================
    
    public sealed class RoleAssignmentConfiguration
    {
        private TimeSpan _transactionTimeout = TimeSpan.FromMinutes(5);
        
        public IReadOnlySet<DepartmentId> SpecialDepartmentCodes { get; init; } = 
            new HashSet<DepartmentId> { new("SPECIAL-DEPT") };
        
        public bool IsVerboseLoggingEnabled { get; init; }
        public bool ValidateExistingRoles { get; init; } = true;
        public bool AllowPartialFailures { get; init; } = false;
        
        public TimeSpan TransactionTimeout
        {
            get => _transactionTimeout;
            init => _transactionTimeout = value > TimeSpan.Zero ? value : 
                throw new ArgumentOutOfRangeException(nameof(value), "Timeout must be positive");
        }
    }

    // ============================================
    // INTERFACES - Dependency abstractions
    // ============================================
    
    public interface IUserRoleRepository
    {
        Task<int> CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default);
        Task<int> CreateUserRolesAsync(IEnumerable<CreateUserRoleCommand> commands, CancellationToken cancellationToken = default);
        Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default);
    }

    public interface IWorkAssignmentRepository
    {
        Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default);
        Task<int> CreateWorkAssignmentsAsync(IEnumerable<WorkAssignmentCommand> commands, CancellationToken cancellationToken = default);
    }

    public interface ISupervisorService
    {
        Task<SupervisorId> GetDepartmentManagerAsync(DepartmentId departmentId, CancellationToken cancellationToken = default);
        Task<SupervisorId> GetProjectCoordinatorAsync(DepartmentId departmentId, CancellationToken cancellationToken = default);
        Task<bool> SupervisorExistsAsync(SupervisorId supervisorId, CancellationToken cancellationToken = default);
    }

    public interface ILogger
    {
        void LogInformation(string message, params object[] args);
        void LogDebug(string message, params object[] args);
        void LogError(Exception ex, string message, params object[] args);
        void LogWarning(string message, params object[] args);
    }

    public interface IUnitOfWork : IDisposable
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
        CancellationToken Token { get; }
    }

    public interface IRoleAssignmentFactory
    {
        Task<IReadOnlyList<RoleAssignment>> CreateStandardAssignmentsAsync(DepartmentId departmentId, CancellationToken cancellationToken = default);
    }

    // ============================================
    // EXTENSION METHODS - Clean code helpers
    // ============================================
    
    public static class RoleAssignmentExtensions
    {
        public static bool RequiresSpecialHandling(this StandardRoleType roleType)
        {
            return roleType == StandardRoleType.ProjectCoordinator;
        }
        
        public static string GetDisplayName(this StandardRoleType roleType)
        {
            return roleType switch
            {
                StandardRoleType.DepartmentManager => "Department Manager",
                StandardRoleType.ProjectCoordinator => "Project Coordinator",
                _ => roleType.ToString()
            };
        }
    }

    // ============================================
    // OPTIONS PATTERN - Reduce constructor complexity
    // ============================================
    
    public sealed class RoleAssignmentServiceOptions
    {
        public required IUserRoleRepository UserRoleRepository { get; init; }
        public required IWorkAssignmentRepository WorkAssignmentRepository { get; init; }
        public required ISupervisorService SupervisorService { get; init; }
        public required IRoleAssignmentFactory RoleAssignmentFactory { get; init; }
        public required IUnitOfWork UnitOfWork { get; init; }
        public required ILogger Logger { get; init; }
        public required RoleAssignmentConfiguration Configuration { get; init; }
    }

    // ============================================
    // FACTORY - Creates role assignments
    // ============================================
    
    public sealed class RoleAssignmentFactory : IRoleAssignmentFactory
    {
        private readonly ISupervisorService supervisorService;
        private readonly RoleAssignmentConfiguration configuration;
        
        public RoleAssignmentFactory(
            ISupervisorService supervisorService,
            RoleAssignmentConfiguration configuration)
        {
            this.supervisorService = supervisorService ?? throw new ArgumentNullException(nameof(supervisorService));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }
        
        public async Task<IReadOnlyList<RoleAssignment>> CreateStandardAssignmentsAsync(
            DepartmentId departmentId, 
            CancellationToken cancellationToken = default)
        {
            var assignments = new List<RoleAssignment>();
            
            // Department Manager Role
            var managerSupervisor = await supervisorService.GetDepartmentManagerAsync(departmentId, cancellationToken);
            assignments.Add(new RoleAssignment(
                StandardRoleType.DepartmentManager,
                managerSupervisor,
                WorkAssignmentRoleCode.ProjectManager,
                RoleAssignmentConstants.Descriptions.DepartmentManager
            ));
            
            // Project Coordinator Role
            var coordinatorSupervisor = await supervisorService.GetProjectCoordinatorAsync(departmentId, cancellationToken);
            var coordinatorWorkRole = GetCoordinatorWorkRole(departmentId);
            assignments.Add(new RoleAssignment(
                StandardRoleType.ProjectCoordinator,
                coordinatorSupervisor,
                coordinatorWorkRole,
                RoleAssignmentConstants.Descriptions.ProjectCoordinator
            ));
            
            return assignments.AsReadOnly();
        }
        
        private WorkAssignmentRoleCode GetCoordinatorWorkRole(DepartmentId departmentId)
        {
            var isSpecialDepartment = configuration.SpecialDepartmentCodes.Contains(departmentId);
            return isSpecialDepartment
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;
        }
    }

    // ============================================
    // SERVICE - Main business logic with improved DI
    // ============================================
    
    public sealed class RoleAssignmentService_GoodWay
    {
        private readonly RoleAssignmentServiceOptions options;

        // Simplified constructor using options pattern
        public RoleAssignmentService_GoodWay(RoleAssignmentServiceOptions options)
        {
            this.options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // Main public method - async with improved result pattern
        public async Task<RoleAssignmentResult> AssignStandardRolesAsync(
            UserId userId, 
            DepartmentId departmentId, 
            DateTime assignedDate,
            CancellationToken cancellationToken = default)
        {
            ValidateInputs(userId, departmentId);

            options.Logger.LogInformation(
                "Starting role assignment for user {UserId} in department {DepartmentId}",
                userId.Value,
                departmentId.Value);

            using var timeoutCts = new CancellationTokenSource(options.Configuration.TransactionTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
            var token = linkedCts.Token;

            try
            {
                using var transaction = await StartTransactionAsync(token);
                
                var roleAssignments = await options.RoleAssignmentFactory.CreateStandardAssignmentsAsync(departmentId, token);
                
                if (!roleAssignments.Any())
                {
                    options.Logger.LogWarning("No standard role assignments defined for department {DepartmentId}", departmentId.Value);
                    return RoleAssignmentResult.Success(); // Consider if this should be failure
                }
                
                var results = await ProcessRoleAssignmentsAsync(userId, departmentId, assignedDate, roleAssignments, token);
                
                if (ShouldCommitTransaction(results))
                {
                    await transaction.CommitAsync();
                    options.Logger.LogInformation(
                        "Successfully assigned {SuccessCount} roles to user {UserId} (Failed: {FailureCount})",
                        results.Successes.Count,
                        userId.Value,
                        results.Failures.Count);
                }
                else
                {
                    await transaction.RollbackAsync();
                    options.Logger.LogWarning(
                        "Transaction rolled back for user {UserId} due to failures",
                        userId.Value);
                }
                
                return results;
            }
            catch (OperationCanceledException)
            {
                options.Logger.LogWarning("Role assignment operation was cancelled for user {UserId}", userId.Value);
                return RoleAssignmentResult.Failure(new RoleAssignmentFailure(StandardRoleType.DepartmentManager, "Operation was cancelled"));
            }
            catch (Exception ex)
            {
                options.Logger.LogError(ex, 
                    "Failed to assign roles for user {UserId} in department {DepartmentId}",
                    userId.Value,
                    departmentId.Value);
                
                return RoleAssignmentResult.Failure(new RoleAssignmentFailure(StandardRoleType.DepartmentManager, $"Role assignment failed: {ex.Message}"));
            }
        }

        // Synchronous overload for backward compatibility
        public void AssignStandardRoles(UserId userId, DepartmentId departmentId, DateTime assignedDate)
        {
            var result = AssignStandardRolesAsync(userId, departmentId, assignedDate).GetAwaiter().GetResult();
            if (result.IsCompleteFailure)
            {
                var errors = string.Join(", ", result.Failures.Select(f => f.Error));
                throw new InvalidOperationException($"Role assignment failed: {errors}");
            }
        }

        private static void ValidateInputs(UserId userId, DepartmentId departmentId)
        {
            ArgumentNullException.ThrowIfNull(userId);
            ArgumentNullException.ThrowIfNull(departmentId);
            
            if (string.IsNullOrWhiteSpace(userId.Value))
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            
            if (string.IsNullOrWhiteSpace(departmentId.Value))
                throw new ArgumentException("Department ID cannot be null or empty", nameof(departmentId));
        }

        private async Task<IUnitOfWork> StartTransactionAsync(CancellationToken cancellationToken)
        {
            await options.UnitOfWork.BeginTransactionAsync(cancellationToken);
            return options.UnitOfWork;
        }

        private async Task<RoleAssignmentResult> ProcessRoleAssignmentsAsync(
            UserId userId,
            DepartmentId departmentId,
            DateTime assignedDate,
            IReadOnlyList<RoleAssignment> roleAssignments,
            CancellationToken cancellationToken)
        {
            var successes = new List<RoleAssignmentSuccess>();
            var failures = new List<RoleAssignmentFailure>();

            // Get existing role types in one call for efficiency
            var existingRoleTypes = options.Configuration.ValidateExistingRoles
                ? await options.UserRoleRepository.GetExistingRoleTypesAsync(userId, cancellationToken)
                : new HashSet<StandardRoleType>();

            foreach (var assignment in roleAssignments)
            {
                try
                {
                    var validation = await ValidateRoleAssignmentAsync(userId, assignment, existingRoleTypes, cancellationToken);
                    if (!validation.IsValid)
                    {
                        var error = string.Join(", ", validation.Errors);
                        options.Logger.LogWarning(
                            "Validation failed for role {RoleType}: {Error}",
                            assignment.RoleType,
                            error);
                        
                        failures.Add(new RoleAssignmentFailure(assignment.RoleType, error));
                        
                        if (!options.Configuration.AllowPartialFailures)
                        {
                            break; // Stop processing on first failure if not allowing partial failures
                        }
                        continue;
                    }
                    
                    var roleId = await ProcessSingleRoleAssignmentAsync(userId, departmentId, assignedDate, assignment, cancellationToken);
                    successes.Add(new RoleAssignmentSuccess(roleId, assignment.RoleType));
                }
                catch (Exception ex)
                {
                    options.Logger.LogError(ex,
                        "Failed to process role assignment {RoleType} for user {UserId}",
                        assignment.RoleType,
                        userId.Value);
                    
                    failures.Add(new RoleAssignmentFailure(assignment.RoleType, ex.Message));
                    
                    if (!options.Configuration.AllowPartialFailures)
                    {
                        break;
                    }
                }
            }

            return RoleAssignmentResult.Mixed(successes, failures);
        }

        private bool ShouldCommitTransaction(RoleAssignmentResult result)
        {
            return options.Configuration.AllowPartialFailures 
                ? result.Successes.Any() 
                : result.IsFullySuccessful;
        }

        private async Task<ValidationResult> ValidateRoleAssignmentAsync(
            UserId userId,
            RoleAssignment assignment,
            IReadOnlySet<StandardRoleType> existingRoleTypes,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            
            // Check if user already has this role (using cached result)
            if (options.Configuration.ValidateExistingRoles && existingRoleTypes.Contains(assignment.RoleType))
            {
                errors.Add(string.Format(
                    RoleAssignmentConstants.ValidationMessages.UserAlreadyHasRole,
                    assignment.RoleType.GetDisplayName()));
            }
            
            // Validate supervisor exists
            if (!await options.SupervisorService.SupervisorExistsAsync(assignment.SupervisorId, cancellationToken))
            {
                errors.Add(string.Format(
                    RoleAssignmentConstants.ValidationMessages.SupervisorNotFound,
                    assignment.SupervisorId.Value));
            }
            
            return errors.Count > 0 
                ? ValidationResult.Failure(errors.ToArray()) 
                : ValidationResult.Success();
        }

        private async Task<RoleId> ProcessSingleRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            DateTime assignedDate, 
            RoleAssignment assignment,
            CancellationToken cancellationToken)
        {
            var roleId = RoleId.New();
            var assignmentId = AssignmentId.New();
            
            // Create strongly-typed command object
            var command = new CreateUserRoleCommand(
                roleId,
                assignmentId,
                userId,
                departmentId,
                assignedDate,
                assignment.RoleType,
                assignment.SupervisorId
            );

            options.Logger.LogInformation(
                "Assigning {RoleType} role to user {UserId}",
                assignment.RoleType.GetDisplayName(),
                userId.Value);
            
            if (options.Configuration.IsVerboseLoggingEnabled)
            {
                options.Logger.LogDebug(
                    "Role assignment details - RoleId: {RoleId}, SupervisorId: {SupervisorId}, Description: {Description}",
                    roleId.Value,
                    assignment.SupervisorId.Value,
                    assignment.Description);
            }

            // Execute database command through repository
            await options.UserRoleRepository.CreateUserRoleAsync(command, cancellationToken);
            
            // Create work assignment
            var workCommand = new WorkAssignmentCommand(
                assignment.SupervisorId,
                assignment.WorkRole,
                userId,
                ResourceType.UserAccount,
                assignedDate
            );
            
            await options.WorkAssignmentRepository.CreateWorkAssignmentAsync(workCommand, cancellationToken);
            
            options.Logger.LogDebug(
                "Successfully created role assignment {RoleId} for user {UserId}",
                roleId.Value,
                userId.Value);
            
            return roleId;
        }
    }

    // ============================================
    // DEFAULT IMPLEMENTATIONS - For demo/testing
    // ============================================
    
    // Simple console logger for demonstration
    public sealed class ConsoleLogger : ILogger
    {
        public void LogInformation(string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"[INFO] {string.Format(message, args)}");
            }
            catch (FormatException)
            {
                Console.WriteLine($"[INFO] {message}");
            }
        }

        public void LogDebug(string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"[DEBUG] {string.Format(message, args)}");
            }
            catch (FormatException)
            {
                Console.WriteLine($"[DEBUG] {message}");
            }
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"[ERROR] {string.Format(message, args)} - Exception: {ex.Message}");
            }
            catch (FormatException)
            {
                Console.WriteLine($"[ERROR] {message} - Exception: {ex.Message}");
            }
        }

        public void LogWarning(string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"[WARN] {string.Format(message, args)}");
            }
            catch (FormatException)
            {
                Console.WriteLine($"[WARN] {message}");
            }
        }
    }

    // Demo implementations for testing without actual database
    public sealed class DemoUserRoleRepository : IUserRoleRepository
    {
        public Task<int> CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"  [REPO] Creating user role - RoleId: {command.RoleId.Value}, UserId: {command.UserId.Value}");
            return Task.FromResult(1);
        }

        public Task<int> CreateUserRolesAsync(IEnumerable<CreateUserRoleCommand> commands, CancellationToken cancellationToken = default)
        {
            var count = 0;
            foreach (var command in commands)
            {
                Console.WriteLine($"  [REPO] Batch creating user role - RoleId: {command.RoleId.Value}, UserId: {command.UserId.Value}");
                count++;
            }
            return Task.FromResult(count);
        }

        public Task<IReadOnlySet<StandardRoleType>> GetExistingRoleTypesAsync(UserId userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlySet<StandardRoleType>>(new HashSet<StandardRoleType>()); // Always return empty for demo
        }
    }

    public sealed class DemoWorkAssignmentRepository : IWorkAssignmentRepository
    {
        public Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"  [REPO] Creating work assignment - Supervisor: {command.SupervisorId.Value}, Role: {command.RoleCode}");
            return Task.FromResult(1);
        }

        public Task<int> CreateWorkAssignmentsAsync(IEnumerable<WorkAssignmentCommand> commands, CancellationToken cancellationToken = default)
        {
            var count = 0;
            foreach (var command in commands)
            {
                Console.WriteLine($"  [REPO] Batch creating work assignment - Supervisor: {command.SupervisorId.Value}, Role: {command.RoleCode}");
                count++;
            }
            return Task.FromResult(count);
        }
    }

    public sealed class DemoSupervisorService : ISupervisorService
    {
        public Task<SupervisorId> GetDepartmentManagerAsync(DepartmentId departmentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SupervisorId($"{RoleAssignmentConstants.ManagerPrefix}{departmentId.Value}"));
        }

        public Task<SupervisorId> GetProjectCoordinatorAsync(DepartmentId departmentId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new SupervisorId($"{RoleAssignmentConstants.CoordinatorPrefix}{departmentId.Value}"));
        }

        public Task<bool> SupervisorExistsAsync(SupervisorId supervisorId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // Always return true for demo
        }
    }

    public sealed class DemoUnitOfWork : IUnitOfWork
    {
        public CancellationToken Token { get; private set; }

        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            Console.WriteLine("  [UOW] Beginning transaction");
            return Task.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("  [UOW] Committing transaction");
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("  [UOW] Rolling back transaction");
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            // No-op for demo
        }
    }

    // ============================================
    // DEMO FACTORY - For backward compatibility
    // ============================================
    
    public sealed class DemoRoleAssignmentService
    {
        public static RoleAssignmentService_GoodWay Create()
        {
            var configuration = new RoleAssignmentConfiguration();
            var logger = new ConsoleLogger();
            var supervisorService = new DemoSupervisorService();
            
            var options = new RoleAssignmentServiceOptions
            {
                UserRoleRepository = new DemoUserRoleRepository(),
                WorkAssignmentRepository = new DemoWorkAssignmentRepository(),
                SupervisorService = supervisorService,
                RoleAssignmentFactory = new RoleAssignmentFactory(supervisorService, configuration),
                UnitOfWork = new DemoUnitOfWork(),
                Logger = logger,
                Configuration = configuration
            };
            
            return new RoleAssignmentService_GoodWay(options);
        }
    }
}
