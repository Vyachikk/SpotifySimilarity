using CsvHelper.Configuration.Attributes;
using System.ComponentModel.DataAnnotations;

namespace SpotifyTracksApi.Models
{
    public class Track
    {
        [Key]
        public int Id { get; set; }
        public string TrackName { get; set; }
        public string AlbumName { get; set; }
        public string Artist { get; set; }
        public string ReleaseDate { get; set; }
        public string ISRC { get; set; }
        public int AllTimeRank { get; set; }
        public double TrackScore { get; set; }
        public long SpotifyStreams { get; set; }
        public int SpotifyPlaylistCount { get; set; }
        public long SpotifyPlaylistReach { get; set; }
        public int SpotifyPopularity { get; set; }
        public long YouTubeViews { get; set; }
        public long YouTubeLikes { get; set; }
        public long TikTokPosts { get; set; }
        public long TikTokLikes { get; set; }
        public long TikTokViews { get; set; }
        public long YouTubePlaylistReach { get; set; }
        public int AppleMusicPlaylistCount { get; set; }
        public int AirPlaySpins { get; set; }
        public int SiriusXMSpins { get; set; }
        public int DeezerPlaylistCount { get; set; }
        public long DeezerPlaylistReach { get; set; }
        public int AmazonPlaylistCount { get; set; }
        public long PandoraStreams { get; set; }
        public int PandoraTrackStations { get; set; }
        public long SoundcloudStreams { get; set; }
        public int ShazamCounts { get; set; }
        public int TidalPopularity { get; set; }
        public bool ExplicitTrack { get; set; }
        public string? SpotifyId { get; set; }
        public string? CoverUrl { get; set; }
        public string? PreviewUrl { get; set; }
    }
}