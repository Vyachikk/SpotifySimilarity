using Microsoft.AspNetCore.Mvc;
using spotify_similarity.Server.Models;
using spotify_similarity.Server.Services;
using SpotifyTracksApi.Models;

[ApiController]
[Route("api/[controller]")]
public class TracksController : ControllerBase
{
    private readonly TrackService _trackService;

    public TracksController(TrackService trackService)
    {
        _trackService = trackService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Track>>> GetTracks(int pageNumber = 1, int pageSize = 10)
    {
        if (pageNumber < 1)
        {
            return BadRequest("Page number must be greater than or equal to 1.");
        }

        if (pageSize < 1)
        {
            return BadRequest("Page size must be greater than or equal to 1.");
        }

        var tracks = await _trackService.GetTracksAsync(pageNumber, pageSize);
        return Ok(tracks);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TrackWithRecommendations>> GetTrackById(int id)
    {
        try
        {
            var (track, recommendations) = await _trackService.GetTrackByIdAsync(id);
            return Ok(new TrackWithRecommendations { Track = track, Recommendations = recommendations });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpGet("{id}/similar")]
    public async Task<ActionResult<IEnumerable<Track>>> GetSimilarTracks(int id, int topN = 50)
    {
        try
        {
            var similarTracks = await _trackService.GetSimilarTracksAsync(id, topN);
            return Ok(similarTracks.Select(t => new { t.track, t.similarity }));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}