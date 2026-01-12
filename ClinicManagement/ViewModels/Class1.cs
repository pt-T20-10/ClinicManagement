

//using ClinicManagement.Models;

//using ClinicManagement.Services;

///// <summary>
///// Xác thực định dạng lịch làm việc theo các mẫu được phép
///// Hỗ trợ các định dạng: "T2, T3, T4: 7h-13h", "T2-T7: 7h-11h", v.v.
///// </summary>
///// <param name="schedule">Chuỗi lịch làm việc cần kiểm tra</param>
///// <returns>True nếu hợp lệ, False nếu không hợp lệ</returns>
//private bool IsValidScheduleFormat(string schedule)
//{
//    if (string.IsNullOrWhiteSpace(schedule))
//        return true; // Lịch làm việc trống là hợp lệ (không bắt buộc)

//    // Các pattern regex để kiểm tra định dạng lịch làm việc
//    // Pattern 1: "T2, T3, T4: 7h-13h" (danh sách các ngày)
//    string pattern1 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

//    // Pattern 2: "T2-T7: 7h-11h" (khoảng ngày)
//    string pattern2 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?$";

//    // Pattern 3: "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h" (nhiều ca làm việc)
//    string pattern3 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";

//    // Pattern 4: Khoảng ngày với nhiều ca
//    string pattern4 = @"^T[2-7]-T[2-7]: \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?(, \d{1,2}h(\d{1,2})?-\d{1,2}h(\d{1,2})?)+$";

//    // Pattern 5: Định dạng có phút cụ thể
//    string pattern5 = @"^(T[2-7]|CN)(, (T[2-7]|CN))*: \d{1,2}h\d{2}-\d{1,2}h\d{2}(, \d{1,2}h\d{2}-\d{1,2}h\d{2})*$";

//    // Kiểm tra xem có pattern nào khớp không
//    if (Regex.IsMatch(schedule, pattern1) ||
//        Regex.IsMatch(schedule, pattern2) ||
//        Regex.IsMatch(schedule, pattern3) ||
//        Regex.IsMatch(schedule, pattern4) ||
//        Regex.IsMatch(schedule, pattern5))
//    {
//        try
//        {
//            // Phân tích và kiểm tra tính logic của các khoảng thời gian
//            string[] parts = schedule.Split(':');
//            if (parts.Length < 2)
//                return false;

//            // Lấy phần thời gian sau dấu ':')
//            string timeSection = string.Join(":", parts.Skip(1)).Trim();
//            var timeRanges = timeSection.Split(',');

//            // Kiểm tra từng khoảng thời gian
//            foreach (var range in timeRanges)
//            {
//                var times = range.Trim().Split('-');
//                if (times.Length == 2)
//                {
//                    var start = ParseTimeString(times[0].Trim());
//                    var end = ParseTimeString(times[1].Trim());

//                    // Kiểm tra định dạng thời gian hợp lệ
//                    if (start == TimeSpan.Zero && end == TimeSpan.Zero)
//                        return false; // Định dạng thời gian không hợp lệ

//                    // Kiểm tra thời gian bắt đầu phải nhỏ hơn thời gian kết thúc
//                    if (start >= end)
//                        return false;
//                }
//            }
//            return true;
//        }
//        catch
//        {
//            return false;
//        }
//    }

//    return false;
//}

///// <summary>
///// Phân tích chuỗi thời gian thành TimeSpan
///// Hỗ trợ các định dạng: "8h", "8h30", "13h", "13h30", v.v.
///// </summary>
///// <param name="timeStr">Chuỗi thời gian cần phân tích</param>
///// <returns>TimeSpan tương ứng hoặc TimeSpan.Zero nếu không hợp lệ</returns>
//private TimeSpan ParseTimeString(string timeStr)
//{
//    // Chuyển đổi định dạng "8h30" thành "8:30"
//    timeStr = timeStr.Replace("h", ":").Replace(" ", "");
//    if (timeStr.EndsWith(":")) timeStr += "00";

//    var parts = timeStr.Split(':');

//    // Xử lý định dạng "giờ:phút"
//    if (parts.Length == 2 && int.TryParse(parts[0], out int h) && int.TryParse(parts[1], out int m))
//        return new TimeSpan(h, m, 0);

//    // Xử lý định dạng chỉ có giờ
//    if (parts.Length == 1 && int.TryParse(parts[0], out h))
//        return new TimeSpan(h, 0, 0);

