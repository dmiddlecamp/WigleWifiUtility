using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.IO;
using NetTopologySuite.Features;

namespace wifiLogReader
{
    /*
     * best / worst power levels for bubbles?
     * steamroller interpolation
     * 
     */


    class Program
    {
        static void Main(string[] args)
        {
            //expecting a database backup from Wigle-Wifi
            var databaseFiles = new string[] {
                @"D:\research\wifidata\backup-1370101505105.sqlite",
                @"D:\research\wifidata\backup-1370188598005.sqlite"
            };


            //open wifi log file
            //load tables into memory

            var d = new WigleWifiDatabase(databaseFiles);

            //just use manual values for now?
            d.IgnoreAbove(1000);

            //Read in our location / network tables
            var networks = d.ImportFiles();


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

            string primaryDatabaseFile = databaseFiles[0];
            string workingPath = System.IO.Path.GetDirectoryName(primaryDatabaseFile);
            Environment.CurrentDirectory = workingPath;


            FeatureCollection networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("networks_full.geojson", writer.Write(networkFeatures));

            networkFeatures = p.GetNetworksAsFeatures(networks, true);
            System.IO.File.WriteAllText("networks_hulls.geojson", writer.Write(networkFeatures));


            //try centering the network around the closest and furthest observation
            p.SetGeomAsCenteredRing(networks);
            networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("network_centered.geojson", writer.Write(networkFeatures));

            //try centering the network around the closest and furthest observation
            p.SetGeomAsMinimumCircle(networks);
            networkFeatures = p.GetNetworksAsFeatures(networks);
            System.IO.File.WriteAllText("network_minspan_001.geojson", writer.Write(networkFeatures));
            
            


            //pull out all our best observations by accuracy and power level
            //(this is the easiest way to get closest to the actual access point)
            FeatureCollection centroidFeatures = p.GetBestObvFeatures(networks);
            System.IO.File.WriteAllText("centers.geojson", writer.Write(centroidFeatures));




        }


    }
}
