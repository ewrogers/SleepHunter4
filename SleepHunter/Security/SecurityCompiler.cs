using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

using System.Reflection;
using System.CodeDom.Compiler;
using Microsoft.CSharp;

namespace SleepHunter.Security
{
   internal static class SecurityCompiler
   {
      static readonly Aes AesObject = AesManaged.Create();
      static readonly byte[] AesKey = { 0xFD, 0xFC, 0xFA, 0x77, 0x11, 0x13, 0x17, 0x19, 0x23, 0x29, 0x31, 0x37, 0x41, 0x43, 0x47, 0x53 };
      static readonly byte[] AesIV = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53 };

      internal static byte[] Compress(byte[] array)
      {
         using (var inputStream = new MemoryStream(array))
         using (var outputStream = new MemoryStream())
         {
            using (var deflateStream = new DeflateStream(outputStream, CompressionMode.Compress))
               inputStream.CopyTo(deflateStream);

            return outputStream.ToArray();
         }
      }

      internal static byte[] Decompress(byte[] array)
      {
         using(var inputStream = new MemoryStream(array))
         using (var outputStream = new MemoryStream())
         {
            using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
               deflateStream.CopyTo(outputStream);

            return outputStream.ToArray();
         }
      }

      internal static byte[] Decrypt(byte[] array)
      {
         using(var inputStream = new MemoryStream(array))
         using (var outputStream = new MemoryStream())
         {
            using (var aesStream = new CryptoStream(inputStream, AesObject.CreateDecryptor(AesKey, AesIV), CryptoStreamMode.Read))
               aesStream.CopyTo(outputStream);

            return outputStream.ToArray();
         }
      }

      internal static byte[] Encrypt(byte[] array)
      {
         using(var inputStream = new MemoryStream(array))
         using(var outputStream = new MemoryStream())
         {
            using (var aesStream = new CryptoStream(outputStream, AesObject.CreateEncryptor(AesKey, AesIV), CryptoStreamMode.Write))
               inputStream.CopyTo(aesStream);

            return outputStream.ToArray();
         }
      }

      internal static string CompressToBase64(byte[] data)
      {
         return Convert.ToBase64String(data);
      }

      internal static byte[] ExpandFromBase64(string base64String)
      {
         return Convert.FromBase64String(base64String);
      }

      internal static string SecureGenerate64(string source)
      {
         var sourceBytes = Encoding.UTF8.GetBytes(source);
         var compressedBytes = Compress(sourceBytes);
         var encryptedBytes = Encrypt(compressedBytes);
         var encrypted64 = CompressToBase64(encryptedBytes);

         return encrypted64;
      }

      internal static CompilerResults SecureCompile(string secureSource64)
      {
         var sourceBytes = ExpandFromBase64(secureSource64);
         var decryptedSource = Decrypt(sourceBytes);
         var decompressedSource = Decompress(decryptedSource);
         var source = Encoding.UTF8.GetString(decompressedSource);

         return Compile(source);
      }

      internal static CompilerResults Compile(string source)
      {
         var parameters = new CompilerParameters();
         parameters.GenerateExecutable = false;
         parameters.GenerateInMemory = true;
         parameters.IncludeDebugInformation = false;
         parameters.WarningLevel = 3;
         parameters.TreatWarningsAsErrors = false;
         parameters.CompilerOptions = "/optimize";

         var assembly = Assembly.GetExecutingAssembly();

         parameters.ReferencedAssemblies.Add(assembly.Location);

         foreach (var assemblyName in assembly.GetReferencedAssemblies())
            parameters.ReferencedAssemblies.Add(Assembly.Load(assemblyName).Location);

         var compiler = CSharpCodeProvider.CreateProvider("CSharp");
     
         return compiler.CompileAssemblyFromSource(parameters, source);
      }
   }
}
