﻿namespace BeatLeader_Server.Models {
    public class Leaderboard {
        public string Id { get; set; }
        public string? SongId { get; set; }
        public Song Song { get; set; }
        public DifficultyDescription Difficulty { get; set; }
        public ICollection<Score> Scores { get; set; }
        public ICollection<ScoreContextExtension> ContextExtensions { get; set; }
        public RankQualification? Qualification { get; set; }
        public RankUpdate? Reweight { get; set; }

        public long Timestamp { get; set; }

        public LeaderboardGroup? LeaderboardGroup { get; set; }
        public ICollection<LeaderboardChange>? Changes { get; set; }
        public ICollection<PlayerLeaderboardStats>? PlayerStats { get; set; }

        public ICollection<EventRanking>? Events { get; set; }
        public int Plays { get; set; }
        public int PlayCount { get; set; }

        public int PositiveVotes { get; set; }
        public int StarVotes { get; set; }
        public int NegativeVotes { get; set; }
        public float VoteStars { get; set; }

        public ICollection<ClanRanking>? ClanRanking { get; set; }
        public bool ClanRankingContested { get; set; }
        public void HideRatings() {
            this.Difficulty.AccRating = null;
            this.Difficulty.TechRating = null;
            this.Difficulty.PassRating = null;
            this.Difficulty.Stars = null;

            this.Difficulty.ModifiersRating = null;
        }
    }

    public class LeaderboardGroup {
        public int Id { get; set; }
        public ICollection<Leaderboard> Leaderboards { get; set; }
    }
}
