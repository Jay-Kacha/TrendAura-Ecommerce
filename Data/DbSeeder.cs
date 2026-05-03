using Microsoft.AspNetCore.Identity;
using trendaura.Models;

namespace trendaura.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAdminUserAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            // Create Customer role if it doesn't exist
            if (!await roleManager.RoleExistsAsync("Customer"))
            {
                await roleManager.CreateAsync(new IdentityRole("Customer"));
            }

            // Check if admin user already exists
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                // Create admin user
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@trendaura.com",
                    FullName = "System Administrator",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, "admin@123");
                
                if (result.Succeeded)
                {
                    // Assign Admin role to the user
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("? Admin user created successfully!");
                    Console.WriteLine("   Username: admin");
                    Console.WriteLine("   Email: admin@trendaura.com");
                    Console.WriteLine("   Password: admin@123");
                }
                else
                {
                    Console.WriteLine("? Failed to create admin user:");
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"   - {error.Description}");
                    }
                }
            }
            else
            {
                var needsUpdate = false;

                // Update email if it differs
                if (!string.Equals(adminUser.Email, "admin@trendaura.com", StringComparison.OrdinalIgnoreCase))
                {
                    adminUser.Email = "admin@trendaura.com";
                    adminUser.NormalizedEmail = "ADMIN@TRENDAURA.COM";
                    adminUser.EmailConfirmed = true;
                    needsUpdate = true;
                }

                if (needsUpdate)
                {
                    var updateResult = await userManager.UpdateAsync(adminUser);
                    if (updateResult.Succeeded)
                    {
                        Console.WriteLine("? Admin user email updated to admin@trendaura.com");
                    }
                    else
                    {
                        Console.WriteLine("? Failed to update admin user email:");
                        foreach (var error in updateResult.Errors)
                        {
                            Console.WriteLine($"   - {error.Description}");
                        }
                    }
                }

                // Ensure admin user has Admin role
                if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("? Admin role assigned to existing admin user");
                }
                else
                {
                    Console.WriteLine("??  Admin user already exists and has Admin role");
                }
            }
        }
    }
}
