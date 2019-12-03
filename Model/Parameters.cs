using System.Drawing;

namespace Bezier.Model
{
    public enum Rotation { Naive, Filter }
    public enum Animation { Rotation, Move }
    class Parameters
    {
        public uint NumberOfPoints { get; set; }
        public bool IsPolylineVisible { set; get; }
        public bool IsGrayColors { set; get; }
        public bool IsRunning { set; get; }
        public Bitmap Image { set; get; }
        public Rotation Rotation { set; get; }
        public Animation Animation { set; get; }
    }
}
