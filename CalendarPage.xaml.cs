using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Group_2.Models;
using Group_2.Services;   // ⬅ tärkeä, että DatabaseService löytyy

namespace Group_2
{
    public partial class CalendarPage : ContentPage
    {
        // Kalenterin käyttämä lista EventItem-olioita (UI-malli)
        private List<EventItem> all = new();
        private List<User> users = new();

        public CalendarPage()
        {
            InitializeComponent();
            DatePick.Date = DateTime.Today;
            _ = InitAsync();
        }

        private async Task InitAsync()
        {
            // Haetaan käyttäjät kannasta
            users = await DatabaseService.LoadUsersAsync();

            // Ladataan tämän päivän tapahtumat
            await LoadForDateAsync(DatePick.Date);
        }

        private async Task LoadForDateAsync(DateTime date)
        {
            // 1) Haetaan kannasta Event-lista (EI EventItem!)
            var eventsFromDb = await DatabaseService.LoadEventsAsync();

            // 2) Mäpätään Event → EventItem kalenteria varten
            all = eventsFromDb.Select(e =>
            {
                var item = new EventItem
                {
                    Id = e.Id,
                    Title = e.Title,
                    Description = e.Subtitle,   // Subtitle → Description
                    StartDate = e.Date,         // Ei erillistä loppuaikaa → sama päivämäärä
                    EndDate = e.EndDate,
                    Category = "",              // jos kategorioita ei käytetä, jätetään tyhjäksi
                    ParticipantIds = e.ParticipantIds != null
                        ? new List<Guid>(e.ParticipantIds)
                        : new List<Guid>()
                };

                // Rakennetaan osallistujien nimilista UI:ta varten
                var names = item.ParticipantIds
                    .Select(id => users.FirstOrDefault(u => u.Id == id)?.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n));

                item.ParticipantsDisplay = names.Any()
                    ? $"Osallistujat: {string.Join(", ", names)}"
                    : "Ei osallistujia";

                return item;
            }).ToList();

            // 3) Suodatetaan haku + päivämäärä
            ApplyFilter(SearchBar?.Text, date);
        }

        private void ApplyFilter(string? text, DateTime date)
        {
            var t = (text ?? string.Empty).Trim().ToLower();

            var filtered = all
                // päiväfiltteri
                .Where(e =>
                    e.StartDate.Date <= date.Date &&
                    e.EndDate.Date >= date.Date)
                // hakufiltteri otsikosta/kuvauksesta
                .Where(e =>
                    string.IsNullOrWhiteSpace(t) ||
                    (e.Title ?? string.Empty).ToLower().Contains(t) ||
                    (e.Description ?? string.Empty).ToLower().Contains(t))
                .OrderBy(e => e.StartDate)
                .ToList();

            DayEvents.ItemsSource = filtered;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(e.NewTextValue, DatePick.Date);
        }

        private async void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            await LoadForDateAsync(e.NewDate);
        }

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}
