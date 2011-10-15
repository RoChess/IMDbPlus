using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;

namespace IMDb
{
    public class XmlWriter
    {
        XmlDocument Document = new XmlDocument();

        public void CreateXmlConfigFile(string file)
        {
            try
            {
                if (!Directory.Exists(Path.GetDirectoryName(file)))
                  Directory.CreateDirectory(Path.GetDirectoryName(file));

                XmlTextWriter textWriter = new XmlTextWriter(file, Encoding.UTF8);

                textWriter.WriteStartDocument();
                textWriter.WriteStartElement("imdbplus");
                textWriter.WriteEndElement();
                textWriter.WriteEndDocument();

                textWriter.Close();
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
            }
        }

        public bool Load(string file)
        {
            if (!File.Exists(file)) return false;

            try
            {
                Document.Load(file);
            }
            catch (XmlException)
            {
                Document = null;
                return false;
            }
            return true;
        }

        public bool Save(string file)
        {
            if (!File.Exists(file)) return false;

            try
            {
                Document.Save(file);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        #region Options Helper
        public bool SetOptionsEntry(string name, string id, string value)
        {
            if (Document == null) return false;

            try
            {
                value = string.IsNullOrEmpty(value) ? string.Empty : value.ToLower();

                XmlNode node = null;
                node = Document.SelectSingleNode(string.Format("/imdbplus//set[@name='{0}']", name));
                if (node == null)
                {
                    // select root node
                    node = Document.SelectSingleNode("/imdbplus");

                    // create new section node
                    XmlNode newNode = Document.CreateElement("set");

                    // create id attribute
                    XmlAttribute newAttribute = Document.CreateAttribute("id");
                    newAttribute.Value = id;
                    newNode.Attributes.Append(newAttribute);
                    node.AppendChild(newNode);

                    // create name attribute
                    newAttribute = Document.CreateAttribute("name");
                    newAttribute.Value = name;
                    newNode.Attributes.Append(newAttribute);
                    node.AppendChild(newNode);

                    // create value attribute
                    newAttribute = Document.CreateAttribute("value");
                    newAttribute.Value = value;
                    newNode.Attributes.Append(newAttribute);
                    node.AppendChild(newNode);

                    return true;
                }
                else
                {
                    node.Attributes["value"].Value = value;
                    return true;
                }
            }
            catch
            {
                Logger.Error("Error saving setting '{0}'", name);
                return false; 
            }
        }
        #endregion

    }
}
