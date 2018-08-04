using Library.Wpf.Mvvm;
using System.ComponentModel;
using System.Windows;

namespace Microscope
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MicroscopeView : Window
    {
        public MicroscopeView()
        {
            InitializeComponent();
            Closing += MicroscopeView_Closing;
        }

        //Run OnClosing in IClosing implementing ViewModels
        private void MicroscopeView_Closing(object sender, CancelEventArgs e)
        {
            IClosing context = DataContext as IClosing;
            if (context != null)
            {
                e.Cancel = !context.OnClosing();
            }
        }



    }
}
