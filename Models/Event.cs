using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Group_2.Models
{
    public class Event : INotifyPropertyChanged
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public List<Guid> ParticipantIds { get; set; } = new();
        private string participantsDisplay = string.Empty;
        public string ParticipantsDisplay
        {
            get => participantsDisplay;
            set
            {
                if (participantsDisplay != value)
                {
                    participantsDisplay = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ParticipantsDisplay)));
                }
            }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}