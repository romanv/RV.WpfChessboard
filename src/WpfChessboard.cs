using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RV.WpfChessboard.Events;
using RV.WpfChessboard.Types;

namespace RV.WpfChessboard;

public partial class WpfChessboard : Canvas
{
    private const string StartingPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private static readonly SolidColorBrush _defaultLightSquaresBrush = new(Color.FromRgb(220, 230, 230));
    private static readonly SolidColorBrush _defaultDarkSquaresBrush = new(Color.FromRgb(140, 160, 170));
    private static readonly SolidColorBrush _defaultLegalDestinationsMarkerBrush = new(Color.FromRgb(20, 85, 30));
    private static readonly SolidColorBrush _defaultSquareHighlightBrush = new(Colors.Orange);

    private readonly DrawingGroup _pieceAnimationDrawing = new();

    private Typeface _coordsTypeface;
    private PieceMoveAnimation? _activeAnimation;
    private PromotionPopup? _promotionPopup;
    private List<Piece> _pieces;
    private List<int> _moveDestinations = [];
    private DraggedPiece? _pieceInHand = null;
    private List<(StreamGeometry Geometry, Pen Pen)> _arrows = [];
    private (int From, int To) _lastPlayerMove = (-1, -1);
    private Rect[][] _squaresRects = new Rect[8][];
    private Point _mousePos;
    private double _squareSize;
    private double _pieceHeight;
    private double _destinationMarkerRadius;
    private int _squareUnderMouse;

    public static readonly DependencyProperty LightSquaresColorProperty = DependencyProperty.Register(
        nameof(LightSquaresColor),
        typeof(SolidColorBrush),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            _defaultLightSquaresBrush,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public SolidColorBrush LightSquaresColor
    {
        get { return (SolidColorBrush)GetValue(LightSquaresColorProperty); }
        set { SetValue(LightSquaresColorProperty, value); }
    }

    public static readonly DependencyProperty DarkSquaresColorProperty = DependencyProperty.Register(
        nameof(DarkSquaresColor),
        typeof(SolidColorBrush),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            _defaultDarkSquaresBrush,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public SolidColorBrush DarkSquaresColor
    {
        get { return (SolidColorBrush)GetValue(DarkSquaresColorProperty); }
        set { SetValue(DarkSquaresColorProperty, value); }
    }

    public static readonly DependencyProperty LegalDestinationsMarkerColorProperty = DependencyProperty.Register(
        nameof(LegalDestinationsMarkerColor),
        typeof(SolidColorBrush),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            _defaultLegalDestinationsMarkerBrush,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public SolidColorBrush LegalDestinationsMarkerColor
    {
        get { return (SolidColorBrush)GetValue(LegalDestinationsMarkerColorProperty); }
        set { SetValue(LegalDestinationsMarkerColorProperty, value); }
    }

    public static readonly DependencyProperty HighlightedSquaresColorProperty = DependencyProperty.Register(
        "HighlightedSquaresColor",
        typeof(SolidColorBrush),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            _defaultSquareHighlightBrush,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public SolidColorBrush HighlightedSquaresColor
    {
        get { return (SolidColorBrush)GetValue(HighlightedSquaresColorProperty); }
        set { SetValue(HighlightedSquaresColorProperty, value); }
    }

    public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.Register(
        nameof(IsFlipped),
        typeof(bool),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public bool IsFlipped
    {
        get { return (bool)GetValue(IsFlippedProperty); }
        set { SetValue(IsFlippedProperty, value); }
    }

    public static readonly DependencyProperty PositionProperty = DependencyProperty.Register(
        nameof(Position),
        typeof(string),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            StartingPosition,
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnPositionChanged));

    public string Position
    {
        get { return (string)GetValue(PositionProperty); }
        set { SetValue(PositionProperty, value); }
    }

    public static readonly DependencyProperty LegalMoveDestinationsProperty = DependencyProperty.Register(
            "LegalMoveDestinations",
            typeof(IEnumerable<string>),
            typeof(WpfChessboard),
            new FrameworkPropertyMetadata(
                Enumerable.Empty<string>(),
                FrameworkPropertyMetadataOptions.AffectsRender,
                OnLegalMoveDestinationsChanged));

    public IEnumerable<string> LegalMoveDestinations
    {
        get { return (IEnumerable<string>)GetValue(LegalMoveDestinationsProperty); }
        set { SetValue(LegalMoveDestinationsProperty, value); }
    }

    public static readonly DependencyProperty UseLegalMoveDestinationsProperty =
        DependencyProperty.Register(
            "UseLegalMoveDestinations",
            typeof(bool),
            typeof(WpfChessboard),
            new PropertyMetadata(true));

    public bool UseLegalMoveDestinations
    {
        get { return (bool)GetValue(UseLegalMoveDestinationsProperty); }
        set { SetValue(UseLegalMoveDestinationsProperty, value); }
    }

    public static readonly DependencyProperty ArrowsProperty = DependencyProperty.Register(
        "Arrows",
        typeof(IEnumerable<WpfChessboardArrow>),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            Enumerable.Empty<WpfChessboardArrow>(),
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnArrowsChanged));

    public IEnumerable<WpfChessboardArrow> Arrows
    {
        get { return (IEnumerable<WpfChessboardArrow>)GetValue(ArrowsProperty); }
        set { SetValue(ArrowsProperty, value); }
    }

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        "CornerRadius", typeof(double), typeof(WpfChessboard), new PropertyMetadata(4.0));

    public double CornerRadius
    {
        get { return (double)GetValue(CornerRadiusProperty); }
        set { SetValue(CornerRadiusProperty, value); }
    }

    public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.Register(
        "FontFamily",
        typeof(FontFamily),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            TextBlock.FontFamilyProperty.DefaultMetadata.DefaultValue,
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnFontFamilyChanged));

    public FontFamily FontFamily
    {
        get { return (FontFamily)GetValue(FontFamilyProperty); }
        set { SetValue(FontFamilyProperty, value); }
    }

    public static readonly DependencyProperty BlockInteractionsProperty = DependencyProperty.Register(
        "BlockInteractions",
        typeof(bool),
        typeof(WpfChessboard),
        new PropertyMetadata(false));

    public bool BlockInteractions
    {
        get { return (bool)GetValue(BlockInteractionsProperty); }
        set { SetValue(BlockInteractionsProperty, value); }
    }

    public static readonly DependencyProperty HighlightedSquaresProperty = DependencyProperty.Register(
        "HighlightedSquares",
        typeof(IEnumerable<string>),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            Enumerable.Empty<string>(),
            FrameworkPropertyMetadataOptions.AffectsRender,
            OnHighlightedSquaresChanged));

