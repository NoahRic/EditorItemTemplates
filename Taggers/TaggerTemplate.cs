// A tagger, which can be used to produce various types of tags (outlining regions, text markers, glyphs, classification, etc.)
// To use this template, replace all instances of "ITag" with the tag type you want to provide.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

namespace $rootnamespace$
{
    [Export(typeof(ITaggerProvider))]
    // TODO: Put an actual tag type here
    [TagType(typeof(ITag))]
    // TODO: Pick a more specific content type, like "csharp", if applicable
    [ContentType("code")]
    class TaggerProvider : ITaggerProvider
    {
        public ITagger<T>  CreateTagger<T>(ITextBuffer buffer) where T : ITag
        {
            return buffer.Properties.GetOrCreateSingletonProperty(() => new $safeitemname$(buffer)) as ITagger<T>;
        }
    }

    class $safeitemname$ : ITagger<ITag>
    {
        SimpleTagger<ITag> simpleTagger;

        public $safeitemname$(ITextBuffer buffer)
        {
            simpleTagger = new SimpleTagger<ITag>(buffer);

            simpleTagger.TagsChanged += (sender, args) =>
                {
                    var temp = TagsChanged;
                    if (temp != null)
                        TagsChanged(this, args);
                };
        }

        public IEnumerable<ITagSpan<ITag>> GetTags(NormalizedSnapshotSpanCollection spans)
        {
            return simpleTagger.GetTags(spans);
        }

        public event EventHandler<SnapshotSpanEventArgs> TagsChanged;
    }
}
