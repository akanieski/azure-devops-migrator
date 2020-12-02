using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace AzureDevOpsMigrator.Migrators.Transformation
{
    public class FieldToFieldTransformation : BaseTransformation, ITransformation
    {
        [JsonIgnore]
        public override string Display => "Field to Field";
        public string SourceField { get; set; }
        public string TargetField { get; set; }
        public string ValueExpressionMatch { get; set; } = null;
        public string StringTemplate { get; set; } = null;

        private Regex _valueExpressionMatch { get; set; }

        public override bool Apply(WorkItem sourceWit, WorkItem targetWit)
        {
            if (!string.IsNullOrEmpty(ValueExpressionMatch))
            {
                _valueExpressionMatch = new Regex(ValueExpressionMatch);
            }

            if (sourceWit.Fields.ContainsKey(SourceField) && 
                (string.IsNullOrEmpty(ValueExpressionMatch) || 
                _valueExpressionMatch.IsMatch(sourceWit.Fields[SourceField].ToString())))
            {
                if (!targetWit.Fields.ContainsKey(TargetField))
                {
                    targetWit.Fields.Add(TargetField, null);
                }
                var template = string.IsNullOrEmpty(StringTemplate) ? "{0}" : StringTemplate;
                targetWit.Fields[TargetField] = string.Format(template, sourceWit.Fields[SourceField]);
                return true;
            }

            return false;
        }
    }
}
