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
    /// Interaktionslogik für ObjectiveItem.xaml
    /// </summary>
    public partial class ObjectiveItem : UserControl
    {
        public event EventHandler Click;

        public ObjectiveItem()
        {
            InitializeComponent();
        }

        private void labelObjectiveName_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            EventExtensions.RaiseEvent(Click, this, null);
        }
    }
}
