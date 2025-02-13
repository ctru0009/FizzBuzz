using backend.Controllers;
using backend.DTOs;
using backend.Interfaces;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace backend.Tests
{
    [TestFixture]
    public class GamesControllerTests
    {
        private Mock<IGame> _mockGameService;
        private GamesController _controller;

        [SetUp]
        public void Setup()
        {
            _mockGameService = new Mock<IGame>();
            _controller = new GamesController(_mockGameService.Object);
        }

        [Test]
        public async Task Post_ValidGame_ReturnsOk()
        {
            // Arrange
            var gameRequestDTO = new GameRequestDTO
            {
                Name = "Test Game",
                PlayerId = 1,
                AuthorName = "Test Author",
                StartRange = 1,
                EndRange = 100,
                Rules = [new RuleDTO { DivisibleBy = 3, ReplacementWord = "Fizz" }]
            };

            var gameResponseDTO = new GameResponseDTO
            {
                Id = 1,
                Name = "Test Game",
                AuthorName = "Test Author",
                StartRange = 1,
                EndRange = 100,
                CreatedAt = DateTime.UtcNow,
                Rules = [new RuleDTO { DivisibleBy = 3, ReplacementWord = "Fizz" }]
            };

            _mockGameService.Setup(service => service.CreateGame(It.IsAny<GameRequestDTO>()))
                .ReturnsAsync(gameResponseDTO);

            // Act
            var result = await _controller.Post(gameRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(gameResponseDTO));
        }

        [Test]
        public async Task Post_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var gameRequestDTO = new GameRequestDTO
            {
                Name = "Test Game",
                PlayerId = 1,
                AuthorName = "Test Author",
                StartRange = 1,
                EndRange = 100,
                Rules = [new RuleDTO { DivisibleBy = 3, ReplacementWord = "Fizz" }]
            };

            _mockGameService.Setup(service => service.CreateGame(It.IsAny<GameRequestDTO>()))
                .ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Post(gameRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }

        [Test]
        public async Task Get_ReturnsOkWithListOfGames()
        {
            // Arrange
            var gameResponseDTOs = new List<GameResponseDTO>
            {
                new GameResponseDTO
                {
                    Id = 1,
                    Name = "Game 1",
                    AuthorName = "Author 1",
                    StartRange = 1,
                    EndRange = 100,
                    CreatedAt = DateTime.UtcNow,
                    Rules = [new RuleDTO { DivisibleBy = 3, ReplacementWord = "Fizz" }]
                },
                new GameResponseDTO
                {
                    Id = 2,
                    Name = "Game 2",
                    AuthorName = "Author 2",
                    StartRange = 1,
                    EndRange = 100,
                    CreatedAt = DateTime.UtcNow,
                    Rules = [new RuleDTO { DivisibleBy = 5, ReplacementWord = "Buzz" }]
                }
            };

            _mockGameService.Setup(service => service.GetGames()).ReturnsAsync(gameResponseDTOs);

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(gameResponseDTOs));
        }

        [Test]
        public async Task Get_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            _mockGameService.Setup(service => service.GetGames()).ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Get();

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }
    }


    [TestFixture]
    public class PlayersControllerTests
    {
        private Mock<IPlayer> _mockPlayerService;
        private PlayersController _controller;

        [SetUp]
        public void Setup()
        {
            _mockPlayerService = new Mock<IPlayer>();
            _controller = new PlayersController(_mockPlayerService.Object);
        }

        [Test]
        public async Task Login_PlayerExists_ReturnsOkWithPlayer()
        {
            // Arrange
            var playerRequestDTO = new PlayerRequestDTO { Name = "ExistingPlayer" };
            var playerResponseDTO = new PlayerResponseDTO { Id = 1, Name = "ExistingPlayer", CreatedAt = DateTime.UtcNow, TotalScores = 0, TotalGamesPlayed = 0 };

            _mockPlayerService.Setup(service => service.GetPlayerByName(playerRequestDTO.Name)).ReturnsAsync(new Player
            {
                Id = 1,
                Name = "ExistingPlayer",
                CreatedAt = DateTime.UtcNow,
                TotalScores = 0,
                TotalGamesPlayed = 0
            });

            // Act
            var result = await _controller.Login(playerRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.InstanceOf<Player>());
        }

        [Test]
        public async Task Login_PlayerDoesNotExist_CreatesAndReturnsPlayer()
        {
            // Arrange
            var playerRequestDTO = new PlayerRequestDTO { Name = "NewPlayer" };
            var playerResponseDTO = new PlayerResponseDTO { Id = 1, Name = "NewPlayer", CreatedAt = DateTime.UtcNow, TotalScores = 0, TotalGamesPlayed = 0 };

            _mockPlayerService.Setup(service => service.GetPlayerByName(playerRequestDTO.Name)).ReturnsAsync((Player)null);
            _mockPlayerService.Setup(service => service.CreatePlayer(playerRequestDTO)).ReturnsAsync(new Player
            {
                Id = 1,
                Name = "NewPlayer",
                CreatedAt = DateTime.UtcNow,
                TotalScores = 0,
                TotalGamesPlayed = 0
            });


            // Act
            var result = await _controller.Login(playerRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult?.Value, Is.InstanceOf<Player>());
        }

        [Test]
        public async Task Login_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var playerRequestDTO = new PlayerRequestDTO { Name = "TestPlayer" };

            _mockPlayerService.Setup(service => service.GetPlayerByName(playerRequestDTO.Name)).ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Login(playerRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }
    }

    [TestFixture]
    public class SessionsControllerTests
    {
        private Mock<backend.Interfaces.ISession> _mockSessionService;
        private SessionsController _controller;

        [SetUp]
        public void Setup()
        {
            _mockSessionService = new Mock<backend.Interfaces.ISession>();
            _controller = new SessionsController(_mockSessionService.Object);
        }

        [Test]
        public async Task Post_ValidSession_ReturnsOk()
        {
            // Arrange
            var sessionCreateRequestDTO = new SessionCreateRequestDTO { GameId = 1, PlayerId = 1, Duration = 60 };
            var sessionResponseDTO = new SessionResponseDTO { Id = 1, GameId = 1, PlayerId = 1, Duration = 60, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddSeconds(60), Score = 0 };

            _mockSessionService.Setup(service => service.CreateSession(It.IsAny<SessionCreateRequestDTO>()))
                .ReturnsAsync(sessionResponseDTO);

            // Act
            var result = await _controller.Post(sessionCreateRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(sessionResponseDTO));
        }

        [Test]
        public async Task Post_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            var sessionCreateRequestDTO = new SessionCreateRequestDTO { GameId = 1, PlayerId = 1, Duration = 60 };

            _mockSessionService.Setup(service => service.CreateSession(It.IsAny<SessionCreateRequestDTO>()))
                .ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Post(sessionCreateRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }

        [Test]
        public async Task Get_ValidSessionId_ReturnsOk()
        {
            // Arrange
            int sessionId = 1;
            var sessionResponseDTO = new SessionResponseDTO { Id = sessionId, GameId = 1, PlayerId = 1, Duration = 60, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddSeconds(60), Score = 0 };

            _mockSessionService.Setup(service => service.GetSession(sessionId)).ReturnsAsync(sessionResponseDTO);

            // Act
            var result = await _controller.Get(sessionId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(sessionResponseDTO));
        }

        [Test]
        public async Task Get_InvalidSessionId_ReturnsBadRequest()
        {
            // Arrange
            int sessionId = 1;

            _mockSessionService.Setup(service => service.GetSession(sessionId)).ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Get(sessionId);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }


        [Test]
        public async Task Put_ValidSession_ReturnsOk()
        {
            // Arrange
            int sessionId = 1;
            var sessionUpdateRequestDTO = new SessionUpdateRequestDTO { SessionId = sessionId, Score = 10 };
            var sessionResponseDTO = new SessionResponseDTO { Id = sessionId, GameId = 1, PlayerId = 1, Duration = 60, StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddSeconds(60), Score = 10 };

            _mockSessionService.Setup(service => service.UpdateSession(It.Is<SessionUpdateRequestDTO>(s => s.SessionId == sessionId)))
                .ReturnsAsync(sessionResponseDTO);

            // Act
            var result = await _controller.Put(sessionId, sessionUpdateRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = result as OkObjectResult;
            Assert.That(okResult, Is.Not.Null);
            Assert.That(okResult.Value, Is.EqualTo(sessionResponseDTO));
        }

        [Test]
        public async Task Put_ExceptionThrown_ReturnsBadRequest()
        {
            // Arrange
            int sessionId = 1;
            var sessionUpdateRequestDTO = new SessionUpdateRequestDTO { SessionId = sessionId, Score = 10 };

            _mockSessionService.Setup(service => service.UpdateSession(It.IsAny<SessionUpdateRequestDTO>()))
                .ThrowsAsync(new Exception("Test Exception"));

            // Act
            var result = await _controller.Put(sessionId, sessionUpdateRequestDTO);

            // Assert
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequestResult = result as BadRequestObjectResult;
            Assert.That(badRequestResult, Is.Not.Null);
            Assert.That(badRequestResult.Value, Is.EqualTo("Test Exception"));
        }
    }
}
