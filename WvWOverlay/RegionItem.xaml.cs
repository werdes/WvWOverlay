using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.IO;

namespace WvWOverlay
{
    /// <summary>
    /// Interaktionslogik für RegionItem.xaml
    /// </summary>
    public partial class RegionItem : UserControl
    {
        public event EventHandler RegionSelected;
        private Model.XML.Region m_oRegion;


        /// <summary>
        /// Füllen der Infos
        /// </summary>
        /// <param name="oRegion"></param>
        public RegionItem(Model.XML.Region oRegion)
        {
            InitializeComponent();

            try
            {
                m_oRegion = oRegion;

                //Icon
                string cIcon = Environment.CurrentDirectory + '\\' + oRegion.Icon;
                imageIcon.Source = new BitmapImage(new Uri(cIcon));

                //Name
                labelRegion.Content = oRegion.Name;

                
            }
            catch(Exception oEx)
            {
                MessageBox.Show(oEx.ToString(), oEx.Message, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
        /// Feuern des Events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelRegion_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            EventExtensions.RaiseEvent(RegionSelected, this, new RegionSelectedEventArgs(m_oRegion));
        }
    }
}
