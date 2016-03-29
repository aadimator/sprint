using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Paper_Portal.Helpers
{
    public static class RoleHelper
    {

        public const string Admin = "admin";
        public const string Teacher = "teacher";
        public const string Printer = "printer";

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
