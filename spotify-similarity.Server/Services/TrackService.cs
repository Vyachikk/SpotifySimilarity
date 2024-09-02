using CsvHelper.Configuration;
using CsvHelper;
using SpotifyTracksApi.Models;
using System.Globalization;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Caching.Memory;

namespace spotify_similarity.Server.Services
{
    public class TrackService
    {
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly SpotifyService _spotifyService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<TrackService> _logger;

        public TrackService(IWebHostEnvironment hostingEnvironment, SpotifyService spotifyService, IMemoryCache cache, ILogger<TrackService> logger)
        {
            _hostingEnvironment = hostingEnvironment;
            _spotifyService = spotifyService;
            _cache = cache;
            _logger = logger;
        }

        private double CalculateCosineSimilarity(Track track1, Track track2)
        {
            double dotProduct = track1.SpotifyStreams * track2.SpotifyStreams +
                                track1.YouTubeLikes * track2.YouTubeLikes +
                                track1.TikTokLikes * track2.TikTokLikes;

            double magnitude1 = Math.Sqrt(Math.Pow(track1.SpotifyStreams, 2) +
                                          Math.Pow(track1.YouTubeLikes, 2) +
                                          Math.Pow(track1.TikTokLikes, 2));

            double magnitude2 = Math.Sqrt(Math.Pow(track2.SpotifyStreams, 2) +
                                          Math.Pow(track2.YouTubeLikes, 2) +
                                          Math.Pow(track2.TikTokLikes, 2));

            if (magnitude1 == 0 || magnitude2 == 0)
            {
                return 0;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        private async Task<IEnumerable<Track>> GetTracksFromFileAsync()
        {
            var filePath = Path.Combine(_hostingEnvironment.ContentRootPath, "assets/Most Streamed Spotify Songs 2024.csv");
            using var reader = new StreamReader(filePath);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                BadDataFound = null,
                MissingFieldFound = null,
                HeaderValidated = null
            };
            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<TrackMap>();

            var records = csv.GetRecords<Track>().ToList();

            for (int i = 0; i < records.Count; i++)
            {
                records[i].Id = i + 1;
            }

            return records;
        }

        public async Task<IEnumerable<Track>> GetTracksAsync(int pageNumber, int pageSize)
        {
            var allTracks = await GetTracksFromFileAsync();
            var paginatedTracks = allTracks
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var tasks = paginatedTracks.Select(async track =>
            {
                var (spotifyId, coverUrl, previewUrl) = await _spotifyService.SearchTrackAsync(track.TrackName, track.Artist);
                track.SpotifyId = spotifyId;
                track.CoverUrl = coverUrl;
                track.PreviewUrl = previewUrl;
                return track;
            });

            return await Task.WhenAll(tasks);
        }

        public async Task<(Track Track, IEnumerable<Track> Recommendations)> GetTrackByIdAsync(int id)
        {
            var allTracks = await GetTracksFromFileAsync();
            var track = allTracks.FirstOrDefault(t => t.Id == id);

            if (track != null)
            {
                var (spotifyId, coverUrl, previewUrl) = await _spotifyService.SearchTrackAsync(track.TrackName, track.Artist);
                track.SpotifyId = spotifyId;
                track.CoverUrl = coverUrl;
                track.PreviewUrl = previewUrl;

                var recommendations = await _spotifyService.GetRecommendationsAsync(track.SpotifyId);
                return (track, recommendations);
            }

            return (null, Enumerable.Empty<Track>());
        }

        public async Task<IEnumerable<(Track track, double similarity)>> GetSimilarTracksAsync(int id, int topN)
        {
            var allTracks = await GetTracksFromFileAsync();
            var referenceTrack = allTracks.FirstOrDefault(t => t.Id == id);

            if (referenceTrack == null)
            {
                throw new KeyNotFoundException("Track not found");
            }

            var similarities = allTracks
                .Where(t => t.Id != referenceTrack.Id)
                .Select(t => (track: t, similarity: CalculateCosineSimilarity(referenceTrack, t)))
                .OrderByDescending(t => t.similarity)
                .Take(topN)
                .ToList();

            var trackIds = similarities.Select(t => t.track.Id).ToList();

            var trackDetailsTasks = allTracks
                .Where(t => trackIds.Contains(t.Id))
                .Select(async t =>
                {
                    var (spotifyId, coverUrl, previewUrl) = await _spotifyService.SearchTrackAsync(t.TrackName, t.Artist);
                    t.SpotifyId = spotifyId;
                    t.CoverUrl = coverUrl;
                    t.PreviewUrl = previewUrl;
                    return t;
                }).ToList();

            var tracksWithDetails = await Task.WhenAll(trackDetailsTasks);

            var detailedSimilarities = similarities
                .Select(s =>
                {
                    var trackWithDetails = tracksWithDetails.FirstOrDefault(t => t.Id == s.track.Id);
                    if (trackWithDetails != null)
                    {
                        s.track.SpotifyId = trackWithDetails.SpotifyId;
                        s.track.CoverUrl = trackWithDetails.CoverUrl;
                    }
                    return s;
                });

            return detailedSimilarities;
        }

    }

