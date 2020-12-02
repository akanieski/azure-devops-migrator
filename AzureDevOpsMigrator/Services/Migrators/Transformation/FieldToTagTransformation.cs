using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AzureDevOpsMigrator.Migrators.Transformation
{
    public class FieldToTagTransformation : BaseTransformation, ITransformation
    {
        [JsonIgnore]
        public override string Display => "Field to Tag";
        public string SourceField { get; set; }
        public string ValueExpressionMatch { get; set; } = null;
        public string StringTemplate { get; set; } = null;

        private Regex _valueExpressionMatch { get; set; }

        public override bool Apply(WorkItem sourceWit, WorkItem targetWit)
        {
            var TargetField = "System.Tags";
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
                    targetWit.Fields.Add(TargetField, "");
                }
                var tags = targetWit.Fields[TargetField].ToString().Split(';', System.StringSplitOptions.RemoveEmptyEntries).ToList();
                var template = string.IsNullOrEmpty(StringTemplate) ? "{0}" : StringTemplate;
                tags.Add(string.Format(template, sourceWit.Fields[SourceField].ToString()));
                targetWit.Fields[TargetField] = string.Join(',', tags.Distinct());
                return true;
            }

            return false;
        }
    }
}
