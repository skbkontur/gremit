using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AsmToBytes
{
    public static class EntryPoint
    {
        public static void Main(string[] args)
        {
            if(args.Length != 4)
            {
                Console.WriteLine("Usage: AsmToBytes <x86|x64> <pathToFasm> <inputFile> <outputFile>");
                return;
            }
            var architecture = args[0];
            var pathToFasm = args[1];
            var inputFile = args[2];
            var outputFile = args[3];
            var dirName = Path.GetDirectoryName(inputFile) ?? "";
            var tempFile = Path.Combine(dirName, "temp.asm");
            var dllFile = Path.Combine(dirName, "temp.dll");
            var marker = Guid.NewGuid().ToByteArray();
            var stringMarker = string.Join(Environment.NewLine, marker.Select(x => string.Format("db 0{0:x2}h", x)));
            string content;
            if(architecture == "x86")
                content = string.Format(@"
format PE GUI 4.0 DLL
entry DllEntryPoint

include '{0}'

section '.text' code readable executable

proc DllEntryPoint hinstDLL, fdwReason, lpvReserved
	mov	eax, TRUE
	ret
endp

{1}
Main:
{2}
{1}

data fixups
end data

section '.edata' export data readable

export 'TEMP.DLL',\
	 Main,'Main'
", Path.Combine(pathToFasm, "include", "win32a.inc"), stringMarker, File.ReadAllText(inputFile));
            else if(architecture == "x64")
                content = string.Format(@"
format PE64 GUI 5.0 DLL
entry DllEntryPoint

include '{0}'

section '.text' code readable executable

proc DllEntryPoint hinstDLL, fdwReason, lpvReserved
	mov	eax, TRUE
	ret
endp

{1}
Main:
{2}
{1}

data fixups
end data

section '.edata' export data readable

export 'TEMP.DLL',\
	 Main,'Main'
", Path.Combine(pathToFasm, "include", "win64a.inc"), stringMarker, File.ReadAllText(inputFile));
            else
            {
                Console.WriteLine("Architecture must be either 'x86' or 'x64'");
                return;
            }
            File.WriteAllText(tempFile, content);
            File.Delete(dllFile);
            var processStartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    Arguments = string.Format(@"""{0}"" ""{1}""", tempFile, dllFile),
                    FileName = Path.Combine(pathToFasm, "fasm.exe")
                };
            var process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };
            process.OutputDataReceived += (sender, e) => Console.WriteLine(e.Data);
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            process.CancelOutputRead();
            if(!File.Exists(dllFile))
            {
                Console.WriteLine("ERROR: dll file has not been created");
                return;
            }
            var dllContent = File.ReadAllBytes(dllFile);
            for(var i = 0; i < dllContent.Length - marker.Length; ++i)
            {
                var ok = true;
                for(var j = 0; j < marker.Length; ++j)
                {
                    if(dllContent[i + j] != marker[j])
                    {
                        ok = false;
                        break;
                    }
                }
                if(!ok) continue;
                i += marker.Length;
                for(var k = i; k < dllContent.Length - marker.Length; ++k)
                {
                    ok = true;
                    for(var j = 0; j < marker.Length; ++j)
                    {
                        if(dllContent[k + j] != marker[j])
                        {
                            ok = false;
                            break;
                        }
                    }
                    if(ok)
                    {
                        var result = new StringBuilder();
                        for(var j = i; j < k; ++j)
                            result.Append(string.Format("0x{0:x2},", dllContent[j]));
                        result.AppendLine();
                        result.AppendLine(Convert.ToBase64String(dllContent, i, k - i));
                        File.WriteAllText(outputFile, result.ToString());
                        Console.WriteLine("SUCCESS");
                        return;
                    }
                }
                Console.WriteLine("ERROR: Cannot find the second marker");
                return;
            }
            Console.WriteLine("ERROR: Cannot find the first marker");
        }
    }
}