namespace RV.WpfChessboard.Types;

internal struct Piece : IEquatable<Piece>
{
    internal int Square { get; set; }
    internal PieceType Type { get; init; }
    internal ChessColor Side { get; init; }

    public readonly bool Equals(Piece other) =>
        Square == other.Square && Type == other.Type && Side == other.Side;

    public override readonly bool Equals(object? obj) => obj is Piece dp && Equals(dp);

    public override readonly int GetHashCode() => HashCode.Combine(Square, Type, Side);

    public static bool operator ==(Piece left, Piece right) => left.Equals(right);

    public static bool operator !=(Piece left, Piece right) => !(left == right);
}
