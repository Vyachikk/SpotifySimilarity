using Microsoft.EntityFrameworkCore;
using SpotifyTracksApi.Models;
using System.Collections.Generic;

namespace spotify_similarity.Server.Data
{
    public class TrackContext : DbContext
    {
        public TrackContext(DbContextOptions<TrackContext> options) : base(options) { }

        public DbSet<Track> Tracks { get; set; }
    }
}
