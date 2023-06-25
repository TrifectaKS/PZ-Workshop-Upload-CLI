using PzWorkshopUploaderCLI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PzWorkshopUploaderCLI.Helpers
{
    internal class WorkshopFileParser
    {
        public static WorkshopFileData Parse(string path)
        {
            WorkshopFileData data = new WorkshopFileData();
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            StringBuilder descriptionSb = new StringBuilder();

            foreach(var line in lines)
            {
                if (line.StartsWith("id="))
                    data.Id = line.Replace("id=", "");
                else if (line.StartsWith("title="))
                    data.Title = line.Replace("title=", "");
                else if (line.StartsWith("description="))
                    descriptionSb.Append(line.Replace("description=", "") + "\n");
                else if (line.StartsWith("tags="))
                    data.Tags = line.Replace("tags=", "").Split(',', ';');
            }

            data.Tags = ValidateTags(data.Tags);
            data.Description = descriptionSb.ToString();

            return data;
        }

        private static IList<string> ValidateTags(IList<string> tags)
        {
            var config = ConfigParser.Load();

            if (!config.validateTags)
            {
                return tags;
            }

            for (int i = 0; i < tags.Count; i++)
            {
                if (!config.validTags.Contains(tags[i]))
                {
                    Console.WriteLine("Removing invalid tag: " + tags[i]);
                    tags.RemoveAt(i);
                    i--;
                }
            }

            return tags;
        }
    }
}
