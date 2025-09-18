// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment with DI
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ModernApproach
{
    // ============================================
    // STRONG TYPES - Prevent primitive obsession
    // ============================================
    
    public record UserId(string Value);
    public record DepartmentId(string Value);
    public record SupervisorId(string Value);
    public record RoleId(Guid Value)
    {
        public static RoleId New() => new(Guid.NewGuid());
    }
    public record AssignmentId(Guid Value)
    {
        public static AssignmentId New() => new(Guid.NewGuid());
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

    public record RoleAssignmentResult
    {
        public bool IsSuccess { get; init; }
        public string[] Errors { get; init; } = Array.Empty<string>();
        public RoleId? AssignedRoleId { get; init; }
        public List<RoleId> AssignedRoleIds { get; init; } = new();
        
        public static RoleAssignmentResult Success(params RoleId[] roleIds) => 
            new() { IsSuccess = true, AssignedRoleIds = roleIds.ToList() };
        
        public static RoleAssignmentResult Failure(params string[] errors) => 
            new() { IsSuccess = false, Errors = errors };
    }

    public record ValidationResult(bool IsValid, List<string> Errors)
    {
        public static ValidationResult Success() => new(true, new List<string>());
        public static ValidationResult Failure(params string[] errors) => new(false, errors.ToList());
    }

    // ============================================
    // CONFIGURATION - Externalized settings
    // ============================================
    
    public class RoleAssignmentConfiguration
    {
        public DepartmentId[] SpecialDepartmentCodes { get; set; } = 
            { new DepartmentId("SPECIAL-DEPT") };
        public bool IsVerboseLoggingEnabled { get; set; }
        public bool ValidateExistingRoles { get; set; } = true;
    }

    // ============================================
    // INTERFACES - Dependency abstractions
    // ============================================
    
    public interface IUserRoleRepository
    {
        Task<int> CreateUserRoleAsync(CreateUserRoleCommand command);
        Task<bool> UserRoleExistsAsync(UserId userId, StandardRoleType roleType);
    }

    public interface IWorkAssignmentRepository
    {
        Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command);
    }

    public interface ISupervisorService
    {
        Task<SupervisorId> GetDepartmentManagerAsync(DepartmentId departmentId);
        Task<SupervisorId> GetProjectCoordinatorAsync(DepartmentId departmentId);
        Task<bool> SupervisorExistsAsync(SupervisorId supervisorId);
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
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
    }

    public interface IRoleAssignmentFactory
    {
        Task<List<RoleAssignment>> CreateStandardAssignmentsAsync(DepartmentId departmentId);
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
    // FACTORY - Creates role assignments
    // ============================================
    
    public class RoleAssignmentFactory : IRoleAssignmentFactory
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
        
        public async Task<List<RoleAssignment>> CreateStandardAssignmentsAsync(DepartmentId departmentId)
        {
            var assignments = new List<RoleAssignment>();
            
            // Department Manager Role
            var managerSupervisor = await supervisorService.GetDepartmentManagerAsync(departmentId);
            assignments.Add(new RoleAssignment(
                StandardRoleType.DepartmentManager,
                managerSupervisor,
                WorkAssignmentRoleCode.ProjectManager,
                RoleAssignmentConstants.Descriptions.DepartmentManager
            ));
            
            // Project Coordinator Role
            var coordinatorSupervisor = await supervisorService.GetProjectCoordinatorAsync(departmentId);
            var coordinatorWorkRole = GetCoordinatorWorkRole(departmentId);
            assignments.Add(new RoleAssignment(
                StandardRoleType.ProjectCoordinator,
                coordinatorSupervisor,
                coordinatorWorkRole,
                RoleAssignmentConstants.Descriptions.ProjectCoordinator
            ));
            
            return assignments;
        }
        
        private WorkAssignmentRoleCode GetCoordinatorWorkRole(DepartmentId departmentId)
        {
            var isSpecialDepartment = configuration.SpecialDepartmentCodes?.Contains(departmentId) ?? false;
            return isSpecialDepartment
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;
        }
    }

    // ============================================
    // SERVICE - Main business logic with DI
    // ============================================
    
    public class RoleAssignmentService_GoodWay
    {
        private readonly IUserRoleRepository? userRoleRepository;
        private readonly IWorkAssignmentRepository? workAssignmentRepository;
        private readonly ISupervisorService? supervisorService;
        private readonly IRoleAssignmentFactory? roleAssignmentFactory;
        private readonly IUnitOfWork? unitOfWork;
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

        // Backward compatibility constructor for gradual migration
        [Obsolete("This constructor will be removed in the next version. Use the constructor with full dependency injection.")]
        public RoleAssignmentService_GoodWay()
        {
            // This would use ServiceFactory or default implementations
            // Included for backward compatibility during migration
            logger = new ConsoleLogger();
            configuration = new RoleAssignmentConfiguration();
            
            // Initialize demo implementations for backward compatibility
            userRoleRepository = new DemoUserRoleRepository();
            workAssignmentRepository = new DemoWorkAssignmentRepository();
            supervisorService = new DemoSupervisorService();
            roleAssignmentFactory = new RoleAssignmentFactory(supervisorService, configuration);
            unitOfWork = new DemoUnitOfWork();
            
            logger.LogWarning("Using deprecated parameterless constructor. Please update to use dependency injection.");
        }

        // Main public method - now async with result pattern
        public async Task<RoleAssignmentResult> AssignStandardRolesAsync(
            UserId userId, 
            DepartmentId departmentId, 
            DateTime assignedDate)
        {
            logger.LogInformation(
                "Starting role assignment for user {0} in department {1}",
                userId.Value,
                departmentId.Value);

            var assignedRoleIds = new List<RoleId>();

            try
            {
                await unitOfWork!.BeginTransactionAsync();
                
                // Get the roles this user should have
                var roleAssignments = await roleAssignmentFactory!.CreateStandardAssignmentsAsync(departmentId);
                
                // Process each role with validation
                foreach (var assignment in roleAssignments)
                {
                    var validation = await ValidateRoleAssignmentAsync(userId, departmentId, assignment);
                    if (!validation.IsValid)
                    {
                        logger.LogWarning(
                            "Validation failed for role {0}: {1}",
                            assignment.RoleType,
                            string.Join(", ", validation.Errors));
                        
                        if (configuration.ValidateExistingRoles)
                        {
                            await unitOfWork.RollbackAsync();
                            return RoleAssignmentResult.Failure(validation.Errors.ToArray());
                        }
                        continue;
                    }
                    
                    var roleId = await ProcessSingleRoleAssignmentAsync(userId, departmentId, assignedDate, assignment);
                    if (roleId != null)
                    {
                        assignedRoleIds.Add(roleId);
                    }
                }
                
                await unitOfWork.CommitAsync();
                
                logger.LogInformation(
                    "Successfully assigned {0} roles to user {1}",
                    assignedRoleIds.Count,
                    userId.Value);
                
                return RoleAssignmentResult.Success(assignedRoleIds.ToArray());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Failed to assign roles for user {0} in department {1}",
                    userId.Value,
                    departmentId.Value);
                
                await unitOfWork!.RollbackAsync();
                return RoleAssignmentResult.Failure($"Role assignment failed: {ex.Message}");
            }
        }

        // Synchronous overload for backward compatibility
        public void AssignStandardRoles(UserId userId, DepartmentId departmentId, DateTime assignedDate)
        {
            var result = AssignStandardRolesAsync(userId, departmentId, assignedDate).GetAwaiter().GetResult();
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException($"Role assignment failed: {string.Join(", ", result.Errors)}");
            }
        }

        private async Task<ValidationResult> ValidateRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            RoleAssignment assignment)
        {
            var errors = new List<string>();
            
            // Check if user already has this role
            if (configuration.ValidateExistingRoles)
            {
                if (await userRoleRepository!.UserRoleExistsAsync(userId, assignment.RoleType))
                {
                    errors.Add($"User already has role {assignment.RoleType.GetDisplayName()}");
                }
            }
            
            // Validate supervisor exists
            if (!await supervisorService!.SupervisorExistsAsync(assignment.SupervisorId))
            {
                errors.Add($"Supervisor {assignment.SupervisorId.Value} not found");
            }
            
            return errors.Any() 
                ? ValidationResult.Failure(errors.ToArray()) 
                : ValidationResult.Success();
        }

        private async Task<RoleId> ProcessSingleRoleAssignmentAsync(
            UserId userId, 
            DepartmentId departmentId,
            DateTime assignedDate, 
            RoleAssignment assignment)
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
                await userRoleRepository!.CreateUserRoleAsync(command);
                
                // Create work assignment
                var workCommand = new WorkAssignmentCommand(
                    assignment.SupervisorId,
                    assignment.WorkRole,
                    userId,
                    ResourceType.UserAccount,
                    assignedDate
                );
                
                await workAssignmentRepository!.CreateWorkAssignmentAsync(workCommand);
                
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
    public class ConsoleLogger : ILogger
    {
        public void LogInformation(string message, params object[] args)
        {
            try
            {
                Console.WriteLine($"[INFO] {string.Format(message, args)}");
            }
            catch (FormatException)
            {
                // Fallback for format issues - just log the message as-is
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
    public class DemoUserRoleRepository : IUserRoleRepository
    {
        public Task<int> CreateUserRoleAsync(CreateUserRoleCommand command)
        {
            Console.WriteLine($"  [REPO] Creating user role - RoleId: {command.RoleId.Value}, UserId: {command.UserId.Value}");
            return Task.FromResult(1);
        }

        public Task<bool> UserRoleExistsAsync(UserId userId, StandardRoleType roleType)
        {
            return Task.FromResult(false); // Always return false for demo
        }
    }

    public class DemoWorkAssignmentRepository : IWorkAssignmentRepository
    {
        public Task<int> CreateWorkAssignmentAsync(WorkAssignmentCommand command)
        {
            Console.WriteLine($"  [REPO] Creating work assignment - Supervisor: {command.SupervisorId.Value}, Role: {command.RoleCode}");
            return Task.FromResult(1);
        }
    }

    public class DemoSupervisorService : ISupervisorService
    {
        public Task<SupervisorId> GetDepartmentManagerAsync(DepartmentId departmentId)
        {
            return Task.FromResult(new SupervisorId($"{RoleAssignmentConstants.ManagerPrefix}{departmentId.Value}"));
        }

        public Task<SupervisorId> GetProjectCoordinatorAsync(DepartmentId departmentId)
        {
            return Task.FromResult(new SupervisorId($"{RoleAssignmentConstants.CoordinatorPrefix}{departmentId.Value}"));
        }

        public Task<bool> SupervisorExistsAsync(SupervisorId supervisorId)
        {
            return Task.FromResult(true); // Always return true for demo
        }
    }

    public class DemoUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync()
        {
            Console.WriteLine("  [UOW] Beginning transaction");
            return Task.CompletedTask;
        }

        public Task CommitAsync()
        {
            Console.WriteLine("  [UOW] Committing transaction");
            return Task.CompletedTask;
        }

        public Task RollbackAsync()
        {
            Console.WriteLine("  [UOW] Rolling back transaction");
            return Task.CompletedTask;
        }
    }
}
