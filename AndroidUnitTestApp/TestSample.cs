using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;

using PCLStorage;

using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace AndroidUnitTestApp
{
    /// <summary>
    /// This logger displays the current status in a status bar.
    /// As soon as a warning or error is logged, a dialog is opened
    /// and all consecutive log lines are sent to both loggers
    /// (status bar and dialog).
    /// </summary>
    public sealed class ShowWarningsLogger : IStatusLogger
    {
        private bool m_bStartedLogging = false;
        private bool m_bEndedLogging = false;

        private List<KeyValuePair<LogStatusType, string>> m_vCachedMessages =
            new List<KeyValuePair<LogStatusType, string>>();

        public ShowWarningsLogger()
        {
        }


        ~ShowWarningsLogger()
        {
            Debug.Assert(m_bEndedLogging);

            try { if (!m_bEndedLogging) EndLogging(); }
            catch (Exception) { Debug.Assert(false); }
        }

        public void StartLogging(string strOperation, bool bWriteOperationToLog)
        {
            Debug.Assert(!m_bStartedLogging && !m_bEndedLogging);

            m_bStartedLogging = true;

            if (bWriteOperationToLog)
                m_vCachedMessages.Add(new KeyValuePair<LogStatusType, string>(
                    LogStatusType.Info, strOperation));
        }

        public void EndLogging()
        {
            Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

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
            Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

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
            Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

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
            Debug.Assert(m_bStartedLogging && !m_bEndedLogging);

            bool b = true;

            return b;
        }
    }

    class TestDb
    {
        private string fileName;
        public string FileName { get { return fileName; } }
        private IOConnectionInfo ioInfo;
        private CompositeKey compositeKey;
        private PwDatabase pwDb = new PwDatabase();

        public TestDb(string fileName)
        {
            this.fileName = fileName;

            ioInfo = IOConnectionInfo.FromPath(FileName);
            compositeKey = new CompositeKey();
            compositeKey.AddUserKey(new KcpPassword("123456"));
            pwDb.New(ioInfo, compositeKey);

            PwGroup pg = new PwGroup(true, true);
            // for(int i = 0; i < 30; ++i) pg.CustomData.Set("Test" + i.ToString("D2"), "12345");
            pwDb.RootGroup.AddGroup(pg, true);

            PwEntry pe = new PwEntry(true, true);
            pe.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "Title"));
            pe.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, "UserName"));
            pe.Strings.Set(PwDefs.UrlField, new ProtectedString(false, "Url"));
            pe.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, "Password"));
            pe.Strings.Set(PwDefs.NotesField, new ProtectedString(false, "Notes"));
            // for(int i = 0; i < 30; ++i) pe.CustomData.Set("Test" + i.ToString("D2"), "12345");
            pwDb.RootGroup.AddEntry(pe, true);
        }

        public void SaveDatabase(PwDatabase pdToSave)
        {
            PwDatabase pd = (pdToSave ?? pwDb);

            if (!pd.IsOpen) return;

            Guid eventGuid = Guid.NewGuid();
            ShowWarningsLogger swLogger = new ShowWarningsLogger();
            swLogger.StartLogging("Saving database ...", true);
            pwDb.Save(swLogger);
        }
    }

    [TestFixture]
    public class TestsSample
    {
        private static IFolder folder = FileSystem.Current.LocalStorage;
        private static string fileName = folder.Path + "/pass.xyz";
        TestDb testDb = new TestDb(fileName);

        [SetUp]
        public void Setup() { }


        [TearDown]
        public void Tear() { }

        [Test]
        public void Pass()
        {
            Console.WriteLine("test1");
            Assert.True(true);
        }

        [Test]
        public void TestCreateDb()
        {
            string fullPath = Path.GetFullPath(testDb.FileName);

            testDb.SaveDatabase(null);

            Debug.Print("FileName=" + fullPath);
            Assert.True(true);
        }

        [Test]
        [Ignore("another time")]
        public void Ignore()
        {
            Assert.True(false);
        }

        [Test]
        public void Inconclusive()
        {
            Assert.Inconclusive("Inconclusive");
        }
    }
}