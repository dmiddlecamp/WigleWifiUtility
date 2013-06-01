using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;

namespace wifiLogReader
{
    public class Processor
    {



        public void ConstructPolygons(List<Network> networks)
        {
            //so... we have tons of observations for any given network.
            for (int i = 0; i < networks.Count; i++)
            {
                var net = networks[i];

                var points = new List<Coordinate>(net.Observations.Count);
                for (int a = 0; a < net.Observations.Count; a++)
                {
                    var obv = net.Observations[a];
                    var pt = new Coordinate(obv.lon, obv.lat);
                    //pt.UserData = obv;
                    points.Add(pt);
                }

                net.Ring = new Polygon(new LinearRing(points.ToArray()));
            }
        }

    }
}
