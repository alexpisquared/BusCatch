using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusCatch.Model
{
  public enum ViewModes { VehTrackg, AgncySeln, RouteList, RmvFvs_NOTUSED }
  public enum TxtSrchModes { Ini, Agncy, Route, VStop }
  public enum ZIdx { BusStop = 1, Vector = 2, VBase, VectorBase, MinKm, Bus0, MyLocation }
}
