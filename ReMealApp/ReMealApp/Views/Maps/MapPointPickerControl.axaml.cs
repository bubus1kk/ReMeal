using Application.DTOs.Maps;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Globalization;
using System.Net.Http.Headers;

namespace ReMealApp.Views.Maps
{
    public partial class MapPointPickerControl : UserControl
    {
        private const int Zoom = 16;
        private const int TileSize = 256;
        private const int TileBuffer = 1;
        private const double WebMercatorMaxLatitude = 85.05112878;
        private static readonly HttpClient TileHttpClient = CreateTileHttpClient();

        private readonly Ellipse _marker;
        private double _selectedLatitude;
        private double _selectedLongitude;
        private double _topLeftPixelX;
        private double _topLeftPixelY;
        private readonly Dictionary<TileKey, TileVisual> _tileVisuals = new();
        private bool _isPointerPressed;
        private bool _isDragging;
        private Point _dragStartPosition;
        private double _dragStartTopLeftPixelX;
        private double _dragStartTopLeftPixelY;
        private int _tileLoadVersion;

        public MapPointPickerControl()
        {
            InitializeComponent();

            _marker = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = SolidColorBrush.Parse("#69CB4B"),
                Stroke = SolidColorBrush.Parse("#F8FFF4"),
                StrokeThickness = 3,
                IsHitTestVisible = false
            };
        }

        public event EventHandler<CoordinatesDto>? CoordinatesApplied;

        public event EventHandler? Cancelled;

        public async Task LoadLocationAsync(double latitude, double longitude)
        {
            _selectedLatitude = latitude;
            _selectedLongitude = longitude;
            CenterViewportOnSelectedCoordinates();
            UpdateCoordinatesText();
            await LoadTilesForCurrentViewportAsync(resetTiles: true);
        }

        private async Task LoadTilesForCurrentViewportAsync(bool resetTiles)
        {
            var loadVersion = ++_tileLoadVersion;
            var viewportTopLeftPixelX = _topLeftPixelX;
            var viewportTopLeftPixelY = _topLeftPixelY;

            if (resetTiles)
            {
                TileCanvas.Children.Clear();
                _tileVisuals.Clear();
            }

            MapStatusText.Text = _tileVisuals.Count == 0 ? "Загрузка карты..." : string.Empty;
            MapStatusText.IsVisible = _tileVisuals.Count == 0;

            var firstTileX = (int)Math.Floor(viewportTopLeftPixelX / TileSize) - TileBuffer;
            var firstTileY = (int)Math.Floor(viewportTopLeftPixelY / TileSize) - TileBuffer;
            var lastTileX = (int)Math.Floor((viewportTopLeftPixelX + TileCanvas.Width) / TileSize) + TileBuffer;
            var lastTileY = (int)Math.Floor((viewportTopLeftPixelY + TileCanvas.Height) / TileSize) + TileBuffer;
            var requiredTiles = new HashSet<TileKey>();
            var loadedTileVisuals = new List<(TileKey Key, TileVisual Visual)>();

            var availableTiles = 0;
            for (var tileY = firstTileY; tileY <= lastTileY; tileY++)
            {
                for (var tileX = firstTileX; tileX <= lastTileX; tileX++)
                {
                    var key = new TileKey(tileX, tileY);
                    requiredTiles.Add(key);

                    if (_tileVisuals.ContainsKey(key))
                    {
                        availableTiles++;
                        continue;
                    }

                    var image = await CreateTileImageAsync(tileX, tileY);
                    if (loadVersion != _tileLoadVersion)
                        return;

                    if (image is null)
                        continue;

                    loadedTileVisuals.Add((key, new TileVisual(image, tileX, tileY)));
                    availableTiles++;
                }
            }

            if (loadVersion != _tileLoadVersion)
                return;

            foreach (var staleTile in _tileVisuals.Keys.Except(requiredTiles).ToArray())
            {
                TileCanvas.Children.Remove(_tileVisuals[staleTile].Image);
                _tileVisuals.Remove(staleTile);
            }

            foreach (var (key, visual) in loadedTileVisuals)
            {
                if (_tileVisuals.ContainsKey(key))
                    continue;

                _tileVisuals[key] = visual;
                TileCanvas.Children.Add(visual.Image);
            }

            UpdateTilePositions();
            BringMarkerToFront();
            MoveMarkerToSelectedCoordinates();
            MapStatusText.IsVisible = availableTiles == 0;
            MapStatusText.Text = availableTiles == 0
                ? "Не удалось загрузить карту. Проверьте подключение к интернету."
                : string.Empty;
        }

        private static async Task<Image?> CreateTileImageAsync(int tileX, int tileY)
        {
            var tileCount = 1 << Zoom;
            if (tileY < 0 || tileY >= tileCount)
                return null;

            var wrappedTileX = ((tileX % tileCount) + tileCount) % tileCount;
            var uri = new Uri(
                FormattableString.Invariant(
                    $"https://tile.openstreetmap.org/{Zoom}/{wrappedTileX}/{tileY}.png"));

            try
            {
                using var response = await TileHttpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync();
                var bitmap = new Bitmap(stream);

                return new Image
                {
                    Source = bitmap,
                    Width = TileSize,
                    Height = TileSize,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                };
            }
            catch
            {
                return null;
            }
        }

