namespace Group_2
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                System.Diagnostics.Debug.WriteLine("UNHANDLED: " + e.ExceptionObject);

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("UNOBSERVED: " + e.Exception);
                e.SetObserved();
            };
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            try
            {
                return new Window(new AdminPanelPage());
            }
            catch (Exception ex)
            {
                // Näytä koko exception ruudulla
                return new Window(
                    new ContentPage
                    {
                        Content = new ScrollView
                        {
                            Content = new Label
                            {
                                Text = "Startuppoikkeus:\n\n" + ex.ToString(),
                                Padding = 20
                            }
                        }
                    });
            }
        }


    }
}