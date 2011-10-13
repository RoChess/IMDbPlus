using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using MediaPortal;
using MediaPortal.Util;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.Database;
using MediaPortal.Plugins.MovingPictures.DataProviders;
using Action = MediaPortal.GUI.Library.Action;

namespace IMDb
{
    public class IMDb : GUIWindow, ISetupForm
    {
        #region Private Variables

        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl forceIMDbPlusButton = null;
        
        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        int PluginID = 31415;
        DBSourceInfo IMDbPlusSource;
        DBSourceInfo IMDbSource;
        Timer syncLibraryTimer;
        string ReplacementsFile = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Rename dBase IMDb+ Scraper.xml");

        #endregion

        #region Constants

        const string ScraperUpdateFile = @"http://imdbplus.googlecode.com/svn/trunk/Scraper/IMDb+.Scraper.SVN.xml";
        const string ReplacementsUpdateFile = @"http://imdbplus.googlecode.com/svn/trunk/Rename%20dBase%20IMDb+%20Scraper.xml";
        const int IMDbPlusScriptId = 314159265;
        const int IMDbScriptId = 874902;
        #endregion

        #region ISetupFrom

        /// <summary>
        /// Returns the Author of the Plugin to Mediaportal
        /// </summary>
        /// <returns>The Author of the Plugin</returns>
        public string Author()
        {
            return "RoChess";
        }

        /// <summary>
        /// Boolean that decides whether the plugin can be enabled or not
        /// </summary>
        /// <returns>The boolean answer</returns>
        public bool CanEnable()
        {
            return true;
        }

        /// <summary>
        /// Decides if the plugin is enabled by default
        /// </summary>
        /// <returns>The boolean answer</returns>
        public bool DefaultEnabled()
        {
            return true;
        }

        /// <summary>
        /// Description of the plugin
        /// </summary>
        /// <returns>The Description</returns>
        public string Description()
        {
            return "IMDb+ Scraper for MovingPictures";
        }

        /// <summary>
        /// Returns the items for the plugin
        /// </summary>
        /// <param name="strButtonText">The Buttons Text</param>
        /// <param name="strButtonImage">The Buttons Image</param>
        /// <param name="strButtonImageFocus">The Buttons Focused Image</param>
        /// <param name="strPictureImage">The Picture Image</param>
        /// <returns></returns>
        public bool GetHome(out string strButtonText, out string strButtonImage, out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = "IMDb+";
            strButtonImage = string.Empty;
            strButtonImageFocus = string.Empty;
            strPictureImage = "hover_imdb+.png";
            return true;
        }

        /// <summary>
        /// Gets the Window id accociated with the plugin
        /// </summary>
        /// <returns>The window id</returns>
        public int GetWindowId()
        {
            return PluginID;
        }

        /// <summary>
        /// Boolean asking if the plugin has a setup setting
        /// </summary>
        /// <returns>The Boolean answer</returns>
        public bool HasSetup()
        {
            return false;
        }

        /// <summary>
        /// The Name of the Plugin
        /// </summary>
        /// <returns>The Name of the Plugin</returns>
        public string PluginName()
        {
            return GUIUtils.PluginName();
        }

        /// <summary>
        /// Shows the Plugins configuration window
        /// </summary>
        public void ShowPlugin()
        {
            return;
        }

        #endregion

        #region GUIWindow Overrides

        public override int GetID
        {
            get
            {
                return PluginID;
            }
        }

        /// <summary>
        /// Starting Point
        /// </summary>
        public override bool Init()
        {
            Logger.Info("Starting IMDb+ v{0}", PluginSettings.Version);           
            
            // Initialize translations
            Translation.Init();

            // Load Settings
            PluginSettings.LoadSettings();

            // Get IMDb+ Data Provider
            IMDbPlusSource = DBSourceInfo.GetFromScriptID(IMDbPlusScriptId);
            SetIMDbProperties();

            // Get IMDB Data Provider
            IMDbSource = DBSourceInfo.GetFromScriptID(IMDbScriptId);

            // Update Script Paths
            UpdateScriptPaths();

            // start update timer, passing along configured parameters
            // add small 3sec delay if syncing on startup.
            int syncInterval = PluginSettings.SyncInterval * 60 * 60 * 1000;
            int startTime = GetSyncStartTime();
            syncLibraryTimer = new Timer(new TimerCallback((o) => { CheckForUpdate(); }), null, startTime, syncInterval);
            
            // Load main skin window
            // this is a launching pad to all other windows
            string xmlSkin = GUIGraphicsContext.Skin + @"\IMDb+.xml";
            Logger.Info("Plugin initialization complete.");
            return Load(xmlSkin);
        }

