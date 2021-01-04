using AsLink;
using MVVM.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TTC.Svc;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        public /*async Task*/ void TestMethod1()
        {
            try
            {
                //loadRoutesForSelectAgency("yrt", true);

                //_isGtfs = true;
                //_prevRoute = "16";

                //await loadPlotRouteForCurAgncyRoute(false);


                _prevRoute = "6";
                _prevVStop = "856";

                //var rt = YrtToTtc.Preds(await WebSvc.GetGtfsPredcts(_prevRoute, _prevVStop));

                //var rrr = await GetPreds();
            }
            catch (Exception ex)
            {
                ex.Pop();
                Debugger.Break();
            }
        }
    }
}
