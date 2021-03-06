﻿using System;
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

        #region Private Methods
        static int LoadTranslations(string lang)
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
                        TranslatedStrings.Add(stringEntry.Attributes.GetNamedItem("name").Value, stringEntry.InnerText.NormalizeTranslation());
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
                    Logger.Info("Translation not found for name: {0}. Using hard-coded English default.", fi.Name);
            }
            return TranslatedStrings.Count;
        }

        /// <summary>
        /// Temp workaround to remove unwanted chars from Transifex
        /// </summary>
        static string NormalizeTranslation(this string input)
        {
            input = input.Replace("\\'", "'");
            input = input.Replace("\\\"", "\"");
            input = input.Replace("\\n", "\n");
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
        public static string AddForeignTitle = "Add the foreign title";
        public static string AddForeignTitleDescription = "Off = Keep title as-is" + "\n" + "On = Add the original title in parentheses" + "\n" + "" + "\n" + "Enabling this setting will allow you to see the original title in parentheses after the English title." + "\n" + "" + "\n" + "Example: Black Book (Zwartboek)";

        // B
        public static string BoolOn = "On";
        public static string BoolOff = "Off";

        // C
        public static string Cancel = "Cancel";
        public static string CountryFilter = "Advanced: Country filter";
        public static string CountryFilterDescription = "There are some English language based movies that have a foreign title. So to aid in the proper detection of English titles it was needed to also filter on the country in which the movie was produced." + "\n" + "" + "\n" + "Default countries that do not use foreign titles are: 'us.ca.gb.ie.au.nz' (United States, Canada, Great Britain, Ireland, Australia and New Zealand)" + "\n" + "" + "\n" + "Note: The MediaPortal virtual keyboard does not support the '|' character, so a '.' was used instead to seperate the different languages. Be very careful in adjusting this setting as it can stop the function of the IMDb+ scraper.";

        // D
        public static string DefaultDescription = "Highlight any of the above options and a more detailed explanation will be shown here.";

        // E        

        // F
        public static string First = "First";
        public static string FixMissingSummary = "Force user-review for empty summary";
        public static string FixMissingSummaryDescription = "Off = Keep summary empty" + "\n" + "On = Force user-review as summary" + "\n" + "" + "\n" + "Not every movie has a summary available yet at the imdb.com website. In some cases, the same movie does however have a user-review. This setting allows you to use this user-review then instead of showing nothing." + "\n" + "" + "\n" + "Note: Be warned that user-reviews can contain spoilers.";
        public static string ForceIMDbPlus = "Force IMDb+";
        public static string ForceIMDbPlusDescription = "Are you ready to force all your existing" + "\n" + "movies, imported with the default IMDb" + "\n" + "scraper, to start using the IMDb+ one?";
        public static string ForceIMDbPlusComplete = "Successfully switched {0}/{1} movies" + "\n" + "over to the IMDb+ scraper.";
        public static string ForeignTitleFirst = "Start the title with the foreign one first";
        public static string ForeignTitleFirstDescription = "Off = English (Foreign)" + "\n" + "On = Foreign (English)" + "\n" + "" + "\n" + "Enabling this setting shows the original foreign title first followed by the English title in parentheses." + "\n" + "" + "\n" + "Example: Zwartboek (Black Book)";

        // G

        // H

        // I
        public static string IMDbMetaScore = "Metacritics MetaScore";
        public static string IMDbMetaScoreDescription = "Off = Main imdb.com score is used" + "\n" + "On = Use Metacritics Metascore instead" + "\n" + "" + "\n" + "The imdb.com website also offers the Metacritics.com metascore. Enabling this setting will use that score, otherwise the IMDb score is used." + "\n" + "" + "\n" + "Note: This setting requires that the 'IMDb Score' setting is also enabled and will therefore disable the RottenTomatoes method for missing summary and runtime info.";
        public static string IMDbScore = "IMDb Score";
        public static string IMDbScoreDescription = "Off = Use RottenTomatoes website" + "\n" + "On = Restrict to imdb.com website only" + "\n" + "" + "\n" + "RottenTomatoes offers additional info, but if you want to restrict yourself to imdb.com then enable this setting." + "\n" + "" + "\n" + "Note: This also disables getting missing summary, runtime and scoreing.";
        public static string InfoPluginVersion = "Plugin Version: v{0}";
        public static string InfoScraperAuthor = "Scraper Author: {0}";
        public static string InfoScraperVersion = "Scraper Version: v{0}";
        public static string InfoScraperPriority = "Scraper Priority: {0}";
        public static string InfoScraperPublished = "Scraper Published: {0}";
        public static string InfoScraperLastUpdateCheck = "Scraper Last Update Check: {0}";
        public static string InfoMoviesIMDbPlusPrimary = "Movies using IMDb+ as scraper source: {0}";
        public static string InfoMoviesOtherPlusPrimary = "Movies that use a different scraper: {0}";
        public static string InfoReplacementsVersion = "Replacements Version: v{0}";
        public static string InfoReplacementsPublished = "Replacements Published: {0}";
        public static string InfoReplacementEntries = "Replacement Entries: {0}";
        public static string InfoCustomReplacementEntries = "Replacement Entries (Custom): {0}";
        public static string IntervalSixHours = "6 Hours";
        public static string IntervalTwelveHours = "12 Hours";
        public static string IntervalTwentyFourHours = "24 Hours";
        public static string IntervalTwoDays = "2 Days";
        public static string IntervalThreeDays = "3 Days";
        public static string IntervalFourDays = "4 Days";
        public static string IntervalSevenDays = "7 Days";


        // J

        // K

        // L
        public static string LanguageFilter = "Advanced: Language filter";
        public static string LanguageFilterDescription = "Special filter to include other language titles besides English (en). Use the imdb.com HTML source to find the ISO 639-1 code after /language/." + "\n" + "" + "\n" + "Default language is English via: 'en'" + "\n" + "" + "\n" + "However if you are from Norway and you wish to have Norwegian movies use their original title and English titles for everything else, then use: 'en.no'" + "\n" + "" + "\n" + "Note: The MediaPortal virtual keyboard does not support the '|' character, so a '.' was used instead to seperate the different languages. Be very careful in adjusting this setting as it can stop the function of the IMDb+ scraper.";
        public static string LongSummary = "Use long summaries which may contain spoilers";
        public static string LongSummaryDescription = "Off = Short summary to describe movie plot" + "\n" + "On = Long summary (might contain spoilers)" + "\n" + "" + "\n" + "Long summaries offer more information, but can sometimes have spoilers contained within them. This is why this setting is disabled by default, but you can of course enable it if you prefer them.";
        public static string LastScraperUpdate = "Last Scraper Update";

        // M
        public static string MinIMDbVotes = "Minimum amount of IMDb votes required";
        public static string MinIMDbVotesDescription = "Off = Always use imdb.com score" + "\n" + "On = Only use imdb.com scores with 20+ votes" + "\n" + "" + "\n" + "The IMDb score is inflated a lot of times by the people who worked on the movie. This is why it is not uncommon for a movie with less then 20 votes to have a very high rating." + "\n" + "" + "\n" + "If you enable this setting then the IMDb scores with less then 20 votes are ignored.";
        public static string MultiSelectDialog = "Multi-Select Dialog";

        // N
        public static string No = "No";
        public static string NumberOfMovies = "{0}/{1} Movies";
        public static string NumberOfMovies2 = "{0} Movies";
        public static string NoSourcesFound = "No movies found that can be forced" + "\n" + "into using the IMDb+ scraper script!";
        public static string NullSource = "No source, but usable IMDb-ID";

        // O
        public static string OneWriterDirector = "Limit import to only one writer and director";
        public static string OneWriterDirectorDescription = "Off = Get all the names for writers and directors" + "\n" + "On = Only use first name for writers and directors" + "\n" + "" + "\n" + "Some skins do not allow much room for writers and directors. So if you want to prevent horizontal scrolling, then enable this setting to only import the first name for writers and directors." + "\n" + "" + "\n" + "Keep in mind that this will affect your ability to search for movies done by a certain writer or director as only one name will be available per movie. On the other hand the amount of movies that feature multiple writers and/or directors are very limited.";
        public static string OriginalTitle = "Use the original title from the movie";
        public static string OriginalTitleDescription = "Off = Always force English title" + "\n" + "On = Use original title for foreign movies" + "\n" + "" + "\n" + "Foreign movies often have a non-English title." + "\n" + "" + "\n" + "If you want all your movies to use an English title, then keep this setting disabled.";

        // P        

        // Q

        // R
        public static string Ratings = "English Rating/Certification System";
        public static string RatingsDescription = "There are multiple English based rating/certification systems throughout the world. The most famous one is the American based MPAA rating system with results such as PG-13, R, NC-17, etc." + "\n" + "" + "\n" + "Users in Australia, Canada, United Kingdom, and New Zealand can use this setting to switch to their localized rating system.";
        public static string RatingsHeader = "Select Rating System";
        public static string Rating01 = "USA";
        public static string Rating02 = "UK";
        public static string Rating03 = "Canada (Alberta)";
        public static string Rating04 = "Canada (British Columbia)";
        public static string Rating05 = "Canada (Manitoba)";
        public static string Rating06 = "Canada (Nova Scotia)";
        public static string Rating07 = "Canada (Ontario)";
        public static string Rating08 = "Canada (Quebec)";
        public static string Rating09 = "Australia";
        public static string Rating10 = "New Zealand";
        public static string RefreshMovies = "Refresh Movies";
        public static string RefreshingMovies = "Refreshing Movies";
        public static string RefreshCancel = "Cancel Refresh";
        public static string RefreshMoviesDescription = "Are you sure you want to refresh all movies" + "\n" + "that have IMDb+ as primary source?";
        public static string RefreshMoviesNotification = "IMDb+ movie refresh is done." + "\n" + "Please enjoy the results.";
        public static string RefreshMoviesCancelNotification = "Refresh has been cancelled." + "\n" + "{0} movies done so far." + "\n" + "Refresh again to resume.";
        public static string RefreshMovieStatus = "Refreshing {0}/{1} Movies: {2}%";
        public static string RefreshAllFields = "Refresh all of the fields";
        public static string RefreshAllFieldsDescription = "Off = Only update a few things" + "\n" + "On = Update everything" + "\n" + "" + "\n" + "The information at IMDb is constantly updated and completed. For this reason it makes sense to once in a while update movies in your collection." + "\n" + "" + "\n" + "Enabling this setting will force everything to be updated. Otherwise only empty, certifications, score and votes fields are updated on a refresh." + "\n" + "" + "\n" + "Note: This setting disables the rename system for existing movies.";
        public static string RenameTitles = "Rename titles so that series are grouped together";
        public static string RenameTitlesDescription = "Off = Use title as-is" + "\n" + "On = Rename title so that series will group together" + "\n" + "" + "\n" + "There are many movie series, and sometimes the titles do not start with the same prefix to group them together. To solve this, a rename database file is used that will rename a title based on the IMDb tt-ID number. The result is that movies are renamed so that they are easy to identify as being part of a series." + "\n" + "" + "\n" + "Example: 'Batman VII: The Dark Knight Rises' will group nicely along with the other movies in the series." + "\n" + "" + "\n" + "The default rename titles are placed in a file 'IMDb+\\Rename dBase IMDb+ Scraper.xml' which resides in your MediaPortal User folder." + "\n" + "" + "\n" + "Any custom rename entries that you want should go into 'IMDb+\\Rename dBase IMDb+ Scraper (Custom).xml'" + "\n" + "" + "\n" + "You can use notepad to edit the custom one, as the default one will be automatically updated at times, please make sure that the contents of the XML files remain compliant.";
        public static string RemoveFirstRoman = "Remove Roman numeral from first movie";
        public static string RemoveFirstRomanDescription = "Off = Keep renamed title as-is" + "\n" + "On = Strip Roman numeral from first movie" + "\n" + "" + "\n" + "If the rename system is enabled, then by default, series will be grouped by adding Roman numerals to the title. This is also done to the first movie in the series, so 'Avatar' becomes 'Avatar I'. Enabling this setting will still sort the first movie in the series correct, but it will remove the Roman numeral from the title." + "\n" + "" + "\n" + "Keep in mind that this is only done when the first movie in the series does not have a subtitle. The movie 'Indiana Jones I: Raiders of the Lost Ark' will therefore keep its Roman numeral.";
        public static string RottenMeter = "RottenTomatoes TomatoMeter";
        public static string RottenMeterDescription = "Off = RottenTomatoes 'Audience' ratings" + "\n" + "On = RottenTomatoes 'TomatoMeter' ratings" + "\n" + "" + "\n" + "There are many different ratings available at the RottenTomatoes website." + "\n" + "" + "\n" + "To use any of the TomatoMeter ratings you have to enable this setting, otherwise the 'Audience' one is used.";
        public static string RottenAverage = "RottenTomatoes average rating";
        public static string RottenAverageDescription = "Off = RottenTomatoes 'Percentage' rating" + "\n" + "On = RottenTomatoes 'Average' rating" + "\n" + "" + "\n" + "With this setting enabled the 'Average' rating value is used, otherwise the 'Percentage' value is." + "\n" + "" + "\n" + "The 'Percentage' rating is a special kind, in that it counts the amount of RottenTomatoes users who voted a certain way.";
        public static string RottenTopCritics = "RottenTomatoes top critics";
        public static string RottenTopCriticsDescription = "Off = RottenTomatoes 'All' critics" + "\n" + "On = RottenTomatoes 'Top' critics" + "\n" + "" + "\n" + "There is a TomatoMeter rating available for all of the critics and a select group of 'Top' critics." + "\n" + "" + "\n" + "Enabling this setting will limit your rating to just the top ones.";
        public static string Resume = "(Resume)";

        // S
        public static string ScraperOptionsTitle = "IMDb+ Scraper Options";
        public static string SecondaryDetails = "Obtain additional information in the following language";
        public static string SecondaryDetailsDescription = "There is support to obtain certain information in a different language. This covers information such as Summary, Certification, Genres and wherever possible even more." + "\n" + "" + "\n" + "Current support includes: Dutch, French, German, Italian, Icelandic, Portugese, Spanish and Swedish.";
        public static string SecondaryDetailsHeader = "Secondary Language";
        public static string SecondaryEnglishTitle = "Secondary English title";
        public static string SecondaryEnglishTitleDescription = "Off = Use foreign title" + "\n" + "On = Force English title" + "\n" + "" + "\n" + "If a foreign language has been selected for certain details, the title will become foreign as well. This option can be used to keep the title in English." + "\n" + "" + "\n" + "Note: This does not apply to the rename system. A translated entry is still required then to get a foreign title, even when this setting is disabled.";
        public static string SecondaryLanguage01 = "English";
        public static string SecondaryLanguage02 = "German";
        public static string SecondaryLanguage03 = "Spanish";
        public static string SecondaryLanguage04 = "French";
        public static string SecondaryLanguage05 = "Italian";
        public static string SecondaryLanguage06 = "Portuguese";
        public static string SecondaryLanguage07 = "Swedish";
        public static string SecondaryLanguage08 = "Icelandic";
        public static string SecondaryLanguage09 = "Dutch";
        public static string SecondarySummary = "Fallback to English summary if foreign one is missing";
        public static string SecondarySummaryDescription = "Off = Show nothing if foreign summary is missing" + "\n" + "On = Use English summary as a fallback when no foreign one exists" + "\n" + "" + "\n" + "Foreign movie websites are not always as extensive as imdb.com or have the same information available. This mainly affects the summary, so to prevent your collection from being empty, enable this option to then fallback on the English summary (if available). Ideally you would register at the foreign movie website and submit the missing information, but until then you can use English.";
        public static string SingleScore = "Single score rating system";
        public static string SingleScoreDescription = "Off = Use average rating system" + "\n" + "On = Single rating value is used" + "\n" + "" + "\n" + "There is a big difference sometimes between all the different ratings that are available. To compensate for that, an average value is used based on the ratings from imdb, metacritics and rottentomatoes." + "\n" + "" + "\n" + "Note: If the 'IMDb Score' is enabled then the average rating of only IMDb and Metacritics is used when this setting is active for an average rating result.";
        public static string SpecialEditions = "Special editions rename tagging support";
        public static string SpecialEditionsDescription = "Off = Keep title as-is" + "\n" + "On = Add special editions tag to the title" + "\n" + "" + "\n" + "If you make the IMDb tt-ID number available inside filename or via NFO files, then you can enable this setting for special edition tagging." + "\n" + "" + "\n" + "You can add the following terms to your filename inside parenthesis:" + "\n" + "" + "\n" + " - 3D" + "\n" + " - Extended" + "\n" + " - Unrated" + "\n" + " - Director's Cut" + "\n" + " - Alternate Ending" + "\n" + "" + "\n" + "You can also have the term 'Edition' postfixed to the end of them if you wish." + "\n" + "" + "\n" + "Example: \"Avatar (tt0499549) (3D).mkv\" will result in \"Avatar (3D)\"";
        public static string SettingPluginEnabledName = "Plugin Enabled";
        public static string SettingPluginEnabledDescription = "Enable / Disable this setting to control if IMDb+ plugin is loaded with MediaPortal.";
        public static string SettingListedHomeName = "Listed in Home";
        public static string SettingListedHomeDescription = "Enable this setting for IMDb+ plugin to appear in the main Home screen menu items.";
        public static string SettingListedPluginsName = "Listed in My Plugins";
        public static string SettingListedPluginsDescription = "Enable this setting for IMDb+ plugin to appear in the My Plugins screen menu items.";
        public static string SettingsSyncIntervalName = "Number of hours until next update check";
        public static string SettingsSyncIntervalDescription = "This setting controls how often the plugin will check online for an updated scraper script and replacements database.";
        public static string SettingsSyncOnStartupName = "Check for updates on startup";
        public static string SettingsSyncOnStartupDescription = "This setting will force the plugin to check online for an updated scraper script and replacements database every time MediaPortal starts.";
        public static string SettingsDisableNotificationsName = "Disable notifications";
        public static string SettingsDisableNotificationsDescription = "This setting will allow you to disable notifications that appear such as when any scraper updates have been applied to your system.";
        public static string SelectSources = "Select Sources to Convert to IMDb+";
        public static string StartsWith = "Starts with {0}";
        public static string SelectAlphas = "Select Movies Starting With...";
        public static string ScraperNotFirstPrompt = "The IMDb+ scraper is not first!\nThis will cause problems.\nWould you like to correct this?";

        // T
        public static string IMDbInfo = "IMDb+ Info";


        // U
        public static string UkRating = "British-English Movie Titles";
        public static string UkRatingDescription = "Off = Use American-English Movie Titles" + "\n" + "On = Use British-English Movie Titles" + "\n" + "" + "\n" + "Use this setting for British-English movie titles." + "\n" + "" + "\n" + "Famous example is the first Harry Potter movie. The original title is \"Harry Potter and the Philosopher's Stone\", which was Americanized into \"Harry Potter and the Sorcerer's Stone\". Since the IMDb website is focused towards an American audience that means all titles will use the American-English title if available. With this setting you can change that to the British-English title." + "\n" + "" + "\n" + "Note: There is currently no rename/replacement database support for British-English titles yet, so you will still end up with \"Harry Potter I: Sorcerer's Stone\", but you can overrule that via the custom rename/replacement database for the time being until proper support is added to the IMDb+ plugin.";
        public static string UpdatedScraperScript = "MovingPictures has been updated with IMDb+ Scraper script v{0}";
        public static string Update = "Update";
        public static string UpdateAll = "Update All";
        public static string UpdateReplacementOnly = "Update Replacements Only";
        public static string UpdateAlphas = "Update Movies Starting With...";

        // V

        // W

        // X

        // Y
        public static string Yes = "Yes";

        // Z

        #endregion
    }
}