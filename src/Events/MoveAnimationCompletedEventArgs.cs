using RV.WpfChessboard.Types;

namespace RV.WpfChessboard.Events;

internal class MoveAnimationCompletedEventArgs : EventArgs
{
    public MoveAnimationCompletedEventArgs(List<Piece> finalPosition) : base()
    {
        FinalPosition = finalPosition;
    }

    public List<Piece> FinalPosition { get; }
}
