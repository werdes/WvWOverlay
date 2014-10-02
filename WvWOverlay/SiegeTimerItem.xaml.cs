using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    /// Interaktionslogik für SiegeTimerItem.xaml
    /// </summary>
    public partial class SiegeTimerItem : UserControl
    {
        public event EventHandler RemoveSiegeTimer;
        public event EventHandler TimerFinished;

        private bool m_bRunEffect;

        public Model.SiegeTimer SiegeTimer;
        public Model.API.matches_match Match;
        private LogWriter m_oLogWriter;

        public SiegeTimerItem(Model.SiegeTimer oSiegeTimer, LogWriter oLogWriter, Model.API.matches_match oMatch)
        {
            TimeSpan oTimespanToGo;

            InitializeComponent();
            SiegeTimer = oSiegeTimer;
            m_oLogWriter = oLogWriter;
            Match = oMatch;

            labelObjectiveMapName.Content = SiegeTimer.Map.Identifier;
            labelObjectiveName.Content = SiegeTimer.XMLObjective.Name;
            imageObjectiveType.Source = new BitmapImage(this.GetIconUri(SiegeTimer.XMLObjective, SiegeTimer.APIObjective.current_owner.color));
            
            oTimespanToGo = SiegeTimer.End - DateTime.Now;

            Countdown((int)oTimespanToGo.TotalSeconds , TimeSpan.FromSeconds(1), cur => labelTimer.Content = string.Format("{0:%m}:{0:ss}", new TimeSpan(0, 0, cur)));       
        }


        /// <summary>
        /// Dispatch Timer 
        /// > Countdown Tick
        /// </summary>
        /// <param name="count"></param>
        /// <param name="interval"></param>
        /// <param name="ts"></param>
        private void Countdown(int count, TimeSpan interval, Action<int> ts)
        {
            SiegeTimer.Timer = new System.Windows.Threading.DispatcherTimer();
            SiegeTimer.Timer.Interval = interval;
            SiegeTimer.Timer.Tick += (_, a) =>
            {
                if (count-- <= 0)
                {
                    SiegeTimer.Timer.Stop();
                    labelTimer.Content = "";

                    EventExtensions.RaiseEvent(TimerFinished, this, null);
                    SetEffect();
                }
                else
                {
                    ts(count);
                }
            };
            ts(count);
            SiegeTimer.Timer.Start();
        }

        private void imageRemove_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_bRunEffect = false;
            if(SiegeTimer.Timer != null)
            {
                SiegeTimer.Timer.Stop();
            }

            EventExtensions.RaiseEvent(RemoveSiegeTimer, this, null);
        }

        /// <summary>
        /// Setzt den Blinkeffekt
        /// </summary>
        private void SetEffect()
        {
            BackgroundWorker oWorker = new BackgroundWorker();
            m_bRunEffect = true;

            oWorker.DoWork += delegate
            {
                while (m_bRunEffect)
                {
                    Thread.Sleep(1000);
                    rectangleEffect.Dispatcher.Invoke(delegate { rectangleEffect.Visibility = System.Windows.Visibility.Visible; });
                    Thread.Sleep(1000);
                    rectangleEffect.Dispatcher.Invoke(delegate { rectangleEffect.Visibility = System.Windows.Visibility.Hidden; });
                }
            };

            oWorker.RunWorkerAsync();
        }


        /// <summary>
        /// Returns the URI of the wanted icon
        /// </summary>
        /// <param name="nTick"></param>
        /// <param name="cColor"></param>
        /// <returns></returns>
        private Uri GetIconUri(Model.XML.Objective oObjective, string cColor)
        {
            Uri oRetVal = null;

            string cStartupPath = Environment.CurrentDirectory;
            string cFileName;

            try
            {
                //null entfernen
                if (string.IsNullOrWhiteSpace(cColor))
                {
                    cColor = Match.worlds.Find(x => x.world_id == SiegeTimer.APIObjective.current_owner.world_id).color.ToUpper();
                }

                if (oObjective.Type != Model.XML.Objective.ObjectiveType.Ruin)
                {
                    switch (cColor)
                    {
                        case "RED":
                            cFileName = string.Format("{0}_{1}.png",
                                oObjective.Type.ToString().ToLower(),
                                "red");

                            break;
                        case "GREEN":
                            cFileName = string.Format("{0}_{1}.png",
                                oObjective.Type.ToString().ToLower(),
                                "green");

                            break;
                        case "BLUE":
                            cFileName = string.Format("{0}_{1}.png",
                                oObjective.Type.ToString().ToLower(),
                                "blue");

                            break;
                        default:
                            cFileName = string.Format("{0}_{1}.png",
                                oObjective.Type.ToString().ToLower(),
                                "neutral");

                            break;
                    }

                    oRetVal = new Uri(cStartupPath + @"\Resources\Icons\" + cFileName);
                }
            }
            catch (Exception oEx)
            {
                oRetVal = null;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
            return oRetVal;
        }

        public void RestartTimer(Model.SiegeTimer oSiegeTimer)
        {
            TimeSpan oTimespanToGo;
            if(SiegeTimer.Timer != null)
            {
                SiegeTimer.Timer.Stop();
            }

            SiegeTimer = oSiegeTimer;

            oTimespanToGo = SiegeTimer.End - DateTime.Now;

            Countdown((int)oTimespanToGo.TotalSeconds, TimeSpan.FromSeconds(1), cur => labelTimer.Content = string.Format("{0:%m}:{0:ss}", new TimeSpan(0, 0, cur)));   
        }
    }
}
