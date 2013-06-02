using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetTopologySuite.IO;
using System.IO;
using log4net;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GeoAPI.Geometries;
using log4net.Config;
using NetTopologySuite.Operation.Overlay;
using System.Windows.Forms;

namespace sampleLocator
{
    class Program
    {
        private static ILog _log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            BasicConfigurator.Configure();


            string geoJSONdatafile = @"D:\research\wifidata\network_minspan_001.geojson";
            
            var networksInRange = new HashSet<string>(new string[] {
                "bc:ae:c5:c3:71:66",
                "00:20:a6:5d:5f:7c",
                "00:22:3f:7a:76:39"
            });


            var json = File.ReadAllText(geoJSONdatafile);
            var coll = (JObject)JsonConvert.DeserializeObject(json);
            List<JObject> found = new List<JObject>();
            List<Polygon> polygons = new List<Polygon>();

            foreach (JObject feature in coll["features"])
            {
                JObject atts = (JObject)feature["properties"];
                var bssid = (string)atts["bssid"];

                if (networksInRange.Contains(bssid))
                {
                    found.Add(feature);
                    //polygons.Add(ReadPolygon((JObject)feature["geometry"]));
                }
            }

            //argh.
            GeoJsonReader reader = new GeoJsonReader();
            //var result = reader.Read<Polygon>(json);
            for (int i = 0; i < found.Count; i++)
            {
                var feat = found[i];
                JObject geom = (JObject)feat["geometry"];
                Polygon p = ReadPolygon(geom);
                polygons.Add(p);

                

                if (p != null)
                {
                    _log.InfoFormat("found {0} : {1} ", feat["bssid"], p.ToText());
                }
            }


            IGeometry result = null;
            if (polygons.Count > 1) {
                
                for (int i = 0; i < polygons.Count - 1; i++)
                {
                    if ((result == null) || (result.IsEmpty)) {
                        result = polygons[i];
                    }
                    result = OverlayOp.Overlay(result, polygons[i + 1], SpatialFunction.Intersection);                   
                }
            }
            _log.InfoFormat("YOU ARE HERE {0} ", result);
            Clipboard.SetText(result.ToString());



            _log.Info("Done -- Copied to clipboard");
        }


        public static Polygon ReadPolygon(JObject geom)
        {
            JArray coords = (JArray)((JArray)geom["coordinates"])[0];
            List<Coordinate> coordArr = new List<Coordinate>();
            foreach (JArray coord in coords)
            {
                coordArr.Add(new Coordinate((double)coord[0], (double)coord[1]));
            }

            return new Polygon(new LinearRing(coordArr.ToArray()));
        }


    }



}
