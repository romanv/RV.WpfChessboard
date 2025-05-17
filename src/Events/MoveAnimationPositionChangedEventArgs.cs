using System.Windows;
using System.Windows.Media;

namespace RV.WpfChessboard.Events;

internal class MoveAnimationPositionChangedEventArgs : EventArgs
{
    public MoveAnimationPositionChangedEventArgs(List<(DrawingImage Image, Point TopLeft)> pieces) : base()
    {
        Pieces = pieces;
    }

    public List<(DrawingImage Image, Point TopLeft)> Pieces { get; }
}
