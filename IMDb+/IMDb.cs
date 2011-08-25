using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using MediaPortal;
using MediaPortal.Configuration;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using MediaPortal.Plugins.MovingPictures;
using MediaPortal.Plugins.MovingPictures.DataProviders;
using Action = MediaPortal.GUI.Library.Action;

namespace IMDb
{
    public class IMDb : GUIWindow, ISetupForm
    {
        #region Private Variables

        int PluginID = 31415;

        [SkinControlAttribute(101)] protected GUIToggleButtonControl btnOriginalTitle = null;
        [SkinControlAttribute(102)] protected GUIToggleButtonControl btnForeignTitle = null;
        [SkinControlAttribute(103)] protected GUIToggleButtonControl btnForeignFirst = null;
        [SkinControlAttribute(104)] protected GUIToggleButtonControl btnSpecialEdition = null;
        [SkinControlAttribute(105)] protected GUIToggleButtonControl btnRenameTitles = null;
        [SkinControlAttribute(201)] protected GUIToggleButtonControl btnSingleScore = null;
        [SkinControlAttribute(202)] protected GUIToggleButtonControl btnMinImdbVotes = null;
        [SkinControlAttribute(203)] protected GUIToggleButtonControl btnLongSummary = null;
        [SkinControlAttribute(204)] protected GUIToggleButtonControl btnUkRating = null;
        [SkinControlAttribute(205)] protected GUIToggleButtonControl btnRefreshAllFields = null;
        [SkinControlAttribute(301)] protected GUIToggleButtonControl btnImdbScore = null;
        [SkinControlAttribute(302)] protected GUIToggleButtonControl btnImdbMetascore = null;
        [SkinControlAttribute(303)] protected GUIToggleButtonControl btnRottenMeter = null;
        [SkinControlAttribute(304)] protected GUIToggleButtonControl btnRottenAverage = null;
        [SkinControlAttribute(305)] protected GUIToggleButtonControl btnRottenTopCritics = null;
        [SkinControlAttribute(401)] protected GUIButtonControl btnLanguageFilter = null;
        [SkinControlAttribute(402)] protected GUIButtonControl btnCountryFilter = null;

        string CountryFilter { get; set; }
        string LanguageFilter { get; set; }

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
            strButtonText = PluginName();
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

            // Set GUI options to match the stored settings from XML file
            btnOriginalTitle.Selected = xmlReader.GetOptionValueAsBool("global_options_original_title", false);
            btnForeignTitle.Selected = xmlReader.GetOptionValueAsBool("global_options_foreign_title", false);
            btnForeignFirst.Selected = xmlReader.GetOptionValueAsBool("global_options_foreign_first", false);
            btnUkRating.Selected = xmlReader.GetOptionValueAsBool("global_options_uk_rating", false);
            btnImdbScore.Selected = xmlReader.GetOptionValueAsBool("global_options_imdb_score", false);
            btnImdbMetascore.Selected = xmlReader.GetOptionValueAsBool("global_options_imdb_metascore", false);
            btnLongSummary.Selected = xmlReader.GetOptionValueAsBool("global_options_long_summary", false);
            btnRottenMeter.Selected = xmlReader.GetOptionValueAsBool("global_options_rotten_meter", false);
            btnRottenAverage.Selected = xmlReader.GetOptionValueAsBool("global_options_rotten_average", false);
            btnRottenTopCritics.Selected = xmlReader.GetOptionValueAsBool("global_options_rotten_top_critics", false);
            btnSpecialEdition.Selected = xmlReader.GetOptionValueAsBool("global_options_special_edition", true);
            btnRenameTitles.Selected = xmlReader.GetOptionValueAsBool("global_options_rename_titles", true);
            btnSingleScore.Selected = xmlReader.GetOptionValueAsBool("global_options_single_score", false);
            btnMinImdbVotes.Selected = xmlReader.GetOptionValueAsBool("global_options_min_imdb_votes", false);
            btnRefreshAllFields.Selected = xmlReader.GetOptionValueAsBool("global_options_refresh_all_fields", false);

            CountryFilter = xmlReader.GetOptionValueAsString("global_options_country_filter", "us|ca|gb|ie|au|nz");
            LanguageFilter = xmlReader.GetOptionValueAsString("global_options_language_filter", "en");

