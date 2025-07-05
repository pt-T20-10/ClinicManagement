using ClinicManagement.Models;
using ClinicManagement.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using ClosedXML.Excel;
using System.IO;
using ClinicManagement.SubWindow;

namespace ClinicManagement.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        #region Basic Properties

        public Func<double, string> CurrencyFormatter { get; set; }
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                // We'll handle loading in the filter commands instead of automatically loading
                // to prevent DbContext threading issues
            }
        }

        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
                // We'll handle loading in the filter commands instead of automatically loading
                // to prevent DbContext threading issues
            }
        }

        // Today/Month Revenue Statistics
        private decimal _todayRevenue;
        public decimal TodayRevenue
        {
            get => _todayRevenue;
            set
            {
                _todayRevenue = value;
                OnPropertyChanged();
            }
        }

        private decimal _monthRevenue;
        public decimal MonthRevenue
        {
            get => _monthRevenue;
            set
            {
                _monthRevenue = value;
                OnPropertyChanged();
            }
        }
        // Today's Appointment Count
        private int _todayAppointmentCount;
        public int TodayAppointmentCount
        {
            get => _todayAppointmentCount;
            set
            {
                _todayAppointmentCount = value;
                OnPropertyChanged();
            }
        }

        // Yesterday's Appointment Count
        private int _yesterdayAppointmentCount;
        public int YesterdayAppointmentCount
        {
            get => _yesterdayAppointmentCount;
            set
            {
                _yesterdayAppointmentCount = value;
                OnPropertyChanged();
            }
        }

        // Appointment Growth Rate
        private string _appointmentGrowth;
        public string AppointmentGrowth
        {
            get => _appointmentGrowth;
            set
            {
                _appointmentGrowth = value;
                OnPropertyChanged();
            }
        }

        // Appointment Growth Percentage
        private double _appointmentPercentage;
        public double AppointmentPercentage
        {
            get => _appointmentPercentage;
            set
            {
                _appointmentPercentage = value;
                OnPropertyChanged();
            }
        }


        // New Patients Count
        private int _newPatients;
        public int NewPatients
        {
            get => _newPatients;
            set
            {
                _newPatients = value;
                OnPropertyChanged();
            }
        }

        // Low Stock Warning Count
        private int _lowStockCount;
        public int LowStockCount
        {
            get => _lowStockCount;
            set
            {
                _lowStockCount = value;
                OnPropertyChanged();
            }
        }

        // Basic Revenue Statistics
        private decimal _totalRevenue;
        public decimal TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged();
                // Update the revenue percentage when total revenue changes
                RevenuePercentage = RevenueTarget > 0 ? (double)((TotalRevenue / RevenueTarget) * 100) : 0;
            }
        }

        private decimal _revenueTarget = 600000;
        public decimal RevenueTarget
        {
            get => _revenueTarget;
            set
            {
                _revenueTarget = value;
                OnPropertyChanged();
                // Update the revenue percentage when target changes
                RevenuePercentage = RevenueTarget > 0 ? (double)((TotalRevenue / RevenueTarget) * 100) : 0;
            }
        }

        private double _revenuePercentage;
        public double RevenuePercentage
        {
            get => _revenuePercentage;
            set
            {
                if (_revenuePercentage != value)
                {
                    _revenuePercentage = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _revenueGrowth;
        public string RevenueGrowth
        {
            get => _revenueGrowth;
            set
            {
                _revenueGrowth = value;
                OnPropertyChanged();
            }
        }

        // Patient Statistics
        private int _totalPatients;
        public int TotalPatients
        {
            get => _totalPatients;
            set
            {
                _totalPatients = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPercentage));
            }
        }

        private int _patientTarget = 300;
        public int PatientTarget
        {
            get => _patientTarget;
            set
            {
                _patientTarget = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPercentage));
            }
        }

        public double PatientPercentage => PatientTarget > 0 ? (double)((TotalPatients / (double)PatientTarget) * 100) : 0;

        private string _patientGrowth;
        public string PatientGrowth
        {
            get => _patientGrowth;
            set
            {
                _patientGrowth = value;
                OnPropertyChanged();
            }
        }

        // Appointment Statistics
        private int _totalAppointments;
        public int TotalAppointments
        {
            get => _totalAppointments;
            set
            {
                _totalAppointments = value;
                OnPropertyChanged();
            }
        }

        // Medicine Statistics
        private int _totalMedicineSold;
        public int TotalMedicineSold
        {
            get => _totalMedicineSold;
            set
            {
                _totalMedicineSold = value;
                OnPropertyChanged();
            }
        }

        private string _PendingAppointments;
        public string PendingAppointments
        {
            get => _PendingAppointments;
            set
            {
                _PendingAppointments = value; OnPropertyChanged();
            }
        }

        private ObservableCollection<TodayAppointment> _TodayAppointments;
        public ObservableCollection<TodayAppointment> TodayAppointments
        {
            get => _TodayAppointments;
            set
            {
                _TodayAppointments = value; OnPropertyChanged();
            }
        }
        private DateTime _CurrentDate;
        public DateTime CurrentDate
        {
            get => _CurrentDate;
            set
            {
                _CurrentDate = value; OnPropertyChanged();
            }
        }

        #endregion

        #region Chart Properties
        // Hour labels for charts
        public string[] HourLabels { get; } = Enumerable.Range(0, 24)
            .Select(h => $"{h}:00").ToArray();

        // Month labels
        public string[] MonthLabels { get; } = { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };

        // Revenue by Time Period Chart
        private SeriesCollection _revenueByMonthSeries;
        public SeriesCollection RevenueByMonthSeries
        {
            get => _revenueByMonthSeries;
            set
            {
                _revenueByMonthSeries = value;
                OnPropertyChanged();
            }
        }

        // Revenue by Time
       

        private string[] _revenueByTimeLabels;
        public string[] RevenueByTimeLabels
        {
            get => _revenueByTimeLabels;
            set
            {
                _revenueByTimeLabels = value;
                OnPropertyChanged();
            }
        }

        // Invoice Type Series
        private SeriesCollection _invoiceTypeSeries;
        public SeriesCollection InvoiceTypeSeries
        {
            get => _invoiceTypeSeries;
            set
            {
                _invoiceTypeSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _invoiceTypeLabels;
        public string[] InvoiceTypeLabels
        {
            get => _invoiceTypeLabels;
            set
            {
                _invoiceTypeLabels = value;
                OnPropertyChanged();
            }
        }

        // Service Revenue Series
        private SeriesCollection _serviceRevenueSeries;
        public SeriesCollection ServiceRevenueSeries
        {
            get => _serviceRevenueSeries;
            set
            {
                _serviceRevenueSeries = value;
                OnPropertyChanged();
            }
        }

  

        private string[] _revenueComparisonLabels;
        public string[] RevenueComparisonLabels
        {
            get => _revenueComparisonLabels;
            set
            {
                _revenueComparisonLabels = value;
                OnPropertyChanged();
            }
        }

        // Top Revenue Days Series
        private SeriesCollection _topRevenueDaysSeries;
        public SeriesCollection TopRevenueDaysSeries
        {
            get => _topRevenueDaysSeries;
            set
            {
                _topRevenueDaysSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _topRevenueDaysLabels;
        public string[] TopRevenueDaysLabels
        {
            get => _topRevenueDaysLabels;
            set
            {
                _topRevenueDaysLabels = value;
                OnPropertyChanged();
            }
        }

        // Revenue Trend Series
        private SeriesCollection _revenueTrendSeries;
        public SeriesCollection RevenueTrendSeries
        {
            get => _revenueTrendSeries;
            set
            {
                _revenueTrendSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _revenueTrendLabels;
        public string[] RevenueTrendLabels
        {
            get => _revenueTrendLabels;
            set
            {
                _revenueTrendLabels = value;
                OnPropertyChanged();
            }
        }

        // Revenue By Hour Series
        private SeriesCollection _revenueByHourSeries;
        public SeriesCollection RevenueByHourSeries
        {
            get => _revenueByHourSeries;
            set
            {
                _revenueByHourSeries = value;
                OnPropertyChanged();
            }
        }

        // Patient Type Series
        private SeriesCollection _patientTypeSeries;
        public SeriesCollection PatientTypeSeries
        {
            get => _patientTypeSeries;
            set
            {
                _patientTypeSeries = value;
                OnPropertyChanged();
            }
        }

      

        // Appointment Status Series
        private SeriesCollection _appointmentStatusSeries;
        public SeriesCollection AppointmentStatusSeries
        {
            get => _appointmentStatusSeries;
            set
            {
                _appointmentStatusSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _appointmentStatusLabels;
        public string[] AppointmentStatusLabels
        {
            get => _appointmentStatusLabels;
            set
            {
                _appointmentStatusLabels = value;
                OnPropertyChanged();
            }
        }

        // Appointment Peak Hours Series
        private SeriesCollection _appointmentPeakHoursSeries;
        public SeriesCollection AppointmentPeakHoursSeries
        {
            get => _appointmentPeakHoursSeries;
            set
            {
                _appointmentPeakHoursSeries = value;
                OnPropertyChanged();
            }
        }

        // Patients By Doctor Series
        private SeriesCollection _patientsByStaffseries;
        public SeriesCollection PatientsByStaffseries
        {
            get => _patientsByStaffseries;
            set
            {
                _patientsByStaffseries = value;
                OnPropertyChanged();
            }
        }

        private string[] _doctorLabels;
        public string[] DoctorLabels
        {
            get => _doctorLabels;
            set
            {
                _doctorLabels = value;
                OnPropertyChanged();
            }
        }

        // Revenue By Category Series
        private SeriesCollection _revenueByCategorySeries;
        public SeriesCollection RevenueByCategorySeries
        {
            get => _revenueByCategorySeries;
            set
            {
                _revenueByCategorySeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _categoryLabels;
        public string[] CategoryLabels
        {
            get => _categoryLabels;
            set
            {
                _categoryLabels = value;
                OnPropertyChanged();
            }
        }
        private SeriesCollection _productDistributionSeries;
        public SeriesCollection ProductDistributionSeries
        {
            get => _productDistributionSeries;
            set
            {
                _productDistributionSeries = value;
                OnPropertyChanged();
            }
        }
        private SeriesCollection _cancellationRateSeries;
        public SeriesCollection CancellationRateSeries
        {
            get => _cancellationRateSeries;
            set
            {
                _cancellationRateSeries = value;
                OnPropertyChanged();
            }
        }

        private string _currentFilterText = "Đang xem: Tháng này";
        public string CurrentFilterText
        {
            get => _currentFilterText;
            set
            {
                _currentFilterText = value;
                OnPropertyChanged();
            }
        }


        #endregion

        #region Collections
        // Top Selling Products
        private ObservableCollection<TopSellingProduct> _topSellingProducts;
        public ObservableCollection<TopSellingProduct> TopSellingProducts
        {
            get => _topSellingProducts ??= new ObservableCollection<TopSellingProduct>();
            set
            {
                _topSellingProducts = value;
                OnPropertyChanged();
            }
        }

        // Top VIP Patients
        private ObservableCollection<VIPPatient> _topVIPPatients;
        public ObservableCollection<VIPPatient> TopVIPPatients
        {
            get => _topVIPPatients ??= new ObservableCollection<VIPPatient>();
            set
            {
                _topVIPPatients = value;
                OnPropertyChanged();
            }
        }

        // Warning Medicines
        private ObservableCollection<WarningMedicine> _warningMedicines;
        public ObservableCollection<WarningMedicine> WarningMedicines
        {
            get => _warningMedicines ??= new ObservableCollection<WarningMedicine>();
            set
            {
                _warningMedicines = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Formatters and Commands
        // Display Formatter for Y-Axis
        public Func<double, string> YFormatter { get; set; }

        // Commands
        public ICommand RefreshCommand { get; set; }
        public ICommand FilterByDayCommand { get; set; }
        public ICommand FilterByMonthCommand { get; set; }
        public ICommand FilterByQuarterCommand { get; set; }
        public ICommand FilterByYearCommand { get; set; }
        public ICommand ViewLowStockCommand { get; set; }
        public ICommand ExportRevenueToExcelCommand { get; private set; }

        // Additional export commands for each tab
        public ICommand ExportPatientsToExcelCommand { get; private set; }
        public ICommand ExportAppointmentsToExcelCommand { get; private set; }
        public ICommand ExportMedicineToExcelCommand { get; private set; }
        #endregion

        // Track if async operation is running
        private bool _isAsyncOperationRunning = false;
        private object _lockObject = new object();

        public StatisticsViewModel()
        {
            InitializeCommands();

            // Định dạng tiền tệ với 0 chữ số thập phân và đơn vị VNĐ
            YFormatter = value => string.Format("{0:N0} VNĐ", value);
            CurrencyFormatter = value => string.Format("{0:N0} VNĐ", value);

            InitializeCharts();
            LoadDashBoard();

            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => FilterByMonth())
            );
        }
        // Add this property to your class
        public Func<double, string> IntegerFormatter { get; set; }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand<object>(
                p => LoadStatisticsAsync(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            FilterByDayCommand = new RelayCommand<object>(
                p => FilterByDay(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            FilterByMonthCommand = new RelayCommand<object>(
                p => FilterByMonth(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            FilterByQuarterCommand = new RelayCommand<object>(
                p => FilterByQuarter(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            FilterByYearCommand = new RelayCommand<object>(
                p => FilterByYear(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            ViewLowStockCommand = new RelayCommand<object>(
                p => ViewLowStock(),
                p => LowStockCount > 0 && !IsLoading && !_isAsyncOperationRunning
            );
            // Add these lines to your existing InitializeCommands method
            ExportRevenueToExcelCommand = new RelayCommand<object>(
                p => ExportRevenueToExcel(),
                p => !IsLoading
            );

            ExportPatientsToExcelCommand = new RelayCommand<object>(
                p => ExportPatientsToExcel(),
                p => !IsLoading
            );

            ExportAppointmentsToExcelCommand = new RelayCommand<object>(
                p => ExportAppointmentsToExcel(),
                p => !IsLoading
            );

            ExportMedicineToExcelCommand = new RelayCommand<object>(
                p => ExportMedicineToExcel(),
                p => !IsLoading
            );
        }

        public void InitializeCharts()
        {
            // Initialize chart series collections
            RevenueByMonthSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Thực tế",
            Values = new ChartValues<double>(new double[12]),
            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        },
        new LineSeries
        {
            Title = "Mục tiêu",
            Values = new ChartValues<double>(new double[12]),
            PointGeometry = DefaultGeometries.Circle,
            StrokeThickness = 3,
            Stroke = new SolidColorBrush(Color.FromRgb(255, 82, 82)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };

            RevenueByHourSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Doanh thu theo giờ",
            Values = new ChartValues<double>(new double[24]),
            Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };

            // Initialize appointment status labels
            AppointmentStatusLabels = new[] { "Đang chờ", "Đã khám", "Đã hủy", "Đang khám" };

            // Initialize appointment status series
            AppointmentStatusSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Số lượng lịch hẹn",
            Values = new ChartValues<double>(new double[AppointmentStatusLabels.Length]),
            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            LabelPoint = point => string.Format("{0:N0}", point.Y)
        }
    };

            // Initialize empty collections with default "No data" placeholders
            ProductDistributionSeries = new SeriesCollection
    {
        new PieSeries
        {
            Title = "Không có dữ liệu",
            Values = new ChartValues<double> { 100 },
            DataLabels = true,
            LabelPoint = chartPoint => "Không có dữ liệu",
            Fill = new SolidColorBrush(Colors.Gray)
        }
    };

            // Default for top revenue days chart
            TopRevenueDaysSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Doanh thu",
            Values = new ChartValues<double>(Enumerable.Repeat(0.0, 7)),
            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };

            // Default labels for days
            TopRevenueDaysLabels = Enumerable.Range(1, 7)
                .Select(i => DateTime.Now.AddDays(-i).ToString("dd/MM"))
                .Reverse()
                .ToArray();

            // Default for revenue trend chart
            RevenueTrendSeries = new SeriesCollection
    {
        new LineSeries
        {
            Title = "Xu hướng doanh thu",
            Values = new ChartValues<double>(new double[12]),
            PointGeometry = null,
            LineSmoothness = 1,
            Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
            Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };
            RevenueTrendLabels = MonthLabels;

            // Default for invoice type series
            InvoiceTypeSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Doanh thu",
            Values = new ChartValues<double>(new double[] { 0, 0, 0 }),
            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };
            InvoiceTypeLabels = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };

            // Default for patient type series
            PatientTypeSeries = new SeriesCollection
    {
        new PieSeries
        {
            Title = "Không có dữ liệu",
            Values = new ChartValues<double> { 100 },
            DataLabels = true,
            LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này",
            Fill = new SolidColorBrush(Colors.Gray)
        }
    };

            // Default for service revenue
            ServiceRevenueSeries = new SeriesCollection
    {
        new PieSeries
        {
            Title = "Không có dữ liệu",
            Values = new ChartValues<double> { 100 },
            DataLabels = true,
            LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này",
            Fill = new SolidColorBrush(Colors.Gray)
        }
    };

            // Default for appointment peak hours
            AppointmentPeakHoursSeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Số lịch hẹn",
            Values = new ChartValues<double>(new double[24]),
            Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
            LabelPoint = point => string.Format("{0:N0}", point.Y)
        }
    };

            // Default for patients by staff series
            PatientsByStaffseries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Số bệnh nhân",
            Values = new ChartValues<double> { 0 },
            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            LabelPoint = point => string.Format("{0:N0}", point.Y)
        }
    };
            DoctorLabels = new[] { "Không có dữ liệu" };

            // Default for revenue by category
            RevenueByCategorySeries = new SeriesCollection
    {
        new ColumnSeries
        {
            Title = "Doanh thu",
            Values = new ChartValues<double> { 0 },
            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
        }
    };
            CategoryLabels = new[] { "Không có dữ liệu" };

            // Default for cancellation rate
            CancellationRateSeries = new SeriesCollection
    {
        new PieSeries
        {
            Title = "Không có dữ liệu",
            Values = new ChartValues<double> { 100 },
            DataLabels = true,
            LabelPoint = chartPoint => "Không có dữ liệu",
            Fill = new SolidColorBrush(Colors.Gray)
        }
    };
        }



        public async void LoadStatisticsAsync()
        {
            if (_isAsyncOperationRunning)
            {
                return;
            }

            lock (_lockObject)
            {
                if (_isAsyncOperationRunning)
                    return;
                _isAsyncOperationRunning = true;
            }

            IsLoading = true;

            try
            {
                // Sử dụng một DbContext riêng cho mỗi tác vụ và AsNoTracking để cải thiện hiệu suất
                using (var context = new ClinicDbContext())
                {
                    // Tải thống kê cơ bản
                    await Task.Run(() => LoadBasicStatistics(context));
                }

                // Cập nhật biểu đồ trên luồng UI
                Application.Current.Dispatcher.Invoke(() =>
                {
                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải các biểu đồ doanh thu
                        LoadRevenueByMonthChart(context);
                        LoadRevenueByHourChart(context);
                        LoadProductDistributionChart(context);
                        LoadTopRevenueDaysChart(context);
                        LoadRevenueTrendChart(context);
                        LoadInvoiceTypeChart(context);
                        LoadServiceRevenueChart(context);
                        LoadRevenueByCategoryChart(context);
                    }

                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải biểu đồ về bệnh nhân và lịch hẹn
                        LoadPatientTypeChart(context);
                        LoadAppointmentStatusChart(context);
                        LoadAppointmentPeakHoursChart(context);
                        LoadPatientsByDoctorChart(context);
                    }

                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải dữ liệu tài chính và thông tin sản phẩm
                        LoadTopSellingProducts(context);
                        LoadTopVIPPatients(context);
                        CalculateGrowthRates(context);
                    }

                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải cảnh báo thuốc (context riêng để tránh vấn đề dịch LINQ)
                        LoadWarningMedicines(context);
                    }
                });

                // Đảm bảo các command có thể được truy vấn lại sau khi tải dữ liệu
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxService.ShowError($"Lỗi khi tải thống kê: {ex.Message}", "Lỗi");
                });
            }
            finally
            {
                IsLoading = false;
                _isAsyncOperationRunning = false;
            }
        }


        #region Data Loading Methods
        private void LoadBasicStatistics(ClinicDbContext context)
        {
            try
            {
                var today = DateTime.Today;
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);
                var yesterday = today.AddDays(-1);

                // Calculate today's appointments
                int todayAppointments = context.Appointments
                    .Count(a => a.AppointmentDate.Date == today && a.IsDeleted != true);

                // Calculate yesterday's appointments
                int yesterdayAppointments = context.Appointments
                    .Count(a => a.AppointmentDate.Date == yesterday && a.IsDeleted != true);

                // Calculate appointment growth percentage
                double appointmentPercentage = 0;
                string appointmentGrowth = "0.0%";

                if (yesterdayAppointments > 0)
                {
                    appointmentPercentage = ((todayAppointments - yesterdayAppointments) / (double)yesterdayAppointments) * 100;

                    // Format with + sign for positive growth
                    if (appointmentPercentage > 0)
                        appointmentGrowth = $"+{appointmentPercentage:0.0}%";
                    else
                        appointmentGrowth = $"{appointmentPercentage:0.0}%";
                }
                else if (yesterdayAppointments == 0 && todayAppointments > 0)
                {
                    // If yesterday had no appointments but today has some
                    appointmentGrowth = "+100.0%";
                    appointmentPercentage = 100.0;
                }
                else if (yesterdayAppointments == 0 && todayAppointments == 0)
                {
                    // If both days have no appointments
                    appointmentGrowth = "0.0%";
                    appointmentPercentage = 0;
                }
       
                // Today's revenue
                var todayInvoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Date == today &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal todayRevenue = todayInvoices.Sum(i => i.TotalAmount);

                // Month's revenue
                var monthInvoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value >= firstDayOfMonth &&
                           i.InvoiceDate.Value <= today &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal monthRevenue = monthInvoices.Sum(i => i.TotalAmount);

                // Total revenue for selected period
                var periodInvoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal totalRevenue = periodInvoices.Sum(i => i.TotalAmount);

                // New patients count
                int newPatientsCount = context.Patients
                    .Count(p => p.CreatedAt >= StartDate &&
                           p.CreatedAt <= EndDate &&
                           p.IsDeleted != true);

                // Total patients
                int totalPatientsCount = context.Patients
                    .Count(p => p.IsDeleted != true);

                // Total appointments
                int totalAppointmentsCount = context.Appointments
                    .Count(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate);

                // Count total medicine sold (done in memory to avoid translation issues)
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();
                int medicineSoldCount = invoiceDetails.Sum(id => id.Quantity ?? 0);

        
   
                // Update UI elements on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TodayAppointmentCount = todayAppointments;
                    YesterdayAppointmentCount = yesterdayAppointments;
                    AppointmentGrowth = appointmentGrowth;
                    AppointmentPercentage = appointmentPercentage;
                    TodayRevenue = todayRevenue;
                    MonthRevenue = monthRevenue;
                    TotalRevenue = totalRevenue;
                    NewPatients = newPatientsCount;
                    TotalPatients = totalPatientsCount;
                    TotalAppointments = totalAppointmentsCount;
                    TotalMedicineSold = medicineSoldCount;
                  
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxService.ShowError($"Lỗi khi tải thống kê cơ bản: {ex.Message}", "Lỗi");
                });
            }
        }


        private void LoadRevenueByMonthChart(ClinicDbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = new double[12];
                var monthlyTarget = new double[12] { 50000, 50000, 50000, 50000, 50000, 50000, 50000, 50000, 50000, 50000, 50000, 50000 };

                // Fetch and process invoices in memory
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Year == currentYear &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int month = invoice.InvoiceDate.Value.Month - 1; // 0-based index
                        monthlyRevenue[month] += (double)invoice.TotalAmount;
                    }
                }

                // Update chart data
                if (RevenueByMonthSeries?.Count >= 2)
                {
                    var actualSeries = RevenueByMonthSeries[0] as ColumnSeries;
                    var targetSeries = RevenueByMonthSeries[1] as LineSeries;

                    if (actualSeries?.Values is ChartValues<double> actualValues)
                    {
                        actualValues.Clear();
                        actualValues.AddRange(monthlyRevenue);
                        // Thêm định dạng tiền tệ cho LabelPoint
                        actualSeries.LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y);
                    }

                    if (targetSeries?.Values is ChartValues<double> targetValues)
                    {
                        targetValues.Clear();
                        targetValues.AddRange(monthlyTarget);
                        // Thêm định dạng tiền tệ cho LabelPoint
                        targetSeries.LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo tháng: {ex.Message}", "Lỗi");
            }
        }


        private void LoadRevenueByHourChart(ClinicDbContext context)
        {
            try
            {
                var revenueByHour = new double[24];

                // Process data in memory
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int hour = invoice.InvoiceDate.Value.Hour;
                        revenueByHour[hour] += (double)invoice.TotalAmount;
                    }
                }

                // Update chart data
                if (RevenueByHourSeries?.Count > 0)
                {
                    var series = RevenueByHourSeries[0] as ColumnSeries;
                    if (series?.Values is ChartValues<double> values)
                    {
                        values.Clear();
                        values.AddRange(revenueByHour);
                        // Thêm định dạng tiền tệ cho LabelPoint
                        series.LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo giờ: {ex.Message}", "Lỗi");
            }
        }


        private void LoadProductDistributionChart(ClinicDbContext context)
        {
            try
            {
                // Fetch medicine categories
                var medicineCategories = context.MedicineCategories
                    .Where(c => c.IsDeleted != true)
                    .Take(5)
                    .ToList();

                // Fetch invoice details with their related entities
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                    .ThenInclude(m => m.Category)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();

                // Group by category and calculate total sales in memory
                var categorySales = invoiceDetails
                    .Where(id => id.Medicine?.Category != null)
                    .GroupBy(id => id.Medicine.Category.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Medicine.Category.CategoryName,
                        TotalSales = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.TotalSales)
                    .Take(5)
                    .ToList();

                var totalSales = categorySales.Sum(c => c.TotalSales);

                var newSeries = new SeriesCollection();

                if (totalSales > 0 && categorySales.Any())
                {
                    foreach (var category in categorySales)
                    {
                        double percentage = (double)((category.TotalSales / totalSales) * 100);

                        newSeries.Add(new PieSeries
                        {
                            Title = category.CategoryName,
                            Values = new ChartValues<double> { Math.Round(percentage, 1) },
                            DataLabels = true,
                            LabelPoint = chartPoint => $"{category.CategoryName}: {chartPoint.Y:0.0}%",
                            Fill = GetRandomBrush()
                        });
                    }
                }
                else
                {
                    // Add a default "No Data" segment when there's no data
                    newSeries.Add(new PieSeries
                    {
                        Title = "Không có dữ liệu",
                        Values = new ChartValues<double> { 100 },
                        DataLabels = true,
                        LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này",
                        Fill = new SolidColorBrush(Colors.Gray)
                    });
                }

                ProductDistributionSeries = newSeries;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ phân bố sản phẩm: {ex.Message}", "Lỗi");
            }
        }

        private void LoadTopRevenueDaysChart(ClinicDbContext context)
        {
            try
            {
                // Process in memory to avoid translation issues
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                // Group by date but order by date ascending (chronological order) instead of by revenue
                var revenueByDate = invoices
                    .GroupBy(i => i.InvoiceDate.Value.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(i => i.TotalAmount) })
                    .OrderBy(x => x.Date) // Changed from OrderByDescending(x => x.Revenue)
                    .Take(7)
                    .ToList();

                if (revenueByDate.Any())
                {
                    var values = revenueByDate.Select(x => (double)x.Revenue).ToArray();
                    var labels = revenueByDate.Select(x => x.Date.ToString("dd/MM")).ToArray();

                    TopRevenueDaysSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(values),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    // Thêm định dạng tiền tệ cho LabelPoint
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };

                    TopRevenueDaysLabels = labels;
                }
                else
                {
                    // Handle empty data case - provide default values
                    var defaultLabels = Enumerable.Range(1, 7)
                        .Select(i => DateTime.Now.AddDays(-i).ToString("dd/MM"))
                        .Reverse()
                        .ToArray();

                    // Initialize with zero values but proper formatting
                    TopRevenueDaysSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(Enumerable.Repeat(0.0, 7)),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };

                    TopRevenueDaysLabels = defaultLabels;
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo ngày: {ex.Message}", "Lỗi");
            }
        }




        private void LoadRevenueTrendChart(ClinicDbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = new double[12];

                // Process in memory
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Year == currentYear &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int month = invoice.InvoiceDate.Value.Month - 1; // 0-based index
                        monthlyRevenue[month] += (double)invoice.TotalAmount;
                    }
                }

                RevenueTrendSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Xu hướng doanh thu",
                Values = new ChartValues<double>(monthlyRevenue),
                PointGeometry = null,
                LineSmoothness = 1,
                Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54)),
                LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
            }
        };

                RevenueTrendLabels = MonthLabels;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ xu hướng doanh thu: {ex.Message}", "Lỗi");
            }
        }

        private void LoadInvoiceTypeChart(ClinicDbContext context)
        {
            try
            {
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };
                var revenueByType = new double[invoiceTypes.Length];

                // Process in memory
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in invoices)
                {
                    if (!string.IsNullOrEmpty(invoice.InvoiceType))
                    {
                        int index = Array.IndexOf(invoiceTypes, invoice.InvoiceType);
                        if (index >= 0)
                        {
                            revenueByType[index] += (double)invoice.TotalAmount;
                        }
                    }
                }

                // Check if we have any data
                if (revenueByType.Sum() > 0)
                {
                    InvoiceTypeSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(revenueByType),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };
                }
                else
                {
                    // Add a default series with zero values
                    InvoiceTypeSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(new double[] { 0, 0, 0 }),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };
                }

                InvoiceTypeLabels = invoiceTypes;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo loại hóa đơn: {ex.Message}", "Lỗi");
            }
        }

        private void LoadServiceRevenueChart(ClinicDbContext context)
        {
            try
            {
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };

                // Process in memory
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                         i.InvoiceDate <= EndDate &&
                         i.Status == "Đã thanh toán")
                    .ToList();

                var totalRevenue = invoices.Sum(i => i.TotalAmount);

                var seriesCollection = new SeriesCollection();

                if (totalRevenue > 0)
                {
                    foreach (var type in invoiceTypes)
                    {
                        var typeRevenue = invoices
                            .Where(i => i.InvoiceType == type)
                            .Sum(i => i.TotalAmount);

                        if (typeRevenue > 0)
                        {
                            double percentage = Math.Round((double)((typeRevenue / totalRevenue) * 100), 1);

                            var brushColor = type switch
                            {
                                "Khám bệnh" => Color.FromRgb(76, 175, 80),
                                "Bán thuốc" => Color.FromRgb(255, 152, 0),
                                _ => Color.FromRgb(33, 150, 243)
                            };

                            seriesCollection.Add(new PieSeries
                            {
                                Title = type,
                                Values = new ChartValues<double> { percentage },
                                DataLabels = true,
                                LabelPoint = chartPoint => $"{type}: {chartPoint.Y:0.0}% ({string.Format("{0:N0} VNĐ", typeRevenue)})",
                                Fill = new SolidColorBrush(brushColor)
                            });
                        }
                    }
                }

                // If no data, add a default segment
                if (seriesCollection.Count == 0)
                {
                    seriesCollection.Add(new PieSeries
                    {
                        Title = "Không có dữ liệu",
                        Values = new ChartValues<double> { 100 },
                        DataLabels = true,
                        LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này",
                        Fill = new SolidColorBrush(Colors.Gray)
                    });
                }

                ServiceRevenueSeries = seriesCollection;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ tỷ lệ doanh thu theo dịch vụ: {ex.Message}", "Lỗi");
            }
        }


        private void LoadPatientTypeChart(ClinicDbContext context)
        {
            try
            {
                var patientTypes = context.PatientTypes
                    .Where(pt => pt.IsDeleted != true)
                    .ToList();

                var seriesCollection = new SeriesCollection();

                // Process in memory
                var patients = context.Patients
                    .Where(p => p.IsDeleted != true &&
                          p.CreatedAt >= StartDate &&
                          p.CreatedAt <= EndDate)
                    .ToList();

                var totalPatients = patients.Count;

                if (totalPatients > 0 && patientTypes.Any())
                {
                    foreach (var type in patientTypes)
                    {
                        var patientCount = patients.Count(p => p.PatientTypeId == type.PatientTypeId);
                        double percentage = Math.Round((double)(patientCount * 100) / totalPatients, 1);

                        if (percentage > 0)
                        {
                            var colorIndex = type.PatientTypeId % 5;
                            var colors = new[]
                            {
                        Color.FromRgb(244, 67, 54),
                        Color.FromRgb(33, 150, 243),
                        Color.FromRgb(76, 175, 80),
                        Color.FromRgb(255, 152, 0),
                        Color.FromRgb(156, 39, 176)
                    };

                            seriesCollection.Add(new PieSeries
                            {
                                Title = type.TypeName,
                                Values = new ChartValues<double> { percentage },
                                DataLabels = true,
                                LabelPoint = chartPoint => $"{type.TypeName}: {chartPoint.Y:0.0}%",
                                Fill = new SolidColorBrush(colors[colorIndex])
                            });
                        }
                    }
                }

                // If there's no data, add a default segment
                if (seriesCollection.Count == 0)
                {
                    seriesCollection.Add(new PieSeries
                    {
                        Title = "Không có dữ liệu",
                        Values = new ChartValues<double> { 100 },
                        DataLabels = true,
                        LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này",
                        Fill = new SolidColorBrush(Colors.Gray)
                    });
                }

                PatientTypeSeries = seriesCollection;
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ phân loại bệnh nhân: {ex.Message}", "Lỗi");
            }
        }

        private void LoadAppointmentStatusChart(ClinicDbContext context)
        {
            try
            {
                // Get all appointments in the date range
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           a.IsDeleted != true)
                    .ToList();

                var statusCounts = new double[AppointmentStatusLabels.Length];

                // Count appointments for each status
                foreach (var status in AppointmentStatusLabels)
                {
                    int index = Array.IndexOf(AppointmentStatusLabels, status);
                    if (index >= 0)
                    {
                        statusCounts[index] = appointments.Count(a => a.Status == status);
                    }
                }

                // Update the chart data
                if (AppointmentStatusSeries?.Count > 0)
                {
                    var series = AppointmentStatusSeries[0] as ColumnSeries;
                    if (series?.Values is ChartValues<double> values)
                    {
                        values.Clear();

                        // Check if we have any data
                        if (statusCounts.Sum() > 0)
                        {
                            values.AddRange(statusCounts);
                        }
                        else
                        {
                            // Add zero values if no data
                            values.AddRange(Enumerable.Repeat(0.0, AppointmentStatusLabels.Length));
                        }

                        // Ensure we have a proper label formatter
                        series.LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ trạng thái lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        private void LoadAppointmentPeakHoursChart(ClinicDbContext context)
        {
            try
            {
                var appointmentsByHour = new double[24];

                // Process in memory
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           a.IsDeleted != true)
                    .ToList();

                foreach (var appointment in appointments)
                {
                    int hour = appointment.AppointmentDate.Hour;
                    if (hour >= 0 && hour < 24)
                    {
                        appointmentsByHour[hour]++;
                    }
                }

                // Check if we have any appointments
                if (appointments.Any())
                {
                    AppointmentPeakHoursSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lịch hẹn",
                    Values = new ChartValues<double>(appointmentsByHour),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0))
                }
            };
                }
                else
                {
                    // Create a series with all zeros if there's no data
                    AppointmentPeakHoursSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lịch hẹn",
                    Values = new ChartValues<double>(Enumerable.Repeat(0.0, 24)),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0))
                }
            };
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ giờ cao điểm lịch hẹn: {ex.Message}", "Lỗi");
            }
        }


        private void LoadPatientsByDoctorChart(ClinicDbContext context)
        {
            try
            {
                // Get all Staffs
                var staffs = context.Staffs
                    .Where(d => d.IsDeleted != true)
                    .Take(10)
                    .ToList();

                if (!staffs.Any())
                {
                    // No doctors found - create empty chart
                    PatientsByStaffseries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số bệnh nhân",
                    Values = new ChartValues<double>(),
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    LabelPoint = point => string.Format("{0:N0}", point.Y)
                }
            };
                    DoctorLabels = new string[] { "Không có dữ liệu" };
                    return;
                }

                // Get appointments in the date range
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           a.IsDeleted != true)
                    .ToList();

                var doctorNames = new List<string>();
                var patientCounts = new List<double>();

                foreach (var doctor in staffs)
                {
                    doctorNames.Add(doctor.FullName);
                    patientCounts.Add(appointments.Count(a => a.StaffId == doctor.StaffId));
                }

                PatientsByStaffseries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = "Số bệnh nhân",
                Values = new ChartValues<double>(patientCounts),
                Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                LabelPoint = point => string.Format("{0:N0}", point.Y)
            }
        };

                DoctorLabels = doctorNames.ToArray();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ bệnh nhân theo bác sĩ: {ex.Message}", "Lỗi");
            }
        }

        private void LoadTopSellingProducts(ClinicDbContext context)
        {
            try
            {
                // Sử dụng AsNoTracking cho hiệu suất tốt hơn với các truy vấn chỉ đọc
                var invoiceDetails = context.InvoiceDetails
                    .AsNoTracking()
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                        .ThenInclude(m => m.Category)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();

                var medicineSales = invoiceDetails
                    .Where(id => id.Medicine != null)
                    .GroupBy(id => id.Medicine.MedicineId)
                    .Select(g => new TopSellingProduct
                    {
                        Id = g.Key,
                        Name = g.First().Medicine.Name,
                        Category = g.First().Medicine.Category?.CategoryName ?? "Không phân loại",
                        Sales = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.Sales)
                    .Take(10)
                    .ToList();

                var totalSales = medicineSales.Sum(m => m.Sales);

                if (totalSales > 0)
                {
                    foreach (var product in medicineSales)
                    {
                        product.Percentage = (int)Math.Round((product.Sales / totalSales) * 100);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopSellingProducts = new ObservableCollection<TopSellingProduct>(medicineSales);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopSellingProducts.Clear();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu sản phẩm bán chạy: {ex.Message}", "Lỗi");
            }
        }


        private void LoadTopVIPPatients(ClinicDbContext context)
        {
            try
            {
                // Process in memory
                var invoices = context.Invoices
                    .Where(i => i.Status == "Đã thanh toán" &&
                           i.PatientId != null &&
                           i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate)
                    .ToList();

                var topPatients = invoices
                    .GroupBy(i => i.PatientId)
                    .Select(g => new
                    {
                        PatientId = g.Key,
                        TotalSpending = g.Sum(i => i.TotalAmount)
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .Take(5)
                    .ToList();

                if (topPatients.Any())
                {
                    var vipPatients = new List<VIPPatient>();

                    // Now fetch patient details
                    foreach (var patientData in topPatients)
                    {
                        var patient = context.Patients
                            .FirstOrDefault(p => p.PatientId == patientData.PatientId);

                        if (patient != null)
                        {
                            vipPatients.Add(new VIPPatient
                            {
                                Id = patient.PatientId,
                                FullName = patient.FullName,
                                Phone = patient.Phone,
                                TotalSpending = patientData.TotalSpending
                            });
                        }
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopVIPPatients = new ObservableCollection<VIPPatient>(vipPatients);
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopVIPPatients.Clear();
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải dữ liệu khách hàng VIP: {ex.Message}",
                               "Lỗi"    );
            }
        }

        // Change from private to public
        public void LoadWarningMedicines(ClinicDbContext context)
        {
            try
            {
                // Xóa dữ liệu theo dõi để đảm bảo dữ liệu mới
                context.ChangeTracker.Clear();

                // Lấy tất cả các thuốc không bị xóa với dữ liệu liên quan
                var medicines = context.Medicines
                    .AsNoTracking()
                    .Where(m => m.IsDeleted != true)
                    .Include(m => m.StockIns)
                    .Include(m => m.Unit)
                    .ToList();

                // Chuẩn bị danh sách cảnh báo
                var warnings = new List<WarningMedicine>();

                // Điểm tham chiếu ngày hiện tại
                var today = DateOnly.FromDateTime(DateTime.Today);
                var thirtyDaysLater = today.AddDays(30);

                foreach (var medicine in medicines)
                {
                    // Đặt lại cache để đảm bảo tính toán mới
                    medicine._availableStockInsCache = null;

                    // Bỏ qua thuốc không có lô
                    if (!medicine.StockIns.Any()) continue;

                    // Lấy các lô có số lượng còn lại và chưa bị tiêu hủy
                    var activeBatches = medicine.StockIns
                        .Where(si => si.RemainQuantity > 0 && !si.IsTerminated)
                        .OrderBy(si => si.ExpiryDate) // Sắp xếp theo ngày hết hạn, gần nhất trước
                        .ToList();

                    // Bỏ qua nếu không có lô nào còn hoạt động
                    if (!activeBatches.Any()) continue;

                    // Đếm tổng số thuốc còn lại
                    int totalRemaining = activeBatches.Sum(si => si.RemainQuantity);

                    // Kiểm tra các lô đang sử dụng
                    foreach (var stockIn in activeBatches.Take(2)) // Chỉ kiểm tra 2 lô đầu tiên theo ngày hết hạn
                    {
                        // 1. Kiểm tra nếu là lô đã hết hạn
                        if (stockIn.ExpiryDate.HasValue && stockIn.ExpiryDate.Value <= today)
                        {
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"CẦN TIÊU HỦY: Lô thuốc đã hết hạn từ {stockIn.ExpiryDate.Value:dd/MM/yyyy} nhưng vẫn còn {stockIn.RemainQuantity} {medicine.Unit?.UnitName ?? "đơn vị"}"
                            });
                            break; // Ưu tiên cảnh báo tiêu hủy
                        }
                        // 2. Kiểm tra nếu là lô sắp hết hạn
                        else if (stockIn.ExpiryDate.HasValue && stockIn.ExpiryDate.Value <= thirtyDaysLater)
                        {
                            int daysUntilExpiry = stockIn.ExpiryDate.Value.DayNumber - today.DayNumber;
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"SẮP HẾT HẠN: Còn {daysUntilExpiry} ngày đến hạn sử dụng ({stockIn.ExpiryDate.Value:dd/MM/yyyy})"
                            });
                            break; // Không kiểm tra các cảnh báo khác cho thuốc này
                        }
                        // 3. Kiểm tra nếu lô đang bán đã được đánh dấu tiêu hủy
                        else if (stockIn.IsSelling && stockIn.IsTerminated)
                        {
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"LỖI CẤU HÌNH: Lô thuốc được đánh dấu tiêu hủy nhưng vẫn là lô đang bán"
                            });
                            break; // Ưu tiên cảnh báo này
                        }
                    }

                    // 4. Kiểm tra tồn kho thấp nếu chưa có cảnh báo nào cho thuốc này
                    if (!warnings.Any(w => w.Id == medicine.MedicineId))
                    {
                        if (totalRemaining <= 10)
                        {
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"TỒN KHO THẤP: Chỉ còn {totalRemaining} {medicine.Unit?.UnitName ?? "đơn vị"} - Cần nhập thêm gấp"
                            });
                        }
                        else if (totalRemaining <= 20)
                        {
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"TỒN KHO THẤP: Chỉ còn {totalRemaining} {medicine.Unit?.UnitName ?? "đơn vị"} - Nên nhập thêm"
                            });
                        }

                        // 5. Kiểm tra nếu lô cuối cùng
                        else if (activeBatches.Count == 1)
                        {
                            warnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"LÔ CUỐI CÙNG: Thuốc chỉ còn 1 lô với {activeBatches[0].RemainQuantity} {medicine.Unit?.UnitName ?? "đơn vị"}"
                            });
                        }
                    }
                }

                // Sắp xếp cảnh báo theo mức độ nghiêm trọng
                var sortedWarnings = warnings
                    .OrderByDescending(w => w.WarningMessage.Contains("CẦN TIÊU HỦY"))
                    .ThenByDescending(w => w.WarningMessage.Contains("LỖI CẤU HÌNH"))
                    .ThenByDescending(w => w.WarningMessage.Contains("SẮP HẾT HẠN"))
                    .ThenByDescending(w => w.WarningMessage.Contains("LÔ CUỐI CÙNG"))
                    .ThenByDescending(w => w.WarningMessage.Contains("TỒN KHO THẤP"))
                    .Take(10)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    WarningMedicines = new ObservableCollection<WarningMedicine>(sortedWarnings);
                    LowStockCount = sortedWarnings.Count;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxService.ShowError($"Lỗi khi tải cảnh báo thuốc: {ex.Message}", "Lỗi");
                    WarningMedicines.Clear();
                    LowStockCount = 0;
                });
            }
        }


        /// <summary>
        /// Tính toán tỷ lệ tăng trưởng doanh thu và bệnh nhân mới giữa kỳ hiện tại và kỳ trước đó.
        /// </summary>
        /// <param name="context">Context cơ sở dữ liệu để truy xuất dữ liệu</param>
        /// <remarks>
        /// Phương thức này thực hiện các tính toán sau:
        /// 1. Xác định khoảng thời gian của kỳ trước đó (có cùng độ dài với kỳ hiện tại)
        /// 2. Tính toán doanh thu của cả hai kỳ từ các hóa đơn đã thanh toán
        /// 3. Tính toán số lượng bệnh nhân mới trong cả hai kỳ
        /// 4. Tính tỷ lệ tăng trưởng doanh thu và bệnh nhân dưới dạng phần trăm
        /// 5. Định dạng kết quả với dấu +/- phù hợp và một chữ số thập phân
        /// 
        /// Các trường hợp đặc biệt:
        /// - Kỳ trước có doanh thu bằng 0, kỳ này có doanh thu: hiển thị "+100.0%"
        /// - Cả hai kỳ đều có doanh thu bằng 0: hiển thị "0.0%"
        /// - Có lỗi trong quá trình tính toán: hiển thị "N/A"
        /// </remarks>
        private void CalculateGrowthRates(ClinicDbContext context)
        {
            try
            {
                // Calculate revenue growth compared to previous period
                var previousPeriodStart = StartDate.AddDays(-(EndDate - StartDate).TotalDays);
                var previousPeriodEnd = StartDate.AddDays(-1);

                // Get invoices for current and previous periods
                var currentPeriodInvoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                var previousPeriodInvoices = context.Invoices
                    .Where(i => i.InvoiceDate >= previousPeriodStart &&
                           i.InvoiceDate <= previousPeriodEnd &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                // Calculate in memory
                var currentRevenue = currentPeriodInvoices.Sum(i => i.TotalAmount);
                var previousRevenue = previousPeriodInvoices.Sum(i => i.TotalAmount);

                // Set value for RevenueGrowth with better formatting
                if (previousRevenue > 0)
                {
                    var revenueGrowth = ((currentRevenue - previousRevenue) / previousRevenue) * 100;

                    // Chỉ hiển thị 1 số thập phân và đảm bảo hiển thị dấu + cho tăng trưởng dương
                    if (revenueGrowth > 0)
                        RevenueGrowth = $"+{revenueGrowth:0.0}%";
                    else
                        RevenueGrowth = $"{revenueGrowth:0.0}%";
                }
                else if (previousRevenue == 0 && currentRevenue > 0)
                {
                    // Nếu kỳ trước không có doanh thu nhưng kỳ này có
                    RevenueGrowth = "+100.0%";
                }
                else if (previousRevenue == 0 && currentRevenue == 0)
                {
                    // Nếu cả hai kỳ đều không có doanh thu
                    RevenueGrowth = "0.0%";
                }
                else
                {
                    RevenueGrowth = "N/A";
                }

                // Calculate patient growth with similar improvements
                var currentPeriodPatients = context.Patients
                    .Count(p => p.CreatedAt >= StartDate &&
                           p.CreatedAt <= EndDate &&
                           p.IsDeleted != true);

                var previousPeriodPatients = context.Patients
                    .Count(p => p.CreatedAt >= previousPeriodStart &&
                           p.CreatedAt <= previousPeriodEnd &&
                           p.IsDeleted != true);

                if (previousPeriodPatients > 0)
                {
                    var patientGrowth = ((currentPeriodPatients - previousPeriodPatients) / (double)previousPeriodPatients) * 100;

                    // Định dạng tương tự như RevenueGrowth
                    if (patientGrowth > 0)
                        PatientGrowth = $"+{patientGrowth:0.0}%";
                    else
                        PatientGrowth = $"{patientGrowth:0.0}%";
                }
                else if (previousPeriodPatients == 0 && currentPeriodPatients > 0)
                {
                    PatientGrowth = "+100.0%";
                }
                else if (previousPeriodPatients == 0 && currentPeriodPatients == 0)
                {
                    PatientGrowth = "0.0%";
                }
                else
                {
                    PatientGrowth = "N/A";
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tính toán tỷ lệ tăng trưởng: {ex.Message}", "Lỗi");
                RevenueGrowth = "N/A";
                PatientGrowth = "N/A";
            }
        }

        private void LoadRevenueByCategoryChart(ClinicDbContext context)
        {
            try
            {
                // Lấy tất cả danh mục thuốc
                var medicineCategories = context.MedicineCategories
                    .Where(c => c.IsDeleted != true)
                    .ToList();

                // Lấy chi tiết hóa đơn thuốc trong khoảng thời gian đã chọn
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                    .ThenInclude(m => m.Category)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();

                // Nhóm theo danh mục và tính tổng doanh thu
                var categoryRevenues = invoiceDetails
                    .Where(id => id.Medicine?.Category != null)
                    .GroupBy(id => id.Medicine.Category.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Medicine.Category.CategoryName,
                        TotalRevenue = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.TotalRevenue)
                    .ToList();

                // Chuẩn bị dữ liệu cho biểu đồ
                var categoryNames = new List<string>();
                var revenueValues = new List<double>();

                foreach (var categoryRevenue in categoryRevenues)
                {
                    categoryNames.Add(categoryRevenue.CategoryName);
                    revenueValues.Add((double)categoryRevenue.TotalRevenue);
                }

                // Check if we have any data
                if (revenueValues.Any())
                {
                    RevenueByCategorySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(revenueValues),
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };

                    CategoryLabels = categoryNames.ToArray();
                }
                else
                {
                    // Create a default chart with no data
                    RevenueByCategorySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double> { 0 },
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };

                    CategoryLabels = new[] { "Không có dữ liệu" };
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo danh mục: {ex.Message}", "Lỗi");
            }
        }

        private void LoadDashBoard()
        {
            try
            {
                CurrentDate = DateTime.Now.Date;
                TodayAppointments = new ObservableCollection<TodayAppointment>();

                using (var context = new ClinicDbContext())
                {
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    var appointments = context.Appointments
                        .AsNoTracking()
                        .Where(a => a.IsDeleted == false || a.IsDeleted == null)
                        .Include(a => a.Patient)
                        .Include(a => a.Staff)
                        .Include(a => a.AppointmentType)
                        .OrderBy(a => a.AppointmentDate)
                        .ToList();

                    int waitingCount = appointments.Count(a => a.Status.Trim() == "Đang chờ");

                    foreach (var appointment in appointments.Where(a => a.AppointmentDate.Date == CurrentDate.Date))
                    {
                        TodayAppointment app = new TodayAppointment
                        {
                            Appointment = appointment,
                            Initials = GetInitialsFromFullName(appointment.Patient?.FullName),
                            PatientName = appointment.Patient?.FullName,
                            DoctorName = appointment.Staff?.FullName,
                            Notes = appointment.Notes,
                            Status = appointment.Status?.Trim(),
                            Time = appointment.AppointmentDate.TimeOfDay
                        };
                        TodayAppointments.Add(app);
                    }

                    TotalAppointments = appointments.Count(a => a.AppointmentDate.Date == CurrentDate.Date);
                    PendingAppointments = waitingCount.ToString();
                    TotalPatients = context.Patients.Count(p => p.IsDeleted != true);
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải Dashboard: {ex.Message}", "Lỗi");
            }
        }

        private string GetInitialsFromFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            if (parts.Length >= 2)
            {
                var middle = parts[parts.Length - 2];
                var last = parts[parts.Length - 1];
                return $"{char.ToUpper(middle[0])}.{char.ToUpper(last[0])}";
            }
            else if (parts.Length == 1)
            {
                return char.ToUpper(parts[0][0]).ToString();
            }

            return string.Empty;
        }
        #endregion

        #region Action Methods
        private void FilterByDay()
        {
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now;
            CurrentFilterText = "Đang xem: Hôm nay";
            LoadStatisticsAsync();
        }

        private void FilterByMonth()
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            CurrentFilterText = "Đang xem: Tháng này";
            LoadStatisticsAsync();
        }

        private void FilterByQuarter()
        {
            int currentMonth = DateTime.Now.Month;
            int quarterNumber = (currentMonth - 1) / 3 + 1;
            int quarterStartMonth = (quarterNumber - 1) * 3 + 1;

            StartDate = new DateTime(DateTime.Now.Year, quarterStartMonth, 1);
            EndDate = DateTime.Now;
            CurrentFilterText = $"Đang xem: Quý {quarterNumber}";
            LoadStatisticsAsync();
        }

        private void FilterByYear()
        {
            StartDate = new DateTime(DateTime.Now.Year, 1, 1);
            EndDate = DateTime.Now;
            CurrentFilterText = $"Đang xem: Năm {DateTime.Now.Year}";
            LoadStatisticsAsync();
        }

        private void ViewLowStock()
        {
            if (WarningMedicines.Count > 0)
            {
                var warningText = string.Join("\n", WarningMedicines.Select(m => $"{m.Name}: {m.WarningMessage}"));
                MessageBoxService.ShowError($"Danh sách thuốc cần chú ý:\n\n{warningText}",
                               "Cảnh báo tồn kho"   );
            }
            else
            {
                MessageBoxService.ShowError("Không có thuốc nào cần chú ý trong kho.",
                               "Thông báo"     );
            }
        }

        private SolidColorBrush GetRandomBrush()
        {
            var colors = new[]
            {
                Color.FromRgb(66, 133, 244),  // Blue
                Color.FromRgb(219, 68, 55),   // Red
                Color.FromRgb(244, 180, 0),   // Yellow
                Color.FromRgb(15, 157, 88),   // Green
                Color.FromRgb(171, 71, 188)   // Purple
            };

            Random random = new Random();
            return new SolidColorBrush(colors[random.Next(colors.Length)]);
        }

        /// <summary>
        /// Exports revenue statistics to Excel
        /// </summary>
        private void ExportRevenueToExcel()
        {
            try
            {
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"ThongKeDoanhThu_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Determine the title based on current filter
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê doanh thu {period}");

                                // Add title
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ DOANH THU {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added header
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Current row position tracker
                                int currentRow = 4;

                                // Export each chart data to separate tables
                                currentRow = ExportTopRevenueDaysChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));

                                currentRow = ExportInvoiceTypeChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                                currentRow = ExportRevenueTrendChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                currentRow = ExportRevenueByHourChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Format the worksheet for better readability
                                worksheet.Columns().AdjustToContents();

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - Complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Show 100% briefly

                                // Close progress dialog and show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê doanh thu thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // Ask if user wants to open the Excel file
                                    if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                    {
                                        try
                                        {
                                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                            {
                                                FileName = saveFileDialog.FileName,
                                                UseShellExecute = true
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Close progress dialog on error
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Exports patient statistics to Excel
        /// </summary>
        private void ExportPatientsToExcel()
        {
            try
            {
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"ThongKeBenhNhan_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Create copies of data on UI thread before passing to background thread
                    var patientTypeSeriesCopy = new List<(string Title, double Value)>();
                    if (PatientTypeSeries != null)
                    {
                        foreach (var series in PatientTypeSeries)
                        {
                            if (series is LiveCharts.Wpf.PieSeries pieSeries &&
                                pieSeries.Values is LiveCharts.ChartValues<double> values &&
                                values.Count > 0)
                            {
                                patientTypeSeriesCopy.Add((pieSeries.Title ?? "Không có tên", values[0]));
                            }
                        }
                    }

                    var topVIPPatientsCopy = TopVIPPatients?.ToList() ?? new List<VIPPatient>();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Determine the title based on current filter
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê bệnh nhân {period}");

                                // Add title
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ BỆNH NHÂN {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added header
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Current row position tracker
                                int currentRow = 4;

                                // Export each chart data to separate tables using copies
                                currentRow = ExportPatientTypeChartFromCopy(worksheet, currentRow, patientTypeSeriesCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(40));

                                currentRow = ExportTopVIPPatientsTableFromCopy(worksheet, currentRow, topVIPPatientsCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // Format the worksheet for better readability
                                worksheet.Columns().AdjustToContents();

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - Complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Show 100% briefly

                                // Close progress dialog and show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê bệnh nhân thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // Ask if user wants to open the Excel file
                                    if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                    {
                                        try
                                        {
                                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                            {
                                                FileName = saveFileDialog.FileName,
                                                UseShellExecute = true
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Close progress dialog on error
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Exports appointment statistics to Excel
        /// </summary>
        private void ExportAppointmentsToExcel()
        {
            try
            {
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"ThongKeLichHen_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // Determine the title based on current filter
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê lịch hẹn {period}");

                                // Add title
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ LỊCH HẸN {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added header
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Current row position tracker
                                int currentRow = 4;

                                // Export each chart data to separate tables
                                currentRow = ExportAppointmentStatusChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(40));

                                currentRow = ExportAppointmentPeakHoursChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                currentRow = ExportPatientsByDoctorChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Format the worksheet for better readability
                                worksheet.Columns().AdjustToContents();

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - Complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Show 100% briefly

                                // Close progress dialog and show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê lịch hẹn thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // Ask if user wants to open the Excel file
                                    if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                    {
                                        try
                                        {
                                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                            {
                                                FileName = saveFileDialog.FileName,
                                                UseShellExecute = true
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Close progress dialog on error
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Exports medicine statistics to Excel
        /// </summary>
        private void ExportMedicineToExcel()
        {
            try
            {
                // Create a save file dialog
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",
                    DefaultExt = "xlsx",
                    Title = "Chọn vị trí lưu file Excel",
                    FileName = $"ThongKeThuoc_{DateTime.Now:dd-MM-yyyy}.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // Create and show progress dialog
                    ProgressDialog progressDialog = new ProgressDialog();

                    // Create copies of collection data on UI thread
                    var revenueByCategorySeries = new List<(string Category, double Value)>();
                    if (RevenueByCategorySeries?.Count > 0 && CategoryLabels?.Length > 0)
                    {
                        var series = RevenueByCategorySeries[0] as LiveCharts.Wpf.ColumnSeries;
                        if (series?.Values is LiveCharts.ChartValues<double> values)
                        {
                            for (int i = 0; i < CategoryLabels.Length; i++)
                            {
                                double value = i < values.Count ? values[i] : 0;
                                revenueByCategorySeries.Add((CategoryLabels[i], value));
                            }
                        }
                    }

                    // Copy other collections
                    var productDistributionCopy = new List<(string Title, double Value)>();
                    if (ProductDistributionSeries != null)
                    {
                        foreach (var series in ProductDistributionSeries)
                        {
                            if (series is LiveCharts.Wpf.PieSeries pieSeries &&
                                pieSeries.Values is LiveCharts.ChartValues<double> values &&
                                values.Count > 0)
                            {
                                productDistributionCopy.Add((pieSeries.Title ?? "Không có tên", values[0]));
                            }
                        }
                    }

                    var topSellingProductsCopy = TopSellingProducts?.ToList() ?? new List<TopSellingProduct>();
                    var warningMedicinesCopy = WarningMedicines?.ToList() ?? new List<WarningMedicine>();

                    // Get period title on UI thread
                    string period = GetCurrentPeriodTitle();

                    // Start export operation in background thread
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // Report progress: 5% - Created workbook
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                var worksheet = workbook.Worksheets.Add($"Thống kê thuốc {period}");

                                // Add title
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ THUỐC {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Add current date
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // Report progress: 10% - Added header
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // Current row position tracker
                                int currentRow = 4;

                                // Export each chart data to separate tables using copies
                                currentRow = ExportRevenueByCategoryChartFromCopy(worksheet, currentRow, revenueByCategorySeries);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));

                                currentRow = ExportProductDistributionChartFromCopy(worksheet, currentRow, productDistributionCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                                currentRow = ExportTopSellingProductsTableFromCopy(worksheet, currentRow, topSellingProductsCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                currentRow = ExportWarningMedicinesTableFromCopy(worksheet, currentRow, warningMedicinesCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // Format the worksheet for better readability
                                worksheet.Columns().AdjustToContents();

                                // Save the workbook
                                workbook.SaveAs(saveFileDialog.FileName);

                                // Report progress: 100% - Complete
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Show 100% briefly

                                // Close progress dialog and show success message
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê thuốc thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // Ask if user wants to open the Excel file
                                    if (MessageBoxService.ShowQuestion("Bạn có muốn mở file Excel không?", "Mở file"))
                                    {
                                        try
                                        {
                                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                            {
                                                FileName = saveFileDialog.FileName,
                                                UseShellExecute = true
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            MessageBoxService.ShowError($"Không thể mở file: {ex.Message}", "Lỗi");
                                        }
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            // Close progress dialog on error
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // Show dialog - this will block until the dialog is closed
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }


        /// <summary>
        /// Gets a descriptive title for the current filter period
        /// </summary>
        private string GetCurrentPeriodTitle()
        {
            // Parse the current filter text to determine which period we're viewing
            if (CurrentFilterText.Contains("Hôm nay"))
            {
                return $"hôm nay ({DateTime.Now:dd/MM/yyyy})";
            }
            else if (CurrentFilterText.Contains("Tháng"))
            {
                return $"tháng {DateTime.Now.Month}-{DateTime.Now.Year}";
            }
            else if (CurrentFilterText.Contains("Quý"))
            {
                int quarterNumber = (DateTime.Now.Month - 1) / 3 + 1;
                return $"quý {quarterNumber}/{DateTime.Now.Year}";
            }
            else if (CurrentFilterText.Contains("Năm"))
            {
                return $"năm {DateTime.Now.Year}";
            }
            else
            {
                // Custom date range
                return $"từ {StartDate:dd/MM/yyyy} đến {EndDate:dd/MM/yyyy}";
            }
        }

        #region Export Chart Methods

        /// <summary>
        /// Exports Top Revenue Days chart to Excel
        /// </summary>
        private int ExportTopRevenueDaysChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO NGÀY";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Ngày";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (TopRevenueDaysSeries?.Count > 0 && TopRevenueDaysLabels?.Length > 0)
            {
                var series = TopRevenueDaysSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    for (int i = 0; i < TopRevenueDaysLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = TopRevenueDaysLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Invoice Type chart to Excel
        /// </summary>
        private int ExportInvoiceTypeChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO LOẠI HÓA ĐƠN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Loại hóa đơn";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (InvoiceTypeSeries?.Count > 0 && InvoiceTypeLabels?.Length > 0)
            {
                var series = InvoiceTypeSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    double total = values.Sum();

                    for (int i = 0; i < InvoiceTypeLabels.Length; i++)
                    {
                        double value = i < values.Count ? values[i] : 0;
                        double percentage = total > 0 ? (value / total) * 100 : 0;

                        worksheet.Cell(startRow, 1).Value = InvoiceTypeLabels[i];
                        worksheet.Cell(startRow, 2).Value = value;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(startRow, 3).Value = percentage;
                        worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = total;
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 3).Value = 1;
                    worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Revenue Trend chart to Excel
        /// </summary>
        private int ExportRevenueTrendChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "XU HƯỚNG DOANH THU THEO THỜI GIAN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Thời gian";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (RevenueTrendSeries?.Count > 0 && RevenueTrendLabels?.Length > 0)
            {
                var series = RevenueTrendSeries[0] as LiveCharts.Wpf.LineSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    for (int i = 0; i < RevenueTrendLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = RevenueTrendLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Revenue By Hour chart to Excel
        /// </summary>
        private int ExportRevenueByHourChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO GIỜ TRONG NGÀY";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Giờ";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (RevenueByHourSeries?.Count > 0)
            {
                var series = RevenueByHourSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    for (int i = 0; i < HourLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = HourLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Patient Type chart to Excel
        /// </summary>
        private int ExportPatientTypeChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "PHÂN TÍCH THEO LOẠI BỆNH NHÂN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Loại bệnh nhân";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (PatientTypeSeries?.Count > 0)
            {
                double total = 0;
                var patientTypes = new List<(string Label, double Value)>();

                // Extract data from pie chart series
                foreach (var series in PatientTypeSeries)
                {
                    if (series is LiveCharts.Wpf.PieSeries pieSeries &&
                        pieSeries.Values is LiveCharts.ChartValues<double> values &&
                        values.Count > 0)
                    {
                        string title = pieSeries.Title ?? "Không có tên";
                        double value = values[0];
                        total += value;
                        patientTypes.Add((title, value));
                    }
                }

                // Add data rows
                foreach (var item in patientTypes)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Label;
                    worksheet.Cell(startRow, 2).Value = item.Value;
                    worksheet.Cell(startRow, 3).Value = percentage / 100; // Format as percentage
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1;
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Top VIP Patients to Excel
        /// </summary>
        private int ExportTopVIPPatientsTable(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "TOP BỆNH NHÂN VIP (CHI TIÊU NHIỀU NHẤT)";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Họ và tên";
            worksheet.Cell(startRow, 3).Value = "Số điện thoại";
            worksheet.Cell(startRow, 4).Value = "Tổng chi tiêu";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (TopVIPPatients != null && TopVIPPatients.Count > 0)
            {
                foreach (var patient in TopVIPPatients)
                {
                    worksheet.Cell(startRow, 1).Value = patient.Id;
                    worksheet.Cell(startRow, 2).Value = patient.FullName;
                    worksheet.Cell(startRow, 3).Value = patient.Phone;
                    worksheet.Cell(startRow, 4).Value = patient.TotalSpending;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Value = TopVIPPatients.Sum(p => p.TotalSpending);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                startRow++;
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 4);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Appointment Status chart to Excel
        /// </summary>
        private int ExportAppointmentStatusChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "LỊCH HẸN THEO TRẠNG THÁI";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Trạng thái";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (AppointmentStatusSeries?.Count > 0 && AppointmentStatusLabels?.Length > 0)
            {
                var series = AppointmentStatusSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    double total = values.Sum();

                    for (int i = 0; i < AppointmentStatusLabels.Length; i++)
                    {
                        double value = i < values.Count ? values[i] : 0;
                        double percentage = total > 0 ? (value / total) * 100 : 0;

                        worksheet.Cell(startRow, 1).Value = AppointmentStatusLabels[i];
                        worksheet.Cell(startRow, 2).Value = value;
                        worksheet.Cell(startRow, 3).Value = percentage / 100;
                        worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = total;
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Value = 1;
                    worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Appointment Peak Hours chart to Excel
        /// </summary>
        private int ExportAppointmentPeakHoursChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "PEAK HOURS ĐẶT LỊCH NHIỀU NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Giờ";
            worksheet.Cell(startRow, 2).Value = "Số lịch hẹn";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (AppointmentPeakHoursSeries?.Count > 0)
            {
                var series = AppointmentPeakHoursSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    for (int i = 0; i < HourLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = HourLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Patients By Doctor chart to Excel
        /// </summary>
        private int ExportPatientsByDoctorChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "SỐ LƯỢNG BỆNH NHÂN CỦA TỪNG BÁC SĨ";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Bác sĩ";
            worksheet.Cell(startRow, 2).Value = "Số bệnh nhân";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (PatientsByStaffseries?.Count > 0 && DoctorLabels?.Length > 0)
            {
                var series = PatientsByStaffseries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    for (int i = 0; i < DoctorLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = DoctorLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Revenue By Category chart to Excel
        /// </summary>
        private int ExportRevenueByCategoryChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (RevenueByCategorySeries?.Count > 0 && CategoryLabels?.Length > 0)
            {
                var series = RevenueByCategorySeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    double total = values.Sum();

                    for (int i = 0; i < CategoryLabels.Length; i++)
                    {
                        double value = i < values.Count ? values[i] : 0;
                        double percentage = total > 0 ? (value / total) * 100 : 0;

                        worksheet.Cell(startRow, 1).Value = CategoryLabels[i];
                        worksheet.Cell(startRow, 2).Value = value;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        worksheet.Cell(startRow, 3).Value = percentage / 100;
                        worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                        startRow++;
                    }

                    // Add total row
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = total;
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 3).Value = 1;
                    worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Product Distribution chart to Excel
        /// </summary>
        private int ExportProductDistributionChart(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "PHÂN BỔ THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (ProductDistributionSeries?.Count > 0)
            {
                double total = 0;
                var categories = new List<(string Label, double Value)>();

                // Extract data from pie chart series
                foreach (var series in ProductDistributionSeries)
                {
                    if (series is LiveCharts.Wpf.PieSeries pieSeries &&
                        pieSeries.Values is LiveCharts.ChartValues<double> values &&
                        values.Count > 0)
                    {
                        string title = pieSeries.Title ?? "Không có tên";
                        double value = values[0];
                        total += value;
                        categories.Add((title, value));
                    }
                }

                // Add data rows
                foreach (var item in categories)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Label;
                    worksheet.Cell(startRow, 2).Value = item.Value;
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1;
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Top Selling Products to Excel
        /// </summary>
        private int ExportTopSellingProductsTable(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "TOP THUỐC BÁN CHẠY NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Danh mục";
            worksheet.Cell(startRow, 4).Value = "Doanh thu";
            worksheet.Cell(startRow, 5).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (TopSellingProducts != null && TopSellingProducts.Count > 0)
            {
                foreach (var product in TopSellingProducts)
                {
                    worksheet.Cell(startRow, 1).Value = product.Id;
                    worksheet.Cell(startRow, 2).Value = product.Name;
                    worksheet.Cell(startRow, 3).Value = product.Category;
                    worksheet.Cell(startRow, 4).Value = product.Sales;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 5).Value = product.Percentage / 100;
                    worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Value = TopSellingProducts.Sum(p => p.Sales);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(startRow, 5).Value = 1;
                worksheet.Cell(startRow, 5).Style.Font.Bold = true;
                worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 5);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Exports Warning Medicines to Excel
        /// </summary>
        private int ExportWarningMedicinesTable(IXLWorksheet worksheet, int startRow)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "CẢNH BÁO TỒN KHO";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Cảnh báo";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (WarningMedicines != null && WarningMedicines.Count > 0)
            {
                foreach (var medicine in WarningMedicines)
                {
                    worksheet.Cell(startRow, 1).Value = medicine.Id;
                    worksheet.Cell(startRow, 2).Value = medicine.Name;
                    worksheet.Cell(startRow, 3).Value = medicine.WarningMessage;

                    // Color-code warnings based on severity
                    if (medicine.WarningMessage.Contains("CẦN TIÊU HỦY"))
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    }
                    else if (medicine.WarningMessage.Contains("SẮP HẾT HẠN"))
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Orange;
                    }
                    else if (medicine.WarningMessage.Contains("TỒN KHO THẤP"))
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Blue;
                    }

                    startRow++;
                }
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có cảnh báo";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 3);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

   
        private int ExportPatientTypeChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Title, double Value)> patientTypes)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "PHÂN TÍCH THEO LOẠI BỆNH NHÂN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Loại bệnh nhân";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (patientTypes != null && patientTypes.Count > 0)
            {
                double total = patientTypes.Sum(pt => pt.Value);

                foreach (var item in patientTypes)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Title;
                    worksheet.Cell(startRow, 2).Value = item.Value;
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1;
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        private int ExportTopVIPPatientsTableFromCopy(IXLWorksheet worksheet, int startRow, List<VIPPatient> patients)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "TOP BỆNH NHÂN VIP (CHI TIÊU NHIỀU NHẤT)";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Họ và tên";
            worksheet.Cell(startRow, 3).Value = "Số điện thoại";
            worksheet.Cell(startRow, 4).Value = "Tổng chi tiêu";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (patients != null && patients.Count > 0)
            {
                foreach (var patient in patients)
                {
                    worksheet.Cell(startRow, 1).Value = patient.Id;
                    worksheet.Cell(startRow, 2).Value = patient.FullName;
                    worksheet.Cell(startRow, 3).Value = patient.Phone;
                    worksheet.Cell(startRow, 4).Value = patient.TotalSpending;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Value = patients.Sum(p => p.TotalSpending);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                startRow++;
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 4);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }
        private int ExportRevenueByCategoryChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Category, double Value)> categories)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (categories != null && categories.Count > 0)
            {
                double total = categories.Sum(c => c.Value);

                foreach (var category in categories)
                {
                    double percentage = total > 0 ? (category.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = category.Category;
                    worksheet.Cell(startRow, 2).Value = category.Value;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(startRow, 3).Value = 1;
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        private int ExportProductDistributionChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Title, double Value)> categories)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "PHÂN BỔ THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (categories != null && categories.Count > 0)
            {
                double total = categories.Sum(item => item.Value);

                foreach (var item in categories)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Title;
                    worksheet.Cell(startRow, 2).Value = item.Value;
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1;
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        private int ExportTopSellingProductsTableFromCopy(IXLWorksheet worksheet, int startRow, List<TopSellingProduct> products)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "TOP THUỐC BÁN CHẠY NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Danh mục";
            worksheet.Cell(startRow, 4).Value = "Doanh thu";
            worksheet.Cell(startRow, 5).Value = "Phần trăm";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (products != null && products.Count > 0)
            {
                foreach (var product in products)
                {
                    worksheet.Cell(startRow, 1).Value = product.Id;
                    worksheet.Cell(startRow, 2).Value = product.Name;
                    worksheet.Cell(startRow, 3).Value = product.Category;
                    worksheet.Cell(startRow, 4).Value = product.Sales;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 5).Value = product.Percentage / 100;
                    worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // Add total row
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Value = products.Sum(p => p.Sales);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(startRow, 5).Value = 1;
                worksheet.Cell(startRow, 5).Style.Font.Bold = true;
                worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 5);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }

        private int ExportWarningMedicinesTableFromCopy(IXLWorksheet worksheet, int startRow, List<WarningMedicine> medicines)
        {
            // Add section title
            worksheet.Cell(startRow, 1).Value = "CẢNH BÁO TỒN KHO";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // Add header row
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Cảnh báo";

            // Style header row
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // Add data rows
            if (medicines != null && medicines.Count > 0)
            {
                foreach (var medicine in medicines)
                {
                    worksheet.Cell(startRow, 1).Value = medicine.Id;
                    worksheet.Cell(startRow, 2).Value = medicine.Name;
                    worksheet.Cell(startRow, 3).Value = medicine.WarningMessage;

                    // Color-code warnings based on severity
                    if (medicine.WarningMessage?.Contains("CẦN TIÊU HỦY") == true)
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    }
                    else if (medicine.WarningMessage?.Contains("SẮP HẾT HẠN") == true)
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Orange;
                    }
                    else if (medicine.WarningMessage?.Contains("TỒN KHO THẤP") == true)
                    {
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Blue;
                    }

                    startRow++;
                }
            }
            else
            {
                worksheet.Cell(startRow, 1).Value = "Không có cảnh báo";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 3);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // Add empty row as separator
            startRow += 2;
            return startRow;
        }
        #endregion

        #endregion

        #region Model Classes
        public class TopSellingProduct
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public decimal Sales { get; set; }
            public int Percentage { get; set; }
        }

        public class VIPPatient
        {
            public int Id { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public decimal TotalSpending { get; set; }
        }

        public class WarningMedicine
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string WarningMessage { get; set; }
        }
        #endregion
    }
}
