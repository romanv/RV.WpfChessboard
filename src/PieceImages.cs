using System.Reflection;
using System.Windows;
using System.Windows.Media;
using RV.WpfChessboard.Types;

namespace RV.WpfChessboard;

internal static class PieceImages
{
    private static readonly Dictionary<(ChessColor Color, PieceType Piece), string> _piecesResources = new()
    {
        { (ChessColor.Light, PieceType.Pawn), "wp" },
        { (ChessColor.Light, PieceType.Knight), "wn" },
        { (ChessColor.Light, PieceType.Bishop), "wb" },
        { (ChessColor.Light, PieceType.Rook), "wr" },
        { (ChessColor.Light, PieceType.Queen), "wq" },
        { (ChessColor.Light, PieceType.King), "wk" },
        { (ChessColor.Dark, PieceType.Pawn), "bp" },
        { (ChessColor.Dark, PieceType.Knight), "bn" },
        { (ChessColor.Dark, PieceType.Bishop), "bb" },
        { (ChessColor.Dark, PieceType.Rook), "br" },
        { (ChessColor.Dark, PieceType.Queen), "bq" },
        { (ChessColor.Dark, PieceType.King), "bk" },
    };

    internal static readonly Dictionary<(ChessColor Color, PieceType Piece), DrawingImage> Images = [];

    static PieceImages()
    {
        var assembly = typeof(WpfChessboard).GetTypeInfo().Assembly;
        var locator = new Uri($"/{assembly};component/Pieces.xaml", UriKind.Relative);
        var dict = (ResourceDictionary)Application.LoadComponent(locator);

        foreach (var desc in _piecesResources)
        {
            if (dict[desc.Value] is DrawingImage img)
                Images.Add(desc.Key, img);
            else
                throw new KeyNotFoundException($"Could not load piece image {desc.Value}");
        }
    }
}
