// ============================================
// DEMONSTRATION
// ============================================

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
        Console.WriteLine("=== MODERN APPROACH: Explicit Business Logic ===");
        Console.WriteLine("Benefits of this refactored approach:");
        Console.WriteLine("- Removes magic number loop entirely");
        Console.WriteLine("- Each role assignment is explicit and documented");
        Console.WriteLine("- Business rules are clear and testable");
        Console.WriteLine("- Easy to add new roles without modifying loops");
        Console.WriteLine("- Separates data structure from processing logic");
        Console.WriteLine();

        var goodService = new ModernApproach.RoleAssignmentService_GoodWay();
        goodService.AssignStandardRoles("user123", "IT-DEPT", DateTime.Now);

        Console.WriteLine();
        Console.WriteLine("=== KEY LESSONS FOR REFACTORING ===");
        Console.WriteLine("1. ELIMINATE MAGIC NUMBER LOOPS");
        Console.WriteLine("   Replace: for (int i = 6; i <= 7; i++)");
        Console.WriteLine("   With: foreach (var role in GetRequiredRoles())");
        Console.WriteLine();
        Console.WriteLine("2. USE DESCRIPTIVE ENUMS");
        Console.WriteLine("   Replace: if (i == 6) // FOAOwner");
        Console.WriteLine("   With: if (role.Type == SolicitationRoleType.FOAOwner)");
        Console.WriteLine();
        Console.WriteLine("3. EXTRACT BUSINESS LOGIC");
        Console.WriteLine("   Replace: Complex conditionals inside loops");
        Console.WriteLine("   With: Separate methods that handle specific business rules");
        Console.WriteLine();
        Console.WriteLine("4. MAKE DATA STRUCTURES EXPLICIT");
        Console.WriteLine("   Replace: Magic numbers representing concepts");
        Console.WriteLine("   With: Strongly-typed objects that model the domain");
        Console.WriteLine();
        Console.WriteLine("This pattern applies to your dependency injection work:");
        Console.WriteLine("- Replace ServiceFactory static calls with injected dependencies");
        Console.WriteLine("- Make configuration paths explicit parameters");
        Console.WriteLine("- Use constructor injection instead of hidden factory dependencies");
    }
}