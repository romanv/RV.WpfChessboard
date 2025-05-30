using System.Windows;

namespace RV.WpfChessboard.Events;

public class MoveCancelledEventArgs : RoutedEventArgs
{
    public MoveCancelledEventArgs(RoutedEvent e) : base(e)
    {
    }
}
