using System.Windows;
using System.Windows.Controls;

namespace RV.WpfChessboard.Types;

internal class DraggedPiece
{
    public int From { get; init; }
    public Piece Piece { get; init; }
    public required Image Image { get; init; }
    public Point TopLeft { get; set; }
}
