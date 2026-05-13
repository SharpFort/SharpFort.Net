using System.Globalization;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace SharpFort.Core.Helper
{
    public class ComputerHelper
    {
        private static readonly char[] NewLineSeparators = ['\n', '\r'];

        /// <summary>
        /// 将 object 转换为 long，若转换失败，则返回 0。不抛出异常。
        /// </summary>
        /// <param name="obj">要转换的对象</param>
        /// <returns>转换后的 long 值，失败返回 0</returns>
        private static long ParseToLong(object obj)
        {
            try
            {
                return long.Parse(obj.ToString()!, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0L;
            }
        }

        /// <summary>
        /// 将string转换为DateTime，若转换失败，则返回日期最小值。不抛出异常。  
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static DateTime ParseToDateTime(string str)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(str))
                {
                    return DateTime.MinValue;
                }
                if (str.Contains('-') || str.Contains('/'))
                {
                    return DateTime.Parse(str, CultureInfo.InvariantCulture);
                }
                else
                {
                    int length = str.Length;
                    return length switch
                    {
                        4 => DateTime.ParseExact(str, "yyyy", CultureInfo.CurrentCulture),
                        6 => DateTime.ParseExact(str, "yyyyMM", CultureInfo.CurrentCulture),
                        8 => DateTime.ParseExact(str, "yyyyMMdd", CultureInfo.CurrentCulture),
                        10 => DateTime.ParseExact(str, "yyyyMMddHH", CultureInfo.CurrentCulture),
                        12 => DateTime.ParseExact(str, "yyyyMMddHHmm", CultureInfo.CurrentCulture),
                        14 => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture),
                        _ => DateTime.ParseExact(str, "yyyyMMddHHmmss", CultureInfo.CurrentCulture),
                    };
                }
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
        private static double ParseToDouble(object obj)
        {
            try
            {
                return double.Parse(obj.ToString()!, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// CPU使用情况
        /// </summary>
        /// <returns></returns>
        public static CPUMetrics GetCPUMetrics()
        {
            CPUMetrics cpuMetrics = new();
            CPUInfo cpudetail = GetCPUDetails();
            cpuMetrics.CoreTotal = cpudetail.Cores;
            cpuMetrics.LogicalProcessors = cpudetail.LogicalProcessors;
            cpuMetrics.CPURate = Math.Ceiling(ParseToDouble(GetCPURate()));
            cpuMetrics.FreeRate = 1 - cpuMetrics.CPURate;
            return cpuMetrics;
        }
        /// <summary>
        /// 内存使用情况
        /// </summary>
        /// <returns></returns>
        public static MemoryMetrics GetMemoryMetrics()
        {
            try
            {
                MemoryMetrics memoryMetrics = IsUnix() ? MemoryMetricsClient.GetUnixMetrics() : MemoryMetricsClient.GetWindowsMetrics();

                memoryMetrics.FreeRam = Math.Round(memoryMetrics.Free / 1024, 2) + "GB";
                memoryMetrics.UsedRam = Math.Round(memoryMetrics.Used / 1024, 2) + "GB";
                memoryMetrics.TotalRAM = Math.Round(memoryMetrics.Total / 1024, 2) + "GB";
                memoryMetrics.RAMRate = Math.Ceiling(100 * memoryMetrics.Used / memoryMetrics.Total).ToString(CultureInfo.InvariantCulture) + "%";

                return memoryMetrics;
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取内存使用出错，msg=" + ex.Message + "," + ex.StackTrace);
            }
            return new MemoryMetrics();
        }

        /// <summary>
        /// 获取磁盘信息
        /// </summary>
        /// <returns></returns>
        public static List<DiskInfo> GetDiskInfos()
        {
            List<DiskInfo> diskInfos = [];

            if (IsUnix())
            {
                try
                {
                    string output = ShellHelper.Bash("df -m / | awk '{print $2,$3,$4,$5,$6}'");
                    string[] arr = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (arr.Length == 0)
                    {
                        return diskInfos;
                    }

                    string[] rootDisk = arr[1].Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (rootDisk == null || rootDisk.Length == 0)
                    {
                        return diskInfos;
                    }
                    DiskInfo diskInfo = new()
                    {
                        DiskName = "/",
                        TotalSize = long.Parse(rootDisk[0], CultureInfo.InvariantCulture) / 1024,
                        Used = long.Parse(rootDisk[1], CultureInfo.InvariantCulture) / 1024,
                        AvailableFreeSpace = long.Parse(rootDisk[2], CultureInfo.InvariantCulture) / 1024,
                        AvailablePercent = decimal.Parse(rootDisk[3].Replace("%", ""), CultureInfo.InvariantCulture)
                    };
                    diskInfos.Add(diskInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("获取磁盘信息出错了" + ex.Message);
                }
            }
            else
            {
                DriveInfo[] driv = DriveInfo.GetDrives();
                foreach (DriveInfo item in driv)
                {
                    try
                    {
                        DiskInfo obj = new()
                        {
                            DiskName = item.Name,
                            TypeName = item.DriveType.ToString(),
                            TotalSize = item.TotalSize / 1024 / 1024 / 1024,
                            AvailableFreeSpace = item.AvailableFreeSpace / 1024 / 1024 / 1024,
                        };
                        obj.Used = obj.TotalSize - obj.AvailableFreeSpace;
                        obj.AvailablePercent = decimal.Ceiling(obj.Used / (decimal)obj.TotalSize * 100);
                        diskInfos.Add(obj);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("获取磁盘信息出错了" + ex.Message);
                        continue;
                    }
                }
            }

            return diskInfos;
        }

        public static bool IsUnix()
        {
            bool isUnix = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            return isUnix;
        }

        public static string GetCPURate()
        {
            string cpuRate;
            if (IsUnix())
            {
                string output = ShellHelper.Bash("top -b -n1 | grep \"Cpu(s)\" | awk '{print $2 + $4}'");
                cpuRate = output.Trim();
            }
            else
            {
                string output = ShellHelper.Cmd("wmic", "cpu get LoadPercentage");
                cpuRate = output.Replace("LoadPercentage", string.Empty).Trim();
            }
            return cpuRate;
        }

        /// <summary>
        /// 获取系统运行时间
        /// </summary>
        /// <returns></returns>
        public static string GetRunTime()
        {
            string runTime = string.Empty;
            try
            {
                if (IsUnix())
                {
                    string output = ShellHelper.Bash("uptime -s").Trim();
                    runTime = DateTimeHelper.FormatTime(ParseToLong((DateTime.Now - ParseToDateTime(output)).TotalMilliseconds.ToString(CultureInfo.InvariantCulture).Split('.')[0]));
                }
                else
                {
                    string output = ShellHelper.Cmd("wmic", "OS get LastBootUpTime/Value");
                    string[] outputArr = output.Split('=', (char)StringSplitOptions.RemoveEmptyEntries);
                    if (outputArr.Length == 2)
                    {
                        runTime = DateTimeHelper.FormatTime(ParseToLong((DateTime.Now - ParseToDateTime(outputArr[1].Split('.')[0])).TotalMilliseconds.ToString(CultureInfo.InvariantCulture).Split('.')[0]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("获取runTime出错" + ex.Message);
            }
            return runTime;
        }



        public static CPUInfo GetCPUDetails()
        {
            int logicalProcessors = 0;
            int cores = 0;

            if (IsUnix())
            {
                string logicalOutput = ShellHelper.Bash("lscpu | grep '^CPU(s):' | awk '{print $2}'");
                logicalProcessors = int.Parse(logicalOutput.Trim(), CultureInfo.InvariantCulture);

                string coresOutput = ShellHelper.Bash("lscpu | grep 'Core(s) per socket:' | awk '{print $4}'");
                string socketsOutput = ShellHelper.Bash("lscpu | grep 'Socket(s):' | awk '{print $2}'");
                cores = int.Parse(coresOutput.Trim(), CultureInfo.InvariantCulture) * int.Parse(socketsOutput.Trim(), CultureInfo.InvariantCulture);
            }
            else
            {
                string output = ShellHelper.Cmd("wmic", "cpu get NumberOfCores,NumberOfLogicalProcessors /format:csv");
                string[] lines = output.Split(NewLineSeparators, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > 1)
                {
                    string[] values = lines[1].Split(',');

                    cores = int.Parse(values[1].Trim(), CultureInfo.InvariantCulture);
                    logicalProcessors = int.Parse(values[2].Trim(), CultureInfo.InvariantCulture);
                }
            }

            return new CPUInfo
            {
                LogicalProcessors = logicalProcessors,
                Cores = cores
            };
        }
    }
    public class CPUInfo
    {
        public int LogicalProcessors { get; set; }
        public int Cores { get; set; }
    }
    public class CPUMetrics
    {
        /// <summary>
        /// 内核数
        /// </summary>
        public int CoreTotal { get; set; }
        /// <summary>
        /// 逻辑处理器数
        /// </summary>
        public int LogicalProcessors { get; set; }
        /// <summary>
        /// CPU使用率%
        /// </summary>
        public double CPURate { get; set; }
        /// <summary>
        /// CPU空闲率%
        /// </summary>
        public double FreeRate { get; set; }
    }

    /// <summary>
    /// 内存信息
    /// </summary>
    public class MemoryMetrics
    {
        [JsonIgnore]
        public double Total { get; set; }
        [JsonIgnore]
        public double Used { get; set; }
        [JsonIgnore]
        public double Free { get; set; }

        public string UsedRam { get; set; } = string.Empty;

        /// <summary>
        /// 总内存 GB
        /// </summary>
        public string TotalRAM { get; set; } = string.Empty;
        /// <summary>
        /// 内存使用率 %
        /// </summary>
        public string RAMRate { get; set; } = string.Empty;
        /// <summary>
        /// 空闲内存
        /// </summary>
        public string FreeRam { get; set; } = string.Empty;
    }

    public class DiskInfo
    {
        /// <summary>
        /// 磁盘名
        /// </summary>
        public string DiskName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public long TotalFree { get; set; }
        public long TotalSize { get; set; }
        /// <summary>
        /// 已使用
        /// </summary>
        public long Used { get; set; }
        /// <summary>
        /// 可使用
        /// </summary>
        public long AvailableFreeSpace { get; set; }
        public decimal AvailablePercent { get; set; }
    }

    public class MemoryMetricsClient
    {
        #region 获取内存信息

        /// <summary>
        /// windows系统获取内存信息
        /// </summary>
        /// <returns></returns>
        public static MemoryMetrics GetWindowsMetrics()
        {
            string output = ShellHelper.Cmd("wmic", "OS get FreePhysicalMemory,TotalVisibleMemorySize /Value");
            MemoryMetrics metrics = new();
            string[] lines = output.Trim().Split('\n', (char)StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length <= 0)
            {
                return metrics;
            }

            string[] freeMemoryParts = lines[0].Split('=', (char)StringSplitOptions.RemoveEmptyEntries);
            string[] totalMemoryParts = lines[1].Split('=', (char)StringSplitOptions.RemoveEmptyEntries);

            metrics.Total = Math.Round(double.Parse(totalMemoryParts[1], CultureInfo.InvariantCulture) / 1024, 0);
            metrics.Free = Math.Round(double.Parse(freeMemoryParts[1], CultureInfo.InvariantCulture) / 1024, 0);//m
            metrics.Used = metrics.Total - metrics.Free;

            return metrics;
        }

        /// <summary>
        /// Unix系统获取
        /// </summary>
        /// <returns></returns>
        public static MemoryMetrics GetUnixMetrics()
        {
            string output = ShellHelper.Bash(@"
# 从 /proc/meminfo 文件中提取总内存
 total_mem=$(cat /proc/meminfo | grep -i ""MemTotal"" | awk '{print $2}')
 # 从 /proc/meminfo 文件中提取剩余内存
free_mem=$(cat /proc/meminfo | grep -i ""MemFree"" | awk '{print $2}')
# 显示提取的信息
echo $total_mem $used_mem $free_mem
 ");
            MemoryMetrics metrics = new();

            if (!string.IsNullOrWhiteSpace(output))
            {
                string[] memory = output.Split(' ', (char)StringSplitOptions.RemoveEmptyEntries);
                if (memory.Length >= 2)
                {
                    metrics.Total = Math.Round(double.Parse(memory[0], CultureInfo.InvariantCulture) / 1024, 0);

                    metrics.Free = Math.Round(double.Parse(memory[1], CultureInfo.InvariantCulture) / 1024, 0);//m
                    metrics.Used = metrics.Total - metrics.Free;
                }
            }
            return metrics;
        }
        #endregion
    }
}