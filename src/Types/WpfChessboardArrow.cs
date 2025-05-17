using System.Windows.Media;

namespace RV.WpfChessboard.Types;

public record WpfChessboardArrow
{
    public WpfChessboardArrow(string from, string to, Color color)
    {
        if (!IsValidSquare(from))
            throw new ArgumentException($"Invalid square '{from}'", nameof(from));

        if (!IsValidSquare(to))
            throw new ArgumentException($"Invalid square '{to}'", nameof(to));

        From = from;
        To = to;
        Color = color;
    }

    public string From { get; private set; }

    public string To { get; private set; }

    public Color Color { get; private set; }

    private static bool IsValidSquare(ReadOnlySpan<char> square) =>
        square.Length == 2 && square[0] >= 'a' && square[0] <= 'h' && square[1] >= '1' && square[1] <= '8';
}
