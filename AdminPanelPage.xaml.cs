using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.ApplicationModel;
using Group_2.Models;
using Group_2.Services;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics;

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
                    base.OnPropertyChanged(nameof(SelectedEvent));
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
                    base.OnPropertyChanged(nameof(SelectedUser));
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
                    base.OnPropertyChanged(nameof(SelectedUsers));
                }
            }
        }

        public bool IsEventView => (TypePicker?.SelectedIndex ?? 0) == 0;
        public bool IsUserView => (TypePicker?.SelectedIndex ?? 0) == 1;


        public AdminPanelPage()
        {
            try
            {
                InitializeComponent();
            }
            catch (Exception ex)
            {
                // ƒLƒ yrit‰ DisplayAlertia t‰ss‰ ñ MainPage ei ole viel‰ n‰kyviss‰.
                // N‰ytet‰‰n virheteksti suoraan sivun sis‰ltˆn‰ ja poistutaan.
                Content = new ScrollView
                {
                    Padding = 16,
                    Content = new Label
                    {
                        Text = "XAML INIT FAIL:\n\n" + ex.ToString(),
                        FontSize = 12
                    }
                };
                return;
            }

            BindingContext = this;

            // Aseta Picker vasta kun XAML on varmasti ladattu
            Dispatcher.Dispatch(() =>
            {
                if (TypePicker != null) TypePicker.SelectedIndex = 0;
            });
        }



        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Run quick connection test first to show full error if it fails
                var connError = await DatabaseService.TestConnectionAsync();
                if (connError != null)
                {
                    System.Diagnostics.Debug.WriteLine("DB TestConnection failed:\n" + connError);
                    await ShowExceptionDialogAsync("DB connection failed", connError);
                    return;
                }

                // Ensure schema and load data at startup (must be enabled for load to work)
                await DatabaseService.EnsureSchemaAsync();
                await LoadData();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OnAppearing exception:\n" + ex);
                await ShowExceptionDialogAsync("DB-yhteysvirhe", ex.ToString());
            }
        }

        async Task LoadData()
        {
            try
            {
                var evs = await DatabaseService.LoadEventsAsync();
                var us = await DatabaseService.LoadUsersAsync();

                users.Clear();
                foreach (var u in us) users.Add(u);

                events.Clear();
                foreach (var e in evs)
                {
                    // Ensure ParticipantIds is non-null
                    if (e.ParticipantIds == null)
                        e.ParticipantIds = new List<Guid>();

                    var participantCount = e.ParticipantIds.Count;

                    // Simple Finnish pluralization: 0 => "Ei osallistujia", 1 => "1 osallistuja", else "N osallistujaa"
                    if (participantCount == 0)
                        e.ParticipantsDisplay = "Ei osallistujia";
                    else if (participantCount == 1)
                        e.ParticipantsDisplay = "1 osallistuja";
                    else
                        e.ParticipantsDisplay = $"{participantCount} osallistujaa";

                    // ensure visible flag default is true so UI shows items
                    e.IsVisible = true;

                    events.Add(e);
                }

                Debug.WriteLine($"LoadData: loaded {users.Count} users and {events.Count} events.");

                // Force refresh CollectionView to pick up updated ParticipantsDisplay
                EventsList.ItemsSource = null;
                EventsList.ItemsSource = events;

                UpdateEmptyLabel();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("LoadData exception: " + ex);
                await ShowExceptionDialogAsync("LoadData failed", ex.ToString());
            }
        }


        async Task SaveAll()
        {
            try
            {
                // debug: log events and participant counts before saving
                Debug.WriteLine("Saving events/users. Current in-memory event participant state:");
                foreach (var ev in events)
                {
                    var ids = ev.ParticipantIds ?? new List<Guid>();
                    Debug.WriteLine($"Event {ev.Id} '{ev.Title}' -> {ids.Count} participants: {string.Join(", ", ids)}");
                }

                // Save users first (FK targets), then events (event rows + per-event joins)
                await DatabaseService.SaveUsersAsync(users.ToList());
                await DatabaseService.SaveEventsAsync(events.ToList());

                // Reload from DB so in-memory state exactly matches DB
                await LoadData();

                // debug: show join rows after save
                try
                {
                    var joins = await DatabaseService.LoadJoinsAsync();
                    Debug.WriteLine("ilmoittautuminen rows after save: " + (joins.Any() ? string.Join("; ", joins.Select(j => $"{j.EventId}/{j.UserId}")) : "(no rows)"));
                }
                catch (Exception dbgEx)
                {
                    Debug.WriteLine("LoadJoinsAsync failed: " + dbgEx);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SaveAll exception:\n" + ex);
                await ShowExceptionDialogAsync("Tallennus ep‰onnistui", ex.ToString());
            }
        }


        // New helper: modal dialog with copy-to-clipboard (kept from previous change)
        private async Task ShowExceptionDialogAsync(string title, string message)
        {
            var editor = new Editor
            {
                Text = message ?? string.Empty,
                IsReadOnly = true,
                FontSize = 12,
                HeightRequest = 300
            };

            var titleLabel = new Label
            {
                Text = title,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16
            };

            var copyButton = new Button { Text = "Copy" };
            copyButton.Clicked += async (_, __) =>
            {
                try
                {
                    await Clipboard.Default.SetTextAsync(message ?? string.Empty);
                    await DisplayAlert("Copied", "Exception text copied to clipboard.", "OK");
                }
                catch (Exception ex)
                {
                    await DisplayAlert("Copy failed", ex.Message, "OK");
                }
            };

            var closeButton = new Button { Text = "Close" };
            closeButton.Clicked += async (_, __) => await Navigation.PopModalAsync();

            var buttons = new StackLayout
            {
                Orientation = StackOrientation.Horizontal,
                Spacing = 12,
                HorizontalOptions = LayoutOptions.Center,
                Children = { copyButton, closeButton }
            };

            var page = new ContentPage
            {
                Title = title,
                Content = new StackLayout
                {
                    Padding = 20,
                    Children =
                    {
                        titleLabel,
                        new ScrollView { Content = editor, HeightRequest = 300 },
                        buttons
                    }
                }
            };

            await Navigation.PushModalAsync(page);
        }


        void UpdateEmptyLabel()
        {
            var showingEvents = TypePicker.SelectedIndex == 0;
            var hasItems = showingEvents ? events.Any() : users.Any();
            EmptyLabel.IsVisible = !hasItems;
        }

        void OnTypeChanged(object sender, EventArgs e)
        {
            base.OnPropertyChanged(nameof(IsEventView));
            base.OnPropertyChanged(nameof(IsUserView));
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
                var date = await DisplayPromptAsync("Uusi tapahtuma", "P‰iv‰m‰‰r‰ (pp.kk.vvvv):");
                if (!DateTime.TryParse(date, out var eventDate)) eventDate = DateTime.Today;

                var ev = new Event
                {
                    Id = Guid.NewGuid(),
                    Title = title.Trim(),
                    Subtitle = subtitle?.Trim() ?? string.Empty,
                    Date = eventDate,
                    ParticipantIds = new List<Guid>(),
                    ParticipantsDisplay = "Ei osallistujia"
                };
                events.Add(ev);
            }
            else if (IsUserView)
            {
                var name = await DisplayPromptAsync("Uusi k‰ytt‰j‰", "Nimi:");
                if (string.IsNullOrWhiteSpace(name)) return;
                var email = await DisplayPromptAsync("Uusi k‰ytt‰j‰", "S‰hkˆposti:");
                var u = new User
                {
                    Id = Guid.NewGuid(),
                    Name = name.Trim(),
                    Email = email?.Trim() ?? string.Empty,
                    Role = string.Empty
                };
                users.Add(u);
            }

            UpdateEmptyLabel();
            await SaveAll();
        }

        async void OnDeleteClicked(object sender, EventArgs e)
        {
            if (IsEventView && SelectedEvent != null)
            {
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{SelectedEvent.Title}'?", "Kyll‰", "Ei");
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
                var confirm = await DisplayAlert("Poista", $"Poistetaanko '{SelectedUser.Name}'?", "Kyll‰", "Ei");
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
                var name = await DisplayPromptAsync("Muokkaa k‰ytt‰j‰‰", "Nimi:", initialValue: SelectedUser.Name);
                if (name == null) return;
                var email = await DisplayPromptAsync("Muokkaa k‰ytt‰j‰‰", "S‰hkˆposti:", initialValue: SelectedUser.Email);
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
                // CollectionView doesn't automatically filter by IsVisible ó implement filtering if needed
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
            // Updated: support both pre-selected users and modal selection
            var selectedEvent = SelectedEvent;
            if (selectedEvent == null)
            {
                await DisplayAlert("Virhe", "Valitse tapahtuma jolle haluat lis‰t‰ osallistujia.", "OK");
                return;
            }

            // If user(s) are selected from the Users view, use them
            var selectedUsersList = SelectedUsers?.ToList() ?? new List<User>();

            List<Guid> usersToAddIds;

            if (selectedUsersList.Any())
            {
                usersToAddIds = selectedUsersList.Select(u => u.Id).ToList();
            }
            else
            {
                // No users preselected ó open modal selection (reuse existing dialog)
                var currently = selectedEvent.ParticipantIds ?? new List<Guid>();
                var picked = await ShowParticipantSelectionDialogAsync(currently);
                // ShowParticipantSelectionDialogAsync returns the full selected set ó we want new additions
                usersToAddIds = picked.Except(currently).ToList();
            }

            if (!usersToAddIds.Any())
            {
                await DisplayAlert("Virhe", "Valitse v‰hint‰‰n yksi k‰ytt‰j‰.", "OK");
                return;
            }

            // Add unique ids to the event
            if (selectedEvent.ParticipantIds == null)
                selectedEvent.ParticipantIds = new List<Guid>();

            foreach (var uid in usersToAddIds)
            {
                if (!selectedEvent.ParticipantIds.Contains(uid))
                    selectedEvent.ParticipantIds.Add(uid);
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
            await DisplayAlert("Onnistui", "Osallistujat lis‰tty tapahtumaan.", "OK");
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

        // Add this method to AdminPanelPage (temporary diagnostic)
        private async Task ShowJoinTableDebugAsync()
        {
            try
            {
                var joins = await DatabaseService.LoadJoinsAsync();
                var lines = joins.Select(j => $"Event={j.EventId}, User={j.UserId}").ToList();
                var text = lines.Any() ? string.Join("\n", lines) : "(no rows)";
                await ShowExceptionDialogAsync("ilmoittautuminen rows", text);
            }
            catch (Exception ex)
            {
                await ShowExceptionDialogAsync("LoadJoins failed", ex.ToString());
            }
        }
    }

    public class NullToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value != null;
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}