using BusCatch.VwMdl;
using System;
using Windows.Devices.Geolocation;
namespace TTC.Model2015
{
    [Serializable]
    public class FavVehStop
    {
        string id = null; double? mapCenterLat = null, mapCenterLon = null;
        public FavVehStop() { }
        public FavVehStop(string stopName_, string agncyTag_, string routeTag_, string vStopTag_, double lat_, double lon_)
        {
            AgncyTag = agncyTag_; StopName = string.Format("{0}-{1}", routeTag_, stopName_); RouteTag = routeTag_; VStopTag = vStopTag_; Lat = lat_; Lon = lon_;
        }
        public Geopoint Location { get { return new Geopoint(new BasicGeoposition() { Latitude = Lat, Longitude = Lon }); } }
        public Geopoint MapCenter { get { return new Geopoint(new BasicGeoposition() { Latitude = MapCenterLat, Longitude = MapCenterLon }); } }
        public string Id { get { if (id == null) id = string.Format(TtcViewModel._idfor, AgncyTag, RouteTag, VStopTag); return id; } }
        public double Lat { get; set; } = 35;
        public double Lon { get; set; } = 35;
        public double MapCenterLat { get { if (mapCenterLat == null) mapCenterLat = Lat; return mapCenterLat.Value; } set { mapCenterLat = value; } }
        public double MapCenterLon { get { if (mapCenterLon == null) mapCenterLon = Lon; return mapCenterLon.Value; } set { mapCenterLon = value; } }
        public double MapZoomLevel { get; set; } = 12.0012; public string StopName { get; set; }
        public string AgncyTag { get; set; }
        public string RouteTag { get; set; }
        public string VStopTag { get; set; }
        public bool IsGtfs => AgncyTag == "yrt";
    }
}