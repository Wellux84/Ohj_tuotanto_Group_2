using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Group_2.Models;
using Group_2.Services;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

namespace Group_2
{
    public partial class AdminPanelPage : ContentPage, INotifyPropertyChanged
    {
        public ObservableCollection<Event> events { get; } = new();
        public ObservableCollection<User> users { get; } = new();

        private Event? selectedEvent;
        private User? selectedUser;
        private ObservableCollection<User> selectedUsers = new();

        public Event? SelectedEvent
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

        public AdminPanelPage()
        {
            InitializeComponent();
            BindingContext = this;
            TypePicker.SelectedIndex = 0;
            _ = LoadData();
        }

        async Task LoadData()
        {
            var evs = await DatabaseService.LoadEventsAsync();
            var us = await DatabaseService.LoadUsersAsync();
            users.Clear();
            foreach (var u in us) users.Add(u);

            events.Clear();
            foreach (var e in evs)
            {
                var names = e.ParticipantIds != null
                    ? string.Join(", ", e.ParticipantIds.Select(id => users.FirstOrDefault(u => u.Id == id)?.Name).Where(n => !string.IsNullOrEmpty(n)))
                    : "";
                e.ParticipantsDisplay = string.IsNullOrEmpty(names) ? "Ei osallistujia" : $"Osallistujat: {names}";
                events.Add(e);
            }

            UpdateEmptyLabel();
        }

        async Task SaveAll()
        {
            await DatabaseService.SaveEventsAsync(events.ToList());
            await DatabaseService.SaveUsersAsync(users.ToList());
        }

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

        async void OnAddClicked(object sender, EventArgs e)
        {
            if (IsEventView)
            {
                var title = await DisplayPromptAsync("Uusi tapahtuma", "Otsikko:");
                if (string.IsNullOrWhiteSpace(title)) return;
                var subtitle = await DisplayPromptAsync("Uusi tapahtuma", "Kuvaus:");
                var date = await DisplayPromptAsync("Uusi tapahtuma", "Päivämäärä (pp.kk.vvvv):");
                if (!DateTime.TryParse(date, out var eventDate)) eventDate = DateTime.Today;
                var ev = new Event { Title = title.Trim(), Subtitle = subtitle?.Trim() ?? string.Empty, Date = eventDate };
                events.Add(ev);
            }
            else if (IsUserView)
            {
                var name = await DisplayPromptAsync("Uusi käyttäjä", "Nimi:");
                if (string.IsNullOrWhiteSpace(name)) return;
                var email = await DisplayPromptAsync("Uusi käyttäjä", "Sähköposti:");
                var u = new User { Name = name.Trim(), Email = email?.Trim() ?? string.Empty };
                users.Add(u);
            }

            UpdateEmptyLabel();
            await SaveAll();
        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (IsEventView && SelectedEvent != null)
            {
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{SelectedEvent.Title}'?", "Kyllä", "Ei");
                if (confirm)
                {
                    events.Remove(SelectedEvent);
                    SelectedEvent = null;
                    UpdateEmptyLabel();
                    await SaveAll();
                }
            }
            else if (IsUserView && SelectedUser != null)
            {
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{SelectedUser.Name}'?", "Kyllä", "Ei");
                if (confirm)
                {
                    users.Remove(SelectedUser);
                    SelectedUser = null;
                    UpdateEmptyLabel();
                    await SaveAll();
                }
            }
        }

        async void OnEditClicked(object sender, EventArgs e)
        {
            if (IsEventView && SelectedEvent != null)
            {
                var title = await DisplayPromptAsync("Muokkaa tapahtumaa", "Otsikko:", initialValue: SelectedEvent.Title);
                if (title == null) return;
                var subtitle = await DisplayPromptAsync("Muokkaa tapahtumaa", "Kuvaus:", initialValue: SelectedEvent.Subtitle);
                SelectedEvent.Title = title.Trim();
                SelectedEvent.Subtitle = subtitle?.Trim() ?? string.Empty;
                var idx = events.IndexOf(SelectedEvent);
                if (idx >= 0) events[idx] = SelectedEvent;
                await SaveAll();
            }
            else if (IsUserView && SelectedUser != null)
            {
                var name = await DisplayPromptAsync("Muokkaa käyttäjää", "Nimi:", initialValue: SelectedUser.Name);
                if (name == null) return;
                var email = await DisplayPromptAsync("Muokkaa käyttäjää", "Sähköposti:", initialValue: SelectedUser.Email);
                SelectedUser.Name = name.Trim();
                SelectedUser.Email = email?.Trim() ?? string.Empty;
                var idx = users.IndexOf(SelectedUser);
                if (idx >= 0) users[idx] = SelectedUser;
                await SaveAll();
            }
        }

