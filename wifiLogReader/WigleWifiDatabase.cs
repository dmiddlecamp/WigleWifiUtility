using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;


namespace wifiLogReader
{
    public class WigleWifiDatabase
    {
        //private static ILog _log = LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected string databaseFileName;
        protected double accuracyUpperLimit;


        public WigleWifiDatabase(string databaseFileName)
        {


            // TODO: Complete member initialization
            this.databaseFileName = databaseFileName;
        }

        internal void IgnoreUnder(double upperLimit)
        {
            this.accuracyUpperLimit = upperLimit;
        }


        public List<Network> ImportFile()
        {
            //generate Network objects

            string connString = string.Format("Data Source={0};Version=3;", this.databaseFileName);
            Database d = new Database();
            d.Connect(Database.theDatabaseType.SQLITE, connString);

            //network
            // bssid, ssid, ffrequency, capabilities, type (W, C)
            var networks = d.RunQuery("select * from network");

            //location
            //id, bssid, level, lat, lon, altitude, accuracy, time
            var locations = d.RunQuery(string.Format("select * from location where accuracy <= {0}", this.accuracyUpperLimit));


            var results = new List<Network>(1024 * 16);
            var dict = new Dictionary<string, Network>();
            foreach (DataRow row in networks.Rows)
            {
                var network = new Network()
                {
                    bssid = row["bssid"] as string,
                    ssid = row["ssid"] as string,
                    frequency = Utilities.GetAs(row["frequency"], -1),
                    capabilities = row["capabilities"] as string,
                    type = row["type"] as string,
                    Observations = new List<Location>(64)
                };

                dict[network.bssid] = network;
                results.Add(network);
            }

            double lastProgress = 0;

            for(int i=0;i<locations.Rows.Count;i++)
            {
                DataRow row = locations.Rows[i];

                var location = new Location()
                {
                    
                    bssid = row["bssid"] as string,
                    id = Utilities.GetAs<Int64>(row["_id"], -1),
                    level = Utilities.GetAs<Int64>(row["level"], -1),
                    lat = Utilities.GetAs<double>(row["lat"], -1),
                    lon = Utilities.GetAs<double>(row["lon"], -1),
                    altitude = Utilities.GetAs<double>(row["altitude"], -1),
                    accuracy = Utilities.GetAs<double>(row["accuracy"],-1),
                    time = Utilities.GetAs<long>(row["time"], - 1),
                };

                if (dict.ContainsKey(location.bssid))
                {
                    dict[location.bssid].Observations.Add(location);
                }

                double progress = (i / (double)locations.Rows.Count);
                if ((progress != lastProgress) && (progress % 5 == 0))
                {
                    Console.WriteLine(string.Format("Importing Locations, {0}% done...", progress));
                    //_log.InfoFormat("Importing Locations, {0}% done...", progress);
                }

            }


            d.Close();
            return results;
        }






    
    }
}
