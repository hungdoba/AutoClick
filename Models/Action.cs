using System.ComponentModel;

namespace AutoClick.Models
{
    public class Action(string type, string? button, Position? position, string? data, bool? executeSucceeded = null) : INotifyPropertyChanged
    {
        public string Type { get; set; } = type;
        public string? Button { get; set; } = button;

        public Position? Position { get; set; } = position;
        public string? Data { get; set; } = data;
        public bool? ExecuteSucceeded { get; set; } = executeSucceeded;


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
