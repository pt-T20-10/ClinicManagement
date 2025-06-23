using ClinicManagement.ViewModels;
using QuestPDF.Infrastructure;
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
            QuestPDF.Settings.License = LicenseType.Community;
            // Initialize shared ViewModels
            // Initialize all shared ViewModels
            Current.Resources["MainVM"] = new MainViewModel();
            Current.Resources["LoginVM"] = new LoginViewModel();
            Current.Resources["PatientVM"] = new PatientViewModel();
            Current.Resources["DoctorVM"] = new DoctorViewModel();
            Current.Resources["AppointmentVM"] = new AppointmentViewModel();
            Current.Resources["StockMedicineVM"] = new StockMedicineViewModel();
            Current.Resources["AddPatientVM"] = new AddPatientViewModel();
            Current.Resources["AddDoctorVM"] = new AddDoctorWindowViewModel();
            Current.Resources["InvoiceVM"] = new InvoiceViewModel();
            Current.Resources["MedicineSellVM"] = new MedicineSellViewModel();
            Current.Resources["StatisticsVM"] = new StatisticsViewModel();
            Current.Resources["SettingVM"] = new SettingViewModel();
        }
    }

}
