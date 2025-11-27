using Group_2.Models;

namespace Group_2
{
    public partial class EditParticipantsDialog : ContentPage
    {
        readonly List<User> _users;
        readonly TaskCompletionSource<List<Guid>?> _tcs = new();

        public EditParticipantsDialog(IEnumerable<User> users, IEnumerable<Guid>? preselected = null)
        {
            InitializeComponent();

            var preselectedIds = preselected?.ToList() ?? new List<Guid>();

            _users = users.Where(u => preselectedIds.Contains(u.Id)).ToList();

            UsersView.ItemsSource = _users;

            Device.BeginInvokeOnMainThread(() =>
            {
                UsersView.SelectedItems = _users.Cast<object>().ToList();
            });
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