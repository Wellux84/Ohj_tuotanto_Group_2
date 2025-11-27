using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Group_2.Models
{
    public class EventItem : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string title = "";
        public string Title
        {
            get => title;
            set => SetField(ref title, value);
        }

        private string description = "";
        public string Description
        {
            get => description;
            set => SetField(ref description, value);
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get => startDate;
            set => SetField(ref startDate, value);
        }

        private DateTime endDate;
        public DateTime EndDate
        {
            get => endDate;
            set => SetField(ref endDate, value);
        }

        private string category = "";
        public string Category
        {
            get => category;
            set => SetField(ref category, value);
        }

        private List<Guid> participantIds = new();
        public List<Guid> ParticipantIds
        {
            get => participantIds;
            set => SetField(ref participantIds, value);
        }

        private string participantsDisplay = "Ei osallistujia";

        [JsonIgnore]
        public string ParticipantsDisplay
        {
            get => participantsDisplay;
            set => SetField(ref participantsDisplay, value);
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