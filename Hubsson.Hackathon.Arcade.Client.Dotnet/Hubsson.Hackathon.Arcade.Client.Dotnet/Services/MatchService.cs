using System;
using System.Diagnostics;
using Hubsson.Hackathon.Arcade.Client.Dotnet.Domain;
using Action = Hubsson.Hackathon.Arcade.Client.Dotnet.Domain.Action;
using ClientGameState = Hubsson.Hackathon.Arcade.Client.Dotnet.Domain.ClientGameState;

namespace Hubsson.Hackathon.Arcade.Client.Dotnet.Services
{
    public class MatchService
    {
        private MatchRepository _matchRepository;
        private ArcadeSettings _arcadeSettings;
        
        public MatchService(ArcadeSettings settings)
        {
            _matchRepository = new MatchRepository()
            {
                CurrentPlayerName = settings.TeamId,
                Bias = Direction.Up
            };
            _arcadeSettings = settings;
        }
        
        public void Init()
        {
            // On Game Init
        }

        public Action Update(ClientGameState gameState)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // On Each Frame Update return an Action for your player
            var newDir = PathFind.DumbAvoider(gameState.players, _matchRepository.CurrentPlayerName, gameState.width,
                gameState.height, _matchRepository.Bias);

            _matchRepository.Bias = newDir;
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
            return new Action() { iteration = gameState.iteration, direction = newDir};
        }

        public class MatchRepository
        {
            public string CurrentPlayerName { get; init; }
            public Direction Bias { get; set; }
        }
    }
}