        /// <summary>
        /// End Point (Clean up)
        /// </summary>
        public override void DeInit()
        {
            PluginSettings.SaveSettings();

            Logger.Info("Goodbye");
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            HideShowForceIMDbPlusButton();
            GUIControl.ClearControl(GetID, Facade.GetID);
            
            int itemId = 0;
            string listIndentation = "   ";
            UpdateListItem(itemId++, Translation.OriginalTitle, PluginSettings.OriginalTitle ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.AddForeignTitle, PluginSettings.ForeignTitle ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, listIndentation + Translation.ForeignTitleFirst, PluginSettings.ForeignFirst ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, Translation.SpecialEditions, PluginSettings.SpecialEdition ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.RenameTitles, PluginSettings.RenameTitles ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.SingleScore, PluginSettings.SingleScore ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, listIndentation + Translation.IMDbScore, PluginSettings.IMDbScore ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.IMDbMetaScore, PluginSettings.IMDbMetaScore ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenMeter, PluginSettings.RottenMeter ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenAverage, PluginSettings.RottenAverage ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenTopCritics, PluginSettings.RottenTopCritics ? Translation.BoolOn : Translation.BoolOff, string.Empty);

            UpdateListItem(itemId++, Translation.MinIMDbVotes, PluginSettings.MinIMDbVotes ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.LongSummary, PluginSettings.LongSummary ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.UkRating, PluginSettings.UkRating ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.OneWriterDirector, PluginSettings.OneWriterDirector ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.SecondaryDetails, GetCountryString(Convert.ToInt32(PluginSettings.SecondaryDetails)), "folder");
            UpdateListItem(itemId++, listIndentation + Translation.SecondarySummary, PluginSettings.SecondarySummary ? Translation.BoolOn : Translation.BoolOff, string.Empty);

            UpdateListItem(itemId++, Translation.RefreshAllFields, PluginSettings.RefreshAllFields ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.CountryFilter, PluginSettings.CountryFilter, "folder");
            UpdateListItem(itemId++, Translation.LanguageFilter, PluginSettings.LanguageFilter, "folder");            

            // Set Facade Layout
            GUIControl.FocusControl(GetID, Facade.GetID);

            // Set Current Selected Item
            Facade.SelectedListItemIndex = 0;

            // Update standard Facade properties
            GUIUtils.SetProperty("#itemcount", Facade.Count.ToString());

            base.OnPageLoad();
        }

        protected override void OnPageDestroy(int new_windowId)
        {
            // read settings
            foreach (GUIListItem item in Facade.ListLayout.ListItems)
            {
                if (item.Label.Trim() == Translation.OriginalTitle)
                    PluginSettings.OriginalTitle = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.AddForeignTitle)
                    PluginSettings.ForeignTitle = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.ForeignTitleFirst)
                    PluginSettings.ForeignFirst = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.UkRating)
                    PluginSettings.UkRating = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.IMDbScore)
                    PluginSettings.IMDbScore = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.IMDbMetaScore)
                    PluginSettings.IMDbMetaScore = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.LongSummary)
                    PluginSettings.LongSummary = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.RottenMeter)
                    PluginSettings.RottenMeter = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.RottenAverage)
                    PluginSettings.RottenAverage = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.RottenTopCritics)
                    PluginSettings.RottenTopCritics = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.SpecialEditions)
                    PluginSettings.SpecialEdition = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.RenameTitles)
                    PluginSettings.RenameTitles = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.SingleScore)
                    PluginSettings.SingleScore = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.MinIMDbVotes)
                    PluginSettings.MinIMDbVotes = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.RefreshAllFields)
                    PluginSettings.RefreshAllFields = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.OneWriterDirector)
                    PluginSettings.OneWriterDirector = (item.Label2 == Translation.BoolOn);

                if (item.Label.Trim() == Translation.SecondarySummary)
                    PluginSettings.SecondarySummary = (item.Label2 == Translation.BoolOn);
                if (item.Label.Trim() == Translation.SecondaryDetails)
                    PluginSettings.SecondaryDetails = GetCountryIntAsString(item.Label2);
                if (item.Label.Trim() == Translation.CountryFilter)
                    PluginSettings.CountryFilter = item.Label2;
                if (item.Label.Trim() == Translation.LanguageFilter)
                    PluginSettings.LanguageFilter = item.Label2;
            }

            // save settings
            PluginSettings.SaveSettings();

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);

            switch (controlId)
            {
                // Force IMDb+
                case (2):
                    ForceIMDbSourceInfo();
                    break;

                // Facade
                case (50):
                    if (actionType == Action.ActionType.ACTION_SELECT_ITEM)
                    {
                        GUIListItem selectedItem = this.Facade.SelectedListItem;
                        if (selectedItem == null) return;

                        //Toggle the Boolean options that got clicked on
                        selectedItem.Label2 = (selectedItem.Label2 == Translation.BoolOn) ? Translation.BoolOff : (selectedItem.Label2 == Translation.BoolOff) ? Translation.BoolOn : selectedItem.Label2;
                        selectedItem.IsPlayed = (selectedItem.Label2 == Translation.BoolOff) ? true : false;

                        if (selectedItem.Label == Translation.SecondaryDetails)
                        {
                            IDialogbox dlg = (IDialogbox)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_MENU);
                            if (dlg == null) return;

                            dlg.Reset();
                            dlg.SetHeading(Translation.SecondaryDetailsHeader);

                            // Create menu items
                            string[] languageArray = {
                                                         Translation.SecondaryLanguage02,
                                                         Translation.SecondaryLanguage03,
                                                         Translation.SecondaryLanguage04,
                                                         Translation.SecondaryLanguage05,
                                                         Translation.SecondaryLanguage06,
                                                         Translation.SecondaryLanguage07,
                                                         Translation.SecondaryLanguage08
                                                     };
                            //Add 'English' as first language to the dialog.
                            GUIListItem listItem = new GUIListItem(Translation.SecondaryLanguage01);
                            dlg.Add(listItem);
                            //Sort the remaining languages and add them to the dialog as well
                            Array.Sort(languageArray);
                            for (int i = 0; i < languageArray.Count(); i++)
                            {
                                GUIListItem listArrayItem = new GUIListItem(languageArray[i]);
                                dlg.Add(listArrayItem);
                            }
                            dlg.DoModal(GUIWindowManager.ActiveWindow);
                            if (dlg.SelectedId <= 0) return;

                            selectedItem.Label2 = dlg.SelectedLabelText;
                        }

                        string output;
                        if (selectedItem.Label == Translation.CountryFilter)
                        {
                            output = selectedItem.Label2.Replace('|', '.');
                            if (GUIUtils.GetStringFromKeyboard(ref output))
                            {
                                selectedItem.Label2 = output.Replace('.', '|');
                            }
                        }
                        if (selectedItem.Label == Translation.LanguageFilter)
                        {
                            output = selectedItem.Label2.Replace('|', '.');
                            if (GUIUtils.GetStringFromKeyboard(ref output))
                            {
                                selectedItem.Label2 = output.Replace('.', '|');
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        #endregion

        private void UpdateListItem(int itemId, string itemName, string itemValue, string itemIcon)
        {
            GUIListItem item = new GUIListItem(itemName);
            item.Label2 = itemValue;
            //Adjust color of item when option is turned off
            item.IsPlayed = (itemValue == Translation.BoolOff) ? true : false;
            //Adjust color on all the non-Bool options
            if (itemValue != Translation.BoolOn && itemValue != Translation.BoolOff)
                item.IsRemote = true;
            item.ItemId = Int32.MaxValue - itemId;

            // check-box.png + check-boxNF.png
            // remote_blue.png + remote_yellow.png + remote_green.png + tvguide_record_button.png
            if (itemIcon == "folder")
            {
                item.IconImage = "defaultFolder.png";
                item.IconImageBig = "defaultFolderBig.png";
                item.ThumbnailImage = "defaultFolderBig.png";
            }
            else
            {
                item.IconImage = itemIcon;
                item.IconImageBig = itemIcon;
                item.ThumbnailImage = itemIcon;
            }
            item.OnItemSelected += OnItemSelected;
            Utils.SetDefaultIcons(item);
            Facade.Add(item);
        }

        private string GetCountryString(int countryId)
        {
            switch (countryId)
            {
                case 1: return Translation.SecondaryLanguage01;
                case 2: return Translation.SecondaryLanguage02;
                case 3: return Translation.SecondaryLanguage03;
                case 4: return Translation.SecondaryLanguage04;
                case 5: return Translation.SecondaryLanguage05;
                case 6: return Translation.SecondaryLanguage06;
                case 7: return Translation.SecondaryLanguage07;
                case 8: return Translation.SecondaryLanguage08;
                default: return "ERROR";
            }
        }

        private string GetCountryIntAsString(string countryString)
        {
            if (countryString == Translation.SecondaryLanguage01) return "01";
            if (countryString == Translation.SecondaryLanguage02) return "02";
            if (countryString == Translation.SecondaryLanguage03) return "03";
            if (countryString == Translation.SecondaryLanguage04) return "04";
            if (countryString == Translation.SecondaryLanguage05) return "05";
            if (countryString == Translation.SecondaryLanguage06) return "06";
            if (countryString == Translation.SecondaryLanguage07) return "07";
            if (countryString == Translation.SecondaryLanguage08) return "08";
            return "01";
        }

        private void OnItemSelected(GUIListItem item, GUIControl parent)
        {
            if (item.Label.Trim() == Translation.OriginalTitle)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.OriginalTitleDescription);
            if (item.Label.Trim() == Translation.AddForeignTitle)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.AddForeignTitleDescription);
            if (item.Label.Trim() == Translation.ForeignTitleFirst)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.ForeignTitleFirstDescription);
            if (item.Label.Trim() == Translation.SpecialEditions)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SpecialEditionsDescription);
            if (item.Label.Trim() == Translation.RenameTitles)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RenameTitlesDescription);

            if (item.Label.Trim() == Translation.SingleScore)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SingleScoreDescription);
            if (item.Label.Trim() == Translation.IMDbScore)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.IMDbScoreDescription);
            if (item.Label.Trim() == Translation.IMDbMetaScore)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.IMDbMetaScoreDescription);
            if (item.Label.Trim() == Translation.RottenMeter)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenMeterDescription);
            if (item.Label.Trim() == Translation.RottenAverage)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenAverageDescription);
            if (item.Label.Trim() == Translation.RottenTopCritics)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenTopCriticsDescription);

            if (item.Label.Trim() == Translation.MinIMDbVotes)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.MinIMDbVotesDescription);
            if (item.Label.Trim() == Translation.LongSummary)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.LongSummaryDescription);
            if (item.Label.Trim() == Translation.UkRating)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.UkRatingDescription);

            if (item.Label.Trim() == Translation.OneWriterDirector)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.OneWriterDirectorDescription);
            if (item.Label.Trim() == Translation.SecondaryDetails)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SecondaryDetailsDescription);
            if (item.Label.Trim() == Translation.SecondarySummary)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SecondarySummaryDescription);

            if (item.Label.Trim() == Translation.RefreshAllFields)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RefreshAllFieldsDescription);

            if (item.Label.Trim() == Translation.CountryFilter)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.CountryFilterDescription);
            if (item.Label.Trim() == Translation.LanguageFilter)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.LanguageFilterDescription);
        }        

        private void SetIMDbProperties()
        {
            // not installed
            if (IMDbPlusSource == null)
            {
                GUIUtils.SetProperty("#IMDb.Scraper.IsInstalled", "false");
                return;
            }

            GUIUtils.SetProperty("#IMDb.Scraper.IsInstalled", "true", true);
            GUIUtils.SetProperty("#IMDb.Scraper.Version", IMDbPlusSource.Provider.Version, true);
            GUIUtils.SetProperty("#IMDb.Scraper.Description", IMDbPlusSource.Provider.Description, true);
            GUIUtils.SetProperty("#IMDb.Scraper.Author", IMDbPlusSource.Provider.Author, true);
            GUIUtils.SetProperty("#IMDb.Scraper.Published", IMDbPlusSource.SelectedScript.Provider.Published.Value.ToShortDateString(), true);
            GUIUtils.SetProperty("#IMDb.Scraper.DetailsPriority", IMDbPlusSource.DetailsPriority.ToString(), true);
            GUIUtils.SetProperty("#IMDb.Scraper.CoverPriority", IMDbPlusSource.CoverPriority.ToString(), true);
        }

        private void CheckForUpdate()
        {
            Thread updateThread = new Thread(delegate(object obj)
            {
                Logger.Info("Checking for scraper update");

                string localFile = GetTempFilename();
                if (DownloadFile(ScraperUpdateFile, localFile))
                {
                    // try to install latest version
                    // will return false if already latest version
                    if (ScraperScriptInstallation(localFile))
                    {
                        // set highest priority if not already installed
                        if (IMDbPlusSource == null)
                        {
                            IMDbPlusSource = DBSourceInfo.GetFromScriptID(IMDbPlusScriptId);
                            ScraperScriptPositioning(ref IMDbPlusSource);
                        }
                        UpdateScriptPaths();
                        SetIMDbProperties();
                        HideShowForceIMDbPlusButton();
                        GUIUtils.ShowNotifyDialog(Translation.Update, string.Format(Translation.UpdatedScraperScript, IMDbPlusSource.Provider.Version));
                    }

                    // remove temp download file
                    try { File.Delete(localFile); }
                    catch { }

                    PluginSettings.SyncLastDateTime = DateTime.Now.ToString();
                    GUIUtils.SetProperty("#IMDb.Scraper.LastUpdated", PluginSettings.SyncLastDateTime);
                }

                // Update Replacements Database
                localFile = GetTempFilename();
                if (DownloadFile(ReplacementsUpdateFile, localFile))
                {
                    try
                    { 
                        // replace existing file
                        Logger.Info("Updating Replacements Database");
                        File.Copy(localFile, ReplacementsFile, true);
   
                        // remove temp download file
                        File.Delete(localFile);
                    }
                    catch { }
                }

            })
            {
                IsBackground = true,
                Name = "Check for Updates"
            };

            updateThread.Start();
        }

        private bool DownloadFile(string url, string localFile)
        {
            WebClient webClient = new WebClient();
          
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(localFile));
                if (!File.Exists(localFile))
                {
                    Logger.Debug("Downloading file from: {0}", url);
                    webClient.DownloadFile(url, localFile);
                }
                return true;
            }
            catch (Exception)
            {
                Logger.Error("Download failed from '{0}' to '{1}'", url, localFile);
                try { if (File.Exists(localFile)) File.Delete(localFile); }
                catch { }
                return false;
            }
        }

        private string GetTempFilename()
        {
            string localFile = string.Empty;
            try
            {
                localFile = string.Format(@"{0}imdb_{1}.xml", Path.GetTempPath(), Guid.NewGuid());
            }
            catch(IOException)
            {
                // can happen if more than 65K temp files already
                localFile = string.Format(@"C:\imdb_{0}.xml", Guid.NewGuid());
            }
            return localFile;
        }

        private void ScraperScriptModify(string scraperFile, string regexReplace)
        {
            // Adjust scraper script before installing it, so that scraper name and ID can be modified if needed
            // This allows IMDb+ scraper to be installed as "imdb.com (IMDb+ Edition)" for existing collections
        }

        private bool ScraperScriptInstallation(string xmlFile)
        {
            // Grab the contents of the scraper script file
            StreamReader reader = new StreamReader(xmlFile);
            string script = reader.ReadToEnd();
            reader.Close();

            // Add it to the scraper script manager
            DataProviderManager.AddSourceResult addResult = MovingPicturesCore.DataProviderManager.AddSource(typeof(ScriptableProvider), script, true);

            if (addResult == DataProviderManager.AddSourceResult.FAILED_VERSION)
            {
                Logger.Info("Skipping update, latest version already installed.");
            }
            else if (addResult == DataProviderManager.AddSourceResult.FAILED_DATE)
            {
                Logger.Error("Load Script Failed: This script does not have a unique 'published' date.");
            }
            else if (addResult == DataProviderManager.AddSourceResult.FAILED)
            {
                Logger.Error("Load Script Failed: The script is malformed or not a Moving Pictures script.");
            }
            else if (addResult == DataProviderManager.AddSourceResult.SUCCESS_REPLACED)
            {
                Logger.Warning("Load Script Warning: Scraper debug-mode enabled, so existing script was replaced.");
                return true;
            }
            else if (addResult == DataProviderManager.AddSourceResult.SUCCESS)
            {
                // Scraper script has been added successfully
                Logger.Info("Scraper updated to latest version successfully.");
                return true;
            }
            else
            {
                Logger.Error("Load Script Failed: Unknown error.");
            }
            // Scraper installation failed
            return false;
        }

        private void ScraperScriptPositioning(ref DBSourceInfo source)
        {
            // Re-Position the IMDb+ scraper-script to become primary source

            if (source == null) return;
            Logger.Info("Repositioning {0} script to become the new primary details source.", source.Provider.Name);
            
            // shift all enabled 'movie detail' sources down by one
            foreach (var enabledSource in DBSourceInfo.GetAll().Where(s => s.DetailsPriority > -1))
            {
                enabledSource.DetailsPriority++;
                enabledSource.Commit();
            }

            // now set highest priority to the IMDb+ source
            source.SetPriority(DataType.DETAILS, 0);
            source.Commit();
        }

        private int GetSyncStartTime()
        {
            Logger.Info("Last script update time: {0}", PluginSettings.SyncLastDateTime);
            GUIUtils.SetProperty("#IMDb.Scraper.LastUpdated", PluginSettings.SyncLastDateTime);

            DateTime lastUpdate = DateTime.MinValue;
            DateTime.TryParse(PluginSettings.SyncLastDateTime, out lastUpdate);
            
            bool startNow = PluginSettings.SyncOnStartup || DateTime.Now > lastUpdate.Add(new TimeSpan(PluginSettings.SyncInterval, 0, 0));

            // start in short period (3secs)
            if (startNow) return 3000;
            
            return Convert.ToInt32(lastUpdate.Add(new TimeSpan(PluginSettings.SyncInterval, 0, 0)).Subtract(DateTime.Now).TotalMilliseconds);
        }

        /// <summary>
        /// Updates scraper script with correct installation paths after update
        /// </summary>
        private void UpdateScriptPaths()
        {
            // correct path to rename db and options file
            // to point to install paths
            if (IMDbPlusSource == null) return;

            Logger.Info("Updating paths in scraper script");

            string oldValue = @"C:\Rename dBase IMDb+ Scraper.xml";
            string newValue = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Rename dBase IMDb+ Scraper.xml");
            IMDbPlusSource.SelectedScript.Contents = IMDbPlusSource.SelectedScript.Contents.Replace(oldValue, newValue);

            oldValue = @"C:\Rename dBase IMDb+ Scraper (Custom).xml";
            newValue = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Rename dBase IMDb+ Scraper (Custom).xml");
            IMDbPlusSource.SelectedScript.Contents = IMDbPlusSource.SelectedScript.Contents.Replace(oldValue, newValue);

            oldValue = @"C:\Options IMDb+ Scraper.xml";
            newValue = Path.Combine(Config.GetFolder(Config.Dir.Config), @"IMDb+\Options IMDb+ Scraper.xml");
            IMDbPlusSource.SelectedScript.Contents = IMDbPlusSource.SelectedScript.Contents.Replace(oldValue, newValue);

            IMDbPlusSource.SelectedScript.Commit();
            IMDbPlusSource.Commit();

            Logger.Info("Finished updating paths in scraper script");
        }

        /// <summary>
        /// Updates all source info from IMDb -> IMDb+
        /// </summary>
        private void ForceIMDbSourceInfo()
        {
            if (forceIMDbPlusButton == null || !forceIMDbPlusButton.VisibleFromSkinCondition) return;

            Thread forceThread = new Thread(delegate(object obj)
            {
                // focus back to main facade
                GUIControl.FocusControl(GetID, Facade.GetID);

                if (GUIUtils.ShowYesNoDialog(Translation.ForceIMDbPlus, Translation.ForceIMDbPlusDescription))
                {
                    // disable repeated attempts
                    GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "false", false);
                    GUIWindowManager.Process();

                    int movieCount = 0;

                    Logger.Info("Converting Source Info for the following movies...");
                    foreach (var movie in DBMovieInfo.GetAll().Where(m => m.PrimarySource == IMDbSource))
                    {
                        Logger.Info(movie.ToString());
                        movie.PrimarySource = IMDbPlusSource;
                        movie.Commit();
                        movieCount++;
                    }

                    GUIUtils.ShowOKDialog(Translation.ForceIMDbPlus, string.Format(Translation.ForceIMDbPlusComplete, movieCount, DBMovieInfo.GetAll().Count));
                    HideShowForceIMDbPlusButton();
                }
            })
            {
                IsBackground = true,
                Name = "Force IMDb+"
            };

            forceThread.Start();
        }


        private void HideShowForceIMDbPlusButton()
        {
            // skin may not have implemented button
            if (forceIMDbPlusButton == null) return;

            // if no applicable sources disable
            if (IMDbPlusSource == null || IMDbSource == null)
            {
                Logger.Debug("IMDb+ and/or IMDb source not installed!");
                GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "false", false);
                return;
            }

            // if there is a movie with imdb as primary source, show button
            if (DBMovieInfo.GetAll().Find(m => m.PrimarySource == IMDbSource) != null)
            {
                GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "true", true);
            }
            else
            {
                GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "false", true);
            }
            
            GUIWindowManager.Process();
        }
    }
}