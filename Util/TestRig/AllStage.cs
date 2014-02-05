/*
 * File: AllStage.cs
 *
 * Author: James Brown
 * Harvey Mudd College, Claremont, CA 91711.
 * Sketchers 2008.
 * 
 * Use at your own risk.  This code is not maintained and not guaranteed to work.
 * We take no responsibility for any harm this code may cause.
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace TestRig
{
	class AllStage : ProcessStage
	{
		#region INTERNALS

		private List<ProcessStage> stages;
		private bool _verbose;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Creates a new, non-verbose AllStage
		/// </summary>
		public AllStage()
			: this(false)
		{
			// Nothing to do here
		}

		/// <summary>
		/// Constructs a new AllStage with the given verbosity
		/// </summary>
		/// <param name="verbosity"></param>
		public AllStage(bool verbosity)
		{
			//prerequisite = null;
			//result       = null;

			name = "All";
			shortname = "all";
			inputFiletype = ".xml";

			_verbose = verbosity;

			stages = new List<ProcessStage>();

            stages.Add(new ClassifyStage());
			stages.Add(new GroupStage());
			stages.Add(new SymbolStage());
		}

		#endregion

		#region GETTERS/SETTERS

		public new bool verbose
		{
			get
			{
				return _verbose;
			}
			set
			{
				foreach (ProcessStage ps in stages)
					ps.verbose = value;
				_verbose = value;
			}
		}

		#endregion

		#region ProcessStage implementation

		/// <summary>
		/// Executes the stages on the given sketch
		/// </summary>
		/// <param name="sketch">The sketch to play with</param>
		/// <param name="filename">The filename the sketch came from</param>
		public override void run(Sketch.Sketch sketch, string filename)
		{
			foreach (ProcessStage ps in stages)
			{
				ps.run(sketch, filename);
			}
		}

		public override void finalize(System.IO.TextWriter handle, string path)
		{
			foreach (ProcessStage ps in stages)
			{
				ps.finalize(handle, path);
			}
		}

		#endregion
	}
}
