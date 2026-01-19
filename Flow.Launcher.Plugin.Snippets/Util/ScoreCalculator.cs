using System;
using System.Collections.Generic;

namespace Flow.Launcher.Plugin.Snippets.Util;

/// <summary>
/// Advanced scoring system for snippet ranking
/// </summary>
public static class ScoreCalculator
{
    /// <summary>
    /// Calculate comprehensive score for a snippet based on multiple factors
    /// </summary>
    /// <param name="snippet">The snippet model to score</param>
    /// <param name="searchQuery">The search query string</param>
    /// <param name="hasVariables">Whether snippet contains variables</param>
    /// <param name="variableCount">Number of variables in snippet</param>
    /// <param name="allVariablesProvided">Whether all required variables are provided</param>
    /// <returns>Calculated score</returns>
    public static int CalculateScore(
        SnippetModel snippet, 
        string searchQuery, 
        bool hasVariables = false,
        int variableCount = 0,
        bool allVariablesProvided = false)
    {
        int score = snippet.Score; // Base score from DB
        
        // A) Match quality score (0-20)
        score += CalculateMatchQualityScore(snippet.Key, searchQuery);
        
        // B) Usage frequency score (0-15)
        score += CalculateUsageFrequencyScore(snippet.UsageCount);
        
        // C) Recency score (0-10)
        score += CalculateRecencyScore(snippet.LastUsedTime);
        
        // D) Variable completeness score (-5 to +10)
        if (hasVariables)
        {
            score += CalculateVariableCompletenessScore(allVariablesProvided);
        }
        
        // E) Complexity penalty (0 to -5)
        if (hasVariables && variableCount > 0)
        {
            score += CalculateComplexityPenalty(variableCount);
        }
        
        // F) Favorite boost (0 or +25)
        if (snippet.IsFavorite)
        {
            score += 25;
        }
        
        return score;
    }
    
    /// <summary>
    /// Calculate score based on how well the search query matches the snippet key
    /// </summary>
    private static int CalculateMatchQualityScore(string key, string searchQuery)
    {
        if (string.IsNullOrEmpty(searchQuery))
            return 0;
            
        var keyLower = key?.ToLower() ?? "";
        var queryLower = searchQuery.ToLower();
        
        // Exact match
        if (keyLower == queryLower)
            return 20;
        
        // Starts with query
        if (keyLower.StartsWith(queryLower))
            return 15;
        
        // Contains query
        if (keyLower.Contains(queryLower))
            return 10;
        
        // Fuzzy match (contains all characters in order)
        if (FuzzyMatch(keyLower, queryLower))
            return 5;
        
        return 0;
    }
    
    /// <summary>
    /// Calculate score based on usage frequency
    /// </summary>
    private static int CalculateUsageFrequencyScore(int usageCount)
    {
        if (usageCount >= 10)
            return 15;
        else if (usageCount >= 5)
            return 10;
        else if (usageCount >= 1)
            return 5;
        
        return 0;
    }
    
    /// <summary>
    /// Calculate score based on recency of last use
    /// </summary>
    private static int CalculateRecencyScore(DateTime? lastUsedTime)
    {
        if (!lastUsedTime.HasValue)
            return 0;
        
        var timeSinceLastUse = DateTime.Now - lastUsedTime.Value;
        
        if (timeSinceLastUse.TotalHours <= 1)
            return 10;
        else if (timeSinceLastUse.TotalDays <= 1)
            return 7;
        else if (timeSinceLastUse.TotalDays <= 7)
            return 5;
        else if (timeSinceLastUse.TotalDays <= 30)
            return 3;
        
        return 0;
    }
    
    /// <summary>
    /// Calculate score based on variable completeness
    /// </summary>
    private static int CalculateVariableCompletenessScore(bool allVariablesProvided)
    {
        return allVariablesProvided ? 10 : -5;
    }
    
    /// <summary>
    /// Calculate penalty based on number of variables (complexity)
    /// </summary>
    private static int CalculateComplexityPenalty(int variableCount)
    {
        if (variableCount >= 5)
            return -5;
        else if (variableCount >= 3)
            return -2;
        
        return 0; // 1-2 variables: no penalty
    }
    
    /// <summary>
    /// Check if all characters of query appear in key in order
    /// </summary>
    private static bool FuzzyMatch(string key, string query)
    {
        int queryIndex = 0;
        
        foreach (char c in key)
        {
            if (queryIndex < query.Length && c == query[queryIndex])
            {
                queryIndex++;
            }
        }
        
        return queryIndex == query.Length;
    }
}
