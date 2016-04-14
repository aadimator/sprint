using Paper_Portal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Paper_Portal.Helpers
{
    public static class DepartmentHelper
    {
        public static readonly string[] Departments = {
            "Agriculture Sciences",
            "Environmental Sciences",
            "Forestry & Wildlife Management",
            "Geology",
            "Information Technology",
            "Medical Lab Technology",
            "Microbiology",
            "Public Health",
            "Economics",
            "Education",
            "Islamic & Religious Studies",
            "Management Sciences",
            "Psychology"
            };

        public static async Task EnsureDepartmentsCreated(this ApplicationDbContext context)
        {
            // add all roles, that should be in database, here
            foreach (var department in Departments)
            {
                if (context.Department.Where(d => d.Name == department).Count() == 0)
                {
                    context.Department.Add(new Department()
                    {
                        Name = department,
                    });
                }
            }
            await context.SaveChangesAsync();
        }
    }
}
