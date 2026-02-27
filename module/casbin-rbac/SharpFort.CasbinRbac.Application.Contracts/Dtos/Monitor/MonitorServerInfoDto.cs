using System;
using System.Collections.Generic;

namespace SharpFort.CasbinRbac.Application.Contracts.Dtos.Monitor
{
    public class MonitorServerInfoDto
    {
        public CpuInfoDto Cpu { get; set; } = new();
        public MemoryInfoDto Memory { get; set; } = new();
        public List<DiskInfoDto> Disks { get; set; } = new();
        public SysInfoDto Sys { get; set; } = new();
        public AppInfoDto App { get; set; } = new();
        public List<NetworkAdapterDto> Networks { get; set; } = new();
        public List<AssemblyInfoDto> Assemblies { get; set; } = new();
    }

    public class CpuInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public int CoreTotal { get; set; }
        public int LogicalProcessors { get; set; }
        public double CPURate { get; set; }
        public double FreeRate { get; set; }
    }

    public class MemoryInfoDto
    {
        public string TotalRAM { get; set; } = string.Empty;
        public string UsedRam { get; set; } = string.Empty;
        public string FreeRam { get; set; } = string.Empty;
        public string RAMRate { get; set; } = string.Empty;
    }

    public class DiskInfoDto
    {
        public string DiskName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public long TotalSize { get; set; }
        public long Used { get; set; }
        public long AvailableFreeSpace { get; set; }
        public decimal AvailablePercent { get; set; }
    }

    public class SysInfoDto
    {
        public string ComputerName { get; set; } = string.Empty;
        public string OsName { get; set; } = string.Empty;
        public string OsArch { get; set; } = string.Empty;
        public string ServerIP { get; set; } = string.Empty;
        public string RunTime { get; set; } = string.Empty;
    }

    public class AppInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string RootPath { get; set; } = string.Empty;
        public string WebRootPath { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string AppRAM { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string RunTime { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
    }

    public class NetworkAdapterDto
    {
        public string Name { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string IPv4 { get; set; } = string.Empty;
    }

    public class AssemblyInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
