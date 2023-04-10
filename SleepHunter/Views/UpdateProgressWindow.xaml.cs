using System.Windows;
using SleepHunter.Services;

namespace SleepHunter.Views
{
    public partial class UpdateProgressWindow : Window
    {
        private readonly IReleaseService releaseService = new ReleaseService();

        public bool ShouldInstall { get; private set; }

        public UpdateProgressWindow()
        {
            InitializeComponent();
        }

        void installButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = true;
            Close();
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            ShouldInstall = false;
            Close();
        }
    }
}
