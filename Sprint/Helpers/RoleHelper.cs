using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Sprint.Helpers
{
    public static class RoleHelper
    {

        public const string Admin = "Admin";
        public const string Teacher = "Faculty Member";
        public const string Printer = "Printer";

        private static async Task EnsureRoleCreated(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        public static async Task EnsureRolesCreated(this RoleManager<IdentityRole> roleManager)
        {
            // add all roles, that should be in database, here
            await EnsureRoleCreated(roleManager, Admin);
            await EnsureRoleCreated(roleManager, Teacher);
            await EnsureRoleCreated(roleManager, Printer);
        }
    }
}