            // Disable buttons according to their sub-grouping
            btnForeignFirst.Disabled = (!btnForeignTitle.Selected) ? true : false;
            btnImdbMetascore.Disabled = (!btnSingleScore.Selected) ? true : false;
            btnRottenMeter.Disabled = (!btnSingleScore.Selected) ? true : false;
            btnRottenAverage.Disabled = (!btnSingleScore.Selected) ? true : false;
            btnRottenTopCritics.Disabled = (!btnSingleScore.Selected) ? true : false;

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

            xmlWriter.SetOptionsEntry("global_options_original_title", "01", btnOriginalTitle.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_foreign_title", "02", btnForeignTitle.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_foreign_first", "03", btnForeignFirst.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_uk_rating", "04", btnUkRating.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_imdb_score", "05", btnImdbScore.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_imdb_metascore", "06", btnImdbMetascore.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_long_summary", "07", btnLongSummary.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_rotten_meter", "08", btnRottenMeter.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_rotten_average", "09", btnRottenAverage.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_rotten_top_critics", "10", btnRottenTopCritics.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_special_edition", "11", btnSpecialEdition.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_rename_titles", "12", btnRenameTitles.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_single_score", "13", btnSingleScore.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_min_imdb_votes", "14", btnMinImdbVotes.Selected.ToString());
            xmlWriter.SetOptionsEntry("global_options_refresh_all_fields", "15", btnRefreshAllFields.Selected.ToString());
            
            xmlWriter.SetOptionsEntry("global_options_country_filter", "98", CountryFilter);
            xmlWriter.SetOptionsEntry("global_options_language_filter", "99", LanguageFilter);
            
            // save file
            xmlWriter.Save(file);

            base.OnPageDestroy(new_windowId);
        }

