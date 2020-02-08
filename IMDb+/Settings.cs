using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Runtime.Serialization.Json;
using MediaPortal.Profile;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;

namespace IMDb
{
    public class PluginSettings
    {
        public static string OptionsFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Options IMDb+ Scraper.xml");

        #region Constants
        public const string cSection = "IMDbPlus";
        public const string cGuid = "9d064213-0b4d-4cee-96a5-405812422b58";

        private const string cOriginalTitle = "global_options_original_title";
        private const string cForeignTitle = "global_options_foreign_title";
        private const string cForeignFirst = "global_options_foreign_first";
        private const string cSpecialEdition = "global_options_special_edition";
        private const string cRenameTitles = "global_options_rename_titles";
        private const string cSingleScore = "global_options_single_score";
        private const string cIMDbScore = "global_options_imdb_score";
        private const string cIMDbMetaScore = "global_options_imdb_metascore";
        private const string cRottenMeter = "global_options_rotten_meter";
        private const string cRottenAverage = "global_options_rotten_average";
        private const string cRottenTopCritics = "global_options_rotten_top_critics";
        private const string cMinIMDbVotes = "global_options_min_imdb_votes";
        private const string cLongSummary = "global_options_long_summary";
        private const string cUkRating = "global_options_uk_rating";
        private const string cRemoveFirstRoman = "global_options_remove_first_roman";
        private const string cOneWriterDirector = "global_options_one_writer_director";
        private const string cFixMissingSummary = "global_options_fix_missing_summary";
        private const string cRatings = "global_options_english_movie_rating";
        private const string cSecondaryEnglishTitle = "global_options_secondary_en_title";
        private const string cSecondaryDetails = "global_options_secondary_details";
        private const string cSecondarySummary = "global_options_secondary_summary";
        private const string cRefreshAllFields = "global_options_refresh_all_fields";
        private const string cCountryFilter = "global_options_country_filter";
        private const string cLanguageFilter = "global_options_language_filter";

        private const string cSyncInterval = "plugin_options_sync_interval";
        private const string cSyncOnStartup = "plugin_options_sync_on_startup";
        private const string cSyncLastDateTime = "plugin_options_sync_last_datetime";
        private const string cDisableNotifications = "plugin_options_disable_notifications";
        private const string cMoviesRefreshed = "plugin_options_movies_refreshed";

        #endregion

        #region Settings

        public static bool OriginalTitle { get; set; }
        public static bool ForeignTitle { get; set; }
        public static bool ForeignFirst { get; set; }
        public static bool SpecialEdition { get; set; }
        public static bool RenameTitles { get; set; }
        public static bool SingleScore { get; set; }
        public static bool IMDbScore { get; set; }
        public static bool IMDbMetaScore { get; set; }
        public static bool RottenMeter { get; set; }
        public static bool RottenAverage { get; set; }
        public static bool RottenTopCritics { get; set; }
        public static bool MinIMDbVotes { get; set; }
        public static bool LongSummary { get; set; }
        public static bool UkRating { get; set; }
        public static bool OneWriterDirector { get; set; }
        public static bool FixMissingSummary { get; set; }
        public static bool RemoveFirstRoman { get; set; }
        public static string Ratings { get; set; }
        public static bool SecondaryEnglishTitle { get; set; }
        public static string SecondaryDetails { get; set; }
        public static bool SecondarySummary { get; set; }
        public static bool RefreshAllFields { get; set; }
        public static string CountryFilter { get; set; }
        public static string LanguageFilter { get; set; }
        
        public static int SyncInterval { get; set; }
        public static bool SyncOnStartup { get; set; }
        public static string SyncLastDateTime { get; set; }
        public static bool DisableNotifications { get; set; }
        public static List<string> MoviesRefreshed { get; set; }

        public static bool ShowLastActiveModuleOnRestart { get; set; }
        public static int LastActiveModule { get; set; }

        /// <summary>
        /// We can't show dialog's OnPageLoad when MediaPortal resume's from restart/standby
        /// </summary>
        public static bool SkipWarningDlg
        {
            get
            {
                if (ShowLastActiveModuleOnRestart && LastActiveModule == IMDb.PluginID)
                {
                    ShowLastActiveModuleOnRestart = false;
                    return true;
                }

                return false;
            }
        }

