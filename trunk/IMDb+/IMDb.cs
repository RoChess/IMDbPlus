using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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

        int PluginID = 31415;

        #region Skin Controls

        [SkinControl(50)] protected GUIFacadeControl Facade = null;

        #endregion

        string CountryFilter { get; set; }
        string LanguageFilter { get; set; }
        DBSourceInfo ImdbPlusSource;

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
            Logger.Info("Starting IMDb+ v{0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());           

            // Initialize translations
            Translation.Init();

            // Get IMDb+ Data Provider
            ImdbPlusSource = DBSourceInfo.GetAll().Find(s => s.ToString() == "IMDb+");
            SetIMDbProperties();

            // Load main skin window
            // this is a launching pad to all other windows
            string xmlSkin = GUIGraphicsContext.Skin + @"\IMDb+.xml";
            Logger.Info("Loading main skin window: " + xmlSkin);
            return Load(xmlSkin);
        }

        /// <summary>
        /// End Point (Clean up)
        /// </summary>
        public override void DeInit()
        {
            Logger.Info("Goodbye");
            base.DeInit();
        }

        protected override void OnPageLoad()
        {
            // Load Options file
            Logger.Info("Loading IMDb+ options from file");
            XmlReader xmlReader = new XmlReader();
            if (!xmlReader.Load(@"C:\Options IMDb+ Scraper.xml"))
            {
                Logger.Error("Error opening IMDb+ Options file, will restore defaults.");
            }

            GUIControl.ClearControl(GetID, Facade.GetID);

            int itemId = 0;
            string listIndentation = "   ";
            UpdateListItem(itemId++, Translation.OriginalTitle, (xmlReader.GetOptionValueAsBool("global_options_original_title", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.AddForeignTitle, (xmlReader.GetOptionValueAsBool("global_options_foreign_title", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, listIndentation + Translation.ForeignTitleFirst, (xmlReader.GetOptionValueAsBool("global_options_foreign_first", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, Translation.SpecialEditions, (xmlReader.GetOptionValueAsBool("global_options_special_edition", true)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.RenameTitles, (xmlReader.GetOptionValueAsBool("global_options_rename_titles", true)) ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.SingleScore, (xmlReader.GetOptionValueAsBool("global_options_single_score", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, listIndentation + Translation.IMDbScore, (xmlReader.GetOptionValueAsBool("global_options_imdb_score", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.IMDbMetaScore, (xmlReader.GetOptionValueAsBool("global_options_imdb_metascore", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenMeter, (xmlReader.GetOptionValueAsBool("global_options_rotten_meter", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenAverage, (xmlReader.GetOptionValueAsBool("global_options_rotten_average", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);
            UpdateListItem(itemId++, listIndentation + Translation.RottenTopCritics, (xmlReader.GetOptionValueAsBool("global_options_rotten_top_critics", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);

            UpdateListItem(itemId++, Translation.MinImdbVotes, (xmlReader.GetOptionValueAsBool("global_options_min_imdb_votes", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.LongSummary, (xmlReader.GetOptionValueAsBool("global_options_long_summary", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.UkRating, (xmlReader.GetOptionValueAsBool("global_options_uk_rating", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.OneWriterDirector, (xmlReader.GetOptionValueAsBool("global_options_one_writer_director", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");
            UpdateListItem(itemId++, Translation.SecondaryDetails, GetCountryString(Convert.ToInt32(xmlReader.GetOptionValueAsString("global_options_secondary_details", "01"))), "folder");
            UpdateListItem(itemId++, listIndentation + Translation.SecondarySummary, (xmlReader.GetOptionValueAsBool("global_options_secondary_summary", false)) ? Translation.BoolOn : Translation.BoolOff, string.Empty);

            UpdateListItem(itemId++, Translation.RefreshAllFields, (xmlReader.GetOptionValueAsBool("global_options_refresh_all_fields", false)) ? Translation.BoolOn : Translation.BoolOff, "folder");

            UpdateListItem(itemId++, Translation.CountryFilter, xmlReader.GetOptionValueAsString("global_options_country_filter", "us|ca|gb|ie|au|nz"), "folder");
            UpdateListItem(itemId++, Translation.LanguageFilter, xmlReader.GetOptionValueAsString("global_options_language_filter", "en"), "folder");

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
            // save settings
            Logger.Info("Saving IMDb+ options to file");

            XmlWriter xmlWriter = new XmlWriter();
            string file = @"C:\Options IMDb+ Scraper.xml";
            if (!xmlWriter.Load(file))
            {
                Logger.Error("Error opening IMDb+ Options file, re-creating...");
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error deleting file: '{0}'", file);
                        Logger.Error("Exception: {0}", e.Message);
                        return;
                    }
                }
            
                // create it
                xmlWriter.CreateXmlConfigFile(file);
            }

            foreach (GUIListItem item in Facade.ListLayout.ListItems)
            {
                if (item.Label.Trim() == Translation.OriginalTitle)
                    xmlWriter.SetOptionsEntry("global_options_original_title", "01", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.AddForeignTitle)
                    xmlWriter.SetOptionsEntry("global_options_foreign_title", "02", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.ForeignTitleFirst)
                    xmlWriter.SetOptionsEntry("global_options_foreign_first", "03", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.UkRating)
                    xmlWriter.SetOptionsEntry("global_options_uk_rating", "04", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.IMDbScore)
                    xmlWriter.SetOptionsEntry("global_options_imdb_score", "05", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.IMDbMetaScore)
                    xmlWriter.SetOptionsEntry("global_options_imdb_metascore", "06", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.LongSummary)
                    xmlWriter.SetOptionsEntry("global_options_long_summary", "07", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.RottenMeter)
                    xmlWriter.SetOptionsEntry("global_options_rotten_meter", "08", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.RottenAverage)
                    xmlWriter.SetOptionsEntry("global_options_rotten_average", "09", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.RottenTopCritics)
                    xmlWriter.SetOptionsEntry("global_options_rotten_top_critics", "10", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.SpecialEditions)
                    xmlWriter.SetOptionsEntry("global_options_special_edition", "11", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.RenameTitles)
                    xmlWriter.SetOptionsEntry("global_options_rename_titles", "12", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.SingleScore)
                    xmlWriter.SetOptionsEntry("global_options_single_score", "13", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.MinImdbVotes)
                    xmlWriter.SetOptionsEntry("global_options_min_imdb_votes", "14", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.RefreshAllFields)
                    xmlWriter.SetOptionsEntry("global_options_refresh_all_fields", "15", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.OneWriterDirector)
                    xmlWriter.SetOptionsEntry("global_options_one_writer_director", "16", (item.Label2 == Translation.BoolOn) ? "true" : "false");

                if (item.Label.Trim() == Translation.SecondarySummary)
                    xmlWriter.SetOptionsEntry("global_options_secondary_summary", "96", (item.Label2 == Translation.BoolOn) ? "true" : "false");
                if (item.Label.Trim() == Translation.SecondaryDetails)
                    xmlWriter.SetOptionsEntry("global_options_secondary_details", "97", GetCountryIntAsString(item.Label2));
                if (item.Label.Trim() == Translation.CountryFilter)
                    xmlWriter.SetOptionsEntry("global_options_country_filter", "98", item.Label2);
                if (item.Label.Trim() == Translation.LanguageFilter)
                    xmlWriter.SetOptionsEntry("global_options_language_filter", "99", item.Label2);
            }
            
            // save file
            xmlWriter.Save(file);

            base.OnPageDestroy(new_windowId);
        }

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

            if (item.Label.Trim() == Translation.MinImdbVotes)
                GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.MinImdbVotesDescription);
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

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);

            switch (controlId)
            {
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

        public override bool OnMessage(GUIMessage message)
        {
            switch (message.Message)
            {
                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                {
                    GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.OriginalTitleDescription);
                    //GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.DefaultDescription);
                    break;
                }

            }
            return base.OnMessage(message);
        }

        #endregion

        private void SetIMDbProperties()
        {
            // not installed
            if (ImdbPlusSource == null)
            {
                GUIUtils.SetProperty("#IMDb.Scraper.IsInstalled", "false");
                return;
            }

            GUIUtils.SetProperty("#IMDb.Scraper.IsInstalled", "true");
            GUIUtils.SetProperty("#IMDb.Scraper.Version", ImdbPlusSource.Provider.Version);
            GUIUtils.SetProperty("#IMDb.Scraper.Description", ImdbPlusSource.Provider.Description);
            GUIUtils.SetProperty("#IMDb.Scraper.Author", ImdbPlusSource.Provider.Author);
            GUIUtils.SetProperty("#IMDb.Scraper.Published", ImdbPlusSource.SelectedScript.Provider.Published.ToString());
        }
    }
}
