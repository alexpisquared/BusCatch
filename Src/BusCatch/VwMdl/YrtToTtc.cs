using BusCatch.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using TTC.Model2015;

namespace BusCatch.VwMdl
{
  public static class YrtToTtc
  {
    internal static RouteConfig yrt2ttcStops(List<GStop> gstops)
    {
      var rlst = new List<bodyRouteL>();

      var rc = new RouteConfig
      {
        route = new bodyRoute()
      };

      rc.route.latMax = gstops.Max(r => r.Latitude);
      rc.route.latMin = gstops.Min(r => r.Latitude);
      rc.route.lonMax = gstops.Max(r => r.Longitude);
      rc.route.lonMin = gstops.Min(r => r.Longitude);

      rc.route.stop = new bodyRouteStop[gstops.Count];
      for (int i = 0; i < gstops.Count; i++)
      {
        if (!int.TryParse(gstops[i].Code, out int fourDifitCode))
          fourDifitCode = -1;

        rc.route.stop[i] = new bodyRouteStop
        {
          tag = gstops[i].StopId,       // 835, Code 3675  - tag is used to id the stop in TTC
          stopId = fourDifitCode,       //  gstops[i].Code,         // 3700, Id-Guid
          title = gstops[i].Name,
          lat = gstops[i].Latitude,
          lon = gstops[i].Longitude,
        };
      }

      return rc;
    }
    internal static VehLocations yrt2ttcVehs(List<GVehicle> vehicles)
    {
      var rc = new VehLocations
      {
        vehicle = new bodyVehicle[vehicles.Count]
      };

      for (int i = 0; i < vehicles.Count; i++)
      {
        rc.vehicle[i] = new bodyVehicle
        {
          heading = (short)(vehicles[i].Bearing == 0 ? -90 : vehicles[i].Bearing), // yrt has no bearing; turn busses on their weels then. Sep`19
          lat = vehicles[i].Latitude,
          lon = vehicles[i].Longitude,
          id = vehicles[i].VehicleId
        };
      }

      return rc;
    }
    internal static RouteList ttc2yrtRoutes(List<GVehicle> vehicles)
    {
      var routes = new List<bodyRouteL>();

      foreach (GVehicle veh in vehicles)
      {
        if (routes.All(r => string.CompareOrdinal(r.tag, veh.Trip.Route.RouteId) != 0))
          routes.Add(new bodyRouteL { tag = veh.Trip.Route.RouteId, title = veh.Trip.Route.RouteNameHtml });
      }

      return new RouteList { route = routes.OrderBy(r => r.title).ToArray() };
    }
    internal static PredictnLst Preds(StopPreds p)
    {
      var rv = new PredictnLst
      {
        predictions = new bodyPredictions()
      };
      rv.predictions.direction = new bodyPredictionsDirection[1];
      rv.predictions.direction[0] = new bodyPredictionsDirection
      {
        prediction = new bodyPredictionsDirectionPrediction[p.stopTimes.Count()]
      };

      int i = 0;
      foreach (Stoptime st in p.stopTimes)
      {
        rv.predictions.direction[0].prediction[i] = new bodyPredictionsDirectionPrediction
        {
          minutes = (byte)st.Minutes,
          epochTime = (ulong)(st.ArrivalDateTime - new DateTime(1970, 1, 1)).TotalMilliseconds,
          branch = st.VehicleId, //st.Minutes.ToString(), // st.TripId // "936986"
          vehicle = st.VehicleId //Jn29 - sometimes OK
        };

        i++;
      }

      return rv;
    }
  }
}
