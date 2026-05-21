using Application.DTOs.Lots;
using Domain.Entities;
using Domain.Enums;
using Tests.TestSupport;

namespace Tests.Lots
{
    [TestClass]
    public sealed class FoodLotServiceTests
    {
        [TestMethod]
        public async Task CreateLotAsync_WhenPartnerHasActiveFoodPoint_CreatesLotForCurrentPartnerPoint()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            var lot = await database.LotService.CreateLotAsync(
                new CreateLotRequest(
                    foodPoint.Id,
                    "Lunch set",
                    "Fresh lunch",
                    string.Empty,
                    7,
                    149.50m,
                    DateTime.UtcNow.AddHours(4),
                    new[]
                    {
                        new LotComponentRequest(null, "Soup", 1, "мл", null, null, 0),
                        new LotComponentRequest(null, "Salad", 2, "шт", "greens", null, 1)
                    }));

            Assert.AreEqual(foodPoint.Id, lot.FoodPointId);
            Assert.AreEqual(7, lot.TotalQuantity);
            Assert.AreEqual(7, lot.AvailableQuantity);
            Assert.AreEqual(149.50m, lot.Price);
            Assert.AreEqual(LotStatus.Active, lot.Status);
            Assert.AreEqual(2, lot.Components.Count);
            Assert.AreEqual("Soup — 1 мл", lot.Components[0].DisplayText);
            Assert.AreEqual("Salad — 2 шт", lot.Components[1].DisplayText);
            Assert.AreEqual("Soup — 1 мл; Salad — 2 шт", lot.Composition);
        }

