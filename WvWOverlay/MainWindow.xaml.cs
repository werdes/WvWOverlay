using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        private Thread m_oThreadMumbleProvider;
        public bool m_bRunMumbleProvider = true;

        private Thread m_oThreadMatchProvider;
        private bool m_bRunMatchProvider = true;

        private Model.XML.Map m_oCurrentMap;
        private Model.API.world m_oCurrentWorld;
        private Model.mumble_ind m_oMumblelinkInd;
        private Model.API.matches_match m_oCurrentMatch;

        private enum CurrentDisplay
        {
            RegionSelection,
            MatchSelection,
            Timer
        }

        private CurrentDisplay m_eCurrentDisplay;

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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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

                m_bRunMumbleProvider = true;
                m_oThreadMumbleProvider = new Thread(new ThreadStart(MumbleProvider));
                m_oThreadMumbleProvider.Start();


                m_bRunMatchProvider = true;

                StartMatchThread(oArgs.Match);

                m_eCurrentDisplay = CurrentDisplay.Timer;

                TriggerLoadingIndicator();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                });


            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                m_bRunMumbleProvider = false;
                m_bRunMatchProvider = false;

                if (m_oThreadMumbleProvider != null)
                    m_oThreadMumbleProvider.Abort();
                if (m_oThreadMatchProvider != null)
                    m_oThreadMatchProvider.Abort();


                this.Close();
                Application.Current.Shutdown();
                Environment.Exit(0);
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                TriggerLoadingIndicator();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void On_imageSelectMatch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopMatchThread();
            if (m_oThreadMumbleProvider != null)
                m_oThreadMumbleProvider.Abort();

            InitializeRegionSelection();

        }

        /// <summary>
        /// Führt den Mumblelink-Thread aus
        /// </summary>
        private void MumbleProvider()
        {
            MumbleLink oMumbleLink;
            MumbleLink.Coordinate oCoordinate;

            List<Model.XML.Map> oLstMaps;

            Model.mumble_ind oInd;
            Model.XML.Profession oProfession;

            try
            {
                oMumbleLink = new MumbleLink();
                oLstMaps = ConfigurationParser.GetMaps();

                while (m_bRunMumbleProvider)
                {
                    oCoordinate = oMumbleLink.GetCoordinates();

                    if (!string.IsNullOrWhiteSpace(oCoordinate.ind))
                    {
                        oInd = JsonConvert.DeserializeObject<Model.mumble_ind>(oCoordinate.ind);
                        m_oMumblelinkInd = oInd;

                        oProfession = m_oLstProfessions.Find(x => x.Id == oInd.profession);

                        if (oProfession != null)
                        {
                            //Invoke for multithreading-access
                            labelPlayerCharacter.Dispatcher.Invoke(delegate { labelPlayerCharacter.Content = string.Format("{0} - {1}", oInd.name, oProfession.Name); });
                        }
                        m_oCurrentMap = oLstMaps.Find(x => x.MapID == oInd.map_id);
                    }
                    else
                    {
                        m_oCurrentMap = null;

                        labelPlayerCharacter.Dispatcher.Invoke(delegate { labelPlayerCharacter.Content = string.Empty; });
                        if (m_eCurrentDisplay == CurrentDisplay.Timer)
                            itemscontrolMain.Dispatcher.Invoke(delegate { itemscontrolMain.Items.Clear(); });
                    }

                    Thread.Sleep(2000);
                }
            }
            catch
            {

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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
                        nCountBloodlustStacks += oMatch.bloodlust.blue_owner_id == m_oCurrentWorld.world_id ? 1 : 0;

                        imageBloodlustColor.Dispatcher.Invoke(delegate
                        {
                            imageBloodlustColor.Source = new BitmapImage(new Uri(GetBloodlustByMap(oMatch)));
                            labelBloodlustStackCount.Content = string.Format("{0} Stacks",
                                nCountBloodlustStacks);
                        });



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
                                            oListItem = new ObjectiveItem(oObjective, m_oLstObjectives);
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
                    }

                    TriggerLoadingIndicator();
                    Thread.Sleep(5000);
                }
            }
            catch (ThreadAbortException)
            {
                //Nothing to do here
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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

            PropertyInfo oPropertyInfo;

            Dictionary<short, string> oDictWorldidsToColors;

            try
            {
                cColorId = string.Format("{0}_owner_id", m_oCurrentMap.Color.ToLower());

                oPropertyInfo = typeof(Model.API.bloodlust).GetProperties().ToList().Find(y => y.Name == cColorId);

                if(oPropertyInfo != null)
                {
                    oDictWorldidsToColors = new Dictionary<short, string>();
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 0).map_owner_id, "red");
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 1).map_owner_id, "blue");
                    oDictWorldidsToColors.Add(oMatch.maps.Find(x => x.map_id == 2).map_owner_id, "green");


                    cFile = string.Format(@"\Resources\Icons\bloodlust_{0}.png", oDictWorldidsToColors[Convert.ToInt16(oPropertyInfo.GetValue(oMatch.bloodlust).ToString())]);

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
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
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
            });

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
                    oMap.objectives_list = (List<Model.API.objective>)oMap.objectives.OrderByDescending(x => x.Value.points).ThenBy(x => x.Value.name).Select(x => x.Value).ToList<Model.API.objective>();
                }
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return oRetVal;
        }

    }
}
