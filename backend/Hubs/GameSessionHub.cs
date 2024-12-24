using backend.DTOs;
using backend.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Concurrent;
namespace backend.Hubs
{
    public class GameSessionHub : Hub
    {
        private readonly IGame _gameService;
        private readonly ILogger<GameSessionHub> _logger;
        private static readonly ConcurrentDictionary<string, List<int>> UserGeneratedNumbers = new();
        public GameSessionHub(IGame gameService, ILogger<GameSessionHub> logger)
        {
            _gameService = gameService;
            _logger = logger;
        }


        public async Task JoinGameSession(GameAnswerSubmit gameAnswerSubmit)
        {
            try
            {
                if (gameAnswerSubmit == null)
                {
                    throw new ArgumentException("Answer cannot be null or empty");
                }
                var sessionId = gameAnswerSubmit.SessionId.ToString();

                // Create a list of numbers for the user
                UserGeneratedNumbers.TryAdd(Context.ConnectionId, new List<int>());


                // Add the user to the group
                await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
                _logger.LogInformation($"Connection ID: {Context.ConnectionId} joined session ID: {sessionId}");

                // Generate the first number
                int firstNumber = _gameService.GenerateRandomNumber(int.Parse(sessionId),
                                                                    UserGeneratedNumbers[Context.ConnectionId].ToArray(),
                                                                    gameAnswerSubmit.StartRange,
                                                                    gameAnswerSubmit.EndRange);
                UserGeneratedNumbers[Context.ConnectionId].Add(firstNumber);
                //_logger.LogInformation("This is first number: " + firstNumber.ToString());

                // Send the first number to the user
                await Clients.Caller.SendAsync("ReceiveGameUpdate", new GameAnswerResponse { IsCorrect = false, NextNumber = firstNumber, Score = 0 });
            }
            catch (Exception e)
            {
                _logger.LogError($"Error JoinGameSession: {e.Message}");
            }

        }

        public async Task SubmitAnswer(GameAnswerSubmit gameAnswerSubmit)
        {
            try
            {
                if (gameAnswerSubmit == null)
                {
                    throw new ArgumentNullException(nameof(gameAnswerSubmit), "GameAnswerSubmit cannot be null.");
                }

                UserGeneratedNumbers.TryAdd(Context.ConnectionId, new List<int>());

                var isCorrect = await _gameService.ValidateAnswer(gameAnswerSubmit);

                // Generate the next number that has not been used
                int nextNumber = _gameService.GenerateRandomNumber(
                                                                    gameAnswerSubmit.SessionId,
                                                                    UserGeneratedNumbers[Context.ConnectionId].ToArray(),
                                                                    gameAnswerSubmit.StartRange,
                                                                    gameAnswerSubmit.EndRange
                                                                );


                // Add the number to the list of used numbers
                UserGeneratedNumbers[Context.ConnectionId].Add(nextNumber);

                var result = new GameAnswerResponse
                {
                    IsCorrect = isCorrect,
                    NextNumber = nextNumber,
                    Score = isCorrect ? gameAnswerSubmit.Score + 10 : gameAnswerSubmit.Score
                };

                _logger.LogInformation($"Answer submitted: {JsonConvert.SerializeObject(gameAnswerSubmit, Formatting.Indented)}");
                await Clients.Group(gameAnswerSubmit.SessionId.ToString()).SendAsync("ReceiveGameUpdate", result);

            }
            catch (Exception e)
            {
                await Clients.Caller.SendAsync("ReceiveError", e.Message);
                _logger.LogError(e.Message);
            }
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (UserGeneratedNumbers.ContainsKey(Context.ConnectionId))
            {
                UserGeneratedNumbers.TryRemove(Context.ConnectionId, out _);
            }
            _logger.LogInformation($"Connection {Context.ConnectionId} disconnected.");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
