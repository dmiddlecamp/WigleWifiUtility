using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using System.Collections.ObjectModel;

namespace wifiLogReader
{
    public class Processor
    {


        public void SetGeomAsCenteredRing(List<Network> networks)
        {
            /*      . . ..
            *    .      ...
            *   .  .   X.  .
            *     .   .   .
            *     .. .. 
            * .   . . .   .
            * 
            */


            //1.) pick the highest power level observation with the best accuracy
            //2.) find the lowest power level reading with the best accuracy
            //calculate the distance, smooth it a bit, and draw a circle that size.




            //so... we have tons of observations for any given network.
            for (int i = 0; i < networks.Count; i++)
            {
                var net = networks[i];
                if (net.Observations.Count == 0)
                {
                    //TODO: Fix -- cell towers
                    continue;
                }

                var closest = PickBestObservation(net, true);
                var furthest = PickBestObservation(net, false);

                var closestPT = closest.GetPoint();
                var furthestPT = furthest.GetPoint();
                var dist = closestPT.Distance(furthestPT);

                //draw circle.
                var fact = new NetTopologySuite.Utilities.GeometricShapeFactory();
                fact.Centre = closestPT.Coordinate;
                fact.Width = dist;
                net.Geom = fact.CreateCircle();

            }
        }

        public void SetGeomAsSimpleRing(List<Network> networks)
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

                if (points.Count < 3)
                {
                    continue;
                }
                else
                {
                    //close that ring
                    points.Add(points[0]);

                }

                net.Geom = new Polygon(new LinearRing(points.ToArray()));
            }
        }

        /// <summary>
        /// Convert a list of 'network' objects (with whatever geom they have) into geojson Features
        /// </summary>
        /// <param name="networks"></param>
        /// <returns></returns>
        public FeatureCollection GetNetworksAsFeatures(List<Network> networks, bool hulls = false)
        {
            var features = new List<Feature>(networks.Count);

            for (int i = 0; i < networks.Count; i++)
            {
                var net = networks[i];
                var atts = new AttributesTable();
                atts.AddAttribute("ssid", net.ssid);
                atts.AddAttribute("bssid", net.bssid);
                atts.AddAttribute("capabilities", net.capabilities);
                atts.AddAttribute("type", net.type);
                atts.AddAttribute("frequency", net.frequency);

                if (hulls)
                {
                    net.Geom = net.Geom.ConvexHull();
                }

                var f = new Feature(net.Geom, atts);
                features.Add(f);
            }

            return new FeatureCollection(new Collection<Feature>(features));
        }


        public Location PickBestObservation(Network net, bool closest = true)
        {
            Location bestObv = null;
            for (int a = 0; a < net.Observations.Count; a++)
            {
                var obv = net.Observations[a];
                if (bestObv == null) { bestObv = obv; }

                if (obv.accuracy < bestObv.accuracy)
                {
                    if (closest)
                    {
                        //closest best
                        if (obv.level > bestObv.level)
                        {
                            bestObv = obv;
                        }
                    }
                    else
                    {
                        //furthest best
                        if (obv.level < bestObv.level)
                        {
                            bestObv = obv;
                        }
                    }
                }
            }

            return bestObv;
        }


        public FeatureCollection GetBestObvFeatures(List<Network> networks)
        {
            var features = new List<Feature>(networks.Count);

            for (int i = 0; i < networks.Count; i++)
            {
                var net = networks[i];
                Location bestObv = PickBestObservation(net);

                if (bestObv == null)
                {
                    //cell tower?
                    continue;
                }



                var atts = new AttributesTable();
                atts.AddAttribute("ssid", net.ssid);
                atts.AddAttribute("bssid", net.bssid);
                atts.AddAttribute("accuracy", bestObv.accuracy);
                atts.AddAttribute("level", bestObv.level);


                var f = new Feature(new Point(bestObv.lat, bestObv.lon), atts);
                features.Add(f);
            }

            return new FeatureCollection(new Collection<Feature>(features));
        }


    }
}
