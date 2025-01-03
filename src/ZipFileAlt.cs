using System.IO;
using System.IO.Compression;

namespace TRLEManager
{
    // Unable to extend static classes, stand alone class needed
    internal static class ZipFileAlt
    {
        internal static void ExtractToDirectory(string sourceArchiveFileName, string destinationDirectoryName, bool overwriteFiles)
        {
            using (ZipArchive archive = ZipFile.OpenRead(sourceArchiveFileName))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    var destinationPath = Path.Combine(destinationDirectoryName, entry.FullName);
                    var isFile = !string.IsNullOrEmpty(Path.GetExtension(destinationPath));
                    var directoryName = Path.GetDirectoryName(destinationPath);

                    if(!Directory.Exists(directoryName))
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));

                    if (!isFile)
                    {
                        // Ensure the destination directory exists
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath));
                    }
                    else if (isFile)
                    {
                        // If overwriteFiles is true, only delete the file it is going to replace
                        if (overwriteFiles && File.Exists(destinationPath))
                            File.Delete(destinationPath);

                        // Extract file if it doesn't exist or if overwrite is enabled
                        if (!File.Exists(destinationPath) || overwriteFiles)
                            entry.ExtractToFile(destinationPath, true);
                    }
                }
            }
        }
    }
}