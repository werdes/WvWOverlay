using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
        private void On_HeaderDrag_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
            
            if(oOpacityProperty != null)
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
    }
}
