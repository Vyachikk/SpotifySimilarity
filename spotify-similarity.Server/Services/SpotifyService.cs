using SpotifyTracksApi.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace spotify_similarity.Server.Services
{
    public class SpotifyService
    {
        private readonly IConfiguration _configuration;
        private string _accessToken;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly string TokenEndpoint = "https://accounts.spotify.com/api/token";
        private static readonly string SearchEndpoint = "https://api.spotify.com/v1/search";
        private static readonly string RecommendationsEndpoint = "https://api.spotify.com/v1/recommendations";

        public SpotifyService(IConfiguration configuration)
        {
            _configuration = configuration;
            Task.Run(() => Authenticate()).Wait();
        }

        private async Task Authenticate()
        {
            var clientId = _configuration["Spotify:ClientId"];
            var clientSecret = _configuration["Spotify:ClientSecret"];
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Basic", auth) },
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                })
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Authentication request failed with status code {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            _accessToken = jsonDoc.RootElement.GetProperty("access_token").GetString();
        }

        public async Task<(string Id, string ImageUrl, string PreviewUrl)> SearchTrackAsync(string trackName, string artist)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{SearchEndpoint}?q=track:{trackName} artist:{artist}&type=track")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken) }
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Search request failed with status code {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);
            var items = jsonDoc.RootElement.GetProperty("tracks").GetProperty("items");

            if (items.GetArrayLength() > 0)
            {
                var track = items[0];
                var id = track.GetProperty("id").GetString();
                var imageUrl = track.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString();
                var previewUrl = track.GetProperty("preview_url").GetString();
                return (id, imageUrl, previewUrl);
            }

            return (null, null, null);
        }
        public async Task<IEnumerable<Track>> GetRecommendationsAsync(string trackId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{RecommendationsEndpoint}?seed_tracks={trackId}")
            {
                Headers = { Authorization = new AuthenticationHeaderValue("Bearer", _accessToken) }
            };

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Recommendations request failed with status code {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();
            var jsonDoc = JsonDocument.Parse(json);

            if (!jsonDoc.RootElement.TryGetProperty("tracks", out JsonElement tracks) || tracks.GetArrayLength() == 0)
            {
                return Enumerable.Empty<Track>();
            }

            return tracks.EnumerateArray().Select(track => new Track
            {
                SpotifyId = track.GetProperty("id").GetString(),
                TrackName = track.GetProperty("name").GetString(),
                Artist = track.GetProperty("artists")[0].GetProperty("name").GetString(),
                CoverUrl = track.GetProperty("album").GetProperty("images")[0].GetProperty("url").GetString(),
                PreviewUrl = track.GetProperty("preview_url").GetString()
            });
        }
    }
}
