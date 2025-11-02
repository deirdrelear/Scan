using LocalScan.Models;
using LocalScan.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static LocalScan.Helpers.XName;

namespace LocalScan.Helpers
{
    public static class XmlSerialization
    {
        static XmlSerialization()
        {
            Data = new List<IXmlSerializable>();
            _characters = new List<IXmlSerializable>();

            TypeCollection = new Dictionary<Type, List<IXmlSerializable>>
            {
                { typeof(Character), _characters },
            };
        }

        #region Чтение

        public static List<IXmlSerializable> Load(string path)
        {
            var res = new List<IXmlSerializable>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("");
            var xDoc = XDocument.Load(path);
            if (xDoc == null || xDoc.Root == null)
                throw new NullReferenceException("");
            var root = xDoc.Root;
            if (root.Name != RootElement)
                throw new Exception("");
            var subRoots = root.Elements();
            if (subRoots != null && subRoots.Any())
            {
                foreach (var sub in subRoots)
                {
                    var elements = sub.Elements().Where(x => ElementNames.Keys.Contains(x.Name.ToString()));
                    foreach (var el in elements)
                    {
                        res.Add(ElementNames[el.Name.ToString()].Invoke(el));
                    }
                }
            }
            return res;
        }

        public static IEnumerable<XElement> ReadParticles(string path)
        {
            var res = new List<IXmlSerializable>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new FileNotFoundException("");
            var xDoc = XDocument.Load(path);
            if (xDoc == null || xDoc.Root == null)
                throw new NullReferenceException("");
            var root = xDoc.Root;
            if (root.Name != RootElement)
                throw new Exception("");

            if (root.Attribute(MaterialPriceUrlAttrib) != null && !string.IsNullOrEmpty(root.Attribute(MaterialPriceUrlAttrib).Value))
                MaterialPriceUrl = root.Attribute(MaterialPriceUrlAttrib).Value;

            return root.Elements();
        }

        public static List<IXmlSerializable> LoadParticles(XElement particle)
        {
            var res = new List<IXmlSerializable>();
            var elements = particle.Elements().Where(x => ElementNames.Keys.Contains(x.Name.ToString()));
            foreach (var el in elements)
            {
                res.Add(ElementNames[el.Name.ToString()].Invoke(el));
            }
            return res;
        }

        #endregion

        #region Запись

        public static void Save(IEnumerable<IXmlSerializable> xmlSerializables, string path) => Save(Fill(xmlSerializables), path);

        public static void Save(IEnumerable<XElement> xElements, string path)
        {
            if (xElements != null && xElements.Any() && !string.IsNullOrEmpty(path))
            {
                var xDoc = new XDocument();
                var root = new XElement(RootElement);
                xDoc.Add(root);

                if (!string.IsNullOrEmpty(MaterialPriceUrl))
                    root.Add(new XAttribute(MaterialPriceUrlAttrib, MaterialPriceUrl));

                var dict = xElements.GroupBy(x => x.Name).ToDictionary(x => $"{x.Key}s", x => x.ToList());

                foreach (var pair in dict)
                {
                    var subRoot = new XElement(pair.Key);
                    foreach (var el in pair.Value)
                        subRoot.Add(el);
                    root.Add(subRoot);
                }
                xDoc.Save(path);
            }
        }

        #endregion

        #region Помощь

        public static IEnumerable<XElement> Fill(IEnumerable<IXmlSerializable> xmlSerializables)
        {
            if (xmlSerializables != null && xmlSerializables.Any())
            {
                return xmlSerializables.Select(x => Fill(x));
            }
            return null;
        }

        public static XElement Fill(IXmlSerializable xmlSerializable)
        {
            if (xmlSerializable != null)
            {
                return xmlSerializable.Serialize();
            }
            return null;
        }

        public static void FirstLoad(string[] paths)
        {
            foreach (var path in paths)
            {
                var particles = ReadParticles(path);
                if (particles != null && particles.Any())
                {
                    foreach (var particle in particles)
                    {
                        var entities = LoadParticles(particle);
                        if (entities.Any())
                        {
                            var key = entities.First().GetType();
                            if (TypeCollection.ContainsKey(key))
                            {
                                TypeCollection[key].AddRange(entities);
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region Данные

        private static List<IXmlSerializable> _characters;
        public static IEnumerable<Character> Characters => _characters.OfType<Character>();

        public static List<IXmlSerializable> Data { get; private set; }
        public static string MaterialPriceUrl { get; private set; }

        private static Dictionary<Type, List<IXmlSerializable>> TypeCollection { get; }

        #endregion

    }

    public static class XName
    {
        public static string NameAttrib { get; } = "name";
        public static string ValueAttrib { get; } = "value";
        public static string RarityAttrib { get; } = "rarity";
        public static string UrlAttrib { get; } = "url";
        public static string DateAttrib { get; } = "date";
        public static string WidthAreaAttrib { get; } = "widthArea";
        public static string HeightAreaAttrib { get; } = "heightArea";
        public static string IconWidthAttrib { get; } = "iconWidth";
        public static string VolumeAttrib { get; } = "volume";
        public static string PortionSizeAttrib { get; } = "portionSize";
        public static string MaterialPriceUrlAttrib { get; } = "materialPriceUrl";

        public static string CharacterElement { get; } = "Character";
        public static string OreElement { get; } = "Ore";
        public static string MaterialElement { get; } = "Material";
        public static string PriceElement { get; } = "Price";
        public static string MaterialsElement { get; } = "Materials";
        public static string PricesElement { get; } = "Prices";
        public static string AltElement { get; } = "Alt";

        public static string RootElement { get; } = "Data";

        public static Dictionary<string, Func<XElement, IXmlSerializable>> ElementNames = new Dictionary<string, Func<XElement, IXmlSerializable>>
        {
            { CharacterElement, Character.Deserialize },
            { AltElement, Alt.Deserialize },
        };
    }
}
