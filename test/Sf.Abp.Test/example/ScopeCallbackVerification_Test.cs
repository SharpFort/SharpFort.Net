using Xunit;
using SqlSugar;
using System;
using Volo.Abp.Domain.Entities;
using System.Reflection;
using System.IO;
using System.Text;
using SharpFort.CasbinRbac.Domain.Entities;
using System.Threading.Tasks;

namespace Sf.Abp.Test.example
{
    public class ScopeCallbackVerificationTest
    {
        [Fact]
        public async Task TestAbpSoftDeleteFilterWithScopeCallback()
        {
            var sb = new StringBuilder();

            // Set up connection config
            var connectionConfig = new ConnectionConfig()
            {
                ConnectionString = "DataSource=:memory:",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = false
            };

            // Option A: Bad Way (Current Implementation in Factory)
            var scopeBad = new SqlSugarScope(connectionConfig);
            scopeBad.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);

            // Option B: Good Way (Proposed Fix)
            var scopeGood = new SqlSugarScope(connectionConfig, db =>
            {
                db.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
            });

            // Initialize tables for both
            scopeBad.DbMaintenance.CreateDatabase();
            scopeBad.CodeFirst.InitTables<Menu>();

            scopeGood.DbMaintenance.CreateDatabase();
            scopeGood.CodeFirst.InitTables<Menu>();

            // Let's start a transaction on both!
            // This replicates the transactional Unit Of Work in the real application.
            await scopeBad.Ado.BeginTranAsync();
            await scopeGood.Ado.BeginTranAsync();

            string sqlBad = "";
            string sqlGood = "";

            sqlBad = scopeBad.Queryable<Menu>().ToSql().Key;
            sqlGood = scopeGood.Queryable<Menu>().ToSql().Key;

            await scopeBad.Ado.RollbackTranAsync();
            await scopeGood.Ado.RollbackTranAsync();

            sb.AppendLine($"Bad Way SQL (With Transaction): {sqlBad}");
            sb.AppendLine($"Good Way SQL (With Transaction): {sqlGood}");

            // Write results to diagnostics file
            string scratchPath = @"C:\Users\hhhelong\.gemini\antigravity\brain\8aaa3db7-322b-4300-a1cc-48f8f763a52b\scratch";
            Directory.CreateDirectory(scratchPath);
            File.WriteAllText(Path.Combine(scratchPath, "scope_test_diag.txt"), sb.ToString(), Encoding.UTF8);

            // Assertions
            Assert.DoesNotContain("where", sqlBad.ToLowerInvariant());
            Assert.Contains("where", sqlGood.ToLowerInvariant());
        }
    }
}
