﻿using BeatLeader_Server.Models;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Sandbox.Queries;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace BeatLeader_Server.Services;

public static class PlayerSearchService
{
    private const int HitsLimit = 1000;
    private static readonly string LuceneDir = Path.Combine(System.AppContext.BaseDirectory, "lucene_index_players");

    private static FSDirectory Directory { get; } = FSDirectory.Open(new DirectoryInfo(LuceneDir));

    public static void AddNewPlayers(IEnumerable<Player> players)
    {
        lock (Directory)
        {
            using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new(LuceneVersion.LUCENE_48, analyzer);
            using IndexWriter writer = new(Directory, config);

            foreach (PlayerMetadata playerMetadata in players)
            {
                AddToLuceneIndex(playerMetadata, writer);
            }

            writer.Commit();
        }
    }

    public static void AddNewPlayer(Player player)
    {
        lock (Directory)
        {
            using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new(LuceneVersion.LUCENE_48, analyzer);
            using IndexWriter writer = new(Directory, config);

            AddToLuceneIndex((PlayerMetadata)player, writer);

            writer.Commit();
        }
    }

    public static void PlayerChangedName(Player player)
    {
        lock (Directory)
        {
            using StandardAnalyzer analyzer = new(LuceneVersion.LUCENE_48);
            IndexWriterConfig config = new(LuceneVersion.LUCENE_48, analyzer);
            using IndexWriter writer = new(Directory, config);

            using DirectoryReader directoryReader = DirectoryReader.Open(Directory);
            IndexSearcher searcher = new(directoryReader);

            Term playerMetadataTerm = new(nameof(PlayerMetadata.Id), player.Id);
            TermQuery searchQuery = new(playerMetadataTerm);

            TopFieldDocs topFieldDocs = searcher.Search(searchQuery, null, HitsLimit, Sort.RELEVANCE);

            PlayerMetadata playerMetadata = (PlayerMetadata)searcher.Doc(topFieldDocs.ScoreDocs[0].Doc);
            playerMetadata.Names.Add(player.Name);

            writer.UpdateDocument(playerMetadataTerm, (Document)playerMetadata);
            writer.Commit();
        }
    }

    public static List<PlayerMetadata> Search(string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery.Replace("*", "").Replace("?", "")))
        {
            return new List<PlayerMetadata>(0);
        }

        using DirectoryReader directoryReader = DirectoryReader.Open(Directory);
        IndexSearcher searcher = new(directoryReader);

        Query query = GetQuery(searchQuery);

        TopFieldDocs topFieldDocs = searcher.Search(query, null, HitsLimit, Sort.RELEVANCE, true, false);
        ScoreDoc[] hits = topFieldDocs.ScoreDocs;

        return hits.Select(scoreDoc =>
        {
            PlayerMetadata result = (PlayerMetadata)searcher.Doc(scoreDoc.Doc);
            result.Score = scoreDoc.Score;

            return result;
        }).ToList();
    }

    private static Query GetQuery(string searchQuery)
    {
        searchQuery = searchQuery.ToLower();

        Term namesTerm = new(nameof(PlayerMetadata.Names), searchQuery);

        Query prefixQuery = new PrefixQuery(namesTerm);
        Query fuzzyPrefix = new FuzzyQuery(namesTerm, 2, searchQuery.Length);
        Query hardFuzzyQuery = new FuzzyQuery(namesTerm, 2, 3);
        Query softFuzzyQuery = new SlowFuzzyQuery(namesTerm, 0.7f);
        Query wildcardQuery = new WildcardQuery(new Term(nameof(PlayerMetadata.Names), "*" + searchQuery + "*"));

        Query fuzzyPrefixBoost = new BoostingQuery(prefixQuery, fuzzyPrefix, 2);
        Query hardFuzzyPrefixBoost = new BoostingQuery(fuzzyPrefixBoost, hardFuzzyQuery, 2);
        Query softHardFuzzyPrefixBoost = new BoostingQuery(hardFuzzyPrefixBoost, softFuzzyQuery, 2);

        BooleanQuery booleanQuery = new()
        {
            { new PrefixQuery(new Term(nameof(PlayerMetadata.Id), searchQuery)), Occur.SHOULD },
            { softHardFuzzyPrefixBoost, Occur.SHOULD },
            { softFuzzyQuery, Occur.SHOULD },
            { wildcardQuery, Occur.SHOULD },
        };

        if (searchQuery.Contains(' ') || searchQuery.Contains('_') || searchQuery.Contains('-'))
        {
            string[] words = searchQuery.Split(' ', '_', '-');

            FuzzyLikeThisQuery fuzzyWordsQuery = new(words.Length, new StandardAnalyzer(LuceneVersion.LUCENE_48));
            fuzzyWordsQuery.AddTerms(searchQuery, nameof(PlayerMetadata.Names), 0.6f, 1);

            booleanQuery.Add(fuzzyWordsQuery, Occur.SHOULD);
        }

        return booleanQuery;
    }

    private static void AddToLuceneIndex(PlayerMetadata playerMetadata, IndexWriter writer)
    {
        Term playerMetadataTerm = new(nameof(PlayerMetadata.Id), playerMetadata.Id);

        writer.UpdateDocument(playerMetadataTerm, (Document)playerMetadata);
    }
}