    public IEnumerable<string> HighlightedSquares
    {
        get { return (IEnumerable<string>)GetValue(HighlightedSquaresProperty); }
        set { SetValue(HighlightedSquaresProperty, value); }
    }

    public static readonly DependencyProperty DrawCoordinatesProperty = DependencyProperty.Register(
        "DrawCoordinates",
        typeof(bool),
        typeof(WpfChessboard),
        new FrameworkPropertyMetadata(
            true,
            FrameworkPropertyMetadataOptions.AffectsRender));

    public bool DrawCoordinates
    {
        get { return (bool)GetValue(DrawCoordinatesProperty); }
        set { SetValue(DrawCoordinatesProperty, value); }
    }

    public static readonly DependencyProperty AnimateProperty = DependencyProperty.Register(
        "Animate",
        typeof(bool),
        typeof(WpfChessboard),
        new PropertyMetadata(true));

    public bool Animate
    {
        get { return (bool)GetValue(AnimateProperty); }
        set { SetValue(AnimateProperty, value); }
    }

    public static readonly RoutedEvent OnMoveStartedEvent = EventManager.RegisterRoutedEvent(
        name: "OnMoveStarted",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(WpfChessboard));

    public event RoutedEventHandler OnMoveStarted
    {
        add { AddHandler(OnMoveStartedEvent, value); }
        remove { RemoveHandler(OnMoveStartedEvent, value); }
    }

    public static readonly RoutedEvent OnMoveCompletedEvent = EventManager.RegisterRoutedEvent(
        name: "OnMoveCompleted",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(WpfChessboard));

    public event RoutedEventHandler OnMoveCompleted
    {
        add { AddHandler(OnMoveCompletedEvent, value); }
        remove { RemoveHandler(OnMoveCompletedEvent, value); }
    }

    public static readonly RoutedEvent OnMoveCancelledEvent = EventManager.RegisterRoutedEvent(
        name: "OnMoveCancelled",
        routingStrategy: RoutingStrategy.Bubble,
        handlerType: typeof(RoutedEventHandler),
        ownerType: typeof(WpfChessboard));

