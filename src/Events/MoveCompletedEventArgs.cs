using System.Windows;

namespace RV.WpfChessboard.Events;

public class MoveCompletedEventArgs : RoutedEventArgs
{
    public MoveCompletedEventArgs(RoutedEvent e, string from, string to, char promoteTo) : base(e)
    {
        From = from;
        To = to;
        PromoteTo = promoteTo;
    }

    public string From { get; private set; }
    public string To { get; private set; }
    public char PromoteTo { get; private set; }
}
