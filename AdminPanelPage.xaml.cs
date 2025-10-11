using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Xaml;
using Group_2.Models;
using Group_2.Services;
using System.Collections; // tarvitaan SelectedItems tyhjent‰miseen

namespace Group_2
{
    public partial class AdminPanelPage : ContentPage
    {
        ObservableCollection<Event> events = new();
        ObservableCollection<User> users = new();
        Event? selectedEvent;
        User? selectedUser;

        public AdminPanelPage()
        {
            InitializeComponent();

            // Varmista, ett‰ n‰m‰ nimet vastaavat XAML:‰‰
            EventsList.ItemsSource = events;
            UsersList.ItemsSource = users;
            TypePicker.SelectedIndex = 0;
            _ = LoadData();
        }

        async Task LoadData()
        {
            var evs = await DatabaseService.LoadEventsAsync();
            events.Clear();
            foreach (var e in evs) events.Add(e);

            var us = await DatabaseService.LoadUsersAsync();
            users.Clear();
            foreach (var u in us) users.Add(u);

            UpdateEmptyLabel();
        }

        async Task SaveAll()
        {
            await DatabaseService.SaveEventsAsync(events.ToList());
            await DatabaseService.SaveUsersAsync(users.ToList());
        }

        void UpdateEmptyLabel()
        {
            var showingEvents = EventsList.IsVisible;
            var hasItems = showingEvents ? events.Any() : users.Any();
            EmptyLabel.IsVisible = !hasItems;
        }

        void OnTypeChanged(object sender, EventArgs e)
        {
            var selected = TypePicker.SelectedIndex;
            EventsList.IsVisible = selected == 0;
            UsersList.IsVisible = selected == 1;
            UpdateEmptyLabel();
            SearchBar.Text = string.Empty;
        }

        async void OnAddClicked(object sender, EventArgs e)
        {
            if (EventsList.IsVisible)
            {
                var title = await DisplayPromptAsync("Uusi tapahtuma", "Otsikko:");
                if (string.IsNullOrWhiteSpace(title)) return;
                var subtitle = await DisplayPromptAsync("Uusi tapahtuma", "Kuvaus:");
                var ev = new Event { Title = title.Trim(), Subtitle = subtitle?.Trim() ?? string.Empty };
                events.Add(ev);
            }
            else
            {
                var name = await DisplayPromptAsync("Uusi k‰ytt‰j‰", "Nimi:");
                if (string.IsNullOrWhiteSpace(name)) return;
                var email = await DisplayPromptAsync("Uusi k‰ytt‰j‰", "S‰hkˆposti:");
                var u = new User { Name = name.Trim(), Email = email?.Trim() ?? string.Empty };
                users.Add(u);
            }

            UpdateEmptyLabel();
            await SaveAll();
        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (EventsList.IsVisible && selectedEvent != null)
            {
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{selectedEvent.Title}'?", "Kyll‰", "Ei");
                if (confirm)
                {
                    events.Remove(selectedEvent);
                    selectedEvent = null;
                    UpdateEmptyLabel();
                    await SaveAll();
                }
            }
            else if (UsersList.IsVisible && selectedUser != null)
            {
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{selectedUser.Name}'?", "Kyll‰", "Ei");
                if (confirm)
                {
                    users.Remove(selectedUser);
                    selectedUser = null;
                    UpdateEmptyLabel();
                    await SaveAll();
                }
            }

            EventsList.SelectedItem = null;
            if (UsersList.SelectedItems is IList selectedItems)
                selectedItems.Clear();
        }

        async void OnEditClicked(object sender, EventArgs e)
        {
            if (EventsList.IsVisible && selectedEvent != null)
            {
                var title = await DisplayPromptAsync("Muokkaa tapahtumaa", "Otsikko:", initialValue: selectedEvent.Title);
                if (title == null) return;
                var subtitle = await DisplayPromptAsync("Muokkaa tapahtumaa", "Kuvaus:", initialValue: selectedEvent.Subtitle);
                selectedEvent.Title = title.Trim();
                selectedEvent.Subtitle = subtitle?.Trim() ?? string.Empty;
                var idx = events.IndexOf(selectedEvent);
                if (idx >= 0) events[idx] = selectedEvent;
                await SaveAll();
            }
            else if (UsersList.IsVisible && selectedUser != null)
            {
                var name = await DisplayPromptAsync("Muokkaa k‰ytt‰j‰‰", "Nimi:", initialValue: selectedUser.Name);
                if (name == null) return;
                var email = await DisplayPromptAsync("Muokkaa k‰ytt‰j‰‰", "S‰hkˆposti:", initialValue: selectedUser.Email);
                selectedUser.Name = name.Trim();
                selectedUser.Email = email?.Trim() ?? string.Empty;
                var idx = users.IndexOf(selectedUser);
                if (idx >= 0) users[idx] = selectedUser;
                await SaveAll();
            }

            EventsList.SelectedItem = null;
            if (UsersList.SelectedItems is IList selectedItems)
                selectedItems.Clear();
        }

        void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = e.NewTextValue?.Trim().ToLower() ?? string.Empty;
            if (EventsList.IsVisible)
            {
                EventsList.ItemsSource = string.IsNullOrWhiteSpace(text)
                    ? events
                    : new ObservableCollection<Event>(events.Where(x =>
                        (x.Title ?? string.Empty).ToLower().Contains(text) ||
                        (x.Subtitle ?? string.Empty).ToLower().Contains(text)));
            }
            else
            {
                UsersList.ItemsSource = string.IsNullOrWhiteSpace(text)
                    ? users
                    : new ObservableCollection<User>(users.Where(x =>
                        (x.Name ?? string.Empty).ToLower().Contains(text) ||
                        (x.Email ?? string.Empty).ToLower().Contains(text)));
            }

            UpdateEmptyLabel();
        }

        void OnEventSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedEvent = e.CurrentSelection.FirstOrDefault() as Event;
        }

        void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = e.CurrentSelection.FirstOrDefault() as User;
        }

        private async void OnAddParticipantsClicked(object sender, EventArgs e)
        {
            var selectedEvent = EventsList.SelectedItem as Event;
            var selectedUsers = UsersList.SelectedItems?.Cast<User>().ToList() ?? new();

            if (selectedEvent == null || !selectedUsers.Any())
            {
                await DisplayAlert("Virhe", "Valitse tapahtuma ja v‰hint‰‰n yksi k‰ytt‰j‰.", "OK");
                return;
            }

            foreach (var user in selectedUsers)
            {
                if (selectedEvent.ParticipantIds == null)
                    selectedEvent.ParticipantIds = new List<Guid>();
                if (!selectedEvent.ParticipantIds.Contains(user.Id))
                    selectedEvent.ParticipantIds.Add(user.Id);
            }

            await SaveAll();
            await DisplayAlert("Onnistui", "Osallistujat lis‰tty tapahtumaan.", "OK");
        }
    }
}