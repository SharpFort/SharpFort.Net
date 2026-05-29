using Xunit;
using SqlSugar;
using System;
using Volo.Abp.Domain.Entities;
using System.Reflection;
using System.IO;
using System.Text;
using SharpFort.CasbinRbac.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using SharpFort.SqlSugarCore.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using SharpFort.SqlSugarCore;
using Volo.Abp.Data;

namespace Sf.Abp.Test.example
{
    public class AbpSoftDeleteFilterTest : SfAbpTestBase
    {
        protected override void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder)
        {
            base.ConfigureAppConfiguration(configurationBuilder);

            // Override connection string and options to use a clean shared in-memory database
            var memoryDbConfig = new Dictionary<string, string>
            {
                {"DbConnOptions:Url", "DataSource=file:abpmemorydb5?mode=memory&cache=shared"},
                {"DbConnOptions:EnabledDbSeed", "false"},
                {"DbConnOptions:EnabledCodeFirst", "true"},
                {"DbConnOptions:EnabledSqlLog", "true"}
            };

            configurationBuilder.AddInMemoryCollection(memoryDbConfig);
        }

        private void PrintFilters(StringBuilder sb, ISqlSugarClient client, string label)
        {
            var queryFilter = client.QueryFilter;
            if (queryFilter != null)
            {
                sb.AppendLine($"--- Filters for {label} ---");
                var filtersField = queryFilter.GetType().GetField("<_Filters>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                                ?? queryFilter.GetType().GetField("_Filters", BindingFlags.Instance | BindingFlags.NonPublic);
                if (filtersField != null)
                {
                    var filters = filtersField.GetValue(queryFilter) as System.Collections.IEnumerable;
                    if (filters != null)
                      {
                        foreach (var filter in filters)
                        {
                            sb.AppendLine($"  Filter type: {filter.GetType().FullName}");
                        }
                    }
                    else
                    {
                        sb.AppendLine("  _Filters field is null");
                    }
                }
                else
                {
                    sb.AppendLine("  _Filters field not found");
                }
            }
            else
            {
                sb.AppendLine($"--- QueryFilter is null for {label} ---");
            }
        }

        [Fact]
        public async Task TestRealAbpSoftDeleteFilterSql()
        {
            var sb = new StringBuilder();

            var rep = GetRequiredService<ISqlSugarRepository<Menu>>();
            var client = await rep.AsSugarClient();

            sb.AppendLine($"Client type: {client.GetType().FullName}");
            sb.AppendLine($"Current ConfigId: {client.CurrentConnectionConfig?.ConfigId}");

            // Print filters on client
            PrintFilters(sb, client, "client");

            // Print filters on default connection
            if (client is SqlSugarScope scope)
            {
                try
                {
                    var defaultClient = scope.GetConnection("Default");
                    sb.AppendLine($"Default Client type: {defaultClient?.GetType().FullName}");
                    PrintFilters(sb, defaultClient, "GetConnection('Default')");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"GetConnection('Default') failed: {ex.Message}");
                }

                try
                {
                    var defaultScope = scope.GetConnectionScope("Default");
                    sb.AppendLine($"Default Scope type: {defaultScope?.GetType().FullName}");
                    PrintFilters(sb, defaultScope, "GetConnectionScope('Default')");
                }
                catch (Exception ex)
                {
                    sb.AppendLine($"GetConnectionScope('Default') failed: {ex.Message}");
                }
            }

            // Generate SQL for the query on Menu
            var sqlObj = client.Queryable<Menu>().ToSql();
            sb.AppendLine("Generated SQL: " + sqlObj.Key);

            // Write to conversation directory scratch folder
            string scratchPath = @"C:\Users\hhhelong\.gemini\antigravity\brain\8aaa3db7-322b-4300-a1cc-48f8f763a52b\scratch";
            Directory.CreateDirectory(scratchPath);
            File.WriteAllText(Path.Combine(scratchPath, "real_abp_filter_diag.txt"), sb.ToString(), Encoding.UTF8);

            // Assert that the filter condition is in the generated SQL's WHERE clause
            Assert.Contains("where", sqlObj.Key.ToLowerInvariant());
        }
    }
}
