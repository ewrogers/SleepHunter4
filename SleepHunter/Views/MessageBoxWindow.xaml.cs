using System.Windows;

namespace SleepHunter.Views
{
    internal partial class MessageBoxWindow : Window
    {
        public string MessageText
        {
            get { return (string)GetValue(MessageTextProperty); }
            set { SetValue(MessageTextProperty, value); }
        }

        public string SubText
        {
            get { return (string)GetValue(SubTextProperty); }
            set { SetValue(SubTextProperty, value); }
        }

        public string OkButtonText
        {
            get { return (string)GetValue(OkButtonTextProperty); }
            set { SetValue(OkButtonTextProperty, value); }
        }

        public string CancelButtonText
        {
            get { return (string)GetValue(CancelButtonTextProperty); }
            set { SetValue(CancelButtonTextProperty, value); }
        }

        public GridLength OkButtonColumnWidth
        {
            get { return (GridLength)GetValue(OkButtonColumnWidthProperty); }
            set { SetValue(OkButtonColumnWidthProperty, value); }
        }

        public GridLength CancelButtonColumnWidth
        {
            get { return (GridLength)GetValue(CancelButtonColumnWidthProperty); }
            set { SetValue(CancelButtonColumnWidthProperty, value); }
        }

        public Visibility OkButtonVisibility
        {
            get { return (Visibility)GetValue(OkButtonVisibilityProperty); }
            set { SetValue(OkButtonVisibilityProperty, value); }
        }

        public Visibility CancelButtonVisibility
        {
            get { return (Visibility)GetValue(CancelButtonVisibilityProperty); }
            set { SetValue(CancelButtonVisibilityProperty, value); }
        }

        public static readonly DependencyProperty MessageTextProperty =
            DependencyProperty.Register("MessageText", typeof(string), typeof(MessageBoxWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty SubTextProperty =
           DependencyProperty.Register("SubText", typeof(string), typeof(MessageBoxWindow), new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty OkButtonTextProperty =
            DependencyProperty.Register("OkButtonText", typeof(string), typeof(MessageBoxWindow), new PropertyMetadata("_OK"));

        public static readonly DependencyProperty CancelButtonTextProperty =
            DependencyProperty.Register("CancelButtonText", typeof(string), typeof(MessageBoxWindow), new PropertyMetadata("_Cancel"));

        public static readonly DependencyProperty CancelButtonColumnWidthProperty =
           DependencyProperty.Register("CancelButtonColumnWidth", typeof(GridLength), typeof(MessageBoxWindow), new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

        public static readonly DependencyProperty OkButtonColumnWidthProperty =
            DependencyProperty.Register("OkButtonColumnWidth", typeof(GridLength), typeof(MessageBoxWindow), new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register("CancelButtonVisibility", typeof(Visibility), typeof(MessageBoxWindow), new PropertyMetadata(Visibility.Visible));

        public static readonly DependencyProperty OkButtonVisibilityProperty =
            DependencyProperty.Register("OkButtonVisibility", typeof(Visibility), typeof(MessageBoxWindow), new PropertyMetadata(Visibility.Visible));


        public MessageBoxWindow()
        {
            InitializeComponent();
        }

        void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
