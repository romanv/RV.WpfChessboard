#  `RV.WpfChessboard`
Chessboard component for WPF applications.

## Usage
```XAML
<chessboard:WpfChessboard
    Position="{Binding GameState.Position}">
    <behaviors:Interaction.Triggers>
        <behaviors:EventTrigger EventName="OnMoveCompleted">
            <behaviors:InvokeCommandAction Command="{Binding MoveCompletedCommand}" PassEventArgsToCommand="True" />
        </behaviors:EventTrigger>
    </behaviors:Interaction.Triggers>
</chessboard:WpfChessboard>
```

## Properties

| Property | Type | Description |
| --- | --- | --- |
| LightSquaresColor | SolidColorBrush | Color of light squares |
| DarkSquaresColor | SolidColorBrush | Color of dark squares |
| HighlightedSquaresColor | SolidColorBrush | Color of highlighted squares |
| HighlightedSquares | IEnumerable<string> | List of highlighted squares (a1, c7, etc.) |
| LegalDestinationsMarkerColor | SolidColorBrush | Color of legal move destination squares |
| LegalMoveDestinations | IENumerable\<string> | List of legal destination squares (a1, c7, etc.) |
| UseLegalMoveDestinations | bool | Use legal move destinations or allow any move |
| IsFlipped | bool | Show flipped board (with black on the bottom) |
| Position | string | Position on the board (in FEN chess notation) |
| Arrows | IEnumerable\<WpfChessboardArrow> | List of arrows to draw on the board |
| CornerRadius | double | Board corner radius |
| FontFamily | FontFamily | Font family used to draw square coordinates |
| DrawCoordinates | bool | Draw square coordinates |
| BlockInteractions | bool | Block all used interactions with the board |

## Events

| Name | Type | Description |
| --- | --- | --- |
| OnMoveStarted | MoveStartedEventArgs | Triggered after the interaction with a piece |
| OnMoveCompleted | MoveCompletedEventArgs | Triggered after the piece is dropped on a target square |
