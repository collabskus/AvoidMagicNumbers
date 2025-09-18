// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment with DI
// ============================================

using System.ComponentModel.DataAnnotations;

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
    
    public record DepartmentId(string Value) : IEquatable<DepartmentId>
    {
        public static implicit operator string(DepartmentId departmentId) => departmentId.Value;
        public override string ToString() => Value;
        
        // Custom equality for case-insensitive comparison
        public bool Equals(DepartmentId? other)
        {
            return other is not null && 
                   string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }
        
        public override int GetHashCode() => 
            Value.ToUpperInvariant().GetHashCode();
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
        [Display(Name = "Department Manager", Description = "Oversees department operations")]
        DepartmentManager = 6,    // Maintaining original DB values for compatibility
        
        [Display(Name = "Project Coordinator", Description = "Manages project workflows")]
        ProjectCoordinator = 7
    }

    public enum WorkAssignmentRoleCode
    {
        [Display(Name = "Project Manager")]
        ProjectManager = 101,
        
        [Display(Name = "General Administrator")]
        GeneralAdministrator = 203,
        
        [Display(Name = "Special Administrator")]
        SpecialAdministrator = 202
    }

    public enum ResourceType
    {
        [Display(Name = "User Account")]
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
    // DATA STRUCTURES - Immutable domain models
    // ============================================
    
    public record RoleAssignment(
        StandardRoleType RoleType,
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode WorkRole,
        string Description
    )
    {
        // Validation at construction
        public RoleAssignment
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(SupervisorId.Value, nameof(SupervisorId));
            ArgumentException.ThrowIfNullOrWhiteSpace(Description, nameof(Description));
        }
    }

    public record CreateUserRoleCommand(
        RoleId RoleId,
        AssignmentId AssignmentId,
        UserId UserId,
        DepartmentId DepartmentId,
        DateTime AssignedDate,
        StandardRoleType RoleType,
        SupervisorId SupervisorId
    )
    {
        // Validation at construction
        public CreateUserRoleCommand
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(UserId.Value, nameof(UserId));
            ArgumentException.ThrowIfNullOrWhiteSpace(DepartmentId.Value, nameof(DepartmentId));
            ArgumentException.ThrowIfNullOrWhiteSpace(SupervisorId.Value, nameof(SupervisorId));
        }
    }

    public record WorkAssignmentCommand(
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode RoleCode,
        UserId UserId,
        ResourceType ResourceType,
        DateTime CreatedDate
    )
    {
        // Validation at construction
        public WorkAssignmentCommand
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(SupervisorId.Value, nameof(SupervisorId));
            ArgumentException.ThrowIfNullOrWhiteSpace(UserId.Value, nameof(UserId));
        }
    }

    public sealed record RoleAssignmentResult
    {
        public bool IsSuccess { get; init; }
        public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
        public IReadOnlyList<RoleId> AssignedRoleIds { get; init; } = Array.Empty<RoleId>();
        
        public static RoleAssignmentResult Success(params RoleId[] roleIds) => 
            new() { IsSuccess = true, AssignedRoleIds = roleIds.ToList().AsReadOnly() };
        
        public static RoleAssignmentResult Failure(params string[] errors) => 
            new() { IsSuccess = false, Errors = errors.ToList().AsReadOnly() };
    }

    public sealed record ValidationResult(bool IsValid, IReadOnlyList<string> Errors)
    {
        public static ValidationResult Success() => new(true, Array.Empty<string>());
        public static ValidationResult Failure(params string[] errors) => 
            new(false, errors.ToList().AsReadOnly());
    }

    // ============================================
    // CONFIGURATION - Externalized settings
    // ============================================
    
    public sealed class RoleAssignmentConfiguration
    {
        public IReadOnlySet<DepartmentId> SpecialDepartmentCodes { get; init; } = 
            new HashSet<DepartmentId> { new("SPECIAL-DEPT") };
        public bool IsVerboseLoggingEnabled { get; init; }
        public bool ValidateExistingRoles { get; init; } = true;
        public TimeSpan TransactionTimeout { get; init; } = TimeSpan.FromMinutes(5);
    }

    // ============================================
    // INTERFACES - Dependency abstractions
    // ============================================
    
    public interface IUserRoleRepository
    {
        Task<int> CreateUserRoleAsync(CreateUserRoleCommand command, CancellationToken cancellationToken = default);
        Task<bool> UserRoleExistsAsync(UserId userId, StandardRoleType roleType, CancellationToken cancellationToken = default);
    }

    public interface IWorkAssignmentRepository
    {
        Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default);
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

    public interface IUnitOfWork
    {
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
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
            var field = roleType.GetType().GetField(roleType.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();
            
            return displayAttribute?.Name ?? roleType.ToString();
        }
        
        public static string GetDescription(this StandardRoleType roleType)
        {
            var field = roleType.GetType().GetField(roleType.ToString());
            var displayAttribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                .Cast<DisplayAttribute>()
                .FirstOrDefault();
            
            return displayAttribute?.Description ?? string.Empty;
        }
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
                StandardRoleType.DepartmentManager.GetDescription()
            ));
            
            // Project Coordinator Role
            var coordinatorSupervisor = await supervisorService.GetProjectCoordinatorAsync(departmentId, cancellationToken);
            var coordinatorWorkRole = GetCoordinatorWorkRole(departmentId);
            assignments.Add(new RoleAssignment(
                StandardRoleType.ProjectCoordinator,
                coordinatorSupervisor,
                coordinatorWorkRole,
                StandardRoleType.ProjectCoordinator.GetDescription()
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
    // SERVICE - Main business logic with DI
    // ============================================
    
    public sealed class RoleAssignmentService_GoodWay
    {
        private readonly IUserRoleRepository userRoleRepository;
        private readonly IWorkAssignmentRepository workAssignmentRepository;
        private readonly ISupervisorService supervisorService;
        private readonly IRoleAssignmentFactory roleAssignmentFactory;
        private readonly IUnitOfWork unitOfWork;
        private readonly ILogger logger;
        private readonly RoleAssignmentConfiguration configuration;

        // Constructor with dependency injection
        public RoleAssignmentService_GoodWay(
            IUserRoleRepository userRoleRepository,
            IWorkAssignmentRepository workAssignmentRepository,
            ISupervisorService supervisorService,
            IRoleAssignmentFactory roleAssignmentFactory,
            IUnitOfWork unitOfWork,
            ILogger logger,
            RoleAssignmentConfiguration configuration)
        {
            this.userRoleRepository = userRoleRepository ?? throw new ArgumentNullException(nameof(userRoleRepository));
            this.workAssignmentRepository = workAssignmentRepository ?? throw new ArgumentNullException(nameof(workAssignmentRepository));
            this.supervisorService = supervisorService ?? throw new ArgumentNullException(nameof(supervisorService));
            this.roleAssignmentFactory = roleAssignmentFactory ?? throw new ArgumentNullException(nameof(roleAssignmentFactory));
            this.unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        // Main public method - now async with result pattern and cancellation support
        public async Task<RoleAssignmentResult> AssignStandardRolesAsync(
            UserId userId, 
            DepartmentId departmentId, 
            DateTime assignedDate,
            CancellationToken cancellationToken = default)
        {
            // Input validation
            ArgumentNullException.ThrowIfNull(userId);
            ArgumentNullException.ThrowIfNull(departmentId);
            ArgumentException.ThrowIfNullOrWhiteSpace(userId.Value, nameof(userId));
            ArgumentException.ThrowIfNullOrWhiteSpace(departmentId.Value, nameof(departmentId));

            logger.LogInformation(
                "Starting role assignment for user {0} in department {1}",
                userId.Value,
                departmentId.Value);

            var assignedRoleIds = new List<RoleId>();

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(configuration.TransactionTimeout);

                await unitOfWork.BeginTransactionAsync(cts.Token);
                
                // Get the roles this user should have
                var roleAssignments = await roleAssignmentFactory.CreateStandardAssignmentsAsync(departmentId, cts.Token);
                
                // Process each role with validation
                foreach (var assignment in roleAssignments)
                {
                    var validation = await ValidateRoleAssignmentAsync(userId, departmentId, assignment, cts.Token);
                    if (!validation.IsValid)
                    {
                        logger.LogWarning(
                            "Validation failed for role {0}: {1}",
                            assignment.RoleType,
                            string.Join(", ", validation.Errors));
                        
                        if (configuration.ValidateExistingRoles)
                        {
                            await unitOfWork.RollbackAsync(cts.Token);
                            return RoleAssignmentResult.Failure(validation.Errors.ToArray());
                        }
                        continue;
                    }
                    
                    var roleId = await ProcessSingleRoleAssignmentAsync(userId, departmentId, assignedDate, assignment, cts.Token);
                    if (roleId != null)
                    {
                        assignedRoleIds.Add(roleId);
                    }
                }
                
                await unitOfWork.CommitAsync(cts.Token);
                
                logger.LogInformation(
                    "Successfully assigned {0} roles to user {1}",
                    assignedRoleIds.Count,
                    userId.Value);
                
                return RoleAssignmentResult.Success(assignedRoleIds.ToArray());
            }
            catch (OperationCanceledException)
            {
                logger.LogWarning("Role assignment operation was cancelled for user {0}", userId.Value);
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return RoleAssignmentResult.Failure("Operation was cancelled");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to assign roles for user {0} in department {1}",
                    userId.Value,
                    departmentId.Value);
                
                await unitOfWork.RollbackAsync(CancellationToken.None);
                return RoleAssignmentResult.Failure($"Role assignment failed: {ex.Message}");
            }
        }

        private async Task<ValidationResult> ValidateRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            RoleAssignment assignment,
            CancellationToken cancellationToken)
        {
            var errors = new List<string>();
            
            // Check if user already has this role
            if (configuration.ValidateExistingRoles)
            {
                if (await userRoleRepository.UserRoleExistsAsync(userId, assignment.RoleType, cancellationToken))
                {
                    errors.Add(string.Format(
                        RoleAssignmentConstants.ValidationMessages.UserAlreadyHasRole,
                        assignment.RoleType.GetDisplayName()));
                }
            }
            
            // Validate supervisor exists
            if (!await supervisorService.SupervisorExistsAsync(assignment.SupervisorId, cancellationToken))
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

            logger.LogInformation(
                "Assigning {0} role to user {1}",
                assignment.RoleType.GetDisplayName(),
                userId.Value);
            
            if (configuration.IsVerboseLoggingEnabled)
            {
                logger.LogDebug(
                    "Role assignment details - RoleId: {0}, SupervisorId: {1}, Description: {2}",
                    roleId.Value,
                    assignment.SupervisorId.Value,
                    assignment.Description);
            }

            try
            {
                // Execute database command through repository
                await userRoleRepository.CreateUserRoleAsync(command, cancellationToken);
                
                // Create work assignment
                var workCommand = new WorkAssignmentCommand(
                    assignment.SupervisorId,
                    assignment.WorkRole,
                    userId,
                    ResourceType.UserAccount,
                    assignedDate
                );
                
                await workAssignmentRepository.CreateWorkAssignmentAsync(workCommand, cancellationToken);
                
                logger.LogDebug(
                    "Successfully created role assignment {0} for user {1}",
                    roleId.Value,
                    userId.Value);
                
                return roleId;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to process role assignment {0} for user {1}",
                    assignment.RoleType,
                    userId.Value);
                throw;
            }
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

        public Task<bool> UserRoleExistsAsync(UserId userId, StandardRoleType roleType, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false); // Always return false for demo
        }
    }

    public sealed class DemoWorkAssignmentRepository : IWorkAssignmentRepository
    {
        public Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"  [REPO] Creating work assignment - Supervisor: {command.SupervisorId.Value}, Role: {command.RoleCode}");
            return Task.FromResult(1);
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
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
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
            
            return new RoleAssignmentService_GoodWay(
                new DemoUserRoleRepository(),
                new DemoWorkAssignmentRepository(),
                supervisorService,
                new RoleAssignmentFactory(supervisorService, configuration),
                new DemoUnitOfWork(),
                logger,
                configuration
            );
        }
    }
}
