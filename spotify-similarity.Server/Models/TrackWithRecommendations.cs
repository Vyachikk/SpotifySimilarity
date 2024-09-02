using SpotifyTracksApi.Models;

namespace spotify_similarity.Server.Models
{
    public class TrackWithRecommendations
    {
        public Track Track { get; set; }
        public IEnumerable<Track> Recommendations { get; set; }
    }
}