    public event RoutedEventHandler OnMoveCancelled
    {
        add { AddHandler(OnMoveCancelledEvent, value); }
        remove { RemoveHandler(OnMoveCancelledEvent, value); }
    }

    public WpfChessboard()
    {
        Focusable = true;
        FocusVisualStyle = null;
        _pieces = GetPiecesFromFen(Position);
        _coordsTypeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        var min = Math.Min(constraint.Width, constraint.Height);

        _pieceInHand?.Image.Measure(constraint);

        return double.IsInfinity(min)
            ? new Size(0, 0)
            : new Size(min, min);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        var min = Math.Min(arrangeSize.Width, arrangeSize.Height);
        _squareSize = min / 8;
        _squaresRects = BuildGrid(_squareSize, min);
        _arrows = BuildArrows();
        _pieceHeight = Math.Floor(_squareSize * 0.75);
        _destinationMarkerRadius = Math.Round(_squareSize * 0.2, 0);
        _pieceInHand?.Image.Arrange(new Rect(
            _pieceInHand.TopLeft.X, _pieceInHand.TopLeft.Y, _pieceInHand.Image.Width, _pieceInHand.Image.Height));
        Clip = new RectangleGeometry(new(0, 0, min, min), CornerRadius, CornerRadius);

        return new Size(min, min);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        if (BlockInteractions)
            return;

        Focus();
        base.OnMouseDown(e);

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _mousePos = Mouse.GetPosition(this);

            if (_promotionPopup != null)
            {
                var promotionEvent = _promotionPopup.OnMouseDown(_mousePos);
                _promotionPopup = null;

                if (promotionEvent != null)
                    RaiseEvent(promotionEvent);

                InvalidateVisual();
            }
            else
            {
                var square = CoordsToSquare(_mousePos.X, _mousePos.Y, _squareSize, IsFlipped);
                var idx = _pieces.FindIndex(p => p.Square == square);

                if (idx < 0 || idx > 63)
                    return;

                var sideToMove = Position.Split(' ')[1] == "w" ? ChessColor.Light : ChessColor.Dark;

                if (_pieces[idx].Side != sideToMove)
                    return;

                var image = PieceImages.Images[(_pieces[idx].Side, _pieces[idx].Type)];
                var width = image.Width / image.Height * _squareSize;

                _pieceInHand = new()
                {
                    Piece = _pieces[idx],
                    From = square,
                    Image = new Image()
                    {
                        Source = image,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = _pieceHeight,
                        Width = width,
                    },
                    TopLeft = new Point(_mousePos.X - width / 2, _mousePos.Y - _pieceHeight / 2),
                };

                Children.Add(_pieceInHand.Image);
                SetLeft(_pieceInHand.Image, _pieceInHand.TopLeft.X);
                SetTop(_pieceInHand.Image, _pieceInHand.TopLeft.Y);

                RaiseEvent(new MoveStartedEventArgs(OnMoveStartedEvent, IdxToSquare(_pieceInHand.From)));
                CaptureMouse();
            }
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            ReleaseMouseCapture();
            _pieceInHand = null;
            _squareUnderMouse = -1;
            InvalidateVisual();
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (BlockInteractions)
            return;

        base.OnMouseMove(e);

        if (_pieceInHand == null && _promotionPopup == null)
            return;

        var mousePos = Mouse.GetPosition(this);
        _mousePos = new Point(
            Math.Clamp(mousePos.X, 0, ActualWidth - 1),
            Math.Clamp(mousePos.Y, 0, ActualHeight - 1));
        _squareUnderMouse = CoordsToSquare(_mousePos.X, _mousePos.Y, _squareSize, IsFlipped);

        if (_pieceInHand != null)
        {
            _pieceInHand.TopLeft = new Point(
                _mousePos.X - _pieceInHand.Image.Width / 2,
                _mousePos.Y - _pieceInHand.Image.Height / 2);
            SetLeft(_pieceInHand.Image, _pieceInHand.TopLeft.X);
            SetTop(_pieceInHand.Image, _pieceInHand.TopLeft.Y);
        }

        InvalidateVisual();
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        if (BlockInteractions)
            return;

        base.OnMouseUp(e);
        ReleaseMouseCapture();

        if (_pieceInHand == null)
            return;

        var mousePos = e.GetPosition(this);
        var isOutside = mousePos.X < 0 || mousePos.X > ActualWidth || mousePos.Y < 0 || mousePos.Y > ActualHeight;

        if (isOutside)
        {
            RaiseEvent(new MoveCancelledEventArgs(OnMoveCancelledEvent));
        }
        else
        {
            var from = IdxToSquare(_pieceInHand.From);
            var to = IdxToSquare(_squareUnderMouse);

            if (from != to)
            {
                if (_pieceInHand.Piece.Type == PieceType.Pawn
                    && (_pieceInHand.Piece.Side == ChessColor.Light && to[1] == '8'
                        || _pieceInHand.Piece.Side == ChessColor.Dark && to[1] == '1'))
                {
                    _promotionPopup = CreatePromotionPopup(from, to);
                }
                else
                {
                    if (!UseLegalMoveDestinations || LegalMoveDestinations.Contains(to))
                    {
                        _lastPlayerMove = (_pieceInHand.From, _squareUnderMouse);
                        RaiseEvent(new MoveCompletedEventArgs(
                            OnMoveCompletedEvent,
                            from,
                            to,
                            '?'));
                    }
                }
            }
        }

        Children.Remove(_pieceInHand.Image);
        _pieceInHand = null;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        DrawBoard(dc);
        if (DrawCoordinates)
            DrawCoords(dc);
        DrawSquareHighlights(dc);

        if (_pieceInHand != null)
        {
            if ((!UseLegalMoveDestinations || _moveDestinations.Contains(_squareUnderMouse))
                && _squareUnderMouse >= 0 && _squareUnderMouse <= 63)
            {
                dc.PushOpacity(0.3);
                var rect = GetSquareRect(_squareUnderMouse);
                dc.DrawRectangle(
                    LegalDestinationsMarkerColor,
                    null,
                    rect);
                dc.Pop();
            }
        }

        if (_pieceInHand != null && UseLegalMoveDestinations)
            DrawMoveDestinations(dc);

        DrawPieces(dc);

        if (_arrows.Count > 0)
            DrawArrows(dc);

        dc.DrawDrawing(_pieceAnimationDrawing);
        _promotionPopup?.Draw(dc, _mousePos);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (_pieceInHand != null)
            {
                Children.Remove(_pieceInHand.Image);
                _pieceInHand = null;
                _moveDestinations = [];
                RaiseEvent(new MoveCancelledEventArgs(OnMoveCancelledEvent));
            }
            else if (_promotionPopup != null)
            {
                _promotionPopup = null;
            }

            InvalidateVisual();
        }

