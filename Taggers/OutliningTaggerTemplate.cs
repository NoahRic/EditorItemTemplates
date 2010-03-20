// An outlining tagger.
// The given implementation reads plaintext files for lines that start with #

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    [Export(typeof(ITaggerProvider))]
    [TagType(typeof(IOutliningRegionTag))]
    [ContentType("plaintext")]
    class $safeitemname$Provider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(buffer)) as ITagger<T>;
        }
    }

    sealed class $safeitemname$ : ITagger<IOutliningRegionTag>
    {
        ITextBuffer _buffer;
        List<ITrackingSpan> _sections;

        public $safeitemname$(ITextBuffer buffer)
        {
            _buffer = buffer;
            _sections = new List<ITrackingSpan>();
            _buffer.Changed += BufferChanged;

            ReparseFile();
        }

        public IEnumerable<ITagSpan<IOutliningRegionTag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (_sections == null || _sections.Count == 0 || spans.Count == 0)
                yield break;

            ITextSnapshot snapshot = spans[0].Snapshot;

            foreach (var section in _sections)
            {
                var sectionSpan = section.GetSpan(snapshot);

                if (spans.IntersectsWith(new NormalizedSnapshotSpanCollection(sectionSpan)))
                {
                    string firstLine = sectionSpan.Start.GetContainingLine().GetText().Trim(' ', '\t');

                    string collapsedHintText;
                    if (sectionSpan.Length > 250)
                        collapsedHintText = snapshot.GetText(sectionSpan.Start, 247) + "...";
                    else
                        collapsedHintText = sectionSpan.GetText();

                    var tag = new OutliningRegionTag(firstLine, collapsedHintText);
                    yield return new TagSpan<IOutliningRegionTag>(sectionSpan, tag);
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        DispatcherTimer _timer;
        void BufferChanged(object s, TextContentChangedEventArgs e)
        {
            if (_timer != null)
            {
                _timer.Stop();
            }
            else
            {
                _timer = new DispatcherTimer(DispatcherPriority.ApplicationIdle)
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };

                _timer.Tick += (sender, args) =>
                {
                    if (_timer != null)
                        _timer.Stop();

                    ReparseFile();
                };
            }

            _timer.Start();
        }

        #region Parsing

        void ReparseFile()
        {
            ITextSnapshot snapshot = _buffer.CurrentSnapshot;

            _sections = ParseSections(snapshot);

            var temp = TagsChanged;
            if (temp != null)
                temp(this, new SnapshotSpanEventArgs(new SnapshotSpan(snapshot, 0, snapshot.Length)));
        }

        List<ITrackingSpan> ParseSections(ITextSnapshot snapshot)
        {
            List<SnapshotSpan> sections = new List<SnapshotSpan>();
            SnapshotPoint? lastStartPoint = null;

            foreach (var line in snapshot.Lines)
            {
                if (line.Length > 0 && line.Start.GetChar() == '#')
                {
                    if (lastStartPoint != null && line.LineNumber > 0)
                    {
                        SnapshotPoint endOfPreviousLine = snapshot.GetLineFromLineNumber(line.LineNumber - 1).End;
                        sections.Add(new SnapshotSpan(lastStartPoint.Value, endOfPreviousLine));
                    }

                    lastStartPoint = line.Start;
                }
            }

            if (lastStartPoint != null)
                sections.Add(new SnapshotSpan(lastStartPoint.Value, new SnapshotPoint(snapshot, snapshot.Length)));

            return sections.Select(s => snapshot.CreateTrackingSpan(s, SpanTrackingMode.EdgeExclusive)).ToList();
        }

        #endregion
    }
}
