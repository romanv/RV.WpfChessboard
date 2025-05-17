using System.Windows;

namespace RV.WpfChessboard.Events;

public class MoveStartedEventArgs : RoutedEventArgs
{
    public MoveStartedEventArgs(RoutedEvent e, string from) : base(e)
    {
        From = from;
    }

    public string From { get; private set; }
}
