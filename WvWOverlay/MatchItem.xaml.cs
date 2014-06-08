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
        public Model.API.matches_match Match;


        public event EventHandler MatchSelected;

        public MatchItem(Model.API.matches_match oMatch)
        {
            InitializeComponent();
            Match = oMatch;

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
            Label oLabel = (Label)sender;

            oLabel.Opacity = 1D;
            oLabel.Margin = new Thickness(oLabel.Margin.Left - 2, oLabel.Margin.Top, oLabel.Margin.Right, oLabel.Margin.Bottom);
            
        }

        /// <summary>
        /// Hover Leave Background
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rectangleBack_MouseLeave(object sender, MouseEventArgs e)
        {
            rectangleBack.Fill.Opacity = 1D;
            Label oLabel = (Label)sender;

            oLabel.Opacity = 0.8D;
            oLabel.Margin = new Thickness(oLabel.Margin.Left + 2, oLabel.Margin.Top, oLabel.Margin.Right, oLabel.Margin.Bottom);
        }


        /// <summary>
        /// Selecting a Match
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void On_MatchSelectionClick(object sender, MouseButtonEventArgs e)
        {
            Model.API.world oWorld = Match.worlds.Find(x => x.color.ToLower() == ((Label)sender).Tag.ToString());
            EventExtensions.RaiseEvent(MatchSelected, this, new MatchSelectedEventArgs(Match, oWorld));
        }
    }
}
