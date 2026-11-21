using Microsoft.AspNetCore.SignalR;
using MyPersonalWebsite.Models;
using MyPersonalWebsite.Services;

namespace MyPersonalWebsite.Hubs
{
    public class TicketHub : Hub
    {
        private readonly GrabGameService _game;

        public TicketHub(GrabGameService game) => _game = game;

        public object StartGrabGame(string difficulty, string? mode, string? trainNumber, string? from, string? to, string? seatClass)
            => GameState(_game.Start(difficulty, mode, trainNumber, from, to, seatClass));

        public object GrabSeat(string key) => _game.GrabSeat(key);

        public object AnswerCaptcha(string code) => _game.AnswerCaptcha(code);

        public object UsePowerup(string type) => _game.UsePowerup(type);

        public void StopGrabGame() => _game.Stop();

        private static object GameState(GrabGame g) => new
        {
            gameId = g.GameId,
            difficulty = g.Difficulty,
            mode = g.Mode,
            seatClass = g.SeatClass,
            trainNumber = g.TrainNumber,
            from = g.FromStation,
            to = g.ToStation,
            target = g.Mode == "endless" ? 0 : g.Config.Target,
            duration = g.Mode == "endless" ? 60 : g.Config.DurationSeconds,
            bots = g.Bots.Select(b => new { name = b.Name, emoji = b.Emoji }).ToList(),
            seats = g.Seats.Select(s => new { key = s.Key, row = s.Row, col = s.Col, status = s.Status, owner = s.Owner, window = s.IsWindow }).ToList()
        };
    }
}
