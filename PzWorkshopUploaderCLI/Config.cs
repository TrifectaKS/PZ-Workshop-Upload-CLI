using Json.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PzWorkshopUploaderCLI
{
    public class Config
    {
        public bool validateTags = false;
        public List<string> validTags = new List<string>();
    }

    [System.Serializable]
    public class ConfigParser
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
