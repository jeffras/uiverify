using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualUIAVerify.Controls;

namespace VisualUIAVerify.Interfaces
{
    public interface ICodeGenerater
    {
        string Name { get; }
        string Generate(AutomationElementTreeNode node);
    }
}
