using System;
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
    /// Interaktionslogik für MatchItem.xaml
    /// </summary>
    public partial class MatchItem : UserControl
    {
        private Model.API.match m_oMatch;
        public event EventHandler MatchSelected;

        public MatchItem(Model.API.match oMatch)
        {
            InitializeComponent();
            m_oMatch = oMatch;

            labelBlueWorld.Content = oMatch.worlds.Find(x => x.color == "Blue").name;
            labelRedWorld.Content = oMatch.worlds.Find(x => x.color == "Red").name;
            labelGreenWorld.Content = oMatch.worlds.Find(x => x.color == "Green").name;
        }

        /// <summary>
        /// Hover Background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rectangleBack_MouseEnter(object sender, MouseEventArgs e)
        {
            rectangleBack.Fill.Opacity = 0.9D;
        }

        /// <summary>
        /// Hover Leave Background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rectangleBack_MouseLeave(object sender, MouseEventArgs e)
        {
            rectangleBack.Fill.Opacity = 1D;
        }


        /// <summary>
        /// Selecting a Match
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_MatchSelectionClick(object sender, MouseButtonEventArgs e)
        {
            EventExtensions.RaiseEvent(MatchSelected, this, new MatchSelectedEventArgs(m_oMatch));
        }
    }
}
