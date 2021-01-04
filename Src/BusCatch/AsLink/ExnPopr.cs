using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace AsLink
{
  public static class ExnPopr
  {
    public static void Pop(this Exception ex, string optl = null, [CallerMemberName] string cmn = "", [CallerFilePath] string cfp = "", [CallerLineNumber] int cln = 0)
    {
      ex.Log(optl);

      if (Debugger.IsAttached)
        Debugger.Break();
      else
      {

        const int max = 1600;
        var dialog = new MessageDialog($"{cfp}({cln}): {cmn}()\r\n{(optl?.Length > 0 ? (optl?.Length > max ? (optl.Substring(0, max) + "...") : optl) + "\r\n" : "")}\n{ex.InnerMessages()}", "Exception");
        Task.Run(async () => await dialog.ShowAsync());
      }
    }
  }
}
///todo: buttons: https://stackoverflow.com/questions/22909329/universal-apps-messagebox-the-name-messagebox-does-not-exist-in-the-current