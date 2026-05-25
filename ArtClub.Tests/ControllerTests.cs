using ArtClub.Controllers;
using ArtClub.Models.Entities;
using ArtClub.Models.ViewModels.Resource;
using ArtClub.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using System.Security.Claims;



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
                new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null).Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

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
                new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null).Object,
                new Mock<IHttpContextAccessor>().Object,
                new Mock<IUserClaimsPrincipalFactory<User>>().Object,
                null, null, null, null);

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
            mockService.Setup(s => s.GetResourceByNameAsync("Inexistenta")).ReturnsAsync((Resource)null);
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
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

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
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

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
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

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

        

        [Fact]
        public async Task Invitation_Inbox_UserNull_ReturnsChallengeResult()
        {
            // Arrange
            var mockService = new Mock<IInvitationService>();
            var mockUserStore = new Mock<IUserStore<User>>();
            var mockUserManager = new Mock<UserManager<User>>(mockUserStore.Object, null, null, null, null, null, null, null, null);

            // Simulam ca UserAsync returneaza null (utilizator invalid)
            mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((User)null);

            var controller = new InvitationController(mockService.Object, mockUserManager.Object);

            // Act
            var result = await controller.Inbox();

            // Assert
            Assert.IsType<ChallengeResult>(result);
        }
    }
}