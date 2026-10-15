namespace InterFullMarkt.WebUI.Controllers;

using Microsoft.AspNetCore.Mvc;
using MediatR;
using System.Text.Json;
using InterFullMarkt.Application.Features.Players.Queries.GetPlayerById;

public class ScoutController : Controller
{
    private readonly IMediator _mediator;
    private const string WatchlistSessionKey = "ScoutWatchlist";
    private const string NotesSessionKey = "ScoutNotes";

    public ScoutController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Watchlist()
    {
        var playerIds = GetWatchlist();
        var players = new List<GetPlayerByIdResult>();
        var notes = GetScoutNotes();

        foreach (var id in playerIds)
        {
            try
            {
                var player = await _mediator.Send(new GetPlayerByIdQuery(id));
                players.Add(player);
            }
            catch
            {
                // Ignore missing or deleted players
            }
        }

        ViewBag.Notes = notes;
        return View(players);
    }

    [HttpPost]
    public IActionResult ToggleWatchlist(Guid playerId)
    {
        var watchlist = GetWatchlist();
        bool added;

        if (watchlist.Contains(playerId))
        {
            watchlist.Remove(playerId);
            added = false;
        }
        else
        {
            watchlist.Add(playerId);
            added = true;
        }

        SaveWatchlist(watchlist);
        return Json(new { success = true, added = added, count = watchlist.Count });
    }

    [HttpGet]
    public IActionResult GetWatchlistCount()
    {
        var watchlist = GetWatchlist();
        return Json(new { count = watchlist.Count });
    }

    [HttpPost]
    public IActionResult SaveNote(Guid playerId, string note)
    {
        var notes = GetScoutNotes();
        notes[playerId.ToString()] = note ?? string.Empty;
        SaveScoutNotes(notes);
        return Json(new { success = true });
    }

    private List<Guid> GetWatchlist()
    {
        var data = HttpContext.Session.GetString(WatchlistSessionKey);
        return string.IsNullOrEmpty(data) ? new List<Guid>() : JsonSerializer.Deserialize<List<Guid>>(data)!;
    }

    private void SaveWatchlist(List<Guid> list)
    {
        HttpContext.Session.SetString(WatchlistSessionKey, JsonSerializer.Serialize(list));
    }

    private Dictionary<string, string> GetScoutNotes()
    {
        var data = HttpContext.Session.GetString(NotesSessionKey);
        return string.IsNullOrEmpty(data) ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(data)!;
    }

    private void SaveScoutNotes(Dictionary<string, string> notes)
    {
        HttpContext.Session.SetString(NotesSessionKey, JsonSerializer.Serialize(notes));
    }
}
