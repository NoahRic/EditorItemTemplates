// A tagger for smart tags.  The given implementation offers a smart tag to replace all instances of "foo" with "bar" or "baz".

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(SmartTag))]
    [ContentType("text")] // TODO: Pick a more specific content type, like "csharp"
    class $safeitemname$Provider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(buffer)) as ITagger<T>;
        }
    }

    class $safeitemname$ : ITagger<SmartTag>
    {
        internal static Regex SearchRegex = new Regex(@"foo", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        internal static string[] Replacements = new string[] { "bar", "baz" };

        ITextBuffer _buffer;

        public $safeitemname$(ITextBuffer buffer)
        {
            _buffer = buffer;

            _buffer.Changed += (sender, args) =>
                {
                    if (args.Changes.Count == 0)
                        return;

                    SnapshotPoint start = new SnapshotPoint(args.After, args.Changes[0].NewPosition);
                    SnapshotPoint end = new SnapshotPoint(args.After, args.Changes[args.Changes.Count - 1].NewEnd);
                    NormalizedSnapshotSpanCollection entire = new NormalizedSnapshotSpanCollection(new SnapshotSpan(start, end));

                    foreach (var line in GetLinesIntersectingSpans(entire))
                        RaiseTagsChanged(line.Extent);
                };
        }

        public IEnumerable<ITagSpan<SmartTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            foreach (var line in GetLinesIntersectingSpans(spans))
            {
                string text = line.GetText();

                foreach (var match in SearchRegex.Matches(text).Cast<Match>())
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(line.Start + match.Index, match.Length);

                    yield return new TagSpan<SmartTag>(matchedSpan, new SmartTag(SmartTagType.Factoid,
                                                                                 CreateActionSets(matchedSpan)));
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #region Helpers

        ReadOnlyCollection<SmartTagActionSet> CreateActionSets(SnapshotSpan span)
        {
            List<SmartTagActionSet> smartTagSets = new List<SmartTagActionSet>();

            ITrackingSpan replaceSpan = span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);

            string word = span.GetText();

            List<ISmartTagAction> actions = new List<ISmartTagAction>();

            foreach (string replaceWith in Replacements)
                actions.Add(new SmartTagAction(replaceSpan, replaceWith));

            smartTagSets.Add(new SmartTagActionSet(actions.AsReadOnly()));
 
            return smartTagSets.AsReadOnly();
        }

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

        void RaiseTagsChanged(SnapshotSpan span)
        {
            var temp = TagsChanged;
            if (temp != null)
                temp(this, new SnapshotSpanEventArgs(span));
        }

        #endregion
    }

    class SmartTagAction : ISmartTagAction
    {
        ITrackingSpan _replaceSpan;
        string _replaceWith;

        public SmartTagAction(ITrackingSpan replaceSpan, string replaceWith)
        {
            _replaceSpan = replaceSpan;
            _replaceWith = replaceWith;
        }

        public ReadOnlyCollection<SmartTagActionSet> ActionSets
        {
            get { return null; }
        }

        public string DisplayText
        {
            get { return _replaceWith; }
        }

        public ImageSource Icon
        {
            get { return null; }
        }

        public void Invoke()
        {
            SnapshotSpan currentSpan = _replaceSpan.GetSpan(_replaceSpan.TextBuffer.CurrentSnapshot);
            _replaceSpan.TextBuffer.Replace(currentSpan, _replaceWith);
        }

        public bool IsEnabled
        {
            get { return true; }
        }
    }
}
