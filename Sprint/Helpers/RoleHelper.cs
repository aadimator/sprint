using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Sprint.Helpers
{
    public static class RoleHelper
    {
        public const string SuperAdmin = "Super Admin";
        public const string Admin = "Admin";
        public const string Teacher = "Faculty Member";
        public const string HOD = "Head of Department";
        public const string IC = "Internal Controller";
        public const string Examiner = "Examination Representative";

        private static async Task EnsureRoleCreated(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
        public static async Task EnsureRolesCreated(RoleManager<IdentityRole> roleManager)
        {
            // add all roles, that should be in database, here
            await EnsureRoleCreated(roleManager, SuperAdmin);
            await EnsureRoleCreated(roleManager, Admin);
            await EnsureRoleCreated(roleManager, Teacher);
            await EnsureRoleCreated(roleManager, HOD);
            await EnsureRoleCreated(roleManager, IC);
            await EnsureRoleCreated(roleManager, Examiner);
        }
    }
}
