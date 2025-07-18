using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClinicManagement.ViewModels
{
    /// <summary>
    /// Lớp ViewModel cơ sở triển khai INotifyPropertyChanged
    /// Cung cấp các chức năng cơ bản cho tất cả ViewModel trong ứng dụng theo mô hình MVVM
    /// </summary>
    public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Sự kiện được kích hoạt khi một thuộc tính thay đổi giá trị
        /// Cho phép giao diện người dùng tự động cập nhật khi dữ liệu thay đổi
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Phương thức bảo vệ để kích hoạt sự kiện PropertyChanged
        /// Được gọi khi một thuộc tính thay đổi để thông báo cho UI cập nhật
        /// </summary>
        /// <param name="propertyName">Tên thuộc tính đã thay đổi. Tự động được điền bởi CallerMemberName</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Phương thức tiện ích để thiết lập giá trị thuộc tính và kích hoạt PropertyChanged nếu có thay đổi
        /// Giúp giảm thiểu code lặp lại trong các setter của thuộc tính
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của thuộc tính</typeparam>
        /// <param name="field">Tham chiếu đến trường backing field</param>
        /// <param name="value">Giá trị mới cần thiết lập</param>
        /// <param name="propertyName">Tên thuộc tính. Tự động được điền bởi CallerMemberName</param>
        /// <returns>True nếu giá trị đã thay đổi, False nếu giá trị giống như cũ</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            // So sánh giá trị mới với giá trị hiện tại
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            
            // Cập nhật giá trị và thông báo thay đổi
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Lớp RelayCommand không có tham số - triển khai ICommand cho MVVM
        /// Cho phép liên kết các phương thức với các điều khiển UI như Button, MenuItem
        /// </summary>
        public class RelayCommand : ICommand
        {
            // Delegate chứa hành động sẽ được thực thi khi command được gọi
            private readonly Action _execute;
            
            // Delegate kiểm tra xem command có thể được thực thi hay không (tùy chọn)
            private readonly Func<bool>? _canExecute;

            /// <summary>
            /// Sự kiện được kích hoạt khi trạng thái CanExecute có thể đã thay đổi
            /// Liên kết với CommandManager để tự động cập nhật trạng thái UI
            /// </summary>
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            /// <summary>
            /// Khởi tạo RelayCommand với hành động thực thi và điều kiện kiểm tra (tùy chọn)
            /// </summary>
            /// <param name="execute">Hành động sẽ được thực thi - bắt buộc</param>
            /// <param name="canExecute">Điều kiện kiểm tra xem có thể thực thi hay không - tùy chọn</param>
            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            /// <summary>
            /// Kiểm tra xem command có thể được thực thi hay không
            /// </summary>
            /// <param name="parameter">Tham số truyền vào (không sử dụng trong phiên bản này)</param>
            /// <returns>True nếu có thể thực thi, False nếu không thể</returns>
            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
            
            /// <summary>
            /// Thực thi hành động của command
            /// </summary>
            /// <param name="parameter">Tham số truyền vào (không sử dụng trong phiên bản này)</param>
            public void Execute(object? parameter) => _execute();
        }

        /// <summary>
        /// Lớp RelayCommand có tham số kiểu T - triển khai ICommand cho MVVM
        /// Cho phép truyền tham số từ UI đến ViewModel khi thực thi command
        /// </summary>
        /// <typeparam name="T">Kiểu dữ liệu của tham số</typeparam>
        public class RelayCommand<T> : ICommand
        {
            // Delegate chứa hành động sẽ được thực thi với tham số kiểu T
            private readonly Action<T> _execute;
            
            // Delegate kiểm tra xem command có thể được thực thi với tham số kiểu T hay không (tùy chọn)
            private readonly Predicate<T>? _canExecute;

            /// <summary>
            /// Sự kiện được kích hoạt khi trạng thái CanExecute có thể đã thay đổi
            /// Liên kết với CommandManager để tự động cập nhật trạng thái UI
            /// </summary>
            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            /// <summary>
            /// Khởi tạo RelayCommand với hành động thực thi và điều kiện kiểm tra (tùy chọn)
            /// </summary>
            /// <param name="execute">Hành động sẽ được thực thi với tham số kiểu T - bắt buộc</param>
            /// <param name="canExecute">Điều kiện kiểm tra với tham số kiểu T - tùy chọn</param>
            public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            /// <summary>
            /// Kiểm tra xem command có thể được thực thi với tham số đã cho hay không
            /// Xử lý việc chuyển đổi kiểu và kiểm tra null safety
            /// </summary>
            /// <param name="parameter">Tham số truyền từ UI</param>
            /// <returns>True nếu có thể thực thi, False nếu không thể</returns>
            public bool CanExecute(object? parameter)
            {
                // Luôn gọi delegate kiểm tra nếu được cung cấp, bất kể tham số là gì
                if (_canExecute != null)
                {
                    // Xử lý trường hợp tham số null với kiểu tham chiếu T
                    if (parameter == null && typeof(T).IsClass)
                        return _canExecute((T)parameter!);

                    // Xử lý trường hợp tham số không null và có thể chuyển đổi sang kiểu T
                    if (parameter is T typedParameter)
                        return _canExecute(typedParameter);

                    // Tham số không thể chuyển đổi sang kiểu T
                    return false;
                }

                // Nếu không có delegate kiểm tra, mặc định cho phép thực thi
                return true;
            }

            /// <summary>
            /// Thực thi hành động của command với tham số đã cho
            /// Xử lý việc chuyển đổi kiểu và kiểm tra tính hợp lệ của tham số
            /// </summary>
            /// <param name="parameter">Tham số truyền từ UI</param>
            /// <exception cref="InvalidCastException">Ném ra khi không thể chuyển đổi tham số sang kiểu T</exception>
            public void Execute(object? parameter)
            {
                // Xử lý trường hợp tham số null với kiểu tham chiếu T
                if (parameter == null && typeof(T).IsClass)
                {
                    _execute((T)parameter!);
                    return;
                }

                // Xử lý trường hợp tham số có thể chuyển đổi sang kiểu T
                if (parameter is T typedParameter)
                    _execute(typedParameter);
                else
                    // Ném ngoại lệ nếu không thể chuyển đổi kiểu
                    throw new InvalidCastException($"Không thể chuyển đổi {parameter?.GetType().Name ?? "null"} sang {typeof(T).Name}");
            }
        }
    }
}
