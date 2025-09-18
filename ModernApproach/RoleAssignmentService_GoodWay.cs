// ============================================
// MODERN APPROACH: Explicit, Readable Role Assignment
// ============================================

namespace ModernApproach
{
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

    // Data structure that clearly represents what we're creating
    public class RoleAssignment
    {
        public StandardRoleType RoleType { get; set; }
        public required string SupervisorId { get; set; }
        public WorkAssignmentRoleCode WorkRole { get; set; }
        public required string Description { get; set; }
    }

    public class RoleAssignmentService_GoodWay
    {
        private readonly Dictionary<string, object> parameters = new Dictionary<string, object>();
        private const string SpecialDepartmentCode = "SPECIAL-DEPT";

        public void AssignStandardRoles(string userId, string departmentId, DateTime assignedDate)
        {
            // Clear intent: we're getting the roles this user should have
            var roleAssignments = GetStandardRoleAssignments(departmentId);

            // Process each role explicitly - no mystery loops
            foreach (var assignment in roleAssignments)
            {
                ProcessSingleRoleAssignment(userId, departmentId, assignedDate, assignment);
            }
        }

        private List<RoleAssignment> GetStandardRoleAssignments(string departmentId)
        {
            return new List<RoleAssignment>
            {
                new RoleAssignment
                {
                    RoleType = StandardRoleType.DepartmentManager,
                    SupervisorId = GetDepartmentManager(departmentId),
                    WorkRole = WorkAssignmentRoleCode.ProjectManager,
                    Description = "Department Manager - oversees department operations"
                },
                new RoleAssignment
                {
                    RoleType = StandardRoleType.ProjectCoordinator,
                    SupervisorId = GetProjectCoordinator(departmentId),
                    WorkRole = GetCoordinatorWorkRole(departmentId),
                    Description = "Project Coordinator - manages project workflows"
                }
            };
        }

        private void ProcessSingleRoleAssignment(string userId, string departmentId,
            DateTime assignedDate, RoleAssignment assignment)
        {
            string roleId = Guid.NewGuid().ToString();
            string assignmentId = Guid.NewGuid().ToString();

            // Clear parameter building
            parameters.Clear();
            parameters.Add("RoleId", roleId);
            parameters.Add("AssignmentId", assignmentId);
            parameters.Add("UserId", userId);
            parameters.Add("DepartmentId", departmentId);
            parameters.Add("AssignedDate", assignedDate);
            parameters.Add("RoleTypeCode", (int)assignment.RoleType);
            parameters.Add("SupervisorId", assignment.SupervisorId);

            Console.WriteLine($"Assigning {assignment.RoleType} role to user {userId}");
            Console.WriteLine($"  Description: {assignment.Description}");

            ExecuteDatabaseCommand("InsertUserRole", parameters);

            CreateWorkAssignment(
                assignment.SupervisorId,
                assignment.WorkRole,
                userId,
                ResourceType.UserAccount
            );
        }

        private WorkAssignmentRoleCode GetCoordinatorWorkRole(string departmentId)
        {
            // Business rule is now explicit and documented
            return IsSpecialDepartment(departmentId)
                ? WorkAssignmentRoleCode.SpecialAdministrator
                : WorkAssignmentRoleCode.GeneralAdministrator;
        }

        private bool IsSpecialDepartment(string departmentId)
        {
            return departmentId.ToUpper() == SpecialDepartmentCode;
        }

        private void ExecuteDatabaseCommand(string command, Dictionary<string, object> parameters)
        {
            Console.WriteLine($"  Executing {command} with {parameters.Count} parameters");
        }

        private void CreateWorkAssignment(string supervisorId, WorkAssignmentRoleCode roleCode,
            string userId, ResourceType resourceType)
        {
            Console.WriteLine($"  Creating work assignment: Supervisor={supervisorId}, " +
                            $"Role={roleCode}, ResourceType={resourceType}");
        }

        private string GetDepartmentManager(string departmentId) => "manager-" + departmentId;
        private string GetProjectCoordinator(string departmentId) => "coordinator-" + departmentId;
    }
}
