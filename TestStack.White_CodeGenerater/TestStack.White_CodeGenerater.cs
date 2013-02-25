using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using VisualUIAVerify.Controls;
using VisualUIAVerify.Features;
using VisualUIAVerify.Interfaces;

namespace TestStack.White_CodeGenerater
{
    [Export(typeof(ICodeGenerater))]
    public class White_CodeGenerater : ICodeGenerater
    {
        private int _tabCount = -1;
        private const string _tab = "    ";
        public string Name { get { return "TestStack.White"; } }

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
	        try
	        {

				return Generate(node, null);
	        }
	        catch (Exception e)
	        {
		        return e.ToString();
	        }
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

	        if (obj == null)
				return string.Format("// Error Failed to create AutomationElementPropertyObject for object {0}", node.Text);

            return CreateCode(obj, ret, parent);
        }

        private string CreateCode(AutomationElementPropertyObject obj, string inner, AutomationElementTreeNode parent)
        {
            _tabCount++;
            try
            {
                if (!String.IsNullOrEmpty(inner))
                    inner = String.Format("{0}{{{1}{2}{3}{4}}}", Tab, Environment.NewLine, inner, Environment.NewLine, Tab);

				var comment = String.Format("//UI Detection Details -- ControlType:{0}, Automation ID:{1}, Name:{2}, Framework:{3}", obj.ControlType, obj.AutomationId, obj.Name, obj.FrameworkType());
                var generated = "";
                AutomationElementPropertyObject parentObj = parent == null ? null : new AutomationElementPropertyObject(parent.AutomationElement);
                switch (obj.ControlType)
                {
                    case "ControlType.Window":
                        string fields = String.Join(string.Empty, _fields);
                        inner = String.Format("{0}public class {1} : Screen{2}" +
                            "{{{3}public {4}() : base(\"{5}\") {{ }}{6}{7}{8}{9}", 
                            Tab, 
                            obj.PropertyName(), 
                            Environment.NewLine + Tab,
                            Environment.NewLine + Tab + _tab, 
                            obj.PropertyName(), 
                            obj.Name,
                            Environment.NewLine + Tab + _tab,
                            fields,
                            inner.TrimStart('{', ' '),
                            Environment.NewLine);
                        break;
                    case "ControlType.TitleBar":
                        return inner.RemoveBrackets();

                    case "ControlType.MenuBar":
                        inner = inner.RemoveBrackets();
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.WhiteType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultFormatByText(obj.WhiteType(), Tab);
                        break;
                    case "ControlType.MenuItem":
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.WhiteType(), obj.FieldName(), Environment.NewLine));
                        generated = String.Format(
                            "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = {5}.MenuItem(\"{6}\")); }} }}{7}",
                            Tab,
                            obj.WhiteType(),
                            obj.PropertyName(),
                            obj.FieldName(),
                            obj.FieldName(),
                            parentObj.PropertyName(),
                            obj.Name,
                            Environment.NewLine);
                        break;
                    case "ControlType.Button":
					case "ControlType.Edit":
                    case "ControlType.Text":
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.WhiteType(), obj.FieldName(), Environment.NewLine));
                        generated = obj.ApplyDefaultFormat(obj.WhiteType(), Tab);
                        break;

                    case "ControlType.Pane":
                        inner = inner.RemoveBrackets();
                        //TODO: Scope searches to pane
                        _fields.Add(String.Format("{0}private {1} {2};{3}", Tab, obj.WhiteType(), obj.FieldName(), Environment.NewLine));
		                generated = parentObj == null ? obj.ApplyDefaultFormat(obj.WhiteType(), Tab) : obj.ApplyDefaultFormatWithParent(Tab, parentObj);
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
        public static string WhiteType(this AutomationElementPropertyObject obj)
        {
            //return obj.FrameworkType() + obj.ShortControlType();
	        if (obj.ShortControlType() == "Edit")
		        return "TextBox";
            return obj.ShortControlType();
        }

        public static string FrameworkType(this AutomationElementPropertyObject obj)
        {
            return obj.FrameworkId == "WPF" ? "Wpf" : "Win";
        }

        public static string ShortControlType(this AutomationElementPropertyObject obj)
        {
            var ret = obj.ControlType.Replace("ControlType.", string.Empty);
            switch (ret)
            {
                case "MenuItem":
                    return "Menu";
            }
            return ret;
        }

        public static string PropertyName(this AutomationElementPropertyObject obj)
        {
	        if (!string.IsNullOrEmpty(obj.AutomationId))
		        return obj.AutomationId;

			var name = string.IsNullOrEmpty(obj.Name) ? string.Empty : obj.Name.Replace(" ", String.Empty);
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

        public static string ApplyDefaultFormat(this AutomationElementPropertyObject obj, string type, string tab)
        {
            //public Button ScanIdButton { get { return Get<Button>("btnScanId"); } }
            if (String.IsNullOrEmpty(obj.AutomationId))
                return ApplyDefaultFormatByText(obj, type, tab);
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = Get<{5}>(\"{6}\")); }} }}{7}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                type,
                                obj.AutomationId,
                                Environment.NewLine);
        }
        public static string ApplyDefaultFormatByText(this AutomationElementPropertyObject obj, string type, string tab)
        {
            //public Button ScanIdButton { get { return Get<Button>("btnScanId"); } }
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = Get<{5}>(SearchCriteria.ByText(\"{6}\"))); }} }}{7}",
                                tab,
                                type,
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                type,
                                obj.Name,
                                Environment.NewLine);
        }

        public static string ApplyDefaultFormatWithParent(this AutomationElementPropertyObject obj, string tab, AutomationElementPropertyObject parent)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ({4} = {5}.Get<{6}>(\"{7}\")); }} }}{8}",
                                tab,
                                obj.WhiteType(),
                                obj.PropertyName(),
                                obj.FieldName(),
                                obj.FieldName(),
                                parent.PropertyName(),
                                obj.WhiteType(),
                                obj.Name,
                                Environment.NewLine);
        }

        public static string ApplyDefaultFormatWithParent(this AutomationElementPropertyObject obj, string type, string tab, AutomationElementPropertyObject parent)
        {
            return String.Format(
                                "{0}public {1} {2} {{ get {{ return {3} ?? ( {4} = {5}.Get<{6}>(\"{7}\")); }} }}{8}",
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
