using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WvWOverlay
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Model.XML.Profession> m_oLstProfessions;
        public List<Model.XML.Objective> m_oLstObjectives;

        private Model.XML.Region m_oSelectedRegion;

        private Thread m_oThreadMumbleFileProvider;
        public bool m_bRunMumbleFileProvider = true;

        private MumbleLink.Coordinate m_oCurrentCoordinate;
        private Model.mumble_identity m_oCurrentIdentity;
        private Thread m_oThreadPlayerDisplayProvider;
        private bool m_bRunPlayerDisplay = true;

        private Thread m_oThreadMatchProvider;
        private bool m_bRunMatchProvider = true;

        private Model.XML.Map m_oCurrentMap;
        private Model.API.world m_oCurrentWorld;

        private string m_cCurrentSpotifyString;


        private Model.API.matches_match m_oCurrentMatch;

        private enum CurrentDisplay
        {
            RegionSelection,
            MatchSelection,
            Timer
        }

        private CurrentDisplay m_eCurrentDisplay;



        private LogWriter m_oLogWriter;
        public LogWriter LOGWRITER
        {
            get
            {
                if (m_oLogWriter == null)
                {
                    m_oLogWriter = new LogWriter(System.IO.Path.Combine(Environment.CurrentDirectory, "Log"), new TimeSpan(7, 0, 0, 0));
                }
                return m_oLogWriter;
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            m_eCurrentDisplay = CurrentDisplay.RegionSelection;
        }

        /// <summary>
        /// Region Selected
        ///  > Load Matches
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void On_RegionSelected(object sender, EventArgs e)
        {
            RegionSelectedEventArgs oArgs;

            try
            {
                oArgs = (RegionSelectedEventArgs)e;
                itemscontrolMain.Items.Clear();

                m_oSelectedRegion = oArgs.Region;

                StartMatchDownload();

            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Download der Matchlist
        /// </summary>
        public void StartMatchDownload()
        {
            WebClient oClient;
            BackgroundWorker oBackgroundWorker;

            try
            {
                TriggerLoadingIndicator();

                oBackgroundWorker = new BackgroundWorker();

                oBackgroundWorker.DoWork += delegate(object sender, DoWorkEventArgs e)
                {
                    oClient = new WebClient();
                    oClient.DownloadStringCompleted += new DownloadStringCompletedEventHandler(On_MatchDownloadFinished);
                    oClient.DownloadStringAsync(new Uri(@"http://gw2stats.net/api/matches.json"));
                };

                oBackgroundWorker.RunWorkerAsync();

            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Download Matchlist finished
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_MatchDownloadFinished(object sender, DownloadStringCompletedEventArgs e)
        {
            Model.API.matches_json oMatchlist;
            MatchItem oMatchItem;

            try
            {
                if (!string.IsNullOrWhiteSpace(e.Result))
                {
                    oMatchlist = JsonConvert.DeserializeObject<Model.API.matches_json>(e.Result);

                    if (m_oSelectedRegion != null && oMatchlist != null)
                    {
                        if (oMatchlist.region.ContainsKey(m_oSelectedRegion.Slug))
                        {
                            m_eCurrentDisplay = CurrentDisplay.MatchSelection;

                            foreach (Model.API.matches_match oMatch in oMatchlist.region[m_oSelectedRegion.Slug])
                            {
                                itemscontrolMain.Dispatcher.Invoke(delegate
                                {
                                    oMatchItem = new MatchItem(oMatch);
                                    oMatchItem.MatchSelected += new EventHandler(On_MatchSelected);
                                    itemscontrolMain.Items.Add(oMatchItem);
                                });
                            }
                        }
                        else
                        {
                            MessageBox.Show("Region not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    MessageBox.Show("No Matchlist", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }

            TriggerLoadingIndicator();
        }

        /// <summary>
        /// Match Selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_MatchSelected(object sender, EventArgs e)
        {
            MatchSelectedEventArgs oArgs;

            try
            {
                oArgs = (MatchSelectedEventArgs)e;

                TriggerLoadingIndicator();
                itemscontrolMain.Items.Clear();

                m_oCurrentMatch = oArgs.Match;
                m_oCurrentWorld = oArgs.World;

                m_bRunMumbleFileProvider = true;
                m_oThreadMumbleFileProvider = new Thread(new ThreadStart(MumbleFileProvider));
                m_oThreadMumbleFileProvider.Start();

                m_bRunPlayerDisplay = true;
                m_oThreadPlayerDisplayProvider = new Thread(new ThreadStart(PlayerDisplayProvider));
                m_oThreadPlayerDisplayProvider.Start();


                m_bRunMatchProvider = true;

                StartMatchThread(oArgs.Match);

                m_eCurrentDisplay = CurrentDisplay.Timer;


                TriggerLoadingIndicator();
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Zeigt die Ladeansicht
        /// </summary>
        public void TriggerLoadingIndicator()
        {
            try
            {
                progressbarLoading.Dispatcher.Invoke((ThreadStart)delegate
                {
                    progressbarLoading.Visibility = progressbarLoading.Visibility == System.Windows.Visibility.Hidden ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
                });
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Zeigt die Ladeansicht
        /// </summary>
        public void TriggerLoadingIndicator(System.Windows.Visibility eVisibility)
        {
            try
            {
                progressbarLoading.Dispatcher.Invoke((ThreadStart)delegate
                {
                    progressbarLoading.Visibility = eVisibility;
                });
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }



        /// <summary>
        /// Zeigt die Regionsansicht
        /// </summary>
        private void InitializeRegionSelection()
        {
            List<Model.XML.Region> oLstRegions;
            try
            {
                m_eCurrentDisplay = CurrentDisplay.RegionSelection;
                m_oCurrentMap = null;
                m_oCurrentMatch = null;
                m_oCurrentWorld = null;

                itemscontrolMain.Items.Clear();

                oLstRegions = ConfigurationParser.GetRegions();

                foreach (Model.XML.Region oRegion in oLstRegions)
                {
                    RegionItem oItem = new RegionItem(oRegion);
                    oItem.RegionSelected += new EventHandler(On_RegionSelected);
                    itemscontrolMain.Items.Add(oItem);
                }


                imageBloodlustColor.Dispatcher.Invoke(delegate
                {
                    imageBloodlustColor.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\Resources\Icons\bloodlust_neutral.png"));
                    labelPlayerWorld.Content = string.Empty;
                });


            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Schließen des Fensters
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_imageClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Closing all the things
        /// </summary>
        private void ApplicationShutdown()
        {
            m_bRunMumbleFileProvider = false;
            m_bRunMatchProvider = false;

            if (m_oThreadMumbleFileProvider != null)
                m_oThreadMumbleFileProvider.Abort();
            if (m_oThreadMatchProvider != null)
                m_oThreadMatchProvider.Abort();
            if (m_oThreadPlayerDisplayProvider != null)
                m_oThreadPlayerDisplayProvider.Abort();



            Application.Current.Shutdown();
            Environment.Exit(0);
        }

        /// <summary>
        /// Dragmove bei linksklick auf Titelbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_HeaderDrag(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// Opacity Hover Handling
        ///  > Enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_OpacityHoverEnter(object sender, MouseEventArgs e)
        {
            List<PropertyInfo> oLstProperties = sender.GetType().GetProperties().ToList();
            PropertyInfo oOpacityProperty = oLstProperties.Find(x => x.Name == "Opacity");

            if (oOpacityProperty != null)
            {
                oOpacityProperty.SetValue(sender, 1D);
            }
        }

        /// <summary>
        /// Opacity Hover Handling
        ///  > Leave
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_OpacityHoverLeave(object sender, MouseEventArgs e)
        {
            List<PropertyInfo> oLstProperties = sender.GetType().GetProperties().ToList();
            PropertyInfo oOpacityInfo = oLstProperties.Find(x => x.Name == "Opacity");

            if (oOpacityInfo != null)
            {
                oOpacityInfo.SetValue(sender, 0.5F);
            }
        }

        /// <summary>
        /// Window-Start
        /// Initialisieren der Regionsauswahl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TriggerLoadingIndicator();

                InitializeRegionSelection();
                m_oLstProfessions = ConfigurationParser.GetProfessions();

                m_oLstObjectives = ConfigurationParser.GetObjectives();

                LOGWRITER.WriteMessage(string.Format("Overlay started, Machine [{0}]", Environment.MachineName), LogWriter.MESSAGE_TYPE.Info);

                TriggerLoadingIndicator();


            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        private void On_imageSelectMatch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopMatchThread();
            if (m_oThreadMumbleFileProvider != null)
                m_oThreadMumbleFileProvider.Abort();

            if (m_oThreadPlayerDisplayProvider != null)
                m_oThreadPlayerDisplayProvider.Abort();

            InitializeRegionSelection();

        }

        /// <summary>
        /// Zieht die Mumble-Coordinate aus dem RAM
        /// </summary>
        private void MumbleFileProvider()
        {
            MumbleLink oMumbleLink;
            List<Model.XML.Map> oLstMaps;

            try
            {
                oMumbleLink = new MumbleLink();

                oLstMaps = ConfigurationParser.GetMaps();

                while (m_bRunMumbleFileProvider)
                {
                    try
                    {
                        m_oCurrentCoordinate = oMumbleLink.GetCoordinates();

                        if (!string.IsNullOrEmpty(m_oCurrentCoordinate.ind))
                        {
                            m_oCurrentIdentity = JsonConvert.DeserializeObject<Model.mumble_identity>(m_oCurrentCoordinate.ind);

                            //Invoke Mapchange if necessary
                            if (m_oCurrentMap != null && m_oCurrentMap.MapID != m_oCurrentIdentity.map_id)
                            {
                                StopMatchThread();
                                m_oCurrentMap = oLstMaps.Find(x => x.MapID == m_oCurrentIdentity.map_id);
                                StartMatchThread(m_oCurrentMatch);
                            }
                            else
                            {
                                m_oCurrentMap = oLstMaps.Find(x => x.MapID == m_oCurrentIdentity.map_id);
                            }
                        }
                        else
                        {

                        }
                    }
                    catch (Exception oEx)
                    {
                        LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
                    }
                    Thread.Sleep(250);
                }
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Führt den Mumblelink-Thread aus
        /// </summary>
        private void PlayerDisplayProvider()
        {
            Model.XML.Profession oProfession;

            SpotifyLocalApi oAPI = null;
            Status oSpotifyStatus = null;

            try
            {

                //Spotify Integration
                try
                {
                    oAPI = new SpotifyLocalApi();
                    if (oAPI.Cfid.Token == null)
                    {
                        m_cCurrentSpotifyString = string.Empty;
                    }
                }
                catch (Exception)
                {
                    //Spotify not running
                    m_cCurrentSpotifyString = string.Empty;
                }


                while (m_bRunPlayerDisplay)
                {
                    //World Name
                    if (!string.IsNullOrWhiteSpace(m_oCurrentCoordinate.ind))
                    {

                        oProfession = m_oLstProfessions.Find(x => x.Id == m_oCurrentIdentity.profession);

                        if (oProfession != null)
                        {
                            //Invoke for multithreading-access
                            labelPlayerCharacter.Dispatcher.Invoke(delegate { labelPlayerCharacter.Content = string.Format("{0} - {1}", m_oCurrentIdentity.name, oProfession.Name); });
                        }
                    }
                    else
                    {
                        m_oCurrentMap = null;

                        labelPlayerCharacter.Dispatcher.Invoke(delegate { labelPlayerCharacter.Content = string.Empty; });
                        if (m_eCurrentDisplay == CurrentDisplay.Timer)
                            itemscontrolMain.Dispatcher.Invoke(delegate { itemscontrolMain.Items.Clear(); });
                    }


                    //Spotify Display
                    if (oAPI != null)
                    {
                        oSpotifyStatus = oAPI.Status;
                        if (oAPI.Cfid.Token != null && oSpotifyStatus != null && oSpotifyStatus.Track != null)
                        {
                            if (oSpotifyStatus.Playing)
                            {
                                m_cCurrentSpotifyString = string.Format("{0} - {1}",
                                    oSpotifyStatus.Track.ArtistResource.Name,
                                    oSpotifyStatus.Track.TrackResource.Name);

                                labelPlayerWorld.Dispatcher.Invoke(delegate
                                {
                                    labelPlayerWorld.Content = m_cCurrentSpotifyString;
                                });
                            }
                            else
                            {
                                labelPlayerWorld.Dispatcher.Invoke(delegate
                                {
                                    labelPlayerWorld.Content = m_oCurrentWorld.name;
                                });
                            }
                        }
                        else
                        {
                            labelPlayerWorld.Dispatcher.Invoke(delegate
                            {
                                labelPlayerWorld.Content = m_oCurrentWorld.name;
                            });
                        }
                    }
                    else
                    {
                        labelPlayerWorld.Dispatcher.Invoke(delegate
                        {
                            labelPlayerWorld.Content = m_oCurrentWorld.name;
                        });
                    }


                    Thread.Sleep(2000);
                }
            }
            catch (ThreadAbortException)
            {

            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
            
        }

        /// <summary>
        /// Campmode switch
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_imageCampMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (imageCampMode.Tag.ToString() == "false")
                {
                    imageCampMode.Opacity = 0.8D;
                    imageCampMode.Tag = "true";

                    if (m_eCurrentDisplay == CurrentDisplay.Timer)
                        RestartMatchThread(m_oCurrentMatch);
                }
                else
                {
                    imageCampMode.Opacity = 0.4D;
                    imageCampMode.Tag = "false";

                    if (m_eCurrentDisplay == CurrentDisplay.Timer)
                        RestartMatchThread(m_oCurrentMatch);
                }
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Runs the Match THread
        /// </summary>
        private void MatchProvider(object oMatchObj)
        {
            Model.API.matches_match oMatchInput;
            Model.API.match oMatch;
            Model.API.map oMap;
            Model.API.map oLastMap = null;

            ObjectiveItem oListItem = null;

            string cHeaderLine = string.Empty;
            string cIconLink = string.Empty;

            int nCountBloodlustStacks = 0;

            try
            {
                oMatchInput = (Model.API.matches_match)oMatchObj;

                while (m_bRunMatchProvider)
                {
                    TriggerLoadingIndicator();

                    nCountBloodlustStacks = 0;
                    cHeaderLine = string.Empty;

                    oMatch = this.GetMatch(oMatchInput.match_id);

                    if (oMatch != null && m_oCurrentMap != null)
                    {
                        oMap = oMatch.maps.Find(x => x.map_id == m_oCurrentMap.Gw2StatsID);

                        if (oLastMap != null && oMap.map_id != oLastMap.map_id)
                        {
                            itemscontrolMain.Dispatcher.Invoke(delegate { itemscontrolMain.Items.Clear(); });
                        }
                        oLastMap = oMap;

                        //Set Header
                        if (m_oCurrentMap.MapID != 38)
                        {
                            cHeaderLine += oMap.map_owner_name + " ";
                        }
                        cHeaderLine += m_oCurrentMap.Title;

                        labelMatchupMapTitle.Dispatcher.Invoke(delegate { labelMatchupMapTitle.Content = cHeaderLine; });

                        //Bloodlust
                        nCountBloodlustStacks += oMatch.bloodlust.blue_owner_id == m_oCurrentWorld.world_id ? 1 : 0;
                        nCountBloodlustStacks += oMatch.bloodlust.red_owner_id == m_oCurrentWorld.world_id ? 1 : 0;
                        nCountBloodlustStacks += oMatch.bloodlust.green_owner_id == m_oCurrentWorld.world_id ? 1 : 0;

                        imageBloodlustColor.Dispatcher.Invoke(delegate
                        {
                            imageBloodlustColor.Source = new BitmapImage(new Uri(GetBloodlustByMap(oMatch)));
                            labelBloodlustStackCount.Content = string.Format("{0} Stack{1}",
                                nCountBloodlustStacks,
                                nCountBloodlustStacks == 0 || nCountBloodlustStacks > 1 ? "s" : "");
                        });

                        //Score
                        labelScoreBlue.Dispatcher.Invoke(delegate
                        {
                            //Green
                            labelScoreGreen.Content = oMatchInput.worlds.Find(x => x.color == "Green").ppt;
                            
                            //Blue
                            labelScoreBlue.Content = oMatchInput.worlds.Find(x => x.color == "Blue").ppt;

                            //Red
                            labelScoreRed.Content = oMatchInput.worlds.Find(x => x.color == "Red").ppt;

                        });

                        oMap.objectives_list = (List<Model.API.objective>)oMap.objectives_list.Where(x => x.points > 0).OrderByDescending(x => x.points).ThenBy(x => m_oLstObjectives.Find(y => y.Id == x.id).Name).ToList();
                        
                        foreach (Model.API.objective oObjective in oMap.objectives_list)
                        {
                            if (oObjective.points > 0)
                            {
                                itemscontrolMain.Dispatcher.Invoke(delegate { oListItem = (ObjectiveItem)itemscontrolMain.ItemContainerGenerator.Items.ToList().Find(x => ((ObjectiveItem)x).Objective.id == oObjective.id); });

                                if (oListItem == null)
                                {
                                    itemscontrolMain.Dispatcher.Invoke(delegate
                                    {
                                        if ((imageCampMode.Tag.ToString() == "true" && oObjective.points == 5) || imageCampMode.Tag.ToString() != "true")
                                        {
                                            oListItem = new ObjectiveItem(oObjective, m_oLstObjectives, oMatchInput, LOGWRITER);
                                            itemscontrolMain.Items.Add(oListItem);
                                        }
                                    });
                                }
                                else
                                {
                                    itemscontrolMain.Dispatcher.Invoke(delegate
                                    {
                                        oListItem.Update(oObjective);
                                    });
                                }
                            }
                        }
                    }
                    else
                    {
                        itemscontrolMain.Dispatcher.Invoke(delegate
                        {
                            if (m_eCurrentDisplay == CurrentDisplay.Timer)
                            {
                                itemscontrolMain.Items.Clear();
                            }
                        });

                        labelMatchupMapTitle.Dispatcher.Invoke(delegate { labelMatchupMapTitle.Content = "No Word vs. World Map"; });

                        imageBloodlustColor.Dispatcher.Invoke(delegate { imageBloodlustColor.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + @"\Resources\Icons\bloodlust_neutral.png")); });
                        labelBloodlustStackCount.Dispatcher.Invoke(delegate { labelBloodlustStackCount.Content = string.Empty; });
                    }

                    TriggerLoadingIndicator();
                    Thread.Sleep(10000);
                }
            }
            catch (ThreadAbortException)
            {
                //Nothing to do here
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Liefert den Blutlust.status der aktuellen map
        /// </summary>
        /// <param name="m_oCurrentMap"></param>
        private string GetBloodlustByMap(Model.API.match oMatch)
        {
            string cRetVal = string.Empty;
            string cFile = string.Empty;
            string cColorId = string.Empty;
            short nWorldID;

            PropertyInfo oPropertyInfo;

            Dictionary<short, string> oDictWorldidsToColors;

            try
            {
                cColorId = string.Format("{0}_owner_id", m_oCurrentMap.Color.ToLower());

                oPropertyInfo = typeof(Model.API.bloodlust).GetProperties().ToList().Find(y => y.Name == cColorId);

                if (oPropertyInfo != null)
                {
                    oDictWorldidsToColors = new Dictionary<short, string>();
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 0).map_owner_id, "red");
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 1).map_owner_id, "blue");
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 2).map_owner_id, "green");

                    nWorldID = Convert.ToInt16(oPropertyInfo.GetValue(oMatch.bloodlust).ToString());

                    cFile = string.Format(@"\Resources\Icons\bloodlust_{0}.png", oDictWorldidsToColors[nWorldID]);

                    cRetVal = Environment.CurrentDirectory + cFile;
                }
                else
                {
                    //Ewige
                    cRetVal = Environment.CurrentDirectory + @"\Resources\Icons\bloodlust_neutral.png";
                }
            }
            catch (Exception oEx)
            {
                cRetVal = cRetVal = Environment.CurrentDirectory + @"\Resources\Icons\bloodlust_neutral.png";
            }
            return cRetVal;
        }

        private void RestartMatchThread(Model.API.matches_match oMatch)
        {
            StopMatchThread();
            StartMatchThread(oMatch);
            LOGWRITER.RequestStop();
        }

        /// <summary>
        /// Stopt den Matchup-provider thread
        /// </summary>
        private void StopMatchThread()
        {
            this.Dispatcher.Invoke(delegate
            {
                itemscontrolMain.Items.Clear();
            });

            TriggerLoadingIndicator(Visibility.Hidden);

            if (m_oThreadMatchProvider != null)
                m_oThreadMatchProvider.Abort();
        }

        /// <summary>
        /// Startet den Matchup Thread
        /// </summary>
        /// <param name="oMatch"></param>
        private void StartMatchThread(Model.API.matches_match oMatch)
        {
            m_oThreadMatchProvider = new Thread(new ParameterizedThreadStart(MatchProvider));
            m_oThreadMatchProvider.Start(oMatch);
        }

        /// <summary>
        /// Lädt ein Match
        /// </summary>
        /// <param name="cMatchID"></param>
        /// <returns></returns>
        private Model.API.match GetMatch(string cMatchID)
        {
            Model.API.match oRetVal = null;
            string cJson;

            try
            {
                cJson = new WebClient().DownloadString(@"http://gw2stats.net/api/objectives.json?type=match&id=" + cMatchID);
                if (!string.IsNullOrEmpty(cJson))
                {
                    oRetVal = JsonConvert.DeserializeObject<Model.API.match>(cJson);
                }

                foreach (Model.API.map oMap in oRetVal.maps)
                {
                    oMap.objectives_list = (List<Model.API.objective>)oMap.objectives.Select(x => x.Value).ToList<Model.API.objective>();
                }
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
                oRetVal = null;
            }
            return oRetVal;
        }

        /// <summary>
        /// Closed event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            ApplicationShutdown();
        }

    }
}