        public static string Version
        {
            get
            {
                return Assembly.GetCallingAssembly().GetName().Version.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Loads the Settings
        /// </summary>
        public static void LoadSettings()
        {
            Logger.Debug("Loading IMDb+ options from file.");

            #region Scraper
            XmlReader xmlReader = new XmlReader();
            if (!xmlReader.Load(OptionsFile))
            {
                Logger.Error("Error opening IMDb+ Options file, will restore defaults.");                
            }
            
            OriginalTitle = xmlReader.GetOptionValueAsBool(cOriginalTitle, false);
            ForeignTitle = xmlReader.GetOptionValueAsBool(cForeignTitle, false);
            ForeignFirst = xmlReader.GetOptionValueAsBool(cForeignFirst, false);
            SpecialEdition = xmlReader.GetOptionValueAsBool(cSpecialEdition, true);
            RenameTitles = xmlReader.GetOptionValueAsBool(cRenameTitles, true);
            SingleScore = xmlReader.GetOptionValueAsBool(cSingleScore, false);
            IMDbScore = xmlReader.GetOptionValueAsBool(cIMDbScore, false);
            IMDbMetaScore = xmlReader.GetOptionValueAsBool(cIMDbMetaScore, false);
            RottenMeter = xmlReader.GetOptionValueAsBool(cRottenMeter, false);
            RottenAverage = xmlReader.GetOptionValueAsBool(cRottenAverage, false);
            RottenTopCritics = xmlReader.GetOptionValueAsBool(cRottenTopCritics, false);
            MinIMDbVotes = xmlReader.GetOptionValueAsBool(cMinIMDbVotes, false);
            LongSummary = xmlReader.GetOptionValueAsBool(cLongSummary, false);
            UkRating = xmlReader.GetOptionValueAsBool(cUkRating, false);
            OneWriterDirector = xmlReader.GetOptionValueAsBool(cOneWriterDirector, false);
            FixMissingSummary = xmlReader.GetOptionValueAsBool(cFixMissingSummary, false);
            RemoveFirstRoman = xmlReader.GetOptionValueAsBool(cRemoveFirstRoman, false);
            Ratings = xmlReader.GetOptionValueAsString(cRatings, "01");
            SecondaryEnglishTitle = xmlReader.GetOptionValueAsBool(cSecondaryEnglishTitle, false);
            SecondaryDetails = xmlReader.GetOptionValueAsString(cSecondaryDetails, "01");
            SecondarySummary = xmlReader.GetOptionValueAsBool(cSecondarySummary, false);
            RefreshAllFields = xmlReader.GetOptionValueAsBool(cRefreshAllFields, true);
            CountryFilter = xmlReader.GetOptionValueAsString(cCountryFilter, "us|ca|gb|ie|au|nz");
            LanguageFilter = xmlReader.GetOptionValueAsString(cLanguageFilter, "en");
            #endregion

            #region Plugin
            using (Settings xmlreader = new MPSettings())
            {
                SyncInterval = xmlreader.GetValueAsInt(cSection, cSyncInterval, 24);
                SyncOnStartup = xmlreader.GetValueAsBool(cSection, cSyncOnStartup, false);
                SyncLastDateTime = xmlreader.GetValueAsString(cSection, cSyncLastDateTime, DateTime.MinValue.ToString());
                DisableNotifications = xmlreader.GetValueAsBool(cSection, cDisableNotifications, false);
                MoviesRefreshed = xmlreader.GetValueAsString(cSection, cMoviesRefreshed, "[]").FromJSONArray<string>().ToList();
            }
            #endregion

            #region MediaPortal
            // Check if MediaPortal will Show TVSeries Plugin when restarting
            // We need to do this because we may need to show a modal dialog e.g. PinCode and we can't do this if MediaPortal window is not yet ready            
            using (Settings xmlreader = new Settings(Config.GetFile(Config.Dir.Config, "MediaPortal.xml")))
            {
                ShowLastActiveModuleOnRestart = xmlreader.GetValueAsBool("general", "showlastactivemodule", false);
                LastActiveModule = xmlreader.GetValueAsInt("general", "lastactivemodule", -1);
            }
            #endregion

            // save settings, might be some new settings added
            SaveSettings();
        }

        /// <summary>
        /// Saves the Settings
        /// </summary>
        public static void SaveSettings()
        {
            Logger.Debug("Saving IMDb+ options to file.");

            #region Scraper
            XmlWriter xmlWriter = new XmlWriter();
            if (!xmlWriter.Load(OptionsFile))
            {
                if (File.Exists(OptionsFile))
                {
                    try
                    {
                        File.Delete(OptionsFile);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error deleting file: '{0}'", OptionsFile);
                        Logger.Error("Exception: {0}", e.Message);
                        return;
                    }
                }

                // create it
                Logger.Info("Creating new IMDb+ options file.");
                xmlWriter.CreateXmlConfigFile(OptionsFile);
                xmlWriter.Load(OptionsFile);
            }

            xmlWriter.SetOptionsEntry(cOriginalTitle, "01", OriginalTitle.ToString());
            xmlWriter.SetOptionsEntry(cForeignTitle, "02", ForeignTitle.ToString());
            xmlWriter.SetOptionsEntry(cForeignFirst, "03", ForeignFirst.ToString());
            xmlWriter.SetOptionsEntry(cUkRating, "04", UkRating.ToString());
            xmlWriter.SetOptionsEntry(cIMDbScore, "05", IMDbScore.ToString());
            xmlWriter.SetOptionsEntry(cIMDbMetaScore, "06", IMDbMetaScore.ToString());
            xmlWriter.SetOptionsEntry(cLongSummary, "07", LongSummary.ToString());
            xmlWriter.SetOptionsEntry(cRottenMeter, "08", RottenMeter.ToString());
            xmlWriter.SetOptionsEntry(cRottenAverage, "09", RottenAverage.ToString());
            xmlWriter.SetOptionsEntry(cRottenTopCritics, "10", RottenTopCritics.ToString());
            xmlWriter.SetOptionsEntry(cSpecialEdition, "11", SpecialEdition.ToString());
            xmlWriter.SetOptionsEntry(cRenameTitles, "12", RenameTitles.ToString());
            xmlWriter.SetOptionsEntry(cSingleScore, "13", SingleScore.ToString());
            xmlWriter.SetOptionsEntry(cMinIMDbVotes, "14", MinIMDbVotes.ToString());
            xmlWriter.SetOptionsEntry(cRefreshAllFields, "15", RefreshAllFields.ToString());
            xmlWriter.SetOptionsEntry(cOneWriterDirector, "16", OneWriterDirector.ToString());
            xmlWriter.SetOptionsEntry(cRemoveFirstRoman, "17", RemoveFirstRoman.ToString());
            xmlWriter.SetOptionsEntry(cFixMissingSummary, "18", FixMissingSummary.ToString());

            xmlWriter.SetOptionsEntry(cRatings, "94", Ratings);
            xmlWriter.SetOptionsEntry(cSecondaryEnglishTitle, "95", SecondaryEnglishTitle.ToString());
            xmlWriter.SetOptionsEntry(cSecondarySummary, "96", SecondarySummary.ToString());
            xmlWriter.SetOptionsEntry(cSecondaryDetails, "97", SecondaryDetails);
            xmlWriter.SetOptionsEntry(cCountryFilter, "98", CountryFilter);
            xmlWriter.SetOptionsEntry(cLanguageFilter, "99", LanguageFilter);

            // save file
            xmlWriter.Save(OptionsFile);
            #endregion

            #region Plugin
            using (Settings xmlwriter = new MPSettings())
            {
                xmlwriter.SetValue(cSection, cSyncInterval, SyncInterval.ToString());
                xmlwriter.SetValueAsBool(cSection, cSyncOnStartup, SyncOnStartup);
                xmlwriter.SetValue(cSection, cSyncLastDateTime, SyncLastDateTime.ToString());
                xmlwriter.SetValueAsBool(cSection, cDisableNotifications, DisableNotifications);
                xmlwriter.SetValue(cSection, cMoviesRefreshed, MoviesRefreshed.ToJSON());
            }
            Settings.SaveCache();
            #endregion
        }
    }

    public static class ExtensionMethods
    {
        #region Extensions Methods

        public static string ToJSON(this object obj)
        {
            if (obj == null) return string.Empty;
            using (var ms = new MemoryStream())
            {
                var ser = new DataContractJsonSerializer(obj.GetType());
                ser.WriteObject(ms, obj);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        public static IEnumerable<T> FromJSONArray<T>(this string jsonArray)
        {
            if (string.IsNullOrEmpty(jsonArray)) return new List<T>();

            try
            {
                using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonArray)))
                {
                    var ser = new DataContractJsonSerializer(typeof(IEnumerable<T>));
                    var result = (IEnumerable<T>)ser.ReadObject(ms);

                    if (result == null)
                    {
                        return new List<T>();
                    }
                    else
                    {
                        return result;
                    }
                }
            }
            catch (Exception)
            {
                return new List<T>();
            }
        }

        #endregion
    }

