using System.Collections.ObjectModel;
using Group_2.Models;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

using DbEvent = Group_2.Models.Event;  // <- alias tietokannan Event-mallille

namespace Group_2
{
    public partial class AdminPanelPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<EventItem> events { get; } = new();
        public ObservableCollection<User> users { get; } = new();

        private EventItem? selectedEvent;
        private User? selectedUser;
        private ObservableCollection<User> selectedUsers = new();

        public EventItem? SelectedEvent
        {
            get => selectedEvent;
            set
            {
                if (selectedEvent != value)
                {
                    selectedEvent = value;
                    OnPropertyChanged(nameof(SelectedEvent));
                }
            }
        }

        public User? SelectedUser
        {
            get => selectedUser;
            set
            {
                if (selectedUser != value)
                {
                    selectedUser = value;
                    OnPropertyChanged(nameof(SelectedUser));
                }
            }
        }

        public ObservableCollection<User> SelectedUsers
        {
            get => selectedUsers;
            set
            {
                if (selectedUsers != value)
                {
                    selectedUsers = value;
                    OnPropertyChanged(nameof(SelectedUsers));
                }
            }
        }

        public bool IsEventView => TypePicker?.SelectedIndex == 0;
        public bool IsUserView => TypePicker?.SelectedIndex == 1;

        private bool _suppressSelection;

