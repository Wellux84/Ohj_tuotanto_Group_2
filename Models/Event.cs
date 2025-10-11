using System;

namespace Group_2.Models
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
    }
}