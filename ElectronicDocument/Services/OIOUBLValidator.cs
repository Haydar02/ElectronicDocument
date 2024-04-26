using ElectronicDocument.Models;
using System.Diagnostics;
using System.Text;
using System.Xml.Schema;
using System.Xml;
using Saxon.Api;

public class OIOUBLValidator
{
    private const string SchematronFileName = "rules.sch";

    public OIOUBLValidator() { }

   

    public async Task<ValidationResultDTO> Validate(string oioublFilePath, string xsdFilePath)
    {
        try
        {
            if (!File.Exists(oioublFilePath))
            {
                throw new FileNotFoundException("The specified XML file does not exist.", oioublFilePath);
            }

            if (!File.Exists(xsdFilePath))
            {
                throw new FileNotFoundException("The specified XSD file does not exist.", xsdFilePath);
            }

            

            XmlDocument xmlDoc = new XmlDocument();
            await Task.Run(() => xmlDoc.Load(oioublFilePath));

            var xsdValidationResult = XSDValidate(xmlDoc, xsdFilePath);
            

            var combinedErrors = new List<ErrorListDTO>(xsdValidationResult.Errors);
           

            string status = combinedErrors.Count == 0 ? "Success" : "Error";

            var validationResult = new ValidationResultDTO
            {
                Status = status,
                Errors = combinedErrors
            };

            
            LogResults(validationResult);

            return validationResult;
        }
        catch (FileNotFoundException ex)
        {
            return new ValidationResultDTO
            {
                Status = "Error",
                Errors = new List<ErrorListDTO> { new ErrorListDTO { Id = 1, Message = ex.Message } }
            };
        }
        catch (Exception ex)
        {
            return new ValidationResultDTO
            {
                Status = "Error",
                Errors = new List<ErrorListDTO> { new ErrorListDTO { Id = 1, Message = $"Validation failed: {ex.Message}" } }
            };
        }
    }

    private void LogResults(ValidationResultDTO validationResult)
    {
        // Log results in different formats
        Debug.WriteLine("JSON Format:");
        Debug.WriteLine(validationResult.ToJson());

        Debug.WriteLine("\nXML Format:");
        Debug.WriteLine(validationResult.ToXml());

        Debug.WriteLine("\nCSV Format:");
        Debug.WriteLine(validationResult.ToCsv());
    }

    private ValidationResultDTO XSDValidate(XmlDocument xmlDoc, string xsdFilePath)
    {
        List<ErrorListDTO> validationErrors = new List<ErrorListDTO>();
        ValidationEventHandler validationEventHandler = (sender, e) =>
        {
            validationErrors.Add(new ErrorListDTO { Id = validationErrors.Count + 1, Message = e.Message });
        };

        XmlSchemaSet schemaSet = new XmlSchemaSet();
        schemaSet.Add("", xsdFilePath);

        xmlDoc.Schemas.Add(schemaSet);
        xmlDoc.Validate(validationEventHandler);

        return new ValidationResultDTO
        {
            Status = validationErrors.Count == 0 ? "Success" : "Error",
            Errors = validationErrors
        };
    }

    private ValidationResultDTO SchematronValidate(string oioublFilePath, string schematronFilePath)
    {
        List<ErrorListDTO> validationErrors = new List<ErrorListDTO>();

        try
        {
            var processor = new Processor();
            var compiler = processor.NewXsltCompiler();
            var executable = compiler.Compile(new Uri(schematronFilePath));
            var transformer = executable.Load();

            var xmlDoc = new XmlDocument();
            xmlDoc.Load(oioublFilePath);

            var xdmNode = processor.NewDocumentBuilder().Build(xmlDoc);
            transformer.InitialContextNode = xdmNode;

            using (var stringWriter = new StringWriter())
            {
                var serializer = processor.NewSerializer();
                serializer.SetOutputWriter(stringWriter);
                transformer.Run(serializer);
            }

            string formattedResults = FormatResults(validationErrors);
            Debug.WriteLine(formattedResults);

         
            return new ValidationResultDTO
            {
                Status = validationErrors.Count == 0 ? "Success" : "Error",
                Errors = validationErrors
            };
        }
        catch (FileNotFoundException ex)
        {
            throw new FileNotFoundException("The specified Schematron file does not exist.", schematronFilePath, ex);
        }
        catch (Exception ex)
        {
            validationErrors.Add(new ErrorListDTO { Id = 1, Message = $"Schematron validation failed: {ex.Message}" });

            return new ValidationResultDTO
            {
                Status = "Error",
                Errors = validationErrors
            };
        }
    }

    private string FormatResults(List<ErrorListDTO> validationErrors)
    {
        var formattedResults = new StringBuilder();

        formattedResults.AppendLine("Schematron Validation Results:");

        foreach (var error in validationErrors)
        {
            formattedResults.AppendLine($"- Error {error.Id}: {error.Message}");
        }

        return formattedResults.ToString();
    }

    
}



