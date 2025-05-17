using System.Windows;
using System.Windows.Media;
using RV.WpfChessboard.Events;
using RV.WpfChessboard.Types;
using static RV.WpfChessboard.WpfChessboard;

namespace RV.WpfChessboard;

internal class PromotionPopup
{
    private static readonly SolidColorBrush _mouseOverBrush = new(Color.FromArgb(100, 255, 255, 255));
    private static readonly SolidColorBrush _shadowBrush = new(Color.FromArgb(50, 0, 0, 0));
    private static readonly PieceType[] _pieceTypes =
        [PieceType.Queen, PieceType.Knight, PieceType.Bishop, PieceType.Rook];

    private readonly double _pieceHeight;
    private readonly Rect[] _squares = new Rect[4];
    private readonly Rect[] _pieces = new Rect[4];
    private readonly Rect _borderRect;
    private readonly Rect _shadowRect;
    private readonly Rect _innerRect;
    private readonly double _backdropWidth;
    private readonly double _backdropHeight;
    private readonly string _from;
    private readonly string _to;
    private readonly ChessColor _side;
    private readonly LinearGradientBrush _bgBrush;
    private readonly RoutedEvent _moveCompleted;

    public PromotionPopup(
        double backdropWidth,
        double backdropHeight,
        string from,
        string to,
        Rect rect,
        ChessColor side,
        SolidColorBrush background,
        RoutedEvent moveCompleted)
    {
        _backdropWidth = backdropWidth;
        _backdropHeight = backdropHeight;
        _from = from;
        _to = to;
        _innerRect = rect;
        _borderRect = new(rect.Left - 2, rect.Top - 2, rect.Width + 4, rect.Height + 4);
        _shadowRect = _borderRect;
        _shadowRect.Offset(4, 4);
        _pieceHeight = Math.Floor(rect.Height * 0.75);
        _side = side;
        _bgBrush = new LinearGradientBrush(background.Color, Darken(background.Color), 30);
        _moveCompleted = moveCompleted;
        var squareWidth = rect.Width / 4;

        for (var i = 0; i < 4; i++)
        {
            _squares[i] = new Rect(_innerRect.X + squareWidth * i, _innerRect.Y, squareWidth, _innerRect.Height);
            var pieceImage = PieceImages.Images[(ChessColor.Light, _pieceTypes[i])];
            _pieces[i] = GetPieceRect(pieceImage, _squares[i].TopLeft, rect.Height, _pieceHeight);
        }
    }

    public void Draw(DrawingContext dc, Point mousePos)
    {
        // Backdrop
        dc.DrawRectangle(_shadowBrush, null, new(0, 0, _backdropWidth, _backdropHeight));
        // Shadow
        dc.DrawRoundedRectangle(_shadowBrush, null, _shadowRect, 3, 3);
        // Popup
        dc.DrawRectangle(Brushes.Black, null, _borderRect);
        dc.DrawRectangle(_bgBrush, null, _innerRect);

        for (var i = 0; i < 4; i++)
        {
            if (_squares[i].Contains(mousePos))
            {
                dc.DrawRectangle(_mouseOverBrush, null, _squares[i]);
                break;
            }
        }

        dc.DrawImage(PieceImages.Images[(_side, PieceType.Queen)], _pieces[0]);
        dc.DrawImage(PieceImages.Images[(_side, PieceType.Knight)], _pieces[1]);
        dc.DrawImage(PieceImages.Images[(_side, PieceType.Bishop)], _pieces[2]);
        dc.DrawImage(PieceImages.Images[(_side, PieceType.Rook)], _pieces[3]);
    }

    internal MoveCompletedEventArgs? OnMouseDown(Point mousePos)
    {
        for (var i = 0; i < 4; i++)
        {
            if (_squares[i].Contains(mousePos))
                return new MoveCompletedEventArgs(_moveCompleted, _from, _to, PieceToPromotionChar(_pieceTypes[i]));
        }

        return null;
    }

    private static char PieceToPromotionChar(PieceType piece)
    {
        return piece switch
        {
            PieceType.Queen => 'q',
            PieceType.Rook => 'r',
            PieceType.Bishop => 'b',
            PieceType.Knight => 'n',
            _ => '?',
        };
    }

    private static Color Darken(Color c) => Color.FromRgb((byte)(c.R * 0.85), (byte)(c.G * 0.85), (byte)(c.B * 0.85));
}
