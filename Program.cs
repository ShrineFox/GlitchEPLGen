using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GFDStudio;
using GFDLibrary;
using GFDLibrary.Models;

namespace GlitchEPLGen
{
    class Program
    {
        public static float effectScale = 10f;
        public static float modelScale = 0.1f;
        public static float particleScale = 1f;

        static void Main(string[] args)
        {
            Directory.CreateDirectory("./output");
            string outputFile = "./output/output.epl";

            using (WaitForFile(outputFile)) { };

            if (File.Exists(outputFile))
                File.Delete(outputFile);

            using (WaitForFile(outputFile)) { };

            using (FileStream stream = new FileStream(outputFile, FileMode.OpenOrCreate))
            {
                using (EndianBinaryWriter writer = new EndianBinaryWriter(stream, Endianness.BigEndian))
                {
                    // Attachment count
                    var ddsFiles = Directory.GetFiles(args[0], "*.dds", SearchOption.TopDirectoryOnly);
                    writer.Write(Convert.ToUInt32(ddsFiles.Count()));

                    for (int i = 0; i < ddsFiles.Count(); i++)
                    {
                        // Get hashed names
                        string dds = ddsFiles[i];

                        // Add attachment type
                        writer.Write(Convert.ToUInt32(7));

                        writer.Write(File.ReadAllBytes("./EPLParts/epl0"));

                        // Add Effect Scale (X Y Z)
                        writer.Write(effectScale); writer.Write(effectScale); writer.Write(effectScale);

                        writer.Write(File.ReadAllBytes("./EPLParts/epl1"));

                        // Add Node Name
                        WriteName(writer, Path.GetFileNameWithoutExtension(dds));

                        writer.Write(File.ReadAllBytes("./EPLParts/epl2"));

                        // Add Node Name Again
                        WriteName(writer, Path.GetFileNameWithoutExtension(dds));

                        writer.Write(File.ReadAllBytes("./EPLParts/epl3"));

                        // Write Angle Seed
                        writer.Write(Convert.ToUInt32(i));

                        writer.Write(File.ReadAllBytes("./EPLParts/epl4"));

                        // Add Particle Scale
                        writer.Write(particleScale);

                        writer.Write(File.ReadAllBytes("./EPLParts/epl5"));

                        // Add Node Name Again
                        WriteName(writer, Path.GetFileNameWithoutExtension(dds));

                        // Field18
                        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x02 });
                        // Field00
                        writer.Write(new byte[] { 0x00, 0x00, 0x00, 0x02 });
                        
                        // Write GMD Size and GMD Data
                        WriteGMD(writer, dds);

                        writer.Write(File.ReadAllBytes("./EPLParts/epl6"));
                    }
                }
            }

            Console.WriteLine($"Done Generating .EPL data: {outputFile}");
        }

        private static void WriteGMD(EndianBinaryWriter writer, string dds)
        {
            var size = new FileInfo(dds).Length;
            string outputGMD = Path.Combine("./output", Path.GetFileNameWithoutExtension(dds) + ".GMD");

            if (File.Exists(outputGMD))
                File.Delete(outputGMD);

            using (WaitForFile(outputGMD)) { };

            if (size == 5648)
            {
                // 1 x 4 Sprite
                Console.WriteLine($"Generating .GMD: {outputGMD}");
                ModelPack model = Resource.Load<ModelPack>("./GMD/1x4.GMD");
                model.Textures.First().Value.Name = Path.GetFileName(dds);
                model.Textures.First().Value.Data = File.ReadAllBytes(dds);
                model.Materials.First().Value.Name = Path.GetFileNameWithoutExtension(dds);
                model.Materials.First().Value.DiffuseMap.Name = Path.GetFileName(dds);
                model.Model.Nodes.Single(x => x.Name.Equals("SMWSpriteMesh1x4")).Scale = new System.Numerics.Vector3(modelScale, modelScale, modelScale);
                model.Model.Nodes.Single(x => x.Name.Equals("SMWSpriteMesh1x4")).Attachments.First(x => x.GetValue().ResourceType.Equals(ResourceType.Mesh)).GetValue<Mesh>().MaterialName = Path.GetFileNameWithoutExtension(dds);
                model.Save(outputGMD);
            }
            else
            {
                throw new Exception();
            }

            if (File.Exists(outputGMD))
            {
                // Write GMD Size
                writer.Write(Convert.ToUInt32(new FileInfo(outputGMD).Length));

                using (WaitForFile(outputGMD)) { };

                // Write GMD Data
                writer.Write(File.ReadAllBytes(outputGMD));
            }
            else
            {
                throw new Exception();
            }
        }

        private static void WriteName(EndianBinaryWriter writer, string name)
        {
            int nameHash = StringHasher.GenerateStringHash(name);
            writer.Write(Convert.ToUInt16(name.Length));
            writer.Write(Encoding.ASCII.GetBytes(name));
            writer.Write(nameHash);
        }

        public static FileStream WaitForFile(string fullPath,
            FileMode mode = FileMode.Open,
            FileAccess access = FileAccess.ReadWrite,
            FileShare share = FileShare.None)
        {
            for (int numTries = 0; numTries < 10; numTries++)
            {
                FileStream fs = null;
                try
                {
                    fs = new FileStream(fullPath, mode, access, share);
                    return fs;
                }
                catch (IOException)
                {
                    if (fs != null)
                    {
                        fs.Dispose();
                    }
                    Thread.Sleep(100);
                }
            }
            return null;
        }
    }
}
