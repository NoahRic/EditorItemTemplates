New Item templates for editor extensions for Visual Studio 2010.

Templates
---------

CommandFilter
=============
A template for adding a command filter to `IVsTextView` instances.  A command
filter is a component that gets an opportunity to observe and handle commands
before and after the editor acts on them.

Tagger
======
A *very* simple skeleton for a tagger provider and tagger.  The template uses
a `SimpleTagger<T>` by default for the actual `ITagger<T>` implementation,
with TODOs about replacing it with a more fitting data structure.

BackgroundTagger
================
A more complex skeleton for a tagger provider and tagger that updates its
collection of tags on a background thread, in response to edits on the buffer.
By default, this stores tags in a `SortedList`, which can/should be replaced
with a structure that more naturally aligns with the type of data the tagger
is updating.  This also includes decently accurate code for keeping a
`SortedList<ITrackingSpan, T>`, and code for simultaneously iterating a
`NormalizedSnapshotSpanCollection` and a sorted collection of tracking
spans (in the implementation of `GetTags`).
