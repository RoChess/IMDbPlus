using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
using MediaPortal.Configuration;

namespace IMDb
{    
    public class Replacements
    {
        public static string ReplacementsFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Rename dBase IMDb+ Scraper.xml");
        public static string CustomReplacementsFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Rename dBase IMDb+ Scraper (Custom).xml");

        public static string Version { get; set; }
        public static DateTime Published { get; set; }

        public static List<DBReplacement> GetAll(bool custom)
        {
            if (!custom && _coreReplacements != null) return _coreReplacements;
            if (custom && _customReplacements != null) return _customReplacements;

            string file = custom ? CustomReplacementsFile : ReplacementsFile;            

            if (!File.Exists(file)) return null;

            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(file);
            }
            catch (Exception e)
            {
                Logger.Error("Error reading Replacements XML file: {0}", e.Message);
                return null;
            }

            #region Details
            if (!custom)
            {
                // get date
                var publishedNode = document.SelectSingleNode("/imdbplus/details/published");
                if (publishedNode != null)
                {
                    try
                    {
                        int year = Convert.ToInt32(publishedNode.Attributes["year"].Value);
                        int month = Convert.ToInt32(publishedNode.Attributes["month"].Value);
                        int day = Convert.ToInt32(publishedNode.Attributes["day"].Value);

                        Published = new DateTime(year, month, day);
                    }
                    catch
                    {
                        Logger.Error("Error parsing Published Date from replacements database");
                    }
                }

                // get version
                var versionNode = document.SelectSingleNode("/imdbplus/details/version");
                if (versionNode != null)
                {
                    try
                    {
                        string major = versionNode.Attributes["major"].Value;
                        string minor = versionNode.Attributes["minor"].Value;
                        string point = versionNode.Attributes["point"].Value;
                        Version = string.Format("{0}.{1}.{2}", major, minor, point);          
                    }
                    catch
                    {
                        Logger.Error("Error parsing version from replacements database");
                    }
                }

            }
            #endregion

            // get imdbplus node
            var renames = document.SelectNodes("/imdbplus/rename");
            if (renames == null) return null;

            List<DBReplacement> replacements = new List<DBReplacement>();

            foreach (XmlNode rename in renames)
            {
                if (rename.Attributes == null) continue;

                DBReplacement replacement = new DBReplacement();
                foreach (XmlAttribute attribute in rename.Attributes)
                {
                    switch (attribute.Name)
                    {
                        case "id":
                            replacement.Id = attribute.Value;
                            break;

                        case "title":
                            replacement.Title = attribute.Value;
                            break;

                        case "sortby":
                            replacement.SortBy = attribute.Value;
                            break;
                    }
                }

                // add new replacement
                if (replacement.Id != "tt0000000")
                    replacements.Add(replacement);
            }

            if (!custom)
                _coreReplacements = new List<DBReplacement>(replacements);
            else
                _customReplacements = new List<DBReplacement>(replacements);

            return replacements;
        }

        public static void ClearCache(bool custom)
        {
            if (!custom)
                _coreReplacements = null;
            else
                _customReplacements = null;
        }

        static List<DBReplacement> _coreReplacements = null;
        static List<DBReplacement> _customReplacements = null;

    }

    public class DBReplacement
    {
        public string Title { get; set; }
        public string SortBy { get; set; }
        public string Id { get; set; }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", Title, Id);
        }
    }
}
