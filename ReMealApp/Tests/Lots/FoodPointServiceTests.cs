using Application.DTOs.FoodPoints;
using Domain.Enums;
using Tests.TestSupport;

namespace Tests.Lots
{
    [TestClass]
    public sealed class FoodPointServiceTests
    {
        [TestMethod]
        public async Task CreateFoodPointAsync_WhenCurrentUserIsPartner_CreatesOwnedFoodPoint()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(partner);

            var foodPoint = await database.FoodPointService.CreateFoodPointAsync(
                new CreateFoodPointRequest("Main cafe", "Campus, 1", "Hot meals", "+10000000000"));

            var saved = (await database.FoodPointRepository.GetByOwnerIdAsync(partner.Id)).Single();

            Assert.IsNotNull(saved);
            Assert.AreEqual(foodPoint.Id, saved.Id);
            Assert.AreEqual(partner.Id, foodPoint.OwnerId);
            Assert.IsTrue(foodPoint.IsActive);
            Assert.AreEqual("Main cafe", saved.Name);
            Assert.AreEqual("Campus, 1", saved.Address);
            Assert.AreEqual("Hot meals", saved.Description);
            Assert.AreEqual("+10000000000", saved.Phone);
        }

        [TestMethod]
        public async Task CreateFoodPointAsync_WhenCurrentUserIsCustomer_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var customer = await database.AddUserAsync(UserRole.StudentCustomer);
            database.Auth.SetCurrentUser(customer);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.FoodPointService.CreateFoodPointAsync(
                    new CreateFoodPointRequest("Cafe", "Campus", "Meals", "+10000000000")));
        }

        [TestMethod]
        public async Task CreateFoodPointAsync_WhenUserIsNotAuthenticated_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.FoodPointService.CreateFoodPointAsync(
                    new CreateFoodPointRequest("Cafe", "Campus", "Meals", "+10000000000")));
        }

        [TestMethod]
        public async Task CreateFoodPointAsync_WhenPartnerAlreadyHasFoodPoint_AllowsAnotherFoodPoint()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(partner);

            var first = await database.FoodPointService.CreateFoodPointAsync(
                new CreateFoodPointRequest("Cafe", "Campus", "Meals", "+10000000000"));

            var second = await database.FoodPointService.CreateFoodPointAsync(
                new CreateFoodPointRequest("Second cafe", "Campus 2", "Meals", "+10000000001"));

            var points = await database.FoodPointService.GetCurrentPartnerFoodPointsAsync();
            var ids = points.Select(x => x.Id).ToArray();

            CollectionAssert.Contains(ids, first.Id);
            CollectionAssert.Contains(ids, second.Id);
            CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, ids);
        }

        [TestMethod]
        public async Task CreateFoodPointAsync_WhenRequiredFieldsAreInvalid_ThrowsArgumentException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            database.Auth.SetCurrentUser(partner);

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.FoodPointService.CreateFoodPointAsync(
                    new CreateFoodPointRequest("", "Campus", "Meals", "+10000000000")));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.FoodPointService.CreateFoodPointAsync(
                    new CreateFoodPointRequest("Cafe", "", "Meals", "+10000000000")));

            await AssertEx.ThrowsAsync<ArgumentException>(() =>
                database.FoodPointService.CreateFoodPointAsync(
                    new CreateFoodPointRequest("Cafe", "Campus", "Meals", "")));
        }

        [TestMethod]
        public async Task GetCurrentPartnerFoodPointsAsync_ReturnsOnlyCurrentPartnerFoodPoints()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var firstPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var secondPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var firstPoint = await database.AddFoodPointAsync(firstPartner, "First cafe");
            var anotherFirstPoint = await database.AddFoodPointAsync(firstPartner, "First cafe branch");
            await database.AddFoodPointAsync(secondPartner, "Second cafe");

            database.Auth.SetCurrentUser(firstPartner);

            var result = await database.FoodPointService.GetCurrentPartnerFoodPointsAsync();
            var ids = result.Select(x => x.Id).ToArray();

            CollectionAssert.Contains(ids, firstPoint.Id);
            CollectionAssert.Contains(ids, anotherFirstPoint.Id);
            CollectionAssert.AreEquivalent(new[] { firstPoint.Id, anotherFirstPoint.Id }, ids);
        }

        [TestMethod]
        public async Task GetAllFoodPointsAsync_ReturnsFoodPointsFromDatabase()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var firstPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var secondPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var firstPoint = await database.AddFoodPointAsync(firstPartner, "First cafe");
            var secondPoint = await database.AddFoodPointAsync(secondPartner, "Second cafe");

            var points = await database.FoodPointService.GetAllFoodPointsAsync();
            var ids = points.Select(x => x.Id).ToArray();

            CollectionAssert.Contains(ids, firstPoint.Id);
            CollectionAssert.Contains(ids, secondPoint.Id);
        }

        [TestMethod]
        public async Task GetFoodPointAsync_ReturnsFoodPointWithOwnerAndLots()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint);

            var result = await database.FoodPointService.GetFoodPointAsync(foodPoint.Id);

            Assert.IsNotNull(result);
            Assert.AreEqual(foodPoint.Id, result.Id);
            Assert.IsNotNull(result.Owner);
            Assert.AreEqual(partner.Id, result.Owner!.Id);
            Assert.IsTrue(result.Lots.Any(x => x.Id == lot.Id));
        }

        [TestMethod]
        public async Task UpdateFoodPointAsync_WhenFoodPointIsOwned_UpdatesData()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            var updated = await database.FoodPointService.UpdateFoodPointAsync(
                new UpdateFoodPointRequest(foodPoint.Id, "Updated cafe", "New address", "Updated meals", "+19999999999"));

            Assert.AreEqual("Updated cafe", updated.Name);
            Assert.AreEqual("New address", updated.Address);
            Assert.AreEqual("Updated meals", updated.Description);
            Assert.AreEqual("+19999999999", updated.Phone);
        }

        [TestMethod]
        public async Task UpdateFoodPointAsync_WhenFoodPointBelongsToOtherPartner_ThrowsUnauthorizedAccessException()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var owner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var otherPartner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(owner);
            database.Auth.SetCurrentUser(otherPartner);

            await AssertEx.ThrowsAsync<UnauthorizedAccessException>(() =>
                database.FoodPointService.UpdateFoodPointAsync(
                    new UpdateFoodPointRequest(foodPoint.Id, "Hacked", "Other", "Other", "+19999999999")));
        }

        [TestMethod]
        public async Task DeactivateFoodPointAsync_WhenFoodPointIsOwned_MarksFoodPointInactive()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            database.Auth.SetCurrentUser(partner);

            await database.FoodPointService.DeactivateFoodPointAsync(foodPoint.Id);

            var saved = await database.FoodPointRepository.GetByIdAsync(foodPoint.Id);

            Assert.IsNotNull(saved);
            Assert.IsFalse(saved.IsActive);
        }

        [TestMethod]
        public async Task DeleteFoodPointAsync_WhenFoodPointIsOwned_RemovesFoodPointAndLots()
        {
            await using var database = await SqliteTestDatabase.CreateAsync();
            var partner = await database.AddUserAsync(UserRole.FoodPointRepresentative);
            var foodPoint = await database.AddFoodPointAsync(partner);
            var lot = await database.AddLotAsync(foodPoint);
            database.Auth.SetCurrentUser(partner);

            await database.FoodPointService.DeleteFoodPointAsync(foodPoint.Id);

            Assert.IsNull(await database.FoodPointRepository.GetByIdAsync(foodPoint.Id));
            Assert.IsNull(await database.FoodLotRepository.GetByIdAsync(lot.Id));
        }
    }
}
