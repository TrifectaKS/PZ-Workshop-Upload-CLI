using Json.Net;
using PzWorkshopUploaderCLI.Models;
using System.IO;
using System.Text;

namespace PzWorkshopUploaderCLI
{
    [System.Serializable]
    internal class ConfigParser
    {
        public const string filename = "config.json";

        public static Config Load()
        {
            string jsonString = File.ReadAllText(filename, Encoding.UTF8);
            Config obj = JsonNet.Deserialize<Config>(jsonString);
            return obj;
        }
    }
}
