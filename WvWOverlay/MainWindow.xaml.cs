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
        private Model.XML.Region m_oSelectedRegion;


        public MainWindow()
        {
            InitializeComponent();
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
                            foreach (Model.API.match oMatch in oMatchlist.region[m_oSelectedRegion.Slug])
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

                MessageBox.Show(oArgs.Match.match_id);
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

                itemscontrolMain.Items.Clear();

                oLstRegions = ConfigurationParser.GetRegions();

                foreach (Model.XML.Region oRegion in oLstRegions)
                {
                    RegionItem oItem = new RegionItem(oRegion);
                    oItem.RegionSelected += new EventHandler(On_RegionSelected);
                    itemscontrolMain.Items.Add(oItem);
                }

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
        private void imageClose_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.Close();
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
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                TriggerLoadingIndicator();

                InitializeRegionSelection();
                m_oLstProfessions = ConfigurationParser.GetProfessions();

                TriggerLoadingIndicator();
            }
            catch (Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void imageSelectMatch_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

    }
}
