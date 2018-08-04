using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Microscope
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private MicroscopeViewModel _vm;
        private MicroscopeView _v;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            _vm = new MicroscopeViewModel();
            _v = new MicroscopeView() { DataContext = _vm };
            _v.Show();

        }


    }
}