    public class TrackMap : ClassMap<Track>
    {
        public TrackMap()
        {
            Map(m => m.TrackName).Name("Track");
            Map(m => m.AlbumName).Name("Album Name");
            Map(m => m.Artist).Name("Artist");
            Map(m => m.ReleaseDate).Name("Release Date");
            Map(m => m.ISRC).Name("ISRC");
            Map(m => m.AllTimeRank).Name("All Time Rank").TypeConverter<CustomIntConverter>();
            Map(m => m.TrackScore).Name("Track Score").TypeConverter<CustomDoubleConverter>();
            Map(m => m.SpotifyStreams).Name("Spotify Streams").TypeConverter<CustomLongConverter>();
            Map(m => m.SpotifyPlaylistCount).Name("Spotify Playlist Count").TypeConverter<CustomIntConverter>();
            Map(m => m.SpotifyPlaylistReach).Name("Spotify Playlist Reach").TypeConverter<CustomLongConverter>();
            Map(m => m.SpotifyPopularity).Name("Spotify Popularity").TypeConverter<CustomIntConverter>();
            Map(m => m.YouTubeViews).Name("YouTube Views").TypeConverter<CustomLongConverter>();
            Map(m => m.YouTubeLikes).Name("YouTube Likes").TypeConverter<CustomLongConverter>();
            Map(m => m.TikTokPosts).Name("TikTok Posts").TypeConverter<CustomLongConverter>();
            Map(m => m.TikTokLikes).Name("TikTok Likes").TypeConverter<CustomLongConverter>();
            Map(m => m.TikTokViews).Name("TikTok Views").TypeConverter<CustomLongConverter>();
            Map(m => m.YouTubePlaylistReach).Name("YouTube Playlist Reach").TypeConverter<CustomLongConverter>();
            Map(m => m.AppleMusicPlaylistCount).Name("Apple Music Playlist Count").TypeConverter<CustomIntConverter>();
            Map(m => m.AirPlaySpins).Name("AirPlay Spins").TypeConverter<CustomIntConverter>();
            Map(m => m.SiriusXMSpins).Name("SiriusXM Spins").TypeConverter<CustomIntConverter>();
            Map(m => m.DeezerPlaylistCount).Name("Deezer Playlist Count").TypeConverter<CustomIntConverter>();
            Map(m => m.DeezerPlaylistReach).Name("Deezer Playlist Reach").TypeConverter<CustomLongConverter>();
            Map(m => m.AmazonPlaylistCount).Name("Amazon Playlist Count").TypeConverter<CustomIntConverter>();
            Map(m => m.PandoraStreams).Name("Pandora Streams").TypeConverter<CustomLongConverter>();
            Map(m => m.PandoraTrackStations).Name("Pandora Track Stations").TypeConverter<CustomIntConverter>();
            Map(m => m.SoundcloudStreams).Name("Soundcloud Streams").TypeConverter<CustomLongConverter>();
            Map(m => m.ShazamCounts).Name("Shazam Counts").TypeConverter<CustomIntConverter>();
            Map(m => m.TidalPopularity).Name("TIDAL Popularity").TypeConverter<CustomIntConverter>();
            Map(m => m.ExplicitTrack).Name("Explicit Track").TypeConverter<CustomBoolConverter>();
        }
    }

    public class CustomLongConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) || !long.TryParse(text.Replace(",", "").Replace("\"", ""), out var result))
            {
                return 0L;
            }
            return result;
        }
    }

    public class CustomDoubleConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) || !double.TryParse(text.Replace(",", "").Replace("\"", "").Replace(".", ","), out var result))
            {
                return 0.0;
            }
            return result;
        }
    }

    public class CustomIntConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            if (string.IsNullOrWhiteSpace(text) || !int.TryParse(text.Replace(",", "").Replace("\"", ""), out var result))
            {
                return 0;
            }
            return result;
        }
    }

    public class CustomBoolConverter : DefaultTypeConverter
    {
        public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return int.TryParse(text, out var result) && result == 1;
        }
    }
}
