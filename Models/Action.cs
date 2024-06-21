namespace AutoClick.Models
{
    public class Action(string type, string? button, System.Drawing.Point? position, string? data)
    {
        public string Type { get; set; } = type;
        public string? Button { get; set; } = button;
        public System.Drawing.Point? Position { get; set; } = position;
        public string? Data { get; set; } = data;
    }
}