//    return TimeSpan.Zero;
//}
//private bool IsAppointmentTimeValid() //Method kiểm tra xem thời gian cuộc hẹn có hợp lệ hay không
//{
//    if (!AppointmentDate.HasValue || !SelectedAppointmentTime.HasValue)// Kiểm tra xem ngày và giờ đã được chọn chưa
//        return false;

//    // Nối giờ và ngày lại
//    DateTime appointmentDateTime = AppointmentDate.Value.Date
//        .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));

//    // Kiểm tra xem cuộc hẹn có ở trong quá khứ hay không
//    if (appointmentDateTime < DateTime.Now)
//        return false;

//    //Kiểm tra bệnh nhân đã có cuộc hẹn nào vào thời gian này chưa
//    if (SelectedPatient != null)
//    {
//        var patientAppointments = DataProvider.Instance.Context.Appointments
//            .Where(a =>
//                a.PatientId == SelectedPatient.PatientId &&
//                a.IsDeleted != true &&
//                a.Status != "Đã hủy" &&
//                a.AppointmentDate.Date == appointmentDateTime.Date)
//            .ToList();

//        //Kiểm tra xem có cuộc hẹn nào trùng giờ và phút không
//        if (patientAppointments.Any(a =>
//            a.AppointmentDate.Hour == appointmentDateTime.Hour &&
//            a.AppointmentDate.Minute == appointmentDateTime.Minute))
//            return false;

//        // Kiểm tra các cuộc hẹn trùng lặp trong vòng 30 phút. Vì các lịch hẹn của bệnh nhân phải cách nhau ít nhất 30 phút
//        var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

//        foreach (var existingAppointment in patientAppointments)
//        {
//            double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
//            double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

//            if (timeDifference < 30 && timeDifference > 0)
//                return false;
//        }
//    }

//    //Chỉ kiểm tra lịch làm việc của bác sĩ nếu đã chọn bác sĩ
//    if (SelectedDoctor != null && !string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
//    {
//        //Lấy ngày trong tuần của cuộc hẹn
//        DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
//        string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);

//        // Parse working hours
//        var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);

//        // Check if doctor works on this day
//        if (!workingDays.Contains(dayCode))
//            return false;

//        //Kiểm tra xem thời gian cuộc hẹn có nằm trong giờ làm việc của bác sĩ hay không
//        TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
//        if (appointmentTime < startTime || appointmentTime > endTime)
//            return false;

//        // Lấy tất cả cuộc hẹn của bác sĩ trong ngày
//        var doctorAppointments = DataProvider.Instance.Context.Appointments
//            .Where(a =>
//                a.StaffId == SelectedDoctor.StaffId &&
//                a.IsDeleted != true &&
//                a.Status != "Đã hủy" &&
//                a.AppointmentDate.Date == appointmentDateTime.Date)
//            .ToList();

//        //Kiểm tra xem có cuộc hẹn nào trùng giờ và phút không
//        if (doctorAppointments.Any(a =>
//            a.AppointmentDate.Hour == appointmentDateTime.Hour &&
//            a.AppointmentDate.Minute == appointmentDateTime.Minute))
//            return false;

//        // Kiểm tra các cuộc hẹn trùng lặp trong vòng 30 phút
//        var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
//        foreach (var existingAppointment in doctorAppointments)
//        {
//            double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
//            double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes); // Tính khoảng cách thời gian giữa các cuộc hẹn, lấy giá trị tuyệt đối

//            if (timeDifference < 30 && timeDifference % 30 != 0) // Kiểm tra nếu khoảng cách thời gian nhỏ hơn 30 phút và không phải là bội số của 30
//                return false;
//        }
//    }

//    return true;
//}
//private string ConvertDayOfWeekToVietnameseCode(DayOfWeek dayOfWeek)//Method chuyển đổi ngày trong tuần sang mã tiếng Việt
//{
//    return dayOfWeek switch
//    {
//        DayOfWeek.Monday => "T2",
//        DayOfWeek.Tuesday => "T3",
//        DayOfWeek.Wednesday => "T4",
//        DayOfWeek.Thursday => "T5",
//        DayOfWeek.Friday => "T6",
//        DayOfWeek.Saturday => "T7",
//        DayOfWeek.Sunday => "CN",
//        _ => string.Empty
//    };
//}
///// <summary>
///// Phân tích chuỗi lịch làm việc của bác sĩ
///// Hỗ trợ các định dạng: "T2-T6: 8h-17h", "T2, T3, T4: 7h-13h", "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
///// </summary>
///// <param name="schedule">Chuỗi lịch làm việc</param>
///// <returns>Tuple chứa danh sách ngày làm việc, giờ bắt đầu và giờ kết thúc</returns>
//private (List<string> WorkingDays, TimeSpan StartTime, TimeSpan EndTime) ParseWorkingSchedule(string schedule)
//{
//    List<string> workingDays = new List<string>();
//    TimeSpan startTime = TimeSpan.Zero;
//    TimeSpan endTime = TimeSpan.Zero;
//    try
//    {
//        // Ví dụ định dạng: "T2-T6: 8h-17h" hoặc "T2, T3, T4: 7h-13h" hoặc "T2, T3, T4, T5,T6: 8h-12h, 13h30-17h"
//        string[] parts = schedule.Split(':');
//        if (parts.Length < 2)
//            return (workingDays, startTime, endTime);

