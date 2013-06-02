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
        protected string[] databaseFileNames;
        protected double accuracyUpperLimit;


        public WigleWifiDatabase(string[] databaseFileNames)
        {


            // TODO: Complete member initialization
            this.databaseFileNames = databaseFileNames;
        }

        internal void IgnoreAbove(double upperLimit)
        {
            this.accuracyUpperLimit = upperLimit;
        }

        public List<Network> ImportFiles()
        {
            DataTable networks = null, locations = null;
            foreach (string databaseFile in this.databaseFileNames)
            {
                var networkDT = this.GetNetworksRaw(databaseFile);
                var locationsDT = this.GetLocationsRaw(databaseFile);

                if (networks == null) { networks = networkDT; }
                else
                {
                    Utilities.MergeIntoDatatable(networks, networkDT);                    
                }
                if (locations == null) { locations = locationsDT; }
                else
                {
                    Utilities.MergeIntoDatatable(locations, locationsDT);                    
                }
            }
            return ConstructNetworkObjects(networks, locations);
        }

        public DataTable GetNetworksRaw(string databaseFileName)
        {
            // bssid, ssid, ffrequency, capabilities, type (W, C)
            return QueryDbRaw(databaseFileName, "select * from network");
        }
        public DataTable GetLocationsRaw(string databaseFileName)
        {
            // bssid, ssid, ffrequency, capabilities, type (W, C)
            return QueryDbRaw(databaseFileName, string.Format("select * from location where accuracy <= {0}", this.accuracyUpperLimit));
        }

        public DataTable QueryDbRaw(string databaseFileName, string query)
        {
            Database d = new Database();
            DataTable resultsDT = null;
            try
            {
                string connString = string.Format("Data Source={0};Version=3;", databaseFileName);
                d.Connect(Database.theDatabaseType.SQLITE, connString);
                resultsDT = d.RunQuery(query);
            }
            catch { }
            finally
            {
                d.Close();
            }

            return resultsDT;
        }


        public List<Network> ConstructNetworkObjects(DataTable networks, DataTable locations)
        {
            

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
            return results;
        }






    
    }
}
