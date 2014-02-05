using System;
using System.Collections.Generic;
using System.Windows.Ink;
using System.Windows;

namespace CommandManagement
{
	/// <summary>
	/// Summary description for Command.
	/// </summary>
	public abstract class Command
	{
        /// <summary>
        /// Is this command undoable?
        /// </summary>
        protected bool isUndoable;

		protected CommandManager CM;

		/// <summary>
		/// Execute the Command
        /// Returns true if the execute was sucessful
		/// </summary>
        public abstract bool Execute();

		/// <summary>
		/// Unexecute the Command
        /// Returns true if the undo was sucessful.
		/// </summary>
        public abstract bool UnExecute();

        /// <summary>
        /// Set the contents of the clipboard for this command.
        /// </summary>
        /// <param name="clipboard">The previous contents of the clipboard</param>
        /// <returns>The new contents of the clipboard</returns>
        public virtual KeyValuePair<StrokeCollection, Rect>? SetClipboard(KeyValuePair<StrokeCollection, Rect>? clipboard)
        {
            return clipboard;
        }

		/// <summary>
		/// Is the Command undoable?
		/// </summary>
		/// <returns>True iff the Command is undoable</returns>
        public bool IsUndoable
        {
            get { return isUndoable; }
        }

        /// <summary>
        /// Type of this command, to tell it appart
        /// </summary>
        /// <returns></returns>
        public abstract string Type();
	}
}
