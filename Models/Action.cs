using System.ComponentModel;
using System.Drawing;

namespace AutoClick.Models
{
    public class Action : INotifyPropertyChanged
    {
        public string Type { get; set; }
        public string? Button { get; set; }
        public Point? Position { get; set; }
        public string? Data { get; set; }
        public bool? ExecuteSucceeded { get; set; }

        public Action(string type, string? button, Point? position, string? data, bool? executeSucceeded = null)
        {
            Type = type;
            Button = button;
            Position = position;
            Data = data;
            ExecuteSucceeded = executeSucceeded;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