        base.OnKeyDown(e);
    }

    private void DrawMoveDestinations(DrawingContext dc)
    {
        dc.PushOpacity(0.5);
        var occupiedByCapturablePiece = _pieceInHand != null
            ? _pieces.Where(p => _moveDestinations.Contains(p.Square) && p.Side != _pieceInHand.Piece.Side)
                .Select(p => p.Square)
                .ToHashSet()
            : [];
        var triangleSide = _squareSize / 5;

        foreach (var squareIdx in _moveDestinations)
        {
            if (squareIdx == _squareUnderMouse)
                continue;

            var center = GetSquareCenter(squareIdx);

            if (occupiedByCapturablePiece.Contains(squareIdx))
            {
                var rect = GetSquareRect(squareIdx);
                DrawTriangle(
                    dc,
                    rect.TopLeft,
                    new(rect.TopLeft.X + triangleSide, rect.TopLeft.Y),
                    new(rect.TopLeft.X, rect.TopLeft.Y + triangleSide),
                    LegalDestinationsMarkerColor);
                DrawTriangle(
                    dc,
                    rect.TopRight,
                    new(rect.TopRight.X - triangleSide, rect.TopRight.Y),
                    new(rect.TopRight.X, rect.TopRight.Y + triangleSide),
                    LegalDestinationsMarkerColor);
                DrawTriangle(
                    dc,
                    rect.BottomLeft,
                    new(rect.BottomLeft.X + triangleSide, rect.BottomLeft.Y),
                    new(rect.BottomLeft.X, rect.BottomLeft.Y - triangleSide),
                    LegalDestinationsMarkerColor);
                DrawTriangle(
                    dc,
                    rect.BottomRight,
                    new(rect.BottomRight.X - triangleSide, rect.BottomRight.Y),
                    new(rect.BottomRight.X, rect.BottomRight.Y - triangleSide),
                    LegalDestinationsMarkerColor);
            }
            else
            {
                dc.DrawEllipse(
                    LegalDestinationsMarkerColor, null, center, _destinationMarkerRadius, _destinationMarkerRadius);
            }
        }

        dc.Pop();
    }

    private void DrawBoard(DrawingContext dc)
    {
        for (var i = 0; i < 8; i++)
        {
            for (var j = 0; j < 8; j++)
            {
                var rect = _squaresRects[i][j];
                var brush = i % 2 == j % 2 ? LightSquaresColor : DarkSquaresColor;
                dc.DrawRectangle(brush, null, rect);
            }
        }
    }

    private void DrawCoords(DrawingContext dc)
    {
        var fontSize = Math.Max(4, Math.Min(16, _squareSize / 4 - 3));

        for (var file = 0; file < 8; file++)
        {
            var fileChar = IsFlipped
                ? (char)(7 - file + 97)
                : (char)(file + 97);

            var text = new FormattedText(
                    fileChar.ToString(),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    _coordsTypeface,
                    fontSize,
                    file % 2 == 0 ? LightSquaresColor : DarkSquaresColor,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var squareRect = _squaresRects[7][file];
            dc.DrawText(text, new Point(squareRect.Left + 4, squareRect.Bottom - text.Height - 2));
        }

        for (var rank = 0; rank < 8; rank++)
        {
            var rankChar = IsFlipped
                ? (char)(rank + 49)
                : (char)(8 - rank + 48);

            var text = new FormattedText(
                rankChar.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                _coordsTypeface,
                fontSize,
                rank % 2 == 0 ? LightSquaresColor : DarkSquaresColor,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var squareRect = _squaresRects[rank][7];
            dc.DrawText(text, new Point(squareRect.Right - text.Width - 4, squareRect.Top - 1));
        }
    }

    private void DrawSquareHighlights(DrawingContext dc)
    {
        dc.PushOpacity(0.25);

        foreach (var square in HighlightedSquares)
        {
            var rect = GetSquareRect(SquareToIdx(square));
            dc.DrawRectangle(HighlightedSquaresColor, null, rect);
        }

        if (_pieceInHand != null)
        {
            var rect = GetSquareRect(_pieceInHand.From);
            dc.DrawRectangle(LegalDestinationsMarkerColor, null, rect);
        }

        dc.Pop();
    }

    private void DrawPieces(DrawingContext dc)
    {
        foreach (var piece in _pieces)
        {
            var square = GetSquareRect(piece.Square);

            if (piece.Square == _pieceInHand?.Piece.Square)
                dc.PushOpacity(0.3);

            var pieceImg = PieceImages.Images[(piece.Side, piece.Type)];
            var pieceRect = GetPieceRect(pieceImg, square.TopLeft, square.Width, _pieceHeight);
            dc.DrawImage(pieceImg, pieceRect);

            if (piece.Square == _pieceInHand?.Piece.Square)
                dc.Pop();
        }
    }

    private void DrawArrows(DrawingContext dc)
    {
        foreach (var arrow in _arrows)
            dc.DrawGeometry(arrow.Pen.Brush, arrow.Pen, arrow.Geometry);
    }

    private void DrawPieceAnimation(object? _, MoveAnimationPositionChangedEventArgs e)
    {
        var dc = _pieceAnimationDrawing.Open();

        foreach (var p in e.Pieces)
        {
            var pieceRect = GetPieceRect(p.Image, p.TopLeft, _squareSize, _pieceHeight);
            dc.DrawImage(p.Image, pieceRect);
        }

        dc.Close();
    }

    private void CompletePieceAnimation(object? sender, MoveAnimationCompletedEventArgs e)
    {
        if (sender is not PieceMoveAnimation me)
            return;

        me.OnUpdated -= DrawPieceAnimation;
        me.OnCompleted -= CompletePieceAnimation;
        _pieceAnimationDrawing.Children.Clear();
        _pieces = e.FinalPosition;
        _activeAnimation = null;
        InvalidateVisual();
    }

    private void CancelPieceAnimationIfExists()
    {
        if (_activeAnimation == null)
            return;

        _activeAnimation.OnUpdated -= DrawPieceAnimation;
        _activeAnimation.OnCompleted -= CompletePieceAnimation;
        _activeAnimation = null;
        _pieceAnimationDrawing.Children.Clear();
    }

    private PromotionPopup CreatePromotionPopup(string from, string to)
    {
        var promotionSide = to[1] == '8' ? ChessColor.Light : ChessColor.Dark;
        var dropSquareRect = GetSquareRect(SquareToIdx(to));
        var popupWidth = _squareSize * 4;
        var popupLeft = dropSquareRect.X + 8;

        if (popupLeft + popupWidth > ActualWidth)
            popupLeft = ActualWidth - popupWidth - 8;

        var popupTop = promotionSide == ChessColor.Light ? dropSquareRect.Y + 8 : dropSquareRect.Y - 8;
        var popupBg = promotionSide == ChessColor.Light ? DarkSquaresColor : LightSquaresColor;

        return new PromotionPopup(
            ActualWidth,
            ActualHeight,
            from,
            to,
            new(popupLeft, popupTop, popupWidth, _squareSize),
            promotionSide,
            popupBg,
            OnMoveCompletedEvent);
    }

    private static Rect[][] BuildGrid(double squareSize, double gridSize)
    {
        var grid = new Rect[8][];
        var top = 0.0;

        for (var bottomIdx = 1; bottomIdx < 9; bottomIdx++)
        {
            var bottom = Math.Min(gridSize, Math.Round(bottomIdx * squareSize));
            var prevRight = 0.0;
            grid[bottomIdx - 1] = new Rect[8];

            for (var rightIdx = 1; rightIdx < 9; rightIdx++)
            {
                var right = Math.Min(gridSize, Math.Round(squareSize * rightIdx));
                var squareTopLeft = new Point(prevRight, top);
                var squareBottomRight = new Point(right, bottom);
                grid[bottomIdx - 1][rightIdx - 1] = new Rect(squareTopLeft, squareBottomRight);
                prevRight = squareBottomRight.X;
            }

            top = grid[bottomIdx - 1][0].Bottom;
        }

        return grid;
    }

    private List<(StreamGeometry, Pen)> BuildArrows()
    {
        if (Arrows == null)
            return [];

        var arrows = new List<(StreamGeometry, Pen)>();
        var offset = _squareSize * 0.3;
        var cos = 0.866;
        var sin = 0.5;

        foreach (var arrow in Arrows)
        {
            var start = GetSquareCenter(SquareToIdx(arrow.From));
            var end = GetSquareCenter(SquareToIdx(arrow.To));
            var vec = new Vector2((float)(end.X - start.X), (float)(end.Y - start.Y));
            var unit = Vector2.Normalize(vec);
            var startShifted = new Point(start.X + unit.X * offset, start.Y + unit.Y * offset);
            var endShifted = new Point(end.X - unit.X * offset, end.Y - unit.Y * offset);

            var a1x = unit.X * cos - unit.Y * sin;
            var a1y = unit.X * sin + unit.Y * cos;
            var a2x = unit.X * cos + unit.Y * sin;
            var a2y = -unit.X * sin + unit.Y * cos;

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                // Line
                ctx.BeginFigure(startShifted, false, true);
                ctx.LineTo(endShifted, false, false);
                // Triangle arrow tip
                ctx.BeginFigure(endShifted, true, true);
                ctx.LineTo(new Point(endShifted.X - 10 * a1x, endShifted.Y - 10 * a1y), true, false);
                ctx.LineTo(new Point(endShifted.X - 10 * a2x, endShifted.Y - 10 * a2y), true, false);
            }
            geometry.Freeze();

            arrows.Add((geometry, new Pen(new SolidColorBrush(arrow.Color), 3)));
        }

        return arrows;
    }

    internal static Rect GetPieceRect(DrawingImage image, Point squareUpperLeft, double squareSize, double pieceHeight)
    {
        var pieceWidth = image.Width / image.Height * pieceHeight;
        var pieceUpperLeft = new Point(
            squareUpperLeft.X + (squareSize - pieceWidth) / 2,
            squareUpperLeft.Y + (squareSize - pieceHeight) / 2);
        var pieceBottomRight = new Point(pieceUpperLeft.X + pieceWidth, pieceUpperLeft.Y + pieceHeight);
        return new Rect(pieceUpperLeft, pieceBottomRight);
    }

    private static string IdxToSquare(int idx)
    {
        var rank = idx / 8;
        var file = idx % 8;
        return $"{(char)(file + 97)}{(char)(rank + 49)}";
    }

    private static int SquareToIdx(ReadOnlySpan<char> square) => square[0] - 97 + (square[1] - 49) * 8;

    private static int CoordsToSquare(double x, double y, double squareSize, bool isFlipped)
    {
        var row = 7 - Math.Floor(y / squareSize);
        var col = Math.Floor(x / squareSize);

        if (isFlipped)
        {
            row = 7 - row;
            col = 7 - col;
        }

        return (int)row * 8 + (int)col;
    }

    private Rect GetSquareRect(int idx)
    {
        var row = idx / 8;
        var col = idx % 8;
        var displayRow = IsFlipped ? row : 7 - row;
        var displayCol = !IsFlipped ? col : 7 - col;
        return _squaresRects[displayRow][displayCol];
    }

    private Point GetSquareCenter(int idx)
    {
        var rect = GetSquareRect(idx);
        return new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
    }

    private static double GetSquareDistance(int sq1, int sq2)
    {
        var rankDiff = sq1 / 8 - sq2 / 8;
        var fileDiff = sq1 % 8 - sq2 / 8;
        return Math.Sqrt(rankDiff * rankDiff + fileDiff * fileDiff);
    }

    private static bool TryFindClosestPieceOfSameKind(Piece piece, List<Piece> candidates, out Piece closest)
    {
        var sameKind = candidates.Where(c => c.Type == piece.Type && c.Side == piece.Side).ToList();

        if (sameKind.Count == 0)
        {
            closest = default;
            return false;
        }

        closest = sameKind.MinBy(c => GetSquareDistance(c.Square, piece.Square));
        return true;
    }

    private void OnLegalMoveDestinationsChanged(object? sender, NotifyCollectionChangedEventArgs? _)
    {
        _moveDestinations = LegalMoveDestinations.Select(d => SquareToIdx(d)).ToList();
        InvalidateVisual();
    }

    private void OnArrowsChanged(object? sender, NotifyCollectionChangedEventArgs? _)
    {
        _arrows = BuildArrows();
        InvalidateVisual();
    }

    private void OnHighlightedSquaresChanged(object? sender, NotifyCollectionChangedEventArgs? _) => InvalidateVisual();

    private static List<Piece> GetPiecesFromFen(string fen)
    {
        if (string.IsNullOrEmpty(fen))
            return [];

        var piecesPart = fen.Split(' ')[0];
        var ranksParts = piecesPart.Split("/");

        if (ranksParts.Length != 8)
            throw new ArgumentException($"Invalid FEN: {fen}");

        var pieces = new List<Piece>(32);

        for (var rank = 0; rank < 8; rank++)
        {
            var rankPart = ranksParts[7 - rank];
            var file = 0;

            foreach (var pieceChar in rankPart)
            {
                if (char.IsDigit(pieceChar))
                {
                    // -48 to convert from char to int, then +1 to account for file offset
                    file += pieceChar - 48;
                    continue;
                }

                var pType = pieceChar switch
                {
                    'p' or 'P' => PieceType.Pawn,
                    'n' or 'N' => PieceType.Knight,
                    'b' or 'B' => PieceType.Bishop,
                    'r' or 'R' => PieceType.Rook,
                    'q' or 'Q' => PieceType.Queen,
                    'k' or 'K' => PieceType.King,
                    _ => throw new ArgumentException($"Invalid piece in the FEN string: '{pieceChar}'")
                };

                var pColor = char.IsUpper(pieceChar) ? ChessColor.Light : ChessColor.Dark;
                pieces.Add(new Piece
                {
                    Side = pColor,
                    Type = pType,
                    Square = rank * 8 + file
                });

                file++;
            }
        }

        return pieces;
    }

    private static void DrawTriangle(DrawingContext dc, Point p0, Point p1, Point p2, Brush brush)
    {
        var g = new StreamGeometry();
        using var ctx = g.Open();
        ctx.BeginFigure(p0, true, true);
        PointCollection points = [p1, p2];
        ctx.PolyLineTo(points, false, false);
        g.Freeze();
        dc.DrawGeometry(brush, null, g);
    }

    private static void OnLegalMoveDestinationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfChessboard cb)
            return;

        if (e.OldValue is INotifyCollectionChanged oldDests)
            oldDests.CollectionChanged -= cb.OnLegalMoveDestinationsChanged;

        if (e.NewValue is INotifyCollectionChanged newDests)
            newDests.CollectionChanged += cb.OnLegalMoveDestinationsChanged;

        cb.OnLegalMoveDestinationsChanged(cb, null);
    }

    private static void OnPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfChessboard cb)
            return;

        if (e.NewValue is not string pos || string.IsNullOrEmpty(pos))
            throw new ArgumentNullException("Position can't be empty");

        if (!cb.Animate)
        {
            cb._pieces = GetPiecesFromFen(pos);
            cb._lastPlayerMove = (-1, -1);
            return;
        }

        var originalNewPieces = GetPiecesFromFen(pos);
        var allNewPieces = originalNewPieces.ToDictionary(p => p.Square, p => p);
        var allOldPieces = cb._pieces.ToDictionary(p => p.Square, p => p);
        var staticPieces = new List<Piece>(allNewPieces.Count);
        var removedPieces = new List<Piece>();
        var newPieces = new List<Piece>();

        for (var square = 0; square < 64; square++)
        {
            if (allNewPieces.TryGetValue(square, out var newPiece))
            {
                if (allOldPieces.TryGetValue(square, out var oldPiece))
                {
                    if (newPiece == oldPiece)
                    {
                        staticPieces.Add(newPiece);
                    }
                    else
                    {
                        // Draw the old piece until the capture animation ends, then the animation itself
                        // will replace it with the final position
                        removedPieces.Add(oldPiece);
                        newPieces.Add(newPiece);
                    }
                }
                else
                {
                    newPieces.Add(newPiece);
                }
            }
            else if (allOldPieces.TryGetValue(square, out var oldPiece))
            {
                removedPieces.Add(oldPiece);
            }
        }

        var animations = new List<(ChessColor Side, PieceType Type, Point From, Point To)>();
        var animationTargets = new HashSet<int>();

        // Do not build animations when board wasn't drawn yet
        if (cb._squaresRects[0] != null)
        {
            foreach (var moved in newPieces)
            {
                if (TryFindClosestPieceOfSameKind(moved, removedPieces, out var piece))
                {
                    // Do not animate manual moves made by dragging the piece
                    if (piece.Square == cb._lastPlayerMove.From && moved.Square == cb._lastPlayerMove.To)
                    {
                        staticPieces.Add(moved);
                    }
                    else
                    {
                        animations.Add(
                            (
                                piece.Side,
                                piece.Type,
                                cb.GetSquareRect(piece.Square).TopLeft,
                                cb.GetSquareRect(moved.Square).TopLeft
                            )
                        );
                        animationTargets.Add(moved.Square);
                    }
                }
                else
                {
                    staticPieces.Add(moved);
                }
            }
        }

        var captured = removedPieces.Where(rp => animationTargets.Contains(rp.Square));
        staticPieces.AddRange(captured);

        cb._pieces = staticPieces;
        var animation = new PieceMoveAnimation(animations, originalNewPieces);
        cb.CancelPieceAnimationIfExists();
        animation.OnUpdated += cb.DrawPieceAnimation;
        animation.OnCompleted += cb.CompletePieceAnimation;
        cb._activeAnimation = animation;
        animation.Start();

        cb._lastPlayerMove = (-1, -1);
    }

    private static void OnArrowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfChessboard cb)
            return;

        if (e.OldValue is INotifyCollectionChanged oldArrows)
            oldArrows.CollectionChanged -= cb.OnArrowsChanged;

        if (e.NewValue is INotifyCollectionChanged newArrows)
            newArrows.CollectionChanged += cb.OnArrowsChanged;

        cb.OnArrowsChanged(cb, null);
    }

    private static void OnHighlightedSquaresChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfChessboard cb)
            return;

        if (e.OldValue is INotifyCollectionChanged oldSquares)
            oldSquares.CollectionChanged -= cb.OnHighlightedSquaresChanged;

        if (e.NewValue is INotifyCollectionChanged newSquares)
            newSquares.CollectionChanged += cb.OnHighlightedSquaresChanged;

        cb.OnHighlightedSquaresChanged(cb, null);
    }

    private static void OnFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not WpfChessboard cb || e.NewValue is not FontFamily fm)
            return;

        cb._coordsTypeface = new Typeface(fm, FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    }
}
