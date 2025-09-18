// ============================================
// DEMONSTRATION
// ============================================

using ModernApproach;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ANTI-PATTERN: Magic Numbers in Loops ===");
        Console.WriteLine("- for (int i = 6; i <= 7; i++) - what do these numbers mean?");
        Console.WriteLine("- Hard-coded conditions based on loop counter values");
        Console.WriteLine("- Business logic buried inside loop iteration");
        Console.WriteLine("- Additional magic numbers scattered throughout (101, 202, 203, 25)");
        Console.WriteLine();

        var badService = new AntiPattern.RoleAssignmentService_BadWay();
        badService.AssignStandardRoles("user123", "IT-DEPT", DateTime.Now);

        Console.WriteLine();
        Console.WriteLine("=== MODERN APPROACH: Type-Safe, Explicit Business Logic ===");
        Console.WriteLine("Benefits of this refactored approach:");
        Console.WriteLine("- Eliminates magic number loop entirely");
        Console.WriteLine("- Strong types prevent parameter mix-ups at compile time");
        Console.WriteLine("- Command objects replace loose parameter dictionaries");
        Console.WriteLine("- Immutable records with clear business meaning");
        Console.WriteLine("- Type-safe comparisons instead of string manipulation");
        Console.WriteLine("- Each role assignment is explicit and testable");
        Console.WriteLine();

        var goodService = ModernApproach.DemoRoleAssignmentService.Create();
        goodService.AssignStandardRoles(new UserId("user123"), new DepartmentId("IT-DEPT"), DateTime.Now);

        Console.WriteLine();
        Console.WriteLine("1. ELIMINATE MAGIC NUMBER LOOPS");
        Console.WriteLine("   Replace: for (int i = 6; i <= 7; i++)");
        Console.WriteLine("   With: foreach (var role in GetRequiredRoles())");
        Console.WriteLine();
        Console.WriteLine("2. USE DESCRIPTIVE ENUMS");
        Console.WriteLine("   Replace: if (i == 6) // FOAOwner");
        Console.WriteLine("   With: if (role.Type == SolicitationRoleType.FOAOwner)");
        Console.WriteLine();
        Console.WriteLine("3. CREATE STRONGLY-TYPED DOMAIN OBJECTS");
        Console.WriteLine("   Replace: string userId, string departmentId");
        Console.WriteLine("   With: UserId userId, DepartmentId departmentId");
        Console.WriteLine();
        Console.WriteLine("4. USE COMMAND OBJECTS INSTEAD OF DICTIONARIES");
        Console.WriteLine("   Replace: parameters.Add(\"UserId\", userId)");
        Console.WriteLine("   With: new CreateUserRoleCommand(userId, departmentId, ...)");
        Console.WriteLine();
        Console.WriteLine("5. LEVERAGE COMPILE-TIME TYPE SAFETY");
        Console.WriteLine("   Replace: String comparisons and casting");
        Console.WriteLine("   With: Record equality and strong typing");
        Console.WriteLine();
        Console.WriteLine("This pattern applies to your dependency injection work:");
        Console.WriteLine("- Replace ServiceFactory static calls with injected dependencies");
        Console.WriteLine("- Make configuration paths explicit parameters");
        Console.WriteLine("- Use constructor injection instead of hidden factory dependencies");
    }
}