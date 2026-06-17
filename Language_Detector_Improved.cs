// ============================================
// C# Language Detector V3 (IMPROVED)
// ============================================
// Input Arguments:
//   in_strText          (String)
//   in_dictKeywordsDE   (Dictionary<String, Integer>)
//   in_dictKeywordsFR   (Dictionary<String, Integer>)
//   in_dictKeywordsIT   (Dictionary<String, Integer>)
//   in_dictKeywordsEN   (Dictionary<String, Integer>)
//   in_dblThreshold     (Double)
// Output Arguments:
//   out_strLanguage     (String)
//   out_strLanguageName (String)
//   out_dblConfidence   (Double)
//   out_strDetails      (String)
// ============================================

using System;
using System.Collections.Generic;
using System.Linq;

public class LanguageDetector
{
    public void DetectLanguage(
        string in_strText,
        Dictionary<string, int> in_dictKeywordsDE,
        Dictionary<string, int> in_dictKeywordsFR,
        Dictionary<string, int> in_dictKeywordsIT,
        Dictionary<string, int> in_dictKeywordsEN,
        double in_dblThreshold,
        out string out_strLanguage,
        out string out_strLanguageName,
        out double out_dblConfidence,
        out string out_strDetails)
    {
        try
        {
            // Initialize outputs
            out_strLanguage = "UNKNOWN";
            out_strLanguageName = "Unknown";
            out_dblConfidence = 0.0;
            out_strDetails = "";

            // Validate inputs
            if (!ValidateInputs(in_strText, in_dictKeywordsDE, in_dictKeywordsFR, in_dictKeywordsIT, in_dictKeywordsEN, out out_strDetails))
            {
                return;
            }

            // Normalize text once
            string textToAnalyze = " " + in_strText.Trim().ToLower() + " ";
            int textLength = textToAnalyze.Length;

            // Define language configurations
            Dictionary<string, LanguageConfig> languageConfigs = new Dictionary<string, LanguageConfig>
            {
                {"DE", new LanguageConfig("German", in_dictKeywordsDE, new char[] {'ä', 'ö', 'ü', 'ß'})},
                {"FR", new LanguageConfig("French", in_dictKeywordsFR, new char[] {'é', 'è', 'ê', 'à', 'ù', 'ç'})},
                {"IT", new LanguageConfig("Italian", in_dictKeywordsIT, new char[] {'à', 'è', 'é', 'ì', 'ò', 'ù'})},
                {"EN", new LanguageConfig("English", in_dictKeywordsEN, new char[] {})}
            };

            // Calculate scores
            Dictionary<string, int> scores = new Dictionary<string, int>();
            Dictionary<string, ScoreBreakdown> scoreDetails = new Dictionary<string, ScoreBreakdown>();

            foreach (var config in languageConfigs)
            {
                string langCode = config.Key;
                ScoreBreakdown breakdown = new ScoreBreakdown();

                // Calculate keyword score
                breakdown.KeywordScore = CalculateKeywordScore(textToAnalyze, config.Value.Keywords);

                // Calculate special character score
                breakdown.SpecialCharScore = CalculateSpecialCharScore(textToAnalyze, config.Value.SpecialChars);

                // Total score
                int totalLangScore = breakdown.KeywordScore + breakdown.SpecialCharScore;
                scores[langCode] = totalLangScore;
                scoreDetails[langCode] = breakdown;
            }

            // Calculate total
            int totalScore = scores.Values.Sum();

            if (totalScore == 0)
            {
                out_strDetails = "No language indicators found in text";
                return;
            }

            // Analyze results
            AnalysisResult analysisResult = AnalyzeScores(scores);

            // Handle tie scenario
            if (analysisResult.IsTie)
            {
                out_strDetails = string.Format("Tie detected between: {0} | Scores: DE={1}, FR={2}, IT={3}, EN={4}",
                    analysisResult.TiedLanguages, scores["DE"], scores["FR"], scores["IT"], scores["EN"]);
                return;
            }

            // Get winner
            string detectedLang = analysisResult.WinnerLang;
            int maxScore = scores[detectedLang];
            int secondBestScore = analysisResult.SecondBestScore;

            // Calculate confidence
            double confidence = CalculateConfidence(maxScore, totalScore, secondBestScore);

            // Apply threshold
            if (confidence >= in_dblThreshold)
            {
                out_strLanguage = detectedLang;
                out_strLanguageName = languageConfigs[detectedLang].LanguageName;
            }
            else
            {
                out_strLanguage = "UNKNOWN";
                out_strLanguageName = "Unknown (Low Confidence)";
            }

            out_dblConfidence = confidence;

            // Build details
            string confidenceLevel = GetConfidenceLevel(confidence);
            out_strDetails = string.Format(
                "DE: {0} (KW:{1} SC:{2}), FR: {3} (KW:{4} SC:{5}), IT: {6} (KW:{7} SC:{8}), EN: {9} (KW:{10} SC:{11}) | Total: {12} | Winner: {13} | Confidence: {14:P1} ({15})",
                scores["DE"], scoreDetails["DE"].KeywordScore, scoreDetails["DE"].SpecialCharScore,
                scores["FR"], scoreDetails["FR"].KeywordScore, scoreDetails["FR"].SpecialCharScore,
                scores["IT"], scoreDetails["IT"].KeywordScore, scoreDetails["IT"].SpecialCharScore,
                scores["EN"], scoreDetails["EN"].KeywordScore, scoreDetails["EN"].SpecialCharScore,
                totalScore, detectedLang, confidence, confidenceLevel);
        }
        catch (Exception ex)
        {
            out_strLanguage = "UNKNOWN";
            out_strLanguageName = "Unknown";
            out_dblConfidence = 0.0;
            out_strDetails = string.Format("Error: {0}", ex.Message);
        }
    }

