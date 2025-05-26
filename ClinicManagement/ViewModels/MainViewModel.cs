
using ClinicManagement.SubWindow;
using ClinicManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static MaterialDesignThemes.Wpf.Theme;

namespace ClinicManagement.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        //private ObservableCollection<Stock> _StockList;
        //public ObservableCollection<Stock> StockList
        //{
        //    get => _StockList;
        //    set
        //    {
        //        _StockList = value; OnPropertyChanged();
        //    }
        //}


        public bool Isloaded = false;
        public string _TotalStock;
        public string TotalStock { get => _TotalStock; set { _TotalStock = value; OnPropertyChanged();  } }
        public ICommand LoadedWindowCommand { get; set; }
        public ICommand AddPatientCommand { get; set; } 
        public ICommand AddAppointmentCommand { get; set; }


        // mọi thứ xử lý sẽ nằm trong này
        public MainViewModel()
        {
            LoadedWindowCommand = new RelayCommand<Window>(

                 (p) =>
                 {
                     Isloaded = true;
                     if (p == null)
                         return;

                     p.Hide();
                     LoginWindow loginWindow = new LoginWindow();
                     loginWindow.ShowDialog();
                     if (loginWindow.DataContext == null)
                         return;

                     var loginVM = loginWindow.DataContext as LoginViewModel;
                     if (loginVM.IsLogin)
                     {
                         p.Show();
                        ;

                     }
                     else
                     {
                         p.Close();
                     }
                 },
                 (p) => true
            );
            AddPatientCommand = new RelayCommand<Window>(
                (p) =>
                {
                    AddPatientWindow addPatientWindow = new AddPatientWindow();
                    addPatientWindow.ShowDialog();
                },
                (p) => true
            );
            AddAppointmentCommand = new RelayCommand<System.Windows.Controls.TabControl>(
                (tabControl) =>
                {
                    if (tabControl != null)
                    {
                        foreach (var item in tabControl.Items)
                        {
                            if (item is TabItem tabItem && tabItem.Name == "AppointmentTab")
                            {
                                tabControl.SelectedItem = tabItem;
                                break;
                            }
                        }
                    }       
                },
        (p) => true
    );
        }
        //void LoadListStock()
        //{
        //    StockList = new ObservableCollection<Stock>();
        //    var objectList = DataProvider.Instance.Context.Objects.ToList();
        //    int i = 1;
        //    int totalStock = 0;
        //    foreach ( var item in objectList)
        //    {
        //        var inputList = DataProvider.Instance.Context.InputInfos.Where(p => p.IdObject == item.Id);
        //        var outputList = DataProvider.Instance.Context.OutputInfos.Where(p => p.IdObject == item.Id);

        //        int sumInput = 0;
        //        int sumOutput = 0;
             

        //        if (inputList != null && inputList.Count() > 0)
        //        {
        //            sumInput = (int)inputList.Sum(p=>p.Count);
        //        }
        //        if (outputList != null && outputList.Count() > 0)
        //        {
        //            sumOutput = (int)outputList.Sum(p => p.Count);
        //        }
        //        Stock stock = new Stock();
        //        stock.STT = i;
        //        stock.Count = sumInput - sumOutput;
        //        stock.Object = item;
        //        totalStock += stock.Count;
        //        StockList.Add(stock);
        //        i++;
        //    }
        //    TotalStock = totalStock.ToString();
     
            
        //}
    }
}       
