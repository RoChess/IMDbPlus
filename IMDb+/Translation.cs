using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Text.RegularExpressions;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Localisation;

namespace IMDb
{
    public static class Translation
    {
        #region Private variables

        private static Dictionary<string, string> translations;
        private static Regex translateExpr = new Regex(@"\$\{([^\}]+)\}");
        private static string path = string.Empty;

        #endregion

        #region Constructor

        static Translation()
        {

        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the translated strings collection in the active language
        /// </summary>
        public static Dictionary<string, string> Strings
        {
            get
            {
                if (translations == null)
                {
                    translations = new Dictionary<string, string>();
                    Type transType = typeof(Translation);
                    FieldInfo[] fields = transType.GetFields(BindingFlags.Public | BindingFlags.Static);
                    foreach (FieldInfo field in fields)
                    {
                        translations.Add(field.Name, field.GetValue(transType).ToString());
                    }
                }
                return translations;
            }
        }

        public static string CurrentLanguage
        {
            get
            {
                string language = string.Empty;
                try
                {
                    language = GUILocalizeStrings.GetCultureName(GUILocalizeStrings.CurrentLanguage());
                }
                catch (Exception)
                {
                    language = CultureInfo.CurrentUICulture.Name;
                }
                return language;
            }
        }
        public static string PreviousLanguage { get; set; }

        #endregion

        #region Public Methods

        public static void Init()
        {
            translations = null;
            Logger.Info("Using language " + CurrentLanguage);

            path = Config.GetSubFolder(Config.Dir.Language, "IMDb+");

            if (!System.IO.Directory.Exists(path))
                System.IO.Directory.CreateDirectory(path);

            string lang = PreviousLanguage = CurrentLanguage;
            LoadTranslations(lang);

            // publish all available translation strings
            // so skins have access to them
            foreach (string name in Strings.Keys)
            {
                GUIUtils.SetProperty("#IMDb.Translation." + name + ".Label", Translation.Strings[name]);
            }
        }

        public static int LoadTranslations(string lang)
        {
            XmlDocument doc = new XmlDocument();
            Dictionary<string, string> TranslatedStrings = new Dictionary<string, string>();
            string langPath = string.Empty;
            try
            {
                langPath = Path.Combine(path, lang + ".xml");
                doc.Load(langPath);
            }
            catch (Exception e)
            {
                if (lang == "en")
                    return 0; // otherwise we are in an endless loop!

                if (e.GetType() == typeof(FileNotFoundException))
                    Logger.Warning("Cannot find translation file {0}. Falling back to English", langPath);
                else
                    Logger.Error("Error in translation xml file: {0}. Falling back to English", lang);

                return LoadTranslations("en");
            }
            foreach (XmlNode stringEntry in doc.DocumentElement.ChildNodes)
            {
                if (stringEntry.NodeType == XmlNodeType.Element)
                {
                    try
                    {
                        TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("Field").Value, stringEntry.InnerText);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("Error in Translation Engine", ex.Message);
                    }
                }
            }

            Type TransType = typeof(Translation);
            FieldInfo[] fieldInfos = TransType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (FieldInfo fi in fieldInfos)
            {
                if (TranslatedStrings != null && TranslatedStrings.ContainsKey(fi.Name))
                    TransType.InvokeMember(fi.Name, BindingFlags.SetField, null, TransType, new object[] { TranslatedStrings[fi.Name] });
                else
                    Logger.Info("Translation not found for field: {0}.  Using hard-coded English default.", fi.Name);
            }
            return TranslatedStrings.Count;
        }

        public static string GetByName(string name)
        {
            if (!Strings.ContainsKey(name))
                return name;

            return Strings[name];
        }

        public static string GetByName(string name, params object[] args)
        {
            return String.Format(GetByName(name), args);
        }

        /// <summary>
        /// Takes an input string and replaces all ${named} variables with the proper translation if available
        /// </summary>
        /// <param name="input">a string containing ${named} variables that represent the translation keys</param>
        /// <returns>translated input string</returns>
        public static string ParseString(string input)
        {
            MatchCollection matches = translateExpr.Matches(input);
            foreach (Match match in matches)
            {
                input = input.Replace(match.Value, GetByName(match.Groups[1].Value));
            }
            return input;
        }

