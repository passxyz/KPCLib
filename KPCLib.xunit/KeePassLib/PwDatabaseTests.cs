using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Xunit;
using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Serialization;

namespace KPCLib.xunit
{
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

            try { if (!m_bEndedLogging) EndLogging(); }
            catch (Exception) { Debug.Assert(false); }
        }

        public void StartLogging(string strOperation, bool bWriteOperationToLog)
        {
            Debug.Assert(!m_bStartedLogging && !m_bEndedLogging);
            if (strOperation != null)
            {
                Debug.WriteLine(strOperation);
            }

            m_bStartedLogging = true;

            if (bWriteOperationToLog)
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

    public class KeePassFixture : IDisposable
    {
        const string TEST_DB = "utdb.kdbx";
        const string TEST_DB_KEY = "12345";

        public KeePassFixture()
        {
            Logger = new KPCLibLogger();
            PwDb = new PwDatabase();
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(TEST_DB);
            CompositeKey cmpKey = new CompositeKey();
            cmpKey.AddUserKey(new KcpPassword(TEST_DB_KEY));
            PwDb.Open(ioc, cmpKey, Logger);
        }

        public void Dispose()
        {
            PwDb.Close();
        }

        public PwDatabase PwDb { get; private set; }
        public KPCLibLogger Logger { get; private set; }
    }

    [CollectionDefinition("PwDatabase collection")]
    public class PwDatabaseCollection : ICollectionFixture<KeePassFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PwDatabase collection")]
    public class PwDatabaseTests
    {
        KeePassFixture keepass;

        public PwDatabaseTests(KeePassFixture fixture)
        {
            this.keepass = fixture;
        }

        [Fact]
        public void IsOpenDbTest()
        {
            Debug.WriteLine($"Name={keepass.PwDb.Name}, Description={keepass.PwDb.Description}");
            Assert.True((keepass.PwDb.IsOpen));
        }

        [Fact]
        public void ListGroupsTests()
        {
            PwGroup pg = keepass.PwDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"    Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntriesTests()
        {
            PwGroup pg = keepass.PwDb.RootGroup;
            int count = 0;
            foreach (var entry in pg.Entries)
            {
                count++;
                Debug.WriteLine($"{count}. {entry.Uuid}");
                foreach (var kp in entry.Strings)
                {
                    Debug.WriteLine($"    {kp.Key}={kp.Value.ReadString()}");
                }
            }
        }

        [Fact]
        public void AddEntryTests()
        {
            var entry = new PwEntry();
            entry.Name = "New Entry";
            Debug.WriteLine($"Uuid={entry.Uuid}");
        }
    }
}
