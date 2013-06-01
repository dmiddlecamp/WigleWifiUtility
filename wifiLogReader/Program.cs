using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            d.IgnoreUnder(1000);

            //Read in our location / network tables
            var networks = d.ImportFile();
            

            //ideas:
            //  auto-calibrate using histogram by accuracy, grab 90th percentile?
            //d.CalibrateAccuracy();


            //read all points, assemble into polygons
            var p = new Processor();
            p.ConstructPolygons(networks);

            //1.) create polygons of all observations of a given wifi network
            //2.) export to geojson?




        }
    }
}
