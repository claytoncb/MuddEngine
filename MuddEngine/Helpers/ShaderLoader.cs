using System;
using System.IO;
using Raylib_cs;

namespace MuddEngine.MuddEngine.ShaderHelper
{
    public static class ShaderLoader
    {
        public static Shader LoadShaderWithIncludes(
            string vertexPath,
            string fragmentPath,
            string outputCompiledPath)
        {
            string vertexSource = File.ReadAllText(vertexPath);
            string fragmentSource = File.ReadAllText(fragmentPath);

            string resolvedFragment = ResolveIncludes(fragmentSource, Path.GetDirectoryName(fragmentPath));

            // Write compiled shader for debugging
            Directory.CreateDirectory(Path.GetDirectoryName(outputCompiledPath));
            File.WriteAllText(outputCompiledPath, resolvedFragment);

            return Raylib.LoadShaderFromMemory(vertexSource, resolvedFragment);
        }

        private static string ResolveIncludes(string source, string baseDir, HashSet<string> seen = null)
        {
            seen ??= new HashSet<string>();

            var lines = source.Split('\n');
            var output = new List<string>();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith("#include"))
                {
                    string includeFile = trimmed.Split('"')[1];
                    string includePath = Path.Combine(baseDir, includeFile);

                    if (!File.Exists(includePath))
                        throw new Exception($"Include file not found: {includePath}");

                    if (seen.Contains(includePath))
                        throw new Exception($"Recursive include detected: {includePath}");

                    seen.Add(includePath);

                    string includeSource = File.ReadAllText(includePath);
                    string resolvedInclude = ResolveIncludes(includeSource, baseDir, seen);

                    output.Add(resolvedInclude);
                }
                else
                {
                    output.Add(line);
                }
            }

            return string.Join("\n", output);
        }
    }
}