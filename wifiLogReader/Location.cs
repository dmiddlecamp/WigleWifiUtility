using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace wifiLogReader
{
    public class Location
    {
        public Int64 id;
        public string bssid;
        public Int64 level;
        public double lat;
        public double lon;
        public double altitude;
        public double accuracy;
        public long time;

        public IPoint GetPoint()
        {
            return new Point(this.lat, this.lon);
        }

    }



}
