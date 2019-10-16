using System.IO;
using System.Text;
using Ionic.Zip;
using Ionic.Zlib;

namespace Comindware.Solution.Git.Manager
{
    public class CtfManager
    {
        private const string GitFolder = ".git";

        public void FromCtf(Stream stream, string repositoryDir)
        {
            CleanFolder(repositoryDir);

            using (var zipFile = ZipFile.Read(stream))
            {
                foreach (var zipEntry in zipFile)
                {
                    var file = Path.Combine(repositoryDir, zipEntry.FileName);
                    var dir = Directory.GetParent(file).FullName;
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    var extractedStream = new MemoryStream();
                    zipEntry.Extract(extractedStream);
                    var bytes = extractedStream.ToArray();
                    using (var fs = new FileStream(file, FileMode.Create))
                    {
                        fs.Write(bytes, 0, bytes.Length);
                    }
                }
            }
        }

        public Stream ToCtf(string repositoryDir)
        {
            if (!Directory.Exists(repositoryDir))
            {
                return null;
            }

            var stream = new MemoryStream();

            using (var zip = new ZipOutputStream(stream, true))
            {

                zip.CompressionLevel = CompressionLevel.BestCompression;
                zip.EnableZip64 = Zip64Option.AsNecessary;
                zip.AlternateEncoding = Encoding.UTF8;
                zip.AlternateEncodingUsage = ZipOption.AsNecessary;

                var files = Directory.GetFiles(repositoryDir, "*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (file.Contains(GitFolder))
                    {
                        continue;
                    }

                    var relativePath = file.Substring(repositoryDir.Length).TrimStart(Path.DirectorySeparatorChar);

                    using (var fsr = File.OpenRead(file))
                    {
                        var buffer = new byte[fsr.Length];
                        zip.PutNextEntry(relativePath);
                        fsr.Read(buffer, 0, buffer.Length);
                        zip.Write(buffer, 0, buffer.Length);
                    }
                }
            }

            return stream;
        }

        private void CleanFolder(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(rootPath))
            {
                File.Delete(file);
            }

            foreach (var dir in Directory.EnumerateDirectories(rootPath))
            {
                var name = Path.GetFileName(dir);

                if (name == GitFolder)
                {
                    continue;
                }

                Directory.Delete(dir, true);
            }
        }
    }
}