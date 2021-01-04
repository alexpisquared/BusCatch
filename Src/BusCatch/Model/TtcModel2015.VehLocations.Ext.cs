using BusCatch.Model;
using System;
using System.Collections.Generic;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;

namespace TTC.Model2015
{
    public class bodyVehicleEx : bodyVehicle
    {
        public MapPolyline PredictionStick //nogo:
        {
            get
            {
                var stops = new List<BasicGeoposition>
                {
                    new BasicGeoposition() { Latitude = (double)lat, Longitude = (double)lon },

                    //todo: forecastedPosition = curPos + speed * (secsSinceReport + secsSinceWebCall) * heading
                    new BasicGeoposition() { Latitude = (double)lat + .0001, Longitude = (double)lon }
                };

                mapElement.Path = new Geopath(stops);
                mapElement.ZIndex = (int)ZIdx.Vector;
                //mapElement.FillColor = Colors.Red;
                mapElement.StrokeColor = Colors.Blue;
                mapElement.StrokeThickness = 3;
                mapElement.StrokeDashed = false;

                return mapElement;
            }
        }
        MapPolyline mapElement = new MapPolyline();
    }
    public static class bodyVehicleExt
    {
        public static MapPolyline DevPredictionGStick(this bodyVehicle bv) // G-shaped indicator
        {
            var stops = new List<BasicGeoposition>
            {
                new BasicGeoposition() { Latitude = (double)bv.lat, Longitude = (double)bv.lon },
                new BasicGeoposition() { Latitude = (double)bv.lat + .0001 * bv.SecsSinceReportAbs, Longitude = (double)bv.lon },      //todo: forecastedPosition = curPos + speed * (secsSinceReport + secsSinceWebCall) * heading
                new BasicGeoposition() { Latitude = (double)bv.lat + .0001 * bv.SecsSinceReportAbs, Longitude = (double)bv.lon + .001 }
            };

            MapPolyline mapElement = new MapPolyline
            {
                Path = new Geopath(stops),
                ZIndex = (int)ZIdx.Vector,
                //mapElement.FillColor = Colors.Red;
                StrokeColor = bv.secsSinceReport < 0 ? Colors.Blue : Colors.Red,
                StrokeThickness = 1,
                StrokeDashed = false
            };

            return mapElement;
        }
    }
}
