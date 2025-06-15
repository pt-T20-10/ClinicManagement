using ClinicManagement.Models;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClinicManagement.ViewModels
{
    public class StatisticsViewModel : BaseViewModel
    {
        #region Basic Properties
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

        // Track if async operation is running
        private bool _isAsyncOperationRunning = false;
        private object _lockObject = new object();

        public StatisticsViewModel()
        {
            InitializeCommands();
            YFormatter = value => value.ToString("N0");
            InitializeCharts();
            LoadDashBoard();
            // Load statistics after a short delay to ensure UI is rendered
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => FilterByMonth())
            );
        }

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
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                },
                new LineSeries
                {
                    Title = "Mục tiêu",
                    Values = new ChartValues<double>(new double[12]),
                    PointGeometry = DefaultGeometries.Circle,
                    StrokeThickness = 3,
                    Stroke = new SolidColorBrush(Color.FromRgb(255, 82, 82))
                }
            };

            RevenueByHourSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu theo giờ",
                    Values = new ChartValues<double>(new double[24]),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0))
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
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80))
                }
            };

            // Initialize empty collections
            ProductDistributionSeries = new SeriesCollection();
            TopRevenueDaysSeries = new SeriesCollection();
            RevenueTrendSeries = new SeriesCollection();
            InvoiceTypeSeries = new SeriesCollection();
            PatientTypeSeries = new SeriesCollection();
            ServiceRevenueSeries = new SeriesCollection();
  

           
            AppointmentPeakHoursSeries = new SeriesCollection();
            PatientsByDoctorSeries = new SeriesCollection();
            RevenueByCategorySeries = new SeriesCollection();
            CancellationRateSeries = new SeriesCollection();
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
                // Use a dedicated DbContext for each operation
                using (var context = new ClinicDbContext())
                {
                    // Load basic statistics
                    await Task.Run(() => LoadBasicStatistics(context));
                }

                // Update charts on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    using (var context = new ClinicDbContext())
                    {
                        // Load revenue charts
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
                        // Load patient and appointment charts
                        LoadPatientTypeChart(context);
                        LoadAppointmentStatusChart(context);
                        LoadAppointmentPeakHoursChart(context);
                        LoadPatientsByDoctorChart(context);
                    }

                    using (var context = new ClinicDbContext())
                    {
                        // Load financial data and product info
                        LoadTopSellingProducts(context);
                        LoadTopVIPPatients(context);
                        CalculateGrowthRates(context);
                    }

                    using (var context = new ClinicDbContext())
                    {
                        // Load medicine warnings (separate context to avoid LINQ translation issues)
                        LoadWarningMedicines(context);
                    }
                });

                // Ensure commands can be requeried after data load
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tải thống kê: {ex.Message}",
                                   "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Calculate today's growth (compared to yesterday)
                var yesterday = today.AddDays(-1);
                var yesterdayInvoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Date == yesterday &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal yesterdayRevenue = yesterdayInvoices.Sum(i => i.TotalAmount);

                double growthPercentage = 0;
                if (yesterdayRevenue > 0)
                {
                    growthPercentage = (double)((todayRevenue - yesterdayRevenue) / yesterdayRevenue * 100);
                }
                else if (todayRevenue > 0)
                {
                    growthPercentage = 100;
                }

                // Update UI elements on the UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TodayRevenue = todayRevenue;
                    MonthRevenue = monthRevenue;
                    TotalRevenue = totalRevenue;
                    NewPatients = newPatientsCount;
                    TotalPatients = totalPatientsCount;
                    TotalAppointments = totalAppointmentsCount;
                    TotalMedicineSold = medicineSoldCount;
                    GrowthPercentage = growthPercentage;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tải thống kê cơ bản: {ex.Message}",
                                   "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }

                    if (targetSeries?.Values is ChartValues<double> targetValues)
                    {
                        targetValues.Clear();
                        targetValues.AddRange(monthlyTarget);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ doanh thu theo tháng: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ doanh thu theo giờ: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            LabelPoint = chartPoint => $"{category.CategoryName}: {chartPoint.Y:0.0}%",
                            Fill = GetRandomBrush()
                        });
                    }

                    ProductDistributionSeries = newSeries;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ phân bố sản phẩm: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                var topRevenueDays = invoices
                    .GroupBy(i => i.InvoiceDate.Value.Date)
                    .Select(g => new { Date = g.Key, Revenue = g.Sum(i => i.TotalAmount) })
                    .OrderByDescending(x => x.Revenue)
                    .Take(7)
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ top ngày có doanh thu cao: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54))
                    }
                };

                RevenueTrendLabels = MonthLabels;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ xu hướng doanh thu: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi tải biểu đồ doanh thu theo loại hóa đơn: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                if (totalRevenue > 0)
                {
                    var seriesCollection = new SeriesCollection();

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
                                LabelPoint = chartPoint => $"{type}: {chartPoint.Y:0.0}%",
                                Fill = new SolidColorBrush(brushColor)
                            });
                        }
                    }

                    ServiceRevenueSeries = seriesCollection;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ tỷ lệ doanh thu theo dịch vụ: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatientTypeChart(ClinicDbContext context)
        {
            try
            {
                var patientTypes = context.PatientTypes
                    .Where(pt => pt.IsDeleted != true)
                    .ToList();

                if (patientTypes.Any())
                {
                    var seriesCollection = new SeriesCollection();

                    // Process in memory
                    var patients = context.Patients
                        .Where(p => p.IsDeleted != true &&
                               p.CreatedAt >= StartDate &&
                               p.CreatedAt <= EndDate)
                        .ToList();

                    var totalPatients = patients.Count;

                    if (totalPatients > 0)
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

                        PatientTypeSeries = seriesCollection;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ phân loại bệnh nhân: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                        values.AddRange(statusCounts);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ trạng thái lịch hẹn: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi tải biểu đồ giờ cao điểm lịch hẹn: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatientsByDoctorChart(ClinicDbContext context)
        {
            try
            {
                // Get all doctors
                var doctors = context.Doctors
                    .Where(d => d.IsDeleted != true)
                    .Take(10)
                    .ToList();

                if (!doctors.Any())
                    return;

                // Get appointments in the date range
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           a.IsDeleted != true)
                    .ToList();

                var doctorNames = new List<string>();
                var patientCounts = new List<double>();

                foreach (var doctor in doctors)
                {
                    doctorNames.Add(doctor.FullName);
                    patientCounts.Add(appointments.Count(a => a.DoctorId == doctor.DoctorId));
                }

                PatientsByDoctorSeries = new SeriesCollection
                {
                    new ColumnSeries
                    {
                        Title = "Số bệnh nhân",
                        Values = new ChartValues<double>(patientCounts),
                        Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
                    }
                };

                DoctorLabels = doctorNames.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ bệnh nhân theo bác sĩ: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTopSellingProducts(ClinicDbContext context)
        {
            try
            {
                // Process in memory to avoid complex LINQ translation
                var invoiceDetails = context.InvoiceDetails
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
                MessageBox.Show($"Lỗi khi tải dữ liệu sản phẩm bán chạy: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Lỗi khi tải dữ liệu khách hàng VIP: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Change from private to public
        public void LoadWarningMedicines(ClinicDbContext context)
        {
            try
            {
                // Clear the change tracker to ensure fresh data
                context.ChangeTracker.Clear();

                // Fetch all non-deleted medicines with their related data
                var medicines = context.Medicines
                    .AsNoTracking()  // Force fresh data from database
                    .Where(m => m.IsDeleted != true)
                    .Include(m => m.StockIns)
                    .Include(m => m.InvoiceDetails)
                    .Include(m => m.Unit)
                    .ToList();

                // Prepare warning collections
                var lowStockWarnings = new List<WarningMedicine>();
                var expiryWarnings = new List<WarningMedicine>();

                // Current date reference points
                var today = DateOnly.FromDateTime(DateTime.Today);
                var thirtyDaysLater = today.AddDays(30);

                foreach (var medicine in medicines)
                {
                    // Force stock quantity calculation by accessing the property
                    int currentStock = medicine.TotalStockQuantity;

                    // 1. Check for low stock (threshold of 10 items)
                    if (currentStock <= 10)
                    {
                        lowStockWarnings.Add(new WarningMedicine
                        {
                            Id = medicine.MedicineId,
                            Name = medicine.Name,
                            WarningMessage = $"Còn {currentStock} {medicine.Unit?.UnitName ?? "đơn vị"} - Dưới mức tối thiểu"
                        });
                    }

                    // 2. Check for expiry issues
                    if (medicine.ExpiryDate.HasValue)
                    {
                        if (medicine.ExpiryDate.Value <= today)
                        {
                            // Already expired
                            expiryWarnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = "Đã hết hạn sử dụng"
                            });
                        }
                        else if (medicine.ExpiryDate.Value <= thirtyDaysLater)
                        {
                            // Expiring soon (within 30 days)
                            int daysUntilExpiry = medicine.ExpiryDate.Value.DayNumber - today.DayNumber;
                            expiryWarnings.Add(new WarningMedicine
                            {
                                Id = medicine.MedicineId,
                                Name = medicine.Name,
                                WarningMessage = $"Hết hạn trong {daysUntilExpiry} ngày"
                            });
                        }
                    }
                }

                // Prioritize warnings and take unique medicines
                // (one medicine might have both low stock and expiry issues)
                var allWarnings = new Dictionary<int, WarningMedicine>();

                // Add expiry warnings first (higher priority)
                foreach (var warning in expiryWarnings)
                {
                    allWarnings[warning.Id] = warning;
                }

                // Then add low stock warnings (if not already added)
                foreach (var warning in lowStockWarnings)
                {
                    if (!allWarnings.ContainsKey(warning.Id))
                    {
                        allWarnings[warning.Id] = warning;
                    }
                }

                // Take top 10 warnings
                var topWarnings = allWarnings.Values.Take(10).ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    WarningMedicines = new ObservableCollection<WarningMedicine>(topWarnings);
                    LowStockCount = topWarnings.Count;
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Lỗi khi tải cảnh báo thuốc: {ex.Message}",
                                   "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    WarningMedicines.Clear();
                    LowStockCount = 0;
                });
            }
        }



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

                // Set value for RevenueGrowth
                if (previousRevenue > 0)
                {
                    var revenueGrowth = ((currentRevenue - previousRevenue) / previousRevenue) * 100;
                    RevenueGrowth = $"{revenueGrowth:+0.0;-0.0}%";
                }
                else
                {
                    RevenueGrowth = "N/A";
                }

                // Calculate patient growth
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
                    PatientGrowth = $"{patientGrowth:+0.0;-0.0}%";
                }
                else
                {
                    PatientGrowth = "N/A";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tính toán tỷ lệ tăng trưởng: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
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

                // Tạo biểu đồ doanh thu theo danh mục
                RevenueByCategorySeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = "Doanh thu",
                Values = new ChartValues<double>(revenueValues),
                Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243))
            }
        };

                CategoryLabels = categoryNames.ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải biểu đồ doanh thu theo danh mục: {ex.Message}",
                               "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadDashBoard()
        {
            CurrentDate = DateTime.Now.Date;
            TodayAppointments = new ObservableCollection<TodayAppointment>();

            var appointments = DataProvider.Instance.Context.Appointments
          .Where(a => a.IsDeleted == false || a.IsDeleted == null)
          .Include(a => a.Patient)
          .Include(a => a.Doctor)
          .Include(a => a.AppointmentType)
          .ToList();
            int waitingCount = appointments.Count(a => a.Status == "Đang chờ");
            foreach (var appointment in appointments)
            {
                TodayAppointment app = new TodayAppointment
                {
                    Appointment = appointment,
                    Initials = GetInitialsFromFullName(appointment.Patient?.FullName),
                    PatientName = appointment.Patient?.FullName,
                    DoctorName = appointment.Doctor?.FullName,
                    Notes = appointment.Notes,
                    Status = appointment.Status,
                    Time = appointment.AppointmentDate.TimeOfDay
                };
                TodayAppointments.Add(app);

                TotalAppointments = appointments.Count();

            }
            var count = DataProvider.Instance.Context.Patients.Count();
            PendingAppointments = waitingCount.ToString();
            TotalPatients = count;
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
            LoadStatisticsAsync();
        }

        private void FilterByMonth()
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            EndDate = DateTime.Now;
            LoadStatisticsAsync();
        }

        private void FilterByQuarter()
        {
            int currentMonth = DateTime.Now.Month;
            int quarterNumber = (currentMonth - 1) / 3 + 1;
            int quarterStartMonth = (quarterNumber - 1) * 3 + 1;

            StartDate = new DateTime(DateTime.Now.Year, quarterStartMonth, 1);
            EndDate = DateTime.Now;
            LoadStatisticsAsync();
        }

        private void FilterByYear()
        {
            StartDate = new DateTime(DateTime.Now.Year, 1, 1);
            EndDate = DateTime.Now;
            LoadStatisticsAsync();
        }

        private void ViewLowStock()
        {
            if (WarningMedicines.Count > 0)
            {
                var warningText = string.Join("\n", WarningMedicines.Select(m => $"{m.Name}: {m.WarningMessage}"));
                MessageBox.Show($"Danh sách thuốc cần chú ý:\n\n{warningText}",
                               "Cảnh báo tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                MessageBox.Show("Không có thuốc nào cần chú ý trong kho.",
                               "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
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