    // ============================================
    // HELPER CLASSES
    // ============================================

    /// <summary>
    /// Class to hold language configuration
    /// </summary>
    public class LanguageConfig
    {
        public string LanguageName { get; set; }
        public Dictionary<string, int> Keywords { get; set; }
        public char[] SpecialChars { get; set; }

        public LanguageConfig(string name, Dictionary<string, int> keywords, char[] specialChars)
        {
            LanguageName = name;
            Keywords = keywords;
            SpecialChars = specialChars;
        }
    }

    /// <summary>
    /// Class to hold score breakdown
    /// </summary>
    public class ScoreBreakdown
    {
        public int KeywordScore { get; set; } = 0;
        public int SpecialCharScore { get; set; } = 0;
    }

    /// <summary>
    /// Class to hold analysis result
    /// </summary>
    public class AnalysisResult
    {
        public string WinnerLang { get; set; }
        public int SecondBestScore { get; set; }
        public bool IsTie { get; set; }
        public string TiedLanguages { get; set; }
    }

    // ============================================
    // VALIDATION FUNCTIONS
    // ============================================

    private bool ValidateInputs(
        string text,
        Dictionary<string, int> dictDE,
        Dictionary<string, int> dictFR,
        Dictionary<string, int> dictIT,
        Dictionary<string, int> dictEN,
        out string errorMsg)
    {
        errorMsg = "";

        if (string.IsNullOrWhiteSpace(text))
        {
            errorMsg = "Input text is empty or null";
            return false;
        }

        if (dictDE == null || dictFR == null || dictIT == null || dictEN == null)
        {
            errorMsg = "Error: One or more keyword dictionaries are null";
            return false;
        }

        return true;
    }

    // ============================================
    // SCORING FUNCTIONS
    // ============================================

    private int CalculateKeywordScore(string text, Dictionary<string, int> keywords)
    {
        if (keywords == null || keywords.Count == 0)
        {
            return 0;
        }

        int score = 0;

        foreach (var entry in keywords)
        {
            // Use word boundary matching for better accuracy (spaces required)
            string pattern = " " + entry.Key + " ";
            int occurrences = 0;
            int searchIndex = 0;

            while (searchIndex < text.Length)
            {
                int position = text.IndexOf(pattern, searchIndex, StringComparison.OrdinalIgnoreCase);
                if (position == -1)
                    break;

                occurrences++;
                searchIndex = position + pattern.Length;
            }

            score += occurrences * entry.Value;
        }

        return score;
    }

    private int CalculateSpecialCharScore(string text, char[] specialChars)
    {
        const int SPECIAL_CHAR_BONUS = 3;
        int score = 0;

        if (specialChars == null || specialChars.Length == 0)
        {
            return 0;
        }

        foreach (char ch in specialChars)
        {
            int count = 0;
            foreach (char c in text)
            {
                if (c == ch)
                    count++;
            }
            score += count * SPECIAL_CHAR_BONUS;
        }

        return score;
    }

    private double CalculateConfidence(int maxScore, int totalScore, int secondBestScore)
    {
        // Base confidence: proportion of winning language
        double confidence = (double)maxScore / (double)totalScore;

        // Adjustment 1: Score separation from second place
        if (secondBestScore > 0)
        {
            double separation = (double)(maxScore - secondBestScore) / (double)maxScore;
            confidence = confidence * (0.7 + 0.3 * separation);
        }

        // Adjustment 2: Penalize low absolute scores
        if (totalScore < 20)
        {
            confidence = confidence * ((double)totalScore / 20.0);
        }

        // Clamp confidence between 0 and 1
        confidence = Math.Min(1.0, Math.Max(0.0, confidence));

        return confidence;
    }

    // ============================================
    // ANALYSIS & DETAIL FUNCTIONS
    // ============================================

    private AnalysisResult AnalyzeScores(Dictionary<string, int> scores)
    {
        AnalysisResult result = new AnalysisResult();

        if (scores == null || scores.Count == 0)
        {
            result.WinnerLang = "UNKNOWN";
            result.IsTie = false;
            result.SecondBestScore = 0;
            return result;
        }

        // Get sorted scores
        List<KeyValuePair<string, int>> sortedScores = scores.OrderByDescending(x => x.Value).ToList();

        int maxScore = sortedScores[0].Value;
        int winnersCount = scores.Values.Count(s => s == maxScore);

        // Check for tie
        if (winnersCount > 1)
        {
            List<string> tiedLanguages = scores.Where(x => x.Value == maxScore).Select(x => x.Key).ToList();
            result.IsTie = true;
            result.TiedLanguages = string.Join(", ", tiedLanguages);
            result.WinnerLang = "UNKNOWN";
        }
        else
        {
            result.WinnerLang = sortedScores[0].Key;
            result.IsTie = false;
            result.TiedLanguages = "";
        }

        // Get second best score
        result.SecondBestScore = sortedScores.Count > 1 ? sortedScores[1].Value : 0;

        return result;
    }

    private string GetConfidenceLevel(double conf)
    {
        if (conf >= 0.8)
            return "Very High";
        else if (conf >= 0.6)
            return "High";
        else if (conf >= 0.4)
            return "Medium";
        else if (conf >= 0.2)
            return "Low";
        else
            return "Very Low";
    }
}
