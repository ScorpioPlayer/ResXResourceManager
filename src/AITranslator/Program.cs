using ResXManager.Infrastructure;
using ResXManager.Translators;
using System.Globalization;

namespace AITranslator
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var result = await AITranslator.TranslateAsync("zh-CN", "hello world", "i love you");


            Console.WriteLine(result);
        }
    }
    public class AzureOpenAITranslatorConfig
    {
        public string SerializedAuthenticationKey { get; set; }
        public string Url { get; set; }
        public string ModelDeploymentName { get; set; }
        public string ModelName { get; set; }
        public bool BatchRequests { get; set; }
    }
    public static class AITranslator
    {
        public static Task<string[]> TranslateAsync(string target, params string[] texts)
        {
            return TranslateAsync(texts, target, source: null, config: null);
        }
        public static async Task<string[]> TranslateAsync(string[] texts, string target, string? source = null, AzureOpenAITranslatorConfig? config = null)
        {
            var translator = new AzureOpenAITranslator
            {
                SerializedAuthenticationKey = config?.SerializedAuthenticationKey ?? "9002bdd3490d45e3b8ff57df27631f6a",
                Url = config?.Url ?? "https://spopenaius.openai.azure.com/",
                ModelDeploymentName = config?.ModelDeploymentName ?? "gpt35t",
                ModelName = config?.ModelName ?? "gpt-3.5-turbo",
                BatchRequests = config?.BatchRequests ?? true
            };

            var items = texts.Select(c => new TranslationItem(c, target)).ToArray();

            var neutral = CultureKey.Parse("en-US").Culture;
            using var session = new TranslationSession(new(TaskScheduler.Current), source == null ? null : CultureKey.Parse(source).Culture, source == null ? neutral : CultureKey.Parse(source).Culture, items);
            await ((ITranslator)translator).Translate(session).ConfigureAwait(false);

            var results = items.SelectMany(c => c.Results.Select(d => d.TranslatedText)).ToArray();
            if (results.Length == texts.Length)
            {
                return results;
            }
            return null;
        }
    }
    public class TranslationItem : ITranslationItem
    {
        public TranslationItem(string text, string targetCulture)
        {
            this.Source = text;
            this.TargetCulture = new CultureKey(targetCulture);
        }
        public string Source { get; }

        public IList<ITranslationMatch> Results { get; } = new List<ITranslationMatch>();

        public CultureKey TargetCulture { get; }

        public string? Translation { get; private set; }

        public bool Apply(string? valuePrefix, string? commentPrefix)
        {
            return false;
        }

        public IList<(CultureInfo Culture, string Text, string? Comment)> GetAllItems(CultureInfo neutralCulture)
        {
            return new List<(CultureInfo Culture, string Text, string? Comment)> { (TargetCulture.Culture, this.Source, (string)null) };
        }
    }
}