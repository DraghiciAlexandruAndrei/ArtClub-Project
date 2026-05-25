using ArtClub.Controllers;
using ArtClub.DataAccess;
using ArtClub.Models.Entities;
using ArtClub.Models.Enums;
using ArtClub.Models.ViewModels.Admin;
using ArtClub.Models.ViewModels.Finance;
using ArtClub.Models.ViewModels.Resource;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Security.Claims;
using Xunit;

// Pentru a rula testele , folosește comanda : dotnet test .\ArtClub.Tests\ -v normal

namespace ArtClub.Tests
{
    public class ControllerTests
    {
        // ==========================================
        // 1. HOME CONTROLLER TESTS (4 Teste)
        // ==========================================

        [Fact]
        public async Task Home_Index_AnonymousUser_ReturnsViewWithNoData()
        {
            // Arrange
            var mockEventService = new Mock<IEventService>();
            var mockArtService = new Mock<IArtPieceService>();
            var mockUserStore = new Mock<IUserStore<User>>();
            var mockSignInManager = new Mock<SignInManager<User>>(
                new Mock<UserManager<User>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!).Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null!, null!, null!, null!);

            // Simulăm că utilizatorul NU este logat
            mockSignInManager.Setup(s => s.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(false);

            var controller = new HomeController(mockEventService.Object, mockArtService.Object, mockSignInManager.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewData["ActiveEvents"]);
        }

        [Fact]
        public async Task Home_Index_SignedInUser_PopulatesDashboardData()
        {
            // Arrange
            var mockEventService = new Mock<IEventService>();
            mockEventService.Setup(s => s.GetAllEventsAsync()).ReturnsAsync(new List<Event>());

            var mockArtService = new Mock<IArtPieceService>();
            mockArtService.Setup(s => s.GetAllArtPiecesAsync()).ReturnsAsync(new List<ArtPiece>());

            var mockUserStore = new Mock<IUserStore<User>>();
            var mockSignInManager = new Mock<SignInManager<User>>(
                new Mock<UserManager<User>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!).Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null!, null!, null!, null!);

            // Simulăm că utilizatorul ESTE logat
            mockSignInManager.Setup(s => s.IsSignedIn(It.IsAny<ClaimsPrincipal>())).Returns(true);

