using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace AsLink
{
    public static class ExnLogr
    {
        public static string Log(this Exception ex, string optl = null, [CallerMemberName] string cmn = "", [CallerFilePath] string cfp = "", [CallerLineNumber] int cln = 0)
        {
            var msgForPopup = $"Exception at {cfp}({cln}): {cmn}()\t{optl}\r\n{ex.InnerMessages()}";

            Debug.WriteLine($"\r\n{DateTime.Now:HH:mm:ss} - {msgForPopup}"); // Trace.WriteLine($"\r\n\n{DateTime.Now:HH:mm:ss} - {msgForPopup}");

            if (Debugger.IsAttached)
                Debugger.Break();
            else
                BeepEr();

            //todo: catch (Exception fatalEx) { Environment.FailFast("An error occured whilst reporting an error.", fatalEx); }//tu: http://blog.functionalfun.net/2013/05/how-to-debug-silent-crashes-in-net.html //tu: Capturing dump files with Windows Error Reporting: Db a key at HKEY_LOCAL_MACHINE\Software\Microsoft\Windows\Windows Error Reporting\LocalDumps\[Your Application Exe FileName]. In that key, create a string value called DumpFolder, and set it to the folder where you want dumps to be written. Then create a DWORD value called DumpType with a value of 2.

            return msgForPopup;
        }
        public static string InnerMessages(this Exception ex)
        {
            var sb = new StringBuilder();
            while (ex != null)
            {
                sb.AppendLine($" - {ex.Message}");
                ex = ex.InnerException;
            }

            return sb.ToString();
        }
        public static string InnermostMessage(this Exception ex)
        {
            while (ex != null)
            {
                if (ex.InnerException == null)
                    return ex.Message;

                ex = ex.InnerException;
            }

            return "This is very-very odd.";
        }

        #region Proposal
        public static void BeepEr()
        {
#if !QUIET_EXN
            //Task.Run(() => {
            Beep(6000, 40);
            Beep(6000, 40);
            Beep(6000, 40);
            Beep(5000, 80);
            //});
#else
      BeepFD(10000, 25);
#endif
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")] public static extern bool Beep(int freq, int dur); // public static void Beep(int freq, int dur) { }    // 
        #endregion
    }
}
