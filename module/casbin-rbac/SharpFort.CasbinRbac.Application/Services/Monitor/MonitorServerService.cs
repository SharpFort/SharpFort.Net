using System.Diagnostics;
using System.Runtime.InteropServices;
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
    public class MonitorServerService(IWebHostEnvironment hostEnvironment, IHttpContextAccessor httpContextAccessor) : ApplicationService, IMonitorServerService
    {
        private readonly IWebHostEnvironment _hostEnvironment = hostEnvironment;
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        // 长时间静态缓存（1 小时 TTL）
        private static List<NetworkAdapterDto> _cachedNetworks = null!;
        private static List<AssemblyInfoDto> _cachedAssemblies = null!;
        private static DateTime _lastStaticCacheTime = DateTime.MinValue;
        private static readonly Lock _staticCacheLock = new();

        // 服务器信息缓存（10 秒 TTL）：用静态字段 + 双重检查锁，适配 IDistributedCache 架构
        private static MonitorServerInfoDto? _cachedServerInfo;
        private static DateTime _lastServerInfoRefresh = DateTime.MinValue;
        private static readonly Lock _serverInfoLock = new();
        private static readonly TimeSpan ServerInfoCacheDuration = TimeSpan.FromSeconds(10);

        [HttpGet("monitor-server/info")]
        public Task<MonitorServerInfoDto> GetServerInfoAsync()
        {
            // 快速路径：缓存有效直接返回
            MonitorServerInfoDto? cached = _cachedServerInfo;
            if (cached != null && (DateTime.Now - _lastServerInfoRefresh) < ServerInfoCacheDuration)
            {
                return Task.FromResult(cached);
            }

            lock (_serverInfoLock)
            {
                // 双重检查：进入锁后再次确认缓存是否仍然有效
                cached = _cachedServerInfo;
                if (cached != null && (DateTime.Now - _lastServerInfoRefresh) < ServerInfoCacheDuration)
                {
                    return Task.FromResult(cached);
                }

                _cachedServerInfo = BuildServerInfo();
                _lastServerInfoRefresh = DateTime.Now;
                return Task.FromResult(_cachedServerInfo);
            }
        }

        /// <summary>
        /// 构建服务器信息（仅在缓存过期时执行，约每 10 秒一次）
        /// </summary>
        private MonitorServerInfoDto BuildServerInfo()
        {
            MonitorServerInfoDto dto = new();

            // 创建 HardwareInfo 实例，仅刷新需要的部分
            HardwareInfo hardwareInfo = new();
            hardwareInfo.RefreshMemoryStatus();
            hardwareInfo.RefreshCPUList();

            // sys info
            long sysRunTimeMs = Environment.TickCount64;
            string sysRunTime = DateTimeHelper.FormatTime(sysRunTimeMs);
            string? serverIp = _httpContextAccessor.HttpContext?.Connection?.LocalIpAddress?.MapToIPv4()?.ToString() + ":" + _httpContextAccessor.HttpContext?.Connection?.LocalPort;
            dto.Sys = new SysInfoDto
            {
                ComputerName = Environment.MachineName,
                OsName = RuntimeInformation.OSDescription,
                OsArch = RuntimeInformation.OSArchitecture.ToString(),
                ServerIP = serverIp ?? "未知",
                RunTime = sysRunTime
            };

            // app info
            Process currentProcess = Process.GetCurrentProcess();
            DateTime programStartTime = currentProcess.StartTime;
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

            // cpu info
            CPU? hardwareCpu = hardwareInfo.CpuList.FirstOrDefault();
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
            ulong totalPhysMem = hardwareInfo.MemoryStatus.TotalPhysical;
            ulong availPhysMem = hardwareInfo.MemoryStatus.AvailablePhysical;
            ulong usedPhysMem = totalPhysMem - availPhysMem;
            dto.Memory = new MemoryInfoDto
            {
                TotalRAM = totalPhysMem > 0 ? Math.Round(totalPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                UsedRam = usedPhysMem > 0 ? Math.Round(usedPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                FreeRam = availPhysMem > 0 ? Math.Round(availPhysMem / 1024.0 / 1024.0 / 1024.0, 2) + "GB" : "未知",
                RAMRate = totalPhysMem > 0 ? Math.Ceiling(100.0 * usedPhysMem / totalPhysMem) + "%" : "未知"
            };

            // disk info
            try
            {
                foreach (DriveInfo? drive in DriveInfo.GetDrives().Where(d => d.IsReady))
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

            // 长时间静态缓存（1 小时 TTL）
            lock (_staticCacheLock)
            {
                if ((DateTime.Now - _lastStaticCacheTime).TotalHours > 1 || _cachedNetworks == null || _cachedAssemblies == null)
                {
                    hardwareInfo.RefreshNetworkAdapterList();
                    _cachedNetworks = [.. hardwareInfo.NetworkAdapterList.Select(net => new NetworkAdapterDto
                    {
                        Name = net.Name,
                        MacAddress = net.MACAddress,
                        IPv4 = net.IPAddressList != null ? string.Join(", ", net.IPAddressList.Select(ip => ip.ToString())) : "N/A"
                    })];

                    _cachedAssemblies = [.. AppDomain.CurrentDomain.GetAssemblies()
                        .Where(a => !a.IsDynamic)
                        .Select(a => new AssemblyInfoDto
                        {
                            Name = a.GetName().Name ?? "Unknown",
                            Version = a.GetName().Version?.ToString() ?? string.Empty
                        })
                        .OrderBy(a => a.Name)];

                    _lastStaticCacheTime = DateTime.Now;
                }
            }

            dto.Networks = _cachedNetworks;
            dto.Assemblies = _cachedAssemblies;

            return dto;
        }
    }
}
