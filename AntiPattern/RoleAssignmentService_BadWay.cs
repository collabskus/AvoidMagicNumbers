using System;
using System.Collections.Generic;

// ============================================
// ANTI-PATTERN: Using Magic Numbers in Loops
// ============================================

namespace AntiPattern
{
    public class RoleAssignmentService_BadWay
    {
        private Dictionary<string, object> parameters = new Dictionary<string, object>();

        public void AssignStandardRoles(string userId, string departmentId, DateTime assignedDate)
        {
            // PROBLEM 1: Magic numbers in loop - what do 6 and 7 represent?
            for (int i = 6; i <= 7; i++)
            {
                string roleId = Guid.NewGuid().ToString();
                string assignmentId = Guid.NewGuid().ToString();
                string supervisorId = null;

                // PROBLEM 2: Building parameters inside loop with unclear logic
                parameters.Clear();
                parameters.Add("RoleId", roleId);
                parameters.Add("AssignmentId", assignmentId);
                parameters.Add("UserId", userId);
                parameters.Add("DepartmentId", departmentId);
                parameters.Add("AssignedDate", assignedDate);

                // PROBLEM 3: Hard-coded conditionals based on magic numbers
                if (i == 6) // What business rule does "6" represent?
                {
                    supervisorId = GetDepartmentManager(departmentId);
                    parameters.Add("RoleTypeCode", i);
                    parameters.Add("SupervisorId", supervisorId);

                    Console.WriteLine($"Assigning role type {i} (what role?) to user {userId}");

                    // PROBLEM 4: Database calls scattered throughout loop
                    ExecuteDatabaseCommand("InsertUserRole", parameters);

                    // PROBLEM 5: More magic numbers in related method calls
                    CreateWorkAssignment(supervisorId, 101, userId, 25); // 101? 25?
                }
                else if (i == 7) // What business rule does "7" represent?
                {
                    supervisorId = GetProjectCoordinator(departmentId);
                    parameters.Add("RoleTypeCode", i);
                    parameters.Add("SupervisorId", supervisorId);

                    Console.WriteLine($"Assigning role type {i} (what role?) to user {userId}");
                    ExecuteDatabaseCommand("InsertUserRole", parameters);

                    // PROBLEM 6: Complex conditional logic with more magic numbers
                    var workRoleCode = (IsSpecialDepartment(departmentId) ? 202 : 203);
                    CreateWorkAssignment(supervisorId, workRoleCode, userId, 25);
                }
            }
        }

        private void ExecuteDatabaseCommand(string command, Dictionary<string, object> parameters)
        {
            Console.WriteLine($"  Executing {command} with {parameters.Count} parameters");
        }

        private void CreateWorkAssignment(string supervisorId, int roleCode, string userId, int resourceType)
        {
            Console.WriteLine($"  Creating work assignment: Supervisor={supervisorId}, RoleCode={roleCode}, ResourceType={resourceType}");
        }

        private string GetDepartmentManager(string departmentId) => "manager-" + departmentId;
        private string GetProjectCoordinator(string departmentId) => "coordinator-" + departmentId;
        private bool IsSpecialDepartment(string departmentId) => departmentId.ToUpper() == "SPECIAL-DEPT";
    }
}