//        // Phân tích ngày
//        string daysSection = parts[0].Trim();
//        if (daysSection.Contains('-'))
//        {
//            // Định dạng khoảng: "T2-T6"
//            string[] dayRange = daysSection.Split('-');
//            if (dayRange.Length == 2)
//            {
//                string startDay = dayRange[0].Trim();
//                string endDay = dayRange[1].Trim();
//                // Chuyển đổi thành số thứ tự ngày
//                int startDayNum = ConvertVietNameseCodeToDayNumber(startDay);
//                int endDayNum = ConvertVietNameseCodeToDayNumber(endDay);
//                for (int i = startDayNum; i <= endDayNum; i++)
//                {
//                    workingDays.Add(ConvertDayNumberToVietnameseCode(i));
//                }
//            }
//        }
//        else if (daysSection.Contains(','))
//        {
//            // Định dạng danh sách: "T2, T3, T4"
//            string[] daysList = daysSection.Split(',');
//            foreach (string day in daysList)
//            {
//                workingDays.Add(day.Trim());
//            }
//        }
//        else
//        {
//            // Định dạng một ngày: "T2"
//            workingDays.Add(daysSection);
//        }

//        // Phân tích phần thời gian - nối tất cả phần sau dấu ':' đầu tiên
//        string timeSection = string.Join(":", parts.Skip(1)).Trim();

//        // Xử lý nhiều khoảng thời gian (ví dụ: "8h-12h, 13h30-17h")
//        // Hiện tại, ta sẽ lấy thời gian đầu và cuối để có được khoảng thời gian làm việc tổng thể
//        var timeRanges = timeSection.Split(',');
//        if (timeRanges.Length > 0)
//        {
//            // Lấy khoảng thời gian đầu tiên cho thời gian bắt đầu
//            var firstRange = timeRanges[0].Trim();
//            var firstRangeParts = firstRange.Split('-');
//            if (firstRangeParts.Length >= 2)
//            {
//                startTime = ParseTimeString(firstRangeParts[0].Trim());
//            }

//            // Lấy khoảng thời gian cuối cùng cho thời gian kết thúc
//            var lastRange = timeRanges[timeRanges.Length - 1].Trim();
//            var lastRangeParts = lastRange.Split('-');
//            if (lastRangeParts.Length >= 2)
//            {
//                endTime = ParseTimeString(lastRangeParts[lastRangeParts.Length - 1].Trim());
//            }
//        }
//    }
//    catch (Exception ex)
//    {

//        // Trong trường hợp lỗi phân tích, trả về kết quả rỗng
//        workingDays.Clear();
//    }
//    return (workingDays, startTime, endTime);
//}
///// <summary>
///// Phân tích chuỗi thời gian thành TimeSpan
///// Hỗ trợ các định dạng: "8h", "8h30", "13h", "13h30", "8:30", "13:30"
///// </summary>
///// <param name="timeStr">Chuỗi thời gian</param>
///// <returns>TimeSpan tương ứng hoặc TimeSpan.Zero nếu không hợp lệ</returns>
//private TimeSpan ParseTimeString(string timeStr)
//{
//    // Loại bỏ hậu tố 'h' nếu có
//    timeStr = timeStr.Replace("h", "").Trim();

//    // Thử phân tích định dạng giờ:phút trước (ví dụ: "8:30", "13:30")
//    timeStr = timeStr.Replace('.', ':'); // Thay dấu chấm bằng dấu hai châm để nhất quán
//    if (timeStr.Contains(':'))
//    {
//        string[] parts = timeStr.Split(':');
//        if (parts.Length == 2 && int.TryParse(parts[0], out int hrs) && int.TryParse(parts[1], out int mins))
//        {
//            if (hrs >= 0 && hrs <= 23 && mins >= 0 && mins <= 59)
//                return new TimeSpan(hrs, mins, 0);
//        }
//    }

