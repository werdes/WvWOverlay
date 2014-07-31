﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Interaktionslogik für MapObjective.xaml
    /// </summary>
    public partial class MapObjectiveItem : UserControl
    {
        public event EventHandler Click;

        private List<Model.XML.Objective> m_oLstObjectives;
        public Model.API.objective Objective;
        public System.Windows.Threading.DispatcherTimer m_oTimer;
        public Model.API.matches_match m_oMatch;
        public LogWriter m_oLogWriter;


        public MapObjectiveItem(Model.API.objective oObjective, List<Model.XML.Objective> oLstObjectives, Model.API.matches_match oMatch, LogWriter oLogWriter)
        {
            InitializeComponent();

            Objective = oObjective;
            m_oLstObjectives = oLstObjectives;
            m_oMatch = oMatch;
            m_oLogWriter = oLogWriter;
            
            Init();
        }

        /// <summary>
        /// Lädt die Controls
        /// </summary>
        private void Init()
        {
            Model.XML.Objective oObjectiveInList;

            try
            {
                oObjectiveInList = m_oLstObjectives.Find(x => x.Id == Objective.id);
                imageGuildClaim.Visibility = Objective.current_guild == null || string.IsNullOrWhiteSpace(Objective.current_guild.id) ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;

                if (oObjectiveInList != null)
                {
                    imageObjectiveType.Source = new BitmapImage(this.GetIconUri(oObjectiveInList, Objective.current_owner.color));
                    
                    labelTimeOwned.Content = GetTimeOwnedString(Objective);

                    if (Objective.ri_remaining.TotalMilliseconds > 0)
                    {
                        imageBlock.Visibility = System.Windows.Visibility.Visible;
                        Countdown((int)Objective.ri_remaining.TotalSeconds, TimeSpan.FromSeconds(1), cur => labelTimer.Content = string.Format("{0:%m}:{0:ss}", new TimeSpan(0, 0, cur)));
                    }
                }
            }
            catch(Exception oEx)
            {
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Update die Ansicht
        /// </summary>
        /// <param name="oObjective"></param>
        public void Update(Model.API.objective oObjective)
        {
            Model.XML.Objective oObjectiveInList;
            
            try
            {
                oObjectiveInList = m_oLstObjectives.Find(x => x.Id == Objective.id);

                if (oObjectiveInList != null)
                {
                    labelTimeOwned.Content = GetTimeOwnedString(oObjective);
                    imageGuildClaim.Visibility = oObjective.current_guild == null || string.IsNullOrWhiteSpace(oObjective.current_guild.id) ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;

                    if (Objective.current_owner.world_id != oObjective.current_owner.world_id)
                    {
                        Objective = oObjective;

                        imageObjectiveType.Source = new BitmapImage(this.GetIconUri(oObjectiveInList, Objective.current_owner.color));
                       
                       
                        if (Objective.ri_remaining.TotalMilliseconds > 0)
                        {
                            if(!string.IsNullOrWhiteSpace(labelTimer.Content.ToString()))
                            {
                                m_oTimer.Stop();
                            }
                            imageBlock.Visibility = System.Windows.Visibility.Visible;
                            Countdown((int)Objective.ri_remaining.TotalSeconds, TimeSpan.FromSeconds(1), cur => labelTimer.Content = string.Format("{0:%m}:{0:ss}", new TimeSpan(0, 0, cur)));
                        }
                    }
                }
            }
            catch(Exception oEx)
            {
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
        }

        /// <summary>
        /// Liefert den String
        /// </summary>
        /// <param name="oObjective"></param>
        /// <returns></returns>
        private string GetTimeOwnedString(Model.API.objective oObjective)
        {
            string cRetVal = string.Empty;

            if(oObjective.time_held.Days > 0)
            {
                cRetVal = oObjective.time_held.Days.ToString() + "d";
            }
            else if(oObjective.time_held.Hours > 0)
            {
                cRetVal = oObjective.time_held.Hours.ToString() + "h";
            }
            else if(oObjective.time_held.Minutes > 0)
            {
                cRetVal = oObjective.time_held.Minutes.ToString() + "m";
            }
            else
            {
                cRetVal = "<1m";
            }

            return cRetVal;
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
            m_oTimer = new System.Windows.Threading.DispatcherTimer();
            m_oTimer.Interval = interval;
            m_oTimer.Tick += (_, a) =>
            {
                if (count-- <= 0)
                {
                    m_oTimer.Stop();
                    labelTimer.Content = "";

                    imageBlock.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    ts(count);
                }
            };
            ts(count);
            m_oTimer.Start();
        }

        private void labelObjectiveName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EventExtensions.RaiseEvent(Click, this, null);
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
                if(string.IsNullOrWhiteSpace(cColor))
                {
                    cColor = m_oMatch.worlds.Find(x => x.world_id == Objective.current_owner.world_id).color.ToUpper();
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
            catch(Exception oEx)
            {
                oRetVal = null;
                m_oLogWriter.WriteMessage(oEx.ToString(), LogWriter.MESSAGE_TYPE.Error);
            }
            return oRetVal;
        }
    }
}
