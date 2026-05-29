using Xunit;
using SqlSugar;
using System;
using Volo.Abp.Domain.Entities;
using System.Reflection;
using System.IO;
using System.Text;
using SharpFort.CasbinRbac.Domain.Entities;

namespace Sf.Abp.Test.example
{
    public class PureSoftDeleteFilterTest
    {
        [Fact]
        public void TestInterfaceFilterInMemory()
        {
            var sb = new StringBuilder();

            // Setup an in-memory SqlSugar client
            var db = new SqlSugarClient(new ConnectionConfig()
            {
                ConnectionString = "DataSource=:memory:",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = false // Keep in-memory DB alive
            });

            // Register global interface filter
            db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);

            // Init tables (CodeFirst)
            db.DbMaintenance.CreateDatabase();
            
            try
            {
                db.CodeFirst.InitTables<Menu>();
            }
            catch (Exception ex)
            {
                sb.AppendLine("InitTables Menu failed: " + ex.Message);
            }

            // Generate SQL for the query on Menu
            var sqlObj = db.Queryable<Menu>().ToSql();
            sb.AppendLine("Generated SQL: " + sqlObj.Key);

            // Write to conversation directory scratch folder
            string scratchPath = @"C:\Users\hhhelong\.gemini\antigravity\brain\8aaa3db7-322b-4300-a1cc-48f8f763a52b\scratch";
            Directory.CreateDirectory(scratchPath);
            File.WriteAllText(Path.Combine(scratchPath, "filter_diag.txt"), sb.ToString(), Encoding.UTF8);
        }
    }
}
