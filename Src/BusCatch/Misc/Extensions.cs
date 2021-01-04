using AsLink;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace AAV.AsLink.Misc
{
  public static class Exts
  {
    public static void ClearAddRangeSynch<T>(this Collection<T> trg, IEnumerable<T> src)
    {
      try { trg.Clear(); if (src != null) src.ToList().ForEach(trg.Add); } catch (Exception ex) { ex.Pop(); }
    }
    public static async void ClearAddRangeAuto<T>(this Collection<T> trg, IEnumerable<T> src)
    {
      // !!!! Debug.Assert(CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess, "*** is NOT on UI thread ***");

      try
      {
        await Task.Factory.StartNew(() =>        //Start a task to wait for UI.
         {
           Windows.Foundation.IAsyncAction ThreadPoolWorkItem = Windows.System.Threading.ThreadPool.RunAsync(async (source) =>
           {
             await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
             {
               trg.Clear();
               if (src != null) src.ToList().ForEach(trg.Add);
             });
           });
         });
      }
      catch (Exception ex) { ex.Pop(); }
    }
    public static async void ClearAddRange2<T>(this Collection<T> trg, IEnumerable<T> src)
    {
      // !!!! Debug.Assert(CoreApplication.MainView.CoreWindow.Dispatcher.HasThreadAccess, "*** is NOT on UI thread ***");

      try { await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { trg.Clear(); if (src != null) src.ToList().ForEach(trg.Add); }); } catch (Exception ex) { ex.Pop(); }
    }
  }
}