        void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var text = e.NewTextValue?.Trim().ToLower() ?? string.Empty;
            if (IsEventView)
            {
                foreach (var ev in events)
                {
                    ev.IsVisible = string.IsNullOrWhiteSpace(text) ||
                        (ev.Title ?? string.Empty).ToLower().Contains(text) ||
                        (ev.Subtitle ?? string.Empty).ToLower().Contains(text);
                }
                // If your UI binds to events directly, ensure your CollectionView/ListView uses a CollectionViewSource or similar to filter by IsVisible
                // Or, alternatively, update the ItemsSource to a filtered collection here
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

        void OnEventSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedEvent = e.CurrentSelection.FirstOrDefault() as Event;
        }

        void OnUserSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = e.CurrentSelection.FirstOrDefault() as User;
            if (UsersList.SelectedItems is IList selectedItems)
            {
                SelectedUsers = new ObservableCollection<User>(selectedItems.Cast<User>());
            }
        }

        private async void OnAddParticipantsClicked(object sender, EventArgs e)
        {
            var selectedEvent = SelectedEvent;
            var selectedUsers = SelectedUsers?.ToList() ?? new();

            if (selectedEvent == null || !selectedUsers.Any())
            {
                await DisplayAlert("Virhe", "Valitse tapahtuma ja vähintään yksi käyttäjä.", "OK");
                return;
            }

            foreach (var user in selectedUsers)
            {
                if (!selectedEvent.ParticipantIds.Contains(user.Id))
                    selectedEvent.ParticipantIds.Add(user.Id);
            }

            var names = selectedEvent.ParticipantIds != null
                ? string.Join(", ", selectedEvent.ParticipantIds.Select(id => users.FirstOrDefault(u => u.Id == id)?.Name).Where(n => !string.IsNullOrEmpty(n)))
                : "";
            selectedEvent.ParticipantsDisplay = string.IsNullOrEmpty(names) ? "Ei osallistujia" : $"Osallistujat: {names}";

            var idx = events.IndexOf(selectedEvent);
            if (idx >= 0)
            {
                events[idx] = selectedEvent;
            }

            await SaveAll();
            await DisplayAlert("Onnistui", "Osallistujat lisätty tapahtumaan.", "OK");
        }

        private async void OnEditParticipantsClicked(object sender, EventArgs e)
        {
            if (SelectedEvent == null)
            {
                await DisplayAlert("Virhe", "Valitse muokattava tapahtuma.", "OK");
                return;
            }

            var selectedIds = SelectedEvent.ParticipantIds ?? new List<Guid>();
            var newIds = await ShowParticipantSelectionDialogAsync(selectedIds);

            SelectedEvent.ParticipantIds = newIds;
            var names = newIds.Any()
                ? string.Join(", ", newIds.Select(id => users.FirstOrDefault(u => u.Id == id)?.Name).Where(n => !string.IsNullOrEmpty(n)))
                : "";
            SelectedEvent.ParticipantsDisplay = string.IsNullOrEmpty(names) ? "Ei osallistujia" : $"Osallistujat: {names}";

            var idx = events.IndexOf(SelectedEvent);
            if (idx >= 0) events[idx] = SelectedEvent;
            await SaveAll();
        }

        private async Task<List<Guid>> ShowParticipantSelectionDialogAsync(List<Guid> currentParticipantIds)
        {
            var page = new ContentPage { Title = "Valitse osallistujat" };
            var selectedIds = new HashSet<Guid>(currentParticipantIds);

            var collectionView = new CollectionView
            {
                ItemsSource = users,
                SelectionMode = SelectionMode.Multiple,
                ItemTemplate = new DataTemplate(() =>
                {
                    var label = new Label();
                    label.SetBinding(Label.TextProperty, nameof(User.Name));
                    return new StackLayout { Children = { label }, Padding = new Thickness(10) };
                }),
            };

            collectionView.SelectedItems = users.Where(u => selectedIds.Contains(u.Id)).Cast<object>().ToList();

            var okButton = new Button { Text = "OK" };
            okButton.Clicked += async (s, e) =>
            {
                await page.Navigation.PopModalAsync();
            };

            page.Content = new StackLayout
            {
                Padding = 20,
                Children = { collectionView, okButton }
            };

            await Navigation.PushModalAsync(page);

            var tcs = new TaskCompletionSource<object?>();
            page.Disappearing += (s, e2) => tcs.TrySetResult(null);
            await tcs.Task;

            return collectionView.SelectedItems.Cast<User>().Select(u => u.Id).ToList();
        }

        public new event PropertyChangedEventHandler? PropertyChanged;
        protected new void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class NullToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value != null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}