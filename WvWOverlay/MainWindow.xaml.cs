using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
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

        private Thread m_oThreadLowFrequencyProvider;
        public bool m_bRunLowFrequencyProvider = true;

        private MumbleLink.Coordinate m_oCurrentCoordinate;
        private Model.mumble_identity m_oCurrentIdentity;
        private Thread m_oThreadPlayerDisplayProvider;
        private bool m_bRunPlayerDisplayProvider = true;

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

        private enum CurrentDisplayMode
        {
            List,
            Map,
            CampsOnly
        }

        private CurrentDisplay m_eCurrentDisplay;
        private CurrentDisplayMode m_eCurrentDisplayMode;


        private Teamspeak.ClientWrapper m_oTsClientWrapper = null;
        private Teamspeak.ClientWrapper TS_CLIENT_WRAPPER
        {
            get
            {
                if (m_oTsClientWrapper == null)
                {
                    m_oTsClientWrapper = new Teamspeak.ClientWrapper(LOGWRITER);
                    m_oTsClientWrapper.TalkStatusChanged += new EventHandler(On_TeamspeakTalkStatusChanged);
                }
                return m_oTsClientWrapper;
            }
        }

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
            m_eCurrentDisplayMode = CurrentDisplayMode.Map;
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
                            LOGWRITER.WriteMessage("Region not found", LogWriter.MESSAGE_TYPE.Error);
                        }
                    }
                }
                else
                {
                    LOGWRITER.WriteMessage("No Matchlist", LogWriter.MESSAGE_TYPE.Error);
                }
            }
            catch (ThreadAbortException)
            {
                //nothing to do here
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

                m_bRunPlayerDisplayProvider = true;
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
        /// Initialisiert die TS-Verbindung
        /// </summary>
        private void InitializeTeamspeakQueryConnection()
        {
            try
            {
                TS_CLIENT_WRAPPER.Connect();

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
            m_bRunPlayerDisplayProvider = false;
            m_bRunLowFrequencyProvider = false;

            if (m_oThreadMumbleFileProvider != null)
                m_oThreadMumbleFileProvider.Abort();
            if (m_oThreadMatchProvider != null)
                m_oThreadMatchProvider.Abort();
            if (m_oThreadPlayerDisplayProvider != null)
                m_oThreadPlayerDisplayProvider.Abort();
            if (m_oThreadLowFrequencyProvider != null)
                m_oThreadLowFrequencyProvider.Abort();



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

                InitializeTeamspeakQueryConnection();

                //Start Low Frequency Provider
                //m_bRunLowFrequencyProvider = true;
                //m_oThreadLowFrequencyProvider = new Thread(new ThreadStart(LowFrequencyProvider));
                //m_oThreadLowFrequencyProvider.Start();

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
            catch (ThreadAbortException)
            {
                //nothing to do here
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Very Low Frequency
        ///  > 5m
        /// </summary>
        public void LowFrequencyProvider()
        {
            try
            {
                while (m_bRunLowFrequencyProvider)
                {
                    Thread.Sleep(2 * 60 * 1000);

                }
            }
            catch (ThreadAbortException)
            {
                //nothing to do here
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
            Ping oPingSender;
            PingReply oPingResult;

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

                //Reload TS Identity
                TS_CLIENT_WRAPPER.ReloadIdentity();

                while (m_bRunPlayerDisplayProvider)
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

                    //Ping

                    oPingSender = new Ping();
                    oPingResult = oPingSender.Send("8.8.8.8");
                    if (oPingResult.Status == IPStatus.Success)
                    {
                        labelPing.Dispatcher.Invoke(delegate
                        {
                            labelPing.Content = (oPingResult.RoundtripTime < 1000 ? oPingResult.RoundtripTime : 999) + "ms";
                            if (oPingResult.RoundtripTime <= 125)
                            {
                                labelPing.Foreground = System.Windows.Media.Brushes.GreenYellow;
                            }
                            else if (oPingResult.RoundtripTime <= 300)
                            {
                                labelPing.Foreground = System.Windows.Media.Brushes.Orange;
                            }
                            else
                            {
                                labelPing.Foreground = System.Windows.Media.Brushes.Red;
                            }
                        });
                    }

                    try
                    {

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

                    }
                    catch (Exception)
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
        private void On_imageDisplayMode_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (m_eCurrentDisplayMode == CurrentDisplayMode.List)
                {
                    //Camp only
                    m_eCurrentDisplayMode = CurrentDisplayMode.CampsOnly;

                    //Image
                    imageDisplayMode.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Resources\\Icons\\camp_white.png"));

                    if (m_eCurrentDisplay == CurrentDisplay.Timer)
                        RestartMatchThread(m_oCurrentMatch);
                }
                else if (m_eCurrentDisplayMode == CurrentDisplayMode.CampsOnly)
                {
                    //Map
                    m_eCurrentDisplayMode = CurrentDisplayMode.Map;

                    imageDisplayMode.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Resources\\Icons\\map.png"));

                    if (m_eCurrentDisplay == CurrentDisplay.Timer)
                        RestartMatchThread(m_oCurrentMatch);
                }
                else if (m_eCurrentDisplayMode == CurrentDisplayMode.Map)
                {
                    //List
                    m_eCurrentDisplayMode = CurrentDisplayMode.List;

                    HideMap();
                    imageDisplayMode.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Resources\\Icons\\keep_white.png"));

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
        /// Sets the Map to hidden
        /// </summary>
        private void HideMap()
        {
            try
            {
                this.Dispatcher.Invoke(delegate
                {
                    rectangleMapBorder.Visibility = System.Windows.Visibility.Hidden;
                    imageMapImage.Visibility = System.Windows.Visibility.Hidden;

                    canvasMapObjectives.Visibility = System.Windows.Visibility.Hidden;
                    canvasMapObjectives.Children.Clear();
                });
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Sets the Map to visible
        /// </summary>
        private void ShowMap()
        {
            try
            {
                this.Dispatcher.Invoke(delegate
                {
                    rectangleMapBorder.Visibility = System.Windows.Visibility.Visible;
                    imageMapImage.Visibility = System.Windows.Visibility.Visible;
                    canvasMapObjectives.Visibility = System.Windows.Visibility.Visible;

                    if (m_oCurrentMap != null)
                    {
                        if (m_oCurrentMap.Gw2StatsID == 3)
                        {
                            //EBG
                            imageMapImage.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Resources\\Maps\\EternalBattlegrounds.jpg"));
                            rectangleMapBorder.Height = 320;
                        }
                        else
                        {
                            //Border
                            imageMapImage.Source = new BitmapImage(new Uri(Environment.CurrentDirectory + "\\Resources\\Maps\\Borderland.jpg"));
                            rectangleMapBorder.Height = 421;
                        }
                    }
                    else
                    {
                        rectangleMapBorder.Visibility = System.Windows.Visibility.Hidden;
                        imageMapImage.Visibility = System.Windows.Visibility.Hidden;
                        canvasMapObjectives.Visibility = System.Windows.Visibility.Hidden;
                    }
                });
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
            Model.XML.Objective oObjectiveXML = null;

            ObjectiveItem oListItem = null;
            MapObjectiveItem oMapItem = null;
            List<Model.XML.Objective.ObjectiveType> oLstDisplayTypes;

            DateTime oNow;

            string cHeaderLine = string.Empty;
            string cIconLink = string.Empty;

            int nCountBloodlustStacks = 0;
            int nCountTries = 0;

            try
            {
                oMatchInput = (Model.API.matches_match)oMatchObj;

                while (m_oCurrentMap == null && nCountTries < 10)
                {
                    //Delay -> wait for the mumble provider to load the map
                    Thread.Sleep(250);
                    nCountTries++;

                }
                nCountTries = 0;


                while (m_bRunMatchProvider)
                {
                    TriggerLoadingIndicator();

                    nCountBloodlustStacks = 0;
                    cHeaderLine = string.Empty;

                    oNow = DateTime.Now;


                    oMatch = this.GetMatch(oMatchInput.match_id);

                    if (oMatch != null && m_oCurrentMap != null)
                    {
                        //Show Map on Map Mode
                        if (m_eCurrentDisplayMode == CurrentDisplayMode.Map)
                        {
                            ShowMap();
                        }

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
                            labelScoreGreen.Content = oMatch.maps.Sum(x => x.objectives.Select(o => o.Value).ToList().FindAll(y => y.current_owner.world_id == oMatchInput.worlds.Find(z => z.color.ToUpper() == "GREEN").world_id).Sum(y => y.points));

                            //Blue
                            labelScoreBlue.Content = oMatch.maps.Sum(x => x.objectives.Select(o => o.Value).ToList().FindAll(y => y.current_owner.world_id == oMatchInput.worlds.Find(z => z.color.ToUpper() == "BLUE").world_id).Sum(y => y.points));

                            //Red
                            labelScoreRed.Content = oMatch.maps.Sum(x => x.objectives.Select(o => o.Value).ToList().FindAll(y => y.current_owner.world_id == oMatchInput.worlds.Find(z => z.color.ToUpper() == "RED").world_id).Sum(y => y.points));

                        });

                        oMap.objectives_list = (List<Model.API.objective>)oMap.objectives_list.Where(x => x.points > 0).OrderByDescending(x => x.points).ThenBy(x => m_oLstObjectives.Find(y => y.Id == x.id).Name).ToList();

                        oLstDisplayTypes = GetObjectiveTypesForView();

                        foreach (Model.API.objective oObjective in oMap.objectives_list)
                        {
                            oObjective.SetTimes(oNow);

                            if (oObjective.points > 0)
                            {
                                if (m_eCurrentDisplayMode == CurrentDisplayMode.Map)
                                {
                                    oMapItem = null;
                                    canvasMapObjectives.Dispatcher.Invoke(delegate
                                    {
                                        foreach (UIElement oCanvasChild in canvasMapObjectives.Children)
                                        {
                                            if (((MapObjectiveItem)oCanvasChild).Objective.id == oObjective.id)
                                            {
                                                oMapItem = (MapObjectiveItem)oCanvasChild;
                                                break;
                                            }
                                        }
                                    });

                                    if (oMapItem == null)
                                    {
                                        //Get XML Objective for Display Check
                                        oObjectiveXML = m_oLstObjectives.Find(x => x.Id == oObjective.id);

                                        //Show Objective type?
                                        if (oLstDisplayTypes.Contains(oObjectiveXML.Type))
                                        {
                                            //Map View
                                            canvasMapObjectives.Dispatcher.Invoke(delegate
                                            {
                                                oMapItem = new MapObjectiveItem(oObjective, m_oLstObjectives, oMatchInput, LOGWRITER);
                                                oMapItem.Click += new EventHandler(On_ObjectiveItemDoubleClick);
                                                canvasMapObjectives.Children.Add(oMapItem);
                                                Canvas.SetLeft(oMapItem, oObjectiveXML.Coordinates.X);
                                                Canvas.SetTop(oMapItem, oObjectiveXML.Coordinates.Y);
                                            });
                                        }
                                    }
                                    else
                                    {
                                        //Map view
                                        canvasMapObjectives.Dispatcher.Invoke(delegate
                                        {
                                            oMapItem.Update(oObjective);
                                        });
                                    }
                                }
                                else
                                {

                                    itemscontrolMain.Dispatcher.Invoke(delegate { oListItem = (ObjectiveItem)itemscontrolMain.ItemContainerGenerator.Items.ToList().Find(x => ((ObjectiveItem)x).Objective.id == oObjective.id); });

                                    if (oListItem == null)
                                    {
                                        //Get XML Objective for Display Check
                                        oObjectiveXML = m_oLstObjectives.Find(x => x.Id == oObjective.id);

                                        //Show Objective type?
                                        if (oLstDisplayTypes.Contains(oObjectiveXML.Type))
                                        {
                                            //List view
                                            itemscontrolMain.Dispatcher.Invoke(delegate
                                            {
                                                oListItem = new ObjectiveItem(oObjective, m_oLstObjectives, oMatchInput, LOGWRITER);
                                                oListItem.Click += new EventHandler(On_ObjectiveItemDoubleClick);
                                                itemscontrolMain.Items.Add(oListItem);
                                            });
                                        }
                                    }
                                    else
                                    {
                                        //List view
                                        itemscontrolMain.Dispatcher.Invoke(delegate
                                        {
                                            oListItem.Update(oObjective);
                                        });
                                    }
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

                        labelMatchupMapTitle.Dispatcher.Invoke(delegate { labelMatchupMapTitle.Content = "No World vs. World Map"; });

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

        public void On_ObjectiveItemDoubleClick(object sender, EventArgs e)
        {
            ObjectiveItemDoubleclickEventArgs oArgs;
            string cText = string.Empty;
            try
            {
                oArgs = (ObjectiveItemDoubleclickEventArgs)e;

                cText += "[" + m_oLstObjectives.Find(x => x.Id == oArgs.Objective.id).Name + "] Derzeitiger Eigner: " + oArgs.Objective.current_owner.name.Split('[')[0].Trim();

                if (oArgs.Ri_Remaining.TotalMilliseconds > 0)
                {
                    cText += " | Buff: " + oArgs.Ri_Remaining.Minutes + ":" + oArgs.Ri_Remaining.ToString("ss");
                }
                else
                {
                    cText += " | Buff: Keiner";
                }

                cText += " | Gehalten seit: ";
                if (oArgs.Time_Held.Days > 0)
                    cText += oArgs.Time_Held.Days.ToString() + " Tage, ";
                if (oArgs.Time_Held.Hours > 0)
                    cText += oArgs.Time_Held.Hours.ToString() + " Stunden, ";
                if (oArgs.Time_Held.Minutes > 0)
                    cText += oArgs.Time_Held.Minutes.ToString() + " Minuten, ";

                cText += oArgs.Time_Held.Seconds.ToString() + " Sekunden";

                if (oArgs.Objective.current_guild != null && !string.IsNullOrWhiteSpace(oArgs.Objective.current_guild.id))
                    cText += " | Geclaimed von: " + oArgs.Objective.current_guild.name;

                Clipboard.SetText(cText);
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Liefert die anzuzeigenden Typen
        /// </summary>
        /// <returns></returns>
        public List<Model.XML.Objective.ObjectiveType> GetObjectiveTypesForView()
        {
            List<Model.XML.Objective.ObjectiveType> oLstRetVal = new List<Model.XML.Objective.ObjectiveType>();

            switch (m_eCurrentDisplayMode)
            {
                case CurrentDisplayMode.CampsOnly:
                    oLstRetVal.Add(Model.XML.Objective.ObjectiveType.Camp);
                    break;
                case CurrentDisplayMode.List:
                case CurrentDisplayMode.Map:
                    oLstRetVal.Add(Model.XML.Objective.ObjectiveType.Camp);
                    oLstRetVal.Add(Model.XML.Objective.ObjectiveType.Castle);
                    oLstRetVal.Add(Model.XML.Objective.ObjectiveType.Tower);
                    oLstRetVal.Add(Model.XML.Objective.ObjectiveType.Keep);
                    break;
            }

            return oLstRetVal;
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
        }

        /// <summary>
        /// Stopt den Matchup-provider thread
        /// </summary>
        private void StopMatchThread()
        {
            this.Dispatcher.Invoke(delegate
            {
                itemscontrolMain.Items.Clear();
                canvasMapObjectives.Children.Clear();
            });

            HideMap();

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
            string cDownloadUrl;

            try
            {
                //Console.WriteLine("GetMatch" + cMatchID);
                if (m_oCurrentMap != null)
                {
                    //cDownloadUrl = @"http://gw2stats.net/api/objectives.json?type=match&id=" + cMatchID;
                    cDownloadUrl = string.Format("http://81.20.136.134:8080/wvw_overlay_backend/match_details.php?MATCH_ID={0}&MAP_ID={1}&PASSWORD=aosfdz0981hoah89ashd",
                        cMatchID,
                        m_oCurrentMap.Gw2StatsID);

                    cJson = new WebClient().DownloadString(cDownloadUrl);

                    if (!string.IsNullOrEmpty(cJson))
                    {
                        oRetVal = JsonConvert.DeserializeObject<Model.API.match>(cJson);
                    }

                    foreach (Model.API.map oMap in oRetVal.maps)
                    {
                        oMap.objectives_list = (List<Model.API.objective>)oMap.objectives.Select(x => x.Value).ToList<Model.API.objective>();
                    }
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
        private void On_Window_Closed(object sender, EventArgs e)
        {
            ApplicationShutdown();
        }

        private void On_TeamspeakTalkStatusChanged(object sender, EventArgs e)
        {
            Teamspeak.ClientWrapper oWrapper = (Teamspeak.ClientWrapper)sender;
            BitmapSource oBitmapsource = null;

            try
            {
                if (oWrapper.Status == Teamspeak.ConnectionState.Connected)
                {
                    switch (oWrapper.TalkingStatus)
                    {
                        case Teamspeak.TalkingState.MicrophoneMuted:
                            oBitmapsource = new BitmapImage(new Uri(Environment.CurrentDirectory + "/Resources/Icons/microphone_muted.png"));
                            break;
                        case Teamspeak.TalkingState.Quiet:
                            oBitmapsource = new BitmapImage(new Uri(Environment.CurrentDirectory + "/Resources/Icons/quiet.png"));
                            break;
                        case Teamspeak.TalkingState.Talking:
                            oBitmapsource = new BitmapImage(new Uri(Environment.CurrentDirectory + "/Resources/Icons/talking.png"));
                            break;
                    }
                }
                else
                {
                    oBitmapsource = new BitmapImage(new Uri(Environment.CurrentDirectory + "/Resources/Icons/quiet.png"));
                }

                imageTeamspeakStatus.Dispatcher.Invoke(delegate
                {
                    imageTeamspeakStatus.Source = oBitmapsource;
                });
            }
            catch (Exception oEx)
            {
                LOGWRITER.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Reset TS Client
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_imageTeamspeakStatus_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            TS_CLIENT_WRAPPER.Disconnect();
            TS_CLIENT_WRAPPER.Connect();
        }
    }
}