        protected override void OnClicked(int controlId, GUIControl control, Action.ActionType actionType)
        {
            base.OnClicked(controlId, control, actionType);

            switch (controlId)
            {
                case 101: //gOption_original_title
                    break;
                case 102: //gOption_foreign_title
                    if (!btnForeignTitle.Selected)
                    {
                        // Foreign titles disabled, so clear Foreign title first setting
                        btnForeignFirst.Selected = false;
                    }
                    btnForeignFirst.Disabled = (!btnForeignTitle.Selected) ? true : false;
                    break;
                case 103: //gOption_foreign_first
                    break;
                case 104: //gOption_special_edition
                    break;
                case 105: //gOption_rename_titles
                    break;
                case 201: //gOption_single_score
                    if (!btnSingleScore.Selected)
                    {
                        // Single score disabled, so clear out individial score options
                        btnImdbMetascore.Selected = false;
                        btnRottenMeter.Selected = false;
                        btnRottenAverage.Selected = false;
                        btnRottenTopCritics.Selected = false;
                    }
                    btnImdbMetascore.Disabled = (!btnSingleScore.Selected) ? true : ((!btnImdbScore.Selected) ? true : false);
                    btnRottenMeter.Disabled = (!btnSingleScore.Selected) ? true : ((btnImdbScore.Selected) ? true : false);
                    btnRottenAverage.Disabled = (!btnSingleScore.Selected) ? true : ((btnImdbScore.Selected) ? true : false);
                    btnRottenTopCritics.Disabled = (!btnSingleScore.Selected) ? true : ((!btnRottenMeter.Selected) ? true : false);
                    break;
                case 202: //gOption_min_imdb_votes
                    break;
                case 203: //gOption_long_summary
                    break;
                case 204: //gOption_uk_rating
                    break;
                case 205: //gOption_refresh_all_fields
                    break;
                case 301: //gOption_imdb_score
                    if (btnImdbScore.Selected)
                    {
                        // IMDb Score selected, so clear out RT score options
                        btnRottenMeter.Selected = false;
                        btnRottenAverage.Selected = false;
                        btnRottenTopCritics.Selected = false;
                    }
                    else
                    {
                        btnImdbMetascore.Selected = false;
                    }
                    btnImdbMetascore.Disabled = (!btnImdbScore.Selected) ? true : ((!btnSingleScore.Selected) ? true : false);
                    btnRottenMeter.Disabled = (btnImdbScore.Selected) ? true : ((!btnSingleScore.Selected) ? true : false);
                    btnRottenAverage.Disabled = (btnImdbScore.Selected) ? true : ((!btnSingleScore.Selected) ? true : false);
                    btnRottenTopCritics.Disabled = (!btnImdbScore.Selected) ? true : ((!btnRottenMeter.Selected) ? true : false);
                    break;
                case 302: //gOption_imdb_metascore
                    break;
                case 303: //gOption_rotten_meter
                    if (!btnRottenMeter.Selected)
                    {
                        btnRottenTopCritics.Selected = false;
                    }
                    btnRottenTopCritics.Disabled = (!btnRottenMeter.Selected) ? true : false;
                    break;
                case 304: //gOption_rotten_average
                    break;
                case 305: //gOption_rotten_top_critics
                    if (btnRottenTopCritics.Selected) btnRottenMeter.Selected = true;
                    break;
                case 401: //gOption_language_filter
                    string output = LanguageFilter.Replace('|', '.');
                    if (GUIUtils.GetStringFromKeyboard(ref output))
                    {
                        LanguageFilter = output.Replace('.', '|');
                    }
                    break;
                case 402: //gOption_country_filter
                    output = CountryFilter.Replace('|', '.');
                    if (GUIUtils.GetStringFromKeyboard(ref output))
                    {
                        CountryFilter = output.Replace('.', '|');
                    }
                    break;
                case 403: //update_Scraper
                    // grab the contents of the file and try
                    StreamReader reader = new StreamReader(@"C:\Scraper.IMDb+.xml");
                    string script = reader.ReadToEnd();
                    reader.Close();

                    // and add it to the manager
                    DataProviderManager.AddSourceResult addResult = MovingPicturesCore.DataProviderManager.AddSource(typeof(ScriptableProvider), script, true);

                    if (addResult == DataProviderManager.AddSourceResult.FAILED_VERSION)
                    {
                        Logger.Error("Load Script Failed: A script with this Version and ID is already loaded.");
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
                        Logger.Error("Load Script Warning: Scraper debug-mode enabled, so existing script was replaced.");
                    }  
                    else
                    {
                        Logger.Error("Load Script: last end-if.");
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
                case GUIMessage.MessageType.GUI_MSG_SETFOCUS:
                {
                    switch (message.TargetControlId)
                    {
                        case 101: //gOption_original_title
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.OriginalTitleDescription);
                            break;
                        case 102: //gOption_foreign_title
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.AddForeignTitleDescription);
                            break;
                        case 103: //gOption_foreign_first
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.ForeignTitleFirstDescription);
                            break;
                        case 104: //gOption_special_edition
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SpecialEditionsDescription);
                            break;
                        case 105: //gOption_rename_titles
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RenameTitlesDescription);
                            break;
                        case 201: //gOption_single_score
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.SingleScoreDescription);
                            break;
                        case 202: //gOption_min_imdb_votes
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.MinImdbVotesDescription);
                            break;
                        case 203: //gOption_long_summary
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.LongSummaryDescription);
                            break;
                        case 204: //gOption_uk_rating
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.UkRatingDescription);
                            break;
                        case 205: //gOption_refresh_all_fields
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RefreshAllFieldsDescription);
                            break;
                        case 301: //gOption_imdb_score
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.IMDbScoreDescription);
                            break;
                        case 302: //gOption_imdb_metascore
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.IMDbMetaScoreDescription);
                            break;
                        case 303: //gOption_rotten_meter
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenMeterDescription);
                            break;
                        case 304: //gOption_rotten_average
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenAverageDescription);
                            break;
                        case 305: //gOption_rotten_top_critics
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.RottenTopCriticsDescription);
                            break;
                        case 401: //gOption_language_filter
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.LanguageFilterDescription);
                            break;
                        case 402: //gOption_country_filter
                            GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.CountryFilterDescription);
                            break;

                        default:
                            // No default message, so textbox scroll controls can be used without altering text.
                            break;
                    }
                    break;
                }
                case GUIMessage.MessageType.GUI_MSG_WINDOW_INIT:
                {
                    GUIPropertyManager.SetProperty("#IMDb.Option.Description", Translation.DefaultDescription);
                    break;
                }

            }
            return base.OnMessage(message);
        }

        #endregion
    }
}