        public AdminPanelPage()
        {
            InitializeComponent();
            BindingContext = this;
            TypePicker.SelectedIndex = 0;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                await DatabaseService.EnsureSchemaAsync();
                await LoadData();
            }
            catch (Exception ex)
            {
                await DisplayAlert("DB-yhteysvirhe", ex.Message, "OK");
            }
        }

        // ---------- MAPPING Event <-> EventItem ----------

        private EventItem MapFromDb(DbEvent e)
        {
            // DB: Title + Subtitle + Date
            // UI: Title + Description + Start/EndDate + Category
            return new EventItem
            {
                Id = e.Id,
                Title = e.Title ?? string.Empty,
                Description = e.Subtitle ?? string.Empty,
                StartDate = e.Date,
                EndDate = e.EndDate,
                Category = string.Empty,
                ParticipantIds = e.ParticipantIds != null
                    ? new List<Guid>(e.ParticipantIds)
                    : new List<Guid>(),
                ParticipantsDisplay = e.ParticipantsDisplay ?? "Ei osallistujia"
            };
        }

        private DbEvent MapToDb(EventItem e)
        {
            return new DbEvent
            {
                Id = e.Id,
                Title = e.Title ?? string.Empty,
                Subtitle = e.Description ?? string.Empty,
                Date = e.StartDate, // käytetään alkupäivää tallennukseen
                EndDate = e.EndDate,
                ParticipantIds = e.ParticipantIds != null
                    ? new List<Guid>(e.ParticipantIds)
                    : new List<Guid>(),
                ParticipantsDisplay = e.ParticipantsDisplay ?? "Ei osallistujia"
            };
        }

        // ---------- DATA LATAUS ----------

        async Task LoadData()
        {
            _suppressSelection = true;

            // → tämä palauttaa List<Event> (DbEvent)
            var dbEvents = await DatabaseService.LoadEventsAsync();
            var us = await DatabaseService.LoadUsersAsync();

            users.Clear();
            foreach (var u in us) users.Add(u);

            events.Clear();
            foreach (var db in dbEvents)
            {
                var e = MapFromDb(db);

                // rakenna ParticipantsDisplay uudestaan käyttäjien nimillä
                var names = e.ParticipantIds.Any()
                    ? string.Join(", ",
                        e.ParticipantIds
                            .Select(id => users.FirstOrDefault(u => u.Id == id)?.Name)
                            .Where(n => !string.IsNullOrWhiteSpace(n)))
                    : string.Empty;

                e.ParticipantsDisplay = string.IsNullOrWhiteSpace(names)
                    ? "Ei osallistujia"
                    : $"Osallistujat: {names}";

                events.Add(e);
            }

            SelectedEvent = null;
            EventsList.SelectedItem = null;

            _suppressSelection = false;
            UpdateEmptyLabel();
        }

        void OnEventSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelection) return;
            SelectedEvent = e.CurrentSelection.FirstOrDefault() as EventItem;
        }

        private async Task<(bool ok, DateTime start, DateTime end)> PickDateRangeAsync(
            DateTime? start = null,
            DateTime? end = null)
        {
            var dlg = new DateRangeDialog(start, end);
            await Navigation.PushModalAsync(dlg);
            return await dlg.WaitForResultAsync();
        }

        // ---------- TALLENNUS ----------

        async Task SaveAll()
        {
            var dbEvents = events.Select(MapToDb).ToList();
            await DatabaseService.SaveEventsAsync(dbEvents);
            await DatabaseService.SaveUsersAsync(users.ToList());
        }

        //async Task SaveAll()
        //{
        //    try
        //    {
        //        var dbEvents = events.Select(MapToDb).ToList();
        //        await DatabaseService.SaveEventsAsync(dbEvents);
        //        await DatabaseService.SaveUsersAsync(users.ToList());
        //    }
        //    catch (Exception ex)
        //    {
        //        await DisplayAlert("Tallennusvirhe", ex.ToString(), "OK");
        //    }
        //}


        // ---------- UI-APUMETODIT ----------

        void UpdateEmptyLabel()
        {
            var showingEvents = TypePicker.SelectedIndex == 0;
            var hasItems = showingEvents ? events.Any() : users.Any();
            EmptyLabel.IsVisible = !hasItems;
        }

        void OnTypeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(IsEventView));
            OnPropertyChanged(nameof(IsUserView));
            UpdateEmptyLabel();
            SearchBar.Text = string.Empty;
        }

        private async void OnOpenCalendarClicked(object sender, EventArgs e)
        {
            await Navigation.PushModalAsync(new CalendarPage());
        }

        // ---------- LISÄYS ----------

        async void OnAddClicked(object sender, EventArgs e)
        {
            if (IsEventView)
            {
                var title = await DisplayPromptAsync("Uusi tapahtuma", "Otsikko:");
                if (string.IsNullOrWhiteSpace(title))
                {
                    await DisplayAlert("Virhe", "Otsikko ei voi olla tyhjä.", "OK");
                    return;
                }

                var desc = await DisplayPromptAsync("Uusi tapahtuma", "Kuvaus:");
                var range = await PickDateRangeAsync();
                if (!range.ok) return;

                // 👇 NÄYTÄ VALITUT PÄIVÄMÄÄRÄT ENNEN LISÄYSTÄ
                var previewText =
                    $"Alkupäivä: {range.start:dd.MM.yyyy}\n" +
                    $"Loppupäivä: {range.end:dd.MM.yyyy}";

                var confirmDates = await DisplayAlert(
                    "Tarkista päivämäärät",
                    previewText,
                    "Hyväksy",
                    "Muuta");

                if (!confirmDates)
                {
                    // Halutessasi voit antaa mahdollisuuden valita uusiksi:
                    var range2 = await PickDateRangeAsync(range.start, range.end);
                    if (!range2.ok) return;
                    range = range2;
                }

                // Tästä eteenpäin sama kuin ennen
                var ev = new EventItem
                {
                    Id = Guid.NewGuid(),
                    Title = title.Trim(),
                    Description = desc?.Trim() ?? string.Empty,
                    StartDate = range.start.Date,
                    EndDate = range.end.Date,
                    Category = string.Empty,
                    ParticipantIds = new List<Guid>()
                };

                events.Add(ev);
                await SaveAll();
                await LoadData();


                // 👇 Ladataan tuoreet tiedot kannasta, jolloin lista päivittyy varmasti
                await LoadData();

                // Halutessasi voit yrittää valita juuri lisätyn takaisin:
                var restored = events.FirstOrDefault(x => x.Id == ev.Id);
                if (restored != null)
                {
                    SelectedEvent = restored;
                    EventsList.SelectedItem = restored;
                }

                if (!string.IsNullOrWhiteSpace(SearchBar?.Text))
                    SearchBar.Text = string.Empty;
            }

            else if (IsUserView)
            {
                var name = await DisplayPromptAsync("Uusi käyttäjä", "Nimi:");
                if (string.IsNullOrWhiteSpace(name))
                {
                    await DisplayAlert("Virhe", "Käyttäjänimi ei voi olla tyhjä.", "OK");
                    return;
                }

                var email = await DisplayPromptAsync("Uusi käyttäjä", "Sähköposti:") ?? string.Empty;
                var password = await DisplayPromptAsync("Uusi käyttäjä", "Salasana:") ?? string.Empty;
                if (string.IsNullOrWhiteSpace(password))
                {
                    await DisplayAlert("Virhe", "Salasana ei voi olla tyhjä.", "OK");
                    return;
                }

                var u = new User
                {
                    Name = name.Trim(),
                    Email = email.Trim(),
                    Password = password.Trim()
                };

                users.Add(u);
                await SaveAll();
                UpdateEmptyLabel();
            }
        }

        // ---------- POISTO ----------

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (IsEventView && SelectedEvent != null)
            {
                var confirm = await DisplayAlert("Poista",
                    $"Poistetaanko '{SelectedEvent.Title}'?", "Kyllä", "Ei");
                if (confirm)
                {
                    var id = SelectedEvent.Id;

                    try
                    {
                        // 1) Poisto tietokannasta
                        await DatabaseService.DeleteEventAsync(id);

                        // 2) Päivitä UI (helpoin: lataa uudelleen)
                        await LoadData();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Virhe",
                            "Tapahtuman poistaminen epäonnistui:\n" + ex.Message, "OK");
                    }
                }
            }
            else if (IsUserView && SelectedUser != null)
            {
                var confirm = await DisplayAlert("Poista",
                    $"Poistetaanko '{SelectedUser.Name}'?", "Kyllä", "Ei");
                if (confirm)
                {
                    var id = SelectedUser.Id;

                    try
                    {
                        await DatabaseService.DeleteUserAsync(id);
                        await LoadData();
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Virhe",
                            "Käyttäjän poistaminen epäonnistui:\n" + ex.Message, "OK");
                    }
                }
            }
        }



        // ---------- MUOKKAUS ----------

        async void OnEditClicked(object sender, EventArgs e)
        {
            if (IsEventView && SelectedEvent != null)
            {
                var eventId = SelectedEvent.Id;

                var newTitle = await DisplayPromptAsync("Muokkaa tapahtumaa", "Otsikko:", initialValue: SelectedEvent.Title);
                if (newTitle == null) return;
                if (string.IsNullOrWhiteSpace(newTitle))
                {
                    await DisplayAlert("Virhe", "Otsikko ei voi olla tyhjä.", "OK");
                    return;
                }

                var newDesc = await DisplayPromptAsync("Muokkaa tapahtumaa", "Kuvaus:", initialValue: SelectedEvent.Description);
                if (newDesc == null) return;

                var range = await PickDateRangeAsync(SelectedEvent.StartDate, SelectedEvent.EndDate);
                if (!range.ok) return;

                if (range.end < range.start)
                {
                    await DisplayAlert("Virhe", "Loppupäivä ei voi olla ennen alkupäivää.", "OK");
                    return;
                }

                SelectedEvent.Title = newTitle.Trim();
                SelectedEvent.Description = newDesc?.Trim() ?? string.Empty;
                SelectedEvent.StartDate = range.start.Date;
                SelectedEvent.EndDate = range.end.Date;

                await SaveAll();
                await LoadData();

                var restored = events.FirstOrDefault(ev => ev.Id == eventId);
                if (restored != null)
                {
                    SelectedEvent = restored;
                    EventsList.SelectedItem = restored;
                }
            }
            else if (IsUserView && SelectedUser != null)
            {
                var name = await DisplayPromptAsync("Muokkaa käyttäjää", "Nimi:", initialValue: SelectedUser.Name);
                if (name == null) return;
                if (string.IsNullOrWhiteSpace(name))
                {
                    await DisplayAlert("Virhe", "Käyttäjänimi ei voi olla tyhjä.", "OK");
                    return;
                }

                var email = await DisplayPromptAsync("Muokkaa käyttäjää", "Sähköposti:", initialValue: SelectedUser.Email);
                if (email == null) return;

                SelectedUser.Name = name.Trim();
                SelectedUser.Email = email.Trim();

                var idx = users.IndexOf(SelectedUser);
                if (idx >= 0)
                {
                    users[idx] = SelectedUser;
                    UsersList.SelectedItem = SelectedUser;
                }

                await SaveAll();
            }
        }

        // ---------- HAKU ----------

        void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = e.NewTextValue?.Trim().ToLower() ?? string.Empty;
            if (IsEventView)
            {
                EventsList.ItemsSource = string.IsNullOrWhiteSpace(text)
                    ? events
                    : new ObservableCollection<EventItem>(
                        events.Where(ev =>
                            (ev.Title ?? string.Empty).ToLower().Contains(text) ||
                            (ev.Description ?? string.Empty).ToLower().Contains(text)));
            }
            else if (IsUserView)
            {
                UsersList.ItemsSource = string.IsNullOrWhiteSpace(text)
                    ? users
                    : new ObservableCollection<User>(users.Where(x =>
                        (x.Name ?? string.Empty).ToLower().Contains(text) ||
                        (x.Email ?? string.Empty).ToLower().Contains(text)));
            }

            UpdateEmptyLabel();
        }

        // ---------- USER-SELECTION ----------

        void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = e.CurrentSelection.FirstOrDefault() as User;
            if (UsersList.SelectedItems is IList selectedItems)
            {
                SelectedUsers = new ObservableCollection<User>(selectedItems.Cast<User>());
            }
        }

        // ---------- OSALLISTUJAT: LISÄYS ----------

        private async void OnAddParticipantsClicked(object sender, EventArgs e)
        {
            if (SelectedEvent == null)
            {
                await DisplayAlert("Virhe", "Valitse tapahtuma.", "OK");
                return;
            }

            var eventId = SelectedEvent.Id;

            var existing = SelectedEvent.ParticipantIds ?? new List<Guid>();
            var available = users.Where(u => !existing.Contains(u.Id)).ToList();

            if (!available.Any())
            {
                await DisplayAlert("Virhe", "Kaikki käyttäjät on jo lisätty tähän tapahtumaan.", "OK");
                return;
            }

            var dlg = new AddParticipantsDialog(available);
            await Navigation.PushModalAsync(dlg);
            var picked = await dlg.WaitForResultAsync();
            if (picked == null || picked.Count == 0)
                return;

            var newList = SelectedEvent.ParticipantIds != null
                ? new List<Guid>(SelectedEvent.ParticipantIds)
                : new List<Guid>();

            foreach (var id in picked)
                if (!newList.Contains(id))
                    newList.Add(id);

            SelectedEvent.ParticipantIds = newList;

            var names = SelectedEvent.ParticipantIds
                .Select(id => users.FirstOrDefault(u => u.Id == id)?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n));

            SelectedEvent.ParticipantsDisplay = names.Any()
                ? $"Osallistujat: {string.Join(", ", names)}"
                : "Ei osallistujia";

            await SaveAll();
            await LoadData();

            var restored = events.FirstOrDefault(ev => ev.Id == eventId);
            if (restored != null)
            {
                SelectedEvent = restored;
                EventsList.SelectedItem = restored;
            }
        }

        // ---------- OSALLISTUJAT: MUOKKAUS ----------

        private async void OnEditParticipantsClicked(object sender, EventArgs e)
        {
            if (SelectedEvent == null)
            {
                await DisplayAlert("Virhe", "Valitse muokattava tapahtuma.", "OK");
                return;
            }

            var eventId = SelectedEvent.Id;

            var existing = SelectedEvent.ParticipantIds ?? new List<Guid>();
            var dlg = new EditParticipantsDialog(users, existing);
            await Navigation.PushModalAsync(dlg);
            var picked = await dlg.WaitForResultAsync();
            if (picked == null)
                return;

            var newList = picked.ToList();
            SelectedEvent.ParticipantIds = newList;

            var names = newList
                .Select(id => users.FirstOrDefault(u => u.Id == id)?.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n));

            SelectedEvent.ParticipantsDisplay = names.Any()
                ? $"Osallistujat: {string.Join(", ", names)}"
                : "Ei osallistujia";

            await SaveAll();
            await LoadData();

            var restored = events.FirstOrDefault(ev => ev.Id == eventId);
            if (restored != null)
            {
                SelectedEvent = restored;
                EventsList.SelectedItem = restored;
            }
        }

        // ---------- INotifyPropertyChanged ----------

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // ---------- Converter XAML:lle ----------

    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value != null;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
