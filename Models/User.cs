using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Group_2.Models
{
    public class User : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        string name = "";
        public string Name
        {
            get => name;
            set => SetField(ref name, value);
        }

        string email = "";
        public string Email
        {
            get => email;
            set => SetField(ref email, value);
        }

        string password = "";
        public string Password
        {
            get => password;
            set => SetField(ref password, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}