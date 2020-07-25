﻿namespace ResXManager.Translators
{
    using System;
    using System.Collections.Generic;
    using System.Composition;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.Serialization;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;

    using ResXManager.Infrastructure;

    using TomsToolbox.Wpf.Composition.AttributedModel;

    [DataTemplate(typeof(MyMemoryTranslator))]
    public class MyMemoryTranslatorConfiguration : Decorator
    {
    }

    [Export(typeof(ITranslator)), Shared]
    public class MyMemoryTranslator : TranslatorBase
    {
        private static readonly Uri _uri = new Uri("http://mymemory.translated.net/doc");

        public MyMemoryTranslator()
            : base("MyMemory", "MyMemory", _uri, GetCredentials())
        {
        }

        private static IList<ICredentialItem> GetCredentials()
        {
            return new ICredentialItem[] { new CredentialItem("Key", "Key") };
        }

        [DataMember(Name = "Key")]
        public string? SerializedKey
        {
            get => SaveCredentials ? Credentials[0].Value : null;
            set => Credentials[0].Value = value;
        }

        private string? Key => Credentials[0].Value;

        protected override async Task Translate(ITranslationSession translationSession)
        {
            using (var client = new HttpClient())
            {

                foreach (var item in translationSession.Items)
                {
                    if (translationSession.IsCanceled)
                        break;

                    var translationItem = item;

                    var targetCulture = translationItem.TargetCulture.Culture ?? translationSession.NeutralResourcesLanguage;
                    var result = await TranslateTextAsync(client, translationItem.Source, Key, translationSession.SourceLanguage, targetCulture, translationSession.CancellationToken).ConfigureAwait(false);

                    await translationSession.MainThread.StartNew(() =>
                    {
                        if (result?.Matches != null)
                        {
                            foreach (var match in result.Matches)
                            {
                                var translation = match.Translation;
                                if (string.IsNullOrEmpty(translation))
                                    continue;

                                translationItem.Results.Add(new TranslationMatch(this, translation, Ranking * match.Match.GetValueOrDefault() * match.Quality.GetValueOrDefault() / 100.0));
                            }
                        }
                        else
                        {
                            var translation = result?.ResponseData?.TranslatedText;
                            if (!string.IsNullOrEmpty(translation))
                            {
                                translationItem.Results.Add(new TranslationMatch(this, translation, Ranking * result?.ResponseData?.Match.GetValueOrDefault() ?? 0));
                            }
                        }
                    }).ConfigureAwait(false);
                }
            }
        }

        private static async Task<Response?> TranslateTextAsync(HttpClient client, string input, string? key, CultureInfo sourceLanguage, CultureInfo targetLanguage, CancellationToken cancellationToken)
        {
            var rawInput = RemoveKeyboardShortcutIndicators(input);

            var url = string.Format(CultureInfo.InvariantCulture,
                "http://api.mymemory.translated.net/get?q={0}!&langpair={1}|{2}",
                WebUtility.UrlEncode(rawInput),
                sourceLanguage, targetLanguage);

            if (!string.IsNullOrEmpty(key))
                url += string.Format(CultureInfo.InvariantCulture, "&key={0}", WebUtility.UrlEncode(key));

            var response = await client.GetAsync(new Uri(url, UriKind.RelativeOrAbsolute), cancellationToken).ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            using (var reader = new StreamReader(await response.Content.ReadAsStreamAsync().ConfigureAwait(false), Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Response>(json);
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class ResponseData
        {
            [DataMember(Name = "translatedText")]
            public string? TranslatedText
            {
                get;
                set;
            }

            [DataMember(Name = "match")]
            public double? Match
            {
                get;
                set;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class MatchData
        {
            [DataMember(Name = "translation")]
            public string? Translation
            {
                get;
                set;
            }

            [DataMember(Name = "quality")]
            public double? Quality
            {
                get;
                set;
            }

            [DataMember(Name = "match")]
            public double? Match
            {
                get;
                set;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses")]
        [DataContract]
        private class Response
        {
            [DataMember(Name = "responseData")]
            public ResponseData? ResponseData
            {
                get;
                set;
            }

            [DataMember(Name = "matches")]
            public MatchData[]? Matches
            {
                get;
                set;
            }
        }
    }
}