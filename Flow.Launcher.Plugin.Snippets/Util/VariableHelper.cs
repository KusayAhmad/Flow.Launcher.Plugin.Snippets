using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Flow.Launcher.Plugin.Snippets.Util
{
    /// <summary>
    /// Provides helper methods for extracting, parsing, and replacing variables in text templates.
    /// </summary>
    public static class VariableHelper
    {
        private static readonly Regex VariablePattern = new Regex(@"\{([a-zA-Z_][a-zA-Z0-9_]*)\}", RegexOptions.Compiled);
        
        /// <summary>
        /// Extracts variable names from the text
        /// </summary>
        /// <param name="text">Text containing variables</param>
        /// <returns>List of variable names</returns>
        public static List<string> ExtractVariables(string text)
        {
            if (string.IsNullOrEmpty(text))
                return new List<string>();

            var matches = VariablePattern.Matches(text);
            return matches.Cast<Match>()
                         .Select(m => m.Groups[1].Value)
                         .Distinct()
                         .ToList();
        }

        /// <summary>
        /// Parses variable arguments from the input text
        /// Example: "pod down a=dragon b=test" -> {"a": "dragon", "b": "test"}
        /// </summary>
        /// <param name="queryParts">Query parts</param>
        /// <param name="keywordCount">Number of words in the keyword</param>
        /// <returns>Variable arguments</returns>
        public static Dictionary<string, string> ParseVariableArguments(string[] queryParts, int keywordCount)
        {
            var variables = new Dictionary<string, string>();
            
            if (queryParts.Length <= keywordCount)
                return variables;

            // Parse arguments from the second part onwards
            for (int i = keywordCount; i < queryParts.Length; i++)
            {
                var part = queryParts[i];
                var equalIndex = part.IndexOf('=');
                
                if (equalIndex > 0 && equalIndex < part.Length - 1)
                {
                    var variableName = part.Substring(0, equalIndex);
                    var variableValue = part.Substring(equalIndex + 1);
                    variables[variableName] = variableValue;
                }
            }

            return variables;
        }

        /// <summary>
        /// Replaces variables in the text with specified values
        /// </summary>
        /// <param name="template">Template text</param>
        /// <param name="variables">Variable values</param>
        /// <returns>Text after replacing variables</returns>
        public static string ReplaceVariables(string template, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(template) || variables == null || !variables.Any())
                return template;

            return VariablePattern.Replace(template, match =>
            {
                var variableName = match.Groups[1].Value;
                return variables.TryGetValue(variableName, out var value) ? value : match.Value;
            });
        }

        /// <summary>
        /// Checks if text contains variables
        /// </summary>
        /// <param name="text">Text to check</param>
        /// <returns>true if text contains variables</returns>
        public static bool HasVariables(string text)
        {
            return !string.IsNullOrEmpty(text) && VariablePattern.IsMatch(text);
        }

        /// <summary>
        /// Gets information about required and provided variables
        /// </summary>
        /// <param name="template">Template text</param>
        /// <param name="providedVariables">Provided variables</param>
        /// <returns>Variable status information</returns>
        public static VariableInfo GetVariableInfo(string template, Dictionary<string, string> providedVariables)
        {
            var requiredVariables = ExtractVariables(template);
            var missingVariables = requiredVariables.Where(v => !providedVariables.ContainsKey(v)).ToList();
            
            return new VariableInfo
            {
                RequiredVariables = requiredVariables,
                ProvidedVariables = providedVariables,
                MissingVariables = missingVariables,
                HasAllRequiredVariables = !missingVariables.Any()
            };
        }
    }

    public class VariableInfo
    {
        public List<string> RequiredVariables { get; set; } = new List<string>();
        public Dictionary<string, string> ProvidedVariables { get; set; } = new Dictionary<string, string>();
        public List<string> MissingVariables { get; set; } = new List<string>();
        public bool HasAllRequiredVariables { get; set; }
    }
}