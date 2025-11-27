namespace Group_2
{
    public partial class DateRangeDialog : ContentPage
    {
        TaskCompletionSource<(bool ok, DateTime start, DateTime end)> _tcs = new();

        public DateRangeDialog(DateTime? initialStart = null, DateTime? initialEnd = null)
        {
            InitializeComponent();
            StartPick.Date = (initialStart ?? DateTime.Today).Date;
            EndPick.Date = (initialEnd ?? StartPick.Date).Date;
            EndPick.MinimumDate = StartPick.Date;
            StartPick.DateSelected += StartPick_DateSelected;
        }

        void StartPick_DateSelected(object? sender, DateChangedEventArgs e)
        {
            EndPick.MinimumDate = e.NewDate.Date;
            if (EndPick.Date < EndPick.MinimumDate)
                EndPick.Date = EndPick.MinimumDate;
        }

        public Task<(bool ok, DateTime start, DateTime end)> WaitForResultAsync() => _tcs.Task;

        async void OnOk(object sender, EventArgs e)
        {
            var start = StartPick.Date.Date;
            var end = EndPick.Date.Date;

            if (end < start)
            {
                await DisplayAlert("Virhe", "Loppupäivä ei voi olla ennen alkupäivää.", "OK");
                return;
            }

            _tcs.TrySetResult((true, start, end));
            await Navigation.PopModalAsync();
        }

        async void OnCancel(object sender, EventArgs e)
        {
            _tcs.TrySetResult((false, DateTime.MinValue, DateTime.MinValue));
            await Navigation.PopModalAsync();
        }
    }
}