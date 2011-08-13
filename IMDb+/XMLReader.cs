using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace IMDb
{
    class XmlReader
    {
        XmlDocument Document = new XmlDocument();

        #region Options Helper
        public bool GetOptionValueAsBool(string name, bool defaultvalue)
        {
            if (Document == null) return defaultvalue;

            XmlNode node = null;
            node = Document.DocumentElement.SelectSingleNode(string.Format("/imdbplus//set[@name='{0}']", name));
            if (node == null) return defaultvalue;

            try
            {
                bool result;
                if (bool.TryParse(node.Attributes["value"].Value, out result))
                    return result;
                else
                    return defaultvalue;
            }
            catch
            {
                return defaultvalue;
            }
        }

        public string GetOptionValueAsString(string name, string defaultvalue)
        {
            if (Document == null) return defaultvalue;

            XmlNode node = null;
            node = Document.DocumentElement.SelectSingleNode(string.Format("/imdbplus//set[@name='{0}']", name));
            if (node == null) return defaultvalue;

            try
            {
                return node.Attributes["value"].Value;
            }
            catch
            {
                return defaultvalue;
            }
        }
        #endregion

        public bool Load(string file)
        {
            if (!File.Exists(file))
            {
                Document = null;
                return false;
            }

            try
            {
                Document.Load(file);
            }
            catch (Exception)
            {
                Document = null;
                return false;
            }
            return true;
        }
    }
}
