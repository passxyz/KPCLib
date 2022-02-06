using System;
using System.Collections.Generic;

using Microsoft.Maui;
using Microsoft.Maui.Controls;

using KPCLib;
using KeePassLib;
using KeePassLib.Security;

namespace PassXYZLib
{
    /// <summary>
    /// A class defined extension methods for PwEntry.
    /// </summary>
    public static class PwEntryEx
    {
        public static bool IsNotes(this PwEntry entry)
        {
            string subType = entry.CustomData.Get(PxDefs.PxCustomDataItemSubType);
            return !string.IsNullOrEmpty(subType) && subType.Equals(ItemSubType.Notes.ToString());
        }

        /// <summary>
        /// This is an extension method of PwEntry.
        /// This method is used to set the sub-type of a PwEntry.
        /// </summary>
        /// <param name="entry">an instance of PwEntry</param>
        /// <param name="itemSubType">sub-type of PwEntry</param>
		/// <returns>A list of fields</returns>
        public static void SetType(this PwEntry entry, ItemSubType itemSubType)
        {
            if (itemSubType != ItemSubType.None && itemSubType != ItemSubType.Group)
            {
                entry.CustomData.Set(PxDefs.PxCustomDataItemSubType, itemSubType.ToString());
            }
        }

        public static bool IsPxEntry(this PwEntry entry)
        {
            return PxDefs.IsPxEntry(entry);
        }

        public static void SetPxEntry(this PwEntry entry)
        {
            entry.CustomData.Set(PxDefs.PxCustomDataItemSubType, ItemSubType.PxEntry.ToString());
        }

        /// <summary>
        /// Create a new encoded key for PxEntry.
        /// </summary>
        /// <param name="entry">an instance of PwEntry</param>
        /// <param name="key">key of PwEntry</param>
		/// <returns>encoded key</returns>
        public static string EncodeKey(this PwEntry entry, string key)
        {
            if (PxDefs.IsPxEntry(entry))
            {
                string lastKey = string.Empty;
                foreach (var pstr in entry.Strings)
                {
                    if (!pstr.Key.Equals(PwDefs.TitleField) && !pstr.Key.Equals(PwDefs.NotesField))
                    {
                        lastKey = pstr.Key;
                    }
                }

                if (string.IsNullOrEmpty(lastKey))
                {
                    return "000" + key;
                }
                else
                {
                    uint index = uint.Parse(lastKey.Substring(0, PxDefs.PxEntryKeyDigits));
                    return PxDefs.EncodeKey(key, index + 1);
                }
            }
            else
            {
                return key;
            }
        }

        /// <summary>
        /// Return the Notes field in PwEntry. This is an extension method of PwEntry.
        /// </summary>
        public static string GetNotes(this PwEntry entry)
        {
            return entry.Strings.ReadSafe(PwDefs.NotesField);
        }

        /// <summary>
        /// Return the Notes field in HTML format. This is an extension method of PwEntry.
        /// </summary>
        public static string GetNotesInHtml(this PwEntry entry)
        {
            return Markdig.Markdown.ToHtml(entry.Strings.ReadSafe(PwDefs.NotesField));

            // TODO: need test on .NET MAUI, Xamarin.iOS cannot work well with version > 0.24
            //if (Device.RuntimePlatform == Device.iOS)
            //{
            //    return Markdig.Markdown.ToHtml(entry.Strings.ReadSafe(PwDefs.NotesField));
            //}
            //else
            //{
            //    var pipeline = new Markdig.MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            //    return Markdig.Markdown.ToHtml(entry.Strings.ReadSafe(PwDefs.NotesField), pipeline);
            //}
        }

        /// <summary>
        /// Convert ProtectedStringDictionary into a list of fields. TitleField and NotesField
        /// are not included in the list.
        /// TitleField will be used to display the title in UI and NotesField will be displayed at the
        /// bottom of a page with Markdown support.
        /// </summary>
        /// <param name="entry">an instance of PwEntry</param>
        /// <param name="encodeKey">true - decode key, false - does not decode key</param>
		/// <returns>A list of fields</returns>
        public static List<Field> GetFields(this PwEntry entry, bool encodeKey = false, Func<string, Object> GetImage = null)
        {
            List<Field> fields = new List<Field>();
            bool isPxEntry = PxDefs.IsPxEntry(entry);

            if (isPxEntry)
            {
                // If this is an instance of PxEntry, we handle it here. We need to convert ProtectedString to Field.
                foreach (var pstr in entry.Strings)
                {
                    if (!pstr.Key.Equals(PwDefs.TitleField) && !pstr.Key.Equals(PwDefs.NotesField))
                    {
                        if (encodeKey)
                        {
                            fields.Add(new Field(pstr.Key, entry.Strings.ReadSafe(pstr.Key), entry.Strings.GetSafe(pstr.Key).IsProtected, GetImage));
                        }
                        else
                        {
                            fields.Add(new Field(PxDefs.DecodeKey(pstr.Key), entry.Strings.ReadSafe(pstr.Key), entry.Strings.GetSafe(pstr.Key).IsProtected, GetImage, pstr.Key));
                        }
                    }
                }
            }
            else
            {
                // If this is an instance of PwEntry, we handle it here.
                if (entry.Strings.Exists(PwDefs.UserNameField))
                {
                    fields.Add(new Field(PwDefs.UserNameField, entry.Strings.ReadSafe(PwDefs.UserNameField), entry.Strings.GetSafe(PwDefs.UserNameField).IsProtected, GetImage));
                }

                if (entry.Strings.Exists(PwDefs.PasswordField))
                {
                    fields.Add(new Field(PwDefs.PasswordField, entry.Strings.ReadSafe(PwDefs.PasswordField), entry.Strings.GetSafe(PwDefs.PasswordField).IsProtected, GetImage));
                }

                if (entry.Strings.Exists(PwDefs.UrlField))
                {
                    fields.Add(new Field(PwDefs.UrlField, entry.Strings.ReadSafe(PwDefs.UrlField), entry.Strings.GetSafe(PwDefs.UrlField).IsProtected, GetImage));
                }

                foreach (var field in entry.Strings)
                {
                    if (!PwDefs.IsStandardField(field.Key))
                    {
                        fields.Add(new Field(field.Key, entry.Strings.ReadSafe(field.Key), entry.Strings.GetSafe(field.Key).IsProtected, GetImage));
                    }
                }
            }

            foreach (var field in entry.Binaries)
            {
                fields.Add(new Field(field.Key, $"Attachment {entry.Binaries.UCount}", false)
                {
                    IsBinaries = true,
                    Binary = entry.Binaries.Get(field.Key),
                    // We use hard code Glyph value here, since we want to put this extension here.
                    ImgSource = new FontImageSource
                    {
                        FontFamily = "FontAwesomeSolid",
                        Glyph = "\uf0c6"
                    }
                });
            }

            return fields;
        }

        /// <summary>
        /// Create an instance of PxPlainFields from PwEntry
        /// </summary>
        /// <param name="entry">an instance of PwEntry</param>
		/// <returns>an instance of PxPlainFields</returns>
        public static PxPlainFields GetPlainFields(this PwEntry entry)
        {
            return new PxPlainFields(entry);
        }

        public static string GetUrlField(this PwEntry entry)
        {
            foreach (KeyValuePair<string, ProtectedString> pstr in entry.Strings)
            {
                if (pstr.Key.EndsWith(PwDefs.UrlField))
                {
                    return entry.Strings.ReadSafe(pstr.Key);
                }
            }
            return string.Empty;
        }

    }

}
