// A glyph tagger lets you place UI in the glyph/indicator margin, alongside breakpoints and bookmarks.
// The given implementation places icons in the glyph margin for lines that have TODO or DONE in them,
// and lets you flip back and forth between them or insert a new TODO by right clicking in the margin.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    [Export(typeof(ITaggerProvider))]
    [Export(typeof(IGlyphFactoryProvider))]
    [Export(typeof(IGlyphMouseProcessorProvider))]
    [TagType(typeof($safeitemname$Tag))]
    [ContentType("text")]
    [Name("GlyphTagger")]
    [Order(Before = "VsTextMarkerMouseHandler")]
    [Order(Before = "VsTextMarker")]
    class $safeitemname$Provider : ITaggerProvider, IGlyphFactoryProvider, IGlyphMouseProcessorProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$()) as ITagger<T>;
        }

        #region Glyph Margin UI support

        [Import]
        IViewTagAggregatorFactoryService AggregatorFactory = null;
        [Import]
        IEditorOperationsFactoryService OperationsFactory = null;

        public IGlyphFactory GetGlyphFactory(IWpfTextView view, IWpfTextViewMargin margin)
        {
            return new GlyphFactory();
        }

        public IMouseProcessor GetAssociatedMouseProcessor(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin margin)
        {
            var aggregator = AggregatorFactory.CreateTagAggregator<$safeitemname$Tag>(wpfTextViewHost.TextView);
            var operations = OperationsFactory.GetEditorOperations(wpfTextViewHost.TextView);

            string commentString;
            if (wpfTextViewHost.TextView.TextDataModel.ContentType.IsOfType("basic"))
                commentString = "' TODO: ";
            else if (wpfTextViewHost.TextView.TextDataModel.ContentType.IsOfType("html"))
                commentString = "<!-- TODO: -->";
            else
                commentString = "// TODO: ";

            return new GlyphMouseProcessor(commentString, wpfTextViewHost.TextView, margin, operations, aggregator);
        }

        class GlyphMouseProcessor : MouseProcessorBase
        {
            string _commentString;
            ITextView _textView;
            IWpfTextViewMargin _glyphMargin;
            IEditorOperations _operations;
            ITagAggregator<$safeitemname$Tag> _aggregator;

            public GlyphMouseProcessor(string commentString, ITextView textView, IWpfTextViewMargin glyphMargin,
                                       IEditorOperations operations, ITagAggregator<$safeitemname$Tag> aggregator)
            {
                _commentString = commentString;
                _textView = textView;
                _glyphMargin = glyphMargin;
                _operations = operations;
                _aggregator = aggregator;
            }

            public override void PreprocessMouseUp(MouseButtonEventArgs e)
            {
                if (e.ClickCount > 1)
                    return;

                double y = e.GetPosition(_glyphMargin.VisualElement).Y + _textView.ViewportTop;
                var viewLine = _textView.TextViewLines.GetTextViewLineContainingYCoordinate(y);

                if (viewLine == null)
                    return;

                var tag = _aggregator.GetTags(viewLine.ExtentAsMappingSpan).FirstOrDefault();

                // Right or middle click on a blank line will insert a new "TODO"
                if (tag == null && (e.ChangedButton == MouseButton.Middle || e.ChangedButton == MouseButton.Right))
                {
                    _operations.MoveCaret(viewLine, 0, false);
                    _operations.MoveToHome(false);
                    _operations.OpenLineAbove();
                    _operations.InsertText(_commentString);

                    e.Handled = true;
                }
                else if (tag != null)
                {
                    string newText = tag.Tag.Done ? "TODO" : "DONE";
                    var span = tag.Tag.TextSpan.GetSpan(tag.Tag.TextSpan.TextBuffer.CurrentSnapshot);

                    span.Snapshot.TextBuffer.Replace(span, newText);

                    e.Handled = true;
                }
            }
        }

        class GlyphFactory : IGlyphFactory
        {
            static Geometry TodoGlyphGeometry = StreamGeometry.Parse("M 1,1 15,1 15,15 1,15 1,1");
            static Geometry DoneGlyphGeometry = StreamGeometry.Parse("M 1,1 15,1 15,15 1,15 1,1 M 4,9 7,12 12,4");

            public UIElement GenerateGlyph(IWpfTextViewLine line, IGlyphTag tag)
            {
                $safeitemname$Tag myTag = tag as $safeitemname$Tag;
                if (myTag != null)
                {
                    Geometry geometry = myTag.Done ? DoneGlyphGeometry : TodoGlyphGeometry;
                    return new Path() { Data = geometry, Stroke = myTag.Done ? Brushes.DarkGreen : Brushes.Red };
                }

                return null;
            }
        }

        #endregion
    }

    class $safeitemname$Tag : IGlyphTag
    {
        public $safeitemname$Tag(bool done, ITrackingSpan textSpan)
        {
            Done = done;
            TextSpan = textSpan;
        }

        public ITrackingSpan TextSpan { get; private set; }
        public bool Done { get; private set; }
    }

    sealed class $safeitemname$ : ITagger<$safeitemname$Tag>
    {
        public IEnumerable<ITagSpan<$safeitemname$Tag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            if (spans == null || spans.Count == 0)
                yield break;

            foreach (var line in GetLinesIntersectingSpans(spans))
            {
                string text = line.GetText();
                int index = -1;
                bool done = false;

                if (-1 != (index = text.IndexOf("TODO")))
                    done = false;
                else if (-1 != (index = text.IndexOf("DONE")))
                    done = true;

                if (index != -1)
                {
                    SnapshotSpan span = new SnapshotSpan(line.Start + index, 4);
                    ITrackingSpan trackingSpan = line.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);

                    yield return new TagSpan<$safeitemname$Tag>(span, new $safeitemname$Tag(done, trackingSpan));
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