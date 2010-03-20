// A tagger, which can be used to produce various types of tags (outlining regions, text markers, glyphs, classification
// To use this template, replace all instances of "ITag" with the tag type you want to provide.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace EditorItemTemplates
{
    // TODO: If you want a view tagger provider, replace all instances of ITaggerProvider with IViewTaggerProvider
    [Export(typeof(ITaggerProvider))]
    // TODO: Put an actual tag type here
    [TagType(typeof(ITag))]
    // TODO: Pick a more specific content type, like "csharp", if applicable
    [ContentType("code")]
    class BackgroundTaggerProvider : ITaggerProvider
    {
        public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(buffer)) as ITagger<T>;
        }
    }

    class $safeitemname$ : ITagger<ITag>
    {
        // TODO: Replace this with a more fitting data structure
        SortedList<ITrackingSpan, ITag> tags;

        // The snapshots we've parsed and need to parse
        volatile ITextSnapshot currentSnapshot;
        ITextSnapshot parsedSnapshot;

        ITextBuffer buffer;

        bool updateThreadRunning;
        object updateThreadMutex = new object();

        public $safeitemname$(ITextBuffer buffer)
        {
            this.buffer = buffer;
            tags = new SortedList<ITrackingSpan, ITag>();
            currentSnapshot = buffer.CurrentSnapshot;
            updateThreadRunning = false;

            buffer.Changed += UpdateDataStructureOnBufferChanged;

            PulseUpdateThread();
        }

        void UpdateDataStructureOnBufferChanged(object sender, TextContentChangedEventArgs e)
        {
            currentSnapshot = e.After;

            PulseUpdateThread();
        }

        /// <summary>
        /// Start the update thread again, if it is gone.
        /// </summary>
        void PulseUpdateThread()
        {
            lock (updateThreadMutex)
            {
                if (!updateThreadRunning)
                    ThreadPool.QueueUserWorkItem(UpdateThread);
            }
        }

        /// <summary>
        /// The update thread logic.  The update thread will continue to run as its parses are out of date,
        /// but will exit once it reaches a clean state.  StartUpdateThread will be sure to start a new one
        /// if necessary.
        /// </summary>
        void UpdateThread(object state)
        {
            // First, check to see if another thread has already been started
            lock (updateThreadMutex)
            {
                if (updateThreadRunning)
                    return;

                updateThreadRunning = true;
            }

            while (true)
            {
                lock (updateThreadMutex)
                {
                    if (parsedSnapshot == currentSnapshot)
                    {
                        updateThreadRunning = false;
                        return;
                    }
                }

                ITextSnapshot snapshotToParse = currentSnapshot;
                SortedList<ITrackingSpan, ITag> newTags = new SortedList<ITrackingSpan, ITag>();
                // TODO: Parse work with snapshotToParse, putting new tags in newTags

                parsedSnapshot = snapshotToParse;
                tags = newTags;

                // TODO: Use a snapshot span more specific than the entire snapshot length
                TagsChanged(this, new SnapshotSpanEventArgs(new SnapshotSpan(parsedSnapshot, 0, parsedSnapshot.Length)));
            }
        }

        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            SortedList<ITrackingSpan, T> currentTags = tags;

            if (spans == null || spans.Count == 0 || currentTags == null || currentTags.Count == 0)
                yield break;

            int requestIndex = 0;
            int tagIndex = 0;

            SnapshotSpan currentRequest = spans[requestIndex];

            ITextSnapshot snapshot = currentRequest.Snapshot;
            SnapshotSpan currentTag = currentTags.Keys[tagIndex].GetSpan(snapshot);

            while (requestIndex < spans.Count && tagIndex < currentTags.Count)
            {
                if (currentRequest.Start > currentTag.End)
                {
                    // Skip to the next tag
                    if (++tagIndex < currentTags.Count)
                        currentTag = currentTags.Keys[tagIndex].GetSpan(snapshot);
                }
                else if (currentTag.Start > currentRequest.End)
                {
                    // Skip to the next request
                    if (++requestIndex < spans.Count)
                        currentRequest = spans[requestIndex];
                }
                else
                {
                    // Yield the tag then move to the next
                    if (currentTag.Length > 0)
                    {
                        yield return new TagSpan<T>(currentTag, currentTags.Values[tagIndex]);
                    }
                    if (++tagIndex < currentTags.Count)
                        currentTag = currentTags.Keys[tagIndex].GetSpan(snapshot);
                }
            }
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

        #region Comparer for SortedList<ITrackingSpan, ITag>
        internal class TrackingSpanComparer : IComparer<ITrackingSpan>
        {
            ITextBuffer buffer;

            internal TrackingSpanComparer(ITextBuffer buffer)
            {
                this.buffer = buffer;
            }

            public int Compare(ITrackingSpan x, ITrackingSpan y)
            {
                SnapshotSpan left = x.GetSpan(buffer.CurrentSnapshot);
                SnapshotSpan right = y.GetSpan(buffer.CurrentSnapshot);

                if (left.Start != right.Start)
                    return left.Start.CompareTo(right.Start);
                else
                    return -left.Length.CompareTo(right.Length);
            }
        }
        #endregion
    }
}