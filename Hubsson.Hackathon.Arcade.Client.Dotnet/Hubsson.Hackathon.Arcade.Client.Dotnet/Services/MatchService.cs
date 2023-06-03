using System;
using System.Collections.Immutable;
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
            // add new coords to Obstacles..
            _matchRepository.Obstacles =
                gameState.players
                    .Select(p => p.coordinates.LastOrDefault())
                    .Aggregate(_matchRepository.Obstacles,
                        (state, coords) => coords is null ? state : state.Add(new Coords(coords.x, coords.y)));
            
            var currentPlayerLastPos = gameState.players.Single(p => p.playerId == _matchRepository.CurrentPlayerName).coordinates.Last();
            var currentPos = new Coords(currentPlayerLastPos.x, currentPlayerLastPos.y);
            
            
            // On Each Frame Update return an Action for your player
            //var newDir = PathFind.DumbAvoider(gameState.players, _matchRepository.CurrentPlayerName, gameState.width,
            //    gameState.height, _matchRepository.Bias);

            var cancellationSource = new CancellationTokenSource();

            var timer = new Timer(_ => cancellationSource.Cancel(false), null, TimeSpan.FromMilliseconds(gameState.tickTimeInMs * 0.5),
                Timeout.InfiniteTimeSpan);

            var otherPlayers =
                gameState.players
                    .Where(p => p.playerId != _matchRepository.CurrentPlayerName)
                    .Select(p =>
                    {
                        var lastPos = p.coordinates.Last();
                        var lastCoords = new Coords(lastPos.x, lastPos.y);
                        if(p.coordinates.Length < 2)
                            return new OtherPlayer(lastCoords, Direction.Up);
                        var lastButOneCoords = p.coordinates[^2];
                        var lastDirection = PathFind.CoordsToDirection(
                            new Coords(lastButOneCoords.x, lastButOneCoords.y),
                            lastCoords
                        );
                        return new OtherPlayer(lastCoords, lastDirection);
                    }).ToImmutableList();
            var newDir = PlanAheadNode.Strategize(_matchRepository.Obstacles, gameState.width, gameState.height,
                currentPos, otherPlayers, cancellationSource.Token);


            _matchRepository.Bias = newDir;
            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
            return new Action() { iteration = gameState.iteration, direction = newDir};
        }

        public class MatchRepository
        {
            public string CurrentPlayerName { get; init; }
            public ImmutableHashSet<Coords> Obstacles { get; set; } = ImmutableHashSet<Coords>.Empty;
            public Direction Bias { get; set; }
        }
    }
}