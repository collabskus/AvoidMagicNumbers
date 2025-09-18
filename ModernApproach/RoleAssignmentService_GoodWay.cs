// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment
// ============================================

namespace ModernApproach
{
    // Strong types instead of primitive strings/ints
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

    // Clear enums that document business meaning
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

    // Immutable data structure with strong types
    public record RoleAssignment(
        StandardRoleType RoleType,
        SupervisorId SupervisorId,
        WorkAssignmentRoleCode WorkRole,
        string Description
    );

    // Command object for database operations
    public record CreateUserRoleCommand(
        RoleId RoleId,
        AssignmentId AssignmentId,
        UserId UserId,
        DepartmentId DepartmentId,
        DateTime AssignedDate,
        StandardRoleType RoleType,
        SupervisorId SupervisorId
    );

    public class RoleAssignmentService_GoodWay
    {
        private static readonly DepartmentId SpecialDepartmentCode = new("SPECIAL-DEPT");

        public void AssignStandardRoles(UserId userId, DepartmentId departmentId, DateTime assignedDate)
        {
            // Clear intent: we're getting the roles this user should have
            var roleAssignments = GetStandardRoleAssignments(departmentId);

            // Process each role explicitly - no mystery loops
            foreach (var assignment in roleAssignments)
            {
                ProcessSingleRoleAssignment(userId, departmentId, assignedDate, assignment);
            }
        }

        private List<RoleAssignment> GetStandardRoleAssignments(DepartmentId departmentId)
        {
            return new List<RoleAssignment>
            {
                new RoleAssignment(
                    StandardRoleType.DepartmentManager,
                    GetDepartmentManager(departmentId),
                    WorkAssignmentRoleCode.ProjectManager,
                    "Department Manager - oversees department operations"
                ),
                new RoleAssignment(
                    StandardRoleType.ProjectCoordinator,
                    GetProjectCoordinator(departmentId),
                    GetCoordinatorWorkRole(departmentId),
                    "Project Coordinator - manages project workflows"
                )
            };
        }

        private void ProcessSingleRoleAssignment(UserId userId, DepartmentId departmentId,
            DateTime assignedDate, RoleAssignment assignment)
        {
            // Create strongly-typed command object
            var command = new CreateUserRoleCommand(
                RoleId.New(),
                AssignmentId.New(),
                userId,
                departmentId,
                assignedDate,
                assignment.RoleType,
                assignment.SupervisorId
            );

            Console.WriteLine($"Assigning {assignment.RoleType} role to user {userId.Value}");
            Console.WriteLine($"  Description: {assignment.Description}");

            // Pass strongly-typed command instead of dictionary
            ExecuteDatabaseCommand(command);

            CreateWorkAssignment(
                assignment.SupervisorId,
                assignment.WorkRole,
                userId,
                ResourceType.UserAccount
            );
        }

        private WorkAssignmentRoleCode GetCoordinatorWorkRole(DepartmentId departmentId)
        {
            // Business rule is now explicit and documented with type-safe comparison
            return departmentId == SpecialDepartmentCode
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;
        }

        // Type-safe method signatures prevent parameter mix-ups
        private void ExecuteDatabaseCommand(CreateUserRoleCommand command)
        {
            Console.WriteLine($"  Executing InsertUserRole command");
            Console.WriteLine($"    RoleId: {command.RoleId.Value}");
            Console.WriteLine($"    UserId: {command.UserId.Value}");
            Console.WriteLine($"    RoleType: {command.RoleType}");
        }

        private void CreateWorkAssignment(SupervisorId supervisorId, WorkAssignmentRoleCode roleCode,
            UserId userId, ResourceType resourceType)
        {
            Console.WriteLine($"  Creating work assignment: Supervisor={supervisorId.Value}, " +
                            $"Role={roleCode}, ResourceType={resourceType}");
        }

        private SupervisorId GetDepartmentManager(DepartmentId departmentId)
            => new SupervisorId("manager-" + departmentId.Value);

        private SupervisorId GetProjectCoordinator(DepartmentId departmentId)
            => new SupervisorId("coordinator-" + departmentId.Value);
    }
}