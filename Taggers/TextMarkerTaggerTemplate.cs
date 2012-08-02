// A text marker tagger, which allows you to draw a background marker under text, like
// breakpoints.

// NOT DONE.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IUrlTag))]
    [ContentType("text")]
    class $safeitemname$Provider : ITaggerProvider
    {
        public ITagger<T>  CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(buffer)) as ITagger<T>;
        }
    }

    class $safeitemname$ : ITagger<IUrlTag>
    {
        internal static Regex SearchRegex = new Regex(@"bing(:(\S*))?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal const string URL_STRING = "http://www.bing.com/search?q=";

        ITextBuffer _buffer;

        public $safeitemname$(ITextBuffer buffer)
        {
            _buffer = buffer;
        }

        public IEnumerable<ITagSpan<IUrlTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            foreach (var line in GetLinesIntersectingSpans(spans))
            {
                string text = line.GetText();

                foreach (var match in SearchRegex.Matches(text).Cast<Match>())
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(line.Start + match.Index, match.Length);

                    string url = URL_STRING;
                    if (match.Groups.Count > 1)
                        url += match.Groups[2].ToString();

                    yield return new TagSpan<IUrlTag>(matchedSpan, new UrlTag(new Uri(url)));
                }
            }
        }

#pragma warning disable 67
        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
#pragma warning restore 67

        #region Helpers

        IEnumerable<ITextSnapshotLine> GetLinesIntersectingSpans(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            ITextSnapshotLine line = null;
            foreach (SnapshotSpan span in spans)
            {
                if (line != null && line.EndIncludingLineBreak >= span.End)
                    continue;

                if (line == null || line.EndIncludingLineBreak <= span.Start)
                {
                    line = span.Start.GetContainingLine();
                    yield return line;
                }

                while (line.EndIncludingLineBreak < span.End && line.LineNumber < line.Snapshot.LineCount)
                {
                    line = line.EndIncludingLineBreak.GetContainingLine();
                    yield return line;
                }
            }
        }

        #endregion
    }
}
