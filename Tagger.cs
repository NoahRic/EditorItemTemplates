// A tagger, which can be used to produce various types of tags (outlining regions, text markers, glyphs, classification

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using System.Collections.Generic;

namespace EditorItemTemplates
{
    // TODO: If you want a view tagger provider, replace all instances of ITaggerProvider with IViewTaggerProvider
    [Export(typeof(ITaggerProvider))]
    // TODO: Put an actual tag type here
    [TagType(typeof(ITag))]
    // TODO: Pick a more specific content type, like "csharp", if applicable
    [ContentType("code")]
    class TaggerProvider : ITaggerProvider
    {
        public ITagger<T>  CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
         	throw new NotImplementedException();
        }
    }

    class Tagger<T> : ITagger<T> where T: ITag
    {
        SimpleTagger<T> simpleTagger;

        public Tagger(ITextBuffer buffer)
        {
            simpleTagger = new SimpleTagger<T>(buffer);

            simpleTagger.TagsChanged += (sender, args) => TagsChanged(this, args);
        }

        public IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return simpleTagger.GetTags(spans);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
