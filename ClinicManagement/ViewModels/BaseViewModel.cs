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
    public class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool>? _canExecute;

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public RelayCommand(Action execute, Func<bool>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
            public void Execute(object? parameter) => _execute();
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Predicate<T>? _canExecute;

            public event EventHandler? CanExecuteChanged
            {
                add => CommandManager.RequerySuggested += value;
                remove => CommandManager.RequerySuggested -= value;
            }

            public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
            {
                _execute = execute ?? throw new ArgumentNullException(nameof(execute));
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                // Always call the delegate if provided, regardless of parameter
                if (_canExecute != null)
                {
                    // For null parameter with reference type T
                    if (parameter == null && typeof(T).IsClass)
                        return _canExecute((T)parameter!);

                    // For non-null parameter that can be cast to T
                    if (parameter is T typedParameter)
                        return _canExecute(typedParameter);

                    // Parameter can't be cast to T
                    return false;
                }

                // If no canExecute delegate provided, default to enabled
                return true;
            }

            public void Execute(object? parameter)
            {
                if (parameter == null && typeof(T).IsClass)
                {
                    _execute((T)parameter!);
                    return;
                }

                if (parameter is T typedParameter)
                    _execute(typedParameter);
                else
                    throw new InvalidCastException($"Cannot cast {parameter?.GetType().Name ?? "null"} to {typeof(T).Name}");
            }
        }
    }
}
