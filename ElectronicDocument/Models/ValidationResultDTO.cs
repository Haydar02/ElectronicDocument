using CsvHelper;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ElectronicDocument.Models
{
    public class ValidationResultDTO
    {
        public string Status { get; set; }
        public List<ErrorListDTO> Errors { get; set; }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        // Serialization to XML
        public string ToXml()
        {
            var serializer = new XmlSerializer(typeof(ValidationResultDTO));
            using (var stringWriter = new StringWriter())
            {
                serializer.Serialize(stringWriter, this);
                return stringWriter.ToString();
            }
        }

        // Serialization to CSV
        public string ToCsv()
        {
            using (var csvWriter = new StringWriter())
            {
                using (var csv = new CsvWriter(csvWriter, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(Errors);
                }
                return csvWriter.ToString();
            }
        }
    }

    public class ErrorListDTO
    {
        public int Id { get; set; }
        public string Message { get; set; }

    }

}
