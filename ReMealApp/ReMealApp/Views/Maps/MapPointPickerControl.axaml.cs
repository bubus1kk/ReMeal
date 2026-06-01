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
        private const int MinZoom = 13;
        private const int MaxZoom = 18;
        private const int TileSize = 256;
        private const int TileBuffer = 1;
        private const int VisibleTileBuffer = 0;
        private const int MaxConcurrentTileRequests = 6;
        private const int ZoomDebounceMilliseconds = 120;
        private const double WebMercatorMaxLatitude = 85.05112878;
        private static readonly HttpClient TileHttpClient = CreateTileHttpClient();
        private static readonly SemaphoreSlim TileRequestGate = new(MaxConcurrentTileRequests, MaxConcurrentTileRequests);
        private static readonly object TileCacheLock = new();
        private static readonly Dictionary<TileCacheKey, Bitmap> TileBitmapCache = new();

        private readonly Ellipse _marker;
        private int _zoom = Zoom;
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
        private CancellationTokenSource? _zoomDebounceCancellation;

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

        public event EventHandler<CoordinatesDto>? CoordinatesSelected;

        public event EventHandler? Cancelled;

        public async Task LoadLocationAsync(double latitude, double longitude)
        {
            _zoom = Zoom;
            _selectedLatitude = latitude;
            _selectedLongitude = longitude;
            CenterViewportOnSelectedCoordinates();
            UpdateCoordinatesText();
            await LoadVisibleThenBufferedTilesAsync(resetTiles: true);
        }

        private async Task LoadVisibleThenBufferedTilesAsync(bool resetTiles)
        {
            await LoadTilesForCurrentViewportAsync(resetTiles, VisibleTileBuffer);

            if (_isPointerPressed)
                return;

            await LoadTilesForCurrentViewportAsync(resetTiles: false, TileBuffer);
        }

        private async Task LoadTilesForCurrentViewportAsync(bool resetTiles, int tileBuffer)
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

            var firstTileX = (int)Math.Floor(viewportTopLeftPixelX / TileSize) - tileBuffer;
            var firstTileY = (int)Math.Floor(viewportTopLeftPixelY / TileSize) - tileBuffer;
            var lastTileX = (int)Math.Floor((viewportTopLeftPixelX + TileCanvas.Width) / TileSize) + tileBuffer;
            var lastTileY = (int)Math.Floor((viewportTopLeftPixelY + TileCanvas.Height) / TileSize) + tileBuffer;
            var requiredTiles = new HashSet<TileKey>();
            var tileLoadTasks = new List<Task<(TileKey Key, TileVisual? Visual)>>();

            var availableTiles = 0;
            for (var tileY = firstTileY; tileY <= lastTileY; tileY++)
            {
                for (var tileX = firstTileX; tileX <= lastTileX; tileX++)
                {
                    var key = new TileKey(_zoom, tileX, tileY);
                    requiredTiles.Add(key);

                    if (_tileVisuals.ContainsKey(key))
                    {
                        availableTiles++;
                        continue;
                    }

                    tileLoadTasks.Add(LoadTileVisualAsync(_zoom, tileX, tileY));
                }
            }

            var loadedTileVisuals = await Task.WhenAll(tileLoadTasks);
            if (loadVersion != _tileLoadVersion)
                return;

            foreach (var staleTile in _tileVisuals.Keys.Except(requiredTiles).ToArray())
            {
                TileCanvas.Children.Remove(_tileVisuals[staleTile].Image);
                _tileVisuals.Remove(staleTile);
            }

            foreach (var (key, visual) in loadedTileVisuals)
            {
                if (visual is null || _tileVisuals.ContainsKey(key))
                    continue;

                _tileVisuals[key] = visual;
                TileCanvas.Children.Add(visual.Image);
                availableTiles++;
            }

            UpdateTilePositions();
            BringMarkerToFront();
            MoveMarkerToSelectedCoordinates();
            MapStatusText.IsVisible = availableTiles == 0;
            MapStatusText.Text = availableTiles == 0
                ? "Не удалось загрузить карту. Проверьте подключение к интернету."
                : string.Empty;
        }

        private static async Task<(TileKey Key, TileVisual? Visual)> LoadTileVisualAsync(
            int zoom,
            int tileX,
            int tileY)
        {
            var key = new TileKey(zoom, tileX, tileY);
            var bitmap = await GetTileBitmapAsync(zoom, tileX, tileY);
            if (bitmap is null)
                return (key, null);

            var image = new Image
            {
                Source = bitmap,
                Width = TileSize,
                Height = TileSize,
                Stretch = Stretch.Fill,
                IsHitTestVisible = false
            };

            return (key, new TileVisual(image, zoom, tileX, tileY));
        }

        private static async Task<Bitmap?> GetTileBitmapAsync(int zoom, int tileX, int tileY)
        {
            var tileCount = 1 << zoom;
            if (tileY < 0 || tileY >= tileCount)
                return null;

            var wrappedTileX = ((tileX % tileCount) + tileCount) % tileCount;
            var cacheKey = new TileCacheKey(zoom, wrappedTileX, tileY);

            lock (TileCacheLock)
            {
                if (TileBitmapCache.TryGetValue(cacheKey, out var cachedBitmap))
                    return cachedBitmap;
            }

            await TileRequestGate.WaitAsync();
            try
            {
                lock (TileCacheLock)
                {
                    if (TileBitmapCache.TryGetValue(cacheKey, out var cachedBitmap))
                        return cachedBitmap;
                }

                return await DownloadTileBitmapAsync(cacheKey);
            }
            finally
            {
                TileRequestGate.Release();
            }
        }

        private static async Task<Bitmap?> DownloadTileBitmapAsync(TileCacheKey cacheKey)
        {
            var uri = new Uri(
                FormattableString.Invariant(
                    $"https://tile.openstreetmap.org/{cacheKey.Zoom}/{cacheKey.TileX}/{cacheKey.TileY}.png"));

            try
            {
                using var response = await TileHttpClient.GetAsync(uri);
                if (!response.IsSuccessStatusCode)
                    return null;

                await using var stream = await response.Content.ReadAsStreamAsync();
                var bitmap = new Bitmap(stream);

                lock (TileCacheLock)
                {
                    TileBitmapCache[cacheKey] = bitmap;
                }

                return bitmap;
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
            _zoomDebounceCancellation?.Cancel();
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
                await LoadVisibleThenBufferedTilesAsync(resetTiles: false);
                e.Handled = true;
                return;
            }

            SelectCoordinatesAt(e.GetPosition(TileCanvas));
            e.Handled = true;
        }

        private async void TileCanvas_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
        {
            var zoomDelta = e.Delta.Y > 0 ? 1 : -1;
            var newZoom = Math.Clamp(_zoom + zoomDelta, MinZoom, MaxZoom);
            if (newZoom == _zoom)
                return;

            var position = e.GetPosition(TileCanvas);
            if (position.X < 0 ||
                position.Y < 0 ||
                position.X > TileCanvas.Width ||
                position.Y > TileCanvas.Height)
            {
                position = new Point(TileCanvas.Width / 2, TileCanvas.Height / 2);
            }

            var anchor = ToLatitudeLongitude(
                _topLeftPixelX + position.X,
                _topLeftPixelY + position.Y,
                _zoom);

            _zoom = newZoom;
            var anchorPixel = ToGlobalPixel(anchor.Latitude, anchor.Longitude, _zoom);
            _topLeftPixelX = anchorPixel.X - position.X;
            _topLeftPixelY = anchorPixel.Y - position.Y;
            _tileLoadVersion++;

            e.Handled = true;
            _zoomDebounceCancellation?.Cancel();
            _zoomDebounceCancellation = new CancellationTokenSource();
            _ = LoadZoomTilesAfterDelayAsync(_zoomDebounceCancellation.Token);
        }

        private async Task LoadZoomTilesAfterDelayAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(ZoomDebounceMilliseconds, cancellationToken);
                await LoadVisibleThenBufferedTilesAsync(resetTiles: false);
            }
            catch (OperationCanceledException)
            {
            }
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
                _topLeftPixelY + position.Y,
                _zoom);

            _selectedLatitude = geo.Latitude;
            _selectedLongitude = geo.Longitude;
            UpdateCoordinatesText();
            MoveMarkerToSelectedCoordinates();
            CoordinatesSelected?.Invoke(this, geo);
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
            var centerPixel = ToGlobalPixel(_selectedLatitude, _selectedLongitude, _zoom);
            _topLeftPixelX = centerPixel.X - TileCanvas.Width / 2;
            _topLeftPixelY = centerPixel.Y - TileCanvas.Height / 2;
        }

        private void MoveMarkerToSelectedCoordinates()
        {
            var pixel = ToGlobalPixel(_selectedLatitude, _selectedLongitude, _zoom);
            Canvas.SetLeft(_marker, pixel.X - _topLeftPixelX - _marker.Width / 2);
            Canvas.SetTop(_marker, pixel.Y - _topLeftPixelY - _marker.Height / 2);
        }

        private void UpdateCoordinatesText()
        {
            CoordinatesText.Text = string.Create(
                CultureInfo.InvariantCulture,
                $"Широта: {_selectedLatitude:F6}; Долгота: {_selectedLongitude:F6}");
        }

        private static (double X, double Y) ToGlobalPixel(double latitude, double longitude, int zoom)
        {
            var clampedLatitude = Math.Clamp(latitude, -WebMercatorMaxLatitude, WebMercatorMaxLatitude);
            var latitudeRadians = DegreesToRadians(clampedLatitude);
            var tileCount = 1 << zoom;

            var x = (longitude + 180) / 360 * tileCount * TileSize;
            var y = (1 - Math.Log(Math.Tan(latitudeRadians) + 1 / Math.Cos(latitudeRadians)) / Math.PI)
                / 2 * tileCount * TileSize;

            return (x, y);
        }

        private static CoordinatesDto ToLatitudeLongitude(double pixelX, double pixelY, int zoom)
        {
            var mapSize = (1 << zoom) * TileSize;
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

        private sealed record TileCacheKey(int Zoom, int TileX, int TileY);

        private sealed record TileKey(int Zoom, int TileX, int TileY);

        private sealed record TileVisual(Image Image, int Zoom, int TileX, int TileY);
    }
}