//    // Thử phân tích chỉ có giờ (ví dụ: "8" -> 08:00, "17" -> 17:00)
//    if (int.TryParse(timeStr, out int hours))
//    {
//        if (hours >= 0 && hours <= 23)
//            return new TimeSpan(hours, 0, 0);
//    }

//    // Thử phân tích định dạng thời gian chuẩn (ví dụ: "08:00:00")
//    if (TimeSpan.TryParse(timeStr + ":00", out TimeSpan result))
//        return result;

//    return TimeSpan.Zero; // Mặc định nếu phân tích thất bại
//}
//private int ConvertVietNameseCodeToDayNumber(string code) //Method chuyển đổi mã ngày tiếng Việt sang số thứ tự ngày
//{
//    return code switch
//    {
//        "T2" => 2,
//        "T3" => 3,
//        "T4" => 4,
//        "T5" => 5,
//        "T6" => 6,
//        "T7" => 7,
//        "CN" => 8,
//        _ => 0
//    };
//}
///// <summary>
///// Chuyển đổi số thứ tự ngày thành mã ngày tiếng Việt
///// </summary>
///// <param name="dayNumber">Số thứ tự ngày</param>
///// <returns>Mã ngày tiếng Việt</returns>
//private string ConvertDayNumberToVietnameseCode(int dayNumber)
//{
//    return dayNumber switch
//    {
//        2 => "T2",
//        3 => "T3",
//        4 => "T4",
//        5 => "T5",
//        6 => "T6",
//        7 => "T7",
//        8 => "CN",
//        _ => string.Empty
//    };
//}

//private string GetVietnameseDayName(DayOfWeek dayOfWeek)//Method lấy tên ngày trong tuần bằng tiếng Việt
//{
//    return dayOfWeek switch
//    {
//        DayOfWeek.Monday => "Thứ hai",
//        DayOfWeek.Tuesday => "Thứ ba",
//        DayOfWeek.Wednesday => "Thứ tư",
//        DayOfWeek.Thursday => "Thứ năm",
//        DayOfWeek.Friday => "Thứ sáu",
//        DayOfWeek.Saturday => "Thứ bảy",
//        DayOfWeek.Sunday => "Chủ nhật",
//        _ => string.Empty
//    };
//}

//private bool ValidateDateTimeSelection()//ethod kiểm tra xem đã chọn ngày và giờ hẹn hợp lệ hay chưa
//{
//    //Kiểm tra xem đã chọn ngày hẹn hay chưa
//    if (!AppointmentDate.HasValue)
//    {
//        MessageBoxService.ShowWarning(
//            "Vui lòng chọn ngày hẹn.",
//            "Lỗi - Ngày hẹn");

//        return false;
//    }
//    if (AppointmentDate.Value.Date < DateTime.Today)
//    {
//        MessageBoxService.ShowWarning(
//            "Ngày hẹn không hợp lệ. Vui lòng chọn ngày hiện tại hoặc trong tương lai.",
//            "Lỗi - Ngày hẹn");

//        return false;
//    }
//    if (!SelectedAppointmentTime.HasValue)
//    {
//        MessageBoxService.ShowError(
//            "Vui lòng chọn giờ hẹn.",
//            "Lỗi - Giờ hẹn");

//        return false;
//    }


//    DateTime appointmentDateTime = AppointmentDate.Value.Date
//        .Add(new TimeSpan(SelectedAppointmentTime.Value.Hour, SelectedAppointmentTime.Value.Minute, 0));


//    if (appointmentDateTime < DateTime.Now)
//    {
//        MessageBoxService.ShowError(
//            "Thời gian hẹn đã qua. Vui lòng chọn thời gian trong tương lai.",
//            "Lỗi - Thời gian hẹn");

//        return false;
//    }

//    // Tạo khoảng thời gian hợp lệ có thể lựa chọn
//    TimeSpan minTime = new TimeSpan(7, 0, 0);  // 07:00
//    TimeSpan maxTime = new TimeSpan(17, 0, 0); // 17:00
//    TimeSpan selectedTime = appointmentDateTime.TimeOfDay;
//    if (selectedTime < minTime || selectedTime > maxTime)
//    {
//        MessageBoxService.ShowWarning(
//            "Giờ hẹn chỉ được phép trong khoảng từ 07:00 đến 17:00.",
//            "Lỗi - Giờ hẹn");
//        return false;
//    }

