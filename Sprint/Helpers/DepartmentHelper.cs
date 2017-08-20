using Sprint.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sprint.Helpers
{
    public static class DepartmentHelper
    {
        public const string AgriculturalSciences = "Agriculture Sciences";
        public const string EnvironmentalSciences = "Environmental Sciences";
        public const string Forestry = "Forestry & Wildlife Management";
        public const string Geology = "Geology";
        public const string IT = "Information Technology";
        public const string MLT = "Medical Lab Technology";
        public const string Microbiology = "Microbiology";
        public const string PublicHealth = "Public Health";
        public const string Economics = "Economics";
        public const string Education = "Education";
        public const string IslamicStudies = "Islamic & Religious Studies";
        public const string ManagementSciences = "Management Sciences";
        public const string Psychology = "Psychology";
        public const string Examination = "Examination";
        public const string Administration = "Administration";


        public static readonly string[] Departments = {
            AgriculturalSciences,
            EnvironmentalSciences,
            Forestry,
            Geology,
            IT,
            MLT,
            Microbiology,
            PublicHealth,
            Economics,
            Education,
            IslamicStudies,
            ManagementSciences,
            Psychology,
            Examination,
            Administration
            };

        public static async Task EnsureDepartmentsCreated(ApplicationDbContext context)
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
