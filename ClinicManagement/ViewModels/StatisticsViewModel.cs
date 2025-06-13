using ClinicManagement.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace ClinicManagement.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        #region Basic Properties

        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                LoadStatistics();
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
                LoadStatistics();
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

        // Growth Percentage
        private double _growthPercentage;
        public double GrowthPercentage
        {
            get => _growthPercentage;
            set
            {
                _growthPercentage = value;
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

        private string[] _revenueByMonthLabels;
        public string[] RevenueByMonthLabels
        {
            get => _revenueByMonthLabels;
            set
            {
                _revenueByMonthLabels = value;
                OnPropertyChanged();
            }
        }

        // Revenue by Location
        private SeriesCollection _revenueByLocationSeries;
        public SeriesCollection RevenueByLocationSeries
        {
            get => _revenueByLocationSeries;
            set
            {
                _revenueByLocationSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _revenueByLocationLabels;
        public string[] RevenueByLocationLabels
        {
            get => _revenueByLocationLabels;
            set
            {
                _revenueByLocationLabels = value;
                OnPropertyChanged();
            }
        }

        // Product Distribution Chart
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

        // Revenue by Time
        private SeriesCollection _revenueByTimeSeries;
        public SeriesCollection RevenueByTimeSeries
        {
            get => _revenueByTimeSeries;
            set
            {
                _revenueByTimeSeries = value;
                OnPropertyChanged();
            }
        }

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

        // Average Revenue Per Patient
        private SeriesCollection _avgRevenuePerPatientSeries;
        public SeriesCollection AvgRevenuePerPatientSeries
        {
            get => _avgRevenuePerPatientSeries;
            set
            {
                _avgRevenuePerPatientSeries = value;
                OnPropertyChanged();
            }
        }

        // Revenue Comparison Series
        private SeriesCollection _revenueComparisonSeries;
        public SeriesCollection RevenueComparisonSeries
        {
            get => _revenueComparisonSeries;
            set
            {
                _revenueComparisonSeries = value;
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

        // Total Patients Registered Series
        private SeriesCollection _totalPatientsRegisteredSeries;
        public SeriesCollection TotalPatientsRegisteredSeries
        {
            get => _totalPatientsRegisteredSeries;
            set
            {
                _totalPatientsRegisteredSeries = value;
                OnPropertyChanged();
            }
        }

        // New Patients By Time Series
        private SeriesCollection _newPatientsByTimeSeries;
        public SeriesCollection NewPatientsByTimeSeries
        {
            get => _newPatientsByTimeSeries;
            set
            {
                _newPatientsByTimeSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _newPatientsTimeLabels;
        public string[] NewPatientsTimeLabels
        {
            get => _newPatientsTimeLabels;
            set
            {
                _newPatientsTimeLabels = value;
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

        // Returning Patients Series
        private SeriesCollection _returningPatientsSeries;
        public SeriesCollection ReturningPatientsSeries
        {
            get => _returningPatientsSeries;
            set
            {
                _returningPatientsSeries = value;
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

        // Cancellation Rate Series
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
        private SeriesCollection _patientsByDoctorSeries;
        public SeriesCollection PatientsByDoctorSeries
        {
            get => _patientsByDoctorSeries;
            set
            {
                _patientsByDoctorSeries = value;
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

        // Completion Rate By Doctor Series
        private SeriesCollection _completionRateByDoctorSeries;
        public SeriesCollection CompletionRateByDoctorSeries
        {
            get => _completionRateByDoctorSeries;
            set
            {
                _completionRateByDoctorSeries = value;
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

        // Profit Margin Series
        private SeriesCollection _profitMarginSeries;
        public SeriesCollection ProfitMarginSeries
        {
            get => _profitMarginSeries;
            set
            {
                _profitMarginSeries = value;
                OnPropertyChanged();
            }
        }

        private string[] _topMedicineLabels;
        public string[] TopMedicineLabels
        {
            get => _topMedicineLabels;
            set
            {
                _topMedicineLabels = value;
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
        #endregion

        public StatisticsViewModel()
        {
            InitializeCommands();
            YFormatter = value => value.ToString("N0");
          //  LoadInitialData();
        }

        private void InitializeCommands()
        {
            RefreshCommand = new RelayCommand<object>(
                p => LoadStatistics(),
                p => true
            );

            FilterByDayCommand = new RelayCommand<object>(
                p => FilterByDay(),
                p => true
            );

            FilterByMonthCommand = new RelayCommand<object>(
                p => FilterByMonth(),
                p => true
            );

            FilterByQuarterCommand = new RelayCommand<object>(
                p => FilterByQuarter(),
                p => true
            );

            FilterByYearCommand = new RelayCommand<object>(
                p => FilterByYear(),
                p => true
            );

            ViewLowStockCommand = new RelayCommand<object>(
                p => ViewLowStock(),
                p => true
            );
        }

        //private void LoadInitialData()
        //{
        //    // Set initial data for displaying purposes until real data is loaded
        //    LoadRevenueByMonthSampleData();
        //    LoadRevenueByLocationSampleData();
        //    LoadProductDistributionSampleData();
        //    LoadTopSellingProductsSampleData();
        //    LoadRevenueByTimeSampleData();
        //    LoadInvoiceTypeSampleData();
        //    LoadServiceRevenueSampleData();
        //    LoadAvgRevenuePerPatientSampleData();
        //    LoadRevenueComparisonSampleData();
        //    LoadTopRevenueDaysSampleData();
        //    LoadRevenueTrendSampleData();
        //    LoadRevenueByHourSampleData();
        //    LoadTotalPatientsRegisteredSampleData();
        //    LoadNewPatientsByTimeSampleData();
        //    LoadTopVIPPatientsSampleData();
        //    LoadPatientTypeSampleData();
        //    LoadReturningPatientsSampleData();
        //    LoadAppointmentStatusSampleData();
        //    LoadCancellationRateSampleData();
        //    LoadAppointmentPeakHoursSampleData();
        //    LoadPatientsByDoctorSampleData();
        //    LoadCompletionRateByDoctorSampleData();
        //    LoadRevenueByCategorySampleData();
        //    LoadWarningMedicinesSampleData();
        //    LoadProfitMarginSampleData();

        //    // Load actual data
        //    LoadStatistics();
        //}

        public void LoadStatistics()
        {
            Task.Run(() =>
            {
                try
                {
                    var context = DataProvider.Instance.Context;

                    // Basic stats
                    LoadTodayAndMonthRevenue(context);
                    LoadTotalRevenue(context);
                    LoadPatientStats(context);
                    LoadAppointmentStats(context);
                    LoadMedicineStats(context);

                    // Revenue analytics
                    LoadRevenueByMonthData(context);
                    LoadRevenueByTimeData(context);
                    LoadInvoiceTypeData(context);
                    LoadServiceRevenueData(context);
                    LoadAvgRevenuePerPatientData(context);
                    LoadRevenueComparisonData(context);
                    LoadTopRevenueDaysData(context);
                    LoadRevenueTrendData(context);
                    LoadRevenueByHourData(context);

                    // Patient analytics
                    LoadTotalPatientsRegisteredData(context);
                    LoadNewPatientsByTimeData(context);
                    LoadTopVIPPatientsData(context);
                    LoadPatientTypeData(context);
                    LoadReturningPatientsData(context);

                    // Appointment analytics
                    LoadAppointmentStatusData(context);
                    LoadCancellationRateData(context);
                    LoadAppointmentPeakHoursData(context);
                    LoadPatientsByDoctorData(context);
                    LoadCompletionRateByDoctorData(context);

                    // Medicine analytics
                    LoadRevenueByCategoryData(context);
                    LoadProductDistributionData(context);
                    LoadTopSellingProductsData(context);
                    LoadProfitMarginData(context);
                    LoadLowStockData(context);

                    // Calculate growth trends
                    CalculateGrowthRates(context);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading statistics: {ex.Message}");
                    // Use sample data as fallback
                    //LoadInitialData();
                }
            });
        }

        #region Data Loading Methods
        //private void LoadRevenueByMonthSampleData()
        //{
        //    RevenueByMonthSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Mục tiêu",
        //            Values = new ChartValues<double> { 50, 60, 50, 66, 72, 58, 50, 43, 51, 45, 60, 50 },
        //            Fill = new SolidColorBrush(Color.FromRgb(98, 189, 250))
        //        },
        //        new LineSeries
        //        {
        //            Title = "Thực hiện",
        //            Values = new ChartValues<double> { 30, 38, 45, 60, 77, 80, 52, 44, 42, 47, 53, 67 },
        //            PointGeometry = DefaultGeometries.Circle,
        //            PointGeometrySize = 8,
        //            Stroke = new SolidColorBrush(Color.FromRgb(15, 97, 255)),
        //            Fill = Brushes.Transparent
        //        }
        //    };

        //    RevenueByMonthLabels = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
        //}

        //private void LoadRevenueByLocationSampleData()
        //{
        //    RevenueByLocationSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Mục tiêu",
        //            Values = new ChartValues<double> { 90, 70, 60, 50, 40 },
        //            Fill = new SolidColorBrush(Color.FromRgb(98, 189, 250))
        //        },
        //        new ColumnSeries
        //        {
        //            Title = "Thực hiện",
        //            Values = new ChartValues<double> { 80, 65, 50, 40, 30 },
        //            Fill = new SolidColorBrush(Color.FromRgb(15, 97, 255))
        //        }
        //    };

        //    RevenueByLocationLabels = new[] { "VP HCM", "VP Hà Nội", "VP Đà Nẵng", "VP Buôn Mê Thuột", "VP Cần Thơ" };
        //}

        //private void LoadProductDistributionSampleData()
        //{
        //    ProductDistributionSeries = new SeriesCollection
        //    {
        //        new PieSeries
        //        {
        //            Title = "HMG1",
        //            Values = new ChartValues<double> { 35.8 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"HMG1: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(66, 133, 244))
        //        },
        //        new PieSeries
        //        {
        //            Title = "HMG2",
        //            Values = new ChartValues<double> { 20.9 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"HMG2: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(219, 68, 55))
        //        },
        //        new PieSeries
        //        {
        //            Title = "HMG3",
        //            Values = new ChartValues<double> { 19.1 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"HMG3: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(244, 180, 0))
        //        },
        //        new PieSeries
        //        {
        //            Title = "HMG4",
        //            Values = new ChartValues<double> { 15.2 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"HMG4: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(15, 157, 88))
        //        },
        //        new PieSeries
        //        {
        //            Title = "HMG5",
        //            Values = new ChartValues<double> { 9.0 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"HMG5: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(171, 71, 188))
        //        }
        //    };
        //}

        //private void LoadTopSellingProductsSampleData()
        //{
        //    TopSellingProducts = new ObservableCollection<TopSellingProduct>
        //    {
        //        new TopSellingProduct { Name = "Kháng sinh Rifampin", Category = "HMG1", Sales = 450000000, Percentage = 15 },
        //        new TopSellingProduct { Name = "Dung dịch sát khuẩn", Category = "HMG2", Sales = 350000000, Percentage = 14 },
        //        new TopSellingProduct { Name = "Thuốc chống viêm Ibuprofen", Category = "HMG3", Sales = 300000000, Percentage = 13 },
        //        new TopSellingProduct { Name = "Dung dịch rửa vết thương", Category = "HMG4", Sales = 300000000, Percentage = 12 },
        //        new TopSellingProduct { Name = "Tấm ALC panel 2 lớp", Category = "HMG5", Sales = 300000000, Percentage = 12 },
        //        new TopSellingProduct { Name = "Tấm panel chống cháy", Category = "HMG6", Sales = 300000000, Percentage = 12 }
        //    };
        //}

        //private void LoadRevenueByTimeSampleData()
        //{
        //    RevenueByTimeSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Doanh thu",
        //            Values = new ChartValues<double> { 500000, 600000, 750000, 850000, 900000, 1100000, 950000, 800000, 850000, 700000, 650000, 700000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };

        //    RevenueByTimeLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
        //}

        //private void LoadInvoiceTypeSampleData()
        //{
        //    InvoiceTypeSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Doanh thu",
        //            Values = new ChartValues<double> { 1200000, 800000, 1500000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        }
        //    };

        //    InvoiceTypeLabels = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };
        //}

        //private void LoadServiceRevenueSampleData()
        //{
        //    ServiceRevenueSeries = new SeriesCollection
        //    {
        //        new PieSeries
        //        {
        //            Title = "Khám bệnh",
        //            Values = new ChartValues<double> { 40 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"Khám bệnh: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        },
        //        new PieSeries
        //        {
        //            Title = "Bán thuốc",
        //            Values = new ChartValues<double> { 25 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"Bán thuốc: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
        //        },
        //        new PieSeries
        //        {
        //            Title = "Khám và bán thuốc",
        //            Values = new ChartValues<double> { 35 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"Khám và bán thuốc: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };
        //}

        //private void LoadAvgRevenuePerPatientSampleData()
        //{
        //    AvgRevenuePerPatientSeries = new SeriesCollection
        //    {
        //        new LineSeries
        //        {
        //            Title = "Doanh thu bình quân",
        //            Values = new ChartValues<double> { 250000, 280000, 300000, 290000, 310000, 350000, 330000, 340000, 330000, 350000, 360000, 380000 },
        //            PointGeometry = DefaultGeometries.Circle,
        //            PointGeometrySize = 8,
        //            LineSmoothness = 0.5,
        //            Stroke = new SolidColorBrush(Color.FromRgb(156, 39, 176))
        //        }
        //    };
        //}

        //private void LoadRevenueComparisonSampleData()
        //{
        //    RevenueComparisonSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "2024",
        //            Values = new ChartValues<double> { 500000, 600000, 750000, 850000, 900000, 1100000, 950000, 800000, 850000, 700000, 650000, 700000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        },
        //        new ColumnSeries
        //        {
        //            Title = "2023",
        //            Values = new ChartValues<double> { 450000, 550000, 700000, 800000, 850000, 1000000, 900000, 750000, 820000, 670000, 600000, 650000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(180, 180, 180))
        //        }
        //    };

        //    RevenueComparisonLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
        //}

        //private void LoadTopRevenueDaysSampleData()
        //{
        //    TopRevenueDaysSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Doanh thu",
        //            Values = new ChartValues<double> { 1200000, 1150000, 1100000, 1050000, 1000000, 950000, 900000, 850000, 800000, 750000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        }
        //    };

        //    TopRevenueDaysLabels = new[] { "15/06", "20/06", "10/06", "25/06", "05/06", "01/06", "28/06", "12/06", "18/06", "22/06" };
        //}

        //private void LoadRevenueTrendSampleData()
        //{
        //    RevenueTrendSeries = new SeriesCollection
        //    {
        //        new LineSeries
        //        {
        //            Title = "Xu hướng doanh thu",
        //            Values = new ChartValues<double> { 500000, 520000, 540000, 580000, 620000, 680000, 700000, 720000, 740000, 760000, 780000, 800000 },
        //            PointGeometry = null,
        //            LineSmoothness = 1,
        //            Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)),
        //            Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54))
        //        }
        //    };

        //    RevenueTrendLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
        //}

        //private void LoadRevenueByHourSampleData()
        //{
        //    RevenueByHourSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Doanh thu theo giờ",
        //            Values = new ChartValues<double> { 10000, 5000, 2000, 0, 0, 0, 10000, 50000, 100000, 120000, 150000, 180000, 200000, 190000, 170000, 150000, 130000, 110000, 80000, 60000, 40000, 30000, 20000, 15000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
        //        }
        //    };
        //}

        //private void LoadTotalPatientsRegisteredSampleData()
        //{
        //    TotalPatientsRegisteredSeries = new SeriesCollection
        //    {
        //        new LineSeries
        //        {
        //            Title = "Tổng số bệnh nhân",
        //            Values = new ChartValues<int> { 120, 150, 180, 210, 250, 280, 310, 350, 390, 420, 450, 480 },
        //            PointGeometry = DefaultGeometries.Circle,
        //            PointGeometrySize = 8,
        //            Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };
        //}

        //private void LoadNewPatientsByTimeSampleData()
        //{
        //    NewPatientsByTimeSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Bệnh nhân mới",
        //            Values = new ChartValues<int> { 30, 35, 40, 38, 42, 45, 40, 48, 52, 55, 58, 62 },
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };

        //    NewPatientsTimeLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
        //}

        //private void LoadTopVIPPatientsSampleData()
        //{
        //    TopVIPPatients = new ObservableCollection<VIPPatient>
        //    {
        //        new VIPPatient { FullName = "Nguyễn Văn A", Phone = "0901234567", TotalSpending = 5000000 },
        //        new VIPPatient { FullName = "Trần Thị B", Phone = "0912345678", TotalSpending = 4500000 },
        //        new VIPPatient { FullName = "Lê Văn C", Phone = "0923456789", TotalSpending = 4000000 },
        //        new VIPPatient { FullName = "Phạm Thị D", Phone = "0934567890", TotalSpending = 3800000 },
        //        new VIPPatient { FullName = "Hoàng Văn E", Phone = "0945678901", TotalSpending = 3500000 }
        //    };
        //}

        //private void LoadPatientTypeSampleData()
        //{
        //    PatientTypeSeries = new SeriesCollection
        //    {
        //        new PieSeries
        //        {
        //            Title = "VIP",
        //            Values = new ChartValues<double> { 15 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"VIP: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54))
        //        },
        //        new PieSeries
        //        {
        //            Title = "Bảo hiểm",
        //            Values = new ChartValues<double> { 40 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"Bảo hiểm: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        },
        //        new PieSeries
        //        {
        //            Title = "Thường",
        //            Values = new ChartValues<double> { 45 },
        //            DataLabels = true,
        //            LabelPoint = chartPoint => $"Thường: {chartPoint.Y}%",
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        }
        //    };
        //}

        //private void LoadReturningPatientsSampleData()
        //{
        //    ReturningPatientsSeries = new SeriesCollection
        //    {
        //        new LineSeries
        //        {
        //            Title = "Tỷ lệ tái khám",
        //            Values = new ChartValues<double> { 35, 38, 40, 42, 45, 48, 50, 52, 55, 57, 60, 62 },
        //            PointGeometry = DefaultGeometries.Circle,
        //            PointGeometrySize = 8,
        //            LineSmoothness = 0.5,
        //            Stroke = new SolidColorBrush(Color.FromRgb(156, 39, 176))
        //        }
        //    };
        //}

        //private void LoadAppointmentStatusSampleData()
        //{
        //    AppointmentStatusSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Số lượng",
        //            Values = new ChartValues<double> { 80, 15, 5 },
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };

        //    AppointmentStatusLabels = new[] { "Hoàn thành", "Hủy bỏ", "Đã hẹn" };
        //}

        //private void LoadCancellationRateSampleData()
        //{
        //    CancellationRateSeries = new SeriesCollection
        //    {
        //        new LineSeries
        //        {
        //            Title = "Tỷ lệ hủy lịch hẹn",
        //            Values = new ChartValues<double> { 15, 14, 12, 13, 11, 10, 9, 8, 7, 8, 7, 6 },
        //            PointGeometry = DefaultGeometries.Circle,
        //            PointGeometrySize = 8,
        //            LineSmoothness = 0.5,
        //            Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54))
        //        }
        //    };
        //}

        //private void LoadAppointmentPeakHoursSampleData()
        //{
        //    AppointmentPeakHoursSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Số lịch hẹn",
        //            Values = new ChartValues<double> { 0, 0, 0, 0, 0, 0, 0, 5, 10, 15, 20, 25, 30, 25, 20, 15, 10, 5, 0, 0, 0, 0, 0, 0 },
        //            Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
        //        }
        //    };
        //}

        //private void LoadPatientsByDoctorSampleData()
        //{
        //    PatientsByDoctorSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Số bệnh nhân",
        //            Values = new ChartValues<double> { 120, 100, 90, 80, 70 },
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        }
        //    };

        //    DoctorLabels = new[] { "Bs. Nguyễn Văn A", "Bs. Trần Thị B", "Bs. Lê Văn C", "Bs. Phạm Thị D", "Bs. Hoàng Văn E" };
        //}

        //private void LoadCompletionRateByDoctorSampleData()
        //{
        //    CompletionRateByDoctorSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Tỷ lệ hoàn thành",
        //            Values = new ChartValues<double> { 95, 93, 90, 88, 85 },
        //            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
        //        }
        //    };
        //}

        //private void LoadRevenueByCategorySampleData()
        //{
        //    RevenueByCategorySeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Doanh thu",
        //            Values = new ChartValues<double> { 1200000, 900000, 800000, 700000, 500000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(156, 39, 176))
        //        }
        //    };

        //    CategoryLabels = new[] { "Thuốc kháng sinh", "Thuốc giảm đau", "Vitamin", "Dụng cụ y tế", "Khác" };
        //}

        //private void LoadWarningMedicinesSampleData()
        //{
        //    WarningMedicines = new ObservableCollection<WarningMedicine>
        //    {
        //        new WarningMedicine { Name = "Paracetamol 500mg", WarningMessage = "Còn 10 đơn vị - Dưới mức tối thiểu" },
        //        new WarningMedicine { Name = "Amoxicillin 500mg", WarningMessage = "Hết hạn trong 15 ngày" },
        //        new WarningMedicine { Name = "Vitamin C 1000mg", WarningMessage = "Không bán trong 30 ngày qua" },
        //        new WarningMedicine { Name = "Omeprazole 20mg", WarningMessage = "Còn 5 đơn vị - Dưới mức tối thiểu" }
        //    };

        //    LowStockCount = WarningMedicines.Count;
        //}

        //private void LoadProfitMarginSampleData()
        //{
        //    ProfitMarginSeries = new SeriesCollection
        //    {
        //        new ColumnSeries
        //        {
        //            Title = "Giá nhập",
        //            Values = new ChartValues<double> { 20000, 15000, 40000, 30000, 25000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54))
        //        },
        //        new ColumnSeries
        //        {
        //            Title = "Giá bán",
        //            Values = new ChartValues<double> { 30000, 22000, 60000, 45000, 40000 },
        //            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
        //        }
        //    };

        //    TopMedicineLabels = new[] { "Paracetamol", "Vitamin C", "Amoxicillin", "Omeprazole", "Ibuprofen" };
        //}

        private void LoadTodayAndMonthRevenue(DbContext context)
        {
            var today = DateTime.Today;
            var firstDayOfMonth = new DateTime(today.Year, today.Month, 1);

            // Today's revenue
            var todayRevenue = context.Set<Invoice>()
                .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value.Date == today && i.Status == "Đã thanh toán")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0;
            TodayRevenue = todayRevenue;

            // Month's revenue
            var monthRevenue = context.Set<Invoice>()
                .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value >= firstDayOfMonth && i.InvoiceDate.Value <= today && i.Status == "Đã thanh toán")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0;
            MonthRevenue = monthRevenue;

            // Count new patients this month
            NewPatients = context.Set<Patient>()
                .Count(p => p.CreatedAt >= firstDayOfMonth && p.CreatedAt <= today && p.IsDeleted != true);

            // Calculate today's growth (compared to yesterday)
            var yesterday = today.AddDays(-1);
            var yesterdayRevenue = context.Set<Invoice>()
                .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value.Date == yesterday && i.Status == "Đã thanh toán")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0;

            if (yesterdayRevenue > 0)
            {
                GrowthPercentage = (double)((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100);
            }
            else if (todayRevenue > 0)
            {
                GrowthPercentage = 100;
            }
            else
            {
                GrowthPercentage = 0;
            }
        }

        private void LoadTotalRevenue(DbContext context)
        {
            var totalRevenue = context.Set<Invoice>()
                .Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate && i.Status == "Đã thanh toán")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0;

            TotalRevenue = totalRevenue;
        }

        private void LoadPatientStats(DbContext context)
        {
            TotalPatients = context.Set<Patient>()
                .Count(p => p.CreatedAt >= StartDate && p.CreatedAt <= EndDate && p.IsDeleted != true);
        }

        private void LoadAppointmentStats(DbContext context)
        {
            TotalAppointments = context.Set<Appointment>()
                .Count(a => a.AppointmentDate >= StartDate && a.AppointmentDate <= EndDate);
        }

        private void LoadMedicineStats(DbContext context)
        {
            var invoiceDetails = context.Set<InvoiceDetail>()
                .Include(id => id.Invoice)
                .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                       id.Invoice.InvoiceDate <= EndDate &&
                       id.Invoice.Status == "Đã thanh toán" &&
                       id.MedicineId != null)
                .ToList();

            TotalMedicineSold = invoiceDetails.Sum(id => id.Quantity ?? 0);
        }

        private void LoadRevenueByMonthData(DbContext context)
        {
            var currentYear = DateTime.Now.Year;
            var monthlyRevenue = new double[12];

            var invoices = context.Set<Invoice>()
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

            // Update chart with real data
            var lineSeries = RevenueByMonthSeries[1] as LineSeries;
            if (lineSeries != null)
            {
                lineSeries.Values = new ChartValues<double>(monthlyRevenue);
            }
        }

        private void LoadRevenueByLocationData(DbContext context)
        {
            // In a real application, you would get this data from the database
            // For now, we'll use the sample data
        }

        private void LoadProductDistributionData(DbContext context)
        {
            var medicineCategories = context.Set<MedicineCategory>()
                .Where(c => c.IsDeleted != true)
                .Take(5)
                .ToList();

            var invoiceDetails = context.Set<InvoiceDetail>()
                .Include(id => id.Invoice)
                .Include(id => id.Medicine)
                .ThenInclude(m => m.Category)
                .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                       id.Invoice.InvoiceDate <= EndDate &&
                       id.Invoice.Status == "Đã thanh toán" &&
                       id.MedicineId != null)
                .ToList();

            // Group by category and calculate total sales
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

            if (totalSales > 0)
            {
                var newSeries = new SeriesCollection();

                foreach (var category in categorySales)
                {
                    double percentage = (double)((category.TotalSales / totalSales) * 100);

                    newSeries.Add(new PieSeries
                    {
                        Title = category.CategoryName,
                        Values = new ChartValues<double> { Math.Round(percentage, 1) },
                        DataLabels = true,
                        LabelPoint = chartPoint => $"{category.CategoryName}: {chartPoint.Y}%",
                        Fill = GetRandomBrush()
                    });
                }

                if (newSeries.Count > 0)
                {
                    ProductDistributionSeries = newSeries;
                }
            }
        }

        private void LoadTopSellingProductsData(DbContext context)
        {
            var invoiceDetails = context.Set<InvoiceDetail>()
                .Include(id => id.Invoice)
                .Include(id => id.Medicine)
                .ThenInclude(m => m.Category)
                .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                       id.Invoice.InvoiceDate <= EndDate &&
                       id.Invoice.Status == "Đã thanh toán" &&
                       id.MedicineId != null)
                .ToList();

            // Group by medicine and calculate total sales
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

                TopSellingProducts = new ObservableCollection<TopSellingProduct>(medicineSales);
            }
        }

        private void LoadLowStockData(DbContext context)
        {
            // Get medicines with low stock (less than 10 units)
            var lowStockMedicines = context.Set<Medicine>()
                .Where(m => m.IsDeleted != true && m.TotalStockQuantity < 10)
                .Take(10)
                .ToList();

            // Get medicines that expire within 30 days
            var expiringMedicines = context.Set<StockIn>()
                .Include(si => si.Medicine)
                .Where(si => si.Medicine.ExpiryDate.HasValue &&

                    si.Medicine.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) <= DateTime.Now.AddDays(30) &&
                        si.Medicine.IsDeleted != true)
                .Take(10)
                .ToList();

            var warningList = new List<WarningMedicine>();

            foreach (var medicine in lowStockMedicines)
            {
                warningList.Add(new WarningMedicine
                {
                    Id = medicine.MedicineId,
                    Name = medicine.Name,
                    WarningMessage = $"Còn {medicine.TotalStockQuantity} đơn vị - Dưới mức tối thiểu"
                });
            }

            foreach (var stockIn in expiringMedicines)
            {
                if (stockIn.Medicine.ExpiryDate.HasValue && stockIn.Medicine != null)
                {
                    int daysUntilExpiry = (stockIn.Medicine.ExpiryDate.Value.ToDateTime(TimeOnly.MinValue) - DateTime.Now).Days;

                    warningList.Add(new WarningMedicine
                    {
                        Id = stockIn.Medicine.MedicineId,
                        Name = stockIn.Medicine.Name,
                        WarningMessage = $"Hết hạn trong {daysUntilExpiry} ngày"
                    });
                }
            }

            // Remove duplicates (same medicine with different warnings)
            var distinctWarnings = warningList
                .GroupBy(w => w.Id)
                .Select(g => g.First())
                .Take(10)
                .ToList();

            WarningMedicines = new ObservableCollection<WarningMedicine>(distinctWarnings);
            LowStockCount = distinctWarnings.Count;
        }

        private void CalculateGrowthRates(DbContext context)
        {
            // Calculate revenue growth compared to previous period
            var previousPeriodStart = StartDate.AddDays(-(EndDate - StartDate).TotalDays);
            var previousPeriodEnd = StartDate.AddDays(-1);

            var currentRevenue = TotalRevenue;
            var previousRevenue = context.Set<Invoice>()
                .Where(i => i.InvoiceDate >= previousPeriodStart && i.InvoiceDate <= previousPeriodEnd && i.Status == "Đã thanh toán")
                .Sum(i => (decimal?)i.TotalAmount) ?? 0m;

            if (previousRevenue > 0)
            {
                var revenueGrowth = ((currentRevenue - previousRevenue) / previousRevenue) * 100;
                RevenueGrowth = $"{revenueGrowth:+0.0;-0.0}%";
                GrowthPercentage = (double)revenueGrowth;
            }
            else
            {
                RevenueGrowth = "N/A";
                GrowthPercentage = 0;
            }

            // Calculate patient growth
            var previousPatients = context.Set<Patient>()
                .Count(p => p.CreatedAt >= previousPeriodStart && p.CreatedAt <= previousPeriodEnd && p.IsDeleted != true);

            if (previousPatients > 0)
            {
                var patientGrowth = ((TotalPatients - previousPatients) / (double)previousPatients) * 100;
                PatientGrowth = $"{patientGrowth:+0.0;-0.0}%";
            }
            else
            {
                PatientGrowth = "N/A";
            }
        }
        #endregion

        #region Real Data Loading Methods

        private void LoadRevenueByTimeData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = new double[12];

                // Get monthly revenue
                var invoices = context.Set<Invoice>()
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


                RevenueByTimeSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Doanh thu",
                        Values = new ChartValues<double>(monthlyRevenue),
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    }
                };

                RevenueByTimeLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading revenue by time data: {ex.Message}");
                //LoadRevenueByTimeSampleData(); // Fallback to sample data
            }
        }

        private void LoadInvoiceTypeData(DbContext context)
        {
            try
            {
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };
                var revenueByType = new double[invoiceTypes.Length];

                var invoices = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate && i.Status == "Đã thanh toán")
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

                InvoiceTypeSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Doanh thu",
                        Values = new ChartValues<double>(revenueByType),
                        Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                    }
                };

                InvoiceTypeLabels = invoiceTypes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading invoice type data: {ex.Message}");
                //LoadInvoiceTypeSampleData(); // Fallback to sample data
            }
        }

        private void LoadServiceRevenueData(DbContext context)
        {
            try
            {
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };
                var totalRevenue = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate && i.Status == "Đã thanh toán")
                    .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                if (totalRevenue > 0)
                {
                    var seriesCollection = new SeriesCollection();

                    foreach (var type in invoiceTypes)
                    {
                        var typeRevenue = context.Set<Invoice>()
                            .Where(i => i.InvoiceDate >= StartDate &&
                                  i.InvoiceDate <= EndDate &&
                                  i.Status == "Đã thanh toán" &&
                                  i.InvoiceType == type)
                            .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                        if (typeRevenue > 0)
                        {
                            double percentage = Math.Round((double)((typeRevenue / totalRevenue) * 100), 1);

                            var brushColor = type switch
                            {
                                "Khám bệnh" => Color.FromRgb(76, 175, 80), // Green
                                "Bán thuốc" => Color.FromRgb(255, 152, 0), // Orange
                                _ => Color.FromRgb(33, 150, 243) // Blue
                            };

                            seriesCollection.Add(new PieSeries
                            {
                                Title = type,
                                Values = new ChartValues<double> { percentage },
                                DataLabels = true,
                                LabelPoint = chartPoint => $"{type}: {chartPoint.Y}%",
                                Fill = new SolidColorBrush(brushColor)
                            });
                        }
                    }

                    ServiceRevenueSeries = seriesCollection;
                }
                else
                {
                    // No revenue, use sample data
                    //LoadServiceRevenueSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading service revenue data: {ex.Message}");
             //   LoadServiceRevenueSampleData(); // Fallback to sample data
            }
        }

        private void LoadAvgRevenuePerPatientData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var avgRevenueByMonth = new double[12];

                for (int month = 1; month <= 12; month++)
                {
                    var startDate = new DateTime(currentYear, month, 1);
                    var endDate = startDate.AddMonths(1).AddDays(-1);

                    // Get total revenue for the month
                    var totalRevenue = context.Set<Invoice>()
                        .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate && i.Status == "Đã thanh toán")
                        .Sum(i => (decimal?)i.TotalAmount) ?? 0;

                    // Get unique patients for the month
                    var uniquePatients = context.Set<Invoice>()
                        .Where(i => i.InvoiceDate >= startDate && i.InvoiceDate <= endDate && i.Status == "Đã thanh toán" && i.PatientId != null)
                        .Select(i => i.PatientId)
                        .Distinct()
                        .Count();

                    // Calculate average revenue per patient
                    avgRevenueByMonth[month - 1] = uniquePatients > 0 ? (double)(totalRevenue / uniquePatients) : 0;
                }

                AvgRevenuePerPatientSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Doanh thu bình quân",
                        Values = new ChartValues<double>(avgRevenueByMonth),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        LineSmoothness = 0.5,
                        Stroke = new SolidColorBrush(Color.FromRgb(156, 39, 176))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading average revenue per patient data: {ex.Message}");
              //  LoadAvgRevenuePerPatientSampleData(); // Fallback to sample data
            }
        }

        private void LoadRevenueComparisonData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var previousYear = currentYear - 1;
                var currentYearRevenue = new double[12];
                var previousYearRevenue = new double[12];

                // Current year revenue
                var currentYearInvoices = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value.Year == currentYear && i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in currentYearInvoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int month = invoice.InvoiceDate.Value.Month - 1;
                        currentYearRevenue[month] += (double)invoice.TotalAmount;
                    }
                }

                // Previous year revenue
                var previousYearInvoices = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate.HasValue && i.InvoiceDate.Value.Year == previousYear && i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in previousYearInvoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int month = invoice.InvoiceDate.Value.Month - 1;
                        previousYearRevenue[month] += (double)invoice.TotalAmount;
                    }
                }

                RevenueComparisonSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = currentYear.ToString(),
                        Values = new ChartValues<double>(currentYearRevenue),
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    },
                    new ColumnSeries
                    {
                        Title = previousYear.ToString(),
                        Values = new ChartValues<double>(previousYearRevenue),
                        Fill = new SolidColorBrush(Color.FromRgb(180, 180, 180))
                    }
                };

                RevenueComparisonLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading revenue comparison data: {ex.Message}");
                //LoadRevenueComparisonSampleData(); // Fallback to sample data
            }
        }

        private void LoadTopRevenueDaysData(DbContext context)
        {
            try
            {
                // Get top 10 revenue days within the selected date range
                var topRevenueDays = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate && i.Status == "Đã thanh toán")
                    .GroupBy(i => i.InvoiceDate.Value.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(i => (decimal?)i.TotalAmount) ?? 0.0m })

                    .OrderByDescending(x => x.Revenue)
                    .Take(10)
                    .ToList();

                if (topRevenueDays.Any())
                {
                    var values = topRevenueDays.Select(x => (double)x.Revenue).ToArray();
                    var labels = topRevenueDays.Select(x => x.Date.ToString("dd/MM")).ToArray();

                    TopRevenueDaysSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Doanh thu",
                            Values = new ChartValues<double>(values),
                            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        }
                    };

                    TopRevenueDaysLabels = labels;
                }
                else
                {
                    //// No data, use sample data
                    //LoadTopRevenueDaysSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading top revenue days data: {ex.Message}");
                //LoadTopRevenueDaysSampleData(); // Fallback to sample data
            }
        }

        private void LoadRevenueTrendData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = new double[12];

                // Get monthly revenue
                var invoices = context.Set<Invoice>()
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
                        Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54))
                    }
                };

                RevenueTrendLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading revenue trend data: {ex.Message}");
          //      LoadRevenueTrendSampleData(); // Fallback to sample data
            }
        }

        private void LoadRevenueByHourData(DbContext context)
        {
            try
            {
                var revenueByHour = new double[24];

                // Get invoices within date range
                var invoices = context.Set<Invoice>()
                    .Where(i => i.InvoiceDate >= StartDate && i.InvoiceDate <= EndDate && i.Status == "Đã thanh toán")
                    .ToList();

                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int hour = invoice.InvoiceDate.Value.Hour;
                        revenueByHour[hour] += (double)invoice.TotalAmount;
                    }
                }

                RevenueByHourSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Doanh thu theo giờ",
                        Values = new ChartValues<double>(revenueByHour),
                        Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading revenue by hour data: {ex.Message}");
                //LoadRevenueByHourSampleData(); // Fallback to sample data
            }
        }

        private void LoadTotalPatientsRegisteredData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var patientsByMonth = new int[12];

                // Calculate cumulative patients by month
                for (int month = 1; month <= 12; month++)
                {
                    var endOfMonth = new DateTime(currentYear, month, DateTime.DaysInMonth(currentYear, month));
                    patientsByMonth[month - 1] = context.Set<Patient>()
                        .Count(p => p.CreatedAt <= endOfMonth && p.IsDeleted != true);
                }

                TotalPatientsRegisteredSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Tổng số bệnh nhân",
                        Values = new ChartValues<int>(patientsByMonth),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        Stroke = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading total patients registered data: {ex.Message}");
           //     LoadTotalPatientsRegisteredSampleData(); // Fallback to sample data
            }
        }

        private void LoadNewPatientsByTimeData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var newPatientsByMonth = new int[12];

                for (int month = 1; month <= 12; month++)
                {
                    var startOfMonth = new DateTime(currentYear, month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    newPatientsByMonth[month - 1] = context.Set<Patient>()
                        .Count(p => p.CreatedAt >= startOfMonth && p.CreatedAt <= endOfMonth && p.IsDeleted != true);
                }

                NewPatientsByTimeSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Bệnh nhân mới",
                        Values = new ChartValues<int>(newPatientsByMonth),
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    }
                };

                NewPatientsTimeLabels = new[] { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading new patients by time data: {ex.Message}");
              //  LoadNewPatientsByTimeSampleData(); // Fallback to sample data
            }
        }

        private void LoadTopVIPPatientsData(DbContext context)
        {
            try
            {
                // Get top spending patients
                var topPatients = context.Set<Invoice>()
                    .Where(i => i.Status == "Đã thanh toán" && i.PatientId != null)
                    .GroupBy(i => i.PatientId)
                    .Select(g => new
                    {
                        PatientId = g.Key,
                        TotalSpending = g.Sum(i => (decimal?)i.TotalAmount ?? 0.0m)
                    })
                    .OrderByDescending(x => x.TotalSpending)
                    .Take(5)
                    .ToList();

                if (topPatients.Any())
                {
                    var vipPatients = new List<VIPPatient>();

                    foreach (var patient in topPatients)
                    {
                        var patientInfo = context.Set<Patient>()
                            .FirstOrDefault(p => p.PatientId == patient.PatientId);

                        if (patientInfo != null)
                        {
                            vipPatients.Add(new VIPPatient
                            {
                                Id = patientInfo.PatientId,
                                FullName = patientInfo.FullName,
                                Phone = patientInfo.Phone,
                                TotalSpending = patient.TotalSpending
                            });
                        }
                    }

                    TopVIPPatients = new ObservableCollection<VIPPatient>(vipPatients);
                }
                else
                {
                    //// No data, use sample data
                    //LoadTopVIPPatientsSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading top VIP patients data: {ex.Message}");
                //LoadTopVIPPatientsSampleData(); // Fallback to sample data
            }
        }

        private void LoadPatientTypeData(DbContext context)
        {
            try
            {
                // Get patient types from database
                var patientTypes = context.Set<PatientType>().ToList();

                if (patientTypes.Any())
                {
                    var seriesCollection = new SeriesCollection();
                    var totalPatients = context.Set<Patient>()
                        .Count(p => p.IsDeleted != true);

                    if (totalPatients > 0)
                    {
                        foreach (var type in patientTypes)
                        {
                            var patientCount = context.Set<Patient>()
                                .Count(p => p.PatientTypeId == type.PatientTypeId && p.IsDeleted != true);

                            double percentage = Math.Round((double)(patientCount * 100) / totalPatients, 1);

                            if (percentage > 0)
                            {
                                // Assign different colors based on type name or id
                                var colorIndex = type.PatientTypeId % 5;
                                var colors = new[]
                                {
                                    Color.FromRgb(244, 67, 54),  // Red
                                    Color.FromRgb(33, 150, 243), // Blue
                                    Color.FromRgb(76, 175, 80),  // Green
                                    Color.FromRgb(255, 152, 0),  // Orange
                                    Color.FromRgb(156, 39, 176)  // Purple
                                };

                                seriesCollection.Add(new PieSeries
                                {
                                    Title = type.TypeName,
                                    Values = new ChartValues<double> { percentage },
                                    DataLabels = true,
                                    LabelPoint = chartPoint => $"{type.TypeName}: {chartPoint.Y}%",
                                    Fill = new SolidColorBrush(colors[colorIndex])
                                });
                            }
                        }

                        PatientTypeSeries = seriesCollection;
                    }
                    else
                    {
                        //// No patients, use sample data
                        //LoadPatientTypeSampleData();
                    }
                }
                else
                {
                    //// No patient types, use sample data
                    //LoadPatientTypeSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patient type data: {ex.Message}");
                //LoadPatientTypeSampleData(); // Fallback to sample data
            }
        }

        private void LoadReturningPatientsData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var returningRateByMonth = new double[12];

                for (int month = 1; month <= 12; month++)
                {
                    var startOfMonth = new DateTime(currentYear, month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    // Count patients who had appointments/invoices this month
                    var thisMonthPatientIds = context.Set<Invoice>()
                        .Where(i => i.InvoiceDate >= startOfMonth && i.InvoiceDate <= endOfMonth && i.PatientId != null)
                        .Select(i => i.PatientId)
                        .Distinct()
                        .ToList();

                    int totalPatientsThisMonth = thisMonthPatientIds.Count;

                    if (totalPatientsThisMonth > 0)
                    {
                        // Count how many of these patients had previous appointments
                        var patientsWithPreviousVisits = context.Set<Invoice>()
                            .Where(i => i.InvoiceDate < startOfMonth && thisMonthPatientIds.Contains(i.PatientId))
                            .Select(i => i.PatientId)
                            .Distinct()
                            .Count();

                        returningRateByMonth[month - 1] = Math.Round((double)(patientsWithPreviousVisits * 100) / totalPatientsThisMonth, 1);
                    }
                    else
                    {
                        returningRateByMonth[month - 1] = 0;
                    }
                }

                ReturningPatientsSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Tỷ lệ tái khám",
                        Values = new ChartValues<double>(returningRateByMonth),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        LineSmoothness = 0.5,
                        Stroke = new SolidColorBrush(Color.FromRgb(156, 39, 176))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading returning patients data: {ex.Message}");
                //LoadReturningPatientsSampleData(); // Fallback to sample data
            }
        }

        private void LoadAppointmentStatusData(DbContext context)
        {
            try
            {
                // Define status categories
                var statusCategories = new[] { "Hoàn thành", "Hủy bỏ", "Đã hẹn" }; // Adjust based on your actual status values
                var appointmentCounts = new double[statusCategories.Length];

                // Map your actual status values to these categories
                var statusMapping = new Dictionary<string, int>
                {
                    { "Hoàn thành", 0 },
                    { "Đã khám", 0 },
                    { "Hủy bỏ", 1 },
                    { "Đã hủy", 1 },
                    { "Đã hẹn", 2 },
                    { "Chờ khám", 2 }
                    // Add more mappings as needed
                };

                var appointments = context.Set<Appointment>()
                    .Where(a => a.AppointmentDate >= StartDate && a.AppointmentDate <= EndDate)
                    .ToList();

                foreach (var appointment in appointments)
                {
                    if (!string.IsNullOrEmpty(appointment.Status) && statusMapping.TryGetValue(appointment.Status, out int index))
                    {
                        appointmentCounts[index]++;
                    }
                }

                AppointmentStatusSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Số lượng",
                        Values = new ChartValues<double>(appointmentCounts),
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    }
                };

                AppointmentStatusLabels = statusCategories;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading appointment status data: {ex.Message}");
                //LoadAppointmentStatusSampleData(); // Fallback to sample data
            }
        }

        private void LoadCancellationRateData(DbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var cancellationRateByMonth = new double[12];

                for (int month = 1; month <= 12; month++)
                {
                    var startOfMonth = new DateTime(currentYear, month, 1);
                    var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

                    // Count total appointments for the month
                    var totalAppointmentsThisMonth = context.Set<Appointment>()
                        .Count(a => a.AppointmentDate >= startOfMonth && a.AppointmentDate <= endOfMonth);

                    if (totalAppointmentsThisMonth > 0)
                    {
                        // Count cancelled appointments for the month
                        var cancelledAppointmentsThisMonth = context.Set<Appointment>()
                            .Count(a => a.AppointmentDate >= startOfMonth &&
                                   a.AppointmentDate <= endOfMonth &&
                                   (a.Status == "Hủy bỏ" || a.Status == "Đã hủy"));

                        cancellationRateByMonth[month - 1] = Math.Round((double)(cancelledAppointmentsThisMonth * 100) / totalAppointmentsThisMonth, 1);
                    }
                    else
                    {
                        cancellationRateByMonth[month - 1] = 0;
                    }
                }

                CancellationRateSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Tỷ lệ hủy lịch hẹn",
                        Values = new ChartValues<double>(cancellationRateByMonth),
                        PointGeometry = DefaultGeometries.Circle,
                        PointGeometrySize = 8,
                        LineSmoothness = 0.5,
                        Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading cancellation rate data: {ex.Message}");
                //LoadCancellationRateSampleData(); // Fallback to sample data
            }
        }

        private void LoadAppointmentPeakHoursData(DbContext context)
        {
            try
            {
                var appointmentsByHour = new double[24];

                var appointments = context.Set<Appointment>()
                    .Where(a => a.AppointmentDate >= StartDate && a.AppointmentDate <= EndDate)
                    .ToList();

                foreach (var appointment in appointments)
                {
                    int hour = appointment.AppointmentDate.Hour;
                    appointmentsByHour[hour]++;
                }

                AppointmentPeakHoursSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Số lịch hẹn",
                        Values = new ChartValues<double>(appointmentsByHour),
                        Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
                    }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading appointment peak hours data: {ex.Message}");
                //LoadAppointmentPeakHoursSampleData(); // Fallback to sample data
            }
        }

        private void LoadPatientsByDoctorData(DbContext context)
        {
            try
            {
                // Get doctors who have appointments in the date range
                var doctorsWithAppointments = context.Set<Appointment>()
                    .Where(a => a.AppointmentDate >= StartDate && a.AppointmentDate <= EndDate && a.DoctorId != null)
                    .GroupBy(a => a.DoctorId)
                    .Select(g => new
                    {
                        DoctorId = g.Key,
                        PatientCount = g.Select(a => a.PatientId).Distinct().Count()
                    })
                    .OrderByDescending(x => x.PatientCount)
                    .Take(5) // Top 5 doctors
                    .ToList();

                if (doctorsWithAppointments.Any())
                {
                    var doctorNames = new List<string>();
                    var patientCounts = new List<double>();

                    foreach (var item in doctorsWithAppointments)
                    {
                        var doctor = context.Set<Doctor>().FirstOrDefault(d => d.DoctorId == item.DoctorId);
                        if (doctor != null)
                        {
                            doctorNames.Add($"Bs. {doctor.FullName}");
                            patientCounts.Add(item.PatientCount);
                        }
                    }

                    PatientsByDoctorSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Số bệnh nhân",
                            Values = new ChartValues<double>(patientCounts),
                            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        }
                    };

                    DoctorLabels = doctorNames.ToArray();
                }
                else
                {
                    // No data, use sample data
                   // LoadPatientsByDoctorSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading patients by doctor data: {ex.Message}");
               // LoadPatientsByDoctorSampleData(); // Fallback to sample data
            }
        }

        private void LoadCompletionRateByDoctorData(DbContext context)
        {
            try
            {
                // If we already have doctors from the previous method, use those
                if (DoctorLabels != null && DoctorLabels.Any() && DoctorLabels[0].StartsWith("Bs. "))
                {
                    var completionRates = new List<double>();

                    foreach (var doctorLabel in DoctorLabels)
                    {
                        string doctorName = doctorLabel.Replace("Bs. ", "");

                        var doctor = context.Set<Doctor>()
                            .FirstOrDefault(d => d.FullName == doctorName);

                        if (doctor != null)
                        {
                            // Count total appointments for this doctor
                            var totalAppointments = context.Set<Appointment>()
                                .Count(a => a.DoctorId == doctor.DoctorId &&
                                       a.AppointmentDate >= StartDate &&
                                       a.AppointmentDate <= EndDate);

                            if (totalAppointments > 0)
                            {
                                // Count completed appointments
                                var completedAppointments = context.Set<Appointment>()
                                    .Count(a => a.DoctorId == doctor.DoctorId &&
                                           a.AppointmentDate >= StartDate &&
                                           a.AppointmentDate <= EndDate &&
                                           (a.Status == "Hoàn thành" || a.Status == "Đã khám"));

                                double completionRate = Math.Round((double)(completedAppointments * 100) / totalAppointments, 1);
                                completionRates.Add(completionRate);
                            }
                            else
                            {
                                completionRates.Add(0);
                            }
                        }
                        else
                        {
                            completionRates.Add(0);
                        }
                    }

                    CompletionRateByDoctorSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Tỷ lệ hoàn thành",
                            Values = new ChartValues<double>(completionRates),
                            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                        }
                    };
                }
                else
                {
                    // If we don't have doctor labels, get the top doctors directly
                    var doctorsCompletionRates = context.Set<Doctor>()
                        .Take(5)
                        .Select(d => new
                        {
                            Doctor = d,
                            CompletionRate = CalculateCompletionRateForDoctor(d.DoctorId, context)
                        })
                        .OrderByDescending(x => x.CompletionRate)
                        .ToList();

                    if (doctorsCompletionRates.Any())
                    {
                        var doctorNames = doctorsCompletionRates.Select(x => $"Bs. {x.Doctor.FullName}").ToArray();
                        var completionRates = doctorsCompletionRates.Select(x => x.CompletionRate).ToArray();

                        CompletionRateByDoctorSeries = new SeriesCollection
                        {
                            new ColumnSeries
                            {
                                Title = "Tỷ lệ hoàn thành",
                                Values = new ChartValues<double>(completionRates),
                                Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                            }
                        };

                        DoctorLabels = doctorNames;
                    }
                    else
                    {
                        // No data, use sample data
                       // LoadCompletionRateByDoctorSampleData();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading completion rate by doctor data: {ex.Message}");
               // LoadCompletionRateByDoctorSampleData(); // Fallback to sample data
            }
        }

        private double CalculateCompletionRateForDoctor(int doctorId, DbContext context)
        {
            // Count total appointments for this doctor
            var totalAppointments = context.Set<Appointment>()
                .Count(a => a.DoctorId == doctorId &&
                       a.AppointmentDate >= StartDate &&
                       a.AppointmentDate <= EndDate);

            if (totalAppointments > 0)
            {
                // Count completed appointments
                var completedAppointments = context.Set<Appointment>()
                    .Count(a => a.DoctorId == doctorId &&
                           a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           (a.Status == "Hoàn thành" || a.Status == "Đã khám"));

                return Math.Round((double)(completedAppointments * 100) / totalAppointments, 1);
            }

            return 0;
        }

        private void LoadRevenueByCategoryData(DbContext context)
        {
            try
            {
                // Get top categories by revenue
                var topCategories = context.Set<InvoiceDetail>()
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                    .ThenInclude(m => m.Category)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null &&
                           id.Medicine.Category != null)
                    .GroupBy(id => new { id.Medicine.Category.CategoryId, id.Medicine.Category.CategoryName })
                    .Select(g => new
                    {
                        CategoryName = g.Key.CategoryName,
                        Revenue = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                if (topCategories.Any())
                {
                    var categoryNames = topCategories.Select(c => c.CategoryName).ToArray();
                    var revenues = topCategories.Select(c => (double)c.Revenue).ToArray();

                    RevenueByCategorySeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Doanh thu",
                            Values = new ChartValues<double>(revenues),
                            Fill = new SolidColorBrush(Color.FromRgb(156, 39, 176))
                        }
                    };

                    CategoryLabels = categoryNames;
                }
                else
                {
                    // No data, use sample data
               //     LoadRevenueByCategorySampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading revenue by category data: {ex.Message}");
              //  LoadRevenueByCategorySampleData(); // Fallback to sample data
            }
        }

        private void LoadProfitMarginData(DbContext context)
        {
            try
            {
                // Get top medicines by revenue
                var topMedicines = context.Set<InvoiceDetail>()
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .GroupBy(id => new { id.MedicineId, id.Medicine.Name })
                    .Select(g => new
                    {
                        MedicineId = g.Key.MedicineId,
                        MedicineName = g.Key.Name,
                        Revenue = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.Revenue)
                    .Take(5)
                    .ToList();

                if (topMedicines.Any())
                {
                    var medicineNames = new List<string>();
                    var purchasePrices = new List<double>();
                    var sellingPrices = new List<double>();

                    foreach (var medicine in topMedicines)
                    {
                        // Get medicine details with purchase and selling prices
                        var medicineDetails = context.Set<Medicine>()
                            .FirstOrDefault(m => m.MedicineId == medicine.MedicineId);

                        if (medicineDetails != null)
                        {
                            medicineNames.Add(medicineDetails.Name);

                            // Use StockIn for purchase price if available
                            var avgPurchasePrice = context.Set<StockIn>()
                                .Where(si => si.MedicineId == medicine.MedicineId)
                                .Average(si => (double?)si.SellPrice) ?? 0;

                            purchasePrices.Add(avgPurchasePrice);
                            sellingPrices.Add((double)medicineDetails.CurrentSellPrice);
                        }
                    }

                    ProfitMarginSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Giá nhập",
                            Values = new ChartValues<double>(purchasePrices),
                            Fill = new SolidColorBrush(Color.FromRgb(244, 67, 54))
                        },
                        new ColumnSeries
                        {
                            Title = "Giá bán",
                            Values = new ChartValues<double>(sellingPrices),
                            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                        }
                    };

                    TopMedicineLabels = medicineNames.ToArray();
                }
                else
                {
                    // No data, use sample data
                   // LoadProfitMarginSampleData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading profit margin data: {ex.Message}");
             //   LoadProfitMarginSampleData(); // Fallback to sample data
            }
        }

        #region Filter Methods
        private void FilterByDay()
        {
            StartDate = DateTime.Now.Date;
            EndDate = DateTime.Now;
            LoadStatistics();
        }

        private void FilterByMonth()
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            LoadStatistics();
        }

        private void FilterByQuarter()
        {
            int currentMonth = DateTime.Now.Month;
            int quarterNumber = (currentMonth - 1) / 3 + 1;
            int quarterStartMonth = (quarterNumber - 1) * 3 + 1;

            StartDate = new DateTime(DateTime.Now.Year, quarterStartMonth, 1);
            EndDate = DateTime.Now;
            LoadStatistics();
        }

        private void FilterByYear()
        {
            StartDate = new DateTime(DateTime.Now.Year, 1, 1);
            EndDate = DateTime.Now;
            LoadStatistics();
        }

        private void ViewLowStock()
        {
            // In a real application, this would navigate to a detailed view
            // of low stock medicines or open a dialog
            System.Diagnostics.Debug.WriteLine("Viewing low stock medicines");
        }
        #endregion

        private SolidColorBrush GetRandomBrush()
        {
            // Pre-defined colors for consistency
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
    }

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
    #endregion
}
