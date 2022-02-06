using System;
using System.Collections.Generic;
using System.Diagnostics;

using Newtonsoft.Json;

using KeePassLib;

namespace PassXYZLib
{
    public class PxPlainFields
    {
        public bool IsPxEntry = false;
        public bool IsGroup = false;
        public string CustomDataType = string.Empty;
        public SortedDictionary<string, PxFieldValue> Strings = new SortedDictionary<string, PxFieldValue>();

        public PxPlainFields()
        {
            IsPxEntry = false;
            IsGroup = false;
        }

        /// <summary>
        /// Create an instance of PxPlainFields from a JSON string
        /// </summary>
        public PxPlainFields(string str, string? password = null)
        {
            string decryptedMessage;
            if (str.StartsWith(PxDefs.PxJsonTemplate))
            {
                decryptedMessage = str.Substring(PxDefs.PxJsonTemplate.Length);
            }
            else if (str.StartsWith(PxDefs.PxJsonData) && !string.IsNullOrEmpty(password))
            {
                string encryptedMessage = str.Substring(PxDefs.PxJsonData.Length);
                decryptedMessage = PxEncryption.DecryptWithPassword(encryptedMessage, password);
                if (string.IsNullOrEmpty(decryptedMessage))
                {
                    Debug.WriteLine("PxPlainFields: cannot decrypt message, error!");
                    return;
                }
            }
            else
            {
                Debug.WriteLine("PxPlainFields: wrong JSON string, error!");
                return;
            }

            try
            {
                PxPlainFields? fields = JsonConvert.DeserializeObject<PxPlainFields>(decryptedMessage);
                if (fields != null) 
                {
                    IsPxEntry = fields.IsPxEntry;
                    IsGroup = fields.IsGroup;
                    Strings = fields.Strings;
                    CustomDataType = fields.CustomDataType;
                }
            }
            catch (JsonReaderException ex)
            {
                Debug.WriteLine($"{ex}");
            }
        }

        /// <summary>
        /// Create an instance of PxPlainFields from a PwGroup
        /// </summary>
        public PxPlainFields(PwGroup group)
        {
            if (group == null)
            {
                Debug.Assert(false); throw new ArgumentNullException("group");
            }

            IsGroup = true;

            Strings.Add(PwDefs.TitleField, new PxFieldValue(group.Name, false));
            Strings.Add(PwDefs.NotesField, new PxFieldValue(group.Notes, false));
        }

        /// <summary>
        /// Create an instance of PxPlainFields from a PwEntry
        /// </summary>
        public PxPlainFields(PwEntry entry)
        {
            if (entry == null)
            {
                Debug.Assert(false); throw new ArgumentNullException("entry");
            }

            IsPxEntry = entry.IsPxEntry();

            var fields = entry.GetFields(IsPxEntry);
            Strings.Add(PwDefs.TitleField, new PxFieldValue(entry.Name, false));
            foreach (var field in fields)
            {
                Strings.Add(field.Key, new PxFieldValue(field.EditValue, field.IsProtected));
            }
            Strings.Add(PwDefs.NotesField, new PxFieldValue(entry.Notes, false));
            CustomDataType = entry.CustomData.Get(PxDefs.PxCustomDataItemSubType);
        }

        private static PxFieldValue? FindPasswordField(SortedDictionary<string, PxFieldValue> fields)
        {
            foreach (var field in fields)
            {
                if (field.Key.Equals(PxDefs.PasswordField) || field.Key.EndsWith("Password"))
                {
                    return field.Value;
                }
            }

            return null;
        }

        public override string ToString()
        {
            PxFieldValue? fieldV = PxPlainFields.FindPasswordField(Strings);

            if (fieldV != null && !string.IsNullOrEmpty(fieldV.Value))
            {
                return PxDefs.PxJsonData + PxEncryption.EncryptWithPassword(JsonConvert.SerializeObject(this), fieldV.Value);
            }

            //if (Strings.TryGetValue(PxDefs.PasswordField, out PxFieldValue fieldV))
            //{
            //    if (fieldV.IsProtected && !string.IsNullOrEmpty(fieldV.Value))
            //    {
            //        return PxDefs.PxJsonData + PxEncryption.EncryptWithPassword(JsonConvert.SerializeObject(this), fieldV.Value);
            //    }
            //}

            return PxDefs.PxJsonTemplate + JsonConvert.SerializeObject(this);
        }
    }
}
