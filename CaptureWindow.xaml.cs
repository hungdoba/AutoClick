using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace AutoClick
{
    public partial class CaptureWindow : Window
    {
        private Rectangle rectangle;

        public CaptureWindow()
        {
            InitializeComponent();

            // Initialize the rectangle
            InitializeRectangle();
        }

        private void InitializeRectangle()
        {
            // Create a Rectangle
            rectangle = new Rectangle
            {
                Width = 100,
                Height = 100,
                Stroke = Brushes.Red,
                StrokeThickness = 2
            };

            // Add the Rectangle to the Canvas
            SelectionCanvas.Children.Add(rectangle);
        }

        private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            // Get the mouse position relative to the Canvas
            //Point mousePos = e.GetPosition(SelectionCanvas);

            Console.WriteLine("here");

            // Update the position of the Rectangle
            //Canvas.SetLeft(rectangle, mousePos.X - rectangle.Width / 2);
            //Canvas.SetTop(rectangle, mousePos.Y - rectangle.Height / 2);
        }
    }
}