//    //Kiểm tra xem bệnh nhân đã có cuộc hẹn nào vào thời gian này chưa
//    if (SelectedPatient != null)
//    {
//        var patientAppointments = DataProvider.Instance.Context.Appointments
//            .Where(a =>
//                a.PatientId == SelectedPatient.PatientId &&
//                a.IsDeleted != true &&
//                a.Status != "Đã hủy" &&
//                a.AppointmentDate.Date == appointmentDateTime.Date)
//            .ToList();


//        bool hasExactSameTime = patientAppointments.Any(a =>
//            a.AppointmentDate.Hour == appointmentDateTime.Hour &&
//            a.AppointmentDate.Minute == appointmentDateTime.Minute);

//        if (hasExactSameTime)
//        {
//            MessageBoxService.ShowError(
//                $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn vào lúc " +
//                $"{appointmentDateTime.ToString("HH:mm")}.\n" +
//                $"Vui lòng chọn thời gian khác.",
//                "Lỗi - Trùng lịch");

//            return false;
//        }


//        var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;

//        foreach (var existingAppointment in patientAppointments)
//        {
//            double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
//            double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

//            if (timeDifference < 30 && timeDifference > 0)
//            {
//                MessageBoxService.ShowError(
//                    $"Bệnh nhân {SelectedPatient.FullName} đã có lịch hẹn khác vào lúc " +
//                    $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
//                    $"Vui lòng chọn thời gian cách ít nhất 30 phút.",
//                    "Lỗi - Trùng lịch");

//                return false;
//            }
//        }
//    }


//    if (SelectedDoctor != null)
//    {

//        if (!string.IsNullOrWhiteSpace(SelectedDoctor.Schedule))
//        {

//            DayOfWeek dayOfWeek = appointmentDateTime.DayOfWeek;
//            string dayCode = ConvertDayOfWeekToVietnameseCode(dayOfWeek);


//            var (workingDays, startTime, endTime) = ParseWorkingSchedule(SelectedDoctor.Schedule);


//            if (!workingDays.Contains(dayCode))
//            {
//                MessageBoxService.ShowError(
//                    $"Bác sĩ {SelectedDoctor.FullName} không làm việc vào ngày {AppointmentDate.Value:dd/MM/yyyy} ({GetVietnameseDayName(dayOfWeek)}).",
//                    "Lỗi - Lịch làm việc");

//                return false;
//            }


//            TimeSpan appointmentTime = new TimeSpan(appointmentDateTime.Hour, appointmentDateTime.Minute, 0);
//            if (appointmentTime < startTime || appointmentTime > endTime)
//            {
//                MessageBoxService.ShowError(
//                    $"Giờ hẹn không nằm trong thời gian làm việc của bác sĩ {SelectedDoctor.FullName}.\n" +
//                    $"Thời gian làm việc: {startTime.ToString("hh\\:mm")} - {endTime.ToString("hh\\:mm")}.",
//                    "Lỗi - Giờ làm việc");

//                return false;
//            }
//        }


//        var doctorAppointments = DataProvider.Instance.Context.Appointments
//            .Where(a =>
//                a.StaffId == SelectedDoctor.StaffId &&
//                a.IsDeleted != true &&
//                a.Status != "Đã hủy" &&
//                a.AppointmentDate.Date == appointmentDateTime.Date)
//            .ToList();


//        if (doctorAppointments.Any(a =>
//            a.AppointmentDate.Hour == appointmentDateTime.Hour &&
//            a.AppointmentDate.Minute == appointmentDateTime.Minute))
//        {
//            MessageBoxService.ShowError(
//                $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
//                $"{appointmentDateTime.ToString("HH:mm")}.\n" +
//                $"Vui lòng chọn thời gian khác.",
//                "Lỗi - Trùng lịch");

//            return false;
//        }


//        var appointmentTimeMinutes = appointmentDateTime.TimeOfDay.TotalMinutes;
//        foreach (var existingAppointment in doctorAppointments)
//        {
//            double existingTimeMinutes = existingAppointment.AppointmentDate.TimeOfDay.TotalMinutes;
//            double timeDifference = Math.Abs(existingTimeMinutes - appointmentTimeMinutes);

//            if (timeDifference < 30 && timeDifference % 30 != 0)
//            {
//                MessageBoxService.ShowError(
//                    $"Bác sĩ {SelectedDoctor.FullName} đã có lịch hẹn vào lúc " +
//                    $"{existingAppointment.AppointmentDate.ToString("HH:mm")}.\n" +
//                    $"Vui lòng chọn thời gian cách ít nhất 30 phút hoặc đúng khung giờ 30 phút.",
//                    "Lỗi - Trùng lịch");

//                return false;
//            }
//        }
//    }

//    return true;
//}