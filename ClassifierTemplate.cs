// A classifier, which can be used to modify the text properties of a piece of text.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    #region Exports

    static class ClassificationTypeAndFormatDefinitions
    {
#pragma warning disable 0414
        [Export(typeof(ClassificationTypeDefinition))]
        [Name($safeitemname$.CLASSIFICATION)]
        [BaseDefinition(PredefinedClassificationTypeNames.Identifier)]
        static ClassificationTypeDefinition CustomClassificationTypeDefinition = null;
#pragma warning restore 0414

        [Export(typeof(EditorFormatDefinition))]
        [ClassificationType(ClassificationTypeNames = $safeitemname$.CLASSIFICATION)]
        [Name($safeitemname$.CLASSIFICATION)]
        [Order(After = Priority.High)]
        [UserVisible(true)] // If set to true, this item will show up in the Fonts and Colors option page
        sealed class CustomClassificationFormatDefinition : ClassificationFormatDefinition
        {
            public CustomClassificationFormatDefinition()
            {
                this.DisplayName = "Custom Classification Format"; // Name of this item in Fonts and Colors options
                this.IsBold = true;
                this.ForegroundColor = Colors.Blue;
                this.BackgroundColor = Colors.LightGray;
            }
        }
    }

    [Export(typeof(IClassifierProvider))]
    // TODO: Pick a more specific content type, like "csharp", if applicable, or create a custom ContentType definition
    [ContentType("code")]
    class ClassifierProvider : IClassifierProvider
    {
        [Import]
        IClassificationTypeRegistryService TypeRegistry = null;

        public IClassifier GetClassifier(ITextBuffer textBuffer)
        {
            return textBuffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(textBuffer, TypeRegistry));
        }
    }

    #endregion

    class $safeitemname$ : IClassifier
    {
        internal const string CLASSIFICATION = "CustomClassificationName";
        internal const string STRING_TO_MATCH = "TODO";

        ITextBuffer _buffer;
        IClassificationTypeRegistryService _typeRegistry;

        public $safeitemname$(ITextBuffer buffer, IClassificationTypeRegistryService typeRegistry)
        {
            _buffer = buffer;
            _typeRegistry = typeRegistry;

            _buffer.Changed += OnBufferChanged;
        }

        void OnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            // TODO: If a buffer change may affect a different part of the buffer, send an event for these other affected areas.

            var temp = ClassificationChanged;
            if (temp != null)
            {
                foreach (var change in e.Changes)
                {
                    temp(this, new ClassificationChangedEventArgs(new SnapshotSpan(e.After, change.NewSpan)));
                }
            }
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            ITextSnapshot snapshot = span.Snapshot;

            List<ClassificationSpan> classificationSpans = new List<ClassificationSpan>();
            IClassificationType classificationType = _typeRegistry.GetClassificationType(CLASSIFICATION);

            ITextSnapshotLine startLine = span.Start.GetContainingLine();
            int startLineNumber = startLine.LineNumber;
            int endLineNumber = (span.End <= startLine.End) ? startLineNumber : snapshot.GetLineNumberFromPosition(span.End);

            for (int lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                ITextSnapshotLine line = snapshot.GetLineFromLineNumber(lineNumber);

                string text = line.GetText();
                int index = 0;

                while (index < text.Length && -1 != (index = text.IndexOf(STRING_TO_MATCH, index)))
                {
                    SnapshotSpan matchedSpan = new SnapshotSpan(line.Start + index, STRING_TO_MATCH.Length);
                    classificationSpans.Add(new ClassificationSpan(matchedSpan, classificationType));
                    index += STRING_TO_MATCH.Length;
                }
            }

            return classificationSpans;
        }
    }
}
