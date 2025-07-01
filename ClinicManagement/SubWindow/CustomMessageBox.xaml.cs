using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClinicManagement.SubWindow
{
    public partial class CustomMessageBox : Window
    {
        public static readonly DependencyProperty MessageTypeProperty =
            DependencyProperty.Register("MessageType", typeof(MessageType), typeof(CustomMessageBox),
                new PropertyMetadata(MessageType.Information));

        public MessageType MessageType
        {
            get { return (MessageType)GetValue(MessageTypeProperty); }
            set { SetValue(MessageTypeProperty, value); }
        }

        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register("MessageText", typeof(string), typeof(CustomMessageBox),
                new PropertyMetadata(string.Empty));

        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register("Caption", typeof(string), typeof(CustomMessageBox),
                new PropertyMetadata(string.Empty));

        public string Caption
        {
            get { return (string)GetValue(CaptionProperty); }
            set { SetValue(CaptionProperty, value); }
        }

        public static readonly DependencyProperty ShowCancelButtonProperty =
            DependencyProperty.Register("ShowCancelButton", typeof(bool), typeof(CustomMessageBox),
                new PropertyMetadata(false));

        public bool ShowCancelButton
        {
            get { return (bool)GetValue(ShowCancelButtonProperty); }
            set { SetValue(ShowCancelButtonProperty, value); }
        }

        public CustomMessageBox()
        {
            InitializeComponent();
            DataContext = this;

            // Setup for animation
            this.Opacity = 0;
            this.RenderTransform = new TranslateTransform();

            // Enable window dragging when clicking anywhere on the title bar
            this.MouseDown += (s, e) =>
            {
                if (e.ChangedButton == MouseButton.Left && e.OriginalSource is FrameworkElement element &&
                    !(element is Button) && element.Name != "CancelButton")
                {
                    this.DragMove();
                }
            };

            // Play the animation when loaded
            this.Loaded += (s, e) =>
            {
                var storyboard = (Storyboard)FindResource("FadeInStoryboard");
                storyboard.Begin(this);
            };
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Escape key triggers cancel/close
                DialogResult = false;
                Close();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // Enter key triggers OK button
                DialogResult = true;
                Close();
                e.Handled = true;
            }
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Shows a custom message box with the specified message, caption, and message type.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="caption">The caption for the message box.</param>
        /// <param name="messageType">The type of the message (Information, Warning, Error, etc.)</param>
        /// <param name="showCancelButton">Whether to show a Cancel button or not.</param>
        /// <returns>true if the OK button was clicked; false if Cancel or Close was clicked.</returns>
        public static bool ShowDialog(string message, string caption = "", MessageType messageType = MessageType.Information, bool showCancelButton = false)
        {
            var msgBox = new CustomMessageBox
            {
                MessageText = message,
                Caption = caption,
                MessageType = messageType,
                ShowCancelButton = messageType == MessageType.Question || showCancelButton
            };

            return msgBox.ShowDialog() ?? false;
        }
    }

    public enum MessageType
    {
        Information,
        Warning,
        Error,
        Success,
        Question
    }
}
