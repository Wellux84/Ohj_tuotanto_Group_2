using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Group_2.Models
{
    public class Event : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        private string title = string.Empty;
        public string Title
        {
            get => title;
            set { if (title != value) { title = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title))); } }
        }

        private string subtitle = string.Empty;
        public string Subtitle
        {
            get => subtitle;
            set { if (subtitle != value) { subtitle = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtitle))); } }
        }

        private DateTime date = DateTime.Today;
        public DateTime Date
        {
            get => date;
            set { if (date != value) { date = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Date))); } }
        }

        public List<Guid> ParticipantIds { get; set; } = new();

        private string participantsDisplay = string.Empty;
        public string ParticipantsDisplay
        {
            get => participantsDisplay;
            set { if (participantsDisplay != value) { participantsDisplay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParticipantsDisplay))); } }
        }

        private bool isVisible = true;
        public bool IsVisible
        {
            get => isVisible;
            set { if (isVisible != value) { isVisible = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible))); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}