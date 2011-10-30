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
    [PluginIcons("IMDb.Images.imdb_icon.png", "IMDb.Images.imdb_icon_disabled.png")]
    public class IMDb : GUIWindow, ISetupForm
    {
        #region Skin Controls

        [SkinControl(2)]
        protected GUIButtonControl forceIMDbPlusButton = null;

        [SkinControl(3)]
        protected GUIButtonControl refreshMoviesButton = null;

        [SkinControl(4)]
        protected GUIButtonControl infoButton = null;

        [SkinControl(50)]
        protected GUIFacadeControl Facade = null;

        #endregion

        #region Private Variables

        int PluginID = 31415;
        DBSourceInfo IMDbPlusSource;
        static Timer syncUpdateTimer;
        bool moviesRefreshing;
        bool cancelRefreshing;
        ExtensionSettings extensionSettings = new ExtensionSettings();

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

            // Init Extension Settings
            extensionSettings.Init();
            
            // Get IMDb+ Data Provider
            IMDbPlusSource = DBSourceInfo.GetFromScriptID(IMDbPlusScriptId);
            SetIMDbProperties();

            // Get Replacements and set properties
            SetReplacementProperties();

            // Init refresh properties
            SetMovieRefreshProperties(null, -1, -1, true);

            // start update timer, passing along configured parameters
            // add small 3sec delay if syncing on startup.
            int syncInterval = PluginSettings.SyncInterval * 60 * 60 * 1000;
            int startTime = GetSyncStartTime();
            syncUpdateTimer = new Timer(new TimerCallback((o) => { CheckForUpdate(); }), null, startTime, syncInterval);

            // listen to resume/standby events
            Microsoft.Win32.SystemEvents.PowerModeChanged += new Microsoft.Win32.PowerModeChangedEventHandler(SystemEvents_PowerModeChanged);

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
            ShutDownPhase();

            PluginSettings.SaveSettings();

            Logger.Info("Goodbye");
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            // set refresh movie state
            SetButtonLabels();
            
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
            Facade.CurrentLayout = GUIFacadeControl.Layout.List;
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

                // Refresh IMDb+ Movies
                case (3):
                    RefreshIMDbPlusMovies();
                    break;

                case (4):
                    ShowIMDbPlusInformation();
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
                                                         Translation.SecondaryLanguage08,
                                                         Translation.SecondaryLanguage09
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

        #region Private Methods

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
                case 9: return Translation.SecondaryLanguage09;
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
            if (countryString == Translation.SecondaryLanguage09) return "09";
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

        private void SetReplacementProperties()
        {
            var replacements = Replacements.GetAll(false);
            var customReplacements = Replacements.GetAll(true);

            GUIUtils.SetProperty("#IMDb.Replacements.Count", replacements != null ? replacements.Count().ToString() : "0", true);
            GUIUtils.SetProperty("#IMDb.Replacements.Custom.Count", customReplacements != null ? customReplacements.Count().ToString() : "0", true);
            GUIUtils.SetProperty("#IMDb.Replacements.Version", Replacements.Version, true);
            GUIUtils.SetProperty("#IMDb.Replacements.Published", Replacements.Published.ToShortDateString(), true);
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
                        if (!PluginSettings.DisableNotifications)
                        {
                            // give some time for mediaportal to load if updated on startup
                            Thread.Sleep(10000);
                            GUIUtils.ShowNotifyDialog(Translation.Update, string.Format(Translation.UpdatedScraperScript, IMDbPlusSource.Provider.Version));
                        }
                    }

                    // remove temp download file
                    try { File.Delete(localFile); }
                    catch { }

                    PluginSettings.SyncLastDateTime = DateTime.Now.ToString();
                    GUIUtils.SetProperty("#IMDb.Scraper.LastUpdated", PluginSettings.SyncLastDateTime);
                }

                Logger.Info("Checking for replacements update");

                // Update Replacements Database
                localFile = GetTempFilename();
                if (DownloadFile(ReplacementsUpdateFile, localFile))
                {
                    // only update replacements if they differ
                    if (!FilesAreEqual(localFile, Replacements.ReplacementsFile))
                    {
                        try 
                        {
                            // replace existing file
                            File.Copy(localFile, Replacements.ReplacementsFile, true); 
                            Logger.Info("Replacements updated to latest version successfully.");
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Replacements update failed: {0}", e.Message);
                        }

                        Replacements.ClearCache(false);
                        SetReplacementProperties();
                    }
                    else
                    {
                        Logger.Info("Skipping update, latest version already installed.");
                    }

                    // remove temp download file
                    try { File.Delete(localFile); }
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

        static int GetSyncStartTime()
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

                // create menu select dialog with all sources used in movie collection
                // display number of movies that have an imdb id as we are only interested in these
                List<MultiSelectionItem> listItems = GetSourceSelectItems();
                if (listItems.Count == 0)
                {
                    GUIUtils.ShowOKDialog(Translation.ForceIMDbPlus, Translation.NoSourcesFound);
                    GUIControl.EnableControl(GetID, forceIMDbPlusButton.GetID);
                    return;
                }

                List<MultiSelectionItem> selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.SelectSources, listItems);
                if (selectedItems == null || selectedItems.Where(i => i.Selected).Count() == 0) return;

                // disable repeated attempts
                GUIControl.DisableControl(GetID, forceIMDbPlusButton.GetID);

                int movieCount = 0;

                Logger.Info("Converting Source Info for the following movies...");
                foreach (var item in selectedItems.Where(i => i.Selected))
                {
                    foreach (var movie in DBMovieInfo.GetAll().Where(m => m.PrimarySource == item.Tag && IsValidIMDb(m.ImdbID)))
                    {
                        Logger.Info("{0}: {1}", item.ItemTitle, movie.ToString());
                        movie.PrimarySource = IMDbPlusSource;
                        movie.Commit();
                        movieCount++;
                    }
                }

                GUIUtils.ShowOKDialog(Translation.ForceIMDbPlus, string.Format(Translation.ForceIMDbPlusComplete, movieCount, DBMovieInfo.GetAll().Count));
                GUIControl.EnableControl(GetID, forceIMDbPlusButton.GetID);
            })
            {
                IsBackground = true,
                Name = "Force IMDb+"
            };

            forceThread.Start();
        }

        /// <summary>
        /// Return a list of Alphas that movies 'start with' in users collection
        /// </summary>
        /// <returns></returns>
        private List<MultiSelectionItem> GetAlphaItems()
        {
            List<MultiSelectionItem> result = new List<MultiSelectionItem>();

            // get all movies with IMDb+ source
            var movies = DBMovieInfo.GetAll().Where(s => s.PrimarySource == IMDbPlusSource);
            if (movies.Count() == 0) return result;

            // get list of movies starting with numeral for '#' entry
            var numberMovies = movies.Where(m => Char.IsDigit(m.SortBy, 0));
            if (numberMovies.Count() > 0)
            {
                MultiSelectionItem multiSelectionItem = new MultiSelectionItem
                {
                    ItemTitle = string.Format(Translation.StartsWith, "#"),
                    ItemTitle2 = string.Format(Translation.NumberOfMovies2, numberMovies.Count()),
                    Tag = numberMovies.ToList()
                };
                result.Add(multiSelectionItem);
            }

            // get list of unique starting letters from collection
            foreach (var alpha in movies.Where(m => !Char.IsDigit(m.SortBy, 0)).Select(m => m.SortBy.ToUpperInvariant()[0]).Distinct().OrderBy(m => m))
            {
                var alphaMovies = movies.Where(m => m.SortBy.StartsWith(alpha.ToString(), StringComparison.InvariantCultureIgnoreCase));

                MultiSelectionItem multiSelectionItem = new MultiSelectionItem
                {
                    ItemTitle = string.Format(Translation.StartsWith, alpha.ToString()),
                    ItemTitle2 = string.Format(Translation.NumberOfMovies2, alphaMovies.Count()),
                    Tag = alphaMovies.ToList()
                };
                result.Add(multiSelectionItem);
            }

            return result;
        }

        /// <summary>
        /// Return a list of select items for each unique DBSourceInfo that has movies with valid IMDb ids
        /// </summary>
        /// <returns></returns>
        private List<MultiSelectionItem> GetSourceSelectItems()
        {
            List<MultiSelectionItem> result = new List<MultiSelectionItem>();

            // Create a new select item for each unique DBSourceInfo that has movies
            // Ignore IMDb+ as we cant do anything more with those
            foreach (var sourceInfo in DBMovieInfo.GetAll().Where(s => s.PrimarySource != IMDbPlusSource).Select(s => s.PrimarySource).Distinct())
            {
                int totalMovies = DBMovieInfo.GetAll().Where(m => m.PrimarySource == sourceInfo).Count();
                int imdbMovies = DBMovieInfo.GetAll().Where(m => m.PrimarySource == sourceInfo && IsValidIMDb(m.ImdbID)).Count();
                if (imdbMovies == 0) continue;

                Logger.Info("Adding source '{0}' to select dialog", (sourceInfo == null) ? "null" : sourceInfo.ToString());
                MultiSelectionItem multiSelectionItem = new MultiSelectionItem
                {
                    ItemTitle = (sourceInfo == null) ? Translation.NullSource : sourceInfo.ToString(),
                    ItemTitle2 = string.Format(Translation.NumberOfMovies, imdbMovies, totalMovies),
                    Selected = false,
                    Tag = sourceInfo
                };
                result.Add(multiSelectionItem);
            }

            return result;
        }

        private void HideShowForceIMDbPlusButton()
        {
            // skin may not have implemented button
            if (forceIMDbPlusButton == null) return;

            // if no applicable sources disable
            if (IMDbPlusSource == null)
            {
                Logger.Debug("IMDb+ source not installed!");
                GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "false", false);
                return;
            }
            // enable for skin compatibility
            GUIUtils.SetProperty("#IMDb.ForceIMDbPlus.Visible", "true", false);
        }

        private bool FilesAreEqual(string f1, string f2)
        {
            if (!File.Exists(f1)) return false;
            if (!File.Exists(f2)) return false;

            // get file length and make sure lengths are identical
            long length = new FileInfo(f1).Length;
            if (length != new FileInfo(f2).Length)
                return false;

            byte[] buf1 = new byte[4096];
            byte[] buf2 = new byte[4096];

            // open both for reading
            using (FileStream stream1 = File.OpenRead(f1))
            using (FileStream stream2 = File.OpenRead(f2))
            {
                // compare content for equality
                int b1, b2;
                while (length > 0)
                {
                    // figure out how much to read
                    int toRead = buf1.Length;
                    if (toRead > length)
                        toRead = (int)length;
                    length -= toRead;

                    // read a chunk from each and compare
                    b1 = stream1.Read(buf1, 0, toRead);
                    b2 = stream2.Read(buf2, 0, toRead);
                    for (int i = 0; i < toRead; ++i)
                        if (buf1[i] != buf2[i])
                            return false;
                }
            }

            return true;
        }

        private void RefreshIMDbPlusMovies()
        {
            if (IMDbPlusSource == null) return;

            if (moviesRefreshing)
            {
                Logger.Info("Cancelling Movie Refresh...");
                GUIControl.DisableControl(GetID, refreshMoviesButton.GetID);
                cancelRefreshing = true;
                return;
            }

            Thread refreshThread = new Thread(delegate(object obj)
            {
                // focus back to main facade
                GUIControl.FocusControl(GetID, Facade.GetID);

                if (PluginSettings.MoviesRefreshed == null)
                    PluginSettings.MoviesRefreshed = new List<string>();

                bool resume = PluginSettings.MoviesRefreshed.Count > 0;

                // add choices to menu
                List<GUIListItem> items = new List<GUIListItem>();

                GUIListItem item = new GUIListItem(string.Format("{0} {1}", Translation.UpdateAll, resume ? Translation.Resume : string.Empty));
                items.Add(item);
                item = new GUIListItem(string.Format("{0} {1}", Translation.UpdateReplacementOnly, resume ? Translation.Resume : string.Empty));
                items.Add(item);
                item = new GUIListItem(string.Format("{0} {1}", Translation.UpdateAlphas, resume ? Translation.Resume : string.Empty));
                items.Add(item);
                item = new GUIListItem(Translation.Cancel);
                items.Add(item);

                int selectedItem = GUIUtils.ShowMenuDialog(Translation.RefreshingMovies, items);
                if (selectedItem < 0 || selectedItem == 3) return;

                // Update Alphas
                var selectedItems = new List<MultiSelectionItem>();
                if (selectedItem == 2)
                {
                    // show multi-select dialog for user to choose
                    // which set of movies to refresh
                    var listItems = GetAlphaItems();
                    selectedItems = GUIUtils.ShowMultiSelectionDialog(Translation.SelectAlphas, listItems);
                    if (selectedItems == null || selectedItems.Where(i => i.Selected).Count() == 0) return;
                }

                moviesRefreshing = true;
                cancelRefreshing = false;
                SetButtonLabels();

                // Save options incase they have been updated
                PluginSettings.SaveSettings();
                   
                // get IMDb+ movies for refresh
                var movies = GetFilteredMovieListFromChoice(selectedItem, selectedItems);

                // we only want to update from primary source.
                int dataProviderReqLimit = MovingPicturesCore.Settings.DataProviderRequestLimit;
                MovingPicturesCore.Settings.DataProviderRequestLimit = -1;                    

                int moviesUpdated = 0;
                int moviesTotal = movies.Count();

                Logger.Info("Refreshing {0} Movies...", moviesTotal);
                foreach (var movie in movies)
                {
                    if (cancelRefreshing)
                    {
                        Logger.Info("Movie refresh cancelled");
                        GUIControl.EnableControl(GetID, refreshMoviesButton.GetID);
                        MovingPicturesCore.Settings.DataProviderRequestLimit = dataProviderReqLimit;
                        SetMovieRefreshProperties(null, -1, -1, true);
                        moviesRefreshing = false;
                        SetButtonLabels();

                        if (!PluginSettings.DisableNotifications)
                        {
                            GUIUtils.ShowNotifyDialog(Translation.RefreshMovies, string.Format(Translation.RefreshMoviesCancelNotification, PluginSettings.MoviesRefreshed.Count));
                        }
                        return;
                    }
                    
                    // disable refresh of movie browser whilst updating
                    // movingpics re-activates this on page load so we don't need to re-enable
                    if (MovingPicturesCore.Browser != null) MovingPicturesCore.Browser.AutoRefresh = false;

                    SetMovieRefreshProperties(movie, ++moviesUpdated, moviesTotal, false);
                    // skip over previous refreshed
                    if (PluginSettings.MoviesRefreshed.Contains(movie.ImdbID)) continue;

                    PluginSettings.MoviesRefreshed.Add(movie.ImdbID);
                    MovingPicturesCore.DataProviderManager.Update(movie);
                    movie.Commit();
                }

                MovingPicturesCore.Settings.DataProviderRequestLimit = dataProviderReqLimit;
                SetMovieRefreshProperties(null, -1, -1, true);
                moviesRefreshing = false;
                SetButtonLabels();

                // clear previous refreshed movies on successful finish
                PluginSettings.MoviesRefreshed.Clear();

                Logger.Info("Movie refresh completed");
                if (!PluginSettings.DisableNotifications)
                {
                    GUIUtils.ShowNotifyDialog(Translation.RefreshMovies, Translation.RefreshMoviesNotification);
                }
            })
            {
                IsBackground = true,
                Name = "Refresh Movies"
            };

            refreshThread.Start();
        }

        private List<DBMovieInfo> GetFilteredMovieListFromChoice(int choice, List<MultiSelectionItem> alphaList)
        {
            List<DBMovieInfo> result = new List<DBMovieInfo>();
            List<DBMovieInfo> movies = DBMovieInfo.GetAll().Where(m => m.PrimarySource == IMDbPlusSource).ToList();
            
            switch (choice)
            {
                // update all
                case 0:
                    result = movies;
                    break;
                
                // replacements only
                case 1:
                    Logger.Info("Filtering movie list...");
                    var coreReplacements = Replacements.GetAll(false);
                    if (coreReplacements != null)
                    {
                        // filter using core replacements
                        result = movies.Where(m => coreReplacements.Any(r => m.ImdbID == r.Id)).ToList();

                        var customReplacements = Replacements.GetAll(true);
                        if (customReplacements != null)
                        {
                            // add filter for custom replacements
                            result = result.Concat(movies.Where(m => customReplacements.Any(r => m.ImdbID == r.Id))).ToList();
                        }
                    }
                    Logger.Info("Filtered out {0} movies from {1} total.", movies.Count - result.Count, movies.Count);
                    break;

                // Alpha list
                case 2:                    
                    foreach (var alpha in alphaList.Where(i => i.Selected))
                    {
                        result.AddRange(alpha.Tag as IEnumerable<DBMovieInfo>);
                    }
                    break;
            }

            // sort by default sort comparer
            result.Sort();

            // return filtered list
            return result;
        }

        private void SetMovieRefreshProperties(DBMovieInfo movie, int progress, int total, bool cancelled)
        {
            if (cancelled)
            {
                GUIUtils.SetProperty("#IMDb.Movie.Refresh.Active", "false");
                GUIUtils.SetProperty("#IMDb.Movie.Refresh.Movie", string.Empty);
                GUIUtils.SetProperty("#IMDb.Movie.Refresh.ProgressPercent", string.Empty);
                GUIUtils.SetProperty("#IMDb.Movie.Refresh.CurrentItem", string.Empty);
                GUIUtils.SetProperty("#IMDb.Movie.Refresh.MovieCount", string.Empty);
                return;
            }

            int percent = (int)(((decimal)progress / total) * 100);

            Logger.Info("Refreshing movie information [{0}/{1}] '{2}'", progress, total, movie.ToString());

            GUIUtils.SetProperty("#IMDb.Movie.Refresh.Status", string.Format(Translation.RefreshMovieStatus, progress, total, percent));
            GUIUtils.SetProperty("#IMDb.Movie.Refresh.Active", "true");
            GUIUtils.SetProperty("#IMDb.Movie.Refresh.Movie", movie.ToString());
            GUIUtils.SetProperty("#IMDb.Movie.Refresh.ProgressPercent", percent.ToString());
            GUIUtils.SetProperty("#IMDb.Movie.Refresh.CurrentItem", progress.ToString());
            GUIUtils.SetProperty("#IMDb.Movie.Refresh.MovieCount", total.ToString());
        }

        private void SetButtonLabels()
        {
            if (GUIWindowManager.ActiveWindow != GetID) return;

            if (refreshMoviesButton != null)
            {
                GUIControl.SetControlLabel(GetID, refreshMoviesButton.GetID, moviesRefreshing ? Translation.RefreshCancel : Translation.RefreshMovies + "...");
            }
        }

        private void ShowIMDbPlusInformation()
        {
            if (IMDbPlusSource == null) return;

            var replacements = Replacements.GetAll(false);
            var customReplacements = Replacements.GetAll(true);

            List<string> textList = new List<string>();

            // IMdb Scraper Description
            textList.Add(IMDbPlusSource.Provider.Description);
            textList.Add(string.Empty);
            textList.Add(string.Format(Translation.InfoPluginVersion, PluginSettings.Version));
            textList.Add(string.Format(Translation.InfoScraperAuthor, IMDbPlusSource.Provider.Author));
            textList.Add(string.Format(Translation.InfoScraperVersion, IMDbPlusSource.Provider.Version));            
            textList.Add(string.Format(Translation.InfoScraperPriority, IMDbPlusSource.DetailsPriority == 0 ? Translation.First : IMDbPlusSource.DetailsPriority.ToString()));
            textList.Add(string.Format(Translation.InfoScraperPublished, IMDbPlusSource.SelectedScript.Provider.Published.Value.ToShortDateString()));
            textList.Add(string.Format(Translation.InfoScraperLastUpdateCheck, PluginSettings.SyncLastDateTime));
            textList.Add(string.Format(Translation.InfoMoviesIMDbPlusPrimary, DBMovieInfo.GetAll().Where(m => m.PrimarySource == IMDbPlusSource).Count()));
            textList.Add(string.Format(Translation.InfoMoviesOtherPlusPrimary, DBMovieInfo.GetAll().Where(m => m.PrimarySource != IMDbPlusSource).Count()));
            textList.Add(string.Format(Translation.InfoReplacementsVersion, Replacements.Version));
            textList.Add(string.Format(Translation.InfoReplacementsPublished, Replacements.Published.ToShortDateString()));
            textList.Add(string.Format(Translation.InfoReplacementEntries, replacements != null ? replacements.Count() : 0));
            textList.Add(string.Format(Translation.InfoCustomReplacementEntries, customReplacements != null ? customReplacements.Count() : 0));

            // show text dialog of information about plugin / scraper
            GUIUtils.ShowTextDialog(Translation.IMDbInfo, textList);
        }

        private bool IsValidIMDb(string imdbid)
        {
            // do some simple checks
            if (string.IsNullOrEmpty(imdbid)) return false;
            if (string.IsNullOrEmpty(imdbid.Trim())) return false;
            if (!imdbid.StartsWith("tt")) return false;
            if (imdbid.Length != 9) return false;
            return true;
        }

        private void ShutDownPhase()
        {
            if (moviesRefreshing)
            {
                // stop it gracefully
                RefreshIMDbPlusMovies();
                while (moviesRefreshing)
                {
                    Thread.Sleep(500);
                }
            }
        }

        #endregion

        #region Public Methods

        public static void UpdateTimer()
        {
            if (syncUpdateTimer == null) return;

            int syncInterval = PluginSettings.SyncInterval * 60 * 60 * 1000;
            int startTime = GetSyncStartTime();

            syncUpdateTimer.Change(startTime, syncInterval);
        }

        #endregion

        #region Event Handlers

        void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            if (e.Mode == Microsoft.Win32.PowerModes.Resume)
            {
                Logger.Info("MediaPortal resuming from Standby");
            }
            else if (e.Mode == Microsoft.Win32.PowerModes.Suspend)
            {
                Logger.Info("MediaPortal entering Standby");
                ShutDownPhase();
                PluginSettings.SaveSettings();
            }
        }

        #endregion

    }
}