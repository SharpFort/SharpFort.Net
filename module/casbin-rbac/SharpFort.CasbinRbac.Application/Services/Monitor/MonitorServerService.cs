using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Hardware.Info;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;
using SharpFort.Core.Helper;
using SharpFort.CasbinRbac.Application.Contracts.IServices;
using SharpFort.CasbinRbac.Application.Contracts.Dtos.Monitor;

namespace SharpFort.CasbinRbac.Application.Services.Monitor
{
    public class MonitorServerService : ApplicationService, IMonitorServerService
    {
        private readonly IWebHostEnvironment _hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MonitorServerService(IWebHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor)
        {
            _hostEnvironment = hostEnvironment;
            _httpContextAccessor = httpContextAccessor;
        }

        private static List<NetworkAdapterDto> _cachedNetworks = null!;
        private static List<AssemblyInfoDto> _cachedAssemblies = null!;
        private static DateTime _lastStaticCacheTime = DateTime.MinValue;
        private static readonly object _staticCacheLock = new object();

        [HttpGet("monitor-server/info")]
        public async Task<MonitorServerInfoDto> GetServerInfoAsync()
        {
            return await Task.Run(() =>
            {
                var dto = new MonitorServerInfoDto();
                
                // PERFORMANCE FIX: Never call RefreshAll() in a request loop! It queries BIOS, Motherboard, Batteries, etc., and blocks the thread.
                // Create a scoped instance and ONLY refresh what we specifically need (Memory and CPU are fast).
                HardwareInfo hardwareInfo = new HardwareInfo();  // CA1859: use concrete type
                hardwareInfo.RefreshMemoryStatus();
                hardwareInfo.RefreshCPUList();

                // sys info (Use native Environment.TickCount64 for cross-platform uptime in milliseconds)
                var sysRunTimeMs = Environment.TickCount64;
                var sysRunTime = DateTimeHelper.FormatTime(sysRunTimeMs);
                var serverIp = _httpContextAccessor.HttpContext?.Connection?.LocalIpAddress?.MapToIPv4()?.ToString() + ":" + _httpContextAccessor.HttpContext?.Connection?.LocalPort;
                dto.Sys = new SysInfoDto
                {
                    ComputerName = Environment.MachineName,
                    OsName = RuntimeInformation.OSDescription,
                    OsArch = RuntimeInformation.OSArchitecture.ToString(),
                    ServerIP = serverIp ?? "未知",
                    RunTime = sysRunTime
                };

                // app info
                var currentProcess = Process.GetCurrentProcess();
                var programStartTime = currentProcess.StartTime;
                string programRunTime = DateTimeHelper.FormatTime((long)(DateTime.Now - programStartTime).TotalMilliseconds);
                string appRAM = ((double)currentProcess.WorkingSet64 / 1048576).ToString("N2", global::System.Globalization.CultureInfo.InvariantCulture) + " MB";
                
                dto.App = new AppInfoDto
                {
                    Name = _hostEnvironment.EnvironmentName,
                    RootPath = _hostEnvironment.ContentRootPath,
                    WebRootPath = _hostEnvironment.WebRootPath,
                    Version = RuntimeInformation.FrameworkDescription,
                    AppRAM = appRAM,
                    StartTime = programStartTime.ToString("yyyy-MM-dd HH:mm:ss", global::System.Globalization.CultureInfo.InvariantCulture),
                    RunTime = programRunTime,
                    Host = serverIp ?? "未知"
                };

                // cpu info (Hardware.Info handles the cross-platform cpu usage without wmic/top)
                var hardwareCpu = hardwareInfo.CpuList.FirstOrDefault();
                double cpuPercent = hardwareCpu?.PercentProcessorTime ?? 0;
                dto.Cpu = new CpuInfoDto
                {
                    Name = hardwareCpu?.Name ?? "未知 CPU",
                    CoreTotal = (int)(hardwareCpu?.NumberOfCores ?? (uint)Environment.ProcessorCount),
                    LogicalProcessors = (int)(hardwareCpu?.NumberOfLogicalProcessors ?? (uint)Environment.ProcessorCount),
                    CPURate = Math.Round(cpuPercent, 2),
                    FreeRate = Math.Round(100.0 - cpuPercent, 2)
                };

                // memory info
                var totalPhysMem = hardwareInfo.MemoryStatus.TotalPhysical;
                var availPhysMem = hardwareInfo.MemoryStatus.AvailablePhysical;
                var usedPhysMem = totalPhysMem - availPhysMem;
                dto.Memory = new MemoryInfoDto
                {
                    TotalRAM = totalPhysMem > 0 ? Math.Round(totalPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                    UsedRam = usedPhysMem > 0 ? Math.Round(usedPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                    FreeRam = availPhysMem > 0 ? Math.Round(availPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                    RAMRate = totalPhysMem > 0 ? Math.Ceiling(100.0 * usedPhysMem / totalPhysMem) + "%" : "未知"
                };

                // disk info (Use native .NET DriveInfo for absolute cross-platform reliability instead of wmic/df)
                try
                {
                    foreach (var drive in global::System.IO.DriveInfo.GetDrives().Where(d => d.IsReady))
                    {
                        long totalSizeGb = drive.TotalSize / 1024 / 1024 / 1024;
                        long freeSpaceGb = drive.AvailableFreeSpace / 1024 / 1024 / 1024;
                        long usedSpaceGb = totalSizeGb - freeSpaceGb;
                        decimal availablePercent = totalSizeGb > 0 ? decimal.Ceiling(usedSpaceGb / (decimal)totalSizeGb * 100) : 0;
                        
                        dto.Disks.Add(new DiskInfoDto
                        {
                            DiskName = drive.Name,
                            TypeName = drive.DriveType.ToString(),
                            TotalSize = totalSizeGb,
                            AvailableFreeSpace = freeSpaceGb,
                            Used = usedSpaceGb,
                            AvailablePercent = availablePercent
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error retrieving disk info natively: {ex.Message}");
                }

                // Cache slowly changing information like Networks and Assemblies to avoid WMI and Reflection overhead
                lock (_staticCacheLock)
                {
                    if ((DateTime.Now - _lastStaticCacheTime).TotalHours > 1 || _cachedNetworks == null || _cachedAssemblies == null)
                    {
                        hardwareInfo.RefreshNetworkAdapterList();
                        _cachedNetworks = hardwareInfo.NetworkAdapterList.Select(net => new NetworkAdapterDto
                        {
                            Name = net.Name,
                            MacAddress = net.MACAddress,
                            IPv4 = net.IPAddressList != null ? string.Join(", ", net.IPAddressList.Select(ip => ip.ToString())) : "N/A"
                        }).ToList();

                        _cachedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                            .Where(a => !a.IsDynamic)
                            .Select(a => new AssemblyInfoDto
                            {
                                Name = a.GetName().Name ?? "Unknown",  // CS8601: Name is string?
                                Version = a.GetName().Version?.ToString()
                            })
                            .OrderBy(a => a.Name)
                            .ToList();

                        _lastStaticCacheTime = DateTime.Now;
                    }
                }

                // Assign cached values
                dto.Networks = _cachedNetworks;
                dto.Assemblies = _cachedAssemblies;

                return dto;
            });
        }
    }
}
