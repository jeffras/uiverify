// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Permissive License.
// See http://www.microsoft.com/opensource/licenses.mspx#Ms-PL.
// All other rights reserved.

using System.Xml;

namespace Microsoft.Test.UIAutomation.Logging.XmlSerializableObjects
{
    public class XmlTestElementInfo
    {
        public XmlNode ElementPath;

        public XmlTestElementInfo() { }
        public XmlTestElementInfo(XmlNode xmlNodeElementPath)
        {
            this.ElementPath = xmlNodeElementPath; 
        }
    }
}
