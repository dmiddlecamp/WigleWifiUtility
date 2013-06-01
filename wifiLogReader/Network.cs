using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.Geometries;
using GeoAPI.Geometries;

namespace wifiLogReader
{   
    public class Network
    {
        public string bssid;
        public string ssid;
        public int frequency;
        public string capabilities;
        public string type;

        public List<Location> Observations;

        public IGeometry Geom;
    }
}
