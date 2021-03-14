/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2018 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;

using KeePassLib.Interfaces;

namespace KPCLib.xunit
{
	/// <summary>
	/// This logger displays the current status in a status bar.
	/// As soon as a warning or error is logged, a dialog is opened
	/// and all consecutive log lines are sent to both loggers
	/// (status bar and dialog).
	/// </summary>
	public sealed class KPCLibLogger : IStatusLogger
	{
		private bool m_bStartedLogging = false;
		private bool m_bEndedLogging = false;

		private List<KeyValuePair<LogStatusType, string>> m_vCachedMessages =
			new List<KeyValuePair<LogStatusType, string>>();

        public KPCLibLogger()
		{
		}

		~KPCLibLogger()
		{
			// Debug.Assert(m_bEndedLogging);

			try { if(!m_bEndedLogging) EndLogging(); }
			catch(Exception) { Debug.Assert(false); }
		}

		public void StartLogging(string strOperation, bool bWriteOperationToLog)
		{
			Debug.Assert(!m_bStartedLogging && !m_bEndedLogging);
            if(strOperation != null)
            {
                Debug.WriteLine(strOperation);
            }

            m_bStartedLogging = true;
			
			if(bWriteOperationToLog)
				m_vCachedMessages.Add(new KeyValuePair<LogStatusType, string>(
					LogStatusType.Info, strOperation));
		}

		public void EndLogging()
		{
			// Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

			m_bEndedLogging = true;
		}

		/// <summary>
		/// Set the current progress in percent.
		/// </summary>
		/// <param name="uPercent">Percent of work finished.</param>
		/// <returns>Returns <c>true</c> if the caller should continue
		/// the current work.</returns>
		public bool SetProgress(uint uPercent)
		{
			// Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

			bool b = true;

			return b;
		}

		/// <summary>
		/// Set the current status text.
		/// </summary>
		/// <param name="strNewText">Status text.</param>
		/// <param name="lsType">Type of the message.</param>
		/// <returns>Returns <c>true</c> if the caller should continue
		/// the current work.</returns>
		public bool SetText(string strNewText, LogStatusType lsType)
		{
			// Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

			m_vCachedMessages.Clear();

			bool b = true;
			m_vCachedMessages.Add(new KeyValuePair<LogStatusType, string>(
				lsType, strNewText));

			return b;
		}

		/// <summary>
		/// Check if the user cancelled the current work.
		/// </summary>
		/// <returns>Returns <c>true</c> if the caller should continue
		/// the current work.</returns>
		public bool ContinueWork()
		{
			// Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

			bool b = true;

			return b;
		}
	}
}
