using ClinicManagement.Models;
using ClinicManagement.Services;
using ClinicManagement.SubWindow;
using ClosedXML.Excel;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// ViewModel quản lý trang thống kê
    /// Hiển thị các biểu đồ, báo cáo doanh thu, bệnh nhân, lịch hẹn và các chỉ số kinh doanh quan trọng
    /// </summary>
    public class StatisticsViewModel : BaseViewModel
    {
        #region Basic Properties - Các thuộc tính cơ bản

        /// <summary>
        /// Hàm định dạng tiền tệ cho các biểu đồ
        /// Sử dụng để hiển thị giá trị tiền tệ theo định dạng Việt Nam
        /// </summary>
        public Func<double, string> CurrencyFormatter { get; set; }

        /// <summary>
        /// Trạng thái đang tải dữ liệu
        /// Sử dụng để hiển thị loading indicator trong UI
        /// </summary>
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

        /// <summary>
        /// Ngày bắt đầu của khoảng thời gian cần thống kê
        /// Mặc định là ngày đầu tháng hiện tại
        /// </summary>
        private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                OnPropertyChanged();
                // Xử lý tải dữ liệu sẽ được thực hiện trong các lệnh lọc
                // để tránh xung đột threading với DbContext
            }
        }

        /// <summary>
        /// Ngày kết thúc của khoảng thời gian cần thống kê
        /// Mặc định là ngày hiện tại
        /// </summary>
        private DateTime _endDate = DateTime.Now;
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                _endDate = value;
                OnPropertyChanged();
                // Xử lý tải dữ liệu sẽ được thực hiện trong các lệnh lọc
                // để tránh xung đột threading với DbContext
            }
        }

        // === THỐNG KÊ DOANH THU NGÀY/THÁNG ===

        /// <summary>
        /// Tổng doanh thu trong ngày hôm nay
        /// Bao gồm tất cả hóa đơn đã thanh toán trong ngày
        /// </summary>
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

        /// <summary>
        /// Tổng doanh thu trong tháng hiện tại
        /// Bao gồm tất cả hóa đơn đã thanh toán từ đầu tháng đến hiện tại
        /// </summary>
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

        // === THỐNG KÊ LỊCH HẸN ===

        /// <summary>
        /// Số lượng lịch hẹn trong ngày hôm nay
        /// Bao gồm tất cả các trạng thái lịch hẹn
        /// </summary>
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

        /// <summary>
        /// Số lượng lịch hẹn trong ngày hôm qua
        /// Sử dụng để tính toán tỷ lệ tăng trưởng
        /// </summary>
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

        /// <summary>
        /// Tỷ lệ tăng trưởng lịch hẹn so với ngày hôm qua (dạng chuỗi)
        /// Ví dụ: "+15%" hoặc "-5%"
        /// </summary>
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

        /// <summary>
        /// Tỷ lệ tăng trưởng lịch hẹn so với ngày hôm qua (dạng số)
        /// Sử dụng cho các biểu đồ và tính toán
        /// </summary>
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

        // === THỐNG KÊ BỆNH NHÂN ===

        /// <summary>
        /// Số lượng bệnh nhân mới trong khoảng thời gian được chọn
        /// </summary>
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

        /// <summary>
        /// Số lượng thuốc sắp hết hàng hoặc cần cảnh báo tồn kho
        /// Sử dụng để hiển thị cảnh báo trong dashboard
        /// </summary>
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

        // === THỐNG KÊ DOANH THU TỔNG QUAN ===

    
        /// <summary>
        /// Phần trăm hoàn thành mục tiêu doanh thu
        /// Được tính tự động dựa trên TotalRevenue và RevenueTarget
        /// </summary>
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
        // === THỐNG KÊ BỆNH NHÂN CHI TIẾT ===

        /// <summary>
        /// Tổng số bệnh nhân trong khoảng thời gian được chọn
        /// Tự động cập nhật phần trăm hoàn thành mục tiêu bệnh nhân
        /// </summary>
        private int _totalPatients;
        public int TotalPatients
        {
            get => _totalPatients;
            set
            {
                _totalPatients = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPercentage)); // Thông báo cập nhật phần trăm bệnh nhân
            }
        }

        /// <summary>
        /// Mục tiêu số lượng bệnh nhân đã đặt ra
        /// Mặc định là 300 bệnh nhân
        /// </summary>
        private int _patientTarget = 300;
        public int PatientTarget
        {
            get => _patientTarget;
            set
            {
                _patientTarget = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PatientPercentage)); // Thông báo cập nhật phần trăm bệnh nhân
            }
        }

        /// <summary>
        /// Phần trăm hoàn thành mục tiêu bệnh nhân
        /// Calculated property - tự động tính toán dựa trên TotalPatients và PatientTarget
        /// </summary>
        public double PatientPercentage => PatientTarget > 0 ? (double)((TotalPatients / (double)PatientTarget) * 100) : 0;

        /// <summary>
        /// Tỷ lệ tăng trưởng bệnh nhân so với kỳ trước (dạng chuỗi)
        /// Ví dụ: "+18%" hoặc "-3%"
        /// </summary>
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

        // === THỐNG KÊ LỊCH HẸN CHI TIẾT ===

        /// <summary>
        /// Tổng số lịch hẹn trong khoảng thời gian được chọn
        /// Bao gồm tất cả các trạng thái lịch hẹn
        /// </summary>
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

        // === THỐNG KÊ THUỐC ===

        /// <summary>
        /// Tổng số lượng thuốc đã bán trong khoảng thời gian được chọn
        /// Tính từ các hóa đơn bán thuốc đã thanh toán
        /// </summary>
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

        /// <summary>
        /// Số lượng lịch hẹn đang chờ xử lý (dạng chuỗi)
        /// Hiển thị trong dashboard để theo dõi công việc cần làm
        /// </summary>
        private string _PendingAppointments;
        public string PendingAppointments
        {
            get => _PendingAppointments;
            set
            {
                _PendingAppointments = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Danh sách lịch hẹn trong ngày hôm nay
        /// Hiển thị trong bảng tổng quan dashboard
        /// </summary>
        private ObservableCollection<TodayAppointment> _TodayAppointments;
        public ObservableCollection<TodayAppointment> TodayAppointments
        {
            get => _TodayAppointments;
            set
            {
                _TodayAppointments = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Ngày hiện tại để hiển thị trong dashboard
        /// </summary>
        private DateTime _CurrentDate;
        public DateTime CurrentDate
        {
            get => _CurrentDate;
            set
            {
                _CurrentDate = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Chart Properties - Thuộc tính biểu đồ

        // === NHÃN CHO CÁC BIỂU ĐỒ ===

        /// <summary>
        /// Nhãn giờ cho biểu đồ theo giờ (7:00 - 17:00)
        /// Sử dụng cho biểu đồ doanh thu theo giờ và lịch hẹn theo giờ
        /// </summary>
        public string[] HourLabels { get; } = Enumerable.Range(7, 17)
            .Select(h => $"{h}:00").ToArray();

        /// <summary>
        /// Nhãn tháng cho biểu đồ theo tháng (T1 - T12)
        /// Sử dụng cho biểu đồ doanh thu theo tháng
        /// </summary>
        public string[] MonthLabels { get; } = { "T1", "T2", "T3", "T4", "T5", "T6", "T7", "T8", "T9", "T10", "T11", "T12" };

        // === BIỂU ĐỒ DOANH THU THEO THỜI GIAN ===



        /// <summary>
        /// Nhãn thời gian cho biểu đồ doanh thu
        /// Thay đổi tùy theo loại lọc (ngày, tháng, quý, năm)
        /// </summary>
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

        // === BIỂU ĐỒ LOẠI HÓA ĐƠN ===

        /// <summary>
        /// Dữ liệu biểu đồ phân bố theo loại hóa đơn
        /// Bao gồm: Khám bệnh, Bán thuốc, Khám và bán thuốc
        /// </summary>
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

        /// <summary>
        /// Nhãn cho biểu đồ loại hóa đơn
        /// Tương ứng với các loại hóa đơn trong hệ thống
        /// </summary>
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

        // === BIỂU ĐỒ DOANH THU DỊCH VỤ ===

        /// <summary>
        /// Dữ liệu biểu đồ doanh thu theo từng dịch vụ
        /// Phân tích đóng góp của từng dịch vụ vào tổng doanh thu
        /// </summary>
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

        /// <summary>
        /// Nhãn so sánh doanh thu
        /// Sử dụng cho các biểu đồ so sánh doanh thu giữa các kỳ
        /// </summary>
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

        // === BIỂU ĐỒ TOP NGÀY CÓ DOANH THU CAO NHẤT ===

        /// <summary>
        /// Dữ liệu biểu đồ top ngày có doanh thu cao nhất
        /// Hiển thị các ngày kinh doanh hiệu quả nhất
        /// </summary>
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

        /// <summary>
        /// Nhãn cho biểu đồ top ngày có doanh thu cao
        /// Chứa thông tin ngày tháng tương ứng
        /// </summary>
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

        // === BIỂU ĐỒ XU HƯỚNG DOANH THU ===

        /// <summary>
        /// Dữ liệu biểu đồ xu hướng doanh thu theo thời gian
        /// Hiển thị đường xu hướng tăng trưởng doanh thu
        /// </summary>
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

        /// <summary>
        /// Nhãn thời gian cho biểu đồ xu hướng doanh thu
        /// Thay đổi theo khoảng thời gian được chọn
        /// </summary>
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

        // === BIỂU ĐỒ DOANH THU THEO GIỜ ===

        /// <summary>
        /// Dữ liệu biểu đồ doanh thu theo từng giờ trong ngày
        /// Phân tích thời gian kinh doanh hiệu quả nhất
        /// </summary>
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

        // === BIỂU ĐỒ PHÂN LOẠI BỆNH NHÂN ===

        /// <summary>
        /// Dữ liệu biểu đồ phân bố bệnh nhân theo loại
        /// Bao gồm: Thường, VIP, Bảo hiểm y tế, v.v.
        /// </summary>
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

        // === BIỂU ĐỒ TRẠNG THÁI LỊCH HẸN ===

        /// <summary>
        /// Dữ liệu biểu đồ phân bố lịch hẹn theo trạng thái
        /// Bao gồm: Đang chờ, Đang khám, Đã khám, Đã hủy
        /// </summary>
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

        /// <summary>
        /// Nhãn cho biểu đồ trạng thái lịch hẹn
        /// Tương ứng với các trạng thái lịch hẹn trong hệ thống
        /// </summary>
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

        // === BIỂU ĐỒ GIỜ CAO ĐIỂM LỊCH HẸN ===

        /// <summary>
        /// Dữ liệu biểu đồ phân bố lịch hẹn theo giờ trong ngày
        /// Xác định khung giờ có nhiều lịch hẹn nhất
        /// </summary>
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

        // === BIỂU ĐỒ BỆNH NHÂN THEO BÁC SĨ ===

        /// <summary>
        /// Dữ liệu biểu đồ phân bố bệnh nhân theo từng bác sĩ/nhân viên
        /// Đánh giá hiệu suất làm việc của từng bác sĩ
        /// </summary>
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

        /// <summary>
        /// Nhãn tên bác sĩ cho biểu đồ bệnh nhân theo bác sĩ
        /// Chứa tên hoặc mã nhân viên
        /// </summary>
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

        // === BIỂU ĐỒ DOANH THU THEO DANH MỤC THUỐC ===

        /// <summary>
        /// Dữ liệu biểu đồ doanh thu theo từng danh mục thuốc
        /// Phân tích danh mục thuốc nào mang lại doanh thu cao nhất
        /// </summary>
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

        /// <summary>
        /// Nhãn danh mục thuốc cho biểu đồ doanh thu
        /// Tương ứng với các danh mục thuốc trong hệ thống
        /// </summary>
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

        /// <summary>
        /// Dữ liệu biểu đồ phân phối sản phẩm
        /// Hiển thị tỷ lệ đóng góp của từng sản phẩm vào tổng doanh thu
        /// </summary>
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

        /// <summary>
        /// Dữ liệu biểu đồ tỷ lệ hủy lịch hẹn
        /// Theo dõi xu hướng hủy lịch hẹn theo thời gian
        /// </summary>
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

        /// <summary>
        /// Văn bản mô tả bộ lọc hiện tại
        /// Hiển thị cho người dùng biết đang xem thống kê của khoảng thời gian nào
        /// Ví dụ: "Đang xem: Tháng này", "Đang xem: Quý này"
        /// </summary>
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

        #region Collections - Bộ sưu tập dữ liệu hiển thị

        /// <summary>
        /// Danh sách sản phẩm bán chạy nhất trong khoảng thời gian được chọn
        /// Chứa thông tin về tên thuốc, danh mục, doanh thu và phần trăm đóng góp
        /// Được sắp xếp theo doanh thu giảm dần, lấy top 10 sản phẩm
        /// Hiển thị trong bảng "Top sản phẩm bán chạy" trên giao diện
        /// </summary>
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

        /// <summary>
        /// Danh sách top bệnh nhân theo tổng chi tiêu trong khoảng thời gian được chọn
        /// Chứa thông tin ID, họ tên, số điện thoại, loại bệnh nhân và tổng chi tiêu
        /// Được sắp xếp theo tổng chi tiêu giảm dần, lấy top 10 bệnh nhân
        /// Hiển thị trong bảng "Bệnh nhân VIP" để nhận biết khách hàng quan trọng
        /// Hỗ trợ phân tích xu hướng chi tiêu và chăm sóc khách hàng đặc biệt
        /// </summary>
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

        /// <summary>
        /// Danh sách cảnh báo về thuốc cần chú ý trong kho
        /// Bao gồm các loại cảnh báo:
        /// - Thuốc đã hết hạn nhưng chưa được tiêu hủy (ưu tiên cao nhất)
        /// - Thuốc sắp hết hạn (trong vòng 60 ngày)
        /// - Thuốc tồn kho thấp (≤ 20 đơn vị)
        /// - Lô thuốc cuối cùng (chỉ còn 1 lô)
        /// - Lỗi cấu hình (lô bán bị đánh dấu tiêu hủy)
        /// Được sắp xếp theo mức độ nghiêm trọng, hiển thị tối đa 10 cảnh báo
        /// Giúp quản lý kho hiệu quả và tránh rủi ro về chất lượng thuốc
        /// </summary>
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

        #region Formatters and Commands - Bộ định dạng và lệnh thao tác

        /// <summary>
        /// Hàm định dạng hiển thị cho trục Y của các biểu đồ
        /// Chuyển đổi giá trị số thành chuỗi hiển thị phù hợp
        /// Sử dụng cho các biểu đồ cần hiển thị đơn vị đo lường cụ thể
        /// </summary>
        public Func<double, string> YFormatter { get; set; }

        // === CÁC LỆNH ĐIỀU KHIỂN THỐNG KÊ ===

        /// <summary>
        /// Lệnh làm mới toàn bộ dữ liệu thống kê
        /// Tải lại tất cả biểu đồ, số liệu và cảnh báo từ cơ sở dữ liệu
        /// Sử dụng khi cần cập nhật dữ liệu real-time hoặc sau khi có thay đổi
        /// </summary>
      
        public ICommand RefreshCommand { get; set; }

        /// <summary>
        /// Lệnh lọc dữ liệu thống kê theo ngày hiện tại
        /// Thiết lập StartDate = hôm nay, EndDate = hiện tại
        /// Hiển thị CurrentFilterText = "Đang xem: Hôm nay"
        /// </summary>
        public ICommand FilterByDayCommand { get; set; }

        /// <summary>
        /// Lệnh lọc dữ liệu thống kê theo tháng hiện tại
        /// Thiết lập StartDate = đầu tháng, EndDate = hiện tại
        /// Hiển thị CurrentFilterText = "Đang xem: Tháng này"
        /// Đây là bộ lọc mặc định khi khởi động ứng dụng
        /// </summary>
        public ICommand FilterByMonthCommand { get; set; }

        /// <summary>
        /// Lệnh lọc dữ liệu thống kê theo quý hiện tại
        /// Tự động xác định quý dựa trên tháng hiện tại (Q1: T1-T3, Q2: T4-T6, Q3: T7-T9, Q4: T10-T12)
        /// Thiết lập StartDate = đầu quý, EndDate = hiện tại
        /// Hiển thị CurrentFilterText = "Đang xem: Quý X"
        /// </summary>
        public ICommand FilterByQuarterCommand { get; set; }

        /// <summary>
        /// Lệnh lọc dữ liệu thống kê theo năm hiện tại
        /// Thiết lập StartDate = 01/01 năm hiện tại, EndDate = hiện tại
        /// Hiển thị CurrentFilterText = "Đang xem: Năm YYYY"
        /// Phù hợp cho báo cáo tổng kết cuối năm
        /// </summary>
        public ICommand FilterByYearCommand { get; set; }

        /// <summary>
        /// Lệnh xem chi tiết danh sách thuốc cần cảnh báo
        /// Hiển thị popup với thông tin đầy đủ về các thuốc cần chú ý
        /// Chỉ khả dụng khi LowStockCount > 0
        /// Giúp người dùng nhanh chóng nắm bắt tình trạng kho thuốc
        /// </summary>
        public ICommand ViewLowStockCommand { get; set; }

        /// <summary>
        /// Lệnh xuất báo cáo doanh thu ra file Excel
        /// Bao gồm các biểu đồ: doanh thu theo ngày, theo loại hóa đơn, xu hướng doanh thu, doanh thu theo giờ
        /// Sử dụng ClosedXML để tạo file Excel với định dạng chuyên nghiệp
        /// Hiển thị progress dialog trong quá trình xuất để theo dõi tiến trình
        /// File được lưu với tên format: ThongKeDoanhThu_dd-MM-yyyy.xlsx
        /// </summary>
        public ICommand ExportRevenueToExcelCommand { get; private set; }

        // === CÁC LỆNH XUẤT EXCEL CHO TỪNG TAB CHUYÊN BIỆT ===

        /// <summary>
        /// Lệnh xuất báo cáo thống kê bệnh nhân ra file Excel
        /// Bao gồm: phân tích theo loại bệnh nhân, danh sách bệnh nhân VIP
        /// Tạo file với tên format: ThongKeBenhNhan_dd-MM-yyyy.xlsx
        /// Sử dụng background thread và progress tracking để tối ưu UX
        /// </summary>
        public ICommand ExportPatientsToExcelCommand { get; private set; }

        /// <summary>
        /// Lệnh xuất báo cáo thống kê lịch hẹn ra file Excel
        /// Bao gồm: phân bố theo trạng thái, giờ cao điểm, thống kê theo bác sĩ
        /// Tạo file với tên format: ThongKeLichHen_dd-MM-yyyy.xlsx
        /// Hỗ trợ phân tích hiệu quả hoạt động và phân bổ công việc
        /// </summary>
        public ICommand ExportAppointmentsToExcelCommand { get; private set; }

        /// <summary>
        /// Lệnh xuất báo cáo thống kê thuốc ra file Excel
        /// Bao gồm: doanh thu theo danh mục, phân bố sản phẩm, top thuốc bán chạy, cảnh báo tồn kho
        /// Tạo file với tên format: ThongKeThuoc_dd-MM-yyyy.xlsx
        /// Đặc biệt hữu ích cho quản lý kho và chiến lược kinh doanh thuốc
        /// Có code màu cho các mức độ cảnh báo khác nhau (đỏ: cần tiêu hủy, cam: sắp hết hạn, xanh: tồn kho thấp)
        /// </summary>
        public ICommand ExportMedicineToExcelCommand { get; private set; }
        #endregion

        #region Async Control Properties - Thuộc tính kiểm soát luồng bất đồng bộ
    
        /// <summary>
        /// Cờ theo dõi xem có thao tác bất đồng bộ nào đang chạy không
        /// Ngăn chặn việc thực hiện nhiều thao tác async cùng lúc để tránh:
        /// - Xung đột DbContext (multiple concurrent operations)
        /// - Tình trạng race condition khi cập nhật UI
        /// - Tải trùng lặp dữ liệu gây hiệu suất kém
        /// </summary>
        private bool _isAsyncOperationRunning = false;

        /// <summary>
        /// Object dùng để đồng bộ hóa luồng (thread synchronization)
        /// Đảm bảo chỉ một luồng có thể kiểm tra/thiết lập _isAsyncOperationRunning tại một thời điểm
        /// Sử dụng pattern lock() để thread-safe trong môi trường đa luồng
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// Constructor khởi tạo StatisticsViewModel
        /// Thiết lập các formatter, commands và tải dữ liệu dashboard ban đầu
        /// Sử dụng Dispatcher để tự động lọc theo tháng sau khi UI được khởi tạo
        /// </summary>
        #endregion
        public StatisticsViewModel()
        {
            InitializeCommands(); // Khởi tạo tất cả commands với logic CanExecute an toàn

            // === THIẾT LẬP CÁC FORMATTER ===

            /// Định dạng tiền tệ với 0 chữ số thập phân và đơn vị VNĐ
            /// Sử dụng cho tất cả biểu đồ hiển thị doanh thu và giá trị tiền tệ
            YFormatter = value => string.Format("{0:N0} VNĐ", value);
            CurrencyFormatter = value => string.Format("{0:N0} VNĐ", value);

            InitializeCharts();  // Khởi tạo các biểu đồ với dữ liệu mặc định
            LoadDashBoard();     // Tải dữ liệu dashboard cơ bản (lịch hẹn hôm nay, v.v.)

            // Sử dụng Dispatcher để tự động lọc theo tháng hiện tại sau khi UI hoàn tất khởi tạo
            // BeginInvoke với Background priority đảm bảo UI được render xong trước
            Application.Current.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Background,
                new Action(() => FilterByMonth())
            );
        }

        /// <summary>
        /// Formatter để hiển thị số nguyên (không có đơn vị)
        /// Sử dụng cho các biểu đồ hiển thị số lượng (bệnh nhân, lịch hẹn, thuốc)
        /// </summary>
        public Func<double, string> IntegerFormatter { get; } = value => ((int)value).ToString("N0");

        /// <summary>
        /// Khởi tạo tất cả các command với logic CanExecute an toàn
        /// Tất cả commands đều kiểm tra trạng thái IsLoading và _isAsyncOperationRunning
        /// để tránh thực hiện đồng thời nhiều thao tác có thể gây xung đột
        /// </summary>
        private void InitializeCommands()
        {
            // === COMMAND LÀM MỚI DỮ LIỆU ===
            /// Tải lại toàn bộ thống kê từ database
            /// CanExecute: chỉ khi không đang loading và không có async operation nào khác
            RefreshCommand = new RelayCommand<object>(
                p => RefreshData(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            // === CÁC COMMAND LỌC THEO THỜI GIAN ===
            /// Tất cả đều có cùng logic CanExecute để đảm bảo an toàn luồng

            /// Lọc dữ liệu theo ngày hiện tại (hôm nay)
            FilterByDayCommand = new RelayCommand<object>(
                p => FilterByDay(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            /// Lọc dữ liệu theo tháng hiện tại (từ đầu tháng đến hiện tại)
            FilterByMonthCommand = new RelayCommand<object>(
                p => FilterByMonth(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            /// Lọc dữ liệu theo quý hiện tại (Q1: T1-T3, Q2: T4-T6, Q3: T7-T9, Q4: T10-T12)
            FilterByQuarterCommand = new RelayCommand<object>(
                p => FilterByQuarter(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            /// Lọc dữ liệu theo năm hiện tại (từ 01/01 đến hiện tại)
            FilterByYearCommand = new RelayCommand<object>(
                p => FilterByYear(),
                p => !IsLoading && !_isAsyncOperationRunning
            );

            // === COMMAND XEM CẢNH BÁO TỒN KHO ===
            /// Hiển thị popup chi tiết về thuốc cần cảnh báo
            /// CanExecute: có cảnh báo (LowStockCount > 0) và không đang thực hiện thao tác khác
            ViewLowStockCommand = new RelayCommand<object>(
                p => ViewLowStock(),
                p => LowStockCount > 0 && !IsLoading && !_isAsyncOperationRunning
            );

            // === CÁC COMMAND XUẤT EXCEL ===
            /// Tất cả commands xuất Excel đều chỉ kiểm tra IsLoading (không cần kiểm tra async operation)
            /// vì xuất Excel chạy trong background thread riêng biệt với progress dialog

            /// Xuất báo cáo doanh thu ra Excel với các biểu đồ: theo ngày, loại hóa đơn, xu hướng, theo giờ
            ExportRevenueToExcelCommand = new RelayCommand<object>(
                p => ExportRevenueToExcel(),
                p => !IsLoading
            );

            /// Xuất báo cáo bệnh nhân ra Excel với: phân loại theo loại, danh sách VIP
            ExportPatientsToExcelCommand = new RelayCommand<object>(
                p => ExportPatientsToExcel(),
                p => !IsLoading
            );

            /// Xuất báo cáo lịch hẹn ra Excel với: trạng thái, giờ cao điểm, phân bố theo bác sĩ
            ExportAppointmentsToExcelCommand = new RelayCommand<object>(
                p => ExportAppointmentsToExcel(),
                p => !IsLoading
            );

            /// Xuất báo cáo thuốc ra Excel với: doanh thu theo danh mục, top bán chạy, cảnh báo tồn kho
            /// Có màu sắc phân biệt mức độ cảnh báo (đỏ: tiêu hủy, cam: sắp hết hạn, xanh: tồn kho thấp)
            ExportMedicineToExcelCommand = new RelayCommand<object>(
                p => ExportMedicineToExcel(),
                p => !IsLoading
            );
        }

        /// <summary>
        /// Khởi tạo tất cả các biểu đồ với dữ liệu mặc định và cấu hình ban đầu
        /// Được gọi trong constructor để đảm bảo UI có dữ liệu ngay khi hiển thị
        /// Tránh lỗi binding và null reference exception
        /// </summary>
        public void InitializeCharts()
        {
    

            // === BIỂU ĐỒ DOANH THU THEO GIỜ ===
            /// Biểu đồ cột hiển thị doanh thu theo 24 giờ trong ngày
            /// Giúp xác định khung giờ kinh doanh hiệu quả nhất
            RevenueByHourSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu theo giờ",
                    Values = new ChartValues<double>(new double[24]), // 24 giờ từ 0:00 đến 23:00
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Màu cam Material Design
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ
                }
            };

            // === BIỂU ĐỒ TRẠNG THÁI LỊCH HẸN ===
            /// Khởi tạo nhãn trạng thái lịch hẹn với các giá trị chuẩn trong hệ thống
            AppointmentStatusLabels = new[] { "Đang chờ", "Đã khám", "Đã hủy", "Đang khám" };

            /// Biểu đồ cột hiển thị số lượng lịch hẹn theo từng trạng thái
            AppointmentStatusSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lượng lịch hẹn",
                    Values = new ChartValues<double>(new double[AppointmentStatusLabels.Length]), // Khởi tạo theo số lượng trạng thái
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Màu xanh lá Material Design
                    LabelPoint = point => string.Format("{0:N0}", point.Y) // Định dạng số nguyên (không có đơn vị)
                }
            };

            // === BIỂU ĐỒ PHÂN BỔ SẢN PHẨM (PIE CHART) ===
            /// Khởi tạo với placeholder "Không có dữ liệu" để tránh UI trống
            /// Sẽ được thay thế bằng dữ liệu thực khi LoadStatisticsAsync() chạy
            ProductDistributionSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Không có dữ liệu",
                    Values = new ChartValues<double> { 100 }, // Hiển thị 100% cho placeholder
                    DataLabels = true, // Hiển thị nhãn dữ liệu trên biểu đồ
                    LabelPoint = chartPoint => "Không có dữ liệu", // Text hiển thị
                    Fill = new SolidColorBrush(Colors.Gray) // Màu xám cho placeholder
                }
            };

            // === BIỂU ĐỒ TOP NGÀY CÓ DOANH THU CAO ===
            /// Hiển thị 7 ngày gần nhất theo mặc định
            TopRevenueDaysSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(Enumerable.Repeat(0.0, 7)), // 7 ngày với giá trị 0
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Màu xanh lá
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ
                }
            };

            /// Tạo nhãn ngày cho 7 ngày gần nhất (định dạng dd/MM)
            TopRevenueDaysLabels = Enumerable.Range(1, 7)
                .Select(i => DateTime.Now.AddDays(-i).ToString("dd/MM")) // Lấy 7 ngày trước
                .Reverse() // Đảo ngược để hiển thị từ cũ đến mới
                .ToArray();

            // === BIỂU ĐỒ XU HƯỚNG DOANH THU (LINE CHART) ===
            /// Đường xu hướng mượt mà hiển thị xu hướng doanh thu theo thời gian
            RevenueTrendSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Xu hướng doanh thu",
                    Values = new ChartValues<double>(new double[12]), // 12 tháng
                    PointGeometry = null, // Không hiển thị điểm trên đường
                    LineSmoothness = 1, // Đường cong mượt mà (giá trị 0-1)
                    Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Màu đỏ Material Design
                    Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54)), // Vùng tô màu với độ trong suốt
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ
                }
            };
            RevenueTrendLabels = MonthLabels; // Sử dụng nhãn tháng đã định nghĩa (T1-T12)

            // === BIỂU ĐỒ DOANH THU THEO LOẠI HÓA ĐƠN ===
            /// Phân tích doanh thu theo 3 loại hóa đơn chính trong hệ thống
            InvoiceTypeSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(new double[] { 0, 0, 0 }), // 3 loại hóa đơn
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Màu xanh lá
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ
                }
            };
            /// Định nghĩa 3 loại hóa đơn chuẩn trong hệ thống quản lý phòng khám
            InvoiceTypeLabels = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };

            // === BIỂU ĐỒ PHÂN LOẠI BỆNH NHÂN (PIE CHART) ===
            /// Placeholder cho biểu đồ phân loại bệnh nhân theo loại (VIP, thường, BHYT...)
            PatientTypeSeries = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Không có dữ liệu",
                    Values = new ChartValues<double> { 100 },
                    DataLabels = true,
                    LabelPoint = chartPoint => "Không có dữ liệu trong khoảng thời gian này", // Thông báo chi tiết hơn
                    Fill = new SolidColorBrush(Colors.Gray)
                }
            };

            // === BIỂU ĐỒ DOANH THU DỊCH VỤ (PIE CHART) ===
            /// Phân tích tỷ lệ đóng góp của từng dịch vụ vào tổng doanh thu
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

            // === BIỂU ĐỒ GIỜ CAO ĐIỂM LỊCH HẸN ===
            /// Phân tích 24 giờ trong ngày để tìm khung giờ có nhiều lịch hẹn nhất
            AppointmentPeakHoursSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lịch hẹn",
                    Values = new ChartValues<double>(new double[24]), // 24 giờ từ 0:00-23:00
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Màu cam
                    LabelPoint = point => string.Format("{0:N0}", point.Y) // Định dạng số nguyên
                }
            };

            // === BIỂU ĐỒ BỆNH NHÂN THEO NHÂN VIÊN ===
            /// Thống kê số lượng bệnh nhân được phụ trách bởi từng nhân viên/bác sĩ
            PatientsByStaffseries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số bệnh nhân",
                    Values = new ChartValues<double> { 0 }, // Khởi tạo với 1 giá trị 0
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Màu xanh dương
                    LabelPoint = point => string.Format("{0:N0}", point.Y) // Định dạng số nguyên
                }
            };
            DoctorLabels = new[] { "Không có dữ liệu" }; // Nhãn placeholder

            // === BIỂU ĐỒ DOANH THU THEO DANH MỤC THUỐC ===
            /// Phân tích đóng góp doanh thu của từng danh mục thuốc
            RevenueByCategorySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double> { 0 }, // Khởi tạo với 1 giá trị 0
                    Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Màu xanh dương
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ
                }
            };
            CategoryLabels = new[] { "Không có dữ liệu" }; // Nhãn placeholder

            // === BIỂU ĐỒ TỶ LỆ HỦY LỊCH HẸN (PIE CHART) ===
            /// Thống kê tỷ lệ hủy lịch hẹn để đánh giá chất lượng dịch vụ
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

        /// <summary>
        /// Phương thức chính tải tất cả dữ liệu thống kê một cách bất đồng bộ
        /// Sử dụng multiple DbContext instances để tránh xung đột threading
        /// Áp dụng pattern Producer-Consumer để tối ưu hiệu suất
        /// Bao gồm cơ chế lock để tránh multiple concurrent operations
        /// </summary>
        public async void LoadStatisticsAsync()
        {
            // === KIỂM TRA CONCURRENT OPERATION ===
            /// Tránh tình trạng nhiều thao tác async cùng chạy gây xung đột DbContext
            if (_isAsyncOperationRunning)
            {
                return; // Thoát ngay nếu đã có operation đang chạy
            }

            // === THREAD-SAFE LOCKING ===
            /// Sử dụng lock để đảm bảo chỉ một luồng có thể thiết lập _isAsyncOperationRunning
            /// Pattern double-checked locking để tối ưu hiệu suất
            lock (_lockObject)
            {
                if (_isAsyncOperationRunning)
                    return; // Kiểm tra lại sau khi có lock
                _isAsyncOperationRunning = true; // Đánh dấu operation đang chạy
            }

            // === THIẾT LẬP TRẠNG THÁI LOADING ===
            /// Cập nhật UI để hiển thị loading indicator
            IsLoading = true;

            try
            {
                // === PHASE 1: TẢI THỐNG KÊ CƠ BẢN (BACKGROUND THREAD) ===
                /// Sử dụng DbContext riêng biệt cho thao tác tính toán nặng
                /// Chạy trên background thread để không block UI
                using (var context = new ClinicDbContext())
                {
                    // Tải thống kê cơ bản (dashboard metrics) trên background thread
                    await Task.Run(() => LoadBasicStatistics(context));
                }

                // === PHASE 2: TẢI CÁC BIỂU ĐỒ (UI THREAD) ===
                /// Cập nhật biểu đồ trên UI thread vì LiveCharts không thread-safe
                /// Chia thành nhiều DbContext để tránh timeout và conflict
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // === SUB-PHASE 2A: BIỂU ĐỒ DOANH THU ===
                    /// Context riêng cho các biểu đồ liên quan đến doanh thu
                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking; // Tối ưu hiệu suất

                        // Tải các biểu đồ doanh thu
                       
                        LoadRevenueByHourChart(context);       // Doanh thu theo giờ
                        LoadProductDistributionChart(context); // Phân bố sản phẩm
                        LoadTopRevenueDaysChart(context);      // Top ngày doanh thu cao
                        LoadRevenueTrendChart(context);        // Xu hướng doanh thu
                        LoadInvoiceTypeChart(context);         // Doanh thu theo loại hóa đơn
                        LoadServiceRevenueChart(context);      // Doanh thu dịch vụ
                        LoadRevenueByCategoryChart(context);   // Doanh thu theo danh mục
                    }

                    // === SUB-PHASE 2B: BIỂU ĐỒ BỆNH NHÂN VÀ LỊCH HẸN ===
                    /// Context riêng cho dữ liệu bệnh nhân và lịch hẹn
                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải biểu đồ về bệnh nhân và lịch hẹn
                        LoadPatientTypeChart(context);          // Phân loại bệnh nhân
                        LoadAppointmentStatusChart(context);    // Trạng thái lịch hẹn
                        LoadAppointmentPeakHoursChart(context); // Giờ cao điểm lịch hẹn
                        LoadPatientsByDoctorChart(context);     // Bệnh nhân theo bác sĩ
                    }

                    // === SUB-PHASE 2C: DỮ LIỆU TÀI CHÍNH VÀ SẢN PHẨM ===
                    /// Context riêng cho analytics tài chính và sản phẩm
                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải dữ liệu tài chính và thông tin sản phẩm
                        LoadTopSellingProducts(context);  // Top sản phẩm bán chạy
                        LoadTopVIPPatients(context);      // Top bệnh nhân VIP
                        CalculateGrowthRates(context);    // Tính tỷ lệ tăng trưởng
                    }

                    // === SUB-PHASE 2D: CẢNH BÁO THUỐC ===
                    /// Context riêng để tránh vấn đề dịch LINQ phức tạp của medicine logic
                    using (var context = new ClinicDbContext())
                    {
                        context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                        // Tải cảnh báo thuốc (context riêng vì có logic LINQ phức tạp)
                        LoadWarningMedicines(context);
                    }
                });

                // === CẬP NHẬT TRẠNG THÁI COMMANDS ===
                /// Đảm bảo các command có thể được đánh giá lại CanExecute sau khi tải dữ liệu
                /// Quan trọng cho các command export và filter
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                // === XỬ LÝ LỖI VÀ THÔNG BÁO ===
                /// Hiển thị lỗi trên UI thread để đảm bảo MessageBox hiển thị đúng
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxService.ShowError($"Lỗi khi tải thống kê: {ex.Message}", "Lỗi");
                });
            }
            finally
            {
                // === CLEANUP VÀ RESET TRẠNG THÁI ===
                /// Luôn đảm bảo reset trạng thái dù thành công hay thất bại
                /// Quan trọng để tránh UI bị "lock" vĩnh viễn
                IsLoading = false;                    // Tắt loading indicator
                _isAsyncOperationRunning = false;     // Cho phép operation tiếp theo
            }
        }

        private void RefreshData()
        {
            LoadDashBoard();
            using (var context = new ClinicDbContext())
            {
                context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking; // Tối ưu hiệu suất
                LoadBasicStatistics(context);
            }
            LoadStatisticsAsync();
        }
        #region Data Loading Methods - Các phương thức tải dữ liệu từ cơ sở dữ liệu

        /// <summary>
        /// Tải các thống kê cơ bản cho dashboard
        /// Chạy trên background thread để không block UI
        /// Bao gồm: lịch hẹn hôm nay/hôm qua, doanh thu, bệnh nhân mới, thuốc đã bán
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu</param>
        public void LoadBasicStatistics(ClinicDbContext context)
        {
            try
            {
                // === THIẾT LẬP CÁC NGÀY THAM CHIẾU ===
                var today = DateTime.Today;                                    // Ngày hôm nay (00:00:00)
                var firstDayOfMonth = new DateTime(today.Year, today.Month, 1); // Ngày đầu tháng hiện tại
                var yesterday = today.AddDays(-1);                            // Ngày hôm qua

                // === THỐNG KÊ LỊCH HẸN HÔM NAY VÀ HÔM QUA ===
                /// Đếm tổng số lịch hẹn hôm nay (bao gồm tất cả trạng thái)
                int todayAppointments = context.Appointments
                    .Count(a => a.AppointmentDate.Date == today && a.IsDeleted != true);

                /// Đếm tổng số lịch hẹn hôm qua (để tính tăng trưởng)
                int yesterdayAppointments = context.Appointments
                    .Count(a => a.AppointmentDate.Date == yesterday && a.IsDeleted != true);

                // === TÍNH TOÁN TỶ LỆ TĂNG TRƯỞNG LỊCH HẸN ===
                double appointmentPercentage = 0;
                string appointmentGrowth = "0.0%";

                if (yesterdayAppointments > 0)
                {
                    // Có dữ liệu hôm qua để so sánh
                    appointmentPercentage = ((todayAppointments - yesterdayAppointments) / (double)yesterdayAppointments) * 100;

                    // Định dạng với dấu + cho tăng trưởng dương
                    if (appointmentPercentage > 0)
                        appointmentGrowth = $"+{appointmentPercentage:0.0}%";
                    else
                        appointmentGrowth = $"{appointmentPercentage:0.0}%";
                }
                else if (yesterdayAppointments == 0 && todayAppointments > 0)
                {
                    // Hôm qua không có lịch hẹn nhưng hôm nay có
                    appointmentGrowth = "+100.0%";
                    appointmentPercentage = 100.0;
                }
                else if (yesterdayAppointments == 0 && todayAppointments == 0)
                {
                    // Cả hai ngày đều không có lịch hẹn
                    appointmentGrowth = "0.0%";
                    appointmentPercentage = 0;
                }

                // === THỐNG KÊ DOANH THU HÔM NAY ===
                /// Lấy tất cả hóa đơn đã thanh toán trong ngày hôm nay
                var todayInvoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Date == today &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal todayRevenue = todayInvoices.Sum(i => i.TotalAmount);

                // === THỐNG KÊ DOANH THU THÁNG HIỆN TẠI ===
                /// Lấy tất cả hóa đơn từ đầu tháng đến hiện tại
                var tomorrow = today.AddDays(1);

                var monthInvoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value >= firstDayOfMonth &&
                           i.InvoiceDate.Value < tomorrow &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal monthRevenue = monthInvoices.Sum(i => i.TotalAmount);

                // === THỐNG KÊ DOANH THU THEO KHOẢNG THỜI GIAN ĐƯỢC CHỌN ===
                /// Doanh thu trong khoảng StartDate - EndDate (được thiết lập bởi filter)
                var periodInvoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();
                decimal totalRevenue = periodInvoices.Sum(i => i.TotalAmount);

                // === THỐNG KÊ BỆNH NHÂN MỚI ===
                /// Đếm bệnh nhân được tạo trong khoảng thời gian được chọn
                int newPatientsCount = context.Patients
                    .Count(p => p.CreatedAt >= StartDate &&
                           p.CreatedAt <= EndDate &&
                           p.IsDeleted != true);

                // === THỐNG KÊ TỔNG SỐ BỆNH NHÂN ===
                /// Tổng số bệnh nhân hiện có trong hệ thống (chưa bị xóa)
                int totalPatientsCount = context.Patients
                    .Count(p => p.IsDeleted != true);

                // === THỐNG KÊ TỔNG SỐ LỊCH HẸN ===
                /// Tổng số lịch hẹn trong khoảng thời gian được chọn
                int totalAppointmentsCount = context.Appointments
                    .Count(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate);

                // === THỐNG KÊ THUỐC ĐÃ BÁN ===
                /// Đếm tổng số lượng thuốc đã bán (thực hiện trong memory để tránh lỗi translation)
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();
                int medicineSoldCount = invoiceDetails.Sum(id => id.Quantity ?? 0);

                // === CẬP NHẬT UI TRÊN UI THREAD ===
                /// Dispatcher.Invoke đảm bảo cập nhật UI properties trên UI thread
                /// Tránh cross-thread exception khi cập nhật từ background thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TodayAppointmentCount = todayAppointments;
                    YesterdayAppointmentCount = yesterdayAppointments;
                    AppointmentGrowth = appointmentGrowth;
                    AppointmentPercentage = appointmentPercentage;
                    TodayRevenue = todayRevenue;
                    MonthRevenue = monthRevenue;
                    NewPatients = newPatientsCount;
                    TotalPatients = totalPatientsCount;
                    TotalAppointments = totalAppointmentsCount;
                    TotalMedicineSold = medicineSoldCount;
                });
            }
            catch (Exception ex)
            {
                // === XỬ LÝ LỖI ===
                /// Hiển thị lỗi trên UI thread để đảm bảo MessageBox hiển thị đúng
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MessageBoxService.ShowError($"Lỗi khi tải thống kê cơ bản: {ex.Message}", "Lỗi");
                });
            }
        }

        /// <summary>
        /// Tải dữ liệu biểu đồ doanh thu theo 24 giờ trong ngày
        /// Phân tích khung giờ kinh doanh hiệu quả nhất
        /// Hỗ trợ tối ưu hóa ca làm việc và phân bổ nhân lực
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu hóa đơn</param>
        private void LoadRevenueByHourChart(ClinicDbContext context)
        {
            try
            {
                var revenueByHour = new double[24]; // Mảng 24 giờ (0:00 - 23:00)

                // === XỬ LÝ DỮ LIỆU TRONG MEMORY ===
                /// Lấy hóa đơn trong khoảng thời gian được chọn
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                /// Phân nhóm doanh thu theo từng giờ
                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int hour = invoice.InvoiceDate.Value.Hour; // Lấy giờ (0-23)
                        revenueByHour[hour] += (double)invoice.TotalAmount;
                    }
                }

                // === CẬP NHẬT BIỂU ĐỒ ===
                /// Kiểm tra và cập nhật series dữ liệu
                if (RevenueByHourSeries?.Count > 0)
                {
                    var series = RevenueByHourSeries[0] as ColumnSeries;
                    if (series?.Values is ChartValues<double> values)
                    {
                        values.Clear();
                        values.AddRange(revenueByHour);
                        // Định dạng hiển thị tiền tệ cho tooltip
                        series.LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo giờ: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải biểu đồ phân bố sản phẩm theo danh mục thuốc
        /// Hiển thị tỷ lệ đóng góp của từng danh mục vào tổng doanh thu
        /// Sử dụng PieSeries để thể hiện phần trăm một cách trực quan
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu chi tiết hóa đơn</param>
        private void LoadProductDistributionChart(ClinicDbContext context)
        {
            try
            {
                // === LẤY CHI TIẾT HÓA ĐƠN THUỐC ===
                /// Include các entity liên quan để tránh N+1 query problem
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                        .ThenInclude(m => m.Category)
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();

                // === TÍNH TOÁN DOANH THU THEO DANH MỤC ===
                /// Nhóm theo CategoryId và tính tổng doanh thu từ Quantity * SalePrice
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
                    .ToList();

                var totalSales = categorySales.Sum(c => c.TotalSales);

                // === TẠO BIỂU ĐỒ PIE CHART ===
                var newSeries = new SeriesCollection();

                if (totalSales > 0 && categorySales.Any())
                {
                    /// Tạo PieSeries cho từng danh mục có doanh thu
                    foreach (var category in categorySales)
                    {
                        double percentage = (double)((category.TotalSales / totalSales) * 100);

                        newSeries.Add(new PieSeries
                        {
                            Title = category.CategoryName,
                            Values = new ChartValues<double> { Math.Round(percentage, 1) },
                            DataLabels = true,
                            LabelPoint = chartPoint => $"{category.CategoryName}: {chartPoint.Y:0.0}%",
                            Fill = GetRandomBrush() // Màu ngẫu nhiên cho mỗi segment
                        });
                    }
                }
                else
                {
                    /// Hiển thị placeholder khi không có dữ liệu
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

        /// <summary>
        /// Tải biểu đồ top những ngày có doanh thu cao nhất
        /// Hiển thị xu hướng theo thời gian thay vì sắp xếp theo doanh thu
        /// Giúp phân tích mô hình kinh doanh theo thời gian
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu hóa đơn</param>
        private void LoadTopRevenueDaysChart(ClinicDbContext context)
        {
            try
            {
                // === XỬ LÝ DỮ LIỆU TRONG MEMORY ===
                /// Tránh lỗi LINQ translation với các phép toán phức tạp
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                // === NHÓM THEO NGÀY VÀ LẤY TOP 7 DOANH THU CAO NHẤT ===
                var revenueByDate = invoices
                    .GroupBy(i => i.InvoiceDate.Value.Date)
                    .Select(g => new {
                        Date = g.Key,
                        Revenue = g.Sum(i => i.TotalAmount)
                    })
                    .OrderByDescending(x => x.Revenue) // Sắp xếp theo doanh thu giảm dần
                    .Take(7) // Lấy 7 ngày doanh thu cao nhất
                    .OrderBy(x => x.Date) // Sắp xếp lại theo thời gian để hiển thị
                    .ToList();

                if (revenueByDate.Any())
                {
                    /// Có dữ liệu thực tế để hiển thị
                    var values = revenueByDate.Select(x => (double)x.Revenue).ToArray();
                    var labels = revenueByDate.Select(x => x.Date.ToString("dd/MM")).ToArray();

                    TopRevenueDaysSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Doanh thu",
                    Values = new ChartValues<double>(values),
                    Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Màu xanh lá Material Design
                    LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                }
            };

                    TopRevenueDaysLabels = labels;
                }
                else
                {
                    /// Xử lý trường hợp không có dữ liệu - tạo labels mặc định
                    var defaultLabels = Enumerable.Range(1, 7)
                        .Select(i => StartDate.AddDays(i - 1).ToString("dd/MM"))
                        .ToArray();

                    /// Khởi tạo với giá trị 0 nhưng vẫn giữ định dạng đúng
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

        /// <summary>
        /// Tải biểu đồ xu hướng doanh thu theo 12 tháng trong năm
        /// Sử dụng LineSeries với đường cong mượt mà để thể hiện xu hướng
        /// Bao gồm vùng tô màu bên dưới đường để tăng tính trực quan
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu hóa đơn</param>
        private void LoadRevenueTrendChart(ClinicDbContext context)
        {
            try
            {
                var currentYear = DateTime.Now.Year;
                var monthlyRevenue = new double[12]; // 12 tháng

                // === XỬ LÝ DỮ LIỆU TRONG MEMORY ===
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate.HasValue &&
                           i.InvoiceDate.Value.Year == currentYear &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                /// Tính tổng doanh thu cho từng tháng
                foreach (var invoice in invoices)
                {
                    if (invoice.InvoiceDate.HasValue)
                    {
                        int month = invoice.InvoiceDate.Value.Month - 1; // Chuyển về 0-based index
                        monthlyRevenue[month] += (double)invoice.TotalAmount;
                    }
                }

                // === TẠO BIỂU ĐỒ ĐƯỜNG XU HƯỚNG ===
                RevenueTrendSeries = new SeriesCollection
                {
                    new LineSeries
                    {
                        Title = "Xu hướng doanh thu",
                        Values = new ChartValues<double>(monthlyRevenue),
                        PointGeometry = null,               // Không hiển thị điểm trên đường
                        LineSmoothness = 1,                 // Đường cong mượt mà (1 = tối đa)
                        Stroke = new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Màu đỏ Material Design
                        Fill = new SolidColorBrush(Color.FromArgb(50, 244, 67, 54)), // Vùng tô với độ trong suốt 50
                        LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                    }
                };

                RevenueTrendLabels = MonthLabels; // Sử dụng nhãn tháng đã định nghĩa (T1-T12)
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ xu hướng doanh thu: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải biểu đồ doanh thu theo loại hóa đơn
        /// Phân tích đóng góp của từng loại dịch vụ: Khám bệnh, Bán thuốc, Khám và bán thuốc
        /// Hỗ trợ chiến lược phát triển dịch vụ dựa trên hiệu quả kinh doanh
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu hóa đơn</param>
        private void LoadInvoiceTypeChart(ClinicDbContext context)
        {
            try
            {
                /// Định nghĩa 3 loại hóa đơn chuẩn trong hệ thống
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };
                var revenueByType = new double[invoiceTypes.Length];

                // === XỬ LÝ DỮ LIỆU TRONG MEMORY ===
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                           i.InvoiceDate <= EndDate &&
                           i.Status == "Đã thanh toán")
                    .ToList();

                /// Phân loại doanh thu theo từng loại hóa đơn
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

                // === TẠO BIỂU ĐỒ CỘT ===
                /// Kiểm tra có dữ liệu hay không
                if (revenueByType.Sum() > 0)
                {
                    /// Có dữ liệu thực tế
                    InvoiceTypeSeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Doanh thu",
                            Values = new ChartValues<double>(revenueByType),
                            Fill = new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Màu xanh lá
                            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                        }
                    };
                }
                else
                {
                    /// Không có dữ liệu - hiển thị series với giá trị 0
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

        /// <summary>
        /// Tải biểu đồ tỷ lệ doanh thu theo dịch vụ (Pie Chart)
        /// Hiển thị phần trăm đóng góp của từng loại dịch vụ vào tổng doanh thu
        /// Bao gồm cả số tiền cụ thể trong tooltip để dễ phân tích
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu hóa đơn</param>
        private void LoadServiceRevenueChart(ClinicDbContext context)
        {
            try
            {
                var invoiceTypes = new[] { "Khám bệnh", "Bán thuốc", "Khám và bán thuốc" };

                // === XỬ LÝ DỮ LIỆU TRONG MEMORY ===
                var invoices = context.Invoices
                    .Where(i => i.InvoiceDate >= StartDate &&
                         i.InvoiceDate <= EndDate &&
                         i.Status == "Đã thanh toán")
                    .ToList();

                var totalRevenue = invoices.Sum(i => i.TotalAmount);

                var seriesCollection = new SeriesCollection();

                if (totalRevenue > 0)
                {
                    /// Tạo PieSeries cho từng loại dịch vụ có doanh thu
                    foreach (var type in invoiceTypes)
                    {
                        var typeRevenue = invoices
                            .Where(i => i.InvoiceType == type)
                            .Sum(i => i.TotalAmount);

                        if (typeRevenue > 0)
                        {
                            double percentage = Math.Round((double)((typeRevenue / totalRevenue) * 100), 1);

                            /// Phân bổ màu sắc theo loại dịch vụ
                            var brushColor = type switch
                            {
                                "Khám bệnh" => Color.FromRgb(76, 175, 80),   // Xanh lá
                                "Bán thuốc" => Color.FromRgb(255, 152, 0),   // Cam
                                _ => Color.FromRgb(33, 150, 243)             // Xanh dương (default)
                            };

                            seriesCollection.Add(new PieSeries
                            {
                                Title = type,
                                Values = new ChartValues<double> { percentage },
                                DataLabels = true,
                                // Tooltip hiển thị cả phần trăm và số tiền
                                LabelPoint = chartPoint => $"{type}: {chartPoint.Y:0.0}% ({string.Format("{0:N0} VNĐ", typeRevenue)})",
                                Fill = new SolidColorBrush(brushColor)
                            });
                        }
                    }
                }

                // === XỬ LÝ TRƯỜNG HỢP KHÔNG CÓ DỮ LIỆU ===
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

        /// <summary>
        /// Tải biểu đồ phân loại bệnh nhân theo loại (VIP, thường, BHYT, v.v.)
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu bệnh nhân và loại bệnh nhân</param>
        private void LoadPatientTypeChart(ClinicDbContext context)
        {
            try
            {
                // === LẤY CÁC LOẠI BỆNH NHÂN ===
                var patientTypes = context.PatientTypes
                    .Where(pt => pt.IsDeleted != true)
                    .ToList();

                var seriesCollection = new SeriesCollection();

                // === XỬ LÝ DỮ LIỆU BỆNH NHÂN TRONG MEMORY ===
                /// Lọc bệnh nhân mới trong khoảng thời gian được chọn
                var patients = context.Patients
                    .Where(p => p.IsDeleted != true &&
                          p.CreatedAt >= StartDate &&
                          p.CreatedAt <= EndDate)
                    .ToList();

                var totalPatients = patients.Count;

                if (totalPatients > 0 && patientTypes.Any())
                {
                    /// Tính phần trăm cho từng loại bệnh nhân
                    foreach (var type in patientTypes)
                    {
                        var patientCount = patients.Count(p => p.PatientTypeId == type.PatientTypeId);
                        double percentage = Math.Round((double)(patientCount * 100) / totalPatients, 1);

                        if (percentage > 0)
                        {
                            /// Phân bổ màu sắc theo PatientTypeId
                            var colorIndex = type.PatientTypeId % 5;
                            var colors = new[]
                            {
                                Color.FromRgb(244, 67, 54),   // Đỏ
                                Color.FromRgb(33, 150, 243),  // Xanh dương
                                Color.FromRgb(76, 175, 80),   // Xanh lá
                                Color.FromRgb(255, 152, 0),   // Cam
                                Color.FromRgb(156, 39, 176)   // Tím
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

                // === XỬ LÝ TRƯỜNG HỢP KHÔNG CÓ DỮ LIỆU ===
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

        /// <summary>
        /// Tải biểu đồ trạng thái lịch hẹn
        /// Phân tích hiệu quả quản lý lịch hẹn: Đang chờ, Đã khám, Đã hủy, Đang khám
        /// Hỗ trợ tối ưu hóa quy trình làm việc và giảm tỷ lệ hủy lịch
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu lịch hẹn</param>
        private void LoadAppointmentStatusChart(ClinicDbContext context)
        {
            try
            {
                // === LẤY TẤT CẢ LỊCH HẸN TRONG KHOẢNG THỜI GIAN ===
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                           a.AppointmentDate <= EndDate &&
                           a.IsDeleted != true)
                    .ToList();

                var statusCounts = new double[AppointmentStatusLabels.Length];

                // === ĐẾM LỊCH HẸN CHO TỪNG TRẠNG THÁI ===
                /// Sử dụng AppointmentStatusLabels đã định nghĩa: ["Đang chờ", "Đã khám", "Đã hủy", "Đang khám"]
                foreach (var status in AppointmentStatusLabels)
                {
                    int index = Array.IndexOf(AppointmentStatusLabels, status);
                    if (index >= 0)
                    {
                        statusCounts[index] = appointments.Count(a => a.Status == status);
                    }
                }

                // === CẬP NHẬT DỮ LIỆU BIỂU ĐỒ ===
                if (AppointmentStatusSeries?.Count > 0)
                {
                    var series = AppointmentStatusSeries[0] as ColumnSeries;
                    if (series?.Values is ChartValues<double> values)
                    {
                        values.Clear();

                        /// Kiểm tra có dữ liệu hay không
                        if (statusCounts.Sum() > 0)
                        {
                            values.AddRange(statusCounts);
                        }
                        else
                        {
                            /// Thêm giá trị 0 nếu không có dữ liệu
                            values.AddRange(Enumerable.Repeat(0.0, AppointmentStatusLabels.Length));
                        }

                        /// Đảm bảo formatter hiển thị số nguyên
                        series.LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ trạng thái lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải biểu đồ giờ cao điểm đặt lịch hẹn
        /// Phân tích khung giờ trong ngày để xác định khung giờ có nhiều lịch hẹn nhất
        /// Hỗ trợ tối ưu hóa lịch làm việc và phân bổ nhân lực theo giờ
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu lịch hẹn</param>
        private void LoadAppointmentPeakHoursChart(ClinicDbContext context)
        {
            try
            {
                // Chỉ lấy giờ từ 7h đến 17h (11 giờ: 7,8,...,17)
                int startHour = 7;
                int endHour = 17;
                int hourCount = endHour - startHour + 1;
                var appointmentsByHour = new double[hourCount];

                // Lấy danh sách lịch hẹn trong khoảng thời gian và chưa bị xóa
                var appointments = context.Appointments
                    .Where(a => a.AppointmentDate >= StartDate &&
                                a.AppointmentDate <= EndDate &&
                                a.IsDeleted != true)
                    .ToList();

                // Đếm số lịch hẹn cho từng giờ trong khung giờ làm việc
                foreach (var appointment in appointments)
                {
                    int hour = appointment.AppointmentDate.Hour;
                    if (hour >= startHour && hour <= endHour)
                    {
                        appointmentsByHour[hour - startHour]++;
                    }
                }

                // Tạo biểu đồ chỉ cho khung giờ làm việc
                if (appointments.Any())
                {
                    AppointmentPeakHoursSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lịch hẹn",
                    Values = new ChartValues<double>(appointmentsByHour),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Màu cam
                    LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0))
                }
            };
                }
                else
                {
                    AppointmentPeakHoursSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Số lịch hẹn",
                    Values = new ChartValues<double>(Enumerable.Repeat(0.0, hourCount)),
                    Fill = new SolidColorBrush(Color.FromRgb(255, 152, 0)),
                    LabelPoint = point => string.Format("{0:N0}", Math.Round(point.Y, 0))
                }
            };
                }

                // Nếu cần, có thể cập nhật nhãn trục X cho biểu đồ (giờ từ 7 đến 17)
                RevenueByTimeLabels = Enumerable.Range(startHour, hourCount)
                    .Select(h => $"{h}:00").ToArray();
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ giờ cao điểm lịch hẹn: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải biểu đồ số lượng bệnh nhân theo từng bác sĩ
        /// Đánh giá hiệu suất làm việc và phân bổ công việc giữa các bác sĩ
        /// Chỉ tính bệnh nhân unique (không trùng lặp) để đảm bảo độ chính xác
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu nhân viên và lịch hẹn</param>
        private void LoadPatientsByDoctorChart(ClinicDbContext context)
        {
            try
            {
                // === LẤY DANH SÁCH BÁC SĨ ===
                /// Chỉ lấy nhân viên có RoleId = 1 (bác sĩ) và chưa bị xóa
                var doctors = context.Staffs
                    .Where(s => s.RoleId == 1 && s.IsDeleted != true)
                    .ToList();

                if (!doctors.Any())
                {
                    /// Không có bác sĩ nào - thiết lập biểu đồ trống
                    PatientsByStaffseries = new SeriesCollection();
                    DoctorLabels = new string[0];
                    return;
                }

                // === TÍNH TOÁN SỐ LƯỢNG BỆNH NHÂN CHO TỪNG BÁC SĨ ===
                var doctorData = new List<(string DoctorName, int PatientCount)>();

                /// Thiết lập bộ lọc thời gian chính xác
                var startDateFilter = StartDate.Date;
                var endDateFilter = EndDate.Date.AddDays(1).AddSeconds(-1); // Cuối ngày được chọn

                /// Đếm bệnh nhân unique cho từng bác sĩ
                foreach (var doctor in doctors)
                {
                    var patientCount = context.Appointments
                        .Where(a => a.StaffId == doctor.StaffId &&
                                   a.AppointmentDate >= startDateFilter &&
                                   a.AppointmentDate <= endDateFilter)
                        .Select(a => a.PatientId)
                        .Distinct() // Đảm bảo không đếm trùng bệnh nhân
                        .Count();

                    /// Thêm tất cả bác sĩ, kể cả không có bệnh nhân
                    doctorData.Add((doctor.FullName ?? "Unknown", patientCount));
                }

                /// Sắp xếp theo số lượng bệnh nhân giảm dần
                doctorData = doctorData.OrderByDescending(d => d.PatientCount).ToList();

                // === TẠO BIỂU ĐỒ ===
                var patientSeries = new ColumnSeries
                {
                    Title = "Số lượng bệnh nhân",
                    Values = new ChartValues<int>(doctorData.Select(d => d.PatientCount)),
                    Fill = GetRandomBrush(), // Màu ngẫu nhiên
                    DataLabels = true // Hiển thị số liệu trên cột
                };

                /// Cập nhật nhãn và dữ liệu biểu đồ
                DoctorLabels = doctorData.Select(d => d.DoctorName).ToArray();
                PatientsByStaffseries = new SeriesCollection { patientSeries };
            }
            catch (Exception ex)
            {
                /// Ghi log lỗi và thiết lập biểu đồ trống
       
                PatientsByStaffseries = new SeriesCollection();
                DoctorLabels = new string[0];
            }
        }

        /// <summary>
        /// Tải danh sách top sản phẩm bán chạy nhất
        /// Phân tích hiệu quả kinh doanh từng loại thuốc để tối ưu hóa tồn kho
        /// Tính toán phần trăm đóng góp vào tổng doanh thu thuốc
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu chi tiết hóa đơn và thuốc</param>
        private void LoadTopSellingProducts(ClinicDbContext context)
        {
            try
            {
                // === LẤY CHI TIẾT HÓA ĐƠN THUỐC ===
                /// Sử dụng AsNoTracking cho hiệu suất tốt hơn với truy vấn chỉ đọc
                var invoiceDetails = context.InvoiceDetails
                    .AsNoTracking()
                    .Include(id => id.Invoice)
                    .Include(id => id.Medicine)
                        .ThenInclude(m => m.Category) // Include danh mục thuốc
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)
                    .ToList();

                // === TÍNH TOÁN DOANH THU THEO TỪNG THUỐC ===
                /// Nhóm theo MedicineId và tính tổng doanh thu từ Quantity * SalePrice
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
                    .OrderByDescending(x => x.Sales) // Sắp xếp theo doanh thu giảm dần
                    .Take(10) // Lấy top 10 sản phẩm
                    .ToList();

                var totalSales = medicineSales.Sum(m => m.Sales);

                if (totalSales > 0)
                {
                    /// Tính phần trăm đóng góp cho từng sản phẩm
                    foreach (var product in medicineSales)
                    {
                        product.Percentage = (int)Math.Round((product.Sales / totalSales) * 100);
                    }

                    /// Cập nhật UI trên UI thread
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        TopSellingProducts = new ObservableCollection<TopSellingProduct>(medicineSales);
                    });
                }
                else
                {
                    /// Không có dữ liệu - xóa danh sách hiện tại
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

        /// <summary>
        /// Tải danh sách top bệnh nhân VIP theo tổng chi tiêu
        /// Phân tích khách hàng có giá trị cao để xây dựng chương trình chăm sóc đặc biệt
        /// Bao gồm thông tin loại bệnh nhân để phân khúc khách hàng
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu bệnh nhân và hóa đơn</param>
        private void LoadTopVIPPatients(ClinicDbContext context)
        {
            try
            {
                IsLoading = true;

                // === THIẾT LẬP BỘ LỌC THỜI GIAN ===
                var startDateFilter = StartDate.Date;
                var endDateFilter = EndDate.Date.AddDays(1).AddSeconds(-1); // Cuối ngày được chọn

                // === LẤY BỆNH NHÂN VÀ TÍNH TỔNG CHI TIÊU ===
                /// Bao gồm tất cả loại bệnh nhân, không chỉ VIP
                var topPatients = context.Patients
                    .Where(p => p.IsDeleted != true)
                    .Select(p => new
                    {
                        Patient = p,
                        /// Tính tổng chi tiêu từ các hóa đơn đã thanh toán trong khoảng thời gian
                        TotalSpending = context.Invoices
                            .Where(i => i.PatientId == p.PatientId &&
                                        i.Status == "Đã thanh toán" &&
                                        i.InvoiceDate >= startDateFilter &&
                                        i.InvoiceDate <= endDateFilter)
                            .Sum(i => i.TotalAmount)
                    })
                    .Where(x => x.TotalSpending > 0) // Chỉ lấy bệnh nhân có chi tiêu
                    .OrderByDescending(x => x.TotalSpending) // Sắp xếp theo chi tiêu giảm dần
                    .Take(10) // Lấy top 10 bệnh nhân chi tiêu nhiều nhất
                    .ToList();

                // === THÊM THÔNG TIN LOẠI BỆNH NHÂN ===
                /// Enrich dữ liệu với thông tin PatientType để hiển thị
                var enrichedPatients = topPatients.Select(x =>
                {
                    var patientType = context.PatientTypes
                        .FirstOrDefault(pt => pt.PatientTypeId == x.Patient.PatientTypeId);

                    return new VIPPatient
                    {
                        Id = x.Patient.PatientId,
                        FullName = x.Patient.FullName,
                        Phone = x.Patient.Phone,
                        TotalSpending = x.TotalSpending,
                        PatientType = patientType?.TypeName ?? "Unknown" // Thêm loại bệnh nhân để hiển thị
                    };
                }).ToList();

                // === CẬP NHẬT UI ===
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TopVIPPatients = new ObservableCollection<VIPPatient>(enrichedPatients);
                });
            }
            catch (Exception ex)
            {
                /// Xử lý lỗi một cách thích hợp
                Application.Current.Dispatcher.Invoke(() =>
                {
                    TopVIPPatients = new ObservableCollection<VIPPatient>();
                });
            }
            finally
            {
                IsLoading = false;
            }
        }

     
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
                var sixtyDaysLater = today.AddDays(60);

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
                        else if (stockIn.ExpiryDate.HasValue && stockIn.ExpiryDate.Value <= sixtyDaysLater)
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
        private void CalculateGrowthRates(ClinicDbContext context)
        {
            try
            {
                // === TÍNH TOÁN KHOẢNG THỜI GIAN KỲ TRƯỚC ===
                /// Xác định kỳ trước có cùng độ dài với kỳ hiện tại
                /// Ví dụ: nếu kỳ hiện tại là 10 ngày, kỳ trước cũng sẽ là 10 ngày
                var previousPeriodStart = StartDate.AddDays(-(EndDate - StartDate).TotalDays);
                var previousPeriodEnd = StartDate.AddDays(-1); // Kết thúc ngay trước kỳ hiện tại

                // === TÍNH TỶ LỆ TĂNG TRƯỞNG BỆNH NHÂN MỚI ===
                /// Đếm số bệnh nhân mới được tạo trong kỳ hiện tại
                var currentPeriodPatients = context.Patients
                    .Count(p => p.CreatedAt >= StartDate &&
                           p.CreatedAt <= EndDate &&
                           p.IsDeleted != true);

                /// Đếm số bệnh nhân mới được tạo trong kỳ trước
                var previousPeriodPatients = context.Patients
                    .Count(p => p.CreatedAt >= previousPeriodStart &&
                           p.CreatedAt <= previousPeriodEnd &&
                           p.IsDeleted != true);

                /// Áp dụng logic tương tự như tăng trưởng doanh thu
                if (previousPeriodPatients > 0)
                {
                    /// Trường hợp bình thường: có bệnh nhân mới kỳ trước để so sánh
                    var patientGrowth = ((currentPeriodPatients - previousPeriodPatients) / (double)previousPeriodPatients) * 100;

                
                    if (patientGrowth > 0)
                        PatientGrowth = $"+{patientGrowth:0.0}%";
                    else
                        PatientGrowth = $"{patientGrowth:0.0}%";
                }
                else if (previousPeriodPatients == 0 && currentPeriodPatients > 0)
                {
                    /// Kỳ trước không có bệnh nhân mới, kỳ này có
                    PatientGrowth = "+100.0%";
                }
                else if (previousPeriodPatients == 0 && currentPeriodPatients == 0)
                {
                    /// Cả hai kỳ đều không có bệnh nhân mới
                    PatientGrowth = "0.0%";
                }
                else
                {
                    /// Trường hợp không xác định được
                    PatientGrowth = "N/A";
                }
            }
            catch (Exception ex)
            {
                // === XỬ LÝ LỖI ===
                /// Ghi log lỗi và thiết lập giá trị mặc định
                MessageBoxService.ShowError($"Lỗi khi tính toán tỷ lệ tăng trưởng: {ex.Message}", "Lỗi");
        
                PatientGrowth = "N/A";
            }
        }

        /// <summary>
        /// Tải biểu đồ doanh thu theo từng danh mục thuốc
        /// Phân tích hiệu quả kinh doanh và đóng góp của từng danh mục vào tổng doanh thu
        /// Giúp xác định danh mục thuốc nào mang lại lợi nhuận cao nhất
        /// </summary>
        /// <param name="context">DbContext để truy xuất dữ liệu chi tiết hóa đơn và danh mục thuốc</param>
        private void LoadRevenueByCategoryChart(ClinicDbContext context)
        {
            try
            {
                // === LẤY DANH SÁCH TẤT CẢ DANH MỤC THUỐC ===
                /// Chỉ lấy các danh mục chưa bị xóa để đảm bảo tính chính xác
                var medicineCategories = context.MedicineCategories
                    .Where(c => c.IsDeleted != true)
                    .ToList();

                // === LẤY CHI TIẾT HÓA ĐƠN THUỐC TRONG KHOẢNG THỜI GIAN ===
                /// Include các thực thể liên quan để tránh lazy loading và N+1 query
                /// Chỉ lấy hóa đơn đã thanh toán và có mặt hàng là thuốc
                var invoiceDetails = context.InvoiceDetails
                    .Include(id => id.Invoice)              // Thông tin hóa đơn
                    .Include(id => id.Medicine)             // Thông tin thuốc
                    .ThenInclude(m => m.Category)           // Thông tin danh mục thuốc
                    .Where(id => id.Invoice.InvoiceDate >= StartDate &&
                           id.Invoice.InvoiceDate <= EndDate &&
                           id.Invoice.Status == "Đã thanh toán" &&
                           id.MedicineId != null)          // Chỉ lấy các dòng có thuốc
                    .ToList();

                // === TÍNH TỔNG DOANH THU THEO TỪNG DANH MỤC ===
                /// Nhóm theo CategoryId và tính tổng doanh thu = Quantity × SalePrice
                /// Lọc ra các thuốc có đầy đủ thông tin danh mục
                var categoryRevenues = invoiceDetails
                    .Where(id => id.Medicine?.Category != null)
                    .GroupBy(id => id.Medicine.Category.CategoryId)
                    .Select(g => new
                    {
                        CategoryId = g.Key,
                        CategoryName = g.First().Medicine.Category.CategoryName,
                        TotalRevenue = g.Sum(id => id.Quantity * id.SalePrice) ?? 0
                    })
                    .OrderByDescending(x => x.TotalRevenue) // Sắp xếp theo doanh thu giảm dần
                    .ToList();

                // === CHUẨN BỊ DỮ LIỆU CHO BIỂU ĐỒ ===
                /// Tách riêng tên danh mục và giá trị doanh thu để binding
                var categoryNames = new List<string>();
                var revenueValues = new List<double>();

                foreach (var categoryRevenue in categoryRevenues)
                {
                    categoryNames.Add(categoryRevenue.CategoryName);
                    revenueValues.Add((double)categoryRevenue.TotalRevenue);
                }

                // === TẠO BIỂU ĐỒ CỘT DOANH THU ===
                /// Kiểm tra có dữ liệu hay không để hiển thị phù hợp
                if (revenueValues.Any())
                {
                    /// Có dữ liệu thực tế - tạo biểu đồ với dữ liệu thực
                    RevenueByCategorySeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Doanh thu",
                            Values = new ChartValues<double>(revenueValues),
                            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)), // Màu xanh dương Material Design
                            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y) // Định dạng tiền tệ VN
                        }
                    };

                    CategoryLabels = categoryNames.ToArray();
                }
                else
                {
                    /// Không có dữ liệu - tạo biểu đồ placeholder
                    RevenueByCategorySeries = new SeriesCollection
                    {
                        new ColumnSeries
                        {
                            Title = "Doanh thu",
                            Values = new ChartValues<double> { 0 }, // Giá trị 0 cho placeholder
                            Fill = new SolidColorBrush(Color.FromRgb(33, 150, 243)),
                            LabelPoint = point => string.Format("{0:N0} VNĐ", point.Y)
                        }
                    };

                    CategoryLabels = new[] { "Không có dữ liệu" }; // Nhãn placeholder
                }
            }
            catch (Exception ex)
            {
                // === XỬ LÝ LỖI ===
                /// Hiển thị thông báo lỗi chi tiết cho người dùng
                MessageBoxService.ShowError($"Lỗi khi tải biểu đồ doanh thu theo danh mục: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Tải dữ liệu dashboard chính của ứng dụng
        /// Bao gồm lịch hẹn hôm nay, thống kê tổng quát và các chỉ số quan trọng
        /// Sử dụng AsNoTracking để tối ưu hiệu suất cho dữ liệu chỉ đọc
        /// </summary>
        public void LoadDashBoard()
        {
            try
            {
                // === THIẾT LẬP NGÀY HIỆN TẠI ===
                /// Lấy ngày hiện tại để lọc và hiển thị dữ liệu dashboard
                CurrentDate = DateTime.Now.Date;
                TodayAppointments = new ObservableCollection<TodayAppointment>();

                // === SỬ DỤNG CONTEXT RIÊNG BIỆT VỚI ASNOTRACKING ===
                /// Tạo context riêng cho dashboard để tránh xung đột
                /// AsNoTracking để tăng hiệu suất vì chỉ đọc dữ liệu
                using (var context = new ClinicDbContext())
                {
                    context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                    // === TẢI TẤT CẢ LỊCH HẸN VỚI DỮ LIỆU LIÊN QUAN ===
                    /// Include các thực thể liên quan để tránh lazy loading
                    /// Lọc ra các lịch hẹn chưa bị xóa
                    var appointments = context.Appointments
                        .AsNoTracking()
                        .Where(a => a.IsDeleted == false || a.IsDeleted == null)
                        .Include(a => a.Patient)        // Thông tin bệnh nhân
                        .Include(a => a.Staff)          // Thông tin nhân viên/bác sĩ
                        .Include(a => a.AppointmentType) // Loại lịch hẹn
                        .OrderBy(a => a.AppointmentDate) // Sắp xếp theo thời gian
                        .ToList();

                    // === TÍNH SỐ LỊCH HẸN ĐANG CHỜ ===
                    /// Đếm các lịch hẹn có trạng thái "Đang chờ" (sau khi trim khoảng trắng)
                    int waitingCount = appointments.Count(a => a.Status.Trim() == "Đang chờ");

                    // === TẠO DANH SÁCH LỊCH HẸN HÔM NAY CHO DASHBOARD ===
                    /// Lọc các lịch hẹn trong ngày hôm nay và tạo đối tượng TodayAppointment
                    foreach (var appointment in appointments.Where(a => a.AppointmentDate.Date == CurrentDate.Date))
                    {
                        TodayAppointment app = new TodayAppointment
                        {
                            Appointment = appointment,
                            Initials = GetInitialsFromFullName(appointment.Patient?.FullName), // Tạo chữ cái đầu
                            PatientName = appointment.Patient?.FullName,
                            DoctorName = appointment.Staff?.FullName,
                            Notes = appointment.Notes,
                            Status = appointment.Status?.Trim(),
                            Time = appointment.AppointmentDate.TimeOfDay // Chỉ lấy phần thời gian
                        };
                        TodayAppointments.Add(app);
                    }

                    // === CẬP NHẬT CÁC CHỈ SỐ DASHBOARD ===
                    /// Tổng số lịch hẹn hôm nay
                    TotalAppointments = appointments.Count(a => a.AppointmentDate.Date == CurrentDate.Date);

                    /// Số lịch hẹn đang chờ (hiển thị dưới dạng string)
                    PendingAppointments = waitingCount.ToString();

                    /// Tổng số bệnh nhân trong hệ thống (chưa bị xóa)
                    TotalPatients = context.Patients.Count(p => p.IsDeleted != true);
                }
            }
            catch (Exception ex)
            {
                // === XỬ LÝ LỖI ===
                /// Hiển thị lỗi chi tiết khi không thể tải dashboard
                MessageBoxService.ShowError($"Lỗi khi tải Dashboard: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Trích xuất chữ cái đầu từ họ tên đầy đủ của bệnh nhân
        /// Sử dụng để hiển thị avatar chữ cái trong danh sách lịch hẹn
        /// Logic: lấy chữ cái đầu của từ áp cuối và từ cuối cùng
        /// </summary>
        /// <param name="fullName">Họ tên đầy đủ của bệnh nhân</param>
        /// <returns>Chuỗi chữ cái đầu, ví dụ: "N.A" cho "Nguyễn Văn An"</returns>
        private string GetInitialsFromFullName(string fullName)
        {
            // === KIỂM TRA ĐẦU VÀO ===
            /// Trả về chuỗi rỗng nếu tên null hoặc chỉ có khoảng trắng
            if (string.IsNullOrWhiteSpace(fullName))
                return string.Empty;

            // === TÁCH CÁC TỪNG TỪ TRONG TÊN ===
            /// Split theo khoảng trắng và loại bỏ các phần tử rỗng
            /// Lọc ra các từ không null/empty để đảm bảo tính chính xác
            var parts = fullName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToArray();

            // === LOGIC TẠO CHỮ CÁI ĐẦU ===
            if (parts.Length >= 2)
            {
                /// Trường hợp có ít nhất 2 từ: lấy từ áp cuối và từ cuối
                /// Ví dụ: "Nguyễn Văn An" → "V.A"
                var middle = parts[parts.Length - 2]; // Từ áp cuối
                var last = parts[parts.Length - 1];   // Từ cuối
                return $"{char.ToUpper(middle[0])}.{char.ToUpper(last[0])}";
            }
            else if (parts.Length == 1)
            {
                /// Trường hợp chỉ có 1 từ: lấy chữ cái đầu của từ đó
                /// Ví dụ: "An" → "A"
                return char.ToUpper(parts[0][0]).ToString();
            }

            /// Trường hợp không xác định được (lý thuyết không xảy ra)
            return string.Empty;
        }
        #endregion


        #region Action Methods
        /// <summary>
        /// Lọc dữ liệu thống kê theo ngày hiện tại (hôm nay)
        /// Thiết lập StartDate = ngày hôm nay (00:00:00), EndDate = thời điểm hiện tại
        /// Cập nhật text hiển thị và tải lại dữ liệu thống kê
        /// </summary>
        private void FilterByDay()
        {
            StartDate = DateTime.Now.Date;                          // Đặt ngày bắt đầu là đầu ngày hôm nay
            EndDate = DateTime.Now;                                 // Đặt ngày kết thúc là thời điểm hiện tại
            CurrentFilterText = "Đang xem: Hôm nay";               // Cập nhật text hiển thị bộ lọc
            LoadStatisticsAsync();                                  // Tải lại dữ liệu thống kê
        }

        /// <summary>
        /// Lọc dữ liệu thống kê theo tháng hiện tại
        /// Thiết lập StartDate = ngày đầu tháng hiện tại, EndDate = thời điểm hiện tại
        /// Đây là bộ lọc mặc định khi khởi động ứng dụng
        /// </summary>
        private void FilterByMonth()
        {
            StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);  // Ngày đầu tháng hiện tại
            EndDate = DateTime.Now;                                              // Thời điểm hiện tại
            CurrentFilterText = "Đang xem: Tháng này";                          // Cập nhật text hiển thị
            LoadStatisticsAsync();                                               // Tải lại dữ liệu thống kê
        }

        /// <summary>
        /// Lọc dữ liệu thống kê theo quý hiện tại
        /// Tự động xác định quý dựa trên tháng hiện tại:
        /// - Quý 1: Tháng 1-3 (T1, T2, T3)
        /// - Quý 2: Tháng 4-6 (T4, T5, T6)  
        /// - Quý 3: Tháng 7-9 (T7, T8, T9)
        /// - Quý 4: Tháng 10-12 (T10, T11, T12)
        /// </summary>
        private void FilterByQuarter()
        {
            int currentMonth = DateTime.Now.Month;                               // Lấy tháng hiện tại (1-12)
            int quarterNumber = (currentMonth - 1) / 3 + 1;                     // Tính số quý (1-4)
            int quarterStartMonth = (quarterNumber - 1) * 3 + 1;                // Tính tháng bắt đầu quý

            StartDate = new DateTime(DateTime.Now.Year, quarterStartMonth, 1);   // Ngày đầu quý
            EndDate = DateTime.Now;                                              // Thời điểm hiện tại
            CurrentFilterText = $"Đang xem: Quý {quarterNumber}";               // Hiển thị quý hiện tại
            LoadStatisticsAsync();                                               // Tải lại dữ liệu thống kê
        }

        /// <summary>
        /// Lọc dữ liệu thống kê theo năm hiện tại
        /// Thiết lập StartDate = 01/01 năm hiện tại, EndDate = thời điểm hiện tại
        /// Phù hợp cho báo cáo tổng kết cuối năm và phân tích xu hướng dài hạn
        /// </summary>
        private void FilterByYear()
        {
            StartDate = new DateTime(DateTime.Now.Year, 1, 1);                  // Ngày đầu năm hiện tại
            EndDate = DateTime.Now;                                              // Thời điểm hiện tại
            CurrentFilterText = $"Đang xem: Năm {DateTime.Now.Year}";           // Hiển thị năm hiện tại
            LoadStatisticsAsync();                                               // Tải lại dữ liệu thống kê
        }

        /// <summary>
        /// Hiển thị chi tiết danh sách thuốc cần cảnh báo
        /// Chỉ kích hoạt khi có thuốc cần chú ý (LowStockCount > 0)
        /// Hiển thị popup với thông tin đầy đủ về từng loại cảnh báo
        /// </summary>
        private void ViewLowStock()
        {
            if (WarningMedicines.Count > 0)
            {
                // Tạo chuỗi text hiển thị tất cả cảnh báo
                var warningText = string.Join("\n", WarningMedicines.Select(m => $"{m.Name}: {m.WarningMessage}"));
                MessageBoxService.ShowError($"Danh sách thuốc cần chú ý:\n\n{warningText}",
                               "Cảnh báo tồn kho");
            }
            else
            {
                // Thông báo khi không có thuốc nào cần chú ý
                MessageBoxService.ShowError("Không có thuốc nào cần chú ý trong kho.",
                               "Thông báo");
            }
        }

        /// <summary>
        /// Tạo màu sắc ngẫu nhiên cho các biểu đồ
        /// Sử dụng bảng màu Material Design để đảm bảo tính thẩm mỹ
        /// Trả về SolidColorBrush để sử dụng trong LiveCharts
        /// </summary>
        /// <returns>SolidColorBrush với màu ngẫu nhiên từ bảng màu định sẵn</returns>
        private SolidColorBrush GetRandomBrush()
        {
            // Bảng màu Material Design đã được chọn lọc
            var colors = new[]
            {
                Color.FromRgb(66, 133, 244),  // Blue - Xanh dương
                Color.FromRgb(219, 68, 55),   // Red - Đỏ
                Color.FromRgb(244, 180, 0),   // Yellow - Vàng
                Color.FromRgb(15, 157, 88),   // Green - Xanh lá
                Color.FromRgb(171, 71, 188)   // Purple - Tím
            };

            Random random = new Random();
            return new SolidColorBrush(colors[random.Next(colors.Length)]);
        }

        /// <summary>
        /// Xuất báo cáo thống kê doanh thu ra file Excel
        /// Sử dụng ClosedXML với progress dialog và background thread để tối ưu UX
        /// Bao gồm các biểu đồ: doanh thu theo ngày, loại hóa đơn, xu hướng và theo giờ
        /// File được lưu với tên format: ThongKeDoanhThu_dd-MM-yyyy.xlsx
        /// </summary>
        private void ExportRevenueToExcel()
        {
            try
            {
                // === TẠO DIALOG CHỌN VỊ TRÍ LƯU FILE ===
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",                        // Chỉ cho phép file Excel
                    DefaultExt = "xlsx",                                            // Extension mặc định
                    Title = "Chọn vị trí lưu file Excel",                          // Tiêu đề dialog
                    FileName = $"ThongKeDoanhThu_{DateTime.Now:dd-MM-yyyy}.xlsx"   // Tên file với ngày hiện tại
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // === TẠO VÀ HIỂN THỊ PROGRESS DIALOG ===
                    ProgressDialog progressDialog = new ProgressDialog();

                    // === BẮT ĐẦU XỬ LÝ XUẤT EXCEL TRONG BACKGROUND THREAD ===
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // === BÁO CÁO TIẾN TRÌNH: 5% - TẠO WORKBOOK ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // === XÁC ĐỊNH TIÊU ĐỀ DỰA TRÊN BỘ LỌC HIỆN TẠI ===
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê doanh thu {period}");

                                // === THÊM TIÊU ĐỀ CHÍNH (MERGED CELLS) ===
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ DOANH THU {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === THÊM NGÀY HIỆN TẠI (MERGED CELLS) ===
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === BÁO CÁO TIẾN TRÌNH: 10% - THÊM HEADER ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // === THEO DÕI VỊ TRÍ HÀNG HIỆN TẠI ===
                                int currentRow = 4;

                                // === XUẤT TỪNG BIỂU ĐỒ THÀNH CÁC BẢNG RIÊNG BIỆT ===

                                // Xuất doanh thu theo ngày
                                currentRow = ExportTopRevenueDaysChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));

                                // Xuất doanh thu theo loại hóa đơn  
                                currentRow = ExportInvoiceTypeChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                                // Xuất xu hướng doanh thu
                                currentRow = ExportRevenueTrendChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                // Xuất doanh thu theo giờ
                                currentRow = ExportRevenueByHourChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // === ĐỊNH DẠNG WORKSHEET ĐỂ DỄ ĐỌC HƠN ===
                                worksheet.Columns().AdjustToContents();

                                // === LƯU WORKBOOK ===
                                workbook.SaveAs(saveFileDialog.FileName);

                                // === BÁO CÁO TIẾN TRÌNH: 100% - HOÀN THÀNH ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Hiển thị 100% trong thời gian ngắn

                                // === ĐÓNG PROGRESS DIALOG VÀ HIỂN THỊ THÔNG BÁO THÀNH CÔNG ===
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê doanh thu thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // === HỎI NGƯỜI DÙNG CÓ MUỐN MỞ FILE EXCEL KHÔNG ===
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
                            // === ĐÓNG PROGRESS DIALOG KHI CÓ LỖI ===
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // === HIỂN THỊ DIALOG - SẼ CHẶN CHO ĐẾN KHI DIALOG ĐÓNG ===
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Xuất báo cáo thống kê bệnh nhân ra file Excel
        /// Bao gồm: phân tích theo loại bệnh nhân và danh sách bệnh nhân VIP
        /// Sử dụng pattern copy data trước khi chuyển sang background thread để tránh cross-thread issues
        /// File được lưu với tên format: ThongKeBenhNhan_dd-MM-yyyy.xlsx
        /// </summary>
        private void ExportPatientsToExcel()
        {
            try
            {
                // === TẠO DIALOG CHỌN VỊ TRÍ LƯU FILE ===
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",                        // Chỉ cho phép file Excel
                    DefaultExt = "xlsx",                                            // Extension mặc định
                    Title = "Chọn vị trí lưu file Excel",                          // Tiêu đề dialog
                    FileName = $"ThongKeBenhNhan_{DateTime.Now:dd-MM-yyyy}.xlsx"   // Tên file với ngày hiện tại
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // === TẠO VÀ HIỂN THỊ PROGRESS DIALOG ===
                    ProgressDialog progressDialog = new ProgressDialog();

                    // === TẠO BẢN SAO DỮ LIỆU TRÊN UI THREAD TRƯỚC KHI CHUYỂN SANG BACKGROUND THREAD ===
                    /// Pattern này tránh cross-thread exception khi truy cập ObservableCollection từ background thread
                    var patientTypeSeriesCopy = new List<(string Title, double Value)>();
                    if (PatientTypeSeries != null)
                    {
                        // Trích xuất dữ liệu từ PieSeries của LiveCharts
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

                    // Tạo bản sao danh sách bệnh nhân VIP
                    var topVIPPatientsCopy = TopVIPPatients?.ToList() ?? new List<VIPPatient>();

                    // === BẮT ĐẦU XỬ LÝ XUẤT EXCEL TRONG BACKGROUND THREAD ===
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // === BÁO CÁO TIẾN TRÌNH: 5% - TẠO WORKBOOK ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // === XÁC ĐỊNH TIÊU ĐỀ DỰA TRÊN BỘ LỌC HIỆN TẠI ===
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê bệnh nhân {period}");

                                // === THÊM TIÊU ĐỀ CHÍNH ===
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ BỆNH NHÂN {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === THÊM NGÀY HIỆN TẠI ===
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === BÁO CÁO TIẾN TRÌNH: 10% - THÊM HEADER ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // === THEO DÕI VỊ TRÍ HÀNG HIỆN TẠI ===
                                int currentRow = 4;

                                // === XUẤT TỪNG BIỂU ĐỒ THÀNH CÁC BẢNG RIÊNG BIỆT SỬ DỤNG BẢN SAO ===

                                // Xuất biểu đồ phân loại bệnh nhân
                                currentRow = ExportPatientTypeChartFromCopy(worksheet, currentRow, patientTypeSeriesCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(40));

                                // Xuất bảng bệnh nhân VIP
                                currentRow = ExportTopVIPPatientsTableFromCopy(worksheet, currentRow, topVIPPatientsCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(80));

                                // === ĐỊNH DẠNG WORKSHEET ĐỂ DỄ ĐỌC HƠN ===
                                worksheet.Columns().AdjustToContents();

                                // === LƯU WORKBOOK ===
                                workbook.SaveAs(saveFileDialog.FileName);

                                // === BÁO CÁO TIẾN TRÌNH: 100% - HOÀN THÀNH ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Hiển thị 100% trong thời gian ngắn

                                // === ĐÓNG PROGRESS DIALOG VÀ HIỂN THỊ THÔNG BÁO THÀNH CÔNG ===
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê bệnh nhân thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // === HỎI NGƯỜI DÙNG CÓ MUỐN MỞ FILE EXCEL KHÔNG ===
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
                            // === ĐÓNG PROGRESS DIALOG KHI CÓ LỖI ===
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // === HIỂN THỊ DIALOG - SẼ CHẶN CHO ĐẾN KHI DIALOG ĐÓNG ===
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }
        /// <summary>
        /// Xuất báo cáo thống kê lịch hẹn ra file Excel
        /// Bao gồm: phân bố theo trạng thái, giờ cao điểm và thống kê theo bác sĩ
        /// File được lưu với tên format: ThongKeLichHen_dd-MM-yyyy.xlsx
        /// Hỗ trợ phân tích hiệu quả hoạt động và phân bổ công việc trong phòng khám
        /// </summary>
        private void ExportAppointmentsToExcel()
        {
            try
            {
                // === TẠO DIALOG CHỌN VỊ TRÍ LƯU FILE ===
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",                        // Chỉ cho phép file Excel
                    DefaultExt = "xlsx",                                            // Extension mặc định
                    Title = "Chọn vị trí lưu file Excel",                          // Tiêu đề dialog
                    FileName = $"ThongKeLichHen_{DateTime.Now:dd-MM-yyyy}.xlsx"   // Tên file với ngày hiện tại
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // === TẠO VÀ HIỂN THỊ PROGRESS DIALOG ===
                    ProgressDialog progressDialog = new ProgressDialog();

                    // === BẮT ĐẦU XỬ LÝ XUẤT EXCEL TRONG BACKGROUND THREAD ===
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // === BÁO CÁO TIẾN TRÌNH: 5% - TẠO WORKBOOK ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                // === XÁC ĐỊNH TIÊU ĐỀ DỰA TRÊN BỘ LỌC HIỆN TẠI ===
                                string period = GetCurrentPeriodTitle();
                                var worksheet = workbook.Worksheets.Add($"Thống kê lịch hẹn {period}");

                                // === THÊM TIÊU ĐỀ CHÍNH (MERGED CELLS) ===
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ LỊCH HẸN {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === THÊM NGÀY HIỆN TẠI (MERGED CELLS) ===
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === BÁO CÁO TIẾN TRÌNH: 10% - THÊM HEADER ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // === THEO DÕI VỊ TRÍ HÀNG HIỆN TẠI ===
                                int currentRow = 4;

                                // === XUẤT TỪNG BIỂU ĐỒ THÀNH CÁC BẢNG RIÊNG BIỆT ===

                                // Xuất thống kê lịch hẹn theo trạng thái
                                currentRow = ExportAppointmentStatusChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(40));

                                // Xuất thống kê giờ cao điểm đặt lịch hẹn
                                currentRow = ExportAppointmentPeakHoursChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                // Xuất thống kê số lượng bệnh nhân theo bác sĩ
                                currentRow = ExportPatientsByDoctorChart(worksheet, currentRow);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // === ĐỊNH DẠNG WORKSHEET ĐỂ DỄ ĐỌC HƠN ===
                                worksheet.Columns().AdjustToContents();

                                // === LƯU WORKBOOK ===
                                workbook.SaveAs(saveFileDialog.FileName);

                                // === BÁO CÁO TIẾN TRÌNH: 100% - HOÀN THÀNH ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Hiển thị 100% trong thời gian ngắn

                                // === ĐÓNG PROGRESS DIALOG VÀ HIỂN THỊ THÔNG BÁO THÀNH CÔNG ===
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê lịch hẹn thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // === HỎI NGƯỜI DÙNG CÓ MUỐN MỞ FILE EXCEL KHÔNG ===
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
                            // === ĐÓNG PROGRESS DIALOG KHI CÓ LỖI ===
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // === HIỂN THỊ DIALOG - SẼ CHẶN CHO ĐẾN KHI DIALOG ĐÓNG ===
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Xuất báo cáo thống kê thuốc ra file Excel
        /// Bao gồm: doanh thu theo danh mục, phân bố sản phẩm, top thuốc bán chạy, cảnh báo tồn kho
        /// File được lưu với tên format: ThongKeThuoc_dd-MM-yyyy.xlsx
        /// Đặc biệt hữu ích cho quản lý kho và chiến lược kinh doanh thuốc
        /// Có code màu cho các mức độ cảnh báo khác nhau (đỏ: cần tiêu hủy, cam: sắp hết hạn, xanh: tồn kho thấp)
        /// </summary>
        private void ExportMedicineToExcel()
        {
            try
            {
                // === TẠO DIALOG CHỌN VỊ TRÍ LƯU FILE ===
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel files (*.xlsx)|*.xlsx",                        // Chỉ cho phép file Excel
                    DefaultExt = "xlsx",                                            // Extension mặc định
                    Title = "Chọn vị trí lưu file Excel",                          // Tiêu đề dialog
                    FileName = $"ThongKeThuoc_{DateTime.Now:dd-MM-yyyy}.xlsx"     // Tên file với ngày hiện tại
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // === TẠO VÀ HIỂN THỊ PROGRESS DIALOG ===
                    ProgressDialog progressDialog = new ProgressDialog();

                    // === TẠO BẢN SAO DỮ LIỆU TRÊN UI THREAD TRƯỚC KHI CHUYỂN SANG BACKGROUND THREAD ===
                    /// Pattern này tránh cross-thread exception khi truy cập ObservableCollection từ background thread
                    var revenueByCategorySeries = new List<(string Category, double Value)>();
                    if (RevenueByCategorySeries?.Count > 0 && CategoryLabels?.Length > 0)
                    {
                        // Trích xuất dữ liệu từ ColumnSeries của LiveCharts
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

                    // === SAO CHÉP CÁC COLLECTION KHÁC ===
                    var productDistributionCopy = new List<(string Title, double Value)>();
                    if (ProductDistributionSeries != null)
                    {
                        // Trích xuất dữ liệu từ PieSeries của LiveCharts
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

                    // Sao chép danh sách sản phẩm bán chạy và cảnh báo thuốc
                    var topSellingProductsCopy = TopSellingProducts?.ToList() ?? new List<TopSellingProduct>();
                    var warningMedicinesCopy = WarningMedicines?.ToList() ?? new List<WarningMedicine>();

                    // Lấy tiêu đề period trên UI thread
                    string period = GetCurrentPeriodTitle();

                    // === BẮT ĐẦU XỬ LÝ XUẤT EXCEL TRONG BACKGROUND THREAD ===
                    Task.Run(() =>
                    {
                        try
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // === BÁO CÁO TIẾN TRÌNH: 5% - TẠO WORKBOOK ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(5));

                                var worksheet = workbook.Worksheets.Add($"Thống kê thuốc {period}");

                                // === THÊM TIÊU ĐỀ CHÍNH ===
                                worksheet.Cell(1, 1).Value = $"THỐNG KÊ THUỐC {period.ToUpper()}";
                                var titleRange = worksheet.Range(1, 1, 1, 10);
                                titleRange.Merge();
                                titleRange.Style.Font.Bold = true;
                                titleRange.Style.Font.FontSize = 16;
                                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === THÊM NGÀY HIỆN TẠI ===
                                worksheet.Cell(2, 1).Value = $"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                                var dateRange = worksheet.Range(2, 1, 2, 10);
                                dateRange.Merge();
                                dateRange.Style.Font.Italic = true;
                                dateRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                                // === BÁO CÁO TIẾN TRÌNH: 10% - THÊM HEADER ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(10));

                                // === THEO DÕI VỊ TRÍ HÀNG HIỆN TẠI ===
                                int currentRow = 4;

                                // === XUẤT TỪNG BIỂU ĐỒ THÀNH CÁC BẢNG RIÊNG BIỆT SỬ DỤNG BẢN SAO ===

                                // Xuất biểu đồ doanh thu theo danh mục thuốc
                                currentRow = ExportRevenueByCategoryChartFromCopy(worksheet, currentRow, revenueByCategorySeries);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(30));

                                // Xuất biểu đồ phân bố sản phẩm theo danh mục
                                currentRow = ExportProductDistributionChartFromCopy(worksheet, currentRow, productDistributionCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(50));

                                // Xuất bảng top thuốc bán chạy nhất
                                currentRow = ExportTopSellingProductsTableFromCopy(worksheet, currentRow, topSellingProductsCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(70));

                                // Xuất bảng cảnh báo tồn kho với color coding
                                currentRow = ExportWarningMedicinesTableFromCopy(worksheet, currentRow, warningMedicinesCopy);
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(90));

                                // === ĐỊNH DẠNG WORKSHEET ĐỂ DỄ ĐỌC HƠN ===
                                worksheet.Columns().AdjustToContents();

                                // === LƯU WORKBOOK ===
                                workbook.SaveAs(saveFileDialog.FileName);

                                // === BÁO CÁO TIẾN TRÌNH: 100% - HOÀN THÀNH ===
                                Application.Current.Dispatcher.Invoke(() => progressDialog.UpdateProgress(100));
                                Thread.Sleep(300); // Hiển thị 100% trong thời gian ngắn

                                // === ĐÓNG PROGRESS DIALOG VÀ HIỂN THỊ THÔNG BÁO THÀNH CÔNG ===
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressDialog.Close();

                                    MessageBoxService.ShowSuccess(
                                        $"Đã xuất thống kê thuốc thành công!\nĐường dẫn: {saveFileDialog.FileName}",
                                        "Thành công"
                                    );

                                    // === HỎI NGƯỜI DÙNG CÓ MUỐN MỞ FILE EXCEL KHÔNG ===
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
                            // === ĐÓNG PROGRESS DIALOG KHI CÓ LỖI ===
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                progressDialog.Close();
                                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                            });
                        }
                    });

                    // === HIỂN THỊ DIALOG - SẼ CHẶN CHO ĐẾN KHI DIALOG ĐÓNG ===
                    progressDialog.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBoxService.ShowError($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
            }
        }

        /// <summary>
        /// Lấy tiêu đề mô tả cho khoảng thời gian lọc hiện tại
        /// Được sử dụng trong tiêu đề các file xuất Excel và báo cáo
        /// Phân tích CurrentFilterText để xác định loại filter đang áp dụng
        /// </summary>
        /// <returns>Chuỗi mô tả khoảng thời gian, ví dụ: "tháng 12-2024", "quý 4-2024", "(25-12-2024)"</returns>
        private string GetCurrentPeriodTitle()
        {
            // === PHÂN TÍCH TEXT BỘ LỌC HIỆN TẠI ĐỂ XÁC ĐỊNH LOẠI KHOẢNG THỜI GIAN ===

            if (CurrentFilterText.Contains("Hôm nay"))
            {
                /// Bộ lọc theo ngày hiện tại - hiển thị ngày cụ thể trong ngoặc
                return $"({DateTime.Now:dd-MM-yyyy})";
            }
            else if (CurrentFilterText.Contains("Tháng"))
            {
                /// Bộ lọc theo tháng hiện tại - hiển thị tháng và năm
                return $"tháng {DateTime.Now.Month}-{DateTime.Now.Year}";
            }
            else if (CurrentFilterText.Contains("Quý"))
            {
                /// Bộ lọc theo quý hiện tại - tính toán số quý dựa trên tháng hiện tại
                int quarterNumber = (DateTime.Now.Month - 1) / 3 + 1;
                return $"quý {quarterNumber}-{DateTime.Now.Year}";
            }
            else if (CurrentFilterText.Contains("Năm"))
            {
                /// Bộ lọc theo năm hiện tại - chỉ hiển thị năm
                return $"năm {DateTime.Now.Year}";
            }
            else
            {
                /// Bộ lọc tùy chỉnh - hiển thị khoảng ngày từ StartDate đến EndDate
                /// Được sử dụng khi người dùng chọn khoảng thời gian tùy ý
                return $"từ {StartDate:dd-MM-yyyy} đến {EndDate:dd-MM-yyyy}";
            }
        }
        #endregion

        #region Export Chart Methods

        /// <summary>
        /// Exports Top Revenue Days chart to Excel
        /// </summary>
        /// <summary>
        /// Xuất biểu đồ top ngày có doanh thu cao nhất ra Excel
        /// Tạo bảng với cột Ngày và Doanh thu, kèm hàng tổng cộng
        /// Định dạng số tiền theo chuẩn Việt Nam với phân cách hàng nghìn
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportTopRevenueDaysChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            /// Tạo tiêu đề "DOANH THU THEO NGÀY" với merged cells và định dạng đậm
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO NGÀY";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Tạo tiêu đề cột với định dạng chuyên nghiệp
            worksheet.Cell(startRow, 1).Value = "Ngày";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // === ĐỊNH DẠNG HEADER ===
            /// Áp dụng style cho header: đậm, nền xám, căn giữa
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            /// Kiểm tra có dữ liệu biểu đồ và labels không
            if (TopRevenueDaysSeries?.Count > 0 && TopRevenueDaysLabels?.Length > 0)
            {
                // Trích xuất ColumnSeries từ SeriesCollection
                var series = TopRevenueDaysSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    /// Lặp qua từng ngày và ghi dữ liệu tương ứng
                    for (int i = 0; i < TopRevenueDaysLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = TopRevenueDaysLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        // Định dạng số tiền với phân cách hàng nghìn
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    /// Tính tổng doanh thu của tất cả các ngày
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            /// Tạo khoảng cách 2 hàng trước section tiếp theo
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ doanh thu theo loại hóa đơn ra Excel
        /// Tạo bảng với cột Loại hóa đơn, Doanh thu và Phần trăm
        /// Bao gồm 3 loại: Khám bệnh, Bán thuốc, Khám và bán thuốc
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportInvoiceTypeChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO LOẠI HÓA ĐƠN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Tạo 3 cột: Loại hóa đơn, Doanh thu và Phần trăm
            worksheet.Cell(startRow, 1).Value = "Loại hóa đơn";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            if (InvoiceTypeSeries?.Count > 0 && InvoiceTypeLabels?.Length > 0)
            {
                var series = InvoiceTypeSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    double total = values.Sum(); // Tính tổng để tính phần trăm

                    /// Ghi dữ liệu cho từng loại hóa đơn
                    for (int i = 0; i < InvoiceTypeLabels.Length; i++)
                    {
                        double value = i < values.Count ? values[i] : 0;
                        double percentage = total > 0 ? (value / total) * 100 : 0;

                        worksheet.Cell(startRow, 1).Value = InvoiceTypeLabels[i];
                        worksheet.Cell(startRow, 2).Value = value;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0"; // Định dạng tiền
                        worksheet.Cell(startRow, 3).Value = percentage;
                        worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%"; // Định dạng phần trăm
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = total;
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    worksheet.Cell(startRow, 3).Value = 1; // 100%
                    worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ xu hướng doanh thu theo thời gian ra Excel
        /// Hiển thị doanh thu theo 12 tháng trong năm để phân tích xu hướng
        /// Sử dụng dữ liệu từ LineSeries của LiveCharts
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportRevenueTrendChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "XU HƯỚNG DOANH THU THEO THỜI GIAN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Chỉ có 2 cột: Thời gian và Doanh thu
            worksheet.Cell(startRow, 1).Value = "Thời gian";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            if (RevenueTrendSeries?.Count > 0 && RevenueTrendLabels?.Length > 0)
            {
                // Trích xuất LineSeries (khác với ColumnSeries ở các method khác)
                var series = RevenueTrendSeries[0] as LiveCharts.Wpf.LineSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    /// Ghi dữ liệu xu hướng theo từng tháng
                    for (int i = 0; i < RevenueTrendLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = RevenueTrendLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ doanh thu theo giờ trong ngày ra Excel
        /// Phân tích 24 giờ (0:00-23:00) để xác định khung giờ kinh doanh hiệu quả
        /// Hỗ trợ tối ưu hóa lịch làm việc và phân bổ nhân lực theo giờ
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportRevenueByHourChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO GIỜ TRONG NGÀY";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "Giờ";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            if (RevenueByHourSeries?.Count > 0)
            {
                var series = RevenueByHourSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    /// Sử dụng HourLabels (0:00-23:00) thay vì labels riêng
                    /// Ghi dữ liệu cho 24 giờ trong ngày
                    for (int i = 0; i < HourLabels.Length; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = HourLabels[i];
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

   

        /// <summary>
        /// Xuất biểu đồ lịch hẹn theo trạng thái ra Excel
        /// Tạo bảng với 3 cột: Trạng thái, Số lượng, Phần trăm
        /// Bao gồm các trạng thái: Đang chờ, Đã khám, Đã hủy, Đang khám
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportAppointmentStatusChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "LỊCH HẸN THEO TRẠNG THÁI";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "Trạng thái";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            if (AppointmentStatusSeries?.Count > 0 && AppointmentStatusLabels?.Length > 0)
            {
                // Trích xuất ColumnSeries từ SeriesCollection
                var series = AppointmentStatusSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    double total = values.Sum(); // Tính tổng để tính phần trăm

                    /// Ghi dữ liệu cho từng trạng thái lịch hẹn
                    for (int i = 0; i < AppointmentStatusLabels.Length; i++)
                    {
                        double value = i < values.Count ? values[i] : 0;
                        double percentage = total > 0 ? (value / total) * 100 : 0;

                        worksheet.Cell(startRow, 1).Value = AppointmentStatusLabels[i];
                        worksheet.Cell(startRow, 2).Value = value;
                        worksheet.Cell(startRow, 3).Value = percentage / 100; // Chia 100 để Excel hiểu là percentage
                        worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%"; // Định dạng phần trăm
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = total;
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Value = 1; // 100%
                    worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ giờ cao điểm đặt lịch hẹn ra Excel
        /// Phân tích 24 giờ trong ngày để tìm khung giờ có nhiều lịch hẹn nhất
        /// Hỗ trợ tối ưu hóa lịch làm việc và phân bổ nhân lực theo giờ
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportAppointmentPeakHoursChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "PEAK HOURS ĐẶT LỊCH NHIỀU NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "Giờ";
            worksheet.Cell(startRow, 2).Value = "Số lịch hẹn";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU ===
            if (AppointmentPeakHoursSeries?.Count > 0)
            {
                var series = AppointmentPeakHoursSeries[0] as LiveCharts.Wpf.ColumnSeries;

                if (series?.Values is LiveCharts.ChartValues<double> values)
                {
                    // Chỉ xuất dữ liệu từ 7h đến 17h
                    int startHour = 7;
                    int endHour = 17;
                    int hourCount = endHour - startHour + 1;

                    for (int i = 0; i < hourCount; i++)
                    {
                        worksheet.Cell(startRow, 1).Value = $"{startHour + i}:00";
                        worksheet.Cell(startRow, 2).Value = i < values.Count ? values[i] : 0;
                        startRow++;
                    }

                    // === THÊM HÀNG TỔNG CỘNG ===
                    worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                    worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                    worksheet.Cell(startRow, 2).Value = values.Take(hourCount).Sum();
                    worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                    startRow++;
                }
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ số lượng bệnh nhân theo từng bác sĩ ra Excel
        /// Đánh giá hiệu suất làm việc và phân bổ công việc giữa các bác sĩ
        /// Xử lý an toàn các trường hợp: không có dữ liệu, lỗi định dạng, không trích xuất được giá trị
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportPatientsByDoctorChart(IXLWorksheet worksheet, int startRow)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "SỐ LƯỢNG BỆNH NHÂN CỦA TỪNG BÁC SĨ";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "Bác sĩ";
            worksheet.Cell(startRow, 2).Value = "Số bệnh nhân";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 2);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === KIỂM TRA DỮ LIỆU CÓ TỒN TẠI KHÔNG ===
            /// Debug logging hoặc verification để đảm bảo có dữ liệu
            bool hasData = PatientsByStaffseries?.Count > 0 && DoctorLabels?.Length > 0;
            if (!hasData)
            {
                // Xử lý trường hợp không có dữ liệu - thêm ghi chú vào worksheet
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                worksheet.Range(startRow, 1, startRow, 2).Merge();
                startRow += 2;
                return startRow;
            }

            // === THỬ LẤY SERIES MỘT CÁCH AN TOÀN ===
            /// Lấy series đầu tiên và cast về base type Series
            var series = PatientsByStaffseries.FirstOrDefault() as LiveCharts.Wpf.Series;
            if (series == null)
            {
                worksheet.Cell(startRow, 1).Value = "Lỗi định dạng dữ liệu";
                worksheet.Range(startRow, 1, startRow, 2).Merge();
                startRow += 2;
                return startRow;
            }

            // === TRÍCH XUẤT VALUES (XỬ LÝ CÁC LOẠI SERIES KHÁC NHAU) ===
            /// Xử lý an toàn cho cả ColumnSeries và các loại series khác
            IList<double> values;
            if (series is LiveCharts.Wpf.ColumnSeries columnSeries && columnSeries.Values is LiveCharts.ChartValues<double> doubleValues)
            {
                values = doubleValues;
            }
            else if (series.Values is LiveCharts.ChartValues<double> otherDoubleValues)
            {
                values = otherDoubleValues;
            }
            else
            {
                // Xử lý trường hợp values không tương thích
                worksheet.Cell(startRow, 1).Value = "Không thể trích xuất giá trị";
                worksheet.Range(startRow, 1, startRow, 2).Merge();
                startRow += 2;
                return startRow;
            }

            // === ĐẢM BẢO CÓ ĐỦ VALUES ===
            /// Lấy độ dài nhỏ hơn giữa labels và values để tránh index out of bounds
            int maxLength = Math.Min(DoctorLabels.Length, values.Count);
            double totalPatients = 0;

            // === THÊM CÁC HÀNG DỮ LIỆU ===
            for (int i = 0; i < maxLength; i++)
            {
                worksheet.Cell(startRow, 1).Value = DoctorLabels[i];
                double patientCount = values[i];
                worksheet.Cell(startRow, 2).Value = patientCount;
                totalPatients += patientCount; // Cộng dồn để tính tổng
                startRow++;
            }

            // === THÊM HÀNG TỔNG CỘNG ===
            worksheet.Cell(startRow, 1).Value = "Tổng cộng";
            worksheet.Cell(startRow, 1).Style.Font.Bold = true;
            worksheet.Cell(startRow, 2).Value = totalPatients;
            worksheet.Cell(startRow, 2).Style.Font.Bold = true;
            startRow++;

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }
        /// <summary>
        /// Xuất biểu đồ phân tích theo loại bệnh nhân ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues khi export trong background thread
        /// Tạo bảng với 3 cột: Loại bệnh nhân, Số lượng, Phần trăm
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="patientTypes">Bản sao dữ liệu loại bệnh nhân đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportPatientTypeChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Title, double Value)> patientTypes)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "PHÂN TÍCH THEO LOẠI BỆNH NHÂN";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Tạo 3 cột tiêu chuẩn cho phân tích dữ liệu loại bệnh nhân
            worksheet.Cell(startRow, 1).Value = "Loại bệnh nhân";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU TỪ BẢN SAO ===
            /// Sử dụng bản sao dữ liệu đã được chuẩn bị trên UI thread
            /// Tránh cross-thread exception khi truy cập ObservableCollection từ background thread
            if (patientTypes != null && patientTypes.Count > 0)
            {
                double total = patientTypes.Sum(pt => pt.Value);

                /// Ghi dữ liệu cho từng loại bệnh nhân
                foreach (var item in patientTypes)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Title;
                    worksheet.Cell(startRow, 2).Value = item.Value;
                    worksheet.Cell(startRow, 3).Value = percentage / 100; // Excel percentage format
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // === THÊM HÀNG TỔNG CỘNG ===
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1; // 100%
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất bảng top bệnh nhân VIP (chi tiêu nhiều nhất) ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues
        /// Tạo bảng với 4 cột: ID, Họ và tên, Số điện thoại, Tổng chi tiêu
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="patients">Bản sao danh sách bệnh nhân VIP đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportTopVIPPatientsTableFromCopy(IXLWorksheet worksheet, int startRow, List<VIPPatient> patients)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "TOP BỆNH NHÂN CHI TIÊU NHIỀU NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Họ và tên";
            worksheet.Cell(startRow, 3).Value = "Số điện thoại";
            worksheet.Cell(startRow, 4).Value = "Tổng chi tiêu";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 4);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU TỪ BẢN SAO ===
            if (patients != null && patients.Count > 0)
            {
                /// Ghi thông tin từng bệnh nhân VIP
                foreach (var patient in patients)
                {
                    worksheet.Cell(startRow, 1).Value = patient.Id;
                    worksheet.Cell(startRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    worksheet.Cell(startRow, 2).Value = patient.FullName;
                    worksheet.Cell(startRow, 3).Value = patient.Phone;
                    worksheet.Cell(startRow, 4).Value = patient.TotalSpending;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0"; // Định dạng tiền VNĐ
                    startRow++;
                }

                // === THÊM HÀNG TỔNG CỘNG ===
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
           
                worksheet.Cell(startRow, 4).Value = patients.Sum(p => p.TotalSpending);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                startRow++;
            }
            else
            {
                // === XỬ LÝ TRƯỜNG HỢP KHÔNG CÓ DỮ LIỆU ===
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 4);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ doanh thu theo danh mục thuốc ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues
        /// Tạo bảng với 3 cột: Danh mục, Doanh thu, Phần trăm
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="categories">Bản sao dữ liệu danh mục đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportRevenueByCategoryChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Category, double Value)> categories)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "DOANH THU THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Doanh thu";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU TỪ BẢN SAO ===
            if (categories != null && categories.Count > 0)
            {
                double total = categories.Sum(c => c.Value);

                /// Ghi dữ liệu doanh thu cho từng danh mục thuốc
                foreach (var category in categories)
                {
                    double percentage = total > 0 ? (category.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = category.Category;
                    worksheet.Cell(startRow, 2).Value = category.Value;
                    worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0"; // Định dạng tiền VNĐ
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // === THÊM HÀNG TỔNG CỘNG ===
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(startRow, 3).Value = 1; // 100%
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất biểu đồ phân bố sản phẩm theo danh mục thuốc ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues
        /// Tạo bảng với 3 cột: Danh mục, Số lượng, Phần trăm
        /// Khác với RevenueByCategoryChart ở chỗ này là số lượng thay vì doanh thu
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="categories">Bản sao dữ liệu phân bố sản phẩm đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportProductDistributionChartFromCopy(IXLWorksheet worksheet, int startRow, List<(string Title, double Value)> categories)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "PHÂN BỔ THEO DANH MỤC THUỐC";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Khác với revenue chart: cột 2 là "Số lượng" thay vì "Doanh thu"
            worksheet.Cell(startRow, 1).Value = "Danh mục";
            worksheet.Cell(startRow, 2).Value = "Số lượng";
            worksheet.Cell(startRow, 3).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU TỪ BẢN SAO ===
            if (categories != null && categories.Count > 0)
            {
                double total = categories.Sum(item => item.Value);

                /// Ghi dữ liệu phân bố số lượng cho từng danh mục
                foreach (var item in categories)
                {
                    double percentage = total > 0 ? (item.Value / total) * 100 : 0;

                    worksheet.Cell(startRow, 1).Value = item.Title;
                    worksheet.Cell(startRow, 2).Value = item.Value; // Số lượng (không cần format tiền)
                    worksheet.Cell(startRow, 3).Value = percentage / 100;
                    worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // === THÊM HÀNG TỔNG CỘNG ===
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 2).Value = total;
                worksheet.Cell(startRow, 2).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Value = 1; // 100%
                worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                worksheet.Cell(startRow, 3).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất bảng top thuốc bán chạy nhất ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues
        /// Tạo bảng với 5 cột: ID, Tên thuốc, Danh mục, Doanh thu, Phần trăm
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="products">Bản sao danh sách sản phẩm bán chạy đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportTopSellingProductsTableFromCopy(IXLWorksheet worksheet, int startRow, List<TopSellingProduct> products)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "TOP THUỐC BÁN CHẠY NHẤT";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            /// Tạo 5 cột cho thông tin chi tiết sản phẩm
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Danh mục";
            worksheet.Cell(startRow, 4).Value = "Doanh thu";
            worksheet.Cell(startRow, 5).Value = "Phần trăm";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 5);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU TỪ BẢN SAO ===
            if (products != null && products.Count > 0)
            {
                /// Ghi thông tin chi tiết từng sản phẩm bán chạy
                foreach (var product in products)
                {
                    worksheet.Cell(startRow, 1).Value = product.Id;
                    worksheet.Cell(startRow, 2).Value = product.Name;
                    worksheet.Cell(startRow, 3).Value = product.Category;
                    worksheet.Cell(startRow, 4).Value = product.Sales;
                    worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0"; // Định dạng tiền VNĐ
                    worksheet.Cell(startRow, 5).Value = product.Percentage / 100;
                    worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                    startRow++;
                }

                // === THÊM HÀNG TỔNG CỘNG ===
                worksheet.Cell(startRow, 1).Value = "Tổng cộng";
                worksheet.Cell(startRow, 1).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Value = products.Sum(p => p.Sales);
                worksheet.Cell(startRow, 4).Style.Font.Bold = true;
                worksheet.Cell(startRow, 4).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(startRow, 5).Value = 1; // 100%
                worksheet.Cell(startRow, 5).Style.Font.Bold = true;
                worksheet.Cell(startRow, 5).Style.NumberFormat.Format = "0.00%";
                startRow++;
            }
            else
            {
                // === XỬ LÝ TRƯỜNG HỢP KHÔNG CÓ DỮ LIỆU ===
                worksheet.Cell(startRow, 1).Value = "Không có dữ liệu";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 5);
                noDataRange.Merge(); // Merge 5 cột để hiển thị thông báo
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }

        /// <summary>
        /// Xuất bảng cảnh báo tồn kho thuốc ra Excel từ bản sao dữ liệu
        /// Sử dụng pattern copy data để tránh cross-thread issues
        /// Tạo bảng với 3 cột: ID, Tên thuốc, Cảnh báo
        /// Áp dụng color-coding để phân biệt mức độ nghiêm trọng
        /// </summary>
        /// <param name="worksheet">Worksheet Excel để ghi dữ liệu</param>
        /// <param name="startRow">Hàng bắt đầu ghi dữ liệu</param>
        /// <param name="medicines">Bản sao danh sách cảnh báo thuốc đã được extract từ UI thread</param>
        /// <returns>Hàng tiếp theo sau khi hoàn thành việc ghi</returns>
        private int ExportWarningMedicinesTableFromCopy(IXLWorksheet worksheet, int startRow, List<WarningMedicine> medicines)
        {
            // === THÊM TIÊU ĐỀ SECTION ===
            worksheet.Cell(startRow, 1).Value = "CẢNH BÁO TỒN KHO";
            var sectionTitleRange = worksheet.Range(startRow, 1, startRow, 5);
            sectionTitleRange.Merge();
            sectionTitleRange.Style.Font.Bold = true;
            sectionTitleRange.Style.Font.FontSize = 14;
            startRow += 1;

            // === THÊM HEADER ROW ===
            worksheet.Cell(startRow, 1).Value = "ID";
            worksheet.Cell(startRow, 2).Value = "Tên thuốc";
            worksheet.Cell(startRow, 3).Value = "Cảnh báo";

            // === ĐỊNH DẠNG HEADER ===
            var headerRange = worksheet.Range(startRow, 1, startRow, 3);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            startRow += 1;

            // === THÊM DỮ LIỆU VỚI COLOR-CODING TỪ BẢN SAO ===
            if (medicines != null && medicines.Count > 0)
            {
                /// Ghi thông tin cảnh báo cho từng thuốc với màu sắc phân biệt
                foreach (var medicine in medicines)
                {
                    worksheet.Cell(startRow, 1).Value = medicine.Id;
                    worksheet.Cell(startRow, 2).Value = medicine.Name;
                    worksheet.Cell(startRow, 3).Value = medicine.WarningMessage;

                    // === PHÂN BIỆT MÀU SẮC THEO MỨC ĐỘ NGHIÊM TRỌNG ===
                    /// Áp dụng color-coding với null-safe checking để tránh NullReferenceException
                    if (medicine.WarningMessage?.Contains("CẦN TIÊU HỦY") == true)
                    {
                        // Màu đỏ + Bold cho cảnh báo nghiêm trọng nhất - thuốc hết hạn cần tiêu hủy
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Red;
                        worksheet.Cell(startRow, 3).Style.Font.Bold = true;
                    }
                    else if (medicine.WarningMessage?.Contains("SẮP HẾT HẠN") == true)
                    {
                        // Màu cam cho cảnh báo trung bình - thuốc sắp hết hạn
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Orange;
                    }
                    else if (medicine.WarningMessage?.Contains("TỒN KHO THẤP") == true)
                    {
                        // Màu xanh cho cảnh báo thấp - tồn kho thấp cần nhập thêm
                        worksheet.Cell(startRow, 3).Style.Font.FontColor = XLColor.Blue;
                    }
                    // Note: Các cảnh báo khác như "LÔ CUỐI CÙNG" và "LỖI CẤU HÌNH" giữ màu mặc định

                    startRow++;
                }
            }
            else
            {
                // === XỬ LÝ TRƯỜNG HỢP KHÔNG CÓ CẢNH BÁO ===
                /// Thông báo tích cực khi không có cảnh báo nào
                worksheet.Cell(startRow, 1).Value = "Không có cảnh báo";
                var noDataRange = worksheet.Range(startRow, 1, startRow, 3);
                noDataRange.Merge();
                noDataRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                startRow++;
            }

            // === THÊM HÀNG TRỐNG PHÂN CÁCH ===
            startRow += 2;
            return startRow;
        }
#endregion

        #region Model Classes

/// <summary>
/// Lớp đại diện cho sản phẩm bán chạy nhất trong hệ thống
/// Được sử dụng để hiển thị trong bảng "Top sản phẩm bán chạy" trên dashboard
/// Chứa thông tin về thuốc và hiệu suất bán hàng của chúng
/// </summary>
public class TopSellingProduct
        {
            /// <summary>
            /// ID của thuốc trong hệ thống
            /// Tương ứng với MedicineId trong bảng Medicine
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Tên thuốc hiển thị
            /// Được lấy từ trường Name trong bảng Medicine
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Tên danh mục thuốc
            /// Được lấy từ bảng MedicineCategory thông qua quan hệ CategoryId
            /// Ví dụ: "Thuốc giảm đau", "Kháng sinh", "Vitamin"
            /// </summary>
            public string Category { get; set; }

            /// <summary>
            /// Tổng doanh thu bán được của thuốc này trong khoảng thời gian thống kê
            /// Được tính từ: Quantity × SalePrice của tất cả InvoiceDetail có MedicineId tương ứng
            /// Đơn vị: VND
            /// </summary>
            public decimal Sales { get; set; }

            /// <summary>
            /// Phần trăm đóng góp vào tổng doanh thu thuốc
            /// Được tính theo công thức: (Sales của thuốc này / Tổng Sales của tất cả thuốc) × 100
            /// Ví dụ: 25 nghĩa là thuốc này chiếm 25% tổng doanh thu thuốc
            /// Sử dụng để hiển thị tầm quan trọng tương đối của từng sản phẩm
            /// </summary>
            public int Percentage { get; set; }
        }

        /// <summary>
        /// Lớp đại diện cho bệnh nhân VIP có chi tiêu cao nhất
        /// Được sử dụng để hiển thị trong bảng "Top bệnh nhân VIP" trên dashboard
        /// Hỗ trợ chiến lược chăm sóc khách hàng và phân khúc thị trường
        /// </summary>
        public class VIPPatient
        {
            /// <summary>
            /// ID của bệnh nhân trong hệ thống
            /// Tương ứng với PatientId trong bảng Patient
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Họ và tên đầy đủ của bệnh nhân
            /// Được lấy từ trường FullName trong bảng Patient
            /// </summary>
            public string FullName { get; set; }

            /// <summary>
            /// Số điện thoại liên hệ của bệnh nhân
            /// Được lấy từ trường Phone trong bảng Patient
            /// Sử dụng để liên hệ chăm sóc khách hàng VIP
            /// </summary>
            public string Phone { get; set; }

            /// <summary>
            /// Tổng số tiền bệnh nhân đã chi tiêu trong khoảng thời gian thống kê
            /// Được tính từ tổng TotalAmount của tất cả hóa đơn đã thanh toán (Status = "Đã thanh toán")
            /// Đơn vị: VND
            /// Sử dụng để xếp hạng và xác định bệnh nhân có giá trị cao
            /// </summary>
            public decimal TotalSpending { get; set; }

            /// <summary>
            /// Loại bệnh nhân hiện tại
            /// Được lấy từ bảng PatientType thông qua quan hệ PatientTypeId
            /// Ví dụ: "VIP", "Thường", "Bảo hiểm y tế"
            /// 
            /// Thuộc tính mới được thêm để hiển thị thông tin phân loại bệnh nhân
            /// Giúp nhân viên nhanh chóng nhận biết loại khách hàng khi chăm sóc
            /// </summary>
            public string PatientType { get; set; }

            /// <summary>
            /// Phần trăm đóng góp vào tổng doanh thu bệnh nhân
            /// Được tính theo công thức: (TotalSpending của bệnh nhân này / Tổng spending của tất cả bệnh nhân) × 100
            /// Ví dụ: 15 nghĩa là bệnh nhân này chiếm 15% tổng doanh thu từ bệnh nhân
            /// Sử dụng để đánh giá tầm quan trọng của từng bệnh nhân VIP
            /// </summary>
            public int Percentage { get; set; }
        }

        /// <summary>
        /// Lớp đại diện cho cảnh báo liên quan đến thuốc trong kho
        /// Được sử dụng để hiển thị trong bảng "Cảnh báo tồn kho" trên dashboard
        /// Hỗ trợ quản lý kho hiệu quả và tránh rủi ro về chất lượng thuốc
        /// </summary>
        public class WarningMedicine
        {
            /// <summary>
            /// ID của thuốc cần cảnh báo
            /// Tương ứng với MedicineId trong bảng Medicine
            /// </summary>
            public int Id { get; set; }

            /// <summary>
            /// Tên thuốc cần cảnh báo
            /// Được lấy từ trường Name trong bảng Medicine
            /// </summary>
            public string Name { get; set; }

            /// <summary>
            /// Thông điệp cảnh báo chi tiết về tình trạng thuốc
            /// Các loại cảnh báo được ưu tiên theo mức độ nghiêm trọng:
            /// 
            /// 1. "CẦN TIÊU HỦY" (Mức độ cao nhất - màu đỏ khi export Excel):
            ///    - Lô thuốc đã hết hạn nhưng chưa được tiêu hủy
            ///    - Ví dụ: "CẦN TIÊU HỦY: Lô thuốc đã hết hạn từ 15/01/2024 nhưng vẫn còn 50 viên"
            /// 
            /// 2. "LỖI CẤU HÌNH" (Mức độ cao):
            ///    - Lô thuốc được đánh dấu tiêu hủy nhưng vẫn là lô đang bán
            ///    - Ví dụ: "LỖI CẤU HÌNH: Lô thuốc được đánh dấu tiêu hủy nhưng vẫn là lô đang bán"
            /// 
            /// 3. "SẮP HẾT HẠN" (Mức độ trung bình - màu cam khi export Excel):
            ///    - Lô thuốc sẽ hết hạn trong vòng 30 ngày
            ///    - Ví dụ: "SẮP HẾT HẠN: Còn 15 ngày đến hạn sử dụng (30/01/2024)"
            /// 
            /// 4. "LÔ CUỐI CÙNG" (Mức độ thấp):
            ///    - Thuốc chỉ còn lại 1 lô duy nhất
            ///    - Ví dụ: "LÔ CUỐI CÙNG: Thuốc chỉ còn 1 lô với 100 viên"
            /// 
            /// 5. "TỒN KHO THẤP" (Mức độ thấp - màu xanh khi export Excel):
            ///    - Số lượng tồn kho ≤ 20 đơn vị (cần nhập thêm)
            ///    - Số lượng tồn kho ≤ 10 đơn vị (cần nhập gấp)
            ///    - Ví dụ: "TỒN KHO THẤP: Chỉ còn 15 viên - Nên nhập thêm"
            /// 
            /// Thông điệp này giúp:
            /// - Quản lý kho nhanh chóng nhận biết vấn đề
            /// - Ưu tiên xử lý theo mức độ nghiêm trọng
            /// - Thực hiện hành động phù hợp (tiêu hủy, nhập hàng, chuyển lô)
            /// </summary>
            public string WarningMessage { get; set; }
        }
        #endregion
    }
}
