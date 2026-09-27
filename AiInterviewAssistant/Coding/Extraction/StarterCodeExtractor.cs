using System;
using System.Text;
using System.Windows.Automation;

namespace AiInterviewAssistant.Coding.Extraction
{
    public sealed class StarterCodeExtractor
    {
        public string Extract(AutomationElement window)
        {
            if (window == null)
                return string.Empty;

            try
            {
                AutomationElementCollection elements =
                    window.FindAll(
                        TreeScope.Descendants,
                        Condition.TrueCondition);

                StringBuilder code =
                    new StringBuilder();

                foreach (AutomationElement element in elements)
                {
                    try
                    {
                        if (element.Current.ControlType !=
                            ControlType.Edit)
                        {
                            continue;
                        }

                        string value = string.Empty;

                        object patternObject;

                        if (element.TryGetCurrentPattern(
                            ValuePattern.Pattern,
                            out patternObject))
                        {
                            ValuePattern pattern =
                                patternObject as ValuePattern;

                            if (pattern != null)
                            {
                                value =
                                    pattern.Current.Value;
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            if (code.Length > 0)
                                code.AppendLine();

                            code.Append(value);
                        }
                    }
                    catch
                    {
                    }
                }

                return code.ToString().Trim();
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}