        private void TileCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            var position = e.GetPosition(TileCanvas);
            if (position.X < 0 ||
                position.Y < 0 ||
                position.X > TileCanvas.Width ||
                position.Y > TileCanvas.Height)
            {
                return;
            }

            _isPointerPressed = true;
            _isDragging = false;
            _dragStartPosition = position;
            _dragStartTopLeftPixelX = _topLeftPixelX;
            _dragStartTopLeftPixelY = _topLeftPixelY;
            _tileLoadVersion++;
            e.Pointer.Capture(TileCanvas);
            e.Handled = true;
        }

        private void TileCanvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (!_isPointerPressed)
                return;

            var position = e.GetPosition(TileCanvas);
            var deltaX = position.X - _dragStartPosition.X;
            var deltaY = position.Y - _dragStartPosition.Y;

            if (!_isDragging && Math.Sqrt(deltaX * deltaX + deltaY * deltaY) < 4)
                return;

            _isDragging = true;
            _topLeftPixelX = _dragStartTopLeftPixelX - deltaX;
            _topLeftPixelY = _dragStartTopLeftPixelY - deltaY;
            UpdateTilePositions();
            MoveMarkerToSelectedCoordinates();
            e.Handled = true;
        }

        private async void TileCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (!_isPointerPressed)
                return;

            _isPointerPressed = false;
            e.Pointer.Capture(null);

            if (_isDragging)
            {
                _isDragging = false;
                await LoadTilesForCurrentViewportAsync(resetTiles: false);
                e.Handled = true;
                return;
            }

            SelectCoordinatesAt(e.GetPosition(TileCanvas));
            e.Handled = true;
        }

        private void SelectCoordinatesAt(Point position)
        {
            if (position.X < 0 ||
                position.Y < 0 ||
                position.X > TileCanvas.Width ||
                position.Y > TileCanvas.Height)
            {
                return;
            }

            var geo = ToLatitudeLongitude(
                _topLeftPixelX + position.X,
                _topLeftPixelY + position.Y);

            _selectedLatitude = geo.Latitude;
            _selectedLongitude = geo.Longitude;
            UpdateCoordinatesText();
            MoveMarkerToSelectedCoordinates();
        }

        private void UpdateTilePositions()
        {
            foreach (var tile in _tileVisuals.Values)
            {
                Canvas.SetLeft(tile.Image, tile.TileX * TileSize - _topLeftPixelX);
                Canvas.SetTop(tile.Image, tile.TileY * TileSize - _topLeftPixelY);
            }
        }

        private void BringMarkerToFront()
        {
            if (TileCanvas.Children.Contains(_marker))
                TileCanvas.Children.Remove(_marker);

            TileCanvas.Children.Add(_marker);
        }

        private void CenterViewportOnSelectedCoordinates()
        {
            var centerPixel = ToGlobalPixel(_selectedLatitude, _selectedLongitude);
            _topLeftPixelX = centerPixel.X - TileCanvas.Width / 2;
            _topLeftPixelY = centerPixel.Y - TileCanvas.Height / 2;
        }

        private void MoveMarkerToSelectedCoordinates()
        {
            var pixel = ToGlobalPixel(_selectedLatitude, _selectedLongitude);
            Canvas.SetLeft(_marker, pixel.X - _topLeftPixelX - _marker.Width / 2);
            Canvas.SetTop(_marker, pixel.Y - _topLeftPixelY - _marker.Height / 2);
        }

        private void UpdateCoordinatesText()
        {
            CoordinatesText.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"Latitude: {_selectedLatitude:F6}; Longitude: {_selectedLongitude:F6}");
        }

        private static (double X, double Y) ToGlobalPixel(double latitude, double longitude)
        {
            var clampedLatitude = Math.Clamp(latitude, -WebMercatorMaxLatitude, WebMercatorMaxLatitude);
            var latitudeRadians = DegreesToRadians(clampedLatitude);
            var tileCount = 1 << Zoom;

            var x = (longitude + 180) / 360 * tileCount * TileSize;
            var y = (1 - Math.Log(Math.Tan(latitudeRadians) + 1 / Math.Cos(latitudeRadians)) / Math.PI)
                / 2 * tileCount * TileSize;

            return (x, y);
        }

        private static CoordinatesDto ToLatitudeLongitude(double pixelX, double pixelY)
        {
            var mapSize = (1 << Zoom) * TileSize;
            var longitude = pixelX / mapSize * 360 - 180;
            var latitudeRadians = Math.Atan(Math.Sinh(Math.PI * (1 - 2 * pixelY / mapSize)));
            var latitude = RadiansToDegrees(latitudeRadians);

            return new CoordinatesDto(latitude, longitude);
        }

        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;

        private static double RadiansToDegrees(double radians) => radians * 180 / Math.PI;

        private static HttpClient CreateTileHttpClient()
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("ReMealApp", "1.0"));
            httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("(educational Avalonia desktop application)"));

            return httpClient;
        }

        private void Apply_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            CoordinatesApplied?.Invoke(
                this,
                new CoordinatesDto(_selectedLatitude, _selectedLongitude));
        }

        private void Cancel_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
        }

        private sealed record TileKey(int TileX, int TileY);

        private sealed record TileVisual(Image Image, int TileX, int TileY);
    }
}
