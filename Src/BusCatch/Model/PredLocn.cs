using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TTC.Model2015;

namespace TTC.Model2015
{
  public class PredLocn //todo: consider this as an alternative for the case if prediction can exists without location.
  {
    public bodyVehicle VehLcn { get; set; }
    public bodyPredictionsDirectionPrediction Predictn { get; set; }
  }
}
