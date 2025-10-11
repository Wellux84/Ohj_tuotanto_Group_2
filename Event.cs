using System;
using System.Collections.Generic;
using System.ComponentModel;

public class Event : INotifyPropertyChanged
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Subtitle { get; set; }
    public DateTime Date { get; set; }
    public List<Guid> ParticipantIds { get; set; }
    private string participantsDisplay;
    public string ParticipantsDisplay { get; set; }

    // Add this property for filtering/searching
    private bool isVisible = true;
    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (isVisible != value)
            {
                isVisible = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;
}