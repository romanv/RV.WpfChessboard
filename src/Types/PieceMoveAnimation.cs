using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using RV.WpfChessboard.Events;
using RV.WpfChessboard.Types;

namespace RV.WpfChessboard.Types;

internal class PieceMoveAnimation
{
    public static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(175);

    private readonly List<(DrawingImage Image, Point StartPos, Point EndPos)> _animations;
    private readonly Stopwatch _timer = new();

    public PieceMoveAnimation(
        List<(ChessColor Side, PieceType Type, Point From, Point To)> animations,
        List<Piece> finalPosition)
    {
        _animations = animations.Select(a => (
            PieceImages.Images[(a.Side, a.Type)],
            a.From,
            a.To
        )).ToList();
        FinalPosition = finalPosition;
    }

    public List<Piece> FinalPosition { get; }

    internal event EventHandler<MoveAnimationCompletedEventArgs>? OnCompleted;

    internal event EventHandler<MoveAnimationPositionChangedEventArgs>? OnUpdated;

    public void Start()
    {
        _timer.Restart();
        CompositionTarget.Rendering += OnRenderFrame;
    }

    private void OnRenderFrame(object? sender, EventArgs e)
    {
        var timelinePct = _timer.ElapsedMilliseconds / Duration.TotalMilliseconds;
        var f = InOutCubicEase(timelinePct);
        var args = new MoveAnimationPositionChangedEventArgs(
            _animations
                .Select(a =>
                    (
                        a.Image,
                        new Point(
                            a.StartPos.X + (a.EndPos.X - a.StartPos.X) * f,
                            a.StartPos.Y + (a.EndPos.Y - a.StartPos.Y) * f
                        )
                    )
                )
                .ToList()
        );

        OnUpdated?.Invoke(this, args);

        if (_timer.ElapsedMilliseconds >= Duration.TotalMilliseconds)
        {
            CompositionTarget.Rendering -= OnRenderFrame;
            OnCompleted?.Invoke(this, new MoveAnimationCompletedEventArgs(FinalPosition));
        }
    }

    // https://gist.github.com/gre/1650294
    private static double InOutCubicEase(double time)
    {
        return time < 0.5
            ? 4 * Math.Pow(time, 3)
            : (time - 1) * (2 * time - 2) * (2 * time - 2) + 1;
    }
}
