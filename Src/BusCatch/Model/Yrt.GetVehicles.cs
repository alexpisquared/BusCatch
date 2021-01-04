using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusCatch.Model
{
	public class Rootobject
	{
		public List<GVehicle> Property1 { get; set; }
	}

	public class GVehicle
	{
		public string TripId { get; set; }
		public int Status { get; set; }
		public int Sequence { get; set; }
		public string VehicleId { get; set; }
		public string Label { get; set; }
		public string LicensePlate { get; set; }
		public int Bearing { get; set; }
		public float Latitude { get; set; }
		public float Longitude { get; set; }
		public Trip Trip { get; set; }
	}

	public class Trip
	{
		public string TripId { get; set; }
		public string Headsign { get; set; }
		public string ShortName { get; set; }
		public string DirectionId { get; set; }
		public string BlockId { get; set; }
		public Route Route { get; set; }
		public Calendar Calendar { get; set; }
	}

	public class Route
	{
		public string RouteId { get; set; }
		public string ShortName { get; set; }
		public string LongName { get; set; }
		public string Description { get; set; }
		public string Color { get; set; }
		public string ColorText { get; set; }
		public string RouteNameHtml { get; set; }
		public string OrderBy { get; set; }
	}

	public class Calendar
	{
		public string CalendarId { get; set; }
		public bool Monday { get; set; }
		public bool Tuesday { get; set; }
		public bool Wednesday { get; set; }
		public bool Thursday { get; set; }
		public bool Friday { get; set; }
		public bool Saturday { get; set; }
		public bool Sunday { get; set; }
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
	}





	public class GStop
	{
		public string Id { get; set; }
		public string StopId { get; set; }
		public string Code { get; set; }
		public string Name { get; set; }
		public float Latitude { get; set; }
		public float Longitude { get; set; }
	}




	public class StopPreds
	{
		public string status { get; set; }
		public Stoptime[] stopTimes { get; set; }
	}

	public class Stoptime
	{
		public string TripId { get; set; }
		public string VehicleId { get; set; }
		public string HeadSign { get; set; }
		public string Name { get; set; }
		public int Minutes { get; set; }
		public DateTime ArrivalDateTime { get; set; }
	}

}