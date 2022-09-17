namespace FileKillerSharp
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Security.Principal;
    using System.Threading.Tasks;
    using static System.Console;
    using static System.Environment;
    using static System.IO.File;
    using static System.Threading.Tasks.Task;

    internal class Program
    {
        private static String path;
        private static String yesno;
        private static Boolean isAdmin;
        static List<Process> lockingProcesses;

        private static async Task Main()
        {
            while (true)
            {
                await Work();
            }
        }

        private static async Task Work()
        {
            await AdminCheck();
            await ValidatePath();
            await Confirmation();
            await Delay(1);
        }

        private static async Task AdminCheck()
        {
            try
            {
                WindowsIdentity user = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(user);
                isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                isAdmin = false;
            }

            if (isAdmin == true)
            {
                WriteLine("Program has administrator rights...");
                WriteLine();
            }

            if (isAdmin == false)
            {
                WriteLine("Program does not have administrator rights, some files will not be deletable!");
                WriteLine();
            }

            await Delay(1);
        }

        private static async Task ValidatePath()
        {
            WriteLine("Enter the path to the file or folder you want to force delete...");
            WriteLine();
            path = ReadLine();
            WriteLine();
            await Delay(1);
        }

        private static async Task Confirmation()
        {
            WriteLine("Are you sure you want to delete: " + path + " Y / N");
            WriteLine();
            yesno = ReadLine();
            WriteLine();

            switch (yesno)
            {
                case "n":
                    WriteLine("Abandoning deletion of file or folder!");
                    WriteLine();
                    WriteLine();
                    WriteLine();
                    WriteLine();

                    await Work();
                    break;
                case "N":
                    WriteLine("Abandoning deletion of file or folder!");
                    WriteLine();
                    WriteLine();
                    WriteLine();
                    WriteLine();

                    await Work();
                    break;
                case "y":
                    await FileKiller();
                    break;
                case "Y":
                    await FileKiller();
                    break;
            }

            if (yesno != "y" || yesno != "Y" || yesno != "n" || yesno != "N")
            {
                WriteLine("Enter a valid answer!");
                WriteLine();

                await Confirmation();
            }
        }

        private static async Task FileKiller()
        {
            if (Exists(path) && path != GetLogicalDrives()[0] && path != SystemDirectory)
            {
                try
                {
                    WriteLine("[FileKiller] Attempt at deleting the file or folder normally...");
                    WriteLine();

                    Delete(path);
                }
                catch (Exception ioError)
                {
                    WriteLine("[FileKiller] IO delete error: " + ioError);
                    WriteLine();

                    try
                    {
                        while (true)
                        {
                            try
                            {
                                lockingProcesses = WhoIsLocking(path);
                            }
                            catch (Exception lockingProcessFinder)
                            {
                                WriteLine("[FileKiller] Failed to find locking process: " + lockingProcessFinder);
                                WriteLine();
                            }

                            if (lockingProcesses.Count == 0)
                            {
                                WriteLine("[FileKiller] No locking processes found, trying again!");
                                WriteLine();
                                WriteLine();
                                WriteLine();
                                WriteLine();

                                await FileKiller();
                            }

                            if (lockingProcesses.Count > 0)
                            {
                                WriteLine("[FileKiller] Trying to kill all processes accessing the file or folder...");
                                WriteLine();

                                try
                                {
                                    foreach (Process p in lockingProcesses)
                                    {
                                        try
                                        {
                                            WriteLine("[FileKiller] Number of locking processes: " + lockingProcesses.Count);
                                            WriteLine();
                                            WriteLine("[FileKiller] Process name: " + p.ProcessName);
                                            WriteLine();
                                            WriteLine("[FileKiller] Window title" + p.MainWindowTitle);
                                            WriteLine();
                                            WriteLine("[FileKiller] Main module file path: " + p.MainModule.FileName);
                                            WriteLine();
                                            WriteLine();
                                            WriteLine();
                                            WriteLine();
                                            WriteLine("[FileKiller] About to kill locking process(es)!");
                                            WriteLine();

                                            try
                                            {
                                                p.Kill();

                                                try
                                                {
                                                    Delete(path);

                                                    WriteLine("[FileKiller] Killed all locking programs!");
                                                    WriteLine();
                                                    WriteLine("[FileKiller] Successfully deleted file!");
                                                    WriteLine();
                                                    WriteLine();
                                                    WriteLine();
                                                    WriteLine();

                                                    await Work();
                                                }
                                                catch (Exception deletefileerror)
                                                {
                                                    WriteLine("[FileKiller] Failed to delete file: " + deletefileerror);
                                                    WriteLine();
                                                }

                                            }
                                            catch (Exception killprocesserror)
                                            {
                                                WriteLine("[FileKiller] Failed to kill process: " + killprocesserror);
                                                WriteLine();
                                            }
                                        }
                                        catch (Exception writeProgramInfo)
                                        {
                                            WriteLine("[FileKiller] Failed to write info to console: " + writeProgramInfo);
                                            WriteLine();
                                        }
                                    }
                                }
                                catch (Exception foreachError)
                                {
                                    WriteLine("[FileKiller] Failed to loop current foreach iteration: " + foreachError);
                                    WriteLine();
                                }

                            }
                        }
                    }
                    catch (Exception loopingFailure)
                    {
                        WriteLine("[FileKiller] Failed to loop FileKiller error: " + loopingFailure);
                        WriteLine();
                    }
                }

                WriteLine();
                WriteLine();
                WriteLine();

                await Work();
            }
            if (!Exists(path))
            {
                WriteLine("[FileKiller] Path does not exist, type a valid path!");
                WriteLine();
                WriteLine();
                WriteLine();
                WriteLine();

                await Work();
            }
        }

        /// <summary>
        /// All credits for the below code go to the author Eric J. from this article:
        /// https://stackoverflow.com/questions/317071/how-do-i-find-out-which-process-is-locking-a-file-using-net
        /// </summary>
        /// 
        [StructLayout(LayoutKind.Sequential)]
        private struct RM_UNIQUE_PROCESS
        {
            public Int32 dwProcessId;
            public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
        }

        private const Int32 RmRebootReasonNone = 0;
        private const Int32 CCH_RM_MAX_APP_NAME = 255;
        private const Int32 CCH_RM_MAX_SVC_NAME = 63;

        private enum RM_APP_TYPE
        {
            RmUnknownApp = 0,
            RmMainWindow = 1,
            RmOtherWindow = 2,
            RmService = 3,
            RmExplorer = 4,
            RmConsole = 5,
            RmCritical = 1000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct RM_PROCESS_INFO
        {
            public RM_UNIQUE_PROCESS Process;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)]
            public String strAppName;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)]
            public String strServiceShortName;

            public RM_APP_TYPE ApplicationType;
            public UInt32 AppStatus;
            public UInt32 TSSessionId;
            [MarshalAs(UnmanagedType.Bool)]
            public Boolean bRestartable;
        }

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 RmRegisterResources(UInt32 pSessionHandle,
                                              UInt32 nFiles,
                                              String[] rgsFilenames,
                                              UInt32 nApplications,
                                              [In] RM_UNIQUE_PROCESS[] rgApplications,
                                              UInt32 nServices,
                                              String[] rgsServiceNames);

        [DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]
        private static extern Int32 RmStartSession(out UInt32 pSessionHandle, Int32 dwSessionFlags, String strSessionKey);

        [DllImport("rstrtmgr.dll")]
        private static extern Int32 RmEndSession(UInt32 pSessionHandle);

        [DllImport("rstrtmgr.dll")]
        private static extern Int32 RmGetList(UInt32 dwSessionHandle,
                                    out UInt32 pnProcInfoNeeded,
                                    ref UInt32 pnProcInfo,
                                    [In, Out] RM_PROCESS_INFO[] rgAffectedApps,
                                    ref UInt32 lpdwRebootReasons);

        /// <summary>
        /// Find out what process(es) have a lock on the specified file.
        /// </summary>
        /// <param name="path">Path of the file.</param>
        /// <returns>Processes locking the file</returns>
        /// <remarks>See also:
        /// http://msdn.microsoft.com/en-us/library/windows/desktop/aa373661(v=vs.85).aspx
        /// http://wyupdate.googlecode.com/svn-history/r401/trunk/frmFilesInUse.cs (no copyright in code at time of viewing)
        /// 
        /// </remarks>
        public static List<Process> WhoIsLocking(String path)
        {
            UInt32 handle;
            String key = Guid.NewGuid().ToString();
            List<Process> processes = new List<Process>();

            Int32 res = RmStartSession(out handle, 0, key);
            if (res != 0) throw new Exception("Could not begin restart session.  Unable to determine file locker.");

            try
            {
                const Int32 ERROR_MORE_DATA = 234;
                UInt32 pnProcInfoNeeded = 0,
                     pnProcInfo = 0,
                     lpdwRebootReasons = RmRebootReasonNone;

                String[] resources = new String[] { path }; // Just checking on one resource.

                res = RmRegisterResources(handle, (UInt32)resources.Length, resources, 0, null, 0, null);

                if (res != 0) throw new Exception("Could not register resource.");

                //Note: there's a race condition here -- the first call to RmGetList() returns
                //      the total number of process. However, when we call RmGetList() again to get
                //      the actual processes this number may have increased.
                res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, null, ref lpdwRebootReasons);

                if (res == ERROR_MORE_DATA)
                {
                    // Create an array to store the process results
                    RM_PROCESS_INFO[] processInfo = new RM_PROCESS_INFO[pnProcInfoNeeded];
                    pnProcInfo = pnProcInfoNeeded;

                    // Get the list
                    res = RmGetList(handle, out pnProcInfoNeeded, ref pnProcInfo, processInfo, ref lpdwRebootReasons);
                    if (res == 0)
                    {
                        processes = new List<Process>((Int32)pnProcInfo);

                        // Enumerate all of the results and add them to the 
                        // list to be returned
                        for (Int32 i = 0; i < pnProcInfo; i++)
                        {
                            try
                            {
                                processes.Add(Process.GetProcessById(processInfo[i].Process.dwProcessId));
                            }
                            // catch the error -- in case the process is no longer running
                            catch (ArgumentException) { }
                        }
                    }
                    else throw new Exception("Could not list processes locking resource.");
                }
                else if (res != 0) throw new Exception("Could not list processes locking resource. Failed to get size of result.");
            }
            finally
            {
                RmEndSession(handle);
            }

            return processes;
        }
    }
}
