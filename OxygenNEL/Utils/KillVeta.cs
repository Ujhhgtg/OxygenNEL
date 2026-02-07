// using System;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using System.Linq;
// using System.Runtime.InteropServices;
// using System.Runtime.InteropServices.ComTypes;
// using System.ServiceProcess;
// using System.Text;
// using System.Threading;
// using System.Threading.Tasks;
// using OxygenNEL.Component;
// using Serilog;
//
// namespace OxygenNEL.Utils;
//
// internal static class KillVeta
// {
//     public static (bool found, bool success, string? dllPath) Run()
//     {
//         var keyword = "Veta";
//         var all = Process.GetProcesses();
//         var targets = all.Where(p =>
//         {
//             try { return p.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0; }
//             catch { return false; }
//         }).ToArray();
//         if (targets.Length == 0) return (false, true, null);
//         
//         EnableAllPrivileges();
//         
//         string? dllPath = null;
//         foreach (var p in targets)
//         {
//             try
//             {
//                 var exe = TryGetProcessPath(p);
//                 var dir = string.IsNullOrEmpty(exe) ? null : Path.GetDirectoryName(exe);
//                 
//                 var killed = TryKillProcess(p);
//                 
//                 if (!killed)
//                     throw new Exception($"无法终止进程 {p.ProcessName}({p.Id})");
//                 
//                 p.WaitForExit(5000);
//                 
//                 if (!string.IsNullOrEmpty(dir))
//                 {
//                     dllPath = dir;
//                     TryDeleteDirectory(dir);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Log.Error(ex, "终止失败: {Name}({PID})", p.ProcessName, p.Id);
//                 Log.Error("检测到 Veta 脱盒，终止失败");
//                 TryNotify("检测到 Veta 脱盒，终止失败", ToastLevel.Error);
//                 return (true, false, dllPath);
//             }
//         }
//         Log.Information("检测到 Veta 脱盒，已帮您成功终止并删除");
//         Log.Information("OxygenNEL提醒您，不要使用假协议脱盒");
//         TryNotify("检测到 Veta 脱盒，已帮您成功终止并删除", ToastLevel.Success);
//         TryNotify("OxygenNEL提醒您，不要使用假协议脱盒", ToastLevel.Warning);
//         return (true, true, dllPath);
//     }
//
//     private const uint PROCESS_TERMINATE = 0x0001;
//     private const uint PROCESS_QUERY_INFORMATION = 0x0400;
//     private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
//     private const uint MAXIMUM_ALLOWED = 0x2000000;
//     private const uint TOKEN_DUPLICATE = 0x0002;
//     private const uint TOKEN_IMPERSONATE = 0x0004;
//     private const uint TOKEN_QUERY = 0x0008;
//     private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
//     private const int SecurityDelegation = 3;
//     private const int TokenImpersonation = 2;
//     private const string SE_DEBUG_NAME = "SeDebugPrivilege";
//     private const string SE_TAKE_OWNERSHIP_NAME = "SeTakeOwnershipPrivilege";
//     private const string SE_RESTORE_NAME = "SeRestorePrivilege";
//     private const string SE_BACKUP_NAME = "SeBackupPrivilege";
//     private const string SE_SECURITY_NAME = "SeSecurityPrivilege";
//     private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
//     private const uint DELETE = 0x00010000;
//     private const uint FILE_SHARE_DELETE = 0x00000004;
//     private const uint FILE_SHARE_READ = 0x00000001;
//     private const uint FILE_SHARE_WRITE = 0x00000002;
//     private const uint OPEN_EXISTING = 3;
//     private const uint FILE_FLAG_DELETE_ON_CLOSE = 0x04000000;
//     private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
//     private const uint FILE_OPEN = 0x00000001;
//     private const uint FILE_DELETE_ON_CLOSE = 0x00001000;
//     private const uint FILE_OPEN_FOR_BACKUP_INTENT = 0x00004000;
//     private const uint FILE_OPEN_REPARSE_POINT = 0x00200000;
//     private const uint SYNCHRONIZE = 0x00100000;
//     private const uint FILE_READ_ATTRIBUTES = 0x0080;
//     private const uint FILE_WRITE_ATTRIBUTES = 0x0100;
//     private const uint OBJ_CASE_INSENSITIVE = 0x00000040;
//     private const uint FileDispositionInformation = 13;
//     private const uint FileDispositionInformationEx = 64;
//     private const uint FILE_DISPOSITION_DELETE = 0x1;
//     private const uint FILE_DISPOSITION_POSIX_SEMANTICS = 0x2;
//     private const uint FILE_DISPOSITION_IGNORE_READONLY_ATTRIBUTE = 0x10;
//     private static readonly int MaxFileParallelism = Math.Max(Environment.ProcessorCount * 4, 16);
//     private static readonly int MaxDirParallelism = Math.Max(Environment.ProcessorCount * 2, 8);
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct LUID { public uint LowPart; public int HighPart; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct LUID_AND_ATTRIBUTES { public LUID Luid; public uint Attributes; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID_AND_ATTRIBUTES Privileges; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct IO_STATUS_BLOCK { public IntPtr Status; public IntPtr Information; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct RM_UNIQUE_PROCESS { public int dwProcessId; public FILETIME ProcessStartTime; }
//
//     [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
//     private struct RM_PROCESS_INFO
//     {
//         public RM_UNIQUE_PROCESS Process;
//         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string strAppName;
//         [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string strServiceShortName;
//         public int ApplicationType; public uint AppStatus; public uint TSSessionId;
//         [MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
//     }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct UNICODE_STRING { public ushort Length; public ushort MaximumLength; public IntPtr Buffer; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct OBJECT_ATTRIBUTES { public int Length; public IntPtr RootDirectory; public IntPtr ObjectName; public uint Attributes; public IntPtr SecurityDescriptor; public IntPtr SecurityQualityOfService; }
//
//     [StructLayout(LayoutKind.Sequential)]
//     private struct FILE_DISPOSITION_INFORMATION_EX { public uint Flags; }
//
//     [DllImport("kernel32.dll", SetLastError = true)]
//     private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);
//     [DllImport("kernel32.dll", SetLastError = true)]
//     private static extern bool CloseHandle(IntPtr hObject);
//     [DllImport("kernel32.dll", SetLastError = true)]
//     private static extern bool TerminateProcess(IntPtr hProcess, uint uExitCode);
//     [DllImport("kernel32.dll", SetLastError = true)]
//     private static extern IntPtr GetCurrentProcess();
//     [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//     private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);
//     [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
//     private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
//     [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
//     private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, IntPtr lpTokenAttributes, int ImpersonationLevel, int TokenType, out IntPtr phNewToken);
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool ImpersonateLoggedOnUser(IntPtr hToken);
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool RevertToSelf();
//     [DllImport("advapi32.dll", SetLastError = true)]
//     private static extern bool SetThreadToken(IntPtr Thread, IntPtr Token);
//     [DllImport("ntdll.dll")]
//     private static extern int NtSetInformationFile(IntPtr FileHandle, out IO_STATUS_BLOCK IoStatusBlock, IntPtr FileInformation, uint Length, uint FileInformationClass);
//     [DllImport("ntdll.dll")]
//     private static extern int NtOpenFile(out IntPtr FileHandle, uint DesiredAccess, ref OBJECT_ATTRIBUTES ObjectAttributes, out IO_STATUS_BLOCK IoStatusBlock, uint ShareAccess, uint OpenOptions);
//     [DllImport("ntdll.dll")]
//     private static extern int NtDeleteFile(ref OBJECT_ATTRIBUTES ObjectAttributes);
//     [DllImport("ntdll.dll")]
//     private static extern int NtClose(IntPtr Handle);
//     [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
//     private static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);
//     [DllImport("rstrtmgr.dll")]
//     private static extern int RmEndSession(uint pSessionHandle);
//     [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
//     private static extern int RmRegisterResources(uint pSessionHandle, uint nFiles, string[] rgsFilenames, uint nApplications, IntPtr rgApplications, uint nServices, IntPtr rgsServiceNames);
//     [DllImport("rstrtmgr.dll")]
//     private static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[]? rgAffectedApps, ref uint lpdwRebootReasons);
//
//     private static void EnableAllPrivileges()
//     {
//         EnablePrivilege(SE_DEBUG_NAME);
//         EnablePrivilege(SE_TAKE_OWNERSHIP_NAME);
//         EnablePrivilege(SE_RESTORE_NAME);
//         EnablePrivilege(SE_BACKUP_NAME);
//         EnablePrivilege(SE_SECURITY_NAME);
//     }
//
//     private static bool EnablePrivilege(string privilegeName)
//     {
//         var hToken = IntPtr.Zero;
//         try
//         {
//             if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out hToken)) return false;
//             if (!LookupPrivilegeValue(null, privilegeName, out var luid)) return false;
//             var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Privileges = new LUID_AND_ATTRIBUTES { Luid = luid, Attributes = SE_PRIVILEGE_ENABLED } };
//             AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
//             return Marshal.GetLastWin32Error() != 1300;
//         }
//         catch { return false; }
//         finally { if (hToken != IntPtr.Zero) CloseHandle(hToken); }
//     }
//
//     private static bool TryKillProcess(Process p)
//     {
//         try { if (TryKillWithTrustedInstaller(p)) return true; } catch { }
//         try { p.Kill(true); return true; } catch { }
//         try { if (TryKillWithNativeApi(p.Id)) return true; } catch { }
//         try { if (TryKillWithTaskkill(p.Id)) return true; } catch { }
//         return false;
//     }
//
//     private static bool TryKillWithTrustedInstaller(Process targetProcess)
//     {
//         var success = false;
//         RunAsTrustedInstaller(() =>
//         {
//             var hProcess = OpenProcess(PROCESS_TERMINATE, false, targetProcess.Id);
//             if (hProcess != IntPtr.Zero)
//             {
//                 try { if (TerminateProcess(hProcess, 0)) { Log.Information("TrustedInstaller 权限终止进程成功: {Name}({PID})", targetProcess.ProcessName, targetProcess.Id); success = true; } }
//                 finally { CloseHandle(hProcess); }
//             }
//         });
//         return success;
//     }
//
//     private static bool TryKillWithNativeApi(int pid)
//     {
//         var hProcess = OpenProcess(PROCESS_TERMINATE, false, pid);
//         if (hProcess == IntPtr.Zero) return false;
//         try { return TerminateProcess(hProcess, 0); }
//         finally { CloseHandle(hProcess); }
//     }
//
//     private static bool TryKillWithTaskkill(int pid)
//     {
//         try
//         {
//             var psi = new ProcessStartInfo { FileName = "taskkill", Arguments = $"/F /T /PID {pid}", UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
//             using var proc = Process.Start(psi);
//             proc?.WaitForExit(10000);
//             return proc?.ExitCode == 0;
//         }
//         catch { return false; }
//     }
//
//     private static IntPtr GetTrustedInstallerToken()
//     {
//         IntPtr tiToken = IntPtr.Zero, processHandle = IntPtr.Zero;
//         try
//         {
//             using var sc = new ServiceController("TrustedInstaller");
//             if (sc.Status != ServiceControllerStatus.Running) { sc.Start(); sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30)); Thread.Sleep(500); }
//             var tiProcesses = Process.GetProcessesByName("TrustedInstaller");
//             if (tiProcesses.Length == 0) return IntPtr.Zero;
//             processHandle = OpenProcess(PROCESS_QUERY_INFORMATION, false, tiProcesses[0].Id);
//             if (processHandle == IntPtr.Zero) return IntPtr.Zero;
//             if (!OpenProcessToken(processHandle, TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY, out tiToken)) return IntPtr.Zero;
//             return tiToken;
//         }
//         catch { return IntPtr.Zero; }
//         finally { if (processHandle != IntPtr.Zero) CloseHandle(processHandle); }
//     }
//
//     private static void RunAsTrustedInstaller(Action action)
//     {
//         IntPtr tiToken = IntPtr.Zero, dupToken = IntPtr.Zero;
//         var impersonating = false;
//         try
//         {
//             tiToken = GetTrustedInstallerToken();
//             if (tiToken == IntPtr.Zero) return;
//             if (!DuplicateTokenEx(tiToken, MAXIMUM_ALLOWED, IntPtr.Zero, SecurityDelegation, TokenImpersonation, out dupToken)) return;
//             if (!ImpersonateLoggedOnUser(dupToken)) return;
//             impersonating = true;
//             SetThreadToken(IntPtr.Zero, dupToken);
//             action();
//         }
//         finally
//         {
//             if (impersonating) { RevertToSelf(); SetThreadToken(IntPtr.Zero, IntPtr.Zero); }
//             if (dupToken != IntPtr.Zero) CloseHandle(dupToken);
//             if (tiToken != IntPtr.Zero) CloseHandle(tiToken);
//         }
//     }
//
//     private static void TryDeleteDirectory(string dirPath)
//     {
//         if (!Directory.Exists(dirPath)) return;
//         Log.Information("开始删除目录: {Dir}", dirPath);
//         KillAllLockingProcessesParallel(dirPath);
//         var deleted = false;
//         RunAsTrustedInstaller(() => { DeleteDirectoryNtApi(dirPath); if (!Directory.Exists(dirPath)) deleted = true; });
//         if (deleted) { Log.Information("目录已删除: {Dir}", dirPath); return; }
//         DeleteDirectoryNtApi(dirPath);
//         if (!Directory.Exists(dirPath)) { Log.Information("目录已删除: {Dir}", dirPath); return; }
//         Log.Warning("目录可能未完全删除: {Dir}", dirPath);
//     }
//
//     private static void KillAllLockingProcessesParallel(string dirPath)
//     {
//         try
//         {
//             var files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories);
//             if (files.Length == 0) return;
//             var killedPids = new ConcurrentDictionary<int, byte>();
//             var lockingProcs = new ConcurrentBag<Process>();
//             Parallel.ForEach(files.Chunk(Math.Max(1, files.Length / MaxFileParallelism + 1)), new ParallelOptions { MaxDegreeOfParallelism = MaxFileParallelism }, batch =>
//             {
//                 foreach (var file in batch) { try { foreach (var proc in GetLockingProcesses(file)) lockingProcs.Add(proc); } catch { } }
//             });
//             Parallel.ForEach(lockingProcs.DistinctBy(p => p.Id), new ParallelOptions { MaxDegreeOfParallelism = 8 }, proc =>
//             {
//                 if (killedPids.TryAdd(proc.Id, 0)) { Log.Debug("终止锁定进程: {Name}({PID})", proc.ProcessName, proc.Id); TryKillProcess(proc); }
//             });
//         }
//         catch { }
//     }
//
//     private static Process[] GetLockingProcesses(string filePath)
//     {
//         var processes = new List<Process>();
//         var result = RmStartSession(out var sessionHandle, 0, Guid.NewGuid().ToString());
//         if (result != 0) return [];
//         try
//         {
//             string[] files = [filePath];
//             if (RmRegisterResources(sessionHandle, 1, files, 0, IntPtr.Zero, 0, IntPtr.Zero) != 0) return [];
//             uint pnProcInfoNeeded = 0, pnProcInfo = 0, lpdwRebootReasons = 0;
//             result = RmGetList(sessionHandle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);
//             if (result == 234 && pnProcInfoNeeded > 0)
//             {
//                 var processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
//                 pnProcInfo = pnProcInfoNeeded;
//                 if (RmGetList(sessionHandle, out _, ref pnProcInfo, processInfo, ref lpdwRebootReasons) == 0)
//                     for (var i = 0; i < pnProcInfo; i++) { try { processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId)); } catch { } }
//             }
//         }
//         finally { RmEndSession(sessionHandle); }
//         return [.. processes];
//     }
//
//     private static void DeleteDirectoryNtApi(string dirPath)
//     {
//         if (!Directory.Exists(dirPath)) return;
//         string[] files;
//         try { files = Directory.GetFiles(dirPath, "*", SearchOption.AllDirectories); } catch { return; }
//         Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = MaxFileParallelism }, NtDeleteFileFast);
//         string[] dirs;
//         try { dirs = Directory.GetDirectories(dirPath, "*", SearchOption.AllDirectories); } catch { dirs = []; }
//         if (dirs.Length > 0)
//         {
//             var dirsByDepth = dirs.GroupBy(d => d.Split(Path.DirectorySeparatorChar).Length).OrderByDescending(g => g.Key);
//             foreach (var group in dirsByDepth)
//                 Parallel.ForEach(group, new ParallelOptions { MaxDegreeOfParallelism = MaxDirParallelism }, NtDeleteDirectoryFast);
//         }
//         NtDeleteDirectoryFast(dirPath);
//     }
//
//     private static void NtDeleteFileFast(string filePath)
//     {
//         var ntPath = @"\??\" + filePath;
//         if (TryNtDeleteFile(ntPath)) return;
//         if (TryNtSetDispositionEx(ntPath)) return;
//         if (TryNtSetDisposition(ntPath)) return;
//         TryCreateFileDeleteOnClose(filePath);
//     }
//
//     private static bool TryNtDeleteFile(string ntPath)
//     {
//         var unicodePtr = Marshal.AllocHGlobal(Marshal.SizeOf<UNICODE_STRING>());
//         var stringPtr = Marshal.StringToHGlobalUni(ntPath);
//         try
//         {
//             var unicodeString = new UNICODE_STRING { Length = (ushort)(ntPath.Length * 2), MaximumLength = (ushort)((ntPath.Length + 1) * 2), Buffer = stringPtr };
//             Marshal.StructureToPtr(unicodeString, unicodePtr, false);
//             var oa = new OBJECT_ATTRIBUTES { Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>(), ObjectName = unicodePtr, Attributes = OBJ_CASE_INSENSITIVE };
//             return NtDeleteFile(ref oa) == 0;
//         }
//         finally { Marshal.FreeHGlobal(stringPtr); Marshal.FreeHGlobal(unicodePtr); }
//     }
//
//     private static bool TryNtSetDispositionEx(string ntPath)
//     {
//         var unicodePtr = Marshal.AllocHGlobal(Marshal.SizeOf<UNICODE_STRING>());
//         var stringPtr = Marshal.StringToHGlobalUni(ntPath);
//         var fileHandle = IntPtr.Zero;
//         try
//         {
//             var unicodeString = new UNICODE_STRING { Length = (ushort)(ntPath.Length * 2), MaximumLength = (ushort)((ntPath.Length + 1) * 2), Buffer = stringPtr };
//             Marshal.StructureToPtr(unicodeString, unicodePtr, false);
//             var oa = new OBJECT_ATTRIBUTES { Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>(), ObjectName = unicodePtr, Attributes = OBJ_CASE_INSENSITIVE };
//             var status = NtOpenFile(out fileHandle, DELETE | SYNCHRONIZE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, ref oa, out _, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_OPEN | FILE_OPEN_REPARSE_POINT | FILE_OPEN_FOR_BACKUP_INTENT);
//             if (status != 0 || fileHandle == IntPtr.Zero) return false;
//             var dispositionEx = new FILE_DISPOSITION_INFORMATION_EX { Flags = FILE_DISPOSITION_DELETE | FILE_DISPOSITION_POSIX_SEMANTICS | FILE_DISPOSITION_IGNORE_READONLY_ATTRIBUTE };
//             var dispPtr = Marshal.AllocHGlobal(Marshal.SizeOf<FILE_DISPOSITION_INFORMATION_EX>());
//             try { Marshal.StructureToPtr(dispositionEx, dispPtr, false); status = NtSetInformationFile(fileHandle, out _, dispPtr, (uint)Marshal.SizeOf<FILE_DISPOSITION_INFORMATION_EX>(), FileDispositionInformationEx); return status == 0; }
//             finally { Marshal.FreeHGlobal(dispPtr); }
//         }
//         finally { if (fileHandle != IntPtr.Zero) NtClose(fileHandle); Marshal.FreeHGlobal(stringPtr); Marshal.FreeHGlobal(unicodePtr); }
//     }
//
//     private static bool TryNtSetDisposition(string ntPath)
//     {
//         var unicodePtr = Marshal.AllocHGlobal(Marshal.SizeOf<UNICODE_STRING>());
//         var stringPtr = Marshal.StringToHGlobalUni(ntPath);
//         var fileHandle = IntPtr.Zero;
//         try
//         {
//             var unicodeString = new UNICODE_STRING { Length = (ushort)(ntPath.Length * 2), MaximumLength = (ushort)((ntPath.Length + 1) * 2), Buffer = stringPtr };
//             Marshal.StructureToPtr(unicodeString, unicodePtr, false);
//             var oa = new OBJECT_ATTRIBUTES { Length = Marshal.SizeOf<OBJECT_ATTRIBUTES>(), ObjectName = unicodePtr, Attributes = OBJ_CASE_INSENSITIVE };
//             var status = NtOpenFile(out fileHandle, DELETE | SYNCHRONIZE, ref oa, out _, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_OPEN | FILE_DELETE_ON_CLOSE | FILE_OPEN_REPARSE_POINT | FILE_OPEN_FOR_BACKUP_INTENT);
//             if (status != 0 || fileHandle == IntPtr.Zero) return false;
//             var dispPtr = Marshal.AllocHGlobal(1);
//             try { Marshal.WriteByte(dispPtr, 1); status = NtSetInformationFile(fileHandle, out _, dispPtr, 1, FileDispositionInformation); return status == 0; }
//             finally { Marshal.FreeHGlobal(dispPtr); }
//         }
//         finally { if (fileHandle != IntPtr.Zero) NtClose(fileHandle); Marshal.FreeHGlobal(stringPtr); Marshal.FreeHGlobal(unicodePtr); }
//     }
//
//     private static void TryCreateFileDeleteOnClose(string filePath)
//     {
//         var hFile = CreateFile(@"\\?\" + filePath, DELETE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_DELETE_ON_CLOSE | FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
//         if (hFile != new IntPtr(-1)) CloseHandle(hFile);
//     }
//
//     private static void NtDeleteDirectoryFast(string dirPath)
//     {
//         if (!Directory.Exists(dirPath)) return;
//         if (TryNtDeleteFile(@"\??\" + dirPath)) return;
//         try { Directory.Delete(dirPath, true); } catch { }
//     }
//
//     private static string? TryGetProcessPath(Process p)
//     {
//         var h = IntPtr.Zero;
//         try
//         {
//             h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, p.Id);
//             if (h == IntPtr.Zero) return null;
//             var sb = new StringBuilder(1024);
//             var size = sb.Capacity;
//             if (QueryFullProcessImageName(h, 0, sb, ref size)) return sb.ToString(0, size);
//             return null;
//         }
//         catch { return null; }
//         finally { if (h != IntPtr.Zero) CloseHandle(h); }
//     }
//
//     private static void TryNotify(string text, ToastLevel level)
//     {
//         try { var dq = MainWindow.UIQueue; if (dq != null) dq.TryEnqueue(() => NotificationHost.ShowGlobal(text, level)); }
//         catch { }
//     }
// }
//
