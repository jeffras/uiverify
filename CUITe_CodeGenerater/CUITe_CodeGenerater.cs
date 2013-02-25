using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VisualUIAVerify.Controls;
using VisualUIAVerify.Features;
using VisualUIAVerify.Interfaces;

namespace CUITe_CodeGenerater
{
    [Export(typeof(ICodeGenerater))]
    public class CUITe_CodeGenerater : ICodeGenerater
    {
        private int _tabCount = -1;
        private const string _tab = "    ";
        public string Name { get { return "CUITe"; } }

        private List<string> _fields = new List<string>();

        public string Tab
        {
            get
            {
                var ret = "";
                for (int i = 0; i < _tabCount; i++)
                {
                    ret += _tab;
                }
                return ret;
            }
        }
        
        public string Generate(AutomationElementTreeNode node)
        {
            return Generate(node, null);
        }

        public string Generate(AutomationElementTreeNode node, AutomationElementTreeNode parent)
        {
            var ret = "";
            foreach (var child in node.Children)
            {
                _tabCount++;
                ret += Generate(child, node);
                _tabCount--;
            }
            var obj = new AutomationElementPropertyObject(node.AutomationElement);

            return CreateCode(obj, ret, parent);
        }

        private string CreateCode(AutomationElementPropertyObject obj, string inner, AutomationElementTreeNode parent)
        {
            _tabCount++;
            try
            {
                if (!String.IsNullOrEmpty(inner))
                    inner = String.Format("{0}{{{1}{2}{3}{4}}}", Tab, Environment.NewLine, inner, Environment.NewLine, Tab);

                var comment = String.Format("//UI Detection Details -- ControlType:{0}, Automation ID:{1}, Name:{2}", obj.ControlType, obj.AutomationId, obj.Name);
                var generated = "";
                AutomationElementPropertyObject parentObj = parent == null ? null : new AutomationElementPropertyObject(parent.AutomationElement);
                switch (obj.ControlType)
                {
                    case "ControlType.Window":
                        string fields = String.Join(string.Empty, _fields);
                        inner = String.Format("{0}public class {1} : CUITe_WinWindow{2}" +
                            "{{{3}public {4}() : base(\"Name={5}\") {{ }}{6}{7}{8}", 
                            Tab, 
                            obj.PropertyName(), 
                            Environment.NewLine + Tab,
                            Environment.NewLine + Tab + _tab, 
                            obj.PropertyName(), 
                            obj.Name,
                            fields,
                            inner.TrimStart('{', ' '),
                            Environment.NewLine);
                        break;
                    case "ControlType.TitleBar":
                        return inner.RemoveBrackets();

                    case "ControlType.MenuBar":
                        inner = inner.RemoveBrackets();
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.CUITeType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultCuiteFormat(obj.CUITeType(), Tab);
                        break;
                    case "ControlType.MenuItem":
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.CUITeType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultCuiteFormatWithParent(obj.CUITeType(), Tab, parentObj);
                        break;
                    case "ControlType.Button":
                    case "ControlType.Text":
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.CUITeType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultCuiteFormatFromWindow(obj.CUITeType(), Tab);
                        break;

                    case "ControlType.Pane":
                        inner = inner.RemoveBrackets();
                        //TODO: Scope searches to pane
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.CUITeType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultCuiteFormatWithParent(Tab, parentObj);
                        break;
                    default:
                        generated = String.Format("{0}// Control type '{1}' not supported{2}", Tab, obj.ControlType, Environment.NewLine);
                        break;
                }

                return string.Format("{0}{1}{2}{3}{4}{5}{6}",
                    Environment.NewLine,
                    Tab,
                    comment,
                    Environment.NewLine,
                    generated,
                    inner,
                    Environment.NewLine);
            }
            finally
            {
                _tabCount--;
            }
        }
    }

    public static class Extenstions
    {
        public static string CUITeType(this AutomationElementPropertyObject obj)
        {
            return "CUITe_" + obj.FrameworkType() + obj.ShortControlType();
        }

        public static string FrameworkType(this AutomationElementPropertyObject obj)
        {
            return obj.FrameworkId == "WPF" ? "Wpf" : "Win";
        }

        public static string ShortControlType(this AutomationElementPropertyObject obj)
        {
            return obj.ControlType.Replace("ControlType.", string.Empty);
        }

        public static string PropertyName(this AutomationElementPropertyObject obj)
        {
            var name = obj.Name.Replace(" ", String.Empty);
            int tmp;
            var parsed = int.TryParse(name, out tmp);
            if (parsed)
                name = "TODO_RENAME_" + name;

            name += obj.ShortControlType();
            return name;
        }

        public static string FieldName(this AutomationElementPropertyObject obj)
        {
            return "_" + Char.ToLowerInvariant(obj.PropertyName()[0]) + obj.PropertyName().Substring(1);
        }

        public static string ApplyDefaultCuiteFormat(this AutomationElementPropertyObject obj, string type, string tab)
        {
            if (String.IsNullOrEmpty(obj.AutomationId))
                return ApplyDefaultCuiteFormatByText(obj, type, tab);
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = Get<{5}>(\"AutomationId={6}\")); }} }}{7}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                type,
                                obj.Name,
                                Environment.NewLine);
        }

        public static string ApplyDefaultCuiteFormatByText(this AutomationElementPropertyObject obj, string type, string tab)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = Get<{5}>(\"Name={6}\")); }} }}{7}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                type,
                                obj.Name,
                                Environment.NewLine);
        }

        public static string ApplyDefaultCuiteFormatFromWindow(this AutomationElementPropertyObject obj, string type, string tab)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = Get<CUITe_WinWindow>(\"Name={5}\").Get<{6}>()); }} }}{7}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                obj.Name,
                                type, 
                                Environment.NewLine);
        }

        public static string ApplyDefaultCuiteFormatWithParent(this AutomationElementPropertyObject obj, string tab, AutomationElementPropertyObject parent)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ({4} = {5}.Get<{6}>(\"Name={7}\")); }} }}{8}",
                                tab,
                                obj.CUITeType(),
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                parent.PropertyName(),
                                obj.CUITeType(),
                                obj.Name,
                                Environment.NewLine);
        }

        public static string ApplyDefaultCuiteFormatWithParent(this AutomationElementPropertyObject obj, string type, string tab, AutomationElementPropertyObject parent)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = {5}.Get<{6}>(\"Name={7}\")); }} }}{8}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                parent.PropertyName(),
                                type,
                                obj.Name,
                                Environment.NewLine);
        }
        public static string RemoveBrackets(this string str)
        {
            return str.TrimStart('{', ' ').TrimEnd('}', ' ');
        }
    }
}
