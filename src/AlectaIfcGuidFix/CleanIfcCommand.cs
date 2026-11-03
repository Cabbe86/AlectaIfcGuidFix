using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.Win32;

namespace Alecta.Revit
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CleanIfcCommand : IExternalCommand
    {
        private static readonly char[] IfcBase64Chars = 
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_$".ToCharArray();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "IFC-filer (*.ifc)|*.ifc|Alla filer (*.*)|*.*",
                Title = "Välj IFC-filen du vill åtgärda"
            };

            if (openFileDialog.ShowDialog() != true)
            {
                return Result.Cancelled;
            }

            string inputPath = openFileDialog.FileName;
            string dir = Path.GetDirectoryName(inputPath) ?? string.Empty;
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            string ext = Path.GetExtension(inputPath);
            string outputPath = Path.Combine(dir, $"{fileNameWithoutExt}_rensad{ext}");

            Regex ifcPattern = new Regex(
                @"^(#\d+\s*=\s*(?:IFC[A-Z0-9_]*ELEMENT|IFC[A-Z0-9_]*PRODUCT|IFCSLAB|IFCWALL|IFCWINDOW|IFCDOOR|IFCBUILDINGSTOREY|IFCSPACE|IFCREL[A-Z0-9_]+)\s*\(\s*\x27)([A-Za-z0-9_$]{22})(\x27)(.*)$",
                RegexOptions.Compiled
            );

            HashSet<string> seenGuids = new HashSet<string>(StringComparer.Ordinal);
            int fixedCount = 0;

            try
            {
                using (StreamReader reader = new StreamReader(inputPath, Encoding.UTF8))
                using (StreamWriter writer = new StreamWriter(outputPath, false, new UTF8Encoding(false)))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        Match match = ifcPattern.Match(line);
                        if (match.Success)
                        {
                            string prefix = match.Groups[1].Value;
                            string guid = match.Groups[2].Value;
                            string quote = match.Groups[3].Value;
                            string suffix = match.Groups[4].Value;

                            if (seenGuids.Contains(guid))
                            {
                                string newGuid = GenerateIfcGuid();
                                while (seenGuids.Contains(newGuid))
                                {
                                    newGuid = GenerateIfcGuid();
                                }

                                seenGuids.Add(newGuid);
                                fixedCount++;
                                line = prefix + newGuid + quote + suffix;
                            }
                            else
                            {
                                seenGuids.Add(guid);
                            }
                        }

                        writer.WriteLine(line);
                    }
                }

                TaskDialog dialog = new TaskDialog("IFC GlobalId Fix")
                {
                    MainInstruction = "Rensning slutförd!",
                    MainContent = $"Åtgärdade {fixedCount} dubblerade GlobalId.\n\nNy fil sparad:\n{Path.GetFileName(outputPath)}",
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Fel vid IFC-behandling", ex.Message);
                return Result.Failed;
            }
        }

        public static string GenerateIfcGuid()
        {
            byte[] b = Guid.NewGuid().ToByteArray();
            BigInteger num = 0;
            for (int i = 0; i < b.Length; i++)
            {
                num = (num << 8) | b[i];
            }

            char[] result = new char[22];
            for (int i = 21; i >= 0; i--)
            {
                result[i] = IfcBase64Chars[(int)(num & 0x3F)];
                num >>= 6;
            }

            return new string(result);
        }
    }
}