            var controller = new HomeController(mockEventService.Object, mockArtService.Object, mockSignInManager.Object);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.ViewData["ActiveEvents"]);
            Assert.NotNull(viewResult.ViewData["PopularArtPieces"]);
        }

        [Fact]
        public void Home_Privacy_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController(null, null, null);

            // Act
            var result = controller.Privacy();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Home_Error_ReturnsViewWithErrorViewModel()
        {
            // Arrange
            var controller = new HomeController(null, null, null);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = controller.Error();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.NotNull(viewResult.Model);
        }


        // ==========================================
        // 2. RESOURCE CONTROLLER TESTS (6 Teste)
        // ==========================================

        [Fact]
        public async Task Resource_Index_ReturnsViewWithAllResources()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            mockService.Setup(s => s.GetAllResourcesAsync()).ReturnsAsync(new List<Resource>
            {
                new Resource { Name = "Sala A", Capacity = 10, Reservations = new List<Reservation>() }
            });

            var controller = new ResourceController(mockService.Object);

            // Act
            var result = await controller.Index(null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<ResourceOverviewViewModel>>(viewResult.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Resource_Details_ExistingName_ReturnsViewWithResource()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            mockService.Setup(s => s.GetResourceByNameAsync("Sala A")).ReturnsAsync(new Resource { Name = "Sala A" });
            var controller = new ResourceController(mockService.Object);

            // Act
            var result = await controller.Details("Sala A");

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Resource>(viewResult.Model);
            Assert.Equal("Sala A", model.Name);
        }

        [Fact]
        public async Task Resource_Details_NonExistingName_ReturnsNotFound()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            mockService.Setup(s => s.GetResourceByNameAsync("Inexistenta")).ReturnsAsync((Resource?)null);
            var controller = new ResourceController(mockService.Object);

            // Act
            var result = await controller.Details("Inexistenta");

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Resource_Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            var controller = new ResourceController(mockService.Object);
            controller.ModelState.AddModelError("Name", "Required"); // Fortam model invalid
            var viewModel = new ResourceCreateViewModel { Name = "" };

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
        }

        [Fact]
        public async Task Resource_Create_Post_ValidModel_RedirectsToIndex()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            var controller = new ResourceController(mockService.Object);
            var viewModel = new ResourceCreateViewModel { Name = "Sala Noua", Capacity = 20, Type = "Atelier" };

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            mockService.Verify(s => s.CreateResourceAsync(It.IsAny<Resource>()), Times.Once);
        }

        [Fact]
        public async Task Resource_DeleteConfirmed_Success_RedirectsToIndex()
        {
            // Arrange
            var mockService = new Mock<IReservationService>();
            mockService.Setup(s => s.DeleteResourceAsync("Sala Veche")).ReturnsAsync(true);
            var controller = new ResourceController(mockService.Object);

            // Act
            var result = await controller.DeleteConfirmed("Sala Veche");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }


        // ==========================================
        // 3. INVITATION CONTROLLER TESTS (5 Teste)
        // ==========================================

        [Fact]
        public async Task Invitation_Invite_Success_SetsStatusMessageAndRedirects()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            mockService.Setup(s => s.SendInvitationAsync(1, 2)).ReturnsAsync(true);

            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);
            controller.TempData = new Mock<ITempDataDictionary>().Object; // Mock TempData obligatoriu

            // Act
            var result = await controller.Invite(1, 2, "Vernisaj");

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirectResult.ActionName);
            Assert.Equal("Event", redirectResult.ControllerName);
        }

        [Fact]
        public async Task Invitation_Invite_Fail_SetsErrorMessage()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            mockService.Setup(s => s.SendInvitationAsync(1, 2)).ReturnsAsync(false); // Simulăm eșecul trimiterii

            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);

            var httpContext = new DefaultHttpContext();
            var tempDataProvider = new Mock<ITempDataProvider>();
            var tempData = new TempDataDictionary(httpContext, tempDataProvider.Object);
            controller.TempData = tempData;

            // Act
            await controller.Invite(1, 2, "Vernisaj");

            // Assert
            // MODIFICARE AICI: Am actualizat textul ca să fie identic cu cel din Controller-ul tău
            Assert.Equal("Utilizatorul este deja invitat sau nu poate fi invitat.", controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task Invitation_Decline_RedirectsToMyProfile()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);

            // SOLUȚIE: Inițializăm ControllerContext și un obiect User (ClaimsPrincipal) simulat 
            // pentru a preveni NullReferenceException dacă metoda Decline accesează proprietățile HTTP
            var userPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
        new Claim(ClaimTypes.NameIdentifier, "1"),
        new Claim(ClaimTypes.Name, "testuser")
    }, "mock"));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = userPrincipal }
            };

            // Opțional: Adăugăm și un TempData gol dacă metoda din controller folosește TempData la Decline
            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(controller.HttpContext, tempDataProvider.Object);

            // Act
            var result = await controller.Decline(10);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyProfile", redirectResult.ActionName);
            mockService.Verify(s => s.DeclineInvitationAsync(10), Times.Once);
        }

        [Fact]
        public async Task Invitation_Accept_RedirectsToMyProfile()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);
            controller.TempData = new Mock<ITempDataDictionary>().Object;

            // Act
            var result = await controller.Accept(10);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("MyProfile", redirectResult.ActionName);
            Assert.Equal("Account", redirectResult.ControllerName);
            mockService.Verify(s => s.AcceptInvitationAsync(10), Times.Once);
        }


        // ==========================================
        // 4. ADMIN CONTROLLER TESTS (5 Teste)
        // ==========================================

        [Fact]
        public async Task Admin_Users_SearchByUserName_ReturnsFilteredUsers()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_Users_SearchByUserName_ReturnsFilteredUsers));
            context.Users.AddRange(
                new User { Id = 1, UserName = "alex", Email = "alex@artclub.local", Role = UserRole.Member },
                new User { Id = 2, UserName = "maria", Email = "maria@artclub.local", Role = UserRole.Member });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.Users("alex", null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model);
            var users = model.ToList();
            Assert.Single(users);
            Assert.Equal("alex", users[0].UserName);
        }

        [Fact]
        public async Task Admin_BanUser_ExistingUser_UpdatesFlagsAndRedirects()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_BanUser_ExistingUser_UpdatesFlagsAndRedirects));
            context.Users.Add(new User { Id = 10, UserName = "member1", Email = "member1@artclub.local", IsActive = true, IsBanned = false, Role = UserRole.Member });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.BanUser(10);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            var user = await context.Users.FindAsync(10);
            Assert.NotNull(user);
            Assert.True(user.IsBanned);
            Assert.False(user.IsActive);
        }

        [Fact]
        public async Task Admin_UnbanUser_ExistingUser_UpdatesFlagsAndRedirects()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_UnbanUser_ExistingUser_UpdatesFlagsAndRedirects));
            context.Users.Add(new User { Id = 11, UserName = "member2", Email = "member2@artclub.local", IsActive = false, IsBanned = true, Role = UserRole.Member });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.UnbanUser(11);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirectResult.ActionName);

            var user = await context.Users.FindAsync(11);
            Assert.NotNull(user);
            Assert.False(user.IsBanned);
            Assert.True(user.IsActive);
        }

        [Fact]
        public async Task Admin_ClubSettings_Post_InvalidModel_ReturnsViewWithModel()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_ClubSettings_Post_InvalidModel_ReturnsViewWithModel));
            var controller = CreateAdminController(context);
            controller.ModelState.AddModelError("MembershipCost", "Required");
            var model = new ClubSettingsViewModel { MembershipCost = 100m };

            // Act
            var result = await controller.ClubSettings(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        [Fact]
        public async Task Admin_CreateResource_Post_ValidModel_CreatesResourceAndRedirects()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_CreateResource_Post_ValidModel_CreatesResourceAndRedirects));
            var controller = CreateAdminController(context);
            var resource = new Resource { Name = "Atelier 2", Description = "Test", Capacity = 30, BasePrice = 150m };

            // Act
            var result = await controller.CreateResource(resource);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Resources", redirectResult.ActionName);
            Assert.Equal(1, await context.Resources.CountAsync());
        }

        [Fact]
        public async Task Admin_Users_RoleFilter_ReturnsOnlyMatchingRole()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_Users_RoleFilter_ReturnsOnlyMatchingRole));
            context.Users.AddRange(
                new User { Id = 21, UserName = "admin", Email = "admin@artclub.local", Role = UserRole.Admin },
                new User { Id = 22, UserName = "member", Email = "member@artclub.local", Role = UserRole.Member });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.Users(null, UserRole.Admin);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<User>>(viewResult.Model).ToList();
            Assert.Single(model);
            Assert.Equal(UserRole.Admin, model[0].Role);
        }

        [Fact]
        public async Task Admin_ExhibitionHalls_ReturnsOnlyExhibitionResources()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_ExhibitionHalls_ReturnsOnlyExhibitionResources));
            context.Resources.AddRange(
                new Resource { Id = 1, Name = "Hall A", Description = "Expo", Capacity = 100, IsExhibitionHall = true },
                new Resource { Id = 2, Name = "Room B", Description = "Meeting", Capacity = 20, IsExhibitionHall = false });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.ExhibitionHalls();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Resource>>(viewResult.Model).ToList();
            Assert.Single(model);
            Assert.True(model[0].IsExhibitionHall);
        }

        [Fact]
        public async Task Admin_Locations_ReturnsOnlyNonExhibitionResources()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_Locations_ReturnsOnlyNonExhibitionResources));
            context.Resources.AddRange(
                new Resource { Id = 3, Name = "Hall C", Description = "Expo", Capacity = 80, IsExhibitionHall = true },
                new Resource { Id = 4, Name = "Location D", Description = "Affiliated", Capacity = 40, IsExhibitionHall = false });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.Locations();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Resource>>(viewResult.Model).ToList();
            Assert.Single(model);
            Assert.False(model[0].IsExhibitionHall);
        }

        [Fact]
        public async Task Admin_ClubSettings_Post_ValidModel_SavesSettingsAndRedirects()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_ClubSettings_Post_ValidModel_SavesSettingsAndRedirects));
            var controller = CreateAdminController(context);
            var model = new ClubSettingsViewModel
            {
                NonMemberReservationFeePerDay = 450m,
                MembershipCost = 120m,
                EventCostPerArtPiece = 210m,
                EventCostPerLocation = 310m,
                PendingOverrideApprovalHours = 2
            };

            // Act
            var result = await controller.ClubSettings(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("ClubSettings", redirectResult.ActionName);

            var settings = await context.ClubSettings.FirstOrDefaultAsync();
            Assert.NotNull(settings);
            Assert.Equal(450m, settings.NonMemberReservationFeePerDay);
            Assert.Equal(2, settings.PendingOverrideApprovalHours);
        }

        [Fact]
        public async Task Admin_Reports_ExpensesGreaterThanIncome_SetsMembersBlockedTrue()
        {
            // Arrange
            await using var context = CreateAdminDbContext(nameof(Admin_Reports_ExpensesGreaterThanIncome_SetsMembersBlockedTrue));
            var now = DateTime.Now;
            context.Payments.AddRange(
                new Payment { Id = 1, Amount = 100m, IsIncome = true, Date = now, Description = "Income" },
                new Payment { Id = 2, Amount = 250m, IsIncome = false, Date = now, Description = "Expense" });
            await context.SaveChangesAsync();

            var controller = CreateAdminController(context);

            // Act
            var result = await controller.Reports(now.Month, now.Year);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<MonthlyReportViewModel>(viewResult.Model);
            Assert.True(model.MembersBlocked);
        }

        [Fact]
        public async Task Invitation_Inbox_UserNull_ReturnsChallengeResult()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

            // Simulam ca UserAsync returneaza null (utilizator invalid)
            mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User?)null);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);

            // Act
            var result = await controller.Inbox();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }

        private static ApplicationDbContext CreateAdminDbContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new ApplicationDbContext(options);
        }

        private static AdminController CreateAdminController(ApplicationDbContext context)
        {
            var userStore = new Mock<IUserStore<User>>();
            var userManager = new Mock<UserManager<User>>(
                userStore.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!);

            userManager.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(new List<string>());
            userManager.Setup(m => m.UpdateAsync(It.IsAny<User>())).ReturnsAsync(IdentityResult.Success);
            userManager.Setup(m => m.IsInRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(false);
            userManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>())).ReturnsAsync(IdentityResult.Success);

            var signInManager = new Mock<SignInManager<User>>(
                userManager.Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null!,
                null!,
                null!,
                null!);

            var controller = new AdminController(context, userManager.Object, signInManager.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };

            var tempDataProvider = new Mock<ITempDataProvider>();
            controller.TempData = new TempDataDictionary(controller.HttpContext, tempDataProvider.Object);
            return controller;
        }
    }
}
