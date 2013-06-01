using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.IO;
using NetTopologySuite.Features;

namespace wifiLogReader
{
    class Program
    {
        static void Main(string[] args)
        {
            //expecting a database backup from Wigle-Wifi
            string databaseFileName = @"D:\research\wifidata\backup-1370101505105.sqlite";

            //open wifi log file
            //load tables into memory

            var d = new WigleWifiDatabase(databaseFileName);

            //just use manual values for now?
            d.IgnoreAbove(1000);

            //Read in our location / network tables
            var networks = d.ImportFile();


            //ideas:
            //  auto-calibrate using histogram by accuracy, grab 90th percentile?
            //d.CalibrateAccuracy();


            //read all points, assemble into polygons
            var p = new Processor();

            //1.) create polygons of all observations of a given wifi network
            p.SetGeomAsSimpleRing(networks);

            //SetGeomAsCenteredRing

            //2.) export to geojson?
            GeoJsonWriter writer = new GeoJsonWriter();

            string workingPath = System.IO.Path.GetDirectoryName(databaseFileName);
            Environment.CurrentDirectory = workingPath;


            FeatureCollection networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("networks_full.geojson", writer.Write(networkFeatures));

            networkFeatures = p.GetNetworksAsFeatures(networks, true);
            System.IO.File.WriteAllText("networks_hulls.geojson",  writer.Write(networkFeatures));


            //try centering the network around the closest and furthest observation
            p.SetGeomAsCenteredRing(networks);
            networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("network_centered.geojson", writer.Write(networkFeatures));

            //try centering the network around the closest and furthest observation
            p.SetGeomAsMinimumCircle(networks);
            networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("network_minspan75.geojson", writer.Write(networkFeatures));



            //pull out all our best observations by accuracy and power level
            //(this is the easiest way to get closest to the actual access point)
            FeatureCollection centroidFeatures = p.GetBestObvFeatures(networks);
            System.IO.File.WriteAllText("centers.geojson", writer.Write(centroidFeatures));


        }


    }
}
