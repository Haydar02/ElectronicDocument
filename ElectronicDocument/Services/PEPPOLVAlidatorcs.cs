using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using ElectronicDocument.Models;
using Saxon.Api;

public class PeppolValidator
{
    private const string SchematronFileName = "peppol_rules.sch";

    public PeppolValidator() { }

    public void Configure(string schematronDirectory, string xsdFilePath)
    {
        string schematronFilePath = Path.Combine(schematronDirectory, SchematronFileName);

        if (!File.Exists(schematronFilePath))
        {
            throw new FileNotFoundException("The specified Schematron file does not exist.", schematronFilePath);
        }
    }

    public async Task<ValidationResultDTO> Validate(string peppolFilePath, string xsdFilePath, string schematronDirectory)
    {
        try
        {
            if (!File.Exists(peppolFilePath))
            {
                throw new FileNotFoundException("The specified XML file does not exist.", peppolFilePath);
            }

            if (!File.Exists(xsdFilePath))
            {
                throw new FileNotFoundException("The specified XSD file does not exist.", xsdFilePath);
            }

            string schematronFilePath = Path.Combine(schematronDirectory, SchematronFileName);

            if (!File.Exists(schematronFilePath))
            {
                throw new FileNotFoundException("The specified Schematron file does not exist.", schematronFilePath);
            }

            XmlDocument xmlDoc = new XmlDocument();
            await Task.Run(() => xmlDoc.Load(peppolFilePath));

            var xsdValidationResult = XSDValidate(xmlDoc, xsdFilePath);
            var schematronValidationResult = SchematronValidate(xmlDoc, schematronFilePath);

            var combinedErrors = new List<ErrorListDTO>(xsdValidationResult.Errors);
            combinedErrors.AddRange(schematronValidationResult.Errors);

            string status = combinedErrors.Count == 0 ? "Success" : "Error";

            return new ValidationResultDTO
            {
                Status = status,
                Errors = combinedErrors
            };
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

    private ValidationResultDTO SchematronValidate(XmlDocument xmlDoc, string schematronFilePath)
    {
        List<ErrorListDTO> validationErrors = new List<ErrorListDTO>();

        try
        {
            var processor = new Processor();
            var compiler = processor.NewXsltCompiler();
            var executable = compiler.Compile(new Uri(schematronFilePath));
            var transformer = executable.Load();

            using (var xmlReader = new XmlNodeReader(xmlDoc))
            {
                var xdmNode = processor.NewDocumentBuilder().Build(xmlReader);
                transformer.InitialContextNode = xdmNode;
            }

            using (var stringWriter = new StringWriter())
            {
                var serializer = processor.NewSerializer();
                serializer.SetOutputWriter(stringWriter);
                transformer.Run(serializer);
            }

            string formattedResults = FormatResults(validationErrors);
            Console.WriteLine(formattedResults);

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
