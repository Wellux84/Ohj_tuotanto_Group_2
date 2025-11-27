using Group_2.Models;

namespace Group_2
{
    public partial class AddParticipantsDialog : ContentPage
    {
        readonly List<User> _users;
        readonly TaskCompletionSource<List<Guid>?> _tcs = new();

        public AddParticipantsDialog(IEnumerable<User> users, IEnumerable<Guid>? existingParticipantIds = null)
        {
            InitializeComponent();
            var existing = new HashSet<Guid>(existingParticipantIds ?? Enumerable.Empty<Guid>());
            _users = users.Where(u => !existing.Contains(u.Id)).ToList();
            UsersView.ItemsSource = _users;
            UsersView.SelectionMode = SelectionMode.Multiple;
        }

        public Task<List<Guid>?> WaitForResultAsync() => _tcs.Task;

        async void OnOk(object sender, EventArgs e)
        {
            var ids = UsersView.SelectedItems?.Cast<User>().Select(u => u.Id).ToList() ?? new();
            _tcs.TrySetResult(ids);
            await Navigation.PopModalAsync();
        }

        async void OnCancel(object sender, EventArgs e)
        {
            _tcs.TrySetResult(null);
            await Navigation.PopModalAsync();
        }
    }
}