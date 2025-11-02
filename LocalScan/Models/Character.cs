using LocalScan.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static LocalScan.Helpers.XName;

namespace LocalScan.Models
{
    public class Character : IXmlSerializable
    {
        public string Main { get; private set; }
        public List<Alt> Alts { get; private set; }

        public Character(string main, IEnumerable<Alt> alts)
        {
            Main = main;
            Alts = alts.ToList();
        }

        public override string ToString() => $"{Main} : {string.Join(",", Alts)}";

        XElement IXmlSerializable.Serialize()
        {
            var res = new XElement(CharacterElement);
            res.Add(new XAttribute(NameAttrib, Main));
            Alts.Where(x => !x.Equals(Main)).ToList().ForEach(x => res.Add(new XElement(AltElement, new XAttribute(NameAttrib, x))));
            return res;
        }

        public static IXmlSerializable Deserialize(XElement xElement)
        {
            var error = $"Ошибка при десериализации объекта класса {typeof(Character).Name}";
            if (xElement == null)
                throw new NullReferenceException($"{error}.");
            if (xElement.Name != CharacterElement)
                throw new Exception($"{error}: не соответствие имени xml.");
            if (xElement.Attribute(NameAttrib) == null || string.IsNullOrEmpty(xElement.Attribute(NameAttrib).Value))
                throw new Exception($"{error}: не найден атрибут основного персонажа или значение пусто.");
            var main = xElement.Attribute(NameAttrib).Value;
            var alts = new List<Alt>();
            if (xElement.Elements(AltElement).Any())
            {
                var _alts = XmlSerialization.LoadParticles(xElement).OfType<Alt>();

                foreach(var alt in _alts.Where(x => main != x.Name))
                {
                    alts.Add(alt);
                }
            }
            var res = new Character(main, alts);
            return res;
        }
    }

    public class Alt : IXmlSerializable
    {
        public string Name { get; private set; }

        public int WidthArea { get; set; }
        public int HeightArea { get; set; }
        public int IconWidth { get; set; }

        public Alt(string name)
        {
            Name = name;
        }

        public override string ToString() => Name;

        XElement IXmlSerializable.Serialize()
        {
            var res = new XElement(AltElement);
            res.Add(new XAttribute(NameAttrib, Name));

            if (WidthArea != 0)
                res.Add(new XAttribute(WidthAreaAttrib, WidthArea));
            if (HeightArea != 0)
                res.Add(new XAttribute(HeightAreaAttrib, HeightArea));
            if (IconWidth != 0)
                res.Add(new XAttribute(IconWidthAttrib, IconWidth));

            return res;
        }

        public static IXmlSerializable Deserialize(XElement xElement)
        {
            var error = $"Ошибка при десериализации объекта класса {typeof(Alt).Name}";
            if (xElement == null)
                throw new NullReferenceException($"{error}.");
            if (xElement.Name != AltElement)
                throw new Exception($"{error}: не соответствие имени xml.");
            if (xElement.Attribute(NameAttrib) == null || string.IsNullOrEmpty(xElement.Attribute(NameAttrib).Value))
                throw new Exception($"{error}: не найден атрибут имени персонажа или значение пусто.");
            var name = xElement.Attribute(NameAttrib).Value;

            var res = new Alt(name);

            if (xElement.Attribute(WidthAreaAttrib) != null && !string.IsNullOrEmpty(xElement.Attribute(WidthAreaAttrib).Value) && int.TryParse(xElement.Attribute(WidthAreaAttrib).Value, out int widthArea))
                res.WidthArea = widthArea;
            if (xElement.Attribute(HeightAreaAttrib) != null && !string.IsNullOrEmpty(xElement.Attribute(HeightAreaAttrib).Value) && int.TryParse(xElement.Attribute(HeightAreaAttrib).Value, out int heightArea))
                res.HeightArea = heightArea;
            if (xElement.Attribute(IconWidthAttrib) != null && !string.IsNullOrEmpty(xElement.Attribute(IconWidthAttrib).Value) && int.TryParse(xElement.Attribute(IconWidthAttrib).Value, out int iconWidth))
                res.IconWidth = iconWidth;

            return res;
        }
    }
}