        [TestMethod]
        public async Task CreateLotAsync_WhenFoodPointDoesNotExist_ThrowsKeyNotFoundException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<KeyNotFoundException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(Guid.NewGuid(), "Lunch", "Description", "Composition", 1, 10, DateTime.UtcNow.AddHours(1))));
        }

        [TestMethod]
        public async Task CreateLotAsync_WhenCurrentUserIsCustomer_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(Guid.NewGuid(), "Lunch", "Description", "Composition", 1, 10, DateTime.UtcNow.AddHours(1))));
        }

        [TestMethod]
        public async Task CreateLotAsync_WhenFoodPointBelongsToOtherPartner_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var owner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(owner);
            database.Auth.SetCurrentUser(otherPartner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 1, 10, DateTime.UtcNow.AddHours(1))));
        }

        [TestMethod]
        public async Task CreateLotAsync_WhenFoodPointIsInactive_ThrowsInvalidOperationException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            foodPoint.Deactivate();
            await database.FoodPointRepository.UpdateAsync(foodPoint);
            await database.FoodPointRepository.SaveChangesAsync();
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<InvalidOperationException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 1, 10, DateTime.UtcNow.AddHours(1))));
        }

        [TestMethod]
        public async Task CreateLotAsync_WhenRequiredValuesAreInvalid_ThrowsArgumentException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "", "Description", "Composition", 1, 10, DateTime.UtcNow.AddHours(1))));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 0, 10, DateTime.UtcNow.AddHours(1))));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 1, 0, DateTime.UtcNow.AddHours(1))));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 1, -1, DateTime.UtcNow.AddHours(1))));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.LotService.CreateLotAsync(
                    new CreateLotRequest(foodPoint.Id, "Lunch", "Description", "Composition", 1, 10, DateTime.UtcNow.AddMinutes(-1))));
        }

        [TestMethod]
        public async Task GetCurrentPartnerLotsAsync_ReturnsOnlyCurrentPartnerLots()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var firstPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var secondPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var firstPoint = await database.AddFoodPointAsync(firstPartner, "First cafe");
            var firstBranch = await database.AddFoodPointAsync(firstPartner, "First cafe branch");
            var secondPoint = await database.AddFoodPointAsync(secondPartner, "Second cafe");
            var ownLot = await database.AddLotAsync(firstPoint, "Own lot");
            var branchLot = await database.AddLotAsync(firstBranch, "Branch lot");
            await database.AddLotAsync(secondPoint, "Other lot");
            database.Auth.SetCurrentUser(firstPartner);

            var lots = await database.LotService.GetCurrentPartnerLotsAsync();
            var ids = lots.Select(x => x.Id).ToArray();

            CollectionAssert.Contains(ids, ownLot.Id);
            CollectionAssert.Contains(ids, branchLot.Id);
            CollectionAssert.AreEquivalent(new[] { ownLot.Id, branchLot.Id }, ids);
        }

        [TestMethod]
        public async Task GetAllLotsAsync_ReturnsLotsWithFoodPointData()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner, "All lots cafe");
            var lot = await database.AddLotAsync(foodPoint, "Visible lot");

            var lots = await database.LotService.GetAllLotsAsync();
            var result = lots.Single(x => x.Id == lot.Id);

            Assert.IsNotNull(result.FoodPoint);
            Assert.AreEqual("All lots cafe", result.FoodPoint!.Name);
            Assert.IsNotNull(result.FoodPoint.Owner);
            Assert.AreEqual(partner.Id, result.FoodPoint.Owner!.Id);
        }

        [TestMethod]
        public async Task GetFoodPointLotsAsync_ReturnsOnlyLotsForRequestedFoodPoint()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var firstPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var secondPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var firstPoint = await database.AddFoodPointAsync(firstPartner);
            var secondPoint = await database.AddFoodPointAsync(secondPartner);
            var firstLot = await database.AddLotAsync(firstPoint, "First point lot");
            await database.AddLotAsync(secondPoint, "Second point lot");

            var lots = await database.LotService.GetFoodPointLotsAsync(firstPoint.Id);
            var result = lots.Single();

            Assert.AreEqual(firstLot.Id, result.Id);
            Assert.AreEqual(firstPoint.Id, result.FoodPointId);
        }

        [TestMethod]
        public async Task GetLotAsync_ReturnsLotByIdWithFoodPointData()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner, "Single lot cafe");
            var lot = await database.AddLotAsync(foodPoint);

            var result = await database.LotService.GetLotAsync(lot.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(lot.Id, result.Id);
            Assert.IsNotNull(result.FoodPoint);
            Assert.AreEqual("Single lot cafe", result.FoodPoint!.Name);
        }

        [TestMethod]
        public async Task GetCurrentPartnerLotAsync_WhenLotIsMissing_ReturnsNull()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            var result = await database.LotService.GetCurrentPartnerLotAsync(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task UpdateLotAsync_WhenLotIsOwned_UpdatesEditableFields()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint, totalQuantity: 6);
            var newDeadline = DateTime.UtcNow.AddHours(6);
            database.Auth.SetCurrentUser(partner);

            var updated = await database.LotService.UpdateLotAsync(
                new UpdateLotRequest(lot.Id, "Updated lot", "Updated description", "Updated composition", 120, newDeadline));

            Assert.AreEqual("Updated lot", updated.Title);
            Assert.AreEqual("Updated description", updated.Description);
            Assert.AreEqual("Updated composition", updated.Composition);
            Assert.AreEqual(120, updated.Price);
            Assert.AreEqual(6, updated.TotalQuantity);
            Assert.AreEqual(6, updated.AvailableQuantity);
            Assert.AreEqual(LotStatus.Active, updated.Status);
        }

        [TestMethod]
        public async Task UpdateLotAsync_WhenComponentsChange_ReplacesComponentsWithoutDuplicates()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            var lot = await database.LotService.CreateLotAsync(
                new CreateLotRequest(
                    foodPoint.Id,
                    "Lunch",
                    "Description",
                    string.Empty,
                    2,
                    10,
                    DateTime.UtcNow.AddHours(2),
                    new[]
                    {
                        new LotComponentRequest(null, "Soup", 1, "шт", null, null, 0),
                        new LotComponentRequest(null, "Dessert", 1, "шт", null, null, 1)
                    }));

            await database.LotService.UpdateLotAsync(
                new UpdateLotRequest(
                    lot.Id,
                    "Lunch updated",
                    "Description",
                    string.Empty,
                    12,
                    DateTime.UtcNow.AddHours(3),
                    new[]
                    {
                        new LotComponentRequest(lot.Components[0].Id, "Soup", 2, "шт", "250 ml", "component-image.png", 0),
                        new LotComponentRequest(null, "Salad", 1, "шт", "greens", null, 1)
                    }));

            database.DbContext.ChangeTracker.Clear();

            var saved = await database.LotService.GetLotAsync(lot.Id)
                ?? throw new InvalidOperationException("Лот не найден.");

            Assert.AreEqual(2, saved.Components.Count);
            CollectionAssert.AreEqual(
                new[] { "Soup — 2 шт", "Salad — 1 шт" },
                saved.Components.OrderBy(x => x.SortOrder).Select(x => x.DisplayText).ToArray());
            Assert.AreEqual("Soup — 2 шт; Salad — 1 шт", saved.Composition);
            Assert.AreEqual("250 ml", saved.Components.OrderBy(x => x.SortOrder).First().Composition);
            Assert.AreEqual("component-image.png", saved.Components.OrderBy(x => x.SortOrder).First().ImagePath);
        }

        [TestMethod]
        public async Task UpdateLotAsync_WhenLotBelongsToOtherPartner_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var owner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(owner);
            var lot = await database.AddLotAsync(foodPoint);
            database.Auth.SetCurrentUser(otherPartner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.LotService.UpdateLotAsync(
                    new UpdateLotRequest(lot.Id, "Updated", "Description", "Composition", 100, DateTime.UtcNow.AddHours(3))));
        }

        [TestMethod]
        public async Task DeleteLotAsync_WhenLotIsOwned_CancelsLotWithoutRemovingIt()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint);
            database.Auth.SetCurrentUser(partner);

            await database.LotService.DeleteLotAsync(lot.Id);

            var saved = await database.FoodLotRepository.GetByIdAsync(lot.Id);

            Assert.IsNotNull(saved);
            Assert.AreEqual(LotStatus.Cancelled, saved.Status);
        }

        [TestMethod]
        public async Task DeleteLotAsync_WhenLotBelongsToOtherPartner_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var owner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(owner);
            var lot = await database.AddLotAsync(foodPoint);
            database.Auth.SetCurrentUser(otherPartner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.LotService.DeleteLotAsync(lot.Id));
        }

        [TestMethod]
        public async Task MarkExpiredLotsAsync_MarksPastActiveLotsAsExpired()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var expiredCandidate = await database.AddLotAsync(foodPoint);
            var activeLot = await database.AddLotAsync(foodPoint);
            await MakeLotExpiredCandidateAsync(database, expiredCandidate.Id);

            var updated = await database.LotService.MarkExpiredLotsAsync();
            var expired = await database.FoodLotRepository.GetByIdAsync(expiredCandidate.Id);
            var active = await database.FoodLotRepository.GetByIdAsync(activeLot.Id);

            Assert.AreEqual(1, updated);
            Assert.IsNotNull(expired);
            Assert.AreEqual(LotStatus.Expired, expired.Status);
            Assert.IsNotNull(active);
            Assert.AreEqual(LotStatus.Active, active.Status);
        }

        [TestMethod]
        public async Task GetAvailableLotsAsync_ReturnsOnlyActiveFutureLotsWithAvailableQuantityAndActiveFoodPoint()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var inactivePartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var inactiveFoodPoint = await database.AddFoodPointAsync(inactivePartner);

            var available = await database.AddLotAsync(foodPoint, "Available lot");
            var expired = await database.AddLotAsync(foodPoint, "Expired lot");
            var cancelled = await database.AddLotAsync(foodPoint, "Cancelled lot");
            var soldOut = await database.AddLotAsync(foodPoint, "Sold out lot", totalQuantity: 2);
            var inactivePointLot = await database.AddLotAsync(inactiveFoodPoint, "Inactive point lot");

            await MakeLotExpiredCandidateAsync(database, expired.Id);

            cancelled.Cancel();
            await database.FoodLotRepository.UpdateAsync(cancelled);

            soldOut.DecreaseQuantity(soldOut.AvailableQuantity);
            await database.FoodLotRepository.UpdateAsync(soldOut);

            inactiveFoodPoint.Deactivate();
            await database.FoodPointRepository.UpdateAsync(inactiveFoodPoint);
            await database.DbContext.SaveChangesAsync();
            database.DbContext.ChangeTracker.Clear();

            var lots = await database.LotService.GetAvailableLotsAsync();
            var result = lots.Single();

            Assert.AreEqual(available.Id, result.Id);
            Assert.AreEqual("Available lot", result.Title);
            Assert.IsNotNull(result.FoodPoint);
            Assert.AreEqual(foodPoint.Id, result.FoodPoint!.Id);
        }

        [TestMethod]
        public async Task GetAvailableLotsAsync_WhenNoAvailableLots_ReturnsEmptyList()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint);
            await MakeLotExpiredCandidateAsync(database, lot.Id);

            var lots = await database.LotService.GetAvailableLotsAsync();

            Assert.IsFalse(lots.Any());
        }

        [TestMethod]
        public async Task SoldOutLot_IsNotAvailableInCatalog()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint, totalQuantity: 1);
            lot.DecreaseQuantity(1);
            await database.FoodLotRepository.UpdateAsync(lot);
            await database.FoodLotRepository.SaveChangesAsync();

            var lots = await database.LotService.GetAvailableLotsAsync();

            Assert.IsFalse(lots.Any());
            Assert.AreEqual(LotStatus.SoldOut, lot.Status);
        }

        private static async Task MakeLotExpiredCandidateAsync(SqliteTestDatabase database, Guid lotId)
        {
            var lot = await database.FoodLotRepository.GetByIdAsync(lotId)
                ?? throw new InvalidOperationException("Тестовый лот не найден.");

            SetPrivateProperty(lot, nameof(FoodLot.PickupDeadline), DateTime.UtcNow.AddHours(-1));
            SetPrivateProperty(lot, nameof(FoodLot.Status), LotStatus.Active);

            await database.FoodLotRepository.UpdateAsync(lot);
            await database.FoodLotRepository.SaveChangesAsync();
            database.DbContext.ChangeTracker.Clear();
        }

        private static void SetPrivateProperty<TValue>(FoodLot lot, string propertyName, TValue value)
        {
            var property = typeof(FoodLot).GetProperty(propertyName)
                ?? throw new InvalidOperationException($"Свойство {propertyName} не найдено.");

            property.SetValue(lot, value);
        }
    }
}
