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

        public bool HasPoint()
        {
            return !(Utilities.IsCrazyDouble(this.lat) || Utilities.IsCrazyDouble(this.lon));
        }

        public IPoint GetPoint()
        {
            if (Utilities.IsCrazyDouble(this.lat) || Utilities.IsCrazyDouble(this.lon))
            {
                return null;
            }

            return new Point(this.lat, this.lon);
        }

    }



}
