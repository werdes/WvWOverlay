using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WvWOverlay
{
    public static class ConfigurationParser
    {
        /// <summary>
        /// Liefert die Regions.xml als Objektliste
        /// </summary>
        /// <returns></returns>
        public static List<Model.XML.Region> GetRegions()
        {
            List<Model.XML.Region> oRetVal = null;
            string cFilePath; 
            XmlSerializer oSerializer = null;
            
            try
            {
                cFilePath = Environment.CurrentDirectory + @"\Resources\Configuration\Regions.xml";
                if (File.Exists(cFilePath))
                {
                    using (StreamReader oReader = new StreamReader(cFilePath))
                    {
                        oSerializer = new XmlSerializer(typeof(List<Model.XML.Region>));
                        oRetVal = (List<Model.XML.Region>)oSerializer.Deserialize(oReader);
                    }
                }
            }
            catch(Exception oEx)
            {
                throw oEx;
            }
            return oRetVal;
        }

        /// <summary>
        /// Liefert die Professions.xml als Objektliste
        /// </summary>
        /// <returns></returns>
        public static List<Model.XML.Profession> GetProfessions()
        {
            List<Model.XML.Profession> oRetVal = null;
            string cFilePath;
            XmlSerializer oSerializer = null;

            try
            {
                cFilePath = Environment.CurrentDirectory + @"\Resources\Configuration\Professions.xml";
                if (File.Exists(cFilePath))
                {
                    using (StreamReader oReader = new StreamReader(cFilePath))
                    {
                        oSerializer = new XmlSerializer(typeof(List<Model.XML.Profession>));
                        oRetVal = (List<Model.XML.Profession>)oSerializer.Deserialize(oReader);
                    }
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
            return oRetVal;
        }

        /// <summary>
        /// Liefert die Maps.xml als Objektliste
        /// </summary>
        /// <returns></returns>
        public static List<Model.XML.Map> GetMaps()
        {
            List<Model.XML.Map> oRetVal = null;
            string cFilePath;
            XmlSerializer oSerializer = null;

            try
            {
                cFilePath = Environment.CurrentDirectory + @"\Resources\Configuration\Maps.xml";
                if (File.Exists(cFilePath))
                {
                    using (StreamReader oReader = new StreamReader(cFilePath))
                    {
                        oSerializer = new XmlSerializer(typeof(List<Model.XML.Map>));
                        oRetVal = (List<Model.XML.Map>)oSerializer.Deserialize(oReader);
                    }
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
            return oRetVal;
        }



        /// <summary>
        /// Liefert die Objectives.xml als Objektliste
        /// </summary>
        /// <returns></returns>
        public static List<Model.XML.Objective> GetObjectives()
        {
            List<Model.XML.Objective> oRetVal = null;
            string cFilePath;
            XmlSerializer oSerializer = null;

            try
            {
                cFilePath = Environment.CurrentDirectory + @"\Resources\Configuration\Objectives.xml";
                if (File.Exists(cFilePath))
                {
                    using (StreamReader oReader = new StreamReader(cFilePath))
                    {
                        oSerializer = new XmlSerializer(typeof(List<Model.XML.Objective>));
                        oRetVal = (List<Model.XML.Objective>)oSerializer.Deserialize(oReader);
                    }
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
            return oRetVal;
        }
    }
}
