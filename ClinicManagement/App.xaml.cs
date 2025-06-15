using ClinicManagement.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;

namespace ClinicManagement
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize shared ViewModels
            Current.Resources["StatisticsViewModel"] = new StatisticsViewModel();
        }
    }

}
