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
        public void SetGeomAsMinimumCircle(List<Network> networks)
        {
            int numSkipped = 0;

            //so... we have tons of observations for any given network.
            for (int i = 0; i < networks.Count; i++)
            {
                var net = networks[i];
                if (net.Observations.Count == 0)
                {
                    //TODO: Fix -- cell towers
                    continue;
                }

                //max distance of around 0.001 seems to work well here
                net.Geom = MinimumSpanningCircle(net, 1, 0.001); 

                if (net.Geom != null)
                {
                    //if (net.Geom.Area > 0.05)
                    //{
                    //    numSkipped++;
                    //    net.Geom = null;
                    //    continue;
                    //}
                }
            }
            Console.WriteLine("SKipped " + numSkipped + " circles ");
        }

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
                if ((closest == null) || (furthest == null))
                {
                    continue;
                }

                var closestPT = closest.GetPoint();
                var furthestPT = furthest.GetPoint();
                var dist = closestPT.Distance(furthestPT);

                if (dist > 0.005)
                {
                    //too big
                    continue;
                }

                //draw circle.
                var fact = new NetTopologySuite.Utilities.GeometricShapeFactory( GeometryFactory.Default);
                var pt = closestPT;
                var env = new Envelope(pt.X + dist, pt.X - dist, pt.Y + dist, pt.Y - dist);

                fact.Envelope = env;
                
                fact.Centre = closestPT.Coordinate;
                fact.NumPoints = 24;
                fact.Size = dist;
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

                    var pt = obv.GetPoint();
                    if (pt == null) { continue; }

                    points.Add(pt.Coordinate);
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
                if (net.Geom == null)
                {
                    continue;
                }

                var atts = new AttributesTable();
                atts.AddAttribute("ssid", net.ssid);
                atts.AddAttribute("bssid", net.bssid);
                atts.AddAttribute("capabilities", net.capabilities);
                atts.AddAttribute("type", net.type);
                atts.AddAttribute("frequency", net.frequency);
                atts.AddAttribute("AREA", net.Geom.Area);

                if (hulls)
                {
                    net.Geom = net.Geom.ConvexHull();
                }

                var f = new Feature(net.Geom, atts);
                features.Add(f);
            }

            return new FeatureCollection(new Collection<Feature>(features));
        }

        public IGeometry MinimumSpanningCircle(Network net, double percentile, double maxDist)
        {
            //get all obvs, sort by accuracy (best to worst (asc))
            //grab XX percentile of points

            var bestObv = PickBestObservation(net, true);
            if (bestObv == null)
            {
                return null;
            }
            var centerPt = bestObv.GetPoint();

            var ordered = net.Observations.OrderBy(o => o.accuracy).ToList();
            
            var maxCount = ordered.Count() * percentile;
            var points = new List<Coordinate>();
            for (int a = 0; a < maxCount; a++)
            {
                var obv = ordered[a];
                if (!obv.HasPoint()) { continue; }

                var pt = obv.GetPoint();
                if (pt.Distance(centerPt) > maxDist)
                    continue;

                points.Add(pt.Coordinate);
            }

            if (points.Count > 3)
            {
                points.Add(points[0]);  //close it
            }
            else { return null; }

            var poly = new Polygon(new LinearRing(points.ToArray()));

            var c = new NetTopologySuite.Algorithm.MinimumBoundingCircle(poly);
            return c.GetCircle();
        }


        public Location PickBestObservation(Network net, bool closest = true)
        {
            Location bestObv = null;
            for (int a = 0; a < net.Observations.Count; a++)
            {
                var obv = net.Observations[a];
                if (!obv.HasPoint()) { continue; }
                
                if (bestObv == null) { bestObv = obv; }
                /**
                 * I think there is something wrong here, hard to describe
                 */
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


                var f = new Feature(bestObv.GetPoint(), atts);
                features.Add(f);
            }

            return new FeatureCollection(new Collection<Feature>(features));
        }



      
    }
}
