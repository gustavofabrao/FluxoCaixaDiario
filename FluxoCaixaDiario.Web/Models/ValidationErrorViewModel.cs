using System;
using System.Collections.Generic;
using System.Linq;

namespace FluxoCaixaDiario.Web.Models
{
    public class ValidationErrorViewModel
    {
        public string Title { get; set; }
        public Dictionary<string, string[]> Errors { get; set; } = new Dictionary<string, string[]>();
         
        public string GetFormattedErrors()
        {
            if (Errors == null || !Errors.Any())
            {
                return Title;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(Title);
            foreach (var error in Errors)
            {
                sb.AppendLine($"- {string.Join(" ", error.Value)}");
            }
            return sb.ToString();
        }

        public string GetErrorsForProperty(string propertyName)
        {
            if (Errors != null && Errors.TryGetValue(propertyName, out var messages))
            {
                return string.Join(" ", messages);
            }
            return string.Empty;
        }
    }
}
