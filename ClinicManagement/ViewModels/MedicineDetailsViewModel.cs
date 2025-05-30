using ClinicManagement.Models;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    public class MedicineDetailsViewModel : BaseViewModel
    {
        private readonly Window _window;
        private Medicine _medicine;
        private ObservableCollection<Medicine.StockInWithRemaining> _detailedStockList;

        public Medicine Medicine
        {
            get => _medicine;
            set
            {
                _medicine = value;
                OnPropertyChanged();
                UpdateDetailsList();
            }
        }

        public ObservableCollection<Medicine.StockInWithRemaining> DetailedStockList
        {
            get => _detailedStockList;
            set
            {
                _detailedStockList = value;
                OnPropertyChanged();
            }
        }

        public string DialogTitle => Medicine != null
            ? $"Chi tiết các lô thuốc: {Medicine.Name}"
            : "Chi tiết lô thuốc";

        public ICommand CloseCommand { get; }

        public MedicineDetailsViewModel(Medicine medicine, Window window)
        {
            _window = window;
            Medicine = medicine;
            
            CloseCommand = new RelayCommand<object>(
                parameter => _window.Close(),
                parameter => true
            );
        }

        private void UpdateDetailsList()
        {
            try
            {
                if (Medicine != null)
                {
                    var details = Medicine.GetDetailedStock();
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>(details);
                    OnPropertyChanged(nameof(DialogTitle));
                }
                else
                {
                    DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải chi tiết lô thuốc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                DetailedStockList = new ObservableCollection<Medicine.StockInWithRemaining>();
            }
        }
    }
}
