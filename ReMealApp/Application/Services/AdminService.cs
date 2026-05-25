using Application.DTOs.Admin;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;

namespace Application.Services
{
    public sealed class AdminService : IAdminService
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IFoodPointRepository _foodPointRepository;
        private readonly IFoodLotRepository _foodLotRepository;
        private readonly IBookingRepository _bookingRepository;

        public AdminService(
            IAuthService authService,
            IUserRepository userRepository,
            IFoodPointRepository foodPointRepository,
            IFoodLotRepository foodLotRepository,
            IBookingRepository bookingRepository)
        {
            _authService = authService;
            _userRepository = userRepository;
            _foodPointRepository = foodPointRepository;
            _foodLotRepository = foodLotRepository;
            _bookingRepository = bookingRepository;
        }

        public async Task EnsureAdministratorAccessAsync(CancellationToken cancellationToken = default)
        {
            var currentUser = await _authService.GetCurrentUserAsync(cancellationToken)
                ?? throw new UnauthorizedAccessException("Пользователь не авторизован.");

            if (!currentUser.IsActive)
                throw new UnauthorizedAccessException("Административный модуль недоступен деактивированному пользователю.");

            if (currentUser.Role != UserRole.Administrator)
                throw new UnauthorizedAccessException("Административный модуль доступен только администратору.");
        }

        public async Task<List<AdminUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var users = await _userRepository.GetAllAsync(cancellationToken);
            return users
                .Select(MapUser)
                .ToList();
        }

        private static AdminUserDto MapUser(User user)
        {
            return new AdminUserDto
            {
                Id = user.Id,
                Login = user.Login,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                IsActive = user.IsActive
            };
        }

        public async Task ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var user = await GetUserOrThrowAsync(userId, cancellationToken);
            user.IsActive = true;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            if (_authService.CurrentUserId == userId)
                throw new InvalidOperationException("Нельзя деактивировать текущую учетную запись администратора.");

            var user = await GetUserOrThrowAsync(userId, cancellationToken);
            user.IsActive = false;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task<User> GetUserOrThrowAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _userRepository.GetByIdAsync(userId, cancellationToken)
                ?? throw new KeyNotFoundException($"Пользователь '{userId}' не найден.");
        }

        public async Task<List<AdminFoodPointDto>> GetFoodPointsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoints = await _foodPointRepository.GetAllAsync(cancellationToken);
            return foodPoints
                .Select(MapFoodPoint)
                .ToList();
        }

        public async Task ActivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoint = await GetFoodPointOrThrowAsync(foodPointId, cancellationToken);
            foodPoint.Activate();

            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task DeactivateFoodPointAsync(Guid foodPointId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var foodPoint = await GetFoodPointOrThrowAsync(foodPointId, cancellationToken);
            foodPoint.Deactivate();

            await _foodPointRepository.UpdateAsync(foodPoint, cancellationToken);
            await _foodPointRepository.SaveChangesAsync(cancellationToken);
        }

        private async Task<FoodPoint> GetFoodPointOrThrowAsync(Guid foodPointId, CancellationToken cancellationToken)
        {
            return await _foodPointRepository.GetByIdAsync(foodPointId, cancellationToken)
                ?? throw new KeyNotFoundException($"Точка питания '{foodPointId}' не найдена.");
        }

        private static AdminFoodPointDto MapFoodPoint(FoodPoint foodPoint)
        {
            return new AdminFoodPointDto
            {
                Id = foodPoint.Id,
                Name = foodPoint.Name,
                Address = foodPoint.Address,
                Phone = foodPoint.Phone,
                OwnerName = string.IsNullOrWhiteSpace(foodPoint.Owner?.FullName)
                    ? "Не указан"
                    : foodPoint.Owner.FullName,
                OwnerLogin = string.IsNullOrWhiteSpace(foodPoint.Owner?.Login)
                    ? string.Empty
                    : foodPoint.Owner.Login,
                IsActive = foodPoint.IsActive
            };
        }

        public async Task<List<AdminLotDto>> GetLotsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var lots = await _foodLotRepository.GetAllAsync(cancellationToken);
            return lots
                .Select(MapLot)
                .ToList();
        }

        public async Task CancelLotAsync(Guid lotId, CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var lot = await _foodLotRepository.GetByIdAsync(lotId, cancellationToken)
                ?? throw new KeyNotFoundException($"Лот '{lotId}' не найден.");

            lot.Cancel();

            await _foodLotRepository.UpdateAsync(lot, cancellationToken);
            await _foodLotRepository.SaveChangesAsync(cancellationToken);
        }

        private static AdminLotDto MapLot(FoodLot lot)
        {
            return new AdminLotDto
            {
                Id = lot.Id,
                Title = lot.Title,
                FoodPointName = lot.FoodPoint?.Name ?? "Не указана",
                OwnerName = string.IsNullOrWhiteSpace(lot.FoodPoint?.Owner?.FullName)
                    ? "Не указан"
                    : lot.FoodPoint.Owner.FullName,
                Price = lot.Price,
                TotalQuantity = lot.TotalQuantity,
                AvailableQuantity = lot.AvailableQuantity,
                PickupDeadline = lot.PickupDeadline,
                Status = lot.Status
            };
        }

        public async Task<List<AdminBookingDto>> GetBookingsAsync(CancellationToken cancellationToken = default)
        {
            await EnsureAdministratorAccessAsync(cancellationToken);

            var bookings = await _bookingRepository.GetAllAsync(cancellationToken);
            return bookings
                .Select(MapBooking)
                .ToList();
        }

        private static AdminBookingDto MapBooking(Booking booking)
        {
            return new AdminBookingDto
            {
                Id = booking.Id,
                CustomerName = booking.User?.FullName ?? "Не указан",
                LotTitle = booking.FoodLot?.Title ?? "Не указан",
                FoodPointName = booking.FoodLot?.FoodPoint?.Name ?? "Не указана",
                Quantity = booking.Quantity,
                PriceAtReservation = booking.PriceAtReservation,
                ReservedAt = booking.ReservedAt,
                Status = booking.Status
            };
        }
    }
}