    public class ExtensionSettings
    {
        public void Init()
        {
            Thread hookThread = new Thread(delegate()
            {
                try
                {
                    Logger.Info("Adding hooks to MPEI Settings");
                    AddHooksIntoMPEISettings();
                }
                catch
                {
                    Logger.Warning("Unable to add hooks into MPEI Settings, Extensions plugin not installed or out of date!");
                }
            })
            {
                Name = "Extension Settings",
                IsBackground = true
            };

            hookThread.Start();
        }

        private void AddHooksIntoMPEISettings()
        {
            // sleep until we know that there has been enough time
            // for window manager to have loaded extension settings window
            // todo: find a better way...
            Thread.Sleep(10000);

            // get a reference to the extension settings window
            MPEIPlugin.GUISettings extensionSettings = (MPEIPlugin.GUISettings)GUIWindowManager.GetWindow(803);
            extensionSettings.OnSettingsChanged += new MPEIPlugin.GUISettings.SettingsChangedHandler(Extensions_OnSettingsChanged);
        }

        private void Extensions_OnSettingsChanged(string guid)
        {
            // settings change occured
            if (guid == PluginSettings.cGuid)
            {
                Logger.Info("Settings updated externally");

                // re-load settings
                PluginSettings.LoadSettings();
                
                // Update Timer settings
                IMDb.UpdateTimer();
            }
        }
    }
}