        #endregion

        #region Translations / Strings

        /// <summary>
        /// These will be loaded with the language files content
        /// if the selected lang file is not found, it will first try to load en(us).xml as a backup
        /// if that also fails it will use the hardcoded strings as a last resort.
        /// </summary>

        // A
        public static string AddForeignTitle = "Add Foreign Title";
        public static string AddForeignTitleDescription = "Off = Keep title as-is\nOn = Add the original title in parentheses\n\nEnabling this setting will allow you to see the original title in parentheses after the English title.\n\nExample: Black Book (Zwartboek)";

        // B

        // C
        public static string CountryFilter = "Country Filter";
        public static string CountryFilterDescription = "There are some English language based movies that have a foreign title. So to aid in the proper detection of English titles it was needed to also filter on the country in which the movie was produced.\n\nDefault countries that do not use foreign titles are: 'us.ca.gb.ie.au.nz' (United States, Canada, Great Britain, Ireland, Australia and New Zealand)\n\nNote: The MediaPortal virtual keyboard does not support the '|' character, so a '.' was used instead to seperate the different languages. Be very careful in adjusting this setting as it can stop the function of the IMDb+ scraper.";

        // D
        public static string DefaultDescription = "Highlight any of the above options and a more detailed explanation will be shown here.";

        // E
       
        // F               
        public static string ForeignTitleFirst = "Foreign Title First";
        public static string ForeignTitleFirstDescription = "Off = English (Foreign)\nOn = Foreign (English)\n\nEnabling this setting shows the original foreign title first followed by the English title in parentheses.\n\nExample: Zwartboek (Black Book)";

        // G
      
        // H
       
        // I
        public static string IMDbMetaScore = "Metacritics Score";
        public static string IMDbMetaScoreDescription = "Off = Main imdb.com score is used\nOn = Use Metacritics Metascore instead\n\nThe imdb.com website also offers the Metacritics.com metascore. Enabling this setting will use that score, otherwise the IMDb score is used.\n\nNote: This setting requires that the 'IMDb Score' setting is also enabled and will therefore disable the RottenTomatoes method for missing summary and runtime info.";
        public static string IMDbScore = "IMDb Score";
        public static string IMDbScoreDescription = "Off = Use RottenTomatoes website\nOn = Restrict to imdb.com website only\n\nRottenTomatoes offers additional info, but if you want to restrict yourself to imdb.com then enable this setting.\n\nNote: This also disables getting missing summary, runtime and scoreing.";

        // J
    
        // L
        public static string LanguageFilter = "Language Filter";
        public static string LanguageFilterDescription = "Special filter to include other language titles besides English (en). Use the imdb.com HTML source to find the ISO 639-1 code after /language/.\n\nDefault language is English via: 'en'\n\nHowever if you are from Norway and you wish to have Norwegian movies use their original title and English titles for everything else, then use: 'en.no'\n\nNote: The MediaPortal virtual keyboard does not support the '|' character, so a '.' was used instead to seperate the different languages. Be very careful in adjusting this setting as it can stop the function of the IMDb+ scraper.";
        public static string LongSummary = "Long Summary";
        public static string LongSummaryDescription = "Off = Short summary to describe movie plot\nOn = Long summary (might contain spoilers)\n\nLong summaries offer more information, but can sometimes have spoilers contained within them. This is why this setting is disabled by default, but you can of course enable it if you prefer them.";

        // M     
        public static string MinImdbVotes = "Min. IMDb Votes";
        public static string MinImdbVotesDescription = "Off = Always use imdb.com score\nOn = Only use imdb.com scores with 20+ votes\n\nThe IMDb score is inflated a lot of times by the people who worked on the movie. This is why it is not uncommon for a movie with less then 20 votes to have a very high rating.\n\nIf you enable this setting then the IMDb scores with less then 20 votes are ignored.";

        // N      

        // O
        public static string OriginalTitle = "Original Title";
        public static string OriginalTitleDescription = "Off = Always force English title\nOn = Use original title for foreign movies\n\nForeign movies often have a non-English title.\n\nIf you want all your movies to use an English title, then keep this setting disabled.";

        // P
      
        // R
        public static string RefreshAllFields = "Refresh All Fields";
        public static string RefreshAllFieldsDescription = "Off = Only update a few things\nOn = Update everything\n\nThe information at IMDb is constantly updated and completed. For this reason it makes sense to once in a while update movies in your collection.\n\nEnabling this setting will force everything to be updated. Otherwise only empty, certifications, score and votes fields are updated on a refresh.\n\nNote: This setting disables the rename system for existing movies.";
        public static string RenameTitles = "Rename & Group";
        public static string RenameTitlesDescription = "Off = Use title as-is\nOn = Rename title so that series will group together\n\nThere are many movie series, and sometimes the titles do not start with the same prefix to group them together. To solve this, a rename database file is used that will rename a title based on the IMDb tt-ID number. The result is that movies are renamed so that they are easy to identify as being part of a series.\n\nExample: 'Batman VII: The Dark Knight Rises' will group nicely along with the other movies in the series.\n\nThe default rename titles are placed in a file located at: 'C:\\Rename dBase IMDb+ Scraper.xml'\n\nAny custom rename entries that you want should go into: 'C:\\Rename dBase IMDb+ Scraper (Custom).xml'\n\nIf those files do not exist on your system, please use the setting page of this plugin to reinstall them.\n\nYou can use notepad to edit the XML files, but it is preferred to use the editor option of this plugin to make sure that the contents of the XML files remain compliant.";
        public static string RottenMeter = "RT TomatoMeter";
        public static string RottenMeterDescription = "Off = RottenTomatoes 'Audience' ratings\nOn = RottenTomatoes 'TomatoMeter' ratings\n\nThere are many different ratings available at the RottenTomatoes website.\n\nTo use any of the TomatoMeter ratings you have to enable this setting, otherwise the 'Audience' one is used.";
        public static string RottenAverage = "RT Avg. Rating";
        public static string RottenAverageDescription = "Off = RottenTomatoes 'Percentage' rating\nOn = RottenTomatoes 'Average' rating\n\nWith this setting enabled the 'Average' rating value is used, otherwise the 'Percentage' value is.\n\nThe 'Percentage' rating is a special kind, in that it counts the amount of RottenTomatoes users who voted a certain way.";
        public static string RottenTopCritics = "RT Top Critics";
        public static string RottenTopCriticsDescription = "Off = RottenTomatoes 'All' critics\nOn = RottenTomatoes 'Top' critics\n\nThere is a TomatoMeter rating available for all of the critics and a select group of 'Top' critics.\n\nEnabling this setting will limit your rating to just the top ones.";

        // S
        public static string ScraperOptionsTitle = "IMDb+ Scraper Options";
        public static string SingleScore = "Single Score";
        public static string SingleScoreDescription = "Off = Use average rating system\nOn = Single rating value is used\n\nThere is a big difference sometimes between all the different ratings that are available. To compensate for that, an average value is used based on the ratings from imdb, metacritics and rottentomatoes.\n\nNote: If the 'IMDb Score' is enabled then the average rating of only IMDb and Metacritics is used when this setting is active for an average rating result.";
        public static string SpecialEditions = "Special Editions";
        public static string SpecialEditionsDescription = "Off = Keep title as-is\nOn = Add special editions tag to the title\n\nIf you make the IMDb tt-ID number available inside filename or via NFO files, then you can enable this setting for special edition tagging.\n\nYou can add the following terms to your filename inside parenthesis:\n\n - 3D\n - Extended\n - Unrated\n - Director's Cut\n - Alternate Ending\n\nYou can also have the term 'Edition' postfixed to the end of them if you wish.\n\nExample: \"Avatar (tt0499549) (3D).mkv\" will result in \"Avatar (3D)\"";

        // T
       
        // U
        public static string UkRating = "UK Rating";
        public static string UkRatingDescription = "Off = Use the American MPAA ratings\nOn = Enable the British BBFC ratings\n\nUse this setting for UK movie certifications.\n\nThis includes 'U' for Universal, 'PG' for Parental Guidance, 12A, 12, 15, 18 and finally 'R18' for Restricted 18.";

        // V
      
        // W
       
        // Y
       

        #endregion

    }

}