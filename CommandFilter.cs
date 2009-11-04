// A command filter for the editor.  Command filters get an opportunity to observe and handle commands before and after the editor acts on them.

using System;
using System.ComponentModel.Composition;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace EditorItemTemplates
{
    [Export(typeof(IVsTextViewCreationListener))]
    // TODO: Pick a more specific content type, like "csharp", if applicable
    [ContentType("code")]
    [TextViewRole(PredefinedTextViewRoles.Interactive)]
    class VsTextViewCreationListener : IVsTextViewCreationListener
    {
        public void  VsTextViewCreated(IVsTextView textViewAdapter)
        {
            CommandFilter filter = new CommandFilter();

            IOleCommandTarget next;
            if (ErrorHandler.Succeeded(textViewAdapter.AddCommandFilter(filter, out next)))
                filter.Next = next;
        }
    }

    class CommandFilter : IOleCommandTarget
    {
        /// <summary>
        /// The next command target in the filter chain (provided by <see cref="IVsTextView.AddCommandFilter"/>).
        /// </summary>
        internal IOleCommandTarget Next { get; set; }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            // TODO: Command handling before passing commands to the Next command target

            // Pass the command on to our next command target (if we want it the editor to handle it)
            int hresult = Next.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);

            // TODO: Command handling after passing commands to the Next command target.

            return hresult;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // TODO: If we want to block or enable commands that the editor handles by default, do it before passing commands to Next

            return Next.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }

        #region Private helpers

        /// <summary>
        /// Get the char for a <see cref="VSConstants.VSStd2KCmdID.TYPECHAR"/> command.
        /// </summary>
        /// <param name="pvaIn">The "pvaIn" arg passed to <see cref="Exec"/>.</param>
        char GetTypedChar(IntPtr pvaIn)
        {
            return (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
        }

        #endregion
    }